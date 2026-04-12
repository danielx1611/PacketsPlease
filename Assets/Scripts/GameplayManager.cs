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

        [Header("Dialogue Flow")]
        public LevelDialoguePlan dialoguePlan;

        [Header("Advance Conditions")]
        public WinConditions winConditions = new();
    }

    [Header("Core References")]
    [SerializeField] private GameplayManual gameplayManual;
    [SerializeField] private GameplayDecisionUI gameplayDecisionUI;
    [SerializeField] private CharacterDialogueUI characterDialogueUI;

    [Header("Optional External Controllers")]
    [SerializeField] private TrainController trainController;
    [SerializeField] private MonoBehaviour inspectionController;

    [Header("Progression Configuration")]
    [SerializeField] private bool autoStartFirstLevelOnStart = true;
    [SerializeField] private int initialLevelIndex;

    [Tooltip("Global multiplier applied on top of each level difficulty.")]
    [SerializeField] private float globalDifficultyMultiplier = 1f;

    [Tooltip("Reserved trigger id used to start optional end-of-level dialogue blocks.")]
    [SerializeField] private string levelCompletionTriggerId = "level_complete";

    [SerializeField] private List<LevelConfig> levels = new();

    public GameState CurrentState { get; private set; } = GameState.Idle;
    public int CurrentLevelIndex { get; private set; } = -1;
    public float CurrentDifficultyScalar { get; private set; } = 1f;

    private readonly Dictionary<string, LevelDialoguePlan.DialogueBlock> blocksById = new(StringComparer.Ordinal);
    private readonly HashSet<string> completedBlocks = new(StringComparer.Ordinal);
    private readonly HashSet<string> startedBlocks = new(StringComparer.Ordinal);

    private int completedInspections;
    private bool decisionReceived;
    private RequiredDecision latestDecision = RequiredDecision.None;
    private bool levelWinConditionsMet;
    private string activeDialogueBlockId;
    private LevelDialoguePlan activeDialoguePlan;

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

        if (!string.IsNullOrWhiteSpace(activeDialogueBlockId))
        {
            OnDialogueBlockCompleted(activeDialogueBlockId);
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
        InitializeDialoguePlan(config.dialoguePlan);
        trainController?.BeginTrainEntry();

        if (!TryStartAutoDialogueForTrigger(string.Empty))
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

    public void StartDialogueBlock(string blockId)
    {
        if (CurrentState == GameState.Dialogue)
        {
            return;
        }

        if (characterDialogueUI == null)
        {
            Debug.LogWarning($"{nameof(GameplayManager)} cannot start dialogue block without {nameof(characterDialogueUI)}.", this);
            return;
        }

        if (!TryGetDialogueBlock(blockId, out var block))
        {
            Debug.LogWarning($"{nameof(GameplayManager)} could not find dialogue block '{blockId}'.", this);
            return;
        }

        if (block.sequence == null)
        {
            Debug.LogWarning($"Dialogue block '{blockId}' has no {nameof(DialogueSequence)} assigned.", this);
            return;
        }

        if (completedBlocks.Contains(blockId))
        {
            return;
        }

        startedBlocks.Add(blockId);
        activeDialogueBlockId = blockId;
        SetState(GameState.Dialogue);
        characterDialogueUI.ShowDialogue(block.sequence, block.startingLineIndex);
    }

    public void OnDialogueBlockCompleted(string blockId)
    {
        if (!string.Equals(activeDialogueBlockId, blockId, StringComparison.Ordinal))
        {
            return;
        }

        activeDialogueBlockId = null;
        completedBlocks.Add(blockId);

        if (levelWinConditionsMet)
        {
            if (!TryStartAutoDialogueForTrigger(levelCompletionTriggerId))
            {
                SetState(GameState.LevelComplete);
            }

            return;
        }

        SetState(GameState.PlayerAction);
    }

    public void ResumeDialogueAfterTrigger(string triggerId)
    {
        var normalizedTriggerId = triggerId?.Trim() ?? string.Empty;

        if (TryStartAutoDialogueForTrigger(normalizedTriggerId))
        {
            return;
        }

        if (CurrentState == GameState.AwaitingTrigger)
        {
            EvaluateLevelProgression();
            return;
        }

        if (levelWinConditionsMet && string.Equals(normalizedTriggerId, levelCompletionTriggerId, StringComparison.Ordinal))
        {
            SetState(GameState.LevelComplete);
        }
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

        trainController?.SendAcceptedTrainsOffScreen();
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

        trainController?.SendRejectedTrainsOffScreen();
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

        levelWinConditionsMet = true;
        ResumeDialogueAfterTrigger(levelCompletionTriggerId);
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

    private void InitializeDialoguePlan(LevelDialoguePlan plan)
    {
        activeDialoguePlan = plan;
        blocksById.Clear();
        completedBlocks.Clear();
        startedBlocks.Clear();
        activeDialogueBlockId = null;

        if (activeDialoguePlan == null || activeDialoguePlan.DialogueBlocks == null)
        {
            return;
        }

        var blocks = activeDialoguePlan.DialogueBlocks;
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null || string.IsNullOrWhiteSpace(block.blockId))
            {
                continue;
            }

            if (blocksById.ContainsKey(block.blockId))
            {
                Debug.LogWarning($"Duplicate dialogue block id '{block.blockId}' in {activeDialoguePlan.name}.", activeDialoguePlan);
                continue;
            }

            blocksById.Add(block.blockId, block);
        }
    }

    private bool TryStartAutoDialogueForTrigger(string triggerId)
    {
        if (CurrentState == GameState.Dialogue || activeDialoguePlan == null)
        {
            return false;
        }

        var normalizedTriggerId = triggerId?.Trim() ?? string.Empty;
        var blocks = activeDialoguePlan.DialogueBlocks;
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null || !block.autoStart || string.IsNullOrWhiteSpace(block.blockId))
            {
                continue;
            }

            if (startedBlocks.Contains(block.blockId) || completedBlocks.Contains(block.blockId))
            {
                continue;
            }

            var blockTrigger = block.triggerId?.Trim() ?? string.Empty;
            if (!string.Equals(blockTrigger, normalizedTriggerId, StringComparison.Ordinal))
            {
                continue;
            }

            StartDialogueBlock(block.blockId);
            return true;
        }

        return false;
    }

    private bool TryGetDialogueBlock(string blockId, out LevelDialoguePlan.DialogueBlock block)
    {
        block = null;
        if (string.IsNullOrWhiteSpace(blockId))
        {
            return false;
        }

        return blocksById.TryGetValue(blockId, out block);
    }

    private void ResetLevelProgressTracking()
    {
        completedInspections = 0;
        decisionReceived = false;
        latestDecision = RequiredDecision.None;
        levelWinConditionsMet = false;
        activeDialogueBlockId = null;
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

        gameplayManual.SetUnlockedPages(config.manualPagesToUnlock, config.levelId);
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
