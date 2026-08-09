// Cine Quest — Minimal live status HUD for on-set monitoring.

using CineQuest.Capture;
using UnityEngine;
using UnityEngine.UI;

namespace CineQuest.UI
{
    public sealed class StatusHud : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;
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
        [SerializeField] float followDistance = 0.85f;
        [SerializeField] Vector3 followOffset = new Vector3(0f, -0.25f, 0f);

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

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null) canvasGroup.alpha = visible ? 1f : 0f;
            else gameObject.SetActive(visible);
        }

        void Start()
        {
            if (captureService == null) captureService = CaptureService.Instance;
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
            Set(latencyText, $"~{st.EstimatedLatencyMs:0} ms");
            Set(formatText, st.ColorFormat.ToString());

            _batteryTimer -= Time.unscaledDeltaTime;
            if (_batteryTimer <= 0f)
            {
                _batteryTimer = 2f;
                float level = SystemInfo.batteryLevel;
                string bat = level < 0 ? "Bat —" : $"Bat {Mathf.RoundToInt(level * 100f)}%";
                if (SystemInfo.batteryStatus == BatteryStatus.Charging) bat += " ⚡";
                Set(batteryText, bat);
            }

            if (warningText != null)
            {
                string warn = null;
                switch (st.Error)
                {
                    case CaptureErrorCode.NoDevice:
                        warn = "NO DEVICE — connect UVC capture card";
                        break;
                    case CaptureErrorCode.UsbSpeedWarning:
                        warn = "USB HI-SPEED — prefer SuperSpeed USB 3 card/cable";
                        break;
                    case CaptureErrorCode.HdcpBlanked:
                        warn = "HDCP — source is encrypting; signal may be blank";
                        break;
                    case CaptureErrorCode.PermissionDenied:
                        warn = "USB PERMISSION DENIED";
                        break;
                    case CaptureErrorCode.SignalLost:
                        warn = "SIGNAL LOST";
                        break;
                    case CaptureErrorCode.UnsupportedResolution:
                        warn = "UNSUPPORTED RESOLUTION";
                        break;
                    case CaptureErrorCode.BackendUnavailable:
                        warn = st.ErrorMessage ?? "CAPTURE BACKEND UNAVAILABLE";
                        break;
                }
                warningText.gameObject.SetActive(!string.IsNullOrEmpty(warn));
                if (!string.IsNullOrEmpty(warn)) warningText.text = warn;
            }
        }

        public void SetLockLabel(bool locked, bool bypass)
        {
            if (lockStateText == null) return;
            if (bypass) lockStateText.text = "REF BYPASS";
            else lockStateText.text = locked ? "LOCKED" : "UNLOCKED";
            lockStateText.color = bypass ? new Color(0.4f, 0.9f, 1f) :
                locked ? new Color(1f, 0.35f, 0.35f) : new Color(0.6f, 1f, 0.6f);
        }

        static void Set(Text t, string v)
        {
            if (t != null) t.text = v;
        }
    }
}
