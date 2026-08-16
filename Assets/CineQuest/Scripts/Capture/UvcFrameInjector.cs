// Cine Quest — Drop this on a GameObject next to your UVC4Unity preview component.
// NotifyTexture is safe from a USB/Java thread: it queues; Tick applies on the main thread.

using UnityEngine;

namespace CineQuest.Capture
{
    public sealed class UvcFrameInjector : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;

        void Start()
        {
            if (captureService == null)
                captureService = CaptureService.Instance;
        }

        /// <summary>Queue a decoded Unity Texture. Does not SetBackend (would tear down mid-frame).</summary>
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
                return;
            }

            Debug.LogWarning("[CineQuest] NotifyTexture ignored — UVC backend not active. Do not SetBackend from the notify path.");
        }
    }
}
