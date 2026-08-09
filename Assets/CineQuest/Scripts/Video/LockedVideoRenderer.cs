// Cine Quest — Binds capture texture to the LockedVideo material. No post-processing.

using CineQuest.Capture;
using UnityEngine;

namespace CineQuest.Video
{
    [RequireComponent(typeof(Renderer))]
    public sealed class LockedVideoRenderer : MonoBehaviour
    {
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] CaptureService captureService;
        [SerializeField] ImageParameterController parameterController;
        [SerializeField] Material lockedVideoMaterial;
        [SerializeField] bool createMaterialInstance = true;
        [SerializeField] bool flipY;

        Renderer _renderer;
        Material _mat;
        Texture _bound;

        public Material Material => _mat;
        public Texture BoundTexture => _bound;

        public void Bind(CaptureService capture, ImageParameterController parameters, Material material = null)
        {
            if (capture != null) captureService = capture;
            if (parameters != null) parameterController = parameters;
            if (material != null)
            {
                lockedVideoMaterial = material;
                _mat = createMaterialInstance ? Instantiate(material) : material;
                if (_renderer != null) _renderer.sharedMaterial = _mat;
            }
            if (parameterController != null && _mat != null)
                parameterController.SetMaterial(_mat);
        }

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (lockedVideoMaterial == null)
            {
                var shader = Shader.Find("CineQuest/LockedVideo");
                if (shader != null)
                    lockedVideoMaterial = new Material(shader) { name = "LockedVideo_Runtime" };
            }

            if (lockedVideoMaterial != null && _mat == null)
            {
                _mat = createMaterialInstance ? Instantiate(lockedVideoMaterial) : lockedVideoMaterial;
                _renderer.sharedMaterial = _mat;
                _mat.SetFloat("_FlipY", flipY ? 1f : 0f);
            }

            if (parameterController != null && _mat != null)
                parameterController.SetMaterial(_mat);
        }

        void OnEnable()
        {
            if (captureService == null)
                captureService = CaptureService.Instance;

            if (captureService != null)
            {
                captureService.OnFrameChanged += OnFrame;
                if (captureService.CurrentFrame != null)
                    OnFrame(captureService.CurrentFrame);
            }
        }

        void OnDisable()
        {
            if (captureService != null)
                captureService.OnFrameChanged -= OnFrame;
        }

        void LateUpdate()
        {
            if (captureService == null)
                captureService = CaptureService.Instance;

            if (captureService != null)
            {
                var t = captureService.CurrentFrame;
                if (t != null && t != _bound)
                    OnFrame(t);
            }

            parameterController?.Push();
        }

        void OnFrame(Texture tex)
        {
            if (_mat == null || tex == null) return;
            _bound = tex;
            _mat.SetTexture(MainTexId, tex);
        }

        public void SetFlipY(bool flip)
        {
            flipY = flip;
            _mat?.SetFloat("_FlipY", flip ? 1f : 0f);
        }
    }
}
