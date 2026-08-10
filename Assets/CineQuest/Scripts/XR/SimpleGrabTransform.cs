// Cine Quest — Lightweight grab for panels without requiring Meta Interaction SDK at compile time.
// Editor: mouse. Quest: XR grip near panel (UnityEngine.XR.InputDevices). Prefer Meta Interaction Grabbable when available.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CineQuest.XR
{
    public sealed class SimpleGrabTransform : MonoBehaviour
    {
        [SerializeField] bool allowMouseInEditor = true;
        [SerializeField] float controllerGrabRadius = 0.45f;

        Camera _cam;
        bool _mouseGrabbing;
        bool _controllerGrabbing;
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
            if (!_mouseGrabbing)
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
                    _mouseGrabbing = true;
                    _grabDistance = hit.distance;
                    _grabOffset = transform.position - hit.point;
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
                _mouseGrabbing = false;

            if (_mouseGrabbing && mouse.leftButton.isPressed)
            {
                var ray = _cam.ScreenPointToRay(mouse.position.ReadValue());
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    _grabDistance = Mathf.Clamp(_grabDistance - scroll * 0.05f, 0.3f, 8f);

                transform.position = ray.GetPoint(_grabDistance) + _grabOffset;

                if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    transform.Rotate(Vector3.up, -delta.x * 0.2f, Space.World);
                    transform.Rotate(_cam.transform.right, delta.y * 0.2f, Space.World);
                }
            }

            if (Keyboard.current != null && _mouseGrabbing)
            {
                if (Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed)
                    transform.localScale *= 1f + Time.deltaTime;
                if (Keyboard.current.minusKey.isPressed || Keyboard.current.numpadMinusKey.isPressed)
                    transform.localScale *= 1f - Time.deltaTime;
            }
        }

        void UpdateControllerGrab()
        {
            // Unity XR subsystem (works without Meta Interaction SDK).
            var rightDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);
            bool anyGrip = false;

            foreach (var device in rightDevices)
            {
                if (!device.isValid) continue;
                if (!device.TryGetFeatureValue(CommonUsages.gripButton, out bool grip) || !grip)
                    continue;

                anyGrip = true;
                if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out var pos))
                    continue;

                float radius = controllerGrabRadius * Mathf.Max(1f, transform.localScale.magnitude);
                if (!_controllerGrabbing)
                {
                    if (Vector3.Distance(pos, transform.position) <= radius)
                    {
                        _controllerGrabbing = true;
                        _grabOffset = transform.position - pos;
                    }
                }

                if (_controllerGrabbing)
                {
                    transform.position = pos + _grabOffset;
                    var head = _cam != null ? _cam.transform : Camera.main != null ? Camera.main.transform : null;
                    if (head != null)
                    {
                        var look = transform.position - head.position;
                        if (look.sqrMagnitude > 0.001f)
                            transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
                    }
                }
            }

            if (!anyGrip)
                _controllerGrabbing = false;
        }
    }
}
