using System;
using System.Collections.Generic;
using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    public enum GameState
    {
        Idle,
        Dialogue,
        PlayerAction,
        AwaitingTrigger,
        LevelComplete
    }

    public enum DifficultyPreset
    {
        Easy,
        Normal,
        Hard,
        Custom
    }

    public enum RequiredDecision
    {
        None,
        Accept,
        Reject,
        Either
    }

    [Serializable]
    public class WinConditions
    {
        [Min(0)]
        public int requiredInspectionsComplete;

        public bool requiresDecision = true;
        public RequiredDecision requiredDecision = RequiredDecision.Either;
    }

    [Serializable]
    public class LevelConfig
    {
        [Min(0)]
        public int levelId;

        public DifficultyPreset difficultyPreset = DifficultyPreset.Normal;

        [Range(0.1f, 5f)]
        public float customDifficultyScalar = 1f;

        [Tooltip("Manual page indexes to unlock at level start.")]
        public List<int> manualPagesToUnlock = new();

        [Header("Dialogue Timeline")]
        public DialogueSequence openingDialogue;
        public int openingDialogueLineIndex;
        public DialogueSequence completionDialogue;
        public int completionDialogueLineIndex;

        [Header("Advance Conditions")]
        public WinConditions winConditions = new();
    }

    [Header("Core References")]
    [SerializeField] private GameplayManual gameplayManual;
    [SerializeField] private GameplayDecisionUI gameplayDecisionUI;
    [SerializeField] private CharacterDialogueUI characterDialogueUI;

    [Header("Optional External Controllers")]
    [SerializeField] private MonoBehaviour trainController;
    [SerializeField] private MonoBehaviour inspectionController;

    [Header("Progression Configuration")]
    [SerializeField] private bool autoStartFirstLevelOnStart = true;
    [SerializeField] private int initialLevelIndex;
    [SerializeField] private int manualPageCountForLocking = 6;

    [Tooltip("Global multiplier applied on top of each level difficulty.")]
    [SerializeField] private float globalDifficultyMultiplier = 1f;

    [SerializeField] private List<LevelConfig> levels = new();

    public GameState CurrentState { get; private set; } = GameState.Idle;
    public int CurrentLevelIndex { get; private set; } = -1;
    public float CurrentDifficultyScalar { get; private set; } = 1f;

    private int completedInspections;
    private bool decisionReceived;
    private RequiredDecision latestDecision = RequiredDecision.None;
    private bool waitingForOpeningDialogue;
    private bool waitingForCompletionDialogue;

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void Start()
    {
        if (autoStartFirstLevelOnStart)
        {
            StartLevel(initialLevelIndex);
        }
    }

    private void Update()
    {
        if (CurrentState != GameState.Dialogue || characterDialogueUI == null)
        {
            return;
        }

        if (characterDialogueUI.IsVisible || characterDialogueUI.IsRevealing)
        {
            return;
        }

        if (waitingForOpeningDialogue)
        {
            waitingForOpeningDialogue = false;
            SetState(GameState.PlayerAction);
            return;
        }

        if (waitingForCompletionDialogue)
        {
            waitingForCompletionDialogue = false;
            SetState(GameState.LevelComplete);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    public void StartLevel(int index)
    {
        if (!TryGetLevel(index, out var config))
        {
            Debug.LogWarning($"{nameof(GameplayManager)} could not start level index {index}. Check level list configuration.", this);
            return;
        }

        CurrentLevelIndex = index;
        ResetLevelProgressTracking();

        ApplyDifficultySettings(config);
        ApplyManualUnlocks(config);
        StartOpeningDialogue(config);

        if (!waitingForOpeningDialogue)
        {
            SetState(GameState.PlayerAction);
        }
    }

    public void AdvanceToNextLevel()
    {
        var nextIndex = CurrentLevelIndex + 1;
        if (!TryGetLevel(nextIndex, out _))
        {
            SetState(GameState.LevelComplete);
            Debug.Log("No additional levels configured. Campaign complete.", this);
            return;
        }

        StartLevel(nextIndex);
    }

    public void RestartLevel()
    {
        if (CurrentLevelIndex < 0)
        {
            StartLevel(initialLevelIndex);
            return;
        }

        StartLevel(CurrentLevelIndex);
    }

    public void NotifyInspectionCompleted()
    {
        if (CurrentState != GameState.PlayerAction && CurrentState != GameState.AwaitingTrigger)
        {
            return;
        }

        completedInspections++;
        EvaluateLevelProgression();
    }

    public void NotifyExternalTriggerSatisfied()
    {
        if (CurrentState != GameState.AwaitingTrigger)
        {
            return;
        }

        EvaluateLevelProgression();
    }

    private void SubscribeToEvents()
    {
        if (gameplayDecisionUI == null)
        {
            return;
        }

        gameplayDecisionUI.OnAcceptTrainRequested += HandleAcceptTrainRequested;
        gameplayDecisionUI.OnRejectTrainRequested += HandleRejectTrainRequested;
    }

    private void UnsubscribeFromEvents()
    {
        if (gameplayDecisionUI == null)
        {
            return;
        }

        gameplayDecisionUI.OnAcceptTrainRequested -= HandleAcceptTrainRequested;
        gameplayDecisionUI.OnRejectTrainRequested -= HandleRejectTrainRequested;
    }

    private void HandleAcceptTrainRequested()
    {
        if (CurrentState != GameState.PlayerAction)
        {
            return;
        }

        decisionReceived = true;
        latestDecision = RequiredDecision.Accept;
        SetState(GameState.AwaitingTrigger);
        EvaluateLevelProgression();
    }

    private void HandleRejectTrainRequested()
    {
        if (CurrentState != GameState.PlayerAction)
        {
            return;
        }

        decisionReceived = true;
        latestDecision = RequiredDecision.Reject;
        SetState(GameState.AwaitingTrigger);
        EvaluateLevelProgression();
    }

    private void EvaluateLevelProgression()
    {
        if (!TryGetLevel(CurrentLevelIndex, out var config))
        {
            return;
        }

        if (!AreWinConditionsMet(config.winConditions))
        {
            if (CurrentState == GameState.AwaitingTrigger)
            {
                SetState(GameState.PlayerAction);
            }

            return;
        }

        StartCompletionDialogueOrComplete(config);
    }

    private bool AreWinConditionsMet(WinConditions conditions)
    {
        if (conditions == null)
        {
            return true;
        }

        if (completedInspections < conditions.requiredInspectionsComplete)
        {
            return false;
        }

        if (!conditions.requiresDecision)
        {
            return true;
        }

        if (!decisionReceived)
        {
            return false;
        }

        return conditions.requiredDecision switch
        {
            RequiredDecision.None => true,
            RequiredDecision.Either => latestDecision == RequiredDecision.Accept || latestDecision == RequiredDecision.Reject,
            _ => latestDecision == conditions.requiredDecision
        };
    }

    private void StartCompletionDialogueOrComplete(LevelConfig config)
    {
        if (characterDialogueUI != null && config.completionDialogue != null)
        {
            waitingForCompletionDialogue = true;
            SetState(GameState.Dialogue);
            characterDialogueUI.ShowDialogue(config.completionDialogue, config.completionDialogueLineIndex);
            return;
        }

        SetState(GameState.LevelComplete);
    }

    private void StartOpeningDialogue(LevelConfig config)
    {
        waitingForOpeningDialogue = false;

        if (characterDialogueUI == null || config.openingDialogue == null)
        {
            return;
        }

        waitingForOpeningDialogue = true;
        SetState(GameState.Dialogue);
        characterDialogueUI.ShowDialogue(config.openingDialogue, config.openingDialogueLineIndex);
    }

    private void ResetLevelProgressTracking()
    {
        completedInspections = 0;
        decisionReceived = false;
        latestDecision = RequiredDecision.None;
        waitingForOpeningDialogue = false;
        waitingForCompletionDialogue = false;
    }

    private void ApplyDifficultySettings(LevelConfig config)
    {
        var levelScalar = config.difficultyPreset switch
        {
            DifficultyPreset.Easy => 0.8f,
            DifficultyPreset.Normal => 1f,
            DifficultyPreset.Hard => 1.35f,
            DifficultyPreset.Custom => config.customDifficultyScalar,
            _ => 1f
        };

        CurrentDifficultyScalar = Mathf.Max(0.1f, levelScalar * Mathf.Max(0.1f, globalDifficultyMultiplier));

        Debug.Log($"Starting level {config.levelId} (index {CurrentLevelIndex}) with difficulty scalar {CurrentDifficultyScalar:0.00}.", this);
    }

    private void ApplyManualUnlocks(LevelConfig config)
    {
        if (gameplayManual == null)
        {
            return;
        }

        var pagesToLock = Mathf.Max(0, manualPageCountForLocking);
        for (var i = 0; i < pagesToLock; i++)
        {
            gameplayManual.SetPageUnlocked(i, false);
        }

        if (config.manualPagesToUnlock == null)
        {
            return;
        }

        for (var i = 0; i < config.manualPagesToUnlock.Count; i++)
        {
            gameplayManual.SetPageUnlocked(config.manualPagesToUnlock[i], true);
        }
    }

    private bool TryGetLevel(int index, out LevelConfig config)
    {
        config = null;
        if (levels == null || index < 0 || index >= levels.Count)
        {
            return false;
        }

        config = levels[index];
        return config != null;
    }

    private void SetState(GameState nextState)
    {
        CurrentState = nextState;
    }
}
