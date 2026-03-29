using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectionPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    private TrainInspectable currentInspectable;

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
        popupRoot.SetActive(false);
        currentInspectable = null;
    }
}