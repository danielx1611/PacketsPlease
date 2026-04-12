using UnityEngine;

public class InspectButton : MonoBehaviour
{
    [SerializeField] private TrainInspectable targetInspectable;
    [SerializeField] private InspectionPopupUI popupUI;
    [SerializeField] private TrainController trainController;

    private void Awake()
    {
        if (trainController == null)
        {
            trainController = FindObjectOfType<TrainController>();
        }
    }

    public void Inspect()
    {
        if (trainController != null && trainController.IsTrainMoving)
        {
            return;
        }

        if (targetInspectable != null && popupUI != null)
        {
            popupUI.Show(targetInspectable);
        }
    }
}
