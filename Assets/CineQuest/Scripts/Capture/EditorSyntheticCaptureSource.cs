// Cine Quest — Editor / no-device synthetic capture.
// Generates SMPTE-style color bars, grayscale ramp, 18% gray, and animated patterns
// so the full pipeline (locked display + scopes) is testable without hardware.

using UnityEngine;

namespace CineQuest.Capture
{
    public enum SyntheticPattern
    {
        ColorBars = 0,
        GrayscaleRamp = 1,
        Gray18 = 2,
        CheckerPulse = 3,
        SkinToneChip = 4
    }

    /// <summary>
    /// CPU-generated test patterns written into a Texture2D each frame (or on pattern change).
    /// Not used on-device when a real UVC source is available.
    /// </summary>
    public sealed class EditorSyntheticCaptureSource : IVideoCaptureSource
    {
        readonly int _width;
        readonly int _height;
        readonly CaptureEvents _events = new CaptureEvents();

        Texture2D _texture;
        Color32[] _pixels;
        SyntheticPattern _pattern = SyntheticPattern.ColorBars;
        float _time;
        float _fpsAccum;
        int _fpsFrames;
        float _measuredFps = 60f;
        bool _dirty = true;
        bool _running;

        public EditorSyntheticCaptureSource(int width = 1920, int height = 1080)
        {
            _width = Mathf.Max(16, width);
            _height = Mathf.Max(16, height);
        }

        public bool IsRunning => _running;
        public Texture CurrentFrame => _texture;
        public int FrameGeneration { get; private set; }
        public CaptureStatus Status { get; private set; }
        public CaptureEvents Events => _events;
        public SyntheticPattern Pattern
        {
            get => _pattern;
            set
            {
                if (_pattern == value) return;
                _pattern = value;
                _dirty = true;
            }
        }

        public void Configure(int preferredWidth, int preferredHeight, float preferredFps)
        {
            // Fixed resolution for synthetic; preferred values only affect status labels.
            Status = BuildStatus();
        }

        public void StartCapture()
        {
            if (_texture == null)
            {
                _texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false, true)
                {
                    name = "CineQuest_SyntheticCapture",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0
                };
                _pixels = new Color32[_width * _height];
            }

            _running = true;
            _dirty = true;
            Generate(0f);
            Status = BuildStatus();
            _events.RaiseFrame(_texture);
            _events.RaiseStatus(Status);
        }

        public void StopCapture()
        {
            _running = false;
            Status = BuildStatus();
            Status.IsStreaming = false;
            _events.RaiseStatus(Status);
        }

        public void Tick()
        {
            if (!_running || _texture == null) return;

            _time += Time.unscaledDeltaTime;
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            if (_fpsAccum >= 0.5f)
            {
                _measuredFps = _fpsFrames / _fpsAccum;
                _fpsFrames = 0;
                _fpsAccum = 0f;
            }

            // CheckerPulse animates; other patterns only regenerate when dirty.
            if (_pattern == SyntheticPattern.CheckerPulse || _dirty)
            {
                Generate(_time);
                _dirty = false;
                _events.RaiseFrame(_texture);
            }

            Status = BuildStatus();
        }

        public void Dispose()
        {
            StopCapture();
            if (_texture != null)
            {
                Object.Destroy(_texture);
                _texture = null;
            }
            _pixels = null;
        }

        CaptureStatus BuildStatus()
        {
            return new CaptureStatus
            {
                IsStreaming = _running,
                Width = _width,
                Height = _height,
                MeasuredFps = _measuredFps,
                EstimatedLatencyMs = 0f,
                UsbSpeed = UsbLinkSpeed.SuperSpeed,
                ColorFormat = CaptureColorFormat.RGBA32,
                HasAudio = false,
                Error = CaptureErrorCode.None,
                ErrorMessage = null,
                DeviceName = $"Synthetic/{_pattern}"
            };
        }

        void Generate(float t)
        {
            switch (_pattern)
            {
                case SyntheticPattern.GrayscaleRamp:
                    FillGrayscaleRamp();
                    break;
                case SyntheticPattern.Gray18:
                    FillSolid(new Color32(46, 46, 46, 255)); // ≈18% of 255 in sRGB-ish
                    break;
                case SyntheticPattern.CheckerPulse:
                    FillCheckerPulse(t);
                    break;
                case SyntheticPattern.SkinToneChip:
                    FillSkinToneChip();
                    break;
                default:
                    FillColorBars();
                    break;
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false, false);
            FrameGeneration++;
        }

        void FillSolid(Color32 c)
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = c;
        }

        void FillGrayscaleRamp()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    byte v = (byte)Mathf.Clamp(Mathf.RoundToInt((x / (float)(_width - 1)) * 255f), 0, 255);
                    _pixels[y * _width + x] = new Color32(v, v, v, 255);
                }
            }
        }

        void FillColorBars()
        {
            // 75% SMPTE-like bars (approximate Rec.709 primaries in 8-bit).
            Color32[] bars =
            {
                new Color32(180, 180, 180, 255), // gray
                new Color32(180, 180, 16, 255),  // yellow
                new Color32(16, 180, 180, 255),  // cyan
                new Color32(16, 180, 16, 255),   // green
                new Color32(180, 16, 180, 255),  // magenta
                new Color32(180, 16, 16, 255),   // red
                new Color32(16, 16, 180, 255)    // blue
            };

            int barW = _width / bars.Length;
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int bi = Mathf.Min(x / Mathf.Max(1, barW), bars.Length - 1);
                    // Bottom pluge / black chip strip
                    if (y < _height * 0.12f)
                    {
                        float u = x / (float)_width;
                        byte v = u < 0.3f ? (byte)0 : u < 0.4f ? (byte)16 : u < 0.5f ? (byte)0 : u < 0.6f ? (byte)235 : (byte)255;
                        _pixels[y * _width + x] = new Color32(v, v, v, 255);
                    }
                    else
                    {
                        _pixels[y * _width + x] = bars[bi];
                    }
                }
            }
        }

        void FillCheckerPulse(float t)
        {
            // Brightness oscillates — used to verify Lock does NOT auto-compensate.
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI * 0.5f); // 0.5 Hz
            byte hi = (byte)Mathf.RoundToInt(Mathf.Lerp(40f, 220f, pulse));
            byte lo = (byte)Mathf.RoundToInt(Mathf.Lerp(20f, 80f, pulse));
            int cell = 64;
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    bool on = ((x / cell) + (y / cell)) % 2 == 0;
                    byte v = on ? hi : lo;
                    _pixels[y * _width + x] = new Color32(v, v, v, 255);
                }
            }
        }

        void FillSkinToneChip()
        {
            // Approximate Rec.709 skin-tone region reference chip + surrounding gray.
            Color32 skin = new Color32(194, 150, 130, 255);
            Color32 gray = new Color32(128, 128, 128, 255);
            int cx = _width / 2;
            int cy = _height / 2;
            int r = Mathf.Min(_width, _height) / 5;
            int r2 = r * r;
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    _pixels[y * _width + x] = (dx * dx + dy * dy) <= r2 ? skin : gray;
                }
            }
        }
    }
}
