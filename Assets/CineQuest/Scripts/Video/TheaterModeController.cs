// Cine Quest — Passthrough (default) vs darkened Theater / Cinema environment.

using UnityEngine;

namespace CineQuest.Video
{
    public enum EnvironmentMode
    {
        Passthrough = 0,
        Theater = 1
    }

    public sealed class TheaterModeController : MonoBehaviour
    {
        [SerializeField] VideoPanel videoPanel;
        [SerializeField] Transform theaterAnchor;
        [SerializeField] GameObject theaterEnvironmentRoot;
        [SerializeField] Light[] dimLights;
        [SerializeField] float theaterLightIntensity = 0.05f;
        [SerializeField] Color theaterAmbient = Color.black;

        EnvironmentMode _mode = EnvironmentMode.Passthrough;
        float[] _savedLightIntensity;
        Color _savedAmbient;
        bool _savedFog;
        Color _savedFogColor;

        public EnvironmentMode Mode => _mode;
        public event System.Action<EnvironmentMode> ModeChanged;

        public void Bind(VideoPanel panel, GameObject environmentRoot, Transform anchor = null)
        {
            if (panel != null) videoPanel = panel;
            if (environmentRoot != null) theaterEnvironmentRoot = environmentRoot;
            if (anchor != null) theaterAnchor = anchor;
        }

        void Awake()
        {
            if (theaterEnvironmentRoot != null)
                theaterEnvironmentRoot.SetActive(false);

            CacheLighting();
        }

        void CacheLighting()
        {
            _savedAmbient = RenderSettings.ambientLight;
            _savedFog = RenderSettings.fog;
            _savedFogColor = RenderSettings.fogColor;
            if (dimLights != null)
            {
                _savedLightIntensity = new float[dimLights.Length];
                for (int i = 0; i < dimLights.Length; i++)
                    if (dimLights[i] != null)
                        _savedLightIntensity[i] = dimLights[i].intensity;
            }
        }

        public void SetMode(EnvironmentMode mode)
        {
            if (_mode == mode) return;
            _mode = mode;

            if (mode == EnvironmentMode.Theater)
            {
                if (theaterEnvironmentRoot != null) theaterEnvironmentRoot.SetActive(true);
                videoPanel?.SetMode(VideoPanelMode.Theater, theaterAnchor);
                ApplyTheaterLighting();
                // Meta Passthrough: disable underlay when using OVRPassthroughLayer / MRUK Building Blocks.
                TrySetPassthrough(false);
            }
            else
            {
                if (theaterEnvironmentRoot != null) theaterEnvironmentRoot.SetActive(false);
                videoPanel?.SetMode(VideoPanelMode.Floating);
                RestoreLighting();
                TrySetPassthrough(true);
            }

            ModeChanged?.Invoke(_mode);
        }

        public void Toggle()
        {
            SetMode(_mode == EnvironmentMode.Passthrough
                ? EnvironmentMode.Theater
                : EnvironmentMode.Passthrough);
        }

        void ApplyTheaterLighting()
        {
            RenderSettings.ambientLight = theaterAmbient;
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.black;
            if (dimLights == null) return;
            for (int i = 0; i < dimLights.Length; i++)
                if (dimLights[i] != null)
                    dimLights[i].intensity = theaterLightIntensity;
        }

        void RestoreLighting()
        {
            RenderSettings.ambientLight = _savedAmbient;
            RenderSettings.fog = _savedFog;
            RenderSettings.fogColor = _savedFogColor;
            if (dimLights == null || _savedLightIntensity == null) return;
            for (int i = 0; i < dimLights.Length; i++)
                if (dimLights[i] != null && i < _savedLightIntensity.Length)
                    dimLights[i].intensity = _savedLightIntensity[i];
        }

        static void TrySetPassthrough(bool enabled)
        {
            // Soft integration: call OVRManager / OVRPassthroughLayer if present.
            var ovrManagerType = System.Type.GetType("OVRManager, Oculus.VR")
                                 ?? System.Type.GetType("OVRManager, Meta.XR.OVRManager");
            if (ovrManagerType == null) return;

            var instProp = ovrManagerType.GetProperty("instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var inst = instProp?.GetValue(null);
            if (inst == null) return;

            var isPassthroughSupported = ovrManagerType.GetProperty("isInsightPassthroughEnabled");
            // Prefer finding OVRPassthroughLayer components
            var layerType = System.Type.GetType("OVRPassthroughLayer, Oculus.VR");
            if (layerType == null) return;
            var layers = Object.FindObjectsByType(layerType, FindObjectsSortMode.None);
            foreach (var layer in layers)
            {
                var behaviour = layer as Behaviour;
                if (behaviour != null) behaviour.enabled = enabled;
            }
        }
    }
}
