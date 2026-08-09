// Cine Quest — Pure scope update-rate policy (no GPU). Used by ScopeManager quality modes.

namespace CineQuest.Core
{
    public enum ScopeQuality
    {
        High = 0,
        Balanced = 1,
        Performance = 2
    }

    public static class ScopeQualityPolicy
    {
        /// <summary>
        /// Whether scopes should run analysis this frame given a monotonic frame counter
        /// and seconds since last update.
        /// </summary>
        public static bool ShouldUpdate(ScopeQuality mode, int frameCounter, float secondsSinceLastUpdate)
        {
            switch (mode)
            {
                case ScopeQuality.High:
                    return true;
                case ScopeQuality.Balanced:
                    return (frameCounter % 2) == 0;
                case ScopeQuality.Performance:
                    return secondsSinceLastUpdate >= (1f / 20f);
                default:
                    return true;
            }
        }

        /// <summary>Target analysis width for downsampling (height follows source aspect).</summary>
        public static int AnalysisWidth(ScopeQuality mode, int sourceWidth)
        {
            switch (mode)
            {
                case ScopeQuality.High:
                    return sourceWidth < 960 ? sourceWidth : 960;
                case ScopeQuality.Balanced:
                    return 640;
                default:
                    return 480;
            }
        }
    }
}
