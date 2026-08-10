// Cine Quest — Owns the active capture backend, hotplug/retry, and status broadcast.
// Chooses Editor synthetic in Editor when no device backend is ready.

using System;
using UnityEngine;

namespace CineQuest.Capture
{
    public enum CaptureBackendKind
    {
        Auto = 0,
        Uvc4Unity = 1,
        UsbVideoNative = 2,
        Synthetic = 3
    }

    /// <summary>
    /// Scene singleton driving IVideoCaptureSource. Other systems subscribe to Events / Status.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class CaptureService : MonoBehaviour
    {
        public static CaptureService Instance { get; private set; }

        [Header("Backend")]
        [SerializeField] CaptureBackendKind backend = CaptureBackendKind.Auto;
        [SerializeField] int preferredWidth = 1920;
        [SerializeField] int preferredHeight = 1080;
        [SerializeField] float preferredFps = 60f;
        [SerializeField] SyntheticPattern editorPattern = SyntheticPattern.ColorBars;
        [SerializeField] bool preferSyntheticInEditor = true;

        [Header("Watchdog")]
        [SerializeField] float signalLostSeconds = 2f;
        [SerializeField] float reconnectIntervalSeconds = 3f;
        [SerializeField] float statusBroadcastHz = 4f;

        IVideoCaptureSource _source;
        IAudioCaptureSource _audio;
        float _lastFrameTime;
        float _reconnectTimer;
        float _lastStatusBroadcast;
        Texture _lastTexture;
        CaptureStatus _lastBroadcastStatus;
        Action<CaptureStatus> _statusHandler;
        Action<Texture> _frameHandler;

        public IVideoCaptureSource Source => _source;
        public CaptureStatus Status => _source?.Status ?? CaptureStatus.Empty;
        public Texture CurrentFrame => _source?.CurrentFrame;
        public CaptureEvents Events => _source?.Events;

        public event Action<CaptureStatus> OnStatusChanged;
        public event Action<Texture> OnFrameChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateBackend();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            TeardownSource();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause) _source?.StopCapture();
            else _source?.StartCapture();
        }

        void Update()
        {
            if (_source == null) return;

            _source.Tick();

            var tex = _source.CurrentFrame;
            if (tex != null && tex != _lastTexture)
            {
                _lastTexture = tex;
                _lastFrameTime = Time.unscaledTime;
                OnFrameChanged?.Invoke(tex);
            }
            else if (tex != null)
            {
                _lastFrameTime = Time.unscaledTime;
            }

            // Signal-lost watchdog while we expect a device stream
            if (_source.IsRunning && !(Application.isEditor && preferSyntheticInEditor))
            {
                if (Time.unscaledTime - _lastFrameTime > signalLostSeconds && _lastFrameTime > 0f)
                {
                    var st = _source.Status;
                    st.Error = CaptureErrorCode.SignalLost;
                    st.ErrorMessage = "Signal lost — check HDMI/DP cable and capture card power";
                    BroadcastStatus(st, force: true);

                    _reconnectTimer += Time.unscaledDeltaTime;
                    if (_reconnectTimer >= reconnectIntervalSeconds)
                    {
                        _reconnectTimer = 0f;
                        Debug.Log("[CineQuest] Attempting capture reconnect…");
                        _source.StopCapture();
                        _source.StartCapture();
                    }
                }
                else
                {
                    _reconnectTimer = 0f;
                }
            }

            // Throttled status (not every frame)
            float interval = statusBroadcastHz > 0.1f ? 1f / statusBroadcastHz : 0.25f;
            if (Time.unscaledTime - _lastStatusBroadcast >= interval)
                BroadcastStatus(_source.Status, force: false);
        }

        void BroadcastStatus(CaptureStatus st, bool force)
        {
            if (!force && StatusEquals(st, _lastBroadcastStatus))
                return;
            _lastBroadcastStatus = st;
            _lastStatusBroadcast = Time.unscaledTime;
            OnStatusChanged?.Invoke(st);
        }

        static bool StatusEquals(CaptureStatus a, CaptureStatus b)
        {
            return a.IsStreaming == b.IsStreaming
                   && a.Width == b.Width
                   && a.Height == b.Height
                   && a.Error == b.Error
                   && a.UsbSpeed == b.UsbSpeed
                   && a.HasAudio == b.HasAudio
                   && Mathf.Abs(a.MeasuredFps - b.MeasuredFps) < 0.5f
                   && a.ErrorMessage == b.ErrorMessage
                   && a.DeviceName == b.DeviceName;
        }

        public void SetSyntheticPattern(SyntheticPattern pattern)
        {
            if (_source is EditorSyntheticCaptureSource synth)
                synth.Pattern = pattern;
            editorPattern = pattern;
        }

        public void Restart()
        {
            TeardownSource();
            CreateBackend();
        }

        public void SetBackend(CaptureBackendKind kind)
        {
            backend = kind;
            Restart();
        }

        void CreateBackend()
        {
            CaptureBackendKind chosen = backend;
            if (chosen == CaptureBackendKind.Auto)
            {
#if UNITY_EDITOR
                chosen = preferSyntheticInEditor ? CaptureBackendKind.Synthetic : CaptureBackendKind.Uvc4Unity;
#else
                chosen = CaptureBackendKind.Uvc4Unity;
#endif
            }

            switch (chosen)
            {
                case CaptureBackendKind.UsbVideoNative:
                    _source = new UsbVideoNativeCaptureSource();
                    break;
                case CaptureBackendKind.Synthetic:
                    var synth = new EditorSyntheticCaptureSource(preferredWidth, preferredHeight)
                    {
                        Pattern = editorPattern
                    };
                    _source = synth;
                    break;
                default:
                    var uvc = new Uvc4UnityCaptureSource();
                    _source = uvc;
                    _audio = uvc;
                    break;
            }

            AttachSourceEvents();
            _source.Configure(preferredWidth, preferredHeight, preferredFps);
            _source.StartCapture();

            // Fail-closed: if primary never runs (no texture), fall back to synthetic.
            if (!_source.IsRunning && chosen != CaptureBackendKind.Synthetic)
            {
                // Give UVC a short grace only if start is in progress (waiting for first frame).
                // UsbVideoNative / missing manager: IsRunning false immediately → fallback now.
                bool waitingForFirstFrame = _source is Uvc4UnityCaptureSource
                                            && _source.Status.Error == CaptureErrorCode.NoDevice
                                            && !string.IsNullOrEmpty(_source.Status.ErrorMessage)
                                            && _source.Status.ErrorMessage.Contains("Waiting");

                if (!waitingForFirstFrame)
                {
                    FallbackToSynthetic(chosen);
                }
                // If waiting, Tick will surface timeout; optional delayed fallback:
                else
                {
                    // Keep UVC path; CaptureService Update/watchdog + UVC 3s timeout handle failure.
                    // For Editor device tests without card, user can switch backend.
                }
            }

            // Immediate synthetic fallback when backend unavailable (not "waiting")
            if (!_source.IsRunning
                && chosen != CaptureBackendKind.Synthetic
                && _source.Status.Error == CaptureErrorCode.BackendUnavailable)
            {
                FallbackToSynthetic(chosen);
            }

            _lastFrameTime = Time.unscaledTime;
            BroadcastStatus(_source.Status, force: true);
            Debug.Log($"[CineQuest] Capture backend: {chosen} running={_source.IsRunning} err={_source.Status.Error}");
        }

        void FallbackToSynthetic(CaptureBackendKind failed)
        {
            Debug.LogWarning($"[CineQuest] Primary capture ({failed}) unavailable — falling back to synthetic patterns.");
            TeardownSource();
            var fallback = new EditorSyntheticCaptureSource(preferredWidth, preferredHeight)
            {
                Pattern = editorPattern
            };
            _source = fallback;
            AttachSourceEvents();
            _source.Configure(preferredWidth, preferredHeight, preferredFps);
            _source.StartCapture();
        }

        void AttachSourceEvents()
        {
            if (_source?.Events == null) return;

            _statusHandler = s => BroadcastStatus(s, force: true);
            _frameHandler = t =>
            {
                _lastTexture = t;
                _lastFrameTime = Time.unscaledTime;
                OnFrameChanged?.Invoke(t);
            };
            _source.Events.StatusChanged += _statusHandler;
            _source.Events.FrameTextureChanged += _frameHandler;
        }

        void DetachSourceEvents()
        {
            if (_source?.Events == null) return;
            if (_statusHandler != null)
                _source.Events.StatusChanged -= _statusHandler;
            if (_frameHandler != null)
                _source.Events.FrameTextureChanged -= _frameHandler;
            _statusHandler = null;
            _frameHandler = null;
        }

        void TeardownSource()
        {
            try
            {
                DetachSourceEvents();
                _audio?.StopAudio();
                _audio?.Dispose();
                _source?.StopCapture();
                _source?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CineQuest] Capture teardown: {ex.Message}");
            }
            _audio = null;
            _source = null;
            _lastTexture = null;
        }

        public void StartAudioIfAvailable()
        {
            _audio?.StartAudio();
        }

        public void StopAudio()
        {
            _audio?.StopAudio();
        }
    }
}
