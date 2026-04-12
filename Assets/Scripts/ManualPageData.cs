using UnityEngine;

[CreateAssetMenu(fileName = "ManualPageData", menuName = "Gameplay/Manual Page Data")]
public class ManualPageData : ScriptableObject
{
    [Min(0)]
    [SerializeField] private int pageIndex;

    [SerializeField] private string pageTitle;

    [TextArea(4, 10)]
    [SerializeField] private string pageBody;

    [Tooltip("Optional label used by designers for filtering/grouping manual pages by difficulty.")]
    [SerializeField] private string difficultyTag;

    [Tooltip("Optional unlock threshold for game systems that unlock manual pages progressively.")]
    [Min(0)]
    [SerializeField] private int unlockThreshold;

    public int PageIndex => pageIndex;
    public string DifficultyTag => difficultyTag;
    public int UnlockThreshold => unlockThreshold;

    public string GetFormattedContent()
    {
        if (string.IsNullOrWhiteSpace(pageTitle))
        {
            return pageBody;
        }

        return $"{pageTitle}\n\n{pageBody}";
    }
}
