using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayDecisionUI : MonoBehaviour
{
    private const string OpenManualInteractionId = "open_manual";
    private const string CloseManualInteractionId = "close_manual";
    private const string AcceptTrainInteractionId = "accept_train";
    private const string RejectTrainInteractionId = "reject_train";

    [Header("Manual UI")]
    [SerializeField] private GameObject manualMenuRoot;
    [SerializeField] private Button manualToggleButton;
    [SerializeField] private Button manualCloseButton;

    [Header("Decision Buttons")]
    [SerializeField] private Button acceptTrainButton;
    [SerializeField] private Button rejectTrainButton;
    [SerializeField] private Button readyToDecideButton;
    [SerializeField] private TMP_Text readyToDecideLabel;

    [Header("Lives UI")]
    [SerializeField] private Image[] lifeHeartIcons = Array.Empty<Image>();
    [SerializeField] private Sprite heartFullIcon;
    [SerializeField] private Sprite heartEmptyIcon;

    public event Action<IReadOnlyList<int>> OnAcceptTrainRequested;
    public event Action<IReadOnlyList<int>> OnRejectTrainRequested;
    public event Action<bool> OnManualVisibilityChanged;
    public event Action<string> OnTutorialInteractionPerformed;

    private readonly List<bool> resolvedTrainStates = new();
    private bool isReadyToDecide;
    private bool hasTutorialInteractionLock;
    private readonly HashSet<string> allowedTutorialInteractions = new();

    private void Awake()
    {
        HideManual();
        UpdateReadyToDecideLabel();
        RefreshDecisionButtonState();
        RefreshLives(0);
    }

    private void OnEnable()
    {
        if (manualToggleButton != null)
        {
            manualToggleButton.onClick.AddListener(ToggleManual);
        }

        if (manualCloseButton != null)
        {
            manualCloseButton.onClick.AddListener(HideManualFromCloseButton);
        }

        if (acceptTrainButton != null)
        {
            acceptTrainButton.onClick.AddListener(AcceptCurrentTrain);
        }

        if (rejectTrainButton != null)
        {
            rejectTrainButton.onClick.AddListener(RejectCurrentTrain);
        }

        if (readyToDecideButton != null)
        {
            readyToDecideButton.onClick.AddListener(ToggleReadyToDecide);
        }

        RefreshDecisionButtonState();
    }

    private void OnDisable()
    {
        if (manualToggleButton != null)
        {
            manualToggleButton.onClick.RemoveListener(ToggleManual);
        }

        if (manualCloseButton != null)
        {
            manualCloseButton.onClick.RemoveListener(HideManualFromCloseButton);
        }

        if (acceptTrainButton != null)
        {
            acceptTrainButton.onClick.RemoveListener(AcceptCurrentTrain);
        }

        if (rejectTrainButton != null)
        {
            rejectTrainButton.onClick.RemoveListener(RejectCurrentTrain);
        }

        if (readyToDecideButton != null)
        {
            readyToDecideButton.onClick.RemoveListener(ToggleReadyToDecide);
        }
    }

    public void ToggleManual()
    {
        if (manualMenuRoot == null)
        {
            return;
        }

        if (manualMenuRoot.activeSelf)
        {
            if (!IsTutorialInteractionAllowed(CloseManualInteractionId))
            {
                return;
            }

            HideManual();
            OnTutorialInteractionPerformed?.Invoke(CloseManualInteractionId);
            return;
        }

        if (!IsTutorialInteractionAllowed(OpenManualInteractionId))
        {
            return;
        }

        ShowManual();
        OnTutorialInteractionPerformed?.Invoke(OpenManualInteractionId);
    }

    public void ShowManual()
    {
        if (!IsTutorialInteractionAllowed(OpenManualInteractionId))
        {
            return;
        }

        if (manualMenuRoot == null)
        {
            return;
        }

        manualMenuRoot.SetActive(true);
        OnManualVisibilityChanged?.Invoke(true);
    }

    public void HideManual()
    {
        if (!IsTutorialInteractionAllowed(CloseManualInteractionId))
        {
            return;
        }

        if (manualMenuRoot == null)
        {
            return;
        }

        manualMenuRoot.SetActive(false);
        OnManualVisibilityChanged?.Invoke(false);
    }

    public void AcceptCurrentTrain()
    {
        if (!IsTutorialInteractionAllowed(AcceptTrainInteractionId))
        {
            return;
        }

        if (!IsReadyToSubmitDecision())
        {
            return;
        }

        var selectedIndices = GetUnresolvedTrainIndices();
        if (selectedIndices.Count == 0)
        {
            return;
        }

        Debug.Log("Accept train requested.");
        OnAcceptTrainRequested?.Invoke(selectedIndices);
        OnTutorialInteractionPerformed?.Invoke(AcceptTrainInteractionId);
    }

    public void RejectCurrentTrain()
    {
        if (!IsTutorialInteractionAllowed(RejectTrainInteractionId))
        {
            return;
        }

        if (!IsReadyToSubmitDecision())
        {
            return;
        }

        var selectedIndices = GetUnresolvedTrainIndices();
        if (selectedIndices.Count == 0)
        {
            return;
        }

        Debug.Log("Reject train requested.");
        OnRejectTrainRequested?.Invoke(selectedIndices);
        OnTutorialInteractionPerformed?.Invoke(RejectTrainInteractionId);
    }

    private void HideManualFromCloseButton()
    {
        if (!IsTutorialInteractionAllowed(CloseManualInteractionId))
        {
            return;
        }

        HideManual();
        OnTutorialInteractionPerformed?.Invoke(CloseManualInteractionId);
    }

    public void SetTutorialInteractionLock(bool isLocked, IReadOnlyCollection<string> allowedInteractionIds)
    {
        hasTutorialInteractionLock = isLocked;
        allowedTutorialInteractions.Clear();

        if (isLocked && allowedInteractionIds != null)
        {
            foreach (var interactionId in allowedInteractionIds)
            {
                if (!string.IsNullOrWhiteSpace(interactionId))
                {
                    allowedTutorialInteractions.Add(interactionId.Trim());
                }
            }
        }

        RefreshTutorialLockState();
    }

    public void ConfigureTrainSelection(int trainCount)
    {
        resolvedTrainStates.Clear();

        for (var i = 0; i < trainCount; i++)
        {
            resolvedTrainStates.Add(false);
        }

        ResetDecisionGate();
    }

    public void ConfigureTrainSelection(int trainCount, IReadOnlyList<int> selectableTrainIndices)
    {
        resolvedTrainStates.Clear();

        for (var i = 0; i < trainCount; i++)
        {
            resolvedTrainStates.Add(true);
        }

        if (selectableTrainIndices == null || selectableTrainIndices.Count == 0)
        {
            for (var i = 0; i < trainCount; i++)
            {
                resolvedTrainStates[i] = false;
            }
        }
        else
        {
            for (var i = 0; i < selectableTrainIndices.Count; i++)
            {
                var index = selectableTrainIndices[i];
                if (index >= 0 && index < trainCount)
                {
                    resolvedTrainStates[index] = false;
                }
            }
        }

        ResetDecisionGate();
    }

    public void ResolveSelectedTrains(IReadOnlyList<int> resolvedTrainIndices)
    {
        if (resolvedTrainIndices != null)
        {
            for (var i = 0; i < resolvedTrainIndices.Count; i++)
            {
                var index = resolvedTrainIndices[i];
                if (index >= 0 && index < resolvedTrainStates.Count)
                {
                    resolvedTrainStates[index] = true;
                }
            }
        }

        ResetDecisionGate();
    }

    private List<int> GetUnresolvedTrainIndices()
    {
        var unresolved = new List<int>();

        for (var i = 0; i < resolvedTrainStates.Count; i++)
        {
            if (!resolvedTrainStates[i])
            {
                unresolved.Add(i);
            }
        }

        return unresolved;
    }

    private bool IsReadyToSubmitDecision()
    {
        if (readyToDecideButton == null)
        {
            return true;
        }

        if (!hasTutorialInteractionLock)
        {
            return isReadyToDecide;
        }

        var tutorialAllowsDirectDecision =
            IsTutorialInteractionAllowed(AcceptTrainInteractionId) ||
            IsTutorialInteractionAllowed(RejectTrainInteractionId);

        return tutorialAllowsDirectDecision || isReadyToDecide;
    }

    private void ToggleReadyToDecide()
    {
        isReadyToDecide = !isReadyToDecide;
        UpdateReadyToDecideLabel();
        RefreshDecisionButtonState();
    }

    private void ResetDecisionGate()
    {
        isReadyToDecide = false;
        UpdateReadyToDecideLabel();
        RefreshDecisionButtonState();
    }

    private void RefreshDecisionButtonState()
    {
        var canDecide = IsReadyToSubmitDecision();
        if (acceptTrainButton != null)
        {
            acceptTrainButton.interactable = canDecide && IsTutorialInteractionAllowed(AcceptTrainInteractionId);
        }

        if (rejectTrainButton != null)
        {
            rejectTrainButton.interactable = canDecide && IsTutorialInteractionAllowed(RejectTrainInteractionId);
        }
    }

    private void RefreshTutorialLockState()
    {
        SetButtonTutorialInteractable(manualToggleButton, OpenManualInteractionId);
        SetButtonTutorialInteractable(manualCloseButton, CloseManualInteractionId);
        SetButtonTutorialInteractable(acceptTrainButton, AcceptTrainInteractionId);
        SetButtonTutorialInteractable(rejectTrainButton, RejectTrainInteractionId);
        RefreshDecisionButtonState();

        if (readyToDecideButton != null)
        {
            readyToDecideButton.interactable = !hasTutorialInteractionLock || IsTutorialInteractionAllowed("ready_to_decide");
        }
    }

    private void SetButtonTutorialInteractable(Button button, string interactionId)
    {
        if (button == null)
        {
            return;
        }

        if (hasTutorialInteractionLock && !IsTutorialInteractionAllowed(interactionId))
        {
            button.interactable = false;
            return;
        }

        if (button == acceptTrainButton || button == rejectTrainButton)
        {
            return;
        }

        button.interactable = true;
    }

    private bool IsTutorialInteractionAllowed(string interactionId)
    {
        return !hasTutorialInteractionLock || (interactionId != null && allowedTutorialInteractions.Contains(interactionId));
    }

    private void UpdateReadyToDecideLabel()
    {
        if (readyToDecideLabel == null)
        {
            return;
        }

        readyToDecideLabel.text = $"{(isReadyToDecide ? "Set Not" : "Set")} Ready To Decide";
    }

    public void RefreshLives(int livesRemaining)
    {
        if (lifeHeartIcons == null || lifeHeartIcons.Length == 0)
        {
            return;
        }

        var clampedLives = Mathf.Max(0, livesRemaining);
        for (var i = 0; i < lifeHeartIcons.Length; i++)
        {
            var heartIcon = lifeHeartIcons[i];
            if (heartIcon == null)
            {
                continue;
            }

            heartIcon.sprite = i < clampedLives ? heartFullIcon : heartEmptyIcon;
            heartIcon.enabled = heartIcon.sprite != null;
        }
    }
}
