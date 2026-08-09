// Cine Quest — Runtime helper to switch synthetic patterns for fidelity demos.

using CineQuest.Capture;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CineQuest.Diagnostics
{
    public sealed class SignalPulseTest : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;

        void Start()
        {
            if (captureService == null)
                captureService = CaptureService.Instance ?? FindFirstObjectByType<CaptureService>();
        }

        public void Bind(CaptureService capture) => captureService = capture;

        void Update()
        {
            if (captureService == null)
                captureService = CaptureService.Instance;

            var kb = Keyboard.current;
            if (kb == null || captureService == null) return;

            // Number keys select synthetic patterns when in Editor/synthetic mode
            if (kb.digit1Key.wasPressedThisFrame) captureService.SetSyntheticPattern(SyntheticPattern.ColorBars);
            if (kb.digit2Key.wasPressedThisFrame) captureService.SetSyntheticPattern(SyntheticPattern.GrayscaleRamp);
            if (kb.digit3Key.wasPressedThisFrame) captureService.SetSyntheticPattern(SyntheticPattern.Gray18);
            if (kb.digit4Key.wasPressedThisFrame) captureService.SetSyntheticPattern(SyntheticPattern.CheckerPulse);
            if (kb.digit5Key.wasPressedThisFrame) captureService.SetSyntheticPattern(SyntheticPattern.SkinToneChip);
        }
    }
}
