// Cine Quest — Freely movable, resizable, rotatable floating video panel.
// Hand/controller grab via XR Interaction Toolkit when available; mouse fallback in Editor.

using UnityEngine;

namespace CineQuest.Video
{
    public enum VideoPanelMode
    {
        Floating = 0,
        Theater = 1
    }

    /// <summary>
    /// World-space monitor panel. Grab body to move; use edge colliders for scale.
    /// </summary>
    public sealed class VideoPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform contentRoot;
        [SerializeField] LockedVideoRenderer videoRenderer;
        [SerializeField] MeshRenderer frameRenderer;
        [SerializeField] Collider grabCollider;

        [Header("Layout")]
        [SerializeField] float minScale = 0.4f;
        [SerializeField] float maxScale = 4f;
        [SerializeField] float defaultDistance = 1.5f;
        [SerializeField] Vector2 aspect = new Vector2(16f, 9f);

        [Header("State")]
        [SerializeField] VideoPanelMode mode = VideoPanelMode.Floating;
        [SerializeField] bool isGrabbed;

        Vector3 _theaterLocalPos;
        Quaternion _theaterLocalRot;
        Vector3 _theaterScale;
        Vector3 _floatingPos;
        Quaternion _floatingRot;
        Vector3 _floatingScale;
        bool _hasFloatingPose;

        public LockedVideoRenderer VideoRenderer => videoRenderer;
        public VideoPanelMode Mode => mode;
        public Transform ContentRoot => contentRoot != null ? contentRoot : transform;

        void Awake()
        {
            if (contentRoot == null) contentRoot = transform;
            EnsureMesh();
        }

        void EnsureMesh()
        {
            if (GetComponent<MeshFilter>() == null)
            {
                var mf = gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = BuildQuadMesh(aspect.x / aspect.y);
            }
            if (GetComponent<MeshRenderer>() == null)
            {
                gameObject.AddComponent<MeshRenderer>();
            }
            if (grabCollider == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(aspect.x / aspect.y, 1f, 0.02f);
                grabCollider = box;
            }
            if (videoRenderer == null)
                videoRenderer = GetComponent<LockedVideoRenderer>() ?? gameObject.AddComponent<LockedVideoRenderer>();
        }

        static Mesh BuildQuadMesh(float widthOverHeight)
        {
            float w = widthOverHeight;
            float h = 1f;
            var mesh = new Mesh { name = "VideoPanelQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-w * 0.5f, -h * 0.5f, 0),
                new Vector3( w * 0.5f, -h * 0.5f, 0),
                new Vector3(-w * 0.5f,  h * 0.5f, 0),
                new Vector3( w * 0.5f,  h * 0.5f, 0)
            };
            mesh.uv = new[]
            {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 1), new Vector2(1, 1)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void PlaceInFrontOf(Transform head, float distance = -1f)
        {
            if (head == null) return;
            float d = distance > 0 ? distance : defaultDistance;
            var pos = head.position + head.forward * d;
            pos.y = head.position.y;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(transform.position - head.position, Vector3.up);
            // Face the user
            transform.rotation = Quaternion.LookRotation(head.position - transform.position, Vector3.up);
            transform.Rotate(0f, 180f, 0f);
        }

        public void SetUniformScale(float scale)
        {
            scale = Mathf.Clamp(scale, minScale, maxScale);
            transform.localScale = Vector3.one * scale;
        }

        public void MultiplyScale(float factor)
        {
            SetUniformScale(transform.localScale.x * factor);
        }

        public void SetMode(VideoPanelMode newMode, Transform theaterAnchor = null)
        {
            if (mode == VideoPanelMode.Floating && newMode == VideoPanelMode.Theater)
            {
                _floatingPos = transform.position;
                _floatingRot = transform.rotation;
                _floatingScale = transform.localScale;
                _hasFloatingPose = true;

                if (theaterAnchor != null)
                {
                    transform.SetPositionAndRotation(theaterAnchor.position, theaterAnchor.rotation);
                    transform.localScale = theaterAnchor.localScale.sqrMagnitude > 0.01f
                        ? theaterAnchor.localScale
                        : Vector3.one * 3f;
                }
                else
                {
                    // Large cinema screen forward
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        transform.position = cam.transform.position + cam.transform.forward * 4f;
                        transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
                        transform.localScale = Vector3.one * 3.5f;
                    }
                }
            }
            else if (mode == VideoPanelMode.Theater && newMode == VideoPanelMode.Floating && _hasFloatingPose)
            {
                transform.SetPositionAndRotation(_floatingPos, _floatingRot);
                transform.localScale = _floatingScale;
            }

            mode = newMode;
        }

        // --- Simple editor / desktop grab helpers (XR grab via XRI on same GameObject) ---

        public void BeginGrab() => isGrabbed = true;
        public void EndGrab() => isGrabbed = false;

        public void ApplyGrabPose(Vector3 worldPos, Quaternion worldRot)
        {
            if (!isGrabbed || mode == VideoPanelMode.Theater) return;
            transform.SetPositionAndRotation(worldPos, worldRot);
        }

        public Pose GetPose() => new Pose(transform.position, transform.rotation);
        public void SetPose(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            transform.SetPositionAndRotation(pos, rot);
            transform.localScale = scale;
        }
    }
}
