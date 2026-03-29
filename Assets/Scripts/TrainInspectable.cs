using UnityEngine;

public class TrainInspectable : MonoBehaviour
{
    [Header("Display")]
    public string componentName;
    [TextArea(3, 6)]
    public string description;

    [Header("Gameplay")]
    public bool isSuspicious;
    public string suspiciousReason;

    public string GetInspectionText()
    {
        if (isSuspicious && !string.IsNullOrEmpty(suspiciousReason))
            return description + "\n\nSuspicious: " + suspiciousReason;

        return description;
    }
}