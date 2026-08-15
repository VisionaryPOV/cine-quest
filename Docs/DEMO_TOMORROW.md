# Demo / debug session agenda (tomorrow)

**Goal:** Show a DP that Cine Quest can use **their existing HDMI → Quest capture gear** and lock the picture so iris/light changes are honest.  
**Budget:** 60–90 minutes.  
**Hardware spend:** $0 (reuse HDMI Link kit).

---

## Before the DP arrives (you)

- [ ] APK built with **Meta XR + UVC4UnityAndroid** (see `BUILD_AND_DEPLOY.md` / `MONDAY_FIRST_TEST.md`)  
- [ ] Sideloaded on demo Quest; appears under **Unknown Sources**  
- [ ] `adb devices` or MQDH works from the Mac  
- [ ] Same capture card + cables the DP uses for HDMI Link on the table  
- [ ] Once: prove the chain in **HDMI Link**, then **fully quit HDMI Link**  
- [ ] Printed or phone copies of this doc + `DP_EXISTING_HDMI_GEAR.md`  

If APK is not ready: still run **Phase 1 synthetic** and explain live path status honestly.

---

## Controller map (no menu required)

| Control | Action |
|---------|--------|
| **A** (right primary) | Toggle **Bypass** |
| **B** (right secondary) | Toggle **Lock** |
| **Menu / Y** | Show/hide operator sheet |
| **Stick click** | Theater / Passthrough |
| **Left grip + trigger** | Freeze |

App boots in **Reference Bypass**, menu **hidden**. HUD says **SYNTHETIC — NOT CAMERA** if the APK is not on a live card.

**Do not** use Iris Evaluation / Lighting Balance / Skin Tone for the trust test. Use **REF BYPASS** only. Waveform IRE numbers are **not** legal; judge motion, not the 100 line.

**Do not invite the DP** until you have: stereo tracking + bars + **A toggles Bypass** on the headset.

---

## Talk track (30 seconds)

> “Quest doesn’t have HDMI in. HDMI Link uses a USB capture dongle and then **auto-enhances** the image, so you can’t trust iris and light moves. Cine Quest uses **that same dongle**, but shows a **locked** picture with scopes—no auto brightness or contrast.”

---

## Phase 0 — Hardware sanity (5 min)

1. Camera/monitor → capture card → Quest.  
2. Open **HDMI Link** → confirm picture.  
3. **Force-quit HDMI Link**.  
4. Open **Cine Quest** → allow USB if prompted.

---

## Phase 1 — Product demo without stress (10 min)

Even if live fails later, do this first.

| Step | Show |
|------|------|
| Launch | Passthrough + floating panel |
| Synthetic / bars | App is alive |
| **Bypass** | Reference / identity path |
| **Lock** | Parameters frozen |
| Key **4** / pulse pattern (if synthetic) | Brightness still moves under Lock → no AGC |
| **Waveform** | Professional QC language |
| Presets | Neutral Lock, Iris Evaluation, Reference Bypass |

**Line:** “This is what we lock so the app never rewrites your exposure.”

---

## Phase 2 — Live HDMI (same gear) (15–25 min)

1. Live feed on panel; HUD shows resolution / FPS.  
2. **Bypass ON**, waveform ON.  
3. DP changes **iris** or a **light**.  
4. **Success:** image + waveform track immediately; no delayed “auto fix.”  
5. Optional: Parade / Vectorscope; freeze frame; Theater mode.

---

## Phase 3 — Debug matrix (as needed)

| Symptom | Do this |
|---------|---------|
| No device | USB permission; quit HDMI Link; unplug/replug; restart Cine Quest |
| Black | 1080p out; non-HDCP monitoring out; power on card; try HDMI Link once to isolate |
| Crash | `adb logcat -s Unity` on Mac |
| Can’t click menus | Controllers + ray interactors missing in this build; use preset/hardware buttons if any; note for next build |
| Soft only | Stay on synthetic + story; schedule rebuild with UVC wiring (`UvcFrameInjector`) |

---

## Phase 4 — Capture DP feedback (10 min)

Ask and write down:

1. Would you use this instead of HDMI Link for iris pulls? Why/why not?  
2. Which scopes matter on your show (waveform / parade / vector)?  
3. Must-have: passthrough vs theater?  
4. Deal-breakers (latency, UI, reliability)?  
5. Capture card brand/model (for our compatibility list)?

---

## Success criteria (honest)

| Level | Criteria |
|-------|----------|
| **A — Full win** | Live HDMI from their card + Bypass iris/light test passes |
| **B — Soft win** | Synthetic Lock/Bypass/scopes clear; live blocked only by APK/plugin (path clear for next day) |
| **C — Learn** | Logcat + card model + exact fail mode; still no new hardware required |

---

## What not to promise tomorrow

- Color-critical grading / legal delivery on Quest  
- Zero system adaptive brightness (tell them to set **manual** headset brightness)  
- Every capture card without testing  

---

## Quick commands (Mac)

```bash
adb devices
adb install -r /path/to/CineQuest.apk
adb shell am start -n com.cinequest.monitor/com.unity3d.player.UnityPlayerActivity
adb logcat -s Unity
```

Repo: https://github.com/VisionaryPOV/cine-quest  
Install guide (Desktop): `How to Install CineQuest.md`  
Same-gear SOP: `Docs/DP_EXISTING_HDMI_GEAR.md`
