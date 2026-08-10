// Cine Quest — Application entry: fidelity policy, quality, layout restore.

using CineQuest.Capture;
using CineQuest.Persistence;
using CineQuest.Scopes;
using CineQuest.UI;
using CineQuest.Video;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CineQuest.App
{
    /// <summary>
    /// Bootstraps runtime policy: disable post-processing that would corrupt monitoring,
    /// restore layout, and keep target frame rate high.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class CineQuestApp : MonoBehaviour
    {
        public static CineQuestApp Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] CaptureService captureService;
        [SerializeField] ImageParameterController imageParams;
        [SerializeField] LayoutStore layoutStore;
        [SerializeField] MonitorMenuController menu;
        [SerializeField] ScopeManager scopeManager;
        [SerializeField] TheaterModeController theater;

        [Header("Policy")]
        [SerializeField] int targetFrameRate = 72;
        [SerializeField] bool disableVolumePostProcess = true;
        [SerializeField] bool loadLayoutOnStart = true;
        [SerializeField] bool startInBypass = false;
        bool _startRan;

        /// <summary>Wire systems created by RuntimeSceneBuilder (serialized fields stay null otherwise).</summary>
        public void Bind(
            CaptureService capture,
            ImageParameterController img,
            LayoutStore store,
            MonitorMenuController menuCtrl,
            ScopeManager scopes,
            TheaterModeController theaterCtrl)
        {
            if (capture != null) captureService = capture;
            if (img != null) imageParams = img;
            if (store != null) layoutStore = store;
            if (menuCtrl != null) menu = menuCtrl;
            if (scopes != null) scopeManager = scopes;
            if (theaterCtrl != null) theater = theaterCtrl;

            // If Start already ran with null refs, apply policy now.
            if (_startRan)
                ApplyStartPolicy();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CineQuest] Duplicate CineQuestApp destroyed.");
                Destroy(this);
                return;
            }
            Instance = this;
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;

            // Never allow sleep mid-monitoring
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            EnforceNoPostProcessingOnCameras();
        }

        void Start()
        {
            _startRan = true;
            ApplyStartPolicy();
        }

        void ApplyStartPolicy()
        {
            if (startInBypass && imageParams != null)
                imageParams.SetBypass(true);

            if (loadLayoutOnStart && layoutStore != null && menu != null)
                menu.LoadLayout();

            // Default quality for scopes
            if (scopeManager != null)
                scopeManager.QualityMode = ScopeQualityMode.Balanced;

            // Ensure passthrough-friendly clear flags if no Meta layer yet
            var cam = Camera.main;
            if (cam != null && theater != null && theater.Mode == EnvironmentMode.Passthrough)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0);
            }

            Debug.Log("[CineQuest] Ready — signal fidelity mode active. Bypass/Lock control image path only via LockedVideo shader.");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void EnforceNoPostProcessingOnCameras()
        {
            if (!disableVolumePostProcess) return;

            // Disable URP Camera post-processing so video is not tonemapped by volumes.
            foreach (var cam in Camera.allCameras)
            {
                if (cam == null) continue;
                var urp = cam.GetComponent<UniversalAdditionalCameraData>();
                if (urp != null)
                {
                    urp.renderPostProcessing = false;
                    urp.renderShadows = false;
                }
            }

            // Disable any active Volume components that might grade the view.
            var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            foreach (var v in volumes)
            {
                // Keep volumes disabled for monitoring fidelity.
                v.enabled = false;
            }
        }

        public void SetTargetFrameRate(int fps)
        {
            targetFrameRate = fps;
            Application.targetFrameRate = fps;
        }
    }
}
