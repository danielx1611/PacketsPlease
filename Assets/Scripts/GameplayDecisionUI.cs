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

    [Header("Train Selection")]
    [SerializeField] private RectTransform trainSelectionContainer;
    [SerializeField] private Button trainSelectionButtonTemplate;

    public event Action<IReadOnlyList<int>> OnAcceptTrainRequested;
    public event Action<IReadOnlyList<int>> OnRejectTrainRequested;
    public event Action<bool> OnManualVisibilityChanged;

    private readonly List<Button> trainSelectionButtons = new();
    private readonly List<TMP_Text> trainSelectionLabels = new();
    private readonly List<bool> selectedTrainStates = new();
    private readonly List<bool> resolvedTrainStates = new();
    private bool isReadyToDecide;

    private void Awake()
    {
        HideManual();
        if (trainSelectionButtonTemplate != null)
        {
            trainSelectionButtonTemplate.gameObject.SetActive(false);
        }

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

        var selectedIndices = GetSelectedTrainIndices();
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

        var selectedIndices = GetSelectedTrainIndices();
        if (selectedIndices.Count == 0)
        {
            return;
        }

        Debug.Log("Reject train requested.");
        OnRejectTrainRequested?.Invoke(selectedIndices);
    }

    public void ConfigureTrainSelection(int trainCount)
    {
        for (var i = 0; i < trainSelectionButtons.Count; i++)
        {
            if (trainSelectionButtons[i] != null)
            {
                Destroy(trainSelectionButtons[i].gameObject);
            }
        }

        trainSelectionButtons.Clear();
        trainSelectionLabels.Clear();
        selectedTrainStates.Clear();
        resolvedTrainStates.Clear();

        if (trainSelectionContainer == null || trainSelectionButtonTemplate == null)
        {
            for (var i = 0; i < trainCount; i++)
            {
                selectedTrainStates.Add(true);
                resolvedTrainStates.Add(false);
            }

            return;
        }

        for (var i = 0; i < trainCount; i++)
        {
            var trainButton = Instantiate(trainSelectionButtonTemplate, trainSelectionContainer);
            trainButton.name = $"TrainSelectButton_{i + 1}";
            trainButton.gameObject.SetActive(true);
            if (trainButton.transform is RectTransform rect)
            {
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -24f - (i * 42f));
            }
            var selectedIndex = i;
            trainButton.onClick.AddListener(() => ToggleTrainSelectedState(selectedIndex));

            var label = trainButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = $"☐ Train {i + 1}";
            }

            trainSelectionButtons.Add(trainButton);
            trainSelectionLabels.Add(label);
            selectedTrainStates.Add(false);
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
                if (index < 0 || index >= trainSelectionButtons.Count)
                {
                    if (index >= 0 && index < resolvedTrainStates.Count)
                    {
                        resolvedTrainStates[index] = true;
                    }

                    continue;
                }

                var trainButton = trainSelectionButtons[index];
                if (trainButton == null)
                {
                    continue;
                }

                trainButton.gameObject.SetActive(false);
                if (index < resolvedTrainStates.Count)
                {
                    resolvedTrainStates[index] = true;
                }

                if (index < selectedTrainStates.Count)
                {
                    selectedTrainStates[index] = false;
                }
            }
        }

        ResetDecisionGate();
    }

    private List<int> GetSelectedTrainIndices()
    {
        var selected = new List<int>();
        if (trainSelectionButtons.Count == 0)
        {
            for (var i = 0; i < resolvedTrainStates.Count; i++)
            {
                if (!resolvedTrainStates[i])
                {
                    selected.Add(i);
                }
            }

            return selected;
        }

        for (var i = 0; i < trainSelectionButtons.Count; i++)
        {
            var trainButton = trainSelectionButtons[i];
            if (trainButton != null && trainButton.gameObject.activeSelf && selectedTrainStates[i])
            {
                selected.Add(i);
            }
        }

        return selected;
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

    private void ToggleTrainSelectedState(int index)
    {
        if (index < 0 || index >= selectedTrainStates.Count || (index < resolvedTrainStates.Count && resolvedTrainStates[index]))
        {
            return;
        }

        selectedTrainStates[index] = !selectedTrainStates[index];
        UpdateTrainSelectionLabel(index);
    }

    private void UpdateReadyToDecideLabel()
    {
        if (readyToDecideLabel == null)
        {
            return;
        }

        readyToDecideLabel.text = $"{(isReadyToDecide ? "☑" : "☐")} Ready To Decide";
    }

    private void UpdateTrainSelectionLabel(int index)
    {
        if (index < 0 || index >= trainSelectionLabels.Count)
        {
            return;
        }

        var label = trainSelectionLabels[index];
        if (label == null)
        {
            return;
        }

        label.text = $"{(selectedTrainStates[index] ? "☑" : "☐")} Train {index + 1}";
    }
}
