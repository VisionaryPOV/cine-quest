# Cine Quest

**Professional video monitoring & QC for Directors of Photography and lighting teams on Meta Quest 3 / Quest 3S.**

Cine Quest solves a specific on-set problem: Meta’s HDMI Link (and similar apps) apply **automatic brightness, contrast, and image enhancements**, making true evaluation of iris, lighting intensity, and filters impossible. Cine Quest displays a UVC capture feed with a **locked, non-adaptive** image path and professional video scopes.

> **Priority zero: signal fidelity and low latency.**  
> Quest is **not** a calibrated reference monitor — use Cine Quest for reliable *relative* judgment on set.

---

## Features

### Accurate, locked video display
- Live UVC capture → floating, grab-able, resizable world panel
- Optional **Theater / Cinema** mode (dark environment)
- Default **Passthrough MR** so you can see the set and lights
- Custom **Unlit** shader only (`CineQuest/LockedVideo`) — no tonemap / AE / bloom on the video
- User controls: brightness/gain, contrast, gamma, saturation, temperature/tint, lift
- **Lock** freezes parameters · **Bypass / Reference** forces identity
- Rec.709 **Limited (16–235)** default · Full range toggle
- Layout + locked settings persistence

### Real-time scopes (compute shaders)
- **Waveform** (Rec.709 luma) with IRE graticule / legal lines  
- **RGB Parade**  
- **Vectorscope** (Cb/Cr) with skin-tone line + 75%/100% targets  
- **Histogram** (assist)  
- Independent show/hide, move, opacity · freeze-frame · quality modes  

### On-set UI
- Dark high-contrast controls · presets (Neutral Lock, Iris Evaluation, Lighting Balance, Skin Tone Check, Reference Bypass)
- Status HUD: resolution, FPS, USB speed, latency estimate, format, battery, HDCP/USB warnings
- Keyboard shortcuts in Editor: `M` menu · `L` lock · `B` bypass · `T` theater · `F` freeze  
- Voice command **stub** ready for Meta Voice SDK  

### Stretch included
- False-color + zebra overlay shader  
- Synthetic test patterns (bars, ramp, 18% gray, skin chip, pulse)  

---

## Unity version & packages

