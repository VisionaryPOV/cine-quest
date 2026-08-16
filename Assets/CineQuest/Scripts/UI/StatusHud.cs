// Cine Quest — Minimal live status HUD for on-set monitoring.

using CineQuest.Capture;
using CineQuest.Core;
using CineQuest.Video;
using UnityEngine;
using UnityEngine.UI;

namespace CineQuest.UI
{
    public sealed class StatusHud : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;
        [SerializeField] FreezeFrameController freezeFrame;
        [SerializeField] ImageParameterController imageParams;
        [SerializeField] Text resolutionText;
        [SerializeField] Text fpsText;
        [SerializeField] Text usbText;
        [SerializeField] Text latencyText;
        [SerializeField] Text formatText;
        [SerializeField] Text batteryText;
        [SerializeField] Text warningText;
        [SerializeField] Text lockStateText;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] bool followHead = true;
        [SerializeField] float followDistance = 0.7f;
        [SerializeField] Vector3 followOffset = new Vector3(0f, -0.32f, 0f);

        Transform _head;
        float _batteryTimer;

        public void BindTexts(Text resolution, Text fps, Text usb, Text latency, Text format, Text battery, Text warning, Text lockState)
        {
            if (resolution != null) resolutionText = resolution;
            if (fps != null) fpsText = fps;
            if (usb != null) usbText = usb;
            if (latency != null) latencyText = latency;
            if (format != null) formatText = format;
            if (battery != null) batteryText = battery;
            if (warning != null) warningText = warning;
            if (lockState != null) lockStateText = lockState;
        }

        public void BindFreeze(FreezeFrameController freeze) => freezeFrame = freeze;
        public void BindImageParams(ImageParameterController img) => imageParams = img;
        public void BindCanvasGroup(CanvasGroup group) => canvasGroup = group;

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null) canvasGroup.alpha = visible ? 1f : 0f;
            else gameObject.SetActive(visible);
        }

        void Start()
        {
            if (captureService == null) captureService = CaptureService.Instance;
            if (freezeFrame == null) freezeFrame = FindFirstObjectByType<FreezeFrameController>();
            if (imageParams == null) imageParams = FindFirstObjectByType<ImageParameterController>();
            var cam = Camera.main;
            if (cam != null) _head = cam.transform;
        }

        void LateUpdate()
        {
            if (followHead && _head != null)
            {
                var target = _head.position + _head.forward * followDistance + _head.TransformVector(followOffset);
                transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 8f);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(transform.position - _head.position, Vector3.up),
                    Time.deltaTime * 8f);
            }

            var st = captureService != null ? captureService.Status : CaptureStatus.Empty;
            Set(resolutionText, st.ResolutionLabel);
            Set(fpsText, st.MeasuredFps > 0 ? $"{st.MeasuredFps:0.0} fps" : "— fps");
            Set(usbText, st.UsbSpeedLabel);
            Set(latencyText, st.EstimatedLatencyMs > 0 ? $"est. {st.EstimatedLatencyMs:0} ms" : "est. —");
            Set(formatText, st.ColorFormat.ToString());

            _batteryTimer -= Time.unscaledDeltaTime;
            if (_batteryTimer <= 0f)
            {
                _batteryTimer = 2f;
                float level = SystemInfo.batteryLevel;
                string bat = level < 0 ? "Bat —" : $"Bat {Mathf.RoundToInt(level * 100f)}%";
                if (SystemInfo.batteryStatus == BatteryStatus.Charging) bat += " CHG";
                Set(batteryText, bat);
            }

            if (warningText != null)
            {
                string warn = BuildWarning(st);
                warningText.gameObject.SetActive(!string.IsNullOrEmpty(warn));
                if (!string.IsNullOrEmpty(warn)) warningText.text = warn;
            }

            bool frozen = freezeFrame != null && freezeFrame.IsFrozen;
            bool locked = imageParams != null && imageParams.IsLocked;
            bool bypass = imageParams != null && imageParams.IsBypass;
            SetLockLabel(locked, bypass, frozen);
        }

        string BuildWarning(CaptureStatus st)
        {
            const string hints = "  A=Bypass  B=Lock  L-Y=Menu";
            if (CaptureLifecyclePolicy.IsSyntheticDeviceName(st.DeviceName))
                return "SYNTHETIC — NOT CAMERA" + hints;
            if (captureService != null && captureService.WaitingForFirstFrame)
                return "WAITING FOR LIVE VIDEO — close HDMI Link, allow USB" + hints;

            switch (st.Error)
            {
                case CaptureErrorCode.NoDevice:
                    return "NO DEVICE — close HDMI Link, allow USB, same capture card" + hints;
                case CaptureErrorCode.UsbSpeedWarning:
                    return "INFERRED USB2 (fps dropped) — check SuperSpeed cable" + hints;
                case CaptureErrorCode.HdcpBlanked:
                    return null; // never claimed; enum reserved
                case CaptureErrorCode.PermissionDenied:
                    return "USB PERMISSION DENIED";
                case CaptureErrorCode.SignalLost:
                    return "SIGNAL LOST";
                case CaptureErrorCode.UnsupportedResolution:
                    return "UNSUPPORTED RESOLUTION — try 1080p60";
                case CaptureErrorCode.BackendUnavailable:
                    return st.ErrorMessage ?? "UVC PLUGIN NOT IN THIS APK";
                default:
                    return null;
            }
        }

        public void SetLockLabel(bool locked, bool bypass)
        {
            SetLockLabel(locked, bypass, freezeFrame != null && freezeFrame.IsFrozen);
        }

        public void SetLockLabel(bool locked, bool bypass, bool frozen)
        {
            if (lockStateText == null) return;
            if (frozen)
            {
                lockStateText.text = "FROZEN";
                lockStateText.color = new Color(1f, 0.55f, 0.2f);
                return;
            }
            if (bypass)
            {
                lockStateText.text = "REF BYPASS";
                lockStateText.color = new Color(0.45f, 0.9f, 1f);
                return;
            }
            lockStateText.text = locked ? "LOCKED" : "UNLOCKED";
            // LOCKED is the safe/intended on-set state — amber, not alarm red
            lockStateText.color = locked
                ? new Color(1f, 0.82f, 0.35f)
                : new Color(0.55f, 0.6f, 0.62f);
        }

        static void Set(Text t, string v)
        {
            if (t != null) t.text = v;
        }
    }
}
