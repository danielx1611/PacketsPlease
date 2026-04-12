using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayManual : MonoBehaviour
{
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private List<Button> tabButtons = new();
    [SerializeField] private int startingPageIndex;

    private readonly List<UnityEngine.Events.UnityAction> tabClickHandlers = new();
    private int currentPageIndex = -1;

    private void OnEnable()
    {
        BindTabButtons();
        ShowFirstAvailablePage();
    }

    private void OnDisable()
    {
        UnbindTabButtons();
    }

    public void SetPageUnlocked(int pageIndex, bool unlocked)
    {
        if (!IsValidPageIndex(pageIndex))
        {
            return;
        }

        tabButtons[pageIndex].interactable = unlocked;

        if (currentPageIndex == pageIndex && !unlocked)
        {
            ShowFirstAvailablePage();
        }
    }

    public void ShowPage(int pageIndex)
    {
        if (!IsValidPageIndex(pageIndex) || !tabButtons[pageIndex].interactable)
        {
            return;
        }

        currentPageIndex = pageIndex;

        if (pageText != null)
        {
            pageText.text = GetHardcodedPageContent(pageIndex);
        }
    }

    private void BindTabButtons()
    {
        UnbindTabButtons();

        for (var index = 0; index < tabButtons.Count; index++)
        {
            var tabButton = tabButtons[index];
            if (tabButton == null)
            {
                tabClickHandlers.Add(null);
                continue;
            }

            var capturedIndex = index;
            UnityEngine.Events.UnityAction clickHandler = () => ShowPage(capturedIndex);
            tabClickHandlers.Add(clickHandler);
            tabButton.onClick.AddListener(clickHandler);
        }
    }

    private void UnbindTabButtons()
    {
        for (var index = 0; index < tabButtons.Count && index < tabClickHandlers.Count; index++)
        {
            var tabButton = tabButtons[index];
            var clickHandler = tabClickHandlers[index];
            if (tabButton == null || clickHandler == null)
            {
                continue;
            }

            tabButton.onClick.RemoveListener(clickHandler);
        }

        tabClickHandlers.Clear();
    }

    private void ShowFirstAvailablePage()
    {
        if (tabButtons.Count == 0)
        {
            if (pageText != null)
            {
                pageText.text = "No pages available.";
            }

            currentPageIndex = -1;
            return;
        }

        var clampedStartingIndex = Mathf.Clamp(startingPageIndex, 0, tabButtons.Count - 1);
        if (tabButtons[clampedStartingIndex] != null && tabButtons[clampedStartingIndex].interactable)
        {
            ShowPage(clampedStartingIndex);
            return;
        }

        for (var index = 0; index < tabButtons.Count; index++)
        {
            var tabButton = tabButtons[index];
            if (tabButton != null && tabButton.interactable)
            {
                ShowPage(index);
                return;
            }
        }

        if (pageText != null)
        {
            pageText.text = "No pages unlocked.";
        }

        currentPageIndex = -1;
    }

    private static string GetHardcodedPageContent(int pageIndex)
    {
        return pageIndex switch
        {
            0 => "Section 1 - Station Rules\n\nArrive prepared, review each manifest, and verify all labels before making a decision.",
            1 => "Section 2 - Cargo Verification\n\nMatch declared cargo to what you see. Flag mismatches and suspicious packaging immediately.",
            2 => "Section 3 - Passenger Screening\n\nCheck identification carefully and confirm every passenger is on the approved list.",
            3 => "Section 4 - Hazard Protocols\n\nIf hazardous materials are present, confirm permits, containment, and routing are valid.",
            4 => "Section 5 - Escalation Steps\n\nWhen uncertain, hold the train and escalate to a supervisor for final review.",
            5 => "Section 6 - Inspection Checklist\n\nChecklist page placeholder. Interactable checklist will be added later.",
            _ => "Section not found."
        };
    }

    private bool IsValidPageIndex(int pageIndex)
    {
        return pageIndex >= 0 && pageIndex < tabButtons.Count;
    }
}