| | |
|--|--|
| **Unity** | **6000.0.60f1** (Unity 6 LTS) recommended |
| **Pipeline** | URP |
| **XR** | OpenXR + **Meta XR All-in-One** (`com.meta.xr.sdk.all`) for device |
| **Input** | Input System + XR Interaction Toolkit |
| **UVC** | [UVC4UnityAndroid](https://github.com/saki4510t/UVC4UnityAndroid) (default) · optional [usb-video](https://github.com/facebookexperimental/usb-video) bridge |

Package pins live in `Packages/manifest.json`. Meta scoped registry is pre-declared.

---

## Monday first Quest test

Single-session on-device plan: **[Docs/MONDAY_FIRST_TEST.md](Docs/MONDAY_FIRST_TEST.md)**  
(sideload → live UVC → Bypass/iris fidelity → scopes → layout save/load)

## Pure logic tests (no headset / no Unity Editor)

```bash
# Requires .NET 8 SDK
dotnet run --project Tools/PureTests
# or:
./Tools/run_pure_tests.sh
```

These tests compile and execute the **same** sources under `Assets/CineQuest/Scripts/Core/**` (lock/bypass, presets, layout JSON round-trip, scope quality policy).

Inventory / fidelity static checks:

```bash
python3 Tools/verify_inventory.py
python3 Tools/verify_fidelity_static.py
```

## Quick start (Editor)

1. Open this folder in Unity Hub with Unity 6.
2. Let packages resolve (add Meta XR packages for device work).
3. Open `Assets/CineQuest/Scenes/Main_CineQuest.unity`  
   — or use menu **Cine Quest → Create Main Scene With Bootstrap**.
4. Press **Play**.  
   `RuntimeSceneBuilder` constructs capture, video panel, scopes, menu, and HUD.  
   Editor uses **synthetic color bars** by default (no capture card required).
5. Grab the panel (click-drag), `R`+drag to rotate, scroll distance, `+/-` scale.  
6. Use menu buttons: Lock, Bypass, scopes, Theater, presets.

### Keyboard

| Key | Action |
|-----|--------|
| M | Toggle menu |
| L | Toggle lock |
| B | Toggle bypass |
| T | Theater / Passthrough |
| F | Freeze frame |
| H | Toggle status HUD |
| S | Save layout |
| O | Load layout |
| 1–5 | Synthetic patterns (bars / ramp / 18% / pulse / skin) |

### Editor menu

- **Cine Quest → Setup Wizard…** — checklist + define helper  
- **Cine Quest → Create Main Scene With Bootstrap**  
- **Cine Quest → Validate Fidelity Settings**

---

## Project structure

```
Assets/CineQuest/
  Scripts/
    App/           Bootstrap, app policy
    Capture/       IVideoCaptureSource + UVC adapters + synthetic
    Video/         Locked display, freeze, theater, false color
    Scopes/        Waveform, parade, vectorscope, histogram
    UI/            Menu, HUD, voice stub
    XR/            Grab + actions
    Audio/         UAC playback
    Persistence/   Layouts + presets
    Diagnostics/   Fidelity report
    Editor/        Menu tools
  Shaders/         LockedVideo, scopes compute + viz, false color
  Resources/       Presets + compute copies for runtime load
  Scenes/          Main_CineQuest
Assets/Plugins/Android/   Manifest + USB filters
Docs/                     Build, UVC, fidelity, testing
ThirdParty/               UVC drop-in instructions
```

---

## UVC capture integration

**Full guide:** [Docs/UVC_INTEGRATION.md](Docs/UVC_INTEGRATION.md)

Summary:

1. Import **UVC4UnityAndroid** `.unitypackage`.
2. Android Player: IL2CPP, ARM64, OpenGLES3.
3. Add define `CINE_QUEST_UVC4UNITY`.
4. Prefer raw YUV/MJPEG; disable UVC auto PU controls if exposed.
5. Optional: implement `UsbVideoNativeCaptureSource` for Meta’s usb-video AAR.

Hardware: any **UVC (+ UAC)** HDMI/DP capture card · **USB 3 SuperSpeed** recommended · 1080p60 preferred.

---

## Build & sideload

**Full guide:** [Docs/BUILD_AND_DEPLOY.md](Docs/BUILD_AND_DEPLOY.md)

```bash
# After building CineQuest.apk from Unity:
adb install -r CineQuest.apk
adb shell am start -n com.cinequest.monitor/com.unity3d.player.UnityPlayerActivity
```

Also supported: **Meta Quest Developer Hub** drag-and-drop install.

App Lab / Horizon Store: package id `com.cinequest.monitor`, VR category + supportedDevices metadata already in the manifest template.

---

## Disable system adaptive brightness / CABC

Even with a perfect app path, Horizon OS display adaptation can affect the panel.

1. Set **manual** brightness.
2. Turn **off** Adaptive / Auto brightness.
3. Disable any system image enhancement options.
4. Prefer **Theater** mode for critical looks.

Details: [Docs/SIGNAL_FIDELITY.md](Docs/SIGNAL_FIDELITY.md)

---

## Known limitations

| Limitation | Notes |
|------------|--------|
| Not a reference monitor | Headset optics/compositor ≠ calibrated grade monitor |
| ~1080p practical max | Higher only if card + USB bandwidth allow |
| HDCP | Protected sources may blank; warning when detected |
| USB2 | Hi-Speed often insufficient for clean 1080p60 |
| Audio latency | UAC via Unity is reference-only |
| Meta XR binaries | Installed via Package Manager (not vendored here) |
| UVC plugin binaries | Import separately (license/size) |

---

## Fidelity testing

**Checklist:** [Docs/TESTING_CHECKLIST.md](Docs/TESTING_CHECKLIST.md)

Minimum:

1. Bypass ON, waveform ON.  
2. Change iris or light intensity on a live camera.  
3. Image **and** waveform must move **immediately** with **no** auto settle-back.

Editor: set synthetic pattern **CheckerPulse** — brightness must keep pulsing under Lock.

In play mode: add `FidelityDiagnostics` or menu **Cine Quest → Validate Fidelity Settings**.

---

## Shader & scope notes

### LockedVideo (`CineQuest/LockedVideo`)
- Optional limited-range expand  
- Lift → brightness → contrast → saturation → temp/tint → gamma  
- `_Bypass=1` skips creative grade  

### Scopes
Compute shaders accumulate every N frames from a downsampled analysis RT so the **main video path never does CPU readback**.

Quality modes: High / Balanced / Performance (drop scope rate first).

---

## License & third party

- Cine Quest project code: provided for your production use in this workspace.  
- UVC4UnityAndroid: Apache-2.0 (upstream).  
- facebookexperimental/usb-video: Apache-2.0 (upstream).  
- Meta XR SDK: Meta license via Package Manager / Asset Store.  

---

## Generation map (implemented)

1. ✅ Project scaffold, AndroidManifest, packages, capture abstraction + synthetic + UVC adapters  
2. ✅ Locked video pipeline, parameters, Lock/Bypass, panel, theater, freeze  
3. ✅ Waveform / Parade / Vectorscope compute + viz, histogram, quality modes  
4. ✅ UI menu, presets, layouts, HUD, false color, audio hook, voice stub  
5. ✅ Docs: README, build/deploy, UVC, fidelity, testing checklist  

---

## Support path for on-set issues

1. Confirm Bypass + manual system brightness.  
2. Check HUD USB speed and FPS.  
3. Run fidelity checklist §C.  
4. Capture `adb logcat` with tag `CineQuest` / `Unity`.  

Built for DPs who need the signal to tell the truth.
