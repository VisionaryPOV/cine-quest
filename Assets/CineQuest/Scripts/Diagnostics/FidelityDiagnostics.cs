// Cine Quest — Editor/runtime checks that the monitoring path is not being auto-graded.

using CineQuest.Video;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CineQuest.Diagnostics
{
    public sealed class FidelityDiagnostics : MonoBehaviour
    {
        [SerializeField] ImageParameterController imageParams;
        [SerializeField] bool logOnStart = true;

        void Start()
        {
            if (imageParams == null)
                imageParams = FindFirstObjectByType<ImageParameterController>();
            if (logOnStart) RunReport();
        }

        public void Bind(ImageParameterController img) => imageParams = img;

        [ContextMenu("Run Fidelity Report")]
        public void RunReport()
        {
            int issues = 0;

            foreach (var cam in Camera.allCameras)
            {
                if (cam == null) continue;
                var urp = cam.GetComponent<UniversalAdditionalCameraData>();
                if (urp != null && urp.renderPostProcessing)
                {
                    Debug.LogWarning($"[CineQuest:Fidelity] Camera '{cam.name}' has URP post-processing ENABLED — disable for monitoring.");
                    issues++;
                }
            }

            var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            foreach (var v in volumes)
            {
                if (v != null && v.enabled && v.isActiveAndEnabled)
                {
                    Debug.LogWarning($"[CineQuest:Fidelity] Volume '{v.name}' is active — may tonemap/grade the view.");
                    issues++;
                }
            }

            if (imageParams != null && imageParams.Parameters != null)
            {
                var p = imageParams.Parameters;
                Debug.Log($"[CineQuest:Fidelity] Bypass={p.bypass} Locked={p.locked} ColorSpace={p.colorSpace} " +
                          $"B={p.brightness} C={p.contrast} G={p.gamma} S={p.saturation}");
            }

            if (issues == 0)
                Debug.Log("[CineQuest:Fidelity] No automatic post-processing issues detected on cameras/volumes.");
            else
                Debug.LogWarning($"[CineQuest:Fidelity] {issues} issue(s) found.");
        }
    }
}
