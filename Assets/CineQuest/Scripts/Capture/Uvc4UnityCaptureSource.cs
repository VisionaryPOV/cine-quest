// Cine Quest — Adapter for UVC4UnityAndroid (saki4510t).
// Compiles without the plugin via CINE_QUEST_UVC4UNITY define.
// Import the plugin, then add scripting define CINE_QUEST_UVC4UNITY.
// See Docs/UVC_INTEGRATION.md.

using System;
using System.Reflection;
using UnityEngine;

namespace CineQuest.Capture
{
    /// <summary>
    /// Production capture backend wrapping UVC4UnityAndroid when present.
    /// Uses reflection so the project compiles before the third-party package is imported.
    /// Prefer YUY2/NV12/MJPEG over H.264 when the plugin API allows format selection.
    /// </summary>
    public sealed class Uvc4UnityCaptureSource : IVideoCaptureSource, IAudioCaptureSource
    {
        readonly CaptureEvents _events = new CaptureEvents();
        int _prefW = 1920;
        int _prefH = 1080;
        float _prefFps = 60f;
        bool _running;
        Texture _frame;
        CaptureStatus _status = CaptureStatus.Empty;
        float _fpsWindow;
        int _fpsCount;
        float _measuredFps;

        // Reflection handles into UVC4UnityAndroid (optional)
        object _uvcManager;
        Type _uvcManagerType;
        MethodInfo _miGetTexture;
        MethodInfo _miStart;
        MethodInfo _miStop;
        bool _pluginPresent;
        bool _audioRunning;
        AudioClip _audioClip;

        public bool IsRunning => _running;
        public Texture CurrentFrame => _frame;
        public CaptureStatus Status => _status;
        public CaptureEvents Events => _events;
        public bool HasAudio => _audioClip != null;
        public AudioClip Clip => _audioClip;
        bool IAudioCaptureSource.IsRunning => _audioRunning;

        public Uvc4UnityCaptureSource()
        {
            TryBindPlugin();
        }

        public void Configure(int preferredWidth, int preferredHeight, float preferredFps)
        {
            _prefW = preferredWidth > 0 ? preferredWidth : 1920;
            _prefH = preferredHeight > 0 ? preferredHeight : 1080;
            _prefFps = preferredFps > 0 ? preferredFps : 60f;
        }

        public void StartCapture()
        {
            if (!_pluginPresent)
            {
                SetError(CaptureErrorCode.BackendUnavailable,
                    "UVC4UnityAndroid not found. Import the plugin and define CINE_QUEST_UVC4UNITY. See Docs/UVC_INTEGRATION.md");
                return;
            }

            try
            {
                // Plugin-specific start is highly version-dependent; prefer explicit binding.
                _miStart?.Invoke(_uvcManager, null);
                _running = true;
                _status = new CaptureStatus
                {
                    IsStreaming = true,
                    Width = _prefW,
                    Height = _prefH,
                    MeasuredFps = 0f,
                    EstimatedLatencyMs = 33f,
                    UsbSpeed = UsbLinkSpeed.Unknown,
                    ColorFormat = CaptureColorFormat.Unknown,
                    HasAudio = false,
                    Error = CaptureErrorCode.None,
                    DeviceName = "UVC4Unity"
                };
                _events.RaiseStatus(_status);
            }
            catch (Exception ex)
            {
                SetError(CaptureErrorCode.InternalError, ex.Message);
            }
        }

        public void StopCapture()
        {
            try { _miStop?.Invoke(_uvcManager, null); } catch { /* ignore */ }
            _running = false;
            _frame = null;
            _status.IsStreaming = false;
            _events.RaiseStatus(_status);
        }

