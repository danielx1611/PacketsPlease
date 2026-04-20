using UnityEngine;
using UnityEngine.UI;

public class InspectButton : MonoBehaviour
{
    private const string DefaultTutorialInteractionId = "train_compartment";
    private const string DefaultDialogueContinueTriggerId = "train_compartment_inspected";

    [SerializeField] private TrainInspectable targetInspectable;
    [SerializeField] private InspectionPopupUI popupUI;
    [SerializeField] private TrainController trainController;
    [SerializeField] private Button button;
    [SerializeField] private string tutorialInteractionId = DefaultTutorialInteractionId;
    [SerializeField] private GameplayTriggerRelay continueDialogueTriggerRelay;
    [SerializeField] private string continueDialogueTriggerId = DefaultDialogueContinueTriggerId;

    public event System.Action<string> OnTutorialInteractionPerformed;
    public string TutorialInteractionId => string.IsNullOrWhiteSpace(tutorialInteractionId)
        ? DefaultTutorialInteractionId
        : tutorialInteractionId.Trim();

    private bool tutorialInteractionLocked;
    private bool tutorialInteractionAllowed = true;

    private void Awake()
    {
        if (trainController == null)
        {
            trainController = FindAnyObjectByType<TrainController>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    public void Inspect()
    {
        if (tutorialInteractionLocked && !tutorialInteractionAllowed)
        {
            return;
        }

        if (trainController != null && trainController.IsTrainMoving)
        {
            return;
        }

        if (targetInspectable != null && popupUI != null)
        {
            popupUI.Show(targetInspectable);
            OnTutorialInteractionPerformed?.Invoke(TutorialInteractionId);
            TriggerDialogueContinue();
        }
    }

    public void SetTutorialInteractionAllowed(bool isLocked, bool isAllowed)
    {
        tutorialInteractionLocked = isLocked;
        tutorialInteractionAllowed = isAllowed;

        if (button != null)
        {
            button.interactable = !tutorialInteractionLocked || tutorialInteractionAllowed;
        }
    }

    private void TriggerDialogueContinue()
    {
        if (continueDialogueTriggerRelay == null || string.IsNullOrWhiteSpace(continueDialogueTriggerId))
        {
            return;
        }

        continueDialogueTriggerRelay.RaiseTrigger(continueDialogueTriggerId.Trim());
    }
}
