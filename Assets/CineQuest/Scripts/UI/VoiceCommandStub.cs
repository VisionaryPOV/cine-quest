// Cine Quest — Optional Meta Voice SDK hook (stub).
// When Meta Voice SDK is imported, bind phrases to the methods below.

using CineQuest.Video;
using CineQuest.Scopes;
using UnityEngine;

namespace CineQuest.UI
{
    public sealed class VoiceCommandStub : MonoBehaviour
    {
        [SerializeField] ImageParameterController imageParams;
        [SerializeField] ScopeManager scopeManager;
        [SerializeField] TheaterModeController theater;
        [SerializeField] FreezeFrameController freeze;

        void Start()
        {
            if (imageParams == null) imageParams = FindFirstObjectByType<ImageParameterController>();
            if (scopeManager == null) scopeManager = FindFirstObjectByType<ScopeManager>();
            if (theater == null) theater = FindFirstObjectByType<TheaterModeController>();
            if (freeze == null) freeze = FindFirstObjectByType<FreezeFrameController>();
        }

        // Bind these from Meta Voice dictation / wit.ai intent handlers:

        public void Voice_LockImage() => imageParams?.SetLocked(true);
        public void Voice_UnlockImage() => imageParams?.SetLocked(false);
        public void Voice_ShowWaveform() => scopeManager?.SetScopeEnabled(ScopeType.Waveform, true);
        public void Voice_HideWaveform() => scopeManager?.SetScopeEnabled(ScopeType.Waveform, false);
        public void Voice_ShowParade() => scopeManager?.SetScopeEnabled(ScopeType.RgbParade, true);
        public void Voice_ShowVectorscope() => scopeManager?.SetScopeEnabled(ScopeType.Vectorscope, true);
        public void Voice_ReferenceBypass() => imageParams?.SetBypass(true);
        public void Voice_TheaterMode() => theater?.SetMode(EnvironmentMode.Theater);
        public void Voice_Passthrough() => theater?.SetMode(EnvironmentMode.Passthrough);
        public void Voice_Freeze() => freeze?.Freeze();
        public void Voice_Unfreeze() => freeze?.Unfreeze();
    }
}
