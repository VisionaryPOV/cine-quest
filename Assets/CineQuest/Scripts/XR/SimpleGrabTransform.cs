// Cine Quest — Lightweight grab for panels without requiring Meta Interaction SDK at compile time.
// Works with mouse in Editor and XR controllers via Unity Input System when available.
// When Meta Interaction SDK / XRI Grabbable is present, prefer those components on the same object.

using UnityEngine;
using UnityEngine.InputSystem;

namespace CineQuest.XR
{
    public sealed class SimpleGrabTransform : MonoBehaviour
    {
        [SerializeField] float mouseDistance = 2f;
        [SerializeField] bool allowMouseInEditor = true;
        [SerializeField] bool freezeInTheater = true;

        Camera _cam;
        bool _grabbing;
        float _grabDistance;
        Vector3 _grabOffset;

        void Start()
        {
            _cam = Camera.main;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (allowMouseInEditor)
                UpdateMouseGrab();
#endif
            // Optional: XR controller grip via Input System
            UpdateControllerGrab();
        }

        void UpdateMouseGrab()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                var ray = _cam.ScreenPointToRay(mouse.position.ReadValue());
                if (Physics.Raycast(ray, out var hit, 10f) && hit.transform == transform)
                {
                    _grabbing = true;
                    _grabDistance = hit.distance;
                    _grabOffset = transform.position - hit.point;
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
                _grabbing = false;

            if (_grabbing && mouse.leftButton.isPressed)
            {
                var ray = _cam.ScreenPointToRay(mouse.position.ReadValue());
                // Scroll to change distance
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    _grabDistance = Mathf.Clamp(_grabDistance - scroll * 0.05f, 0.3f, 8f);

                var point = ray.GetPoint(_grabDistance);
                transform.position = point + _grabOffset;

                // Right-drag style rotate with R held
                if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    transform.Rotate(Vector3.up, -delta.x * 0.2f, Space.World);
                    transform.Rotate(_cam.transform.right, delta.y * 0.2f, Space.World);
                }
            }

            // Scale with keyboard
            if (Keyboard.current != null && _grabbing)
            {
                if (Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed)
                    transform.localScale *= 1f + Time.deltaTime;
                if (Keyboard.current.minusKey.isPressed || Keyboard.current.numpadMinusKey.isPressed)
                    transform.localScale *= 1f - Time.deltaTime;
            }
        }

        void UpdateControllerGrab()
        {
            // Placeholder for grip-based grab using XR Input Devices.
            // Full hand tracking should use Meta Interaction SDK Grabbable / XRI.
        }
    }
}
