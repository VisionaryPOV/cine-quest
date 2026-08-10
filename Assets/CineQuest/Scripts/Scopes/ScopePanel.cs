// Cine Quest — Movable / resizable / opacity-controlled scope panel.

using UnityEngine;

namespace CineQuest.Scopes
{
    public sealed class ScopePanel : MonoBehaviour
    {
        [SerializeField] ScopeType scopeType = ScopeType.Waveform;
        [SerializeField] Renderer scopeRenderer;
        [Range(0.1f, 1f)] [SerializeField] float opacity = 0.95f;
        [SerializeField] float minScale = 0.25f;
        [SerializeField] float maxScale = 2.5f;

        public ScopeType Type => scopeType;
        public float Opacity => opacity;

        public void SetType(ScopeType type) => scopeType = type;

        void LateUpdate()
        {
            if (scopeRenderer != null && scopeRenderer.sharedMaterial != null)
            {
                if (scopeRenderer.sharedMaterial.HasProperty("_Opacity"))
                    scopeRenderer.sharedMaterial.SetFloat("_Opacity", opacity);
            }
        }

        public void SetOpacity(float value)
        {
            opacity = Mathf.Clamp01(value);
        }

        public void SetUniformScale(float s)
        {
            s = Mathf.Clamp(s, minScale, maxScale);
            transform.localScale = new Vector3(s * 1.6f, s, s);
        }

        public void SetPose(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            transform.SetPositionAndRotation(pos, rot);
            transform.localScale = scale;
        }

        public Pose GetPose() => new Pose(transform.position, transform.rotation);
    }
}
