using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectionPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private string closeInteractionId = "close_train_compartment_popup";

    private TrainInspectable currentInspectable;
    public event System.Action<string> OnTutorialInteractionPerformed;

    private void Start()
    {
        Hide();
    }

    public void Show(TrainInspectable inspectable)
    {
        currentInspectable = inspectable;

        popupRoot.SetActive(true);
        titleText.text = inspectable.componentName;
        bodyText.text = inspectable.GetInspectionText();
    }

    public void Hide()
    {
        var wasVisible = popupRoot != null && popupRoot.activeSelf;
        popupRoot.SetActive(false);
        currentInspectable = null;

        if (wasVisible && !string.IsNullOrWhiteSpace(closeInteractionId))
        {
            OnTutorialInteractionPerformed?.Invoke(closeInteractionId.Trim());
        }
    }
}
