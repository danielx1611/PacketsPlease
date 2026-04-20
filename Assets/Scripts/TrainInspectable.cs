using UnityEngine;

public class TrainInspectable : MonoBehaviour
{
    private string inspectionTitle = string.Empty;
    private string inspectionData = string.Empty;

    public void SetInspectionData(string newTitle, string newData)
    {
        inspectionTitle = newTitle?.Trim() ?? string.Empty;
        inspectionData = newData?.Trim() ?? string.Empty;
    }

    public string GetInspectionTitle()
    {
        return string.IsNullOrWhiteSpace(inspectionTitle) ? "Inspection" : inspectionTitle;
    }

    public string GetInspectionData()
    {
        return inspectionData;
    }
}
