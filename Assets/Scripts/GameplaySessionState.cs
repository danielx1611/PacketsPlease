public static class GameplaySessionState
{
    public static int RequestedStartLevelIndex { get; private set; } = -1;
    public static bool SkipFirstLevelTutorialOnLoad { get; private set; }
    public static int LastKnownLevelIndex { get; private set; } = -1;

    public static void SetLastKnownLevelIndex(int levelIndex)
    {
        LastKnownLevelIndex = levelIndex;
    }

    public static void RequestStartLevel(int levelIndex, bool skipFirstLevelTutorial)
    {
        RequestedStartLevelIndex = levelIndex;
        SkipFirstLevelTutorialOnLoad = skipFirstLevelTutorial;
    }

    public static bool ConsumeRequestedStartLevel(out int levelIndex, out bool skipFirstLevelTutorial)
    {
        levelIndex = RequestedStartLevelIndex;
        skipFirstLevelTutorial = SkipFirstLevelTutorialOnLoad;

        var hasRequest = levelIndex >= 0;
        RequestedStartLevelIndex = -1;
        SkipFirstLevelTutorialOnLoad = false;
        return hasRequest;
    }

    public static void ResetSessionStartOverrides()
    {
        RequestedStartLevelIndex = -1;
        SkipFirstLevelTutorialOnLoad = false;
    }
}
