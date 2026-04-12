using System;
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

    public event Action OnAcceptTrainRequested;
    public event Action OnRejectTrainRequested;
    public event Action<bool> OnManualVisibilityChanged;

    private void Awake()
    {
        HideManual();
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
        Debug.Log("Accept train requested.");
        OnAcceptTrainRequested?.Invoke();
    }

    public void RejectCurrentTrain()
    {
        Debug.Log("Reject train requested.");
        OnRejectTrainRequested?.Invoke();
    }
}
