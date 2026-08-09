// Cine Quest — Pure image-parameter state + grade math (no UnityEngine).
// Shipped source of truth for lock/bypass/range behavior; Unity wrappers map to materials.

using System;

namespace CineQuest.Core
{
    public enum ColorSpaceMode
    {
        Rec709Limited = 0,
        FullRange = 1
    }

    /// <summary>
    /// Serializable monitoring grade parameters. Unity's ImageParameters mirrors this.
    /// </summary>
    [Serializable]
    public sealed class ImageParameterState
    {
        public bool locked;
        public bool bypass;
        public ColorSpaceMode colorSpace = ColorSpaceMode.Rec709Limited;
        public float brightness;
        public float contrast = 1f;
        public float gamma = 1f;
        public float saturation = 1f;
        public float temperature;
        public float tint;
        public float lift;

        public static ImageParameterState CreateNeutral()
        {
            return new ImageParameterState
            {
                locked = false,
                bypass = false,
                colorSpace = ColorSpaceMode.Rec709Limited,
                brightness = 0f,
                contrast = 1f,
                gamma = 1f,
                saturation = 1f,
                temperature = 0f,
                tint = 0f,
                lift = 0f
            };
        }

        public static ImageParameterState CreateBypass()
        {
            var p = CreateNeutral();
            p.bypass = true;
            p.locked = true;
            return p;
        }

        public ImageParameterState Clone()
        {
            return new ImageParameterState
            {
                locked = locked,
                bypass = bypass,
                colorSpace = colorSpace,
                brightness = brightness,
                contrast = contrast,
                gamma = gamma,
                saturation = saturation,
                temperature = temperature,
                tint = tint,
                lift = lift
            };
        }

        public void CopyFrom(ImageParameterState other)
        {
            if (other == null) return;
            locked = other.locked;
            bypass = other.bypass;
            colorSpace = other.colorSpace;
            brightness = other.brightness;
            contrast = other.contrast;
            gamma = other.gamma;
            saturation = other.saturation;
            temperature = other.temperature;
            tint = other.tint;
            lift = other.lift;
        }

        /// <summary>
        /// Effective grade pushed to the shader. Bypass forces identity creative controls.
        /// Limited-range flag remains so legal/full decode can still apply.
        /// </summary>
        public EffectiveGrade GetEffectiveGrade()
        {
            if (bypass)
            {
                return new EffectiveGrade
                {
                    Bypass = true,
                    LimitedRange = colorSpace == ColorSpaceMode.Rec709Limited,
                    Brightness = 0f,
                    Contrast = 1f,
                    Gamma = 1f,
                    Saturation = 1f,
                    Temperature = 0f,
                    Tint = 0f,
                    Lift = 0f
                };
            }

            return new EffectiveGrade
            {
                Bypass = false,
                LimitedRange = colorSpace == ColorSpaceMode.Rec709Limited,
                Brightness = brightness,
                Contrast = contrast,
                Gamma = gamma,
                Saturation = saturation,
                Temperature = temperature,
                Tint = tint,
                Lift = lift
            };
        }

        /// <summary>Try set a named scalar. Returns false when locked (or unknown name).</summary>
        public bool TrySet(string name, float value)
        {
            if (locked) return false;
            if (string.IsNullOrEmpty(name)) return false;

            switch (name.ToLowerInvariant())
            {
                case "brightness":
                    brightness = Clamp(value, -1f, 1f);
                    return true;
                case "contrast":
                    contrast = Clamp(value, 0f, 2f);
                    return true;
                case "gamma":
                    gamma = Clamp(value, 0.1f, 3f);
                    return true;
                case "saturation":
                    saturation = Clamp(value, 0f, 2f);
                    return true;
                case "temperature":
                    temperature = Clamp(value, -1f, 1f);
                    return true;
                case "tint":
                    tint = Clamp(value, -1f, 1f);
                    return true;
                case "lift":
                    lift = Clamp(value, -0.5f, 0.5f);
                    return true;
                default:
                    return false;
            }
        }

        public void SetLocked(bool value) => locked = value;

        public void SetBypass(bool value)
        {
            bypass = value;
            if (value) locked = true;
        }

        /// <summary>Color range is allowed even when locked (signal format, not creative grade).</summary>
        public void SetColorSpace(ColorSpaceMode space) => colorSpace = space;

        public void ApplyPreset(ImageParameterState preset, bool forceUnlock = false)
        {
            if (preset == null) return;
            if (locked && !forceUnlock && !preset.bypass) return;
            CopyFrom(preset);
        }

        /// <summary>
        /// Apply the same contrast formula as LockedVideo.shader:
        /// (c - 0.5) * contrast + 0.5
        /// Full grade path for unit verification of creative transform.
        /// </summary>
        public static float ApplyContrast(float c, float contrast)
        {
            return (c - 0.5f) * contrast + 0.5f;
        }

        /// <summary>
        /// Apply creative grade to a single channel mid-gray sample (0–1).
        /// When bypass is true, returns input unchanged (after optional limited expand is separate).
        /// </summary>
        public float ApplyCreativeToChannel(float c)
        {
            var g = GetEffectiveGrade();
            if (g.Bypass) return c;

            c = c + g.Lift;
            c = c + g.Brightness;
            c = ApplyContrast(c, g.Contrast);
            // Saturation/temp/tint need RGB; single-channel path only exercises lift/gain/contrast
            if (Math.Abs(g.Gamma - 1f) > 1e-6f)
            {
                float sign = c < 0 ? -1f : 1f;
                c = sign * (float)Math.Pow(Math.Abs(c) + 1e-5, 1.0 / Math.Max(g.Gamma, 1e-3));
            }
            return Clamp(c, 0f, 1f);
        }

        /// <summary>Expand Rec.709 limited 8-bit style sample (0–1 domain) to full range.</summary>
        public static float ExpandLimited(float c)
        {
            const float offset = 16f / 255f;
            const float scale = 255f / 219f;
            return Clamp((c - offset) * scale, 0f, 1f);
        }

        static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }

    public struct EffectiveGrade
    {
        public bool Bypass;
        public bool LimitedRange;
        public float Brightness;
        public float Contrast;
        public float Gamma;
        public float Saturation;
        public float Temperature;
        public float Tint;
        public float Lift;
    }
}
