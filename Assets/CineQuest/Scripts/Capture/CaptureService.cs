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
        int _lastFrameGeneration = -1;
        bool _hadRealFrame;
        CaptureStatus _lastBroadcastStatus;
        Action<CaptureStatus> _statusHandler;
        Action<Texture> _frameHandler;
        VideoFrameNormalizer _normalizer;

        public IVideoCaptureSource Source => _source;
        public CaptureStatus Status => _source?.Status ?? CaptureStatus.Empty;
        public Texture CurrentFrame => _source?.CurrentFrame;
        /// <summary>2D ARGB copy for freeze/scopes (safe to Blit). May be null until first normalize.</summary>
        public Texture DisplayFrame =>
            _normalizer != null && _normalizer.RgbFrame != null
                ? _normalizer.RgbFrame
                : CurrentFrame;
        public bool WaitingForFirstFrame => !_hadRealFrame && !CineQuest.Core.CaptureLifecyclePolicy.IsSyntheticDeviceName(Status.DeviceName);
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

        void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                // Release USB so HDMI Link can reclaim the card. Do not wipe last displayed texture.
                _source?.StopCapture();
            }
            else
            {
                _source?.StartCapture();
            }
        }

        void Update()
        {
            if (_source == null) return;

            _source.Tick();

            var tex = _source.CurrentFrame;
            int gen = _source.FrameGeneration;
            bool texChanged = tex != null && tex != _lastTexture;
            bool genChanged = gen != _lastFrameGeneration && gen > 0;
            if (CineQuest.Core.CaptureLifecyclePolicy.ShouldAdvanceLastFrameTime(texChanged, genChanged))
            {
                _lastTexture = tex;
                _lastFrameGeneration = gen;
                _lastFrameTime = Time.unscaledTime;
                _hadRealFrame = true;
                if (tex != null)
                {
                    _normalizer?.Normalize(tex);
                    OnFrameChanged?.Invoke(_normalizer != null && _normalizer.RgbFrame != null
                        ? _normalizer.RgbFrame
                        : tex);
                }
            }

            float since = _hadRealFrame ? Time.unscaledTime - _lastFrameTime : 0f;
            if (CineQuest.Core.CaptureLifecyclePolicy.ShouldWatchdogReconnect(
                    _source.IsRunning, _hadRealFrame, since, signalLostSeconds)
                && !(Application.isEditor && preferSyntheticInEditor))
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
                    _source.StartCapture();
                }
            }
            else
            {
                _reconnectTimer = 0f;
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

            if (_normalizer == null)
                _normalizer = new VideoFrameNormalizer();
            _hadRealFrame = false;
            _lastTexture = null;
            _lastFrameGeneration = -1;
            _lastFrameTime = 0f;

            AttachSourceEvents();
            _source.Configure(preferredWidth, preferredHeight, preferredFps);
            _source.StartCapture();

            // Editor: synthetic fallback so Play Mode works without a card.
            // Device: never silently fake a live feed — HUD must show SYNTHETIC or NO DEVICE.
            bool allowFallback = CineQuest.Core.CaptureLifecyclePolicy.AllowSilentSyntheticFallback(Application.isEditor);
            if (allowFallback
                && !_source.IsRunning
                && chosen != CaptureBackendKind.Synthetic
                && _source.Status.Error == CaptureErrorCode.BackendUnavailable)
            {
                FallbackToSynthetic(chosen);
            }

            BroadcastStatus(_source.Status, force: true);
            string srcName = _source.Status.DeviceName ?? _source.GetType().Name;
            Debug.Log($"[CineQuest] Capture backend chosen={chosen} source={srcName} running={_source.IsRunning} err={_source.Status.Error}");
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
                int gen = _source != null ? _source.FrameGeneration : 0;
                bool texChanged = t != null && t != _lastTexture;
                bool genChanged = gen != _lastFrameGeneration && gen > 0;
                if (!CineQuest.Core.CaptureLifecyclePolicy.ShouldAdvanceLastFrameTime(texChanged, genChanged))
                    return;
                _lastTexture = t;
                _lastFrameGeneration = gen;
                _lastFrameTime = Time.unscaledTime;
                _hadRealFrame = true;
                _normalizer?.Normalize(t);
                OnFrameChanged?.Invoke(_normalizer != null && _normalizer.RgbFrame != null
                    ? _normalizer.RgbFrame
                    : t);
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
            _hadRealFrame = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            TeardownSource();
            _normalizer?.Dispose();
            _normalizer = null;
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
