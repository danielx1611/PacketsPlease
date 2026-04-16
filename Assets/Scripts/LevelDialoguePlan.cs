using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDialoguePlan", menuName = "Dialogue/Level Dialogue Plan")]
public class LevelDialoguePlan : ScriptableObject
{
    [Serializable]
    public class DialogueBlock
    {
        [Tooltip("Unique id for the block within this plan.")]
        public string blockId;

        public DialogueSequence sequence;

        [Min(0)]
        public int startingLineIndex;

        [Tooltip("Trigger id required to start this block. Leave empty for opening or manually started blocks.")]
        public string triggerId;

        [Tooltip("If enabled, the block will auto-start when its trigger is resumed. For empty trigger ids, this starts at level begin.")]
        public bool autoStart = true;

        [Tooltip("Optional trigger id emitted when this block finishes.")]
        public string completionTriggerId;
    }

    [SerializeField] private List<DialogueBlock> dialogueBlocks = new();

    public IReadOnlyList<DialogueBlock> DialogueBlocks => dialogueBlocks;

    public DialogueBlock GetBlock(string blockId)
    {
        if (string.IsNullOrWhiteSpace(blockId))
        {
            return null;
        }

        for (var i = 0; i < dialogueBlocks.Count; i++)
        {
            var block = dialogueBlocks[i];
            if (block != null && string.Equals(block.blockId, blockId, StringComparison.Ordinal))
            {
                return block;
            }
        }

        return null;
    }
}
