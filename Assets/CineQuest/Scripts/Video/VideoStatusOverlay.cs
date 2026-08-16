// Cine Quest — Full-panel messaging for capture errors (no device, HDCP, USB, etc.).

using CineQuest.Capture;
using UnityEngine;
using UnityEngine.UI;

namespace CineQuest.Video
{
    /// <summary>
    /// World-space text overlay in front of the video panel.
    /// Hidden while streaming cleanly; shows high-contrast errors otherwise.
    /// </summary>
    public sealed class VideoStatusOverlay : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;
        [SerializeField] Text messageText;
        [SerializeField] Image background;
        [SerializeField] CanvasGroup canvasGroup;

        void Start()
        {
            if (captureService == null)
                captureService = CaptureService.Instance;
        }

        void LateUpdate()
        {
            if (captureService == null)
            {
                captureService = CaptureService.Instance;
                if (captureService == null) return;
            }

            var st = captureService.Status;
            string msg = null;
            Color bg = new Color(0.05f, 0.05f, 0.07f, 0.88f);

            bool waiting = captureService != null && captureService.WaitingForFirstFrame;
            if (waiting)
            {
                msg = "WAITING FOR LIVE VIDEO\nClose HDMI Link · Allow USB for Cine Quest · same capture card";
                bg = new Color(0.05f, 0.07f, 0.1f, 0.92f);
            }
            else if (!st.IsStreaming || st.Error != CaptureErrorCode.None)
            {
                switch (st.Error)
                {
                    case CaptureErrorCode.NoDevice:
                        msg = "NO CAPTURE DEVICE\nUse the same USB HDMI capture card as Meta HDMI Link.\nClose HDMI Link first, then Allow USB for Cine Quest.";
                        break;
                    case CaptureErrorCode.PermissionDenied:
                        msg = "USB PERMISSION DENIED\nUnplug and replug the capture card, then Accept";
                        break;
                    case CaptureErrorCode.UsbSpeedWarning:
                        // Still streaming — soft warning only; keep video visible
                        msg = null;
                        break;
                    case CaptureErrorCode.HdcpBlanked:
                        // Not assigned by capture; reserved. Do not claim HDCP detection.
                        msg = null;
                        break;
                    case CaptureErrorCode.UnsupportedResolution:
                        msg = "UNSUPPORTED RESOLUTION\nPrefer 1080p60 from the camera/monitor output";
                        break;
                    case CaptureErrorCode.SignalLost:
                        msg = "SIGNAL LOST\nCheck HDMI/DP cable, capture card power, and source output";
                        break;
                    case CaptureErrorCode.BackendUnavailable:
                        msg = st.ErrorMessage ?? "CAPTURE BACKEND UNAVAILABLE\nImport UVC4UnityAndroid — see Docs/UVC_INTEGRATION.md";
                        break;
                    case CaptureErrorCode.InternalError:
                        msg = st.ErrorMessage ?? "CAPTURE ERROR";
                        break;
                    default:
                        if (!st.IsStreaming)
                            msg = "WAITING FOR VIDEO…";
                        break;
                }
            }

            bool show = !string.IsNullOrEmpty(msg);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            else if (messageText != null)
            {
                messageText.gameObject.SetActive(show);
            }

            if (show && messageText != null)
                messageText.text = msg;
            if (show && background != null)
                background.color = bg;
        }

        public void Bind(CaptureService capture, Text text, Image bg, CanvasGroup group)
        {
            if (capture != null) captureService = capture;
            if (text != null) messageText = text;
            if (bg != null) background = bg;
            if (group != null) canvasGroup = group;
        }
    }
}
