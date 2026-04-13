using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayDecisionUI : MonoBehaviour
{
    [Header("Manual UI")]
    [SerializeField] private GameObject manualMenuRoot;
    [SerializeField] private Button manualToggleButton;
    [SerializeField] private Button manualCloseButton;

    [Header("Decision Buttons")]
    [SerializeField] private Button acceptTrainButton;
    [SerializeField] private Button rejectTrainButton;
    [SerializeField] private Button readyToDecideButton;
    [SerializeField] private TMP_Text readyToDecideLabel;

    public event Action<IReadOnlyList<int>> OnAcceptTrainRequested;
    public event Action<IReadOnlyList<int>> OnRejectTrainRequested;
    public event Action<bool> OnManualVisibilityChanged;

    private readonly List<bool> resolvedTrainStates = new();
    private bool isReadyToDecide;

    private void Awake()
    {
        HideManual();
        UpdateReadyToDecideLabel();
        RefreshDecisionButtonState();
    }

    private void OnEnable()
    {
        if (manualToggleButton != null)
        {
            manualToggleButton.onClick.AddListener(ToggleManual);
        }

        if (manualCloseButton != null)
        {
            manualCloseButton.onClick.AddListener(HideManual);
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
            manualCloseButton.onClick.RemoveListener(HideManual);
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
            HideManual();
            return;
        }

        ShowManual();
    }

    public void ShowManual()
    {
        if (manualMenuRoot == null)
        {
            return;
        }

        manualMenuRoot.SetActive(true);
        OnManualVisibilityChanged?.Invoke(true);
    }

    public void HideManual()
    {
        if (manualMenuRoot == null)
        {
            return;
        }

        manualMenuRoot.SetActive(false);
        OnManualVisibilityChanged?.Invoke(false);
    }

    public void AcceptCurrentTrain()
    {
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
    }

    public void RejectCurrentTrain()
    {
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
        return readyToDecideButton == null || isReadyToDecide;
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
            acceptTrainButton.interactable = canDecide;
        }

        if (rejectTrainButton != null)
        {
            rejectTrainButton.interactable = canDecide;
        }
    }

    private void UpdateReadyToDecideLabel()
    {
        if (readyToDecideLabel == null)
        {
            return;
        }

        readyToDecideLabel.text = $"{(isReadyToDecide ? "Set Not" : "Set")} Ready To Decide";
    }
}
