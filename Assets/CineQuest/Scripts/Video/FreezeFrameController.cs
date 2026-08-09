// Cine Quest — Freeze-frame for still analysis (display + scopes).

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
        Texture _liveBeforeFreeze;

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
            _liveBeforeFreeze = src;

            if (freezeAffectsDisplay && videoRenderer != null && videoRenderer.Material != null)
            {
                videoRenderer.Material.SetTexture("_MainTex", _freezeRt);
            }

            FreezeChanged?.Invoke(true);
        }

        public void Unfreeze()
        {
            _frozen = false;
            if (freezeAffectsDisplay && videoRenderer != null && captureService != null && captureService.CurrentFrame != null)
            {
                videoRenderer.Material?.SetTexture("_MainTex", captureService.CurrentFrame);
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
