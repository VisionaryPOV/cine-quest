// Cine Quest — Pure capture start/timeout/reconnect rules (unit-tested).

namespace CineQuest.Core
{
    public static class CaptureLifecyclePolicy
    {
        public const float FirstFrameTimeoutSeconds = 3f;

        /// <summary>After timeout, keep polling for frames (do not disable Tick).</summary>
        public static bool ShouldKeepPollingAfterTimeout => true;

        /// <summary>
        /// Silent synthetic fallback hides a missing live path on device.
        /// Editor may fallback; device should stay on UVC and show No Device.
        /// </summary>
        public static bool AllowSilentSyntheticFallback(bool isEditor)
        {
            return isEditor;
        }

        /// <summary>Watchdog may reconnect when we expected a stream but have none.</summary>
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
    }
}
