# Monday First-Test Runbook — Cine Quest

**Audience:** you, first Quest 3 / 3S session with a UVC capture card  
**Goal:** prove locked monitoring + scopes on a live set signal in one session  
**Time budget:** ~60–90 minutes on device after packages are already installed on the laptop

---

## Before you leave home (laptop, no headset)

### A. Unity open (once)

1. Open project in **Unity 6000.0.60f1** (or closest Unity 6 LTS).
2. **Cine Quest → Setup Wizard…** — all green checks except UVC define until plugin import.
3. Play `Assets/CineQuest/Scenes/Main_CineQuest.unity`:
   - Synthetic color bars on video panel  
   - Waveform scope visible  
   - **Bypass** / **Lock** buttons work  
   - Key **4** = CheckerPulse still pulses when Locked  
4. Stop Play.

### B. Packages (**blockers** for real Quest use — not optional polish)

1. Package Manager → install **Meta XR All-in-One** (`com.meta.xr.sdk.all`) via Meta scoped registry.
2. XR Plug-in Management → **Android** → OpenXR + Meta feature group.
3. Enable **Passthrough** + **Hand Tracking** features.
4. Add **XR Origin / Camera Rig + ray interactors** (Meta Building Blocks or XRI) so world-space UI is pointable. Bootstrap alone is Editor-first (mouse grab).
5. Import **UVC4UnityAndroid** release `.unitypackage` (r0.5+ / Unity 6).
6. Player Settings → Android → Scripting Define Symbols → add **`CINE_QUEST_UVC4UNITY`**  
   (or Setup Wizard button).
7. Graphics APIs: **OpenGLES3** first (remove Vulkan for first build).
8. IL2CPP, ARM64 only, Min API 32, package `com.cinequest.monitor`.
9. Custom Main Manifest: **enabled**.
10. Prefer wiring UVC frames via `Uvc4UnityCaptureSource.InjectFrame` if reflection does not resolve your plugin version.

### C. Scene for device (quick)

1. Add Meta **Camera Rig / XR Origin + Passthrough** Building Block (or sample rig).
2. Keep `CineQuest_Bootstrap` (`RuntimeSceneBuilder`) in scene **or** wire prefab refs manually.
3. Place plugin `UVCManager` in scene if required by your UVC package version.
4. File → Build Settings → Android → add Main scene → **Build** `CineQuest.apk` (Development Build ON for first day).

### D. Bring to work

- [ ] Quest 3 or 3S, charged, **Developer Mode** on  
- [ ] USB-C data cable (computer ↔ headset)  
- [ ] UVC **USB 3 SuperSpeed** HDMI (or DP) capture card + SuperSpeed cable  
- [ ] HDMI from camera/monitor out (non-HDCP if possible)  
- [ ] Laptop with `adb` / Meta Quest Developer Hub + the APK  
- [ ] This runbook (printed or phone)

---

## On set — first power-on (ordered)

### 1. Sideload (5 min)

```bash
adb devices
adb install -r /path/to/CineQuest.apk
adb shell am start -n com.cinequest.monitor/com.unity3d.player.UnityPlayerActivity
```

Or drag APK into **Meta Quest Developer Hub**.

### 2. Permissions (2 min)

- Accept microphone / camera / USB prompts.
- Plug capture card → **Allow** USB access for Cine Quest.

### 3. Confirm live path (10 min)

| Check | Pass? |
|-------|-------|
| Floating video shows camera/monitor feed | |
| Status HUD: resolution (e.g. 1920×1080), FPS | |
| USB SuperSpeed preferred; Hi-Speed shows warning | |
| Passthrough: see physical set around panel | |
| Grab / move panel (hands or controllers) | |

If black: see `Docs/UVC_INTEGRATION.md` troubleshooting + HDCP warning on panel.

### 4. Fidelity (critical, 15 min) — **Bypass ON, Waveform ON**

| Action | Expected |
|--------|----------|
| Static scene, no one touching iris/lights | Image and waveform **stable** (no slow auto-lift/crush) |
| Open iris / raise light | Image + waveform **rise immediately** and stay |
| Close iris / dim light | Image + waveform **fall immediately** and stay |
| Lock ON with mild contrast tweak, then change light | Grade fixed; **scene change still visible** |
| Bypass ON again | Closest 1:1; no creative grade |

Also disable headset **adaptive brightness** (manual brightness). See `Docs/SIGNAL_FIDELITY.md`.

### 5. Scopes + UI (10 min)

- [ ] Toggle Parade, Vectorscope, Histogram  
- [ ] Freeze frame freezes analysis  
- [ ] Scope quality button cycles High/Balanced/Performance without killing main video  
- [ ] Preset **Reference Bypass**, **Neutral Lock**, **Iris Evaluation**  
- [ ] **Save layout** → quit app → relaunch → **Load layout**  
- [ ] **Theater** mode darkens / enlarges; back to Passthrough  

### 6. Log failures (if any)

```bash
adb logcat -s Unity CineQuest ActivityManager
```

Capture: OS version, capture card model, resolution/FPS, USB speed, screenshot of HUD.

---

## Done when

- Live UVC image on locked Unlit path with Bypass proven (iris/light test).  
- At least Waveform trustworthy.  
- Layout save/load works.  
- Notes filed for any USB/HDCP/permission issues before next shoot day.

## Do not claim on Monday

- Color-critical mastering accuracy  
- Zero system CABC if OS still adapts panel  
- Universal capture-card compatibility  

---

## Quick links

| Doc | Purpose |
|-----|---------|
| [BUILD_AND_DEPLOY.md](BUILD_AND_DEPLOY.md) | APK / ADB / MQDH / Player Settings |
| [UVC_INTEGRATION.md](UVC_INTEGRATION.md) | UVC4UnityAndroid + usb-video stub |
| [SIGNAL_FIDELITY.md](SIGNAL_FIDELITY.md) | Lock/Bypass policy + CABC |
| [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md) | Full QA matrix |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Module map |
