// Cine Quest — Optional false-color / zebra overlay on a secondary quad.

using CineQuest.Capture;
using CineQuest.Core;
using UnityEngine;

namespace CineQuest.Video
{
    public sealed class FalseColorController : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;
        [SerializeField] Renderer overlayRenderer;
        [SerializeField] Material falseColorMaterial;
        [SerializeField] bool startEnabled;

        Material _mat;
        bool _enabled;

        public bool IsEnabled => _enabled;

        void Awake()
        {
            if (overlayRenderer == null)
                overlayRenderer = GetComponent<Renderer>();

            if (falseColorMaterial == null)
            {
                var sh = Shader.Find("CineQuest/FalseColor");
                if (sh != null) falseColorMaterial = new Material(sh);
            }
            if (falseColorMaterial != null)
                _mat = Instantiate(falseColorMaterial);

            if (overlayRenderer != null)
            {
                overlayRenderer.sharedMaterial = _mat;
                overlayRenderer.enabled = startEnabled;
            }
            _enabled = startEnabled;
        }

        void OnEnable()
        {
            if (captureService == null) captureService = CaptureService.Instance;
            if (captureService != null) captureService.OnFrameChanged += OnFrame;
        }

        void OnDisable()
        {
            if (captureService != null) captureService.OnFrameChanged -= OnFrame;
        }

        void OnFrame(Texture t)
        {
            if (_mat == null) return;
            var freeze = FindFirstObjectByType<FreezeFrameController>();
            var chosen = DisplayFreezePolicy.SelectAnalysisTexture(
                freeze != null && freeze.IsFrozen,
                freeze != null ? freeze.AnalysisTexture : null,
                t);
            if (chosen != null) _mat.mainTexture = chosen;
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (overlayRenderer != null) overlayRenderer.enabled = enabled;
        }

        public void Toggle() => SetEnabled(!_enabled);
    }
}
