// Cine Quest — Burn-in tally on the video raster (broadcast-monitor language).

using CineQuest.Capture;
using CineQuest.Core;
using CineQuest.Video;
using UnityEngine;
using UnityEngine.UI;

namespace CineQuest.UI
{
    public sealed class MonitorTallyStrip : MonoBehaviour
    {
        [SerializeField] CaptureService capture;
        [SerializeField] ImageParameterController image;
        [SerializeField] FreezeFrameController freeze;
        [SerializeField] Text tallyText;
        [SerializeField] Image bar;

        public void Bind(CaptureService cap, ImageParameterController img, FreezeFrameController fr, Text text, Image background)
        {
            capture = cap;
            image = img;
            freeze = fr;
            tallyText = text;
            bar = background;
        }

        void LateUpdate()
        {
            if (tallyText == null) return;
            if (capture == null) capture = CaptureService.Instance;
            if (image == null) image = FindFirstObjectByType<ImageParameterController>();
            if (freeze == null) freeze = FindFirstObjectByType<FreezeFrameController>();

            var st = capture != null ? capture.Status : CaptureStatus.Empty;
            bool bypass = image != null && image.IsBypass;
            bool locked = image != null && image.IsLocked;
            bool frozen = freeze != null && freeze.IsFrozen;
            bool synth = CaptureLifecyclePolicy.IsSyntheticDeviceName(st.DeviceName);

            string mode = frozen ? "FROZEN" : bypass ? "BYPASS" : locked ? "LOCKED" : "UNLOCKED";
            string src = synth ? "SYNTHETIC" : capture != null && capture.WaitingForFirstFrame ? "WAITING" : st.ResolutionLabel;
            string fps = st.MeasuredFps > 1f ? $"{st.MeasuredFps:0}p" : "—";

            tallyText.text = $"{mode}  ·  {src}  ·  {fps}  ·  RELATIVE";

            if (bar != null)
            {
                if (frozen) bar.color = new Color(0.85f, 0.35f, 0.08f, 0.88f);
                else if (synth || (capture != null && capture.WaitingForFirstFrame))
                    bar.color = new Color(0.15f, 0.15f, 0.18f, 0.88f);
                else if (bypass) bar.color = new Color(0.05f, 0.35f, 0.42f, 0.88f);
                else if (locked) bar.color = new Color(0.42f, 0.32f, 0.08f, 0.88f);
                else bar.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            }
        }
    }
}
