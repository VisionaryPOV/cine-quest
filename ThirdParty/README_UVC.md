# Third-party UVC plugins

## Recommended: UVC4UnityAndroid

1. Download the latest release from  
   https://github.com/saki4510t/UVC4UnityAndroid  
   (r0.5.0+ targets Unity 6000.0.x).
2. Import the `.unitypackage` into this project.
3. Prefer **OpenGLES3** Graphics API on Android (Vulkan is experimental in the plugin).
4. Add scripting define: `CINE_QUEST_UVC4UNITY`  
   **Edit → Project Settings → Player → Android → Scripting Define Symbols**
5. Place a `UVCManager` (plugin component) in the scene **or** let `Uvc4UnityCaptureSource` probe for it.
6. Wire any plugin texture callback to `Uvc4UnityCaptureSource.InjectFrame` if automatic reflection binding fails for your version.

## Optional: facebookexperimental/usb-video

This is a native Android app/library, **not** a Unity package:

https://github.com/facebookexperimental/usb-video

To integrate:

1. Build an AAR from the library modules.
2. Place the AAR under `Assets/Plugins/Android/`.
3. Implement JNI / External OES texture → Unity `Texture` in `UsbVideoNativeCaptureSource`.
4. Switch capture backend to `UsbVideoNative` on `CaptureService`.

See **Docs/UVC_INTEGRATION.md** for a full bridge checklist.
