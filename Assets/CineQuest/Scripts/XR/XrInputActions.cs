// Cine Quest — Keyboard (Editor) + Quest controllers (no rays, no Horizon Menu).
// Right A = Bypass · Right B = Lock · Left Y = operator sheet · Left grip+trigger = Freeze · stick click = Theater

using System.Collections.Generic;
using CineQuest.UI;
using CineQuest.Video;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CineQuest.XR
{
    public sealed class XrInputActions : MonoBehaviour
    {
        [SerializeField] MonitorMenuController menu;
        [SerializeField] ImageParameterController imageParams;
        [SerializeField] TheaterModeController theater;
        [SerializeField] FreezeFrameController freeze;
        [SerializeField] StatusHud hud;
        [SerializeField] bool hudVisible = true;

        readonly Dictionary<ulong, ControllerEdges> _edges = new Dictionary<ulong, ControllerEdges>();

        struct ControllerEdges
        {
            public bool primary;
            public bool secondary;
            public bool menu;
            public bool grip2;
            public bool stickClick;
        }

        void Start() => AutoBind();

        public void Bind(
            MonitorMenuController menuCtrl,
            ImageParameterController img,
            TheaterModeController th,
            FreezeFrameController fr,
            StatusHud statusHud)
        {
            if (menuCtrl != null) menu = menuCtrl;
            if (img != null) imageParams = img;
            if (th != null) theater = th;
            if (fr != null) freeze = fr;
            if (statusHud != null) hud = statusHud;
        }

        void AutoBind()
        {
            if (menu == null) menu = FindFirstObjectByType<MonitorMenuController>();
            if (imageParams == null) imageParams = FindFirstObjectByType<ImageParameterController>();
            if (theater == null) theater = FindFirstObjectByType<TheaterModeController>();
            if (freeze == null) freeze = FindFirstObjectByType<FreezeFrameController>();
            if (hud == null) hud = FindFirstObjectByType<StatusHud>();
        }

        void Update()
        {
            PollKeyboard();
            PollControllers();
        }

        void PollKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.mKey.wasPressedThisFrame) Action_ToggleMenu();
            if (kb.lKey.wasPressedThisFrame) Action_ToggleLock();
            if (kb.bKey.wasPressedThisFrame) Action_ToggleBypass();
            if (kb.tKey.wasPressedThisFrame) Action_ToggleTheater();
            if (kb.fKey.wasPressedThisFrame) Action_ToggleFreeze();
            if (kb.hKey.wasPressedThisFrame) Action_ToggleHud();
            if (kb.sKey.wasPressedThisFrame) Action_SaveLayout();
            if (kb.oKey.wasPressedThisFrame) Action_LoadLayout();
        }

        void PollControllers()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);

            foreach (var device in devices)
            {
                if (!device.isValid) continue;
                ulong id = device.deviceId;
                _edges.TryGetValue(id, out var prev);

                bool primary = Feature(device, CommonUsages.primaryButton);
                bool secondary = Feature(device, CommonUsages.secondaryButton);
                bool grip2 = Feature(device, CommonUsages.gripButton) && Feature(device, CommonUsages.triggerButton);
                bool stick = Feature(device, CommonUsages.primary2DAxisClick);
                bool left = device.characteristics.HasFlag(InputDeviceCharacteristics.Left);

                // Never bind Horizon's Menu button (drops the user into OS).
                if (left)
                {
                    if (secondary && !prev.secondary) Action_ToggleMenu(); // Left Y
                    if (grip2 && !prev.grip2) Action_ToggleFreeze();
                }
                else
                {
                    if (primary && !prev.primary) Action_ToggleBypass();   // Right A
                    if (secondary && !prev.secondary) Action_ToggleLock(); // Right B
                    if (stick && !prev.stickClick) Action_ToggleTheater();
                }

                _edges[id] = new ControllerEdges
                {
                    primary = primary,
                    secondary = secondary,
                    grip2 = grip2,
                    stickClick = stick
                };
            }
        }

        static bool Feature(InputDevice device, InputFeatureUsage<bool> usage)
        {
            return device.TryGetFeatureValue(usage, out bool v) && v;
        }

        public void Action_ToggleMenu() => menu?.ToggleMenu();

        public void Action_ToggleLock()
        {
            if (imageParams != null)
            {
                imageParams.SetLocked(!imageParams.IsLocked);
                hud?.SetLockLabel(imageParams.IsLocked, imageParams.IsBypass);
                ControllerHaptics.Click();
            }
        }

        public void Action_ToggleBypass()
        {
            if (imageParams != null)
            {
                imageParams.SetBypass(!imageParams.IsBypass);
                hud?.SetLockLabel(imageParams.IsLocked, imageParams.IsBypass);
                ControllerHaptics.Click();
            }
        }

        public void Action_ToggleTheater() => theater?.Toggle();
        public void Action_ToggleFreeze() => freeze?.Toggle();
        public void Action_SaveLayout() => menu?.SaveLayout();
        public void Action_LoadLayout() => menu?.LoadLayout();

        public void Action_ToggleHud()
        {
            hudVisible = !hudVisible;
            hud?.SetVisible(hudVisible);
        }
    }
}
