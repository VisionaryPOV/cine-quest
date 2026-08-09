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

        IVideoCaptureSource _source;
        IAudioCaptureSource _audio;
        float _lastFrameTime;
        float _reconnectTimer;
        Texture _lastTexture;

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
                    OnStatusChanged?.Invoke(st);

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

            OnStatusChanged?.Invoke(_source.Status);
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

            if (_source.Events != null)
            {
                _source.Events.StatusChanged += s => OnStatusChanged?.Invoke(s);
                _source.Events.FrameTextureChanged += t =>
                {
                    _lastTexture = t;
                    _lastFrameTime = Time.unscaledTime;
                    OnFrameChanged?.Invoke(t);
                };
            }

            _source.Configure(preferredWidth, preferredHeight, preferredFps);
            _source.StartCapture();

            // If UVC backend unavailable on device, fall back to synthetic so UI remains usable.
            if (!_source.IsRunning && chosen != CaptureBackendKind.Synthetic)
            {
                Debug.LogWarning("[CineQuest] Primary capture backend failed — falling back to synthetic patterns.");
                TeardownSource();
                var fallback = new EditorSyntheticCaptureSource(preferredWidth, preferredHeight)
                {
                    Pattern = editorPattern
                };
                _source = fallback;
                _source.Configure(preferredWidth, preferredHeight, preferredFps);
                _source.StartCapture();
            }

            _lastFrameTime = Time.unscaledTime;
            Debug.Log($"[CineQuest] Capture backend: {chosen} running={_source.IsRunning}");
        }

        void TeardownSource()
        {
            try
            {
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
