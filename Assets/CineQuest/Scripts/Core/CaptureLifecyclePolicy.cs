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

        /// <summary>Measured fps is a frame counter, not a Unity tick counter.</summary>
        public static bool ShouldCountFpsSample(bool frameGenerationAdvancedThisTick)
            => frameGenerationAdvancedThisTick;

        /// <summary>Tick may clear SignalLost only after a real new frame.</summary>
        public static bool ShouldClearSignalLost(bool frameGenerationAdvancedThisTick)
            => frameGenerationAdvancedThisTick;

        /// <summary>
        /// Published HUD status cannot be healthier than the watchdog decision
        /// in the same tick. CaptureStatus is a struct — do not mutate a copy.
        /// </summary>
        public static bool PublishedAsSignalLost(bool watchdogSignalLost, bool generationAdvancedThisTick)
            => watchdogSignalLost && !generationAdvancedThisTick;

        public static bool PublishedIsStreaming(
            bool sourceIsStreaming,
            bool watchdogSignalLost,
            bool generationAdvancedThisTick)
        {
            if (watchdogSignalLost && !generationAdvancedThisTick)
                return false;
            return sourceIsStreaming;
        }

        /// <summary>
        /// DisplayFrame / freeze RT are 2D RGB. Only a raw Android CurrentFrame
        /// (typically External OES) should enable CQ_EXTERNAL_OES.
        /// </summary>
        public static bool ShouldSampleAsExternalOes(bool androidDevice, bool boundTextureIs2dRgb)
        {
            if (boundTextureIs2dRgb) return false;
            return androidDevice;
        }

        /// <summary>
        /// OES blit only for a non-2D/external source that has not already failed.
        /// RenderTexture / readable Texture2D → 2D blit. Do not use luma-max≈0
        /// (dark sets are valid).
        /// </summary>
        public static bool ShouldBlitWithExternalOes(
            bool androidDevice,
            bool oesAlreadyFailed,
            bool sourceIsRenderTexture,
            bool sourceIsReadableTexture2D)
        {
            if (!androidDevice || oesAlreadyFailed) return false;
            if (sourceIsRenderTexture || sourceIsReadableTexture2D) return false;
            return true;
        }
    }
}
