// Cine Quest — High-level XR / keyboard actions for menu, freeze, lock, theater.
// Hand tracking first-class: Meta Interaction SDK buttons should call the same public methods.

using CineQuest.UI;
using CineQuest.Video;
using UnityEngine;
using UnityEngine.InputSystem;

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

        void Start()
        {
            AutoBind();
        }

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
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.mKey.wasPressedThisFrame) menu?.ToggleMenu();
            if (kb.lKey.wasPressedThisFrame && imageParams != null)
                imageParams.SetLocked(!imageParams.IsLocked);
            if (kb.bKey.wasPressedThisFrame && imageParams != null)
                imageParams.SetBypass(!imageParams.IsBypass);
            if (kb.tKey.wasPressedThisFrame) theater?.Toggle();
            if (kb.fKey.wasPressedThisFrame) freeze?.Toggle();
            if (kb.hKey.wasPressedThisFrame)
            {
                hudVisible = !hudVisible;
                hud?.SetVisible(hudVisible);
            }
            if (kb.sKey.wasPressedThisFrame) menu?.SaveLayout();
            if (kb.oKey.wasPressedThisFrame) menu?.LoadLayout(); // O = open/load layout
        }

        // --- Methods for Meta Interaction SDK / UI buttons ---

        public void Action_ToggleMenu() => menu?.ToggleMenu();
        public void Action_ToggleLock()
        {
            if (imageParams != null) imageParams.SetLocked(!imageParams.IsLocked);
        }
        public void Action_ToggleBypass()
        {
            if (imageParams != null) imageParams.SetBypass(!imageParams.IsBypass);
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