        public void Tick()
        {
            if (!_running) return;

            try
            {
                if (_miGetTexture != null)
                {
                    var tex = _miGetTexture.Invoke(_uvcManager, null) as Texture;
                    if (tex != null && tex != _frame)
                    {
                        _frame = tex;
                        _events.RaiseFrame(_frame);
                    }
                    else if (tex != null)
                    {
                        _frame = tex;
                    }
                }

                // Also try common WebCamTexture fallback path if plugin exposes none.
                if (_frame == null)
                {
                    // Leave frame null; CaptureService may surface NoDevice/SignalLost.
                }

                _fpsCount++;
                _fpsWindow += Time.unscaledDeltaTime;
                if (_fpsWindow >= 0.5f)
                {
                    _measuredFps = _fpsCount / _fpsWindow;
                    _fpsCount = 0;
                    _fpsWindow = 0f;
                }

                if (_frame != null)
                {
                    _status.IsStreaming = true;
                    _status.Width = _frame.width;
                    _status.Height = _frame.height;
                    _status.MeasuredFps = _measuredFps;
                    _status.EstimatedLatencyMs = EstimateLatencyMs(_status.Width, _status.Height, _status.UsbSpeed);
                    _status.Error = CaptureErrorCode.None;
                    _status.ErrorMessage = null;

                    // USB2 bandwidth heuristic for 1080p60 uncompressed-ish streams
                    if (_status.Width >= 1920 && _status.Height >= 1080 && _measuredFps < 45f)
                    {
                        _status.UsbSpeed = UsbLinkSpeed.HiSpeed;
                        _status.Error = CaptureErrorCode.UsbSpeedWarning;
                        _status.ErrorMessage = "Frame rate low — use USB 3 SuperSpeed capture card & cable";
                    }
                    else if (_status.UsbSpeed == UsbLinkSpeed.Unknown && _measuredFps >= 55f)
                    {
                        _status.UsbSpeed = UsbLinkSpeed.SuperSpeed;
                    }

                    _events.RaiseStatus(_status);
                }
            }
            catch (Exception ex)
            {
                SetError(CaptureErrorCode.InternalError, ex.Message);
            }
        }

        public void StartAudio()
        {
            // UVC4UnityAndroid UAC is experimental; wire when plugin present.
            _audioRunning = _audioClip != null;
        }

        public void StopAudio()
        {
            _audioRunning = false;
        }

        public void Dispose()
        {
            StopAudio();
            StopCapture();
            _uvcManager = null;
        }

        /// <summary>
        /// Allow CaptureService / integrators to inject the live texture when using
        /// a custom UVC MonoBehaviour that already produces a Texture each frame.
        /// </summary>
        public void InjectFrame(Texture texture, CaptureStatus status)
        {
            _frame = texture;
            _running = texture != null;
            _status = status;
            if (texture != null) _events.RaiseFrame(texture);
            _events.RaiseStatus(_status);
        }

        void TryBindPlugin()
        {
#if CINE_QUEST_UVC4UNITY
            // When define is set, look for common manager type names used by UVC4UnityAndroid.
            _uvcManagerType =
                Type.GetType("Serenegiant.UVC.UVCManager, UVC4UnityAndroidPlugin") ??
                Type.GetType("UVC.UVCManager") ??
                FindType("UVCManager");

            if (_uvcManagerType == null)
            {
                _pluginPresent = false;
                return;
            }

            var existing = UnityEngine.Object.FindObjectsByType(_uvcManagerType, FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0)
                _uvcManager = existing[0];

            _miGetTexture = _uvcManagerType.GetMethod("GetTexture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            ?? _uvcManagerType.GetMethod("get_PreviewTexture", BindingFlags.Instance | BindingFlags.Public);
            _miStart = _uvcManagerType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public)
                       ?? _uvcManagerType.GetMethod("StartPreview", BindingFlags.Instance | BindingFlags.Public);
            _miStop = _uvcManagerType.GetMethod("Close", BindingFlags.Instance | BindingFlags.Public)
                      ?? _uvcManagerType.GetMethod("StopPreview", BindingFlags.Instance | BindingFlags.Public);

            _pluginPresent = _uvcManager != null || _uvcManagerType != null;
#else
            // Soft probe without define — still works if types exist on classpath after import.
            _uvcManagerType = FindType("UVCManager");
            _pluginPresent = _uvcManagerType != null;
            if (_pluginPresent)
            {
                Debug.Log("[CineQuest] UVCManager type found. Add scripting define CINE_QUEST_UVC4UNITY for full binding.");
            }
#endif
        }

        static Type FindType(string simpleName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.Name == simpleName) return t;
                    }
                }
                catch
                {
                    // Dynamic assemblies may throw
                }
            }
            return null;
        }

        void SetError(CaptureErrorCode code, string message)
        {
            _status.Error = code;
            _status.ErrorMessage = message;
            _status.IsStreaming = false;
            _events.RaiseError(code, message);
            _events.RaiseStatus(_status);
            Debug.LogWarning($"[CineQuest] Capture: {code} — {message}");
        }

        static float EstimateLatencyMs(int w, int h, UsbLinkSpeed speed)
        {
            // Engineering estimate only — not a hardware measurement.
            float baseMs = 16f; // one display frame-ish
            if (speed == UsbLinkSpeed.HiSpeed) baseMs += 20f;
            if (w * h > 1920 * 1080) baseMs += 8f;
            return baseMs;
        }
    }
}
