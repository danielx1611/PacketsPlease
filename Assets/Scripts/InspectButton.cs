using UnityEngine;

public class InspectButton : MonoBehaviour
{
    [SerializeField] private TrainInspectable targetInspectable;
    [SerializeField] private InspectionPopupUI popupUI;
    [SerializeField] private TrainController trainController;
    [SerializeField] private string tutorialInteractionId = "train_compartment";

    public event System.Action<string> OnTutorialInteractionPerformed;

    private bool tutorialInteractionLocked;
    private bool tutorialInteractionAllowed = true;

    private void Awake()
    {
        if (trainController == null)
        {
            trainController = FindAnyObjectByType<TrainController>();
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
            OnTutorialInteractionPerformed?.Invoke(tutorialInteractionId);
        }
    }

    public void SetTutorialInteractionAllowed(bool isLocked, bool isAllowed)
    {
        tutorialInteractionLocked = isLocked;
        tutorialInteractionAllowed = isAllowed;
    }
}
