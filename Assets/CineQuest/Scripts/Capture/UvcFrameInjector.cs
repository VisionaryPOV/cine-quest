// Cine Quest — Drop this on a GameObject next to your UVC4Unity preview component.
// Call NotifyTexture each frame (or on texture change) for a known-good path that bypasses reflection guesses.

using UnityEngine;

namespace CineQuest.Capture
{
    /// <summary>
    /// Explicit handoff from a third-party UVC MonoBehaviour into CaptureService.
    /// Prefer this over reflection when your plugin API differs from Uvc4UnityCaptureSource guesses.
    /// </summary>
    public sealed class UvcFrameInjector : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;
        [SerializeField] bool setOesOnLockedVideo;
        [SerializeField] Video.LockedVideoRenderer lockedVideo;

        void Start()
        {
            if (captureService == null)
                captureService = CaptureService.Instance;
        }

        /// <summary>Push a decoded Unity Texture into the active UVC adapter if present.</summary>
        public void NotifyTexture(Texture texture, int width = 0, int height = 0, float fps = 0f)
        {
            if (texture == null) return;

            if (captureService == null)
                captureService = CaptureService.Instance;

            var status = new CaptureStatus
            {
                IsStreaming = true,
                Width = width > 0 ? width : texture.width,
                Height = height > 0 ? height : texture.height,
                MeasuredFps = fps,
                EstimatedLatencyMs = 16f,
                UsbSpeed = UsbLinkSpeed.Unknown,
                ColorFormat = CaptureColorFormat.Unknown,
                HasAudio = false,
                Error = CaptureErrorCode.None,
                DeviceName = "UVC-Injected"
            };

            if (captureService?.Source is Uvc4UnityCaptureSource uvc)
            {
                uvc.InjectFrame(texture, status);
            }
            else
            {
                // Force restart with UVC backend so InjectFrame has a home on next frame
                captureService?.SetBackend(CaptureBackendKind.Uvc4Unity);
                if (captureService?.Source is Uvc4UnityCaptureSource uvc2)
                    uvc2.InjectFrame(texture, status);
            }

            if (setOesOnLockedVideo)
            {
                if (lockedVideo == null)
                    lockedVideo = FindFirstObjectByType<Video.LockedVideoRenderer>();
                lockedVideo?.SetUseExternalOes(true);
            }
        }
    }
}
