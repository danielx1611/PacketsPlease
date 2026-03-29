using UnityEngine;

public class InspectButton : MonoBehaviour
{
    [SerializeField] private TrainInspectable targetInspectable;
    [SerializeField] private InspectionPopupUI popupUI;

    public void Inspect()
    {
        if (targetInspectable != null && popupUI != null)
        {
            popupUI.Show(targetInspectable);
        }
    }
}