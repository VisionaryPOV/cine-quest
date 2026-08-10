# Cine Quest Architecture

## Design principles

1. **Signal fidelity first** — never auto-grade the monitoring path.
2. **Low latency** — main video is texture → Unlit material; scopes never block that path.
3. **Backend swappable** — `IVideoCaptureSource` isolates UVC plugins.
4. **Runtime bootstrap** — `RuntimeSceneBuilder` builds a working hierarchy without hand-authored prefab graphs.

## Data flow

```
UVC / Synthetic / Native bridge
        │
        ▼
 CaptureService  ──────────────────────────────┐
        │                                       │
        │ CurrentFrame (Texture)                │ CaptureStatus
        ▼                                       ▼
 LockedVideoRenderer                    StatusHud / VideoStatusOverlay
        │
        │ CineQuest/LockedVideo (user params or Bypass)
        ▼
 VideoPanel (world quad, grab / theater)
        │
        │ (optional freeze RT)
        ▼
 ScopeManager → downsample RT → compute scopes → scope panels
```

## Module map

| Namespace | Responsibility |
|-----------|----------------|
| `CineQuest.Capture` | Device I/O, status, backends |
| `CineQuest.Video` | Locked display, freeze, theater, false color, overlays |
| `CineQuest.Scopes` | Waveform, parade, vectorscope, histogram |
| `CineQuest.UI` | Menu, HUD, runtime UI factory, voice stub |
| `CineQuest.XR` | Grab + high-level actions |
| `CineQuest.Audio` | UAC playback |
| `CineQuest.Persistence` | Layout JSON + presets |
| `CineQuest.App` | Policy, bootstrap |
| `CineQuest.Diagnostics` | Fidelity report, pulse patterns |

## Shader contracts

### `CineQuest/LockedVideo`
Properties: `_MainTex`, `_Bypass`, `_LimitedRange`, `_Brightness`, `_Contrast`, `_Gamma`, `_Saturation`, `_Temperature`, `_Tint`, `_Lift`, `_Opacity`, `_FlipY`.

### Scope compute
Kernels: `Clear`, `Accumulate`, `Resolve`.  
Buffers: `_Bins` (uint), output `RWTexture2D`.  
Source: analysis `RenderTexture` via `.Load`.

## Persistence

`Application.persistentDataPath/cinequest_layout.json` via `LayoutStore` / `CineQuest.Core.LayoutSerializer` (pure JSON round-trip tested).

## Extension points

| Goal | Where |
|------|--------|
| New capture backend | Implement `IVideoCaptureSource`, select in `CaptureService` |
| Meta Interaction grab | Add Grabbable on panel; keep `SimpleGrabTransform` as Editor fallback |
| Voice | Wire Meta Voice → `VoiceCommandStub` methods |
| usb-video AAR | Fill `UsbVideoNativeCaptureSource.OnNativeFrame` |

## Threading / performance

- Main video: every frame, GPU only.
- Scopes: rate-limited by `ScopeQualityMode`; downsample before compute.
- Histogram: CPU readback on tiny RT only (assist tool).
