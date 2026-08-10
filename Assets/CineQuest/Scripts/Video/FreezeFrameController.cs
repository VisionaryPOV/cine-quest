// Cine Quest — Freeze-frame for still analysis (display + scopes).
// Single ownership: sets LockedVideoRenderer display freeze so live frames cannot clobber.

using CineQuest.Capture;
using UnityEngine;

namespace CineQuest.Video
{
    public sealed class FreezeFrameController : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;
        [SerializeField] LockedVideoRenderer videoRenderer;
        [SerializeField] bool freezeAffectsDisplay = true;

        RenderTexture _freezeRt;
        bool _frozen;

        public bool IsFrozen => _frozen;
        public Texture AnalysisTexture => _frozen && _freezeRt != null
            ? _freezeRt
            : (captureService != null ? captureService.CurrentFrame : null);

        public event System.Action<bool> FreezeChanged;

        public void Bind(CaptureService capture, LockedVideoRenderer renderer)
        {
            if (capture != null) captureService = capture;
            if (renderer != null) videoRenderer = renderer;
        }

        void Start()
        {
            if (captureService == null) captureService = CaptureService.Instance;
            if (videoRenderer == null) videoRenderer = GetComponent<LockedVideoRenderer>();
        }

        void OnDestroy()
        {
            ReleaseRt();
        }

        public void Toggle()
        {
            if (_frozen) Unfreeze();
            else Freeze();
        }

        public void Freeze()
        {
            if (captureService == null) captureService = CaptureService.Instance;
            var src = captureService != null ? captureService.CurrentFrame : null;
            if (src == null) return;

            EnsureRt(src.width, src.height);
            Graphics.Blit(src, _freezeRt);
            _frozen = true;

            if (freezeAffectsDisplay)
            {
                if (videoRenderer == null) videoRenderer = GetComponent<LockedVideoRenderer>();
                // Own display binding so CaptureService frame events cannot overwrite.
                videoRenderer?.SetDisplayFrozen(true, _freezeRt);
            }

            FreezeChanged?.Invoke(true);
        }

        public void Unfreeze()
        {
            _frozen = false;
            if (freezeAffectsDisplay)
            {
                if (videoRenderer == null) videoRenderer = GetComponent<LockedVideoRenderer>();
                var live = captureService != null ? captureService.CurrentFrame : null;
                videoRenderer?.SetDisplayFrozen(false, live);
            }
            FreezeChanged?.Invoke(false);
        }

        void EnsureRt(int w, int h)
        {
            if (_freezeRt != null && _freezeRt.width == w && _freezeRt.height == h) return;
            ReleaseRt();
            _freezeRt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                name = "CineQuest_FreezeFrame",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            _freezeRt.Create();
        }

        void ReleaseRt()
        {
            if (_freezeRt == null) return;
            _freezeRt.Release();
            Destroy(_freezeRt);
            _freezeRt = null;
        }
    }
}
