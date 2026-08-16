// Cine Quest — Short click on Lock / Bypass (Quest controller impulse if available).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace CineQuest.XR
{
    public static class ControllerHaptics
    {
        public static void Click()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller,
                devices);
            foreach (var d in devices)
            {
                if (!d.isValid) continue;
                if (d.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    d.SendHapticImpulse(0, 0.35f, 0.04f);
            }
        }
    }
}
