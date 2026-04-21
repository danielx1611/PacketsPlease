using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChecklistPanelUI : MonoBehaviour
{
    [Serializable]
    public class ChecklistEntry
    {
        public string categoryId;
        public string categoryLabel;
        public Button toggleButton;
        public TMP_Text stateText;

        [NonSerialized] public bool isChecked;
    }

    [SerializeField] private List<ChecklistEntry> entries = new();

    private readonly Dictionary<string, ChecklistEntry> entriesById = new();
    private readonly Dictionary<ChecklistEntry, UnityAction> clickHandlersByEntry = new();

    private void Awake()
    {
        RebuildLookup();
        RefreshAllEntryStates();
    }

    private void OnEnable()
    {
        foreach (var entry in entries)
        {
            if (entry?.toggleButton == null || clickHandlersByEntry.ContainsKey(entry))
            {
                continue;
            }

            UnityAction clickHandler = () => ToggleEntry(entry);
            clickHandlersByEntry.Add(entry, clickHandler);
            entry.toggleButton.onClick.AddListener(clickHandler);
        }
    }

    private void OnDisable()
    {
        foreach (var pair in clickHandlersByEntry)
        {
            var entry = pair.Key;
            var clickHandler = pair.Value;
            if (entry?.toggleButton == null)
            {
                continue;
            }

            entry.toggleButton.onClick.RemoveListener(clickHandler);
        }

        clickHandlersByEntry.Clear();
    }

    public void SetCategoryChecked(string categoryId, bool isChecked)
    {
        if (!entriesById.TryGetValue(categoryId, out var entry))
        {
            return;
        }

        entry.isChecked = isChecked;
        RefreshEntryState(entry);
    }

    public void SetCategoryInteractable(string categoryId, bool isInteractable)
    {
        if (!entriesById.TryGetValue(categoryId, out var entry) || entry.toggleButton == null)
        {
            return;
        }

        entry.toggleButton.interactable = isInteractable;
        RefreshEntryState(entry);
    }

    private void ToggleEntry(ChecklistEntry entry)
    {
        if (entry == null || entry.toggleButton == null || !entry.toggleButton.interactable)
        {
            return;
        }

        entry.isChecked = !entry.isChecked;
        RefreshEntryState(entry);
    }

    private void RefreshAllEntryStates()
    {
        foreach (var entry in entries)
        {
            RefreshEntryState(entry);
        }
    }

    private void RefreshEntryState(ChecklistEntry entry)
    {
        if (entry == null || entry.stateText == null)
        {
            return;
        }

        var statusPrefix = entry.isChecked ? "Selected" : "Not selected";
        if (entry.toggleButton != null && !entry.toggleButton.interactable)
        {
            statusPrefix = $"{statusPrefix} (Disabled)";
        }

        var label = string.IsNullOrWhiteSpace(entry.categoryLabel) ? entry.categoryId : entry.categoryLabel;
        entry.stateText.text = $"{label}: {statusPrefix}";
    }

    private void RebuildLookup()
    {
        entriesById.Clear();
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.categoryId))
            {
                continue;
            }

            if (!entriesById.ContainsKey(entry.categoryId))
            {
                entriesById.Add(entry.categoryId, entry);
            }
        }
    }
}
