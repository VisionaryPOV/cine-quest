// Cine Quest — RGB/Luma histogram on downsampled analysis RT (CPU path, rate-limited by ScopeManager).

using UnityEngine;

namespace CineQuest.Scopes
{
    public sealed class HistogramScope : MonoBehaviour
    {
        [SerializeField] int bins = 256;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] int sampleMaxPixels = 16384;

        Texture2D _readback;
        Texture2D _histTex;
        int[] _histR, _histG, _histB, _histY;
        Color32[] _histPixels;
        bool _busy;

        public void SetTargetRenderer(Renderer r) => targetRenderer = r;

        void OnEnable()
        {
            _histR = new int[bins];
            _histG = new int[bins];
            _histB = new int[bins];
            _histY = new int[bins];
            _histTex = new Texture2D(bins, 128, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "HistogramTex"
            };
            _histPixels = new Color32[bins * 128];
        }

        void OnDisable()
        {
            if (_readback != null) Destroy(_readback);
            if (_histTex != null) Destroy(_histTex);
            _busy = false;
        }

        public void Process(RenderTexture source)
        {
            if (!isActiveAndEnabled || source == null || _busy) return;
            _busy = true;

            try
            {
                int rw = Mathf.Min(source.width, 160);
                int rh = Mathf.Min(source.height, 90);

                var tmp = RenderTexture.GetTemporary(rw, rh, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, tmp);

                if (_readback == null || _readback.width != rw || _readback.height != rh)
                {
                    if (_readback != null) Destroy(_readback);
                    _readback = new Texture2D(rw, rh, TextureFormat.RGBA32, false);
                }

                var prev = RenderTexture.active;
                RenderTexture.active = tmp;
                _readback.ReadPixels(new Rect(0, 0, rw, rh), 0, 0, false);
                _readback.Apply(false, false);
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(tmp);

                System.Array.Clear(_histR, 0, bins);
                System.Array.Clear(_histG, 0, bins);
                System.Array.Clear(_histB, 0, bins);
                System.Array.Clear(_histY, 0, bins);

                var px = _readback.GetPixels32();
                int step = Mathf.Max(1, px.Length / sampleMaxPixels);
                for (int i = 0; i < px.Length; i += step)
                {
                    var c = px[i];
                    _histR[c.r]++;
                    _histG[c.g]++;
                    _histB[c.b]++;
                    int y = (c.r * 54 + c.g * 183 + c.b * 19) >> 8;
                    _histY[Mathf.Clamp(y, 0, bins - 1)]++;
                }

                int max = 1;
                for (int i = 0; i < bins; i++)
                    max = Mathf.Max(max, Mathf.Max(_histY[i], Mathf.Max(_histR[i], Mathf.Max(_histG[i], _histB[i]))));

                int texH = 128;
                for (int x = 0; x < bins; x++)
                {
                    int hY = Mathf.RoundToInt((_histY[x] / (float)max) * (texH - 1));
                    int hR = Mathf.RoundToInt((_histR[x] / (float)max) * (texH - 1));
                    int hG = Mathf.RoundToInt((_histG[x] / (float)max) * (texH - 1));
                    int hB = Mathf.RoundToInt((_histB[x] / (float)max) * (texH - 1));
                    for (int y = 0; y < texH; y++)
                    {
                        byte r = (byte)(y <= hR ? 200 : 10);
                        byte g = (byte)(y <= hG ? 200 : 10);
                        byte b = (byte)(y <= hB ? 200 : 10);
                        if (y <= hY) { r = (byte)Mathf.Min(255, r + 40); g = (byte)Mathf.Min(255, g + 40); b = (byte)Mathf.Min(255, b + 40); }
                        _histPixels[y * bins + x] = new Color32(r, g, b, 255);
                    }
                }

                _histTex.SetPixels32(_histPixels);
                _histTex.Apply(false, false);

                if (targetRenderer != null)
                {
                    if (targetRenderer.sharedMaterial == null)
                    {
                        var sh = Shader.Find("CineQuest/ScopeWaveformViz");
                        if (sh != null) targetRenderer.sharedMaterial = new Material(sh);
                    }
                    if (targetRenderer.sharedMaterial != null)
                        targetRenderer.sharedMaterial.mainTexture = _histTex;
                }
            }
            finally
            {
                _busy = false;
            }
        }
    }
}
