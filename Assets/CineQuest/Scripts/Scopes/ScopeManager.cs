// Cine Quest — Coordinates downsample + scope updates without starving main video.

using CineQuest.Capture;
using CineQuest.Core;
using CineQuest.Video;
using UnityEngine;

namespace CineQuest.Scopes
{
    public sealed class ScopeManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] CaptureService captureService;
        [SerializeField] FreezeFrameController freezeFrame;

        [Header("Compute")]
        [SerializeField] ComputeShader waveformCompute;
        [SerializeField] ComputeShader paradeCompute;
        [SerializeField] ComputeShader vectorscopeCompute;

        [Header("Quality")]
        [SerializeField] ScopeQualityMode qualityMode = ScopeQualityMode.Balanced;

        [Header("Scopes")]
        [SerializeField] WaveformScope waveform;
        [SerializeField] ParadeScope parade;
        [SerializeField] VectorscopeScope vectorscope;
        [SerializeField] HistogramScope histogram;

        RenderTexture _analysisRt;
        int _frameCounter;
        float _lastUpdateTime;

        public ScopeQualityMode QualityMode
        {
            get => qualityMode;
            set => qualityMode = value;
        }

        public Texture AnalysisSource { get; private set; }

        void OnEnable()
        {
            if (captureService == null) captureService = CaptureService.Instance;
        }

        void OnDestroy()
        {
            ReleaseAnalysis();
        }

        void LateUpdate()
        {
            Texture src = null;
            if (freezeFrame != null && freezeFrame.IsFrozen)
                src = freezeFrame.AnalysisTexture;
            else if (captureService != null)
                src = captureService.CurrentFrame;

            if (src == null) return;
            AnalysisSource = src;

            if (!ShouldUpdateThisFrame()) return;

            EnsureAnalysisRt(src);
            Graphics.Blit(src, _analysisRt);

            bool any =
                (waveform != null && waveform.isActiveAndEnabled) ||
                (parade != null && parade.isActiveAndEnabled) ||
                (vectorscope != null && vectorscope.isActiveAndEnabled) ||
                (histogram != null && histogram.isActiveAndEnabled);

            if (!any) return;

            waveform?.Process(_analysisRt, waveformCompute);
            parade?.Process(_analysisRt, paradeCompute);
            vectorscope?.Process(_analysisRt, vectorscopeCompute);
            histogram?.Process(_analysisRt);

            _lastUpdateTime = Time.unscaledTime;
        }

        bool ShouldUpdateThisFrame()
        {
            _frameCounter++;
            var mode = (ScopeQuality)(int)qualityMode;
            return ScopeQualityPolicy.ShouldUpdate(mode, _frameCounter, Time.unscaledTime - _lastUpdateTime);
        }

        void EnsureAnalysisRt(Texture src)
        {
            int w = ScopeQualityPolicy.AnalysisWidth((ScopeQuality)(int)qualityMode, src.width);
            float aspect = src.width / (float)Mathf.Max(1, src.height);
            int h = Mathf.Max(1, Mathf.RoundToInt(w / aspect));

            if (_analysisRt != null && _analysisRt.width == w && _analysisRt.height == h) return;
            ReleaseAnalysis();
            _analysisRt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                name = "CineQuest_ScopeAnalysis",
                filterMode = FilterMode.Point,
                enableRandomWrite = false,
                useMipMap = false
            };
            _analysisRt.Create();
        }

        void ReleaseAnalysis()
        {
            if (_analysisRt == null) return;
            _analysisRt.Release();
            Destroy(_analysisRt);
            _analysisRt = null;
        }

        public void SetScopeEnabled(ScopeType type, bool enabled)
        {
            switch (type)
            {
                case ScopeType.Waveform:
                    if (waveform != null) waveform.gameObject.SetActive(enabled);
                    break;
                case ScopeType.RgbParade:
                    if (parade != null) parade.gameObject.SetActive(enabled);
                    break;
                case ScopeType.Vectorscope:
                    if (vectorscope != null) vectorscope.gameObject.SetActive(enabled);
                    break;
                case ScopeType.Histogram:
                    if (histogram != null) histogram.gameObject.SetActive(enabled);
                    break;
            }
        }

        /// <summary>Runtime wiring used by RuntimeSceneBuilder when Inspector refs are empty.</summary>
        public void Bind(
            CaptureService capture,
            FreezeFrameController freeze,
            WaveformScope wf,
            ParadeScope pr,
            VectorscopeScope vs,
            HistogramScope hs,
            ComputeShader wfCs,
            ComputeShader paradeCs,
            ComputeShader vecCs)
        {
            if (capture != null) captureService = capture;
            if (freeze != null) freezeFrame = freeze;
            if (wf != null) waveform = wf;
            if (pr != null) parade = pr;
            if (vs != null) vectorscope = vs;
            if (hs != null) histogram = hs;
            if (wfCs != null) waveformCompute = wfCs;
            if (paradeCs != null) paradeCompute = paradeCs;
            if (vecCs != null) vectorscopeCompute = vecCs;
        }

        public void AutoFindScopes()
        {
            if (waveform == null) waveform = FindFirstObjectByType<WaveformScope>();
            if (parade == null) parade = FindFirstObjectByType<ParadeScope>();
            if (vectorscope == null) vectorscope = FindFirstObjectByType<VectorscopeScope>();
            if (histogram == null) histogram = FindFirstObjectByType<HistogramScope>();
            if (captureService == null) captureService = CaptureService.Instance;
            if (freezeFrame == null) freezeFrame = FindFirstObjectByType<FreezeFrameController>();

            if (waveformCompute == null) waveformCompute = Resources.Load<ComputeShader>("Compute/ScopeWaveform");
            if (paradeCompute == null) paradeCompute = Resources.Load<ComputeShader>("Compute/ScopeParade");
            if (vectorscopeCompute == null) vectorscopeCompute = Resources.Load<ComputeShader>("Compute/ScopeVectorscope");
        }
    }
}
