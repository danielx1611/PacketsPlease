using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea]
    public string text;

    public string speakerName;
    public Sprite portrait;

    [Tooltip("Enable to override the dialogue UI's default reveal speed for this line.")]
    public bool useCustomRevealSpeed;

    [Min(0f)]
    [Tooltip("Characters revealed per second when using a custom reveal speed.")]
    public float revealLettersPerSecond = 30f;

    public DialogueLine(string text, Sprite portrait = null, string speakerName = null)
    {
        this.text = text;
        this.portrait = portrait;
        this.speakerName = speakerName;
        useCustomRevealSpeed = false;
        revealLettersPerSecond = 30f;
    }

    public DialogueLine(
        string text,
        Sprite portrait,
        string speakerName,
        bool useCustomRevealSpeed,
        float revealLettersPerSecond)
    {
        this.text = text;
        this.portrait = portrait;
        this.speakerName = speakerName;
        this.useCustomRevealSpeed = useCustomRevealSpeed;
        this.revealLettersPerSecond = revealLettersPerSecond;
    }
}
