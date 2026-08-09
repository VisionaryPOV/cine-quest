# UVC Integration Guide — Cine Quest

Cine Quest never depends on Android Camera2 auto-exposure / AWB for monitoring. Capture backends implement `IVideoCaptureSource` and hand a `Texture` to the **LockedVideo** shader path only.

## Architecture

```
Capture card (HDMI/DP → UVC)
        │
        ▼
IVideoCaptureSource  ──► CaptureService ──► LockedVideoRenderer
        │                                        │
        │                                        ▼
        │                               CineQuest/LockedVideo (Unlit)
        │
        └──► ScopeManager (downsampled analysis RT)
```

### Backends

| Class | When |
|-------|------|
| `EditorSyntheticCaptureSource` | Unity Editor / no device |
| `Uvc4UnityCaptureSource` | Device default (UVC4UnityAndroid) |
| `UsbVideoNativeCaptureSource` | Optional Meta usb-video AAR |

## UVC4UnityAndroid (default production path)

### Install

1. Open the project in **Unity 6000.0.60f1** (or compatible Unity 6 LTS).
2. Import UVC4UnityAndroid release package (Apache-2.0).
3. Player Settings → Android:
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64**
   - Graphics APIs: **OpenGLES3** first (remove Vulkan until plugin confirms)
   - Minimum API: **32** (Quest 3 / Horizon OS)
4. Scripting Define Symbols (Android): add `CINE_QUEST_UVC4UNITY`
5. Confirm `Assets/Plugins/Android/AndroidManifest.xml` merges with plugin USB permission activity.

### Prefer raw formats

When the plugin UI or API allows format selection:

1. Prefer **YUY2 / UYVY / NV12** over H.264 when bandwidth allows.
2. MJPEG is acceptable if uncompressed overruns USB2.
3. Disable any UVC Processing Unit (brightness/contrast/AWB) controls if exposed via `GetCtrls` / `SetValue`.

### Binding the texture

`Uvc4UnityCaptureSource` tries reflection against `UVCManager`. If your package version uses different type names:

```csharp
// From a MonoBehaviour that owns the plugin texture:
var uvc = CaptureService.Instance.Source as Uvc4UnityCaptureSource;
uvc?.InjectFrame(myTexture, statusStruct);
```

## facebookexperimental/usb-video bridge (optional)

Repo: https://github.com/facebookexperimental/usb-video

### Why it is not the default

- Pure Android Gradle project (sample app + library).
- No official Unity package or UPM module.
- Requires custom AAR + SurfaceTexture/OES external texture pipeline.

### Integration checklist

1. Clone and build library modules in Android Studio → produce `usb-video.aar` (+ dependencies).
2. Copy AAR to `Assets/Plugins/Android/`.
3. Add Java/Kotlin proxy class implementing a Unity-send message or AndroidJavaProxy callback with:
   - GLES texture id
   - width / height
   - timestamp
4. In `UsbVideoNativeCaptureSource.OnNativeFrame`:
   - `Texture2D.CreateExternalTexture` (or update existing)
   - Raise `Events.RaiseFrame`
5. Audio: map UAC PCM into `AudioClip` or native OpenSL → Unity.
6. Set `CaptureService` backend to `UsbVideoNative`.

## Permissions & USB attach

Manifest already includes:

- `CAMERA`, `RECORD_AUDIO`
- `android.hardware.usb.host`
- USB device attach intent + `usb_device_filter.xml` (UVC class 14 / UAC class 1)

On first plug-in, the user must accept the USB permission dialog. Some Horizon OS versions changed camera/USB permission UX — if the device lists but never streams, re-check OS release notes and re-grant USB access.

## Recommended capture hardware

- UVC + UAC HDMI (or DisplayPort) capture card
- **USB 3.0 SuperSpeed** card + SuperSpeed cable
- 1080p60 preferred (practical target on Quest)
- Powered hub if the card is bus-power hungry

## Audio (UAC)

`CaptureAudioPlayer` starts muted by default. Unity audio is **not** frame-locked to video; expect residual delay. Use for reference tone only, not critical A/V sync decisions.

## Troubleshooting

| Symptom | Action |
|---------|--------|
| No device | USB permission; try different cable/port; SuperSpeed recommended |
| Low FPS at 1080p | USB2 path — use SuperSpeed card/cable; try MJPEG |
| Black image | HDCP on source; or wrong format; check plugin logs |
| Editor black | Expected without synthetic — CaptureService uses synthetic in Editor by default |
| Auto brightness still visible | System CABC — see SIGNAL_FIDELITY.md (not app shader) |
