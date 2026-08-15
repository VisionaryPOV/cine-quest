# Signal Fidelity Policy — Cine Quest

Cine Quest exists because **Meta Quest HDMI Link** (and similar viewers) apply automatic brightness, contrast, and image enhancements. Those make iris pulls and lighting changes impossible to evaluate truthfully.

## What Cine Quest guarantees (in-app)

1. **No automatic response to scene content** in the video path.
2. Video is drawn with **`CineQuest/LockedVideo`** (URP Unlit custom HLSL only).
3. **No** post-processing volumes, auto-exposure, tonemapping, bloom, or filmic curves on the monitoring image.
4. User parameters (brightness, contrast, gamma, saturation, temperature, tint, lift) apply **only** when Bypass is off, and only as explicit math.
5. **Lock** freezes parameter values so they cannot drift while you work.
6. **Bypass / Reference Mode** forces identity transforms (optional Rec.709 limited-range expand only).

### Contrast formula (explicit)

```
color = (color - 0.5) * contrast + 0.5
```

### Color range

| Mode | Behavior |
|------|----------|
| Rec.709 Limited (default) | Expand 16–235-style limited range toward full for display |
| Full Range | No expand |

## What Cine Quest cannot fully control

### 1. Quest display is not a calibrated reference monitor

OLED/LCD headset panels, optics, and compositor color management mean **absolute** color/luminance judgment is not broadcast-legal. Use Cine Quest for **relative** evaluation: iris/light changes should move the image immediately and consistently.

### 2. System adaptive brightness / CABC

Horizon OS may still apply system-level brightness adaptation or content-adaptive backlight (CABC-like behavior) to the **panel**. That is outside the Unity render target.

**User steps to maximize consistency** (menus vary by OS version):

1. Open **Quick Settings** / **Settings → Display**.
2. Set brightness **manually** to a fixed comfortable level.
3. Disable **Adaptive Brightness** / **Auto Brightness** if present.
4. Disable any **Extra image clarity / enhancement / dynamic contrast** options if present.
5. Prefer **Theater** environment in Cine Quest (dark surroundings) to reduce pupil adaptation and passthrough glare when critically evaluating.
6. Keep headset firmware updated; re-check settings after OS updates.

Document the OS build you tested on for each production show.

### 3. Capture card / HDMI processing

Some capture cards re-time, scale, or convert color. Prefer cards that advertise clean pass-through at 1080p60. **HDCP-protected HDMI often blanks at the capture card. Cine Quest cannot read HDCP status** and will not show a dedicated “HDCP detected” flag — a protected source looks like black / no signal.

### 4. Decode path

UVC MJPEG/H.264 decode is lossy. Prefer uncompressed or lightly compressed UVC formats when USB3 bandwidth allows.

## Lock vs Bypass

| Mode | Use when |
|------|----------|
| **Unlocked** | Dialing in a monitoring look (rare on set once locked) |
| **Locked** | Operating on set — parameters frozen; scene changes remain visible |
| **Bypass** | Closest 1:1; ignore creative grade entirely |

## Scopes vs display grade

Waveform / parade / vectorscope sample the **capture analysis RT** (pre–LockedVideo creative grade). That matches engineering scopes: they show signal, not your monitoring look. Limited-range expand applies on the **display** path by default; scopes see the raw decoded texture unless you pre-process the analysis RT.

## Verification procedure

See [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md). Minimum bar:

1. Enable Bypass.
2. Point camera at a constant chart.
3. Change iris or dim a light **in the real world**.
3. Confirm waveform and image **both** move immediately.
4. Confirm no delayed “auto settle” of brightness toward a previous look.
