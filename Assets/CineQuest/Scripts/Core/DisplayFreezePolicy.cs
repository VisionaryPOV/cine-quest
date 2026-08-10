// Cine Quest — Pure freeze display ownership rules (unit-tested).
// Live capture must not rebind the panel texture while frozen.

namespace CineQuest.Core
{
    public static class DisplayFreezePolicy
    {
        /// <summary>
        /// Returns whether a live capture frame should update the monitor material.
        /// </summary>
        public static bool ShouldBindLiveFrame(bool isDisplayFrozen)
        {
            return !isDisplayFrozen;
        }

        /// <summary>
        /// Selects the texture that should be bound for display and analysis consumers.
        /// </summary>
        public static T SelectAnalysisTexture<T>(bool isFrozen, T freezeTexture, T liveTexture)
            where T : class
        {
            if (isFrozen && freezeTexture != null)
                return freezeTexture;
            return liveTexture;
        }
    }
}
