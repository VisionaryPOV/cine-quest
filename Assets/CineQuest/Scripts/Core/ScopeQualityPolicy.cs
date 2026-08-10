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

        /// <summary>
        /// Target analysis width for downsampling. Never larger than sourceWidth (no upscale).
        /// Height should follow source aspect in the caller.
        /// </summary>
        public static int AnalysisWidth(ScopeQuality mode, int sourceWidth)
        {
            if (sourceWidth < 1) sourceWidth = 1;
            int target;
            switch (mode)
            {
                case ScopeQuality.High:
                    target = 960;
                    break;
                case ScopeQuality.Balanced:
                    target = 640;
                    break;
                default:
                    target = 480;
                    break;
            }
            return sourceWidth < target ? sourceWidth : target;
        }
    }
}
