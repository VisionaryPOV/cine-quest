# Build & Deploy — Cine Quest (Meta Quest 3 / 3S)

## Requirements

| Item | Version / notes |
|------|-----------------|
| Unity | **6000.0.60f1** LTS recommended (Unity 6) |
| Modules | Android Build Support, OpenJDK, Android SDK & NDK (Unity Hub) |
| Headset | Meta Quest 3 or Quest 3S, **Developer Mode** enabled |
| Host OS | macOS / Windows / Linux with USB |
| Tools | Android platform-tools (`adb`), optional **Meta Quest Developer Hub (MQDH)** |

### Unity packages (see `Packages/manifest.json`)

- Universal RP
- XR Plugin Management + OpenXR
- Input System
- XR Interaction Toolkit
- TextMeshPro / uGUI

### Install Meta XR (required for production passthrough + hands)

1. Enable scoped registry **Meta** (`https://npm.developer.oculus.com`, scope `com.meta.xr`) — already listed in `manifest.json`.
2. Package Manager → add:
   - `com.meta.xr.sdk.all` **or** Core + Interaction + Audio
3. Project Settings → XR Plug-in Management → Android → enable **OpenXR** + Meta feature group.
4. Enable **Passthrough**, **Hand Tracking** in Meta / OpenXR feature lists.
5. Use Meta **Building Blocks** or Interaction SDK samples to add:
   - Camera Rig / XR Origin
   - Passthrough layer
   - Hand / controller interactors  
   Then parent or replace the simple camera created by `RuntimeSceneBuilder` for device builds.

### UVC plugin

See [UVC_INTEGRATION.md](UVC_INTEGRATION.md).

---

## Player Settings checklist (Android)

| Setting | Value |
|---------|--------|
| Company Name | CineQuest (or yours) |
| Product Name | Cine Quest |
| Package Name | `com.cinequest.monitor` |
| Minimum API Level | **32** |
| Target API Level | **34** (or Meta’s current requirement) |
| Scripting Backend | **IL2CPP** |
| Target Architectures | **ARM64** only |
| Graphics APIs | **OpenGLES3** (primary) |
| Multithreaded Rendering | On (validate external textures) |
| Color Space | Linear |
| Orientation | Landscape |
| Custom Main Manifest | **Enabled** (uses `Assets/Plugins/Android/AndroidManifest.xml`) |
| Stereo Rendering | Multiview / Single Pass Instanced as Meta recommends |

URP asset:

- Disable unnecessary post-processing on the XR camera.
- `CineQuestApp` forces `renderPostProcessing = false` on cameras at runtime.

---

## Build APK (Editor)

1. File → Build Settings → **Android** → Switch Platform.
2. Add scene `Assets/CineQuest/Scenes/Main_CineQuest.unity`.
3. Run Device → Development Build recommended for first bring-up.
4. **Build** (or Build And Run with headset connected).
5. Output: `CineQuest.apk`.

### Command-line (optional)

```bash
# Example — adjust UNITY_PATH
"$UNITY_PATH" -quit -batchmode -projectPath "/path/to/Cine Quest" \
  -buildTarget Android \
  -executeMethod # use your CI build method if added
```

---

## Sideload with ADB

```bash
# Discover device
adb devices

# Install (replace path)
adb install -r "/path/to/CineQuest.apk"

# Launch
adb shell am start -n com.cinequest.monitor/com.unity3d.player.UnityPlayerActivity

# Logcat (filter)
adb logcat -s Unity ActivityManager USB

# Uninstall
adb uninstall com.cinequest.monitor
```

### Wireless ADB (optional)

```bash
adb tcpip 5555
adb connect <headset-ip>:5555
```

---

## Meta Quest Developer Hub (MQDH)

1. Install MQDH from Meta.
2. Connect Quest → enable file access / developer prompts on headset.
3. Use **Build & Run** or drag-drop APK install.
4. Use performance HUD / metrics while testing scopes + 1080p60.

---

## App Lab / Horizon Store readiness

Project is structured for later store submission:

- Unique package id `com.cinequest.monitor`
- Manifest includes `com.oculus.intent.category.VR` and `com.oculus.supportedDevices`
- Hand tracking optional feature flags present

Before store:

1. Create Meta Horizon developer organization + app id.
2. Add Platform SDK / entitlement checks if required.
3. Replace development signing with release keystore.
4. Complete Data Use Checkup, privacy policy, age rating.
5. Capture store screenshots / trailer (no real set confidential material).
6. Submit for App Lab first; promote to Horizon Store when approved.

---

## First-run on headset

1. Accept microphone/camera/USB prompts.
2. Plug UVC capture card (SuperSpeed).
3. Accept USB device permission for Cine Quest.
4. Confirm live image on floating panel.
5. Enable **Bypass / Reference** and perform fidelity checks in [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md).

---

## Common build errors

| Error | Fix |
|-------|-----|
| Manifest merge conflict | Align plugin USB activities; use `tools:replace` carefully |
| Vulkan texture black | Force GLES3 |
| Meta XR missing | Install `com.meta.xr.sdk.*` packages |
| Hands not tracking | Enable hand tracking permission + OpenXR feature |
| USB never attaches | Developer Mode; try `adb shell dumpsys usb` |
