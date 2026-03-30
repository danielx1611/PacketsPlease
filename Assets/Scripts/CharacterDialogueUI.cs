using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterDialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject rootContainer;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text dialogueText;

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
        if (!IsRevealing)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!hasAcceleratedReveal)
        {
            hasAcceleratedReveal = true;
            return;
        }

        skipToEndRequested = true;
    }

    public void ShowDialogue(DialogueLine line)
    {
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
            ShowDialogue(string.Empty);
            return;
        }

        var line = sequence.GetLine(lineIndex);
        ShowDialogue(line);
    }

    public void ShowDialogue(string text, Sprite portrait = null, string speakerName = null)
    {
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

        currentFullText = FormatDialogueText(text, speakerName);
        currentLettersPerSecond = useCustomRevealSpeed ? Mathf.Max(0f, lineRevealSpeed) : lettersPerSecond;
        dialogueText.text = string.Empty;
        hasAcceleratedReveal = false;
        skipToEndRequested = false;

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
    }

    private string FormatDialogueText(string text, string speakerName)
    {
        var safeText = text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(speakerName))
        {
            return safeText;
        }

        return speakerName + ":\n" + safeText;
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
}
