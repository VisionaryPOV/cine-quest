// Cine Quest — Stub for facebookexperimental/usb-video native AAR bridge.
// The usb-video project is a pure Android Gradle app, not a Unity package.
// When you build an AAR + JNI plugin, implement StartCapture/Tick here
// and hand External OES / Texture2D frames to InjectFrame-equivalent paths.
// See Docs/UVC_INTEGRATION.md § Native usb-video bridge.

using UnityEngine;

namespace CineQuest.Capture
{
    /// <summary>
    /// Placeholder backend for Meta's experimental usb-video library.
    /// Always reports BackendUnavailable until a native plugin is supplied.
    /// </summary>
    public sealed class UsbVideoNativeCaptureSource : IVideoCaptureSource
    {
        readonly CaptureEvents _events = new CaptureEvents();
        CaptureStatus _status;

        public bool IsRunning => false;
        public Texture CurrentFrame => null;
        public int FrameGeneration => 0;
        public CaptureStatus Status => _status;
        public CaptureEvents Events => _events;

        public UsbVideoNativeCaptureSource()
        {
            _status = new CaptureStatus
            {
                IsStreaming = false,
                Error = CaptureErrorCode.BackendUnavailable,
                ErrorMessage = "usb-video native bridge not linked. See Docs/UVC_INTEGRATION.md",
                UsbSpeed = UsbLinkSpeed.Unknown
            };
        }

        public void Configure(int preferredWidth, int preferredHeight, float preferredFps) { }

        public void StartCapture()
        {
            _events.RaiseError(_status.Error, _status.ErrorMessage);
            _events.RaiseStatus(_status);
        }

        public void StopCapture() { }

        public void Tick() { }

        public void Dispose() { }

        /// <summary>
        /// Future: called from AndroidJavaProxy when native frames arrive.
        /// </summary>
        public void OnNativeFrame(int textureId, int width, int height, long timestampNs)
        {
            // Implement with Texture2D.CreateExternalTexture / OES sampler when AAR is ready.
            Debug.Log($"[CineQuest] usb-video frame stub {width}x{height} tex={textureId} t={timestampNs}");
        }
    }
}
