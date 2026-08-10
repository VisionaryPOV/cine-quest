// Cine Quest — Adapter for UVC4UnityAndroid (saki4510t).
// Fail-closed: IsRunning only after a real texture (or InjectFrame). See Docs/UVC_INTEGRATION.md.

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
        bool _startRequested;
        Texture _frame;
        CaptureStatus _status = CaptureStatus.Empty;
        float _fpsWindow;
        int _fpsCount;
        float _measuredFps;
        float _startTime;

        object _uvcManager;
        Type _uvcManagerType;
        MethodInfo _miGetTexture;
        MethodInfo _miStart;
        MethodInfo _miStop;
        bool _pluginPresent;
        bool _audioRunning;
        AudioClip _audioClip;

        /// <summary>True only when we have an open path that has produced (or injected) a frame.</summary>
        public bool IsRunning => _running && _frame != null;
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
            if (!_pluginPresent || _uvcManagerType == null)
            {
                SetError(CaptureErrorCode.BackendUnavailable,
                    "UVC4UnityAndroid not found. Import the plugin and define CINE_QUEST_UVC4UNITY. See Docs/UVC_INTEGRATION.md");
                return;
            }

            if (_uvcManager == null)
            {
                // Re-probe for a scene instance (may appear after bootstrap).
                TryBindPlugin();
            }

            if (_uvcManager == null)
            {
                SetError(CaptureErrorCode.BackendUnavailable,
                    "UVCManager type found but no instance in scene. Add UVCManager (UVC4UnityAndroid) to the scene.");
                return;
            }

            if (_miStart == null && _miGetTexture == null)
            {
                SetError(CaptureErrorCode.BackendUnavailable,
                    "UVCManager API methods not found. Use InjectFrame from your UVC component, or update adapter method names.");
                return;
            }

            try
            {
                _miStart?.Invoke(_uvcManager, null);
                _startRequested = true;
                _startTime = Time.unscaledTime;
                // Fail-closed: do NOT set _running until a texture arrives (Tick or InjectFrame).
                _running = false;
                _status = new CaptureStatus
                {
                    IsStreaming = false,
                    Width = _prefW,
                    Height = _prefH,
                    MeasuredFps = 0f,
                    EstimatedLatencyMs = 33f,
                    UsbSpeed = UsbLinkSpeed.Unknown,
                    ColorFormat = CaptureColorFormat.Unknown,
                    HasAudio = false,
                    Error = CaptureErrorCode.NoDevice,
                    ErrorMessage = "Waiting for UVC frames…",
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
            _startRequested = false;
            _running = false;
            _frame = null;
            _status.IsStreaming = false;
            _events.RaiseStatus(_status);
        }

        public void Tick()
        {
            if (!_startRequested && !_running) return;

            try
            {
                if (_miGetTexture != null && _uvcManager != null)
                {
                    var tex = _miGetTexture.Invoke(_uvcManager, null) as Texture;
                    if (tex != null)
                    {
                        bool isNew = tex != _frame;
                        _frame = tex;
                        _running = true;
                        if (isNew)
                            _events.RaiseFrame(_frame);
                    }
                }

                // Timeout if start was requested but no frames arrived.
                if (_startRequested && _frame == null && Time.unscaledTime - _startTime > 3f)
                {
                    SetError(CaptureErrorCode.NoDevice,
                        "No UVC frames after 3s. Close Meta HDMI Link, allow USB for Cine Quest (same capture card), check UVCManager / InjectFrame.");
                    _startRequested = false;
                    return;
                }

                if (_frame == null) return;

                _fpsCount++;
                _fpsWindow += Time.unscaledDeltaTime;
                if (_fpsWindow >= 0.5f)
                {
                    _measuredFps = _fpsCount / _fpsWindow;
                    _fpsCount = 0;
                    _fpsWindow = 0f;
                }

                _status.IsStreaming = true;
                _status.Width = _frame.width;
                _status.Height = _frame.height;
                _status.MeasuredFps = _measuredFps;
                _status.EstimatedLatencyMs = EstimateLatencyMs(_status.Width, _status.Height, _status.UsbSpeed);
                _status.DeviceName = "UVC4Unity";

                if (_status.Width >= 1920 && _status.Height >= 1080 && _measuredFps > 0f && _measuredFps < 45f)
                {
                    _status.UsbSpeed = UsbLinkSpeed.HiSpeed;
                    _status.Error = CaptureErrorCode.UsbSpeedWarning;
                    _status.ErrorMessage = "Frame rate low — use USB 3 SuperSpeed capture card & cable";
                }
                else
                {
                    if (_status.UsbSpeed == UsbLinkSpeed.Unknown && _measuredFps >= 55f)
                        _status.UsbSpeed = UsbLinkSpeed.SuperSpeed;
                    _status.Error = CaptureErrorCode.None;
                    _status.ErrorMessage = null;
                }

                _events.RaiseStatus(_status);
            }
            catch (Exception ex)
            {
                SetError(CaptureErrorCode.InternalError, ex.Message);
            }
        }

        public void StartAudio()
        {
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
        /// Integrators with a known-good UVC MonoBehaviour should call this each frame (or on texture swap).
        /// </summary>
        public void InjectFrame(Texture texture, CaptureStatus status)
        {
            _frame = texture;
            _running = texture != null;
            _startRequested = texture != null;
            _status = status;
            if (texture != null)
            {
                _status.IsStreaming = true;
                _events.RaiseFrame(texture);
            }
            else
            {
                _status.IsStreaming = false;
            }
            _events.RaiseStatus(_status);
        }

        void TryBindPlugin()
        {
#if CINE_QUEST_UVC4UNITY
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

            // Plugin present only if we can actually talk to an instance (or InjectFrame will be used).
            _pluginPresent = true;
#else
            _uvcManagerType = FindType("UVCManager");
            _pluginPresent = _uvcManagerType != null;
            if (_pluginPresent)
            {
                Debug.Log("[CineQuest] UVCManager type found. Add scripting define CINE_QUEST_UVC4UNITY for full binding.");
                var existing = UnityEngine.Object.FindObjectsByType(_uvcManagerType, FindObjectsSortMode.None);
                if (existing != null && existing.Length > 0)
                    _uvcManager = existing[0];
                _miGetTexture = _uvcManagerType.GetMethod("GetTexture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                ?? _uvcManagerType.GetMethod("get_PreviewTexture", BindingFlags.Instance | BindingFlags.Public);
                _miStart = _uvcManagerType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public)
                           ?? _uvcManagerType.GetMethod("StartPreview", BindingFlags.Instance | BindingFlags.Public);
                _miStop = _uvcManagerType.GetMethod("Close", BindingFlags.Instance | BindingFlags.Public)
                          ?? _uvcManagerType.GetMethod("StopPreview", BindingFlags.Instance | BindingFlags.Public);
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
            _running = false;
            _startRequested = false;
            _status.Error = code;
            _status.ErrorMessage = message;
            _status.IsStreaming = false;
            _events.RaiseError(code, message);
            _events.RaiseStatus(_status);
            Debug.LogWarning($"[CineQuest] Capture: {code} — {message}");
        }

        static float EstimateLatencyMs(int w, int h, UsbLinkSpeed speed)
        {
            float baseMs = 16f;
            if (speed == UsbLinkSpeed.HiSpeed) baseMs += 20f;
            if (w * h > 1920 * 1080) baseMs += 8f;
            return baseMs;
        }
    }
}
