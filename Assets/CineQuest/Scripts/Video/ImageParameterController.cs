// Cine Quest — Owns ImageParameters, lock/bypass rules, and material push.
// Mutation rules delegated to CineQuest.Core.ImageParameterState.

using System;
using CineQuest.Core;
using UnityEngine;

namespace CineQuest.Video
{
    public sealed class ImageParameterController : MonoBehaviour
    {
        [SerializeField] ImageParameters parameters = ImageParameters.CreateNeutral();
        [SerializeField] Material targetMaterial;

        public ImageParameters Parameters => parameters;

        public event Action<ImageParameters> ParametersChanged;

        public bool IsLocked => parameters != null && parameters.locked;
        public bool IsBypass => parameters != null && parameters.bypass;

        void OnEnable()
        {
            Push();
        }

        public void SetMaterial(Material mat)
        {
            targetMaterial = mat;
            Push();
        }

        public void SetLocked(bool locked)
        {
            var state = EnsureState();
            state.SetLocked(locked);
            parameters.ApplyState(state);
            Push();
            ParametersChanged?.Invoke(parameters);
        }

        public void SetBypass(bool bypass)
        {
            var state = EnsureState();
            state.SetBypass(bypass);
            parameters.ApplyState(state);
            Push();
            ParametersChanged?.Invoke(parameters);
        }

        public void SetColorSpace(VideoColorSpace space)
        {
            var state = EnsureState();
            state.SetColorSpace((ColorSpaceMode)(int)space);
            parameters.ApplyState(state);
            Push();
            ParametersChanged?.Invoke(parameters);
        }

        /// <summary>Set a scalar parameter if not locked.</summary>
        public bool TrySet(string name, float value)
        {
            var state = EnsureState();
            if (!state.TrySet(name, value)) return false;
            parameters.ApplyState(state);
            Push();
            ParametersChanged?.Invoke(parameters);
            return true;
        }

        public void ApplyPreset(ImageParameters preset, bool forceUnlock = false)
        {
            if (preset == null) return;
            var state = EnsureState();
            state.ApplyPreset(preset.ToState(), forceUnlock);
            parameters.ApplyState(state);
            Push();
            ParametersChanged?.Invoke(parameters);
        }

        public void ReplaceAll(ImageParameters src)
        {
            if (src == null) return;
            if (parameters == null) parameters = ImageParameters.CreateNeutral();
            parameters.CopyFrom(src);
            Push();
            ParametersChanged?.Invoke(parameters);
        }

        public void Push()
        {
            parameters?.ApplyToMaterial(targetMaterial);
        }

        ImageParameterState EnsureState()
        {
            if (parameters == null) parameters = ImageParameters.CreateNeutral();
            return parameters.ToState();
        }
    }
}
