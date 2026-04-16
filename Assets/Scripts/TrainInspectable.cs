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
    public string payload;

    public void SetPayloadData(string newComponentName, string newDescription, bool newIsSuspicious, string newSuspiciousReason)
    {
        componentName = newComponentName;
        description = newDescription;
        isSuspicious = newIsSuspicious;
        suspiciousReason = newSuspiciousReason;
    }

    public void SetPayload(string newPayload)
    {
        payload = newPayload;
    }

    public string GetInspectionText()
    {
        var payloadText = string.IsNullOrWhiteSpace(payload)
            ? string.Empty
            : $"Payload: {payload}\n\n";

        if (isSuspicious && !string.IsNullOrEmpty(suspiciousReason))
            return payloadText + description + "\n\nSuspicious: " + suspiciousReason;

        return payloadText + description;
    }
}
