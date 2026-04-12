using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterDialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject rootContainer;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text continueHintText;

    [Header("Reveal Timing")]
    [Min(0f)]
    [SerializeField] private float lettersPerSecond = 30f;
    [Min(0f)]
    [SerializeField] private float acceleratedLettersPerSecond = 120f;

    private Coroutine revealCoroutine;
    private bool hasAcceleratedReveal;
    private bool skipToEndRequested;
    private string currentFullText;
    private float currentLettersPerSecond;
    private DialogueSequence activeSequence;
    private int activeSequenceLineIndex;

    public bool IsRevealing { get; private set; }
    public bool IsVisible => rootContainer != null && rootContainer.activeSelf;
    public bool IsAcceleratingReveal => IsRevealing && hasAcceleratedReveal;

    private void Awake()
    {
        if (!ValidateCoreReferences())
        {
            return;
        }

        HideDialogue();
    }

    private void Update()
    {
        if (!IsVisible)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (IsRevealing)
        {
            if (!hasAcceleratedReveal)
            {
                hasAcceleratedReveal = true;
                return;
            }

            skipToEndRequested = true;
            return;
        }

        AdvanceDialogue();
    }

    public void ShowDialogue(DialogueLine line)
    {
        ResetSequenceProgress();

        if (line == null)
        {
            ShowDialogue(string.Empty);
            return;
        }

        ShowDialogue(line.text, line.portrait, line.speakerName, line.useCustomRevealSpeed, line.revealLettersPerSecond);
    }

    public void ShowDialogue(DialogueSequence sequence, int lineIndex = 0)
    {
        if (sequence == null)
        {
            ResetSequenceProgress();
            ShowDialogue(string.Empty);
            return;
        }

        activeSequence = sequence;
        activeSequenceLineIndex = Mathf.Max(0, lineIndex);
        ShowDialogueLineFromActiveSequence();
    }

    public void ShowDialogue(string text, Sprite portrait = null, string speakerName = null)
    {
        ResetSequenceProgress();
        ShowDialogue(text, portrait, speakerName, false, lettersPerSecond);
    }

    private void ShowDialogue(string text, Sprite portrait, string speakerName, bool useCustomRevealSpeed, float lineRevealSpeed)
    {
        if (!ValidateCoreReferences())
        {
            return;
        }

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        rootContainer.SetActive(true);

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        SetSpeakerName(speakerName);
        currentFullText = text ?? string.Empty;
        currentLettersPerSecond = useCustomRevealSpeed ? Mathf.Max(0f, lineRevealSpeed) : lettersPerSecond;
        dialogueText.text = string.Empty;
        hasAcceleratedReveal = false;
        skipToEndRequested = false;
        SetContinueHintVisible(false);

        revealCoroutine = StartCoroutine(RevealTextCoroutine());
    }

    public void HideDialogue()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        IsRevealing = false;
        hasAcceleratedReveal = false;
        skipToEndRequested = false;
        currentFullText = string.Empty;
        currentLettersPerSecond = lettersPerSecond;
        ResetSequenceProgress();
        SetContinueHintVisible(false);
        SetSpeakerName(null);

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (rootContainer != null)
        {
            rootContainer.SetActive(false);
        }
    }

    private IEnumerator RevealTextCoroutine()
    {
        IsRevealing = true;

        for (var i = 0; i < currentFullText.Length; i++)
        {
            if (skipToEndRequested)
            {
                dialogueText.text = currentFullText;
                break;
            }

            dialogueText.text += currentFullText[i];

            var revealSpeed = hasAcceleratedReveal ? acceleratedLettersPerSecond : currentLettersPerSecond;
            if (revealSpeed <= 0f)
            {
                continue;
            }

            yield return new WaitForSeconds(1f / revealSpeed);
        }

        dialogueText.text = currentFullText;
        IsRevealing = false;
        hasAcceleratedReveal = false;
        skipToEndRequested = false;
        revealCoroutine = null;
        SetContinueHintVisible(true);
    }

    private bool ValidateCoreReferences()
    {
        if (rootContainer == null)
        {
            Debug.LogWarning($"{nameof(CharacterDialogueUI)} on '{name}' is missing {nameof(rootContainer)} reference.", this);
            return false;
        }

        if (dialogueText == null)
        {
            Debug.LogWarning($"{nameof(CharacterDialogueUI)} on '{name}' is missing {nameof(dialogueText)} reference.", this);
            return false;
        }

        return true;
    }

    private void AdvanceDialogue()
    {
        if (activeSequence == null)
        {
            HideDialogue();
            return;
        }

        activeSequenceLineIndex++;
        ShowDialogueLineFromActiveSequence();
    }

    private void ShowDialogueLineFromActiveSequence()
    {
        if (activeSequence == null)
        {
            HideDialogue();
            return;
        }

        var line = activeSequence.GetLine(activeSequenceLineIndex);
        if (line == null)
        {
            HideDialogue();
            return;
        }

        ShowDialogue(line.text, line.portrait, line.speakerName, line.useCustomRevealSpeed, line.revealLettersPerSecond);
    }

    private void ResetSequenceProgress()
    {
        activeSequence = null;
        activeSequenceLineIndex = 0;
    }

    private void SetContinueHintVisible(bool isVisible)
    {
        if (continueHintText == null)
        {
            return;
        }

        continueHintText.text = "Press left click to continue";
        continueHintText.gameObject.SetActive(isVisible);
    }

    private void SetSpeakerName(string speakerName)
    {
        if (speakerNameText == null)
        {
            return;
        }

        var hasSpeaker = !string.IsNullOrWhiteSpace(speakerName);
        speakerNameText.text = hasSpeaker ? $"{speakerName}:" : string.Empty;
        speakerNameText.gameObject.SetActive(hasSpeaker);
    }
}
