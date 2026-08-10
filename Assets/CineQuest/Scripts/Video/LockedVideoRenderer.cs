// Cine Quest — Binds capture texture to the LockedVideo material. No post-processing.
// Freeze ownership + optional External OES keyword for Android UVC SurfaceTextures.

using CineQuest.Capture;
using CineQuest.Core;
using UnityEngine;

namespace CineQuest.Video
{
    [RequireComponent(typeof(Renderer))]
    public sealed class LockedVideoRenderer : MonoBehaviour
    {
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        const string OesKeyword = "CQ_EXTERNAL_OES";

        [SerializeField] CaptureService captureService;
        [SerializeField] ImageParameterController parameterController;
        [SerializeField] FreezeFrameController freezeFrame;
        [SerializeField] Material lockedVideoMaterial;
        [SerializeField] bool createMaterialInstance = true;
        [SerializeField] bool flipY;
        [Tooltip("Enable GL_TEXTURE_EXTERNAL_OES sampling (many Android UVC plugins).")]
        [SerializeField] bool useExternalOes;

        Renderer _renderer;
        Material _mat;
        Texture _bound;
        bool _displayFrozen;

        public Material Material => _mat;
        public Texture BoundTexture => _bound;
        public bool IsDisplayFrozen => _displayFrozen;

        public void Bind(CaptureService capture, ImageParameterController parameters, Material material = null,
            FreezeFrameController freeze = null)
        {
            if (capture != null) captureService = capture;
            if (parameters != null) parameterController = parameters;
            if (freeze != null) freezeFrame = freeze;
            if (material != null)
            {
                lockedVideoMaterial = material;
                _mat = createMaterialInstance ? Instantiate(material) : material;
                if (_renderer != null) _renderer.sharedMaterial = _mat;
                ApplyOesKeyword();
            }
            if (parameterController != null && _mat != null)
                parameterController.SetMaterial(_mat);
        }

        public void SetUseExternalOes(bool enabled)
        {
            useExternalOes = enabled;
            ApplyOesKeyword();
        }

        void ApplyOesKeyword()
        {
            if (_mat == null) return;
            if (useExternalOes) _mat.EnableKeyword(OesKeyword);
            else _mat.DisableKeyword(OesKeyword);
            if (_mat.HasProperty("_UseExternalOES"))
                _mat.SetFloat("_UseExternalOES", useExternalOes ? 1f : 0f);
        }

        /// <summary>Freeze owns the display texture while frozen. Live capture must not rebind.</summary>
        public void SetDisplayFrozen(bool frozen, Texture freezeOrLiveTexture)
        {
            _displayFrozen = frozen;
            if (freezeOrLiveTexture != null && _mat != null)
            {
                _bound = freezeOrLiveTexture;
                _mat.SetTexture(MainTexId, freezeOrLiveTexture);
            }
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
                ApplyOesKeyword();
            }

            if (parameterController != null && _mat != null)
                parameterController.SetMaterial(_mat);
        }

        void OnEnable()
        {
            if (captureService == null)
                captureService = CaptureService.Instance;
            if (freezeFrame == null)
                freezeFrame = GetComponent<FreezeFrameController>();

            if (freezeFrame != null)
                freezeFrame.FreezeChanged += OnFreezeChanged;

            if (captureService != null)
            {
                captureService.OnFrameChanged += OnFrame;
                if (DisplayFreezePolicy.ShouldBindLiveFrame(_displayFrozen) && captureService.CurrentFrame != null)
                    OnFrame(captureService.CurrentFrame);
            }
        }

        void OnDisable()
        {
            if (captureService != null)
                captureService.OnFrameChanged -= OnFrame;
            if (freezeFrame != null)
                freezeFrame.FreezeChanged -= OnFreezeChanged;
        }

        void LateUpdate()
        {
            bool frozen = _displayFrozen || (freezeFrame != null && freezeFrame.IsFrozen);
            if (!DisplayFreezePolicy.ShouldBindLiveFrame(frozen))
            {
                parameterController?.Push();
                return;
            }

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

        void OnFreezeChanged(bool frozen) => _displayFrozen = frozen;

        void OnFrame(Texture tex)
        {
            bool frozen = _displayFrozen || (freezeFrame != null && freezeFrame.IsFrozen);
            if (!DisplayFreezePolicy.ShouldBindLiveFrame(frozen))
                return;
            if (_mat == null || tex == null) return;
            _bound = tex;
            _mat.SetTexture(MainTexId, tex);

            // Heuristic: Texture2D.CreateExternalTexture often reports dimension Tex2D but is OES on device.
            // Integrators can force via SetUseExternalOes(true).
        }

        public void SetFlipY(bool flip)
        {
            flipY = flip;
            _mat?.SetFloat("_FlipY", flip ? 1f : 0f);
        }
    }
}
