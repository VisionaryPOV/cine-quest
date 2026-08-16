// Cine Quest — Vectorscope (Cb/Cr accumulation).

using UnityEngine;

namespace CineQuest.Scopes
{
    public sealed class VectorscopeScope : MonoBehaviour
    {
        [SerializeField] int size = 256;
        [SerializeField] float normalizeMax = 1024f;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Material vizMaterial;

        ComputeBuffer _bins;
        RenderTexture _outRt;
        int _kClear, _kAcc, _kRes;
        bool _kernelsReady;

        public RenderTexture Output => _outRt;

        public void SetTargetRenderer(Renderer r) => targetRenderer = r;

        void OnEnable() => EnsureResources();
        void OnDisable() => Release();

        public void Process(RenderTexture source, ComputeShader cs)
        {
            if (!isActiveAndEnabled || cs == null || source == null) return;
            EnsureResources();

            if (!_kernelsReady)
            {
                try
                {
                    _kClear = cs.FindKernel("Clear");
                    _kAcc = cs.FindKernel("Accumulate");
                    _kRes = cs.FindKernel("Resolve");
                    _kernelsReady = true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[CineQuest] Vectorscope compute unavailable: {ex.Message}");
                    enabled = false;
                    return;
                }
            }

            cs.SetInt("_Size", size);
            cs.SetInt("_SrcWidth", source.width);
            cs.SetInt("_SrcHeight", source.height);
            cs.SetFloat("_MaxCount", normalizeMax);

            cs.SetBuffer(_kClear, "_Bins", _bins);
            cs.Dispatch(_kClear, Mathf.CeilToInt((size * size) / 64f), 1, 1);

            cs.SetTexture(_kAcc, "_Source", source);
            cs.SetBuffer(_kAcc, "_Bins", _bins);
            cs.Dispatch(_kAcc,
                Mathf.CeilToInt(source.width / 8f),
                Mathf.CeilToInt(source.height / 8f), 1);

            cs.SetBuffer(_kRes, "_Bins", _bins);
            cs.SetTexture(_kRes, "_OutTex", _outRt);
            cs.Dispatch(_kRes,
                Mathf.CeilToInt(size / 8f),
                Mathf.CeilToInt(size / 8f), 1);

            if (targetRenderer != null)
            {
                if (vizMaterial == null)
                {
                    var sh = Shader.Find("CineQuest/ScopeVectorscopeViz");
                    if (sh != null) vizMaterial = new Material(sh);
                }
                if (vizMaterial != null)
                {
                    vizMaterial.mainTexture = _outRt;
                    targetRenderer.sharedMaterial = vizMaterial;
                }
            }
        }

        void EnsureResources()
        {
            int count = size * size;
            if (_bins == null || _bins.count != count)
            {
                _bins?.Release();
                _bins = new ComputeBuffer(count, sizeof(uint));
            }
            if (_outRt == null || _outRt.width != size)
            {
                if (_outRt != null) { _outRt.Release(); Destroy(_outRt); }
                _outRt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32)
                {
                    name = "VectorscopeRT",
                    enableRandomWrite = true,
                    filterMode = FilterMode.Bilinear
                };
                _outRt.Create();
            }
        }

        void Release()
        {
            _bins?.Release();
            _bins = null;
            if (_outRt != null) { _outRt.Release(); Destroy(_outRt); _outRt = null; }
            _kernelsReady = false;
        }
    }
}
