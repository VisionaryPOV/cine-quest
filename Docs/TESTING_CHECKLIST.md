# Testing Checklist — Cine Quest

Use this before trusting the app on a real set (e.g. network episodic).

## A. Build & launch

- [ ] APK installs on Quest 3 / 3S
- [ ] App launches into Passthrough (or clear MR path after Meta XR setup)
- [ ] Hand tracking and/or controllers can interact with UI
- [ ] No crash on cold start without USB device (synthetic or clear “No Device” state)

## B. Capture path

- [ ] UVC capture card enumerated; USB permission accepted
- [ ] 1080p stream preferred; resolution shown on HUD
- [ ] SuperSpeed vs Hi-Speed indicated; Hi-Speed warns at low FPS
- [ ] Hot-unplug shows Signal Lost / No Device messaging
- [ ] Re-plug recovers without restart (reconnect watchdog)
- [ ] HDCP-encrypted source: blank + clear warning (if available to test)
- [ ] UAC audio (if card supports): unmute plays sound (latency acceptable for ref only)

## C. Signal fidelity (critical)

**Setup:** Stable chart or gray card under controllable light. Bypass ON. Waveform ON.

- [ ] Image does **not** slowly auto-lift or auto-crush when nothing in scene changes
- [ ] **Iris open**: image and waveform luma rise immediately and stay put
- [ ] **Iris close**: image and waveform fall immediately and stay put
- [ ] **Light intensity up/down**: same — no delayed compensation
- [ ] **ND / filter change**: expected exposure step; no auto white-hunt if Bypass
- [ ] Lock ON with non-default grade: parameters frozen; scene changes still visible through fixed grade
- [ ] Switching Bypass ON/OFF only changes grade, not “tracking” behavior
- [ ] URP post-processing disabled on XR camera (`FidelityDiagnostics` clean)

**Editor synthetic stress test:** Pattern = CheckerPulse (0.5 Hz brightness pulse). With Lock ON, pulse must remain clearly visible — proves no AGC.

## D. Color / range

- [ ] Limited vs Full Range toggle changes blacks/near-black presentation as expected
- [ ] Color bars (synthetic or generator): primaries distinct; no weird invert
- [ ] Skin-tone chip (synthetic): vectorscope energy near skin line region

## E. Scopes

- [ ] Waveform: IRE graticule visible; legal lines; density tracks exposure
- [ ] RGB Parade: three channels; channel balance visible on white/gray
- [ ] Vectorscope: center at gray; primaries land near targets; skin line drawn
- [ ] Histogram (if enabled): channels respond to exposure
- [ ] Multiple scopes simultaneous without video dropping below usable rate
- [ ] Quality mode Performance recovers headroom under load
- [ ] Freeze frame freezes display analysis source for scopes
- [ ] Scope panels movable; opacity control works

## F. UI / UX

- [ ] Menu: Lock, Bypass, sliders (when unlocked), presets
- [ ] Presets: Neutral Lock, Iris Evaluation, Lighting Balance, Skin Tone Check, Reference Bypass
- [ ] Save layout → quit → relaunch → Load layout restores poses/params
- [ ] Theater mode darkens environment and enlarges panel
- [ ] Status HUD: resolution, FPS, USB, latency estimate, battery, warnings
- [ ] False color / zebra overlay optional and off by default

## G. Performance

- [ ] Maintains headset refresh under Passthrough + main video
- [ ] Scopes do not freeze main video when GPU-bound
- [ ] Thermal: 10–15 min continuous 1080p monitoring acceptable

## H. Known limitations acknowledged

- [ ] Team understands Quest is **not** a calibrated reference monitor
- [ ] System adaptive brightness disabled per SIGNAL_FIDELITY.md
- [ ] Practical max often 1080p; higher only if card + USB allow
- [ ] HDCP may prevent any image from protected devices

## Sign-off

| Role | Name | Date | Pass? |
|------|------|------|-------|
| Engineering | | | |
| DIT / Video | | | |
| DP (optional) | | | |

Notes:

_________________________________________________________________

_________________________________________________________________
