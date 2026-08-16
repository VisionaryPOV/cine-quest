// Cine Quest — Pure capture start/timeout/reconnect rules (unit-tested).

namespace CineQuest.Core
{
    public static class CaptureLifecyclePolicy
    {
        public const float FirstFrameTimeoutSeconds = 3f;

        public static bool ShouldKeepPollingAfterTimeout => true;

        public static bool AllowSilentSyntheticFallback(bool isEditor) => isEditor;

        /// <summary>
        /// Advance "last live frame" only on a new raise (generation bump or new texture object).
        /// A leftover Texture reference is not a live feed.
        /// </summary>
        public static bool ShouldAdvanceLastFrameTime(bool textureObjectChanged, bool frameGenerationChanged)
        {
            return textureObjectChanged || frameGenerationChanged;
        }

        /// <summary>hadFrameOnce must be a real raise, not backend-create time.</summary>
        public static bool HadRealFrame(bool lastTextureAssigned, bool lastFrameTimeWasSetAtCreate)
        {
            if (lastFrameTimeWasSetAtCreate) return false;
            return lastTextureAssigned;
        }

        public static bool ShouldWatchdogReconnect(bool isStreaming, bool hadFrameOnce, float secondsSinceLastFrame, float lostSeconds)
        {
            if (!hadFrameOnce) return false;
            if (isStreaming && secondsSinceLastFrame < lostSeconds) return false;
            return secondsSinceLastFrame >= lostSeconds;
        }

        public static bool IsSyntheticDeviceName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) return false;
            return deviceName.StartsWith("Synthetic");
        }

        /// <summary>USB Hi-Speed warning only if fps collapsed from a previously high rate.</summary>
        public static bool ShouldWarnUsbSpeed(float previousFps, float currentFps, int width, int height)
        {
            if (width < 1920 || height < 1080) return false;
            if (currentFps <= 0f) return false;
            if (previousFps >= 50f && currentFps < 40f) return true;
            return false;
        }
    }
}
