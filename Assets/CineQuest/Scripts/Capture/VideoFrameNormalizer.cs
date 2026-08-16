// Cine Quest — Copy live UVC (possibly External OES) into one ARGB32 RT.
// Freeze, scopes, and false color all Blit this 2D texture.

using UnityEngine;

namespace CineQuest.Capture
{
    public sealed class VideoFrameNormalizer : System.IDisposable
    {
        RenderTexture _rgb;
        Material _blitMat;

        public Texture RgbFrame => _rgb;

        public void Normalize(Texture source)
        {
            if (source == null) return;

            int w = Mathf.Max(1, source.width);
            int h = Mathf.Max(1, source.height);
            if (_rgb == null || _rgb.width != w || _rgb.height != h)
            {
                _rgb?.Release();
                if (_rgb != null) Object.Destroy(_rgb);
                _rgb = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
                {
                    name = "CineQuest_NormalizedRgb",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false
                };
                _rgb.Create();
            }

            if (_blitMat == null)
            {
                var sh = Shader.Find("CineQuest/OesBlit")
                         ?? Resources.Load<Shader>("Shaders/OesBlit");
                if (sh != null) _blitMat = new Material(sh);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_blitMat != null)
            {
                _blitMat.EnableKeyword("CQ_EXTERNAL_OES");
                Graphics.Blit(source, _rgb, _blitMat);
                return;
            }
#endif
            if (_blitMat != null)
            {
                _blitMat.DisableKeyword("CQ_EXTERNAL_OES");
                Graphics.Blit(source, _rgb, _blitMat);
            }
            else
            {
                Graphics.Blit(source, _rgb);
            }
        }

        public void Dispose()
        {
            if (_rgb != null)
            {
                _rgb.Release();
                Object.Destroy(_rgb);
                _rgb = null;
            }
            if (_blitMat != null)
            {
                Object.Destroy(_blitMat);
                _blitMat = null;
            }
        }
    }
}
