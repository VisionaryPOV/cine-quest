# Using the DP’s existing HDMI gear (no new purchases)

## Bottom line

If the DP already watches camera/monitor **HDMI in the Quest** (Meta **HDMI Link** or similar), they already have everything Cine Quest needs for live video:

| Gear they already use | Role with Cine Quest |
|----------------------|----------------------|
| Quest 3 / 3S | Runs the app |
| USB HDMI (or DP) **capture card** | Same device — UVC |
| HDMI cable from camera/monitor | Same source |
| USB-C cable into Quest | Same link |

**Cine Quest replaces the viewing app, not the hardware.**  
Do **not** buy a second capture card unless the current one fails with *both* HDMI Link and Cine Quest.

---

## How Quest “HDMI in” actually works

Quest has **no native HDMI port**. HDMI Link always uses a **USB Video Class (UVC)** capture dongle.

```
Camera / monitor  --HDMI-->  capture card  --USB-C-->  Quest  -->  App (HDMI Link OR Cine Quest)
```

Only **one app** can own the USB device at a time.

---

## Setup (same as their normal day — then switch the app)

1. **Close Meta HDMI Link** (and any other USB-video app).  
2. Leave the **same** capture card + cables + camera/monitor path connected.  
3. Open **Cine Quest** (Unknown Sources if sideloaded).  
4. When the headset asks **Allow USB access for Cine Quest?** → **Allow**.  
5. Prefer **1080p60** on the camera/monitor output if selectable.  
6. In Cine Quest: **Bypass** (or Neutral Lock) + **Waveform** for the fidelity demo.

### Plug order if rebuilding the chain cold

1. Power the capture card (if it needs external power).  
2. HDMI from **camera monitoring out** (or monitor loop-out) → capture card.  
3. USB-C capture card → Quest.  
4. Launch Cine Quest → accept USB permission.

---

## Fidelity test the DP will care about (2 minutes)

1. **Bypass ON**, waveform ON.  
2. Hold a gray card or point at a steady area.  
3. Change **iris** or **light intensity** on set.  
4. **Pass:** image and waveform jump immediately and **stay** (no slow auto-brighten/crush).  
5. Compare mentally to HDMI Link, which often **re-enhances** after lighting changes.

Honest framing: Quest is **not** a calibrated reference monitor. Cine Quest is for **trustworthy relative** judgment and locking out app auto-processing.

---

## Troubleshooting (still $0)

| Problem | Fix |
|---------|-----|
| No signal / “no device” | Close HDMI Link; replug USB; re-accept USB permission for **Cine Quest** |
| Works in HDMI Link, not Cine Quest | APK missing UVC plugin / define; rebuild with UVC4UnityAndroid + `CINE_QUEST_UVC4UNITY` |
| Black image both apps | Bad cable, no power on card, wrong HDMI source, or **HDCP** on source |
| Low FPS / stutter | USB 2 path — use the SuperSpeed cable/card they already use for best HDMI Link results |
| Image but can’t press UI | Build needs XR ray / Meta Interaction (controllers); still can use Bypass if bound to buttons |

---

## What you do **not** need to buy for first demo

- Second capture card  
- Special “Cine Quest only” cable set (use what works with HDMI Link)  
- External reference monitor  
- New camera  

If their HDMI Link path already works on set, **that path is the demo path**.
