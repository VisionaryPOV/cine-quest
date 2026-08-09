// Cine Quest — Unity-facing image parameters; delegates pure logic to CineQuest.Core.

using System;
using CineQuest.Core;
using UnityEngine;

namespace CineQuest.Video
{
    public enum VideoColorSpace
    {
        Rec709Limited = 0,
        FullRange = 1
    }

    /// <summary>
    /// Serializable grade parameters for Unity Inspector + materials.
    /// Pure behavior lives in <see cref="ImageParameterState"/>.
    /// </summary>
    [Serializable]
    public class ImageParameters
    {
        [Tooltip("Freeze parameters so iris/light changes are faithfully visible.")]
        public bool locked;

        [Tooltip("Force pure identity transform (Reference / Bypass mode).")]
        public bool bypass;

        public VideoColorSpace colorSpace = VideoColorSpace.Rec709Limited;

        [Range(-1f, 1f)] public float brightness;
        [Range(0f, 2f)] public float contrast = 1f;
        [Range(0.1f, 3f)] public float gamma = 1f;
        [Range(0f, 2f)] public float saturation = 1f;
        [Range(-1f, 1f)] public float temperature;
        [Range(-1f, 1f)] public float tint;
        [Range(-0.5f, 0.5f)] public float lift;

        public static ImageParameters CreateNeutral()
        {
            return FromState(ImageParameterState.CreateNeutral());
        }

        public static ImageParameters CreateBypass()
        {
            return FromState(ImageParameterState.CreateBypass());
        }

        public ImageParameters Clone()
        {
            return FromState(ToState().Clone());
        }

        public void CopyFrom(ImageParameters other)
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

        public ImageParameterState ToState()
        {
            return new ImageParameterState
            {
                locked = locked,
                bypass = bypass,
                colorSpace = (ColorSpaceMode)(int)colorSpace,
                brightness = brightness,
                contrast = contrast,
                gamma = gamma,
                saturation = saturation,
                temperature = temperature,
                tint = tint,
                lift = lift
            };
        }

        public void ApplyState(ImageParameterState state)
        {
            if (state == null) return;
            locked = state.locked;
            bypass = state.bypass;
            colorSpace = (VideoColorSpace)(int)state.colorSpace;
            brightness = state.brightness;
            contrast = state.contrast;
            gamma = state.gamma;
            saturation = state.saturation;
            temperature = state.temperature;
            tint = state.tint;
            lift = state.lift;
        }

        public static ImageParameters FromState(ImageParameterState state)
        {
            var p = new ImageParameters();
            p.ApplyState(state);
            return p;
        }

        /// <summary>
        /// Apply to a material using CineQuest/LockedVideo property names.
        /// Uses pure GetEffectiveGrade so Bypass identity matches unit-tested core.
        /// </summary>
        public void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;
            var g = ToState().GetEffectiveGrade();
            mat.SetFloat("_Bypass", g.Bypass ? 1f : 0f);
            mat.SetFloat("_LimitedRange", g.LimitedRange ? 1f : 0f);
            mat.SetFloat("_Brightness", g.Brightness);
            mat.SetFloat("_Contrast", g.Contrast);
            mat.SetFloat("_Gamma", g.Gamma);
            mat.SetFloat("_Saturation", g.Saturation);
            mat.SetFloat("_Temperature", g.Temperature);
            mat.SetFloat("_Tint", g.Tint);
            mat.SetFloat("_Lift", g.Lift);
        }
    }
}
