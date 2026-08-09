// Cine Quest — Luma waveform monitor (compute-backed).

using UnityEngine;

namespace CineQuest.Scopes
{
    public sealed class WaveformScope : MonoBehaviour
    {
        [SerializeField] int columns = 256;
        [SerializeField] int binCount = 256;
        [SerializeField] float normalizeMax = 512f;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Material vizMaterial;

        ComputeBuffer _bins;
        RenderTexture _outRt;
        int _kClear, _kAcc, _kRes;
        bool _kernelsReady;

        public RenderTexture Output => _outRt;

        public void SetTargetRenderer(Renderer r) => targetRenderer = r;

        void OnEnable()
        {
            EnsureResources();
        }

        void OnDisable()
        {
            Release();
        }

        public void Process(RenderTexture source, ComputeShader cs)
        {
            if (!isActiveAndEnabled || cs == null || source == null) return;
            EnsureResources();
            if (_bins == null || _outRt == null) return;

            if (!_kernelsReady)
            {
                _kClear = cs.FindKernel("Clear");
                _kAcc = cs.FindKernel("Accumulate");
                _kRes = cs.FindKernel("Resolve");
                _kernelsReady = true;
            }

            cs.SetInt("_Columns", columns);
            cs.SetInt("_BinCount", binCount);
            cs.SetInt("_SrcWidth", source.width);
            cs.SetInt("_SrcHeight", source.height);
            cs.SetFloat("_MaxCount", normalizeMax);

            cs.SetBuffer(_kClear, "_Bins", _bins);
            cs.Dispatch(_kClear, Mathf.CeilToInt((columns * binCount) / 64f), 1, 1);

            cs.SetTexture(_kAcc, "_Source", source);
            cs.SetBuffer(_kAcc, "_Bins", _bins);
            cs.Dispatch(_kAcc,
                Mathf.CeilToInt(source.width / 8f),
                Mathf.CeilToInt(source.height / 8f), 1);

            cs.SetBuffer(_kRes, "_Bins", _bins);
            cs.SetTexture(_kRes, "_OutTex", _outRt);
            cs.Dispatch(_kRes,
                Mathf.CeilToInt(columns / 8f),
                Mathf.CeilToInt(binCount / 8f), 1);

            ApplyToRenderer();
        }

        void EnsureResources()
        {
            int count = columns * binCount;
            if (_bins == null || _bins.count != count)
            {
                _bins?.Release();
                _bins = new ComputeBuffer(count, sizeof(uint));
            }

            if (_outRt == null || _outRt.width != columns || _outRt.height != binCount)
            {
                if (_outRt != null)
                {
                    _outRt.Release();
                    Destroy(_outRt);
                }
                _outRt = new RenderTexture(columns, binCount, 0, RenderTextureFormat.ARGB32)
                {
                    name = "WaveformRT",
                    enableRandomWrite = true,
                    filterMode = FilterMode.Bilinear
                };
                _outRt.Create();
            }
        }

        void ApplyToRenderer()
        {
            if (targetRenderer == null) return;
            if (vizMaterial == null)
            {
                var sh = Shader.Find("CineQuest/ScopeWaveformViz");
                if (sh != null) vizMaterial = new Material(sh);
            }
            if (vizMaterial == null) return;
            vizMaterial.mainTexture = _outRt;
            targetRenderer.sharedMaterial = vizMaterial;
        }

        void Release()
        {
            _bins?.Release();
            _bins = null;
            if (_outRt != null)
            {
                _outRt.Release();
                Destroy(_outRt);
                _outRt = null;
            }
            _kernelsReady = false;
        }
    }
}
