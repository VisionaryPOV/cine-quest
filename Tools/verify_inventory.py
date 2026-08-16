#!/usr/bin/env python3
"""Inventory Cine Quest workspace against Priority-0 / plan acceptance criteria 1–4."""
from __future__ import annotations

import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def exists(rel: str) -> bool:
    return (ROOT / rel).exists()


def read(rel: str) -> str:
    p = ROOT / rel
    return p.read_text(encoding="utf-8", errors="replace") if p.exists() else ""


def main() -> int:
    lines: list[str] = []
    ok = True

    def check(label: str, cond: bool, detail: str = "") -> None:
        nonlocal ok
        status = "OK" if cond else "MISSING"
        if not cond:
            ok = False
        lines.append(f"[{status}] {label}" + (f" — {detail}" if detail else ""))

    lines.append(f"ROOT={ROOT}")
    lines.append("")

    # Criterion 1 — locked video path
    lines.append("== Criterion 1: Locked video path ==")
    shader = read("Assets/CineQuest/Shaders/LockedVideo.shader")
    check("LockedVideo.shader", bool(shader))
    check("Contrast formula in shader", "(c - 0.5) * _Contrast + 0.5" in shader or "(c - 0.5) * _Contrast + 0.5" in shader.replace(" ", ""))
    # flexible whitespace
    check(
        "Contrast formula (loose)",
        re.search(r"\(c\s*-\s*0\.5\)\s*\*\s*_Contrast\s*\+\s*0\.5", shader) is not None,
    )
    check("Bypass in shader", "_Bypass" in shader and "Bypass" in shader)
    check("Limited range expand", "ExpandLimited" in shader or "16.0 / 255.0" in shader)
    check("ImageParameterState core", exists("Assets/CineQuest/Scripts/Core/ImageParameterState.cs"))
    check("ImageParameterController", exists("Assets/CineQuest/Scripts/Video/ImageParameterController.cs"))
    check("LockedVideoRenderer", exists("Assets/CineQuest/Scripts/Video/LockedVideoRenderer.cs"))
    check("Capture IVideoCaptureSource", exists("Assets/CineQuest/Scripts/Capture/IVideoCaptureSource.cs"))
    check("Synthetic capture", exists("Assets/CineQuest/Scripts/Capture/EditorSyntheticCaptureSource.cs"))
    check("UVC4Unity adapter", exists("Assets/CineQuest/Scripts/Capture/Uvc4UnityCaptureSource.cs"))
    check("usb-video stub", exists("Assets/CineQuest/Scripts/Capture/UsbVideoNativeCaptureSource.cs"))
    app = read("Assets/CineQuest/Scripts/App/CineQuestApp.cs")
    check("Disable URP post-processing policy", "renderPostProcessing = false" in app)
    check("No tonemap in LockedVideo", "tonemap" not in shader.lower() or "No tone map" in shader or "Do NOT apply ACES" in shader)

    # Criterion 2 — scopes
    lines.append("")
    lines.append("== Criterion 2: Scopes ==")
    check("Waveform compute", exists("Assets/CineQuest/Shaders/ScopeWaveform.compute"))
    check("Parade compute", exists("Assets/CineQuest/Shaders/ScopeParade.compute"))
    check("Vectorscope compute", exists("Assets/CineQuest/Shaders/ScopeVectorscope.compute"))
    check("Waveform viz", exists("Assets/CineQuest/Shaders/ScopeWaveformViz.shader"))
    check("Vectorscope viz", exists("Assets/CineQuest/Shaders/ScopeVectorscopeViz.shader"))
    vs = read("Assets/CineQuest/Shaders/ScopeVectorscopeViz.shader")
    check("Skin-tone line graticule", "skin" in vs.lower())
    check("75%/100% radius markers", "0.55" in vs and "0.75" in vs)
    wf = read("Assets/CineQuest/Shaders/ScopeWaveformViz.shader")
    check("IRE graticule", "IRE" in wf or "GraticuleLine" in wf)
    check("WaveformScope.cs", exists("Assets/CineQuest/Scripts/Scopes/WaveformScope.cs"))
    check("ParadeScope.cs", exists("Assets/CineQuest/Scripts/Scopes/ParadeScope.cs"))
    check("VectorscopeScope.cs", exists("Assets/CineQuest/Scripts/Scopes/VectorscopeScope.cs"))
    check("ScopeManager quality", exists("Assets/CineQuest/Scripts/Scopes/ScopeManager.cs"))
    check("FreezeFrameController", exists("Assets/CineQuest/Scripts/Video/FreezeFrameController.cs"))
    check("ScopeQualityPolicy pure", exists("Assets/CineQuest/Scripts/Core/ScopeQualityPolicy.cs"))
    sm = read("Assets/CineQuest/Scripts/Scopes/ScopeManager.cs")
    check("Scopes use analysis downsample (not main CPU read every frame)", "EnsureAnalysisRt" in sm or "Analysis" in sm)

    # Criterion 3 — UX + persistence
    lines.append("")
    lines.append("== Criterion 3: UX + persistence ==")
    check("MonitorMenuController", exists("Assets/CineQuest/Scripts/UI/MonitorMenuController.cs"))
    check("StatusHud", exists("Assets/CineQuest/Scripts/UI/StatusHud.cs"))
    check("VideoStatusOverlay", exists("Assets/CineQuest/Scripts/Video/VideoStatusOverlay.cs"))
    check("TheaterModeController", exists("Assets/CineQuest/Scripts/Video/TheaterModeController.cs"))
    check("LayoutStore + serializer", exists("Assets/CineQuest/Scripts/Persistence/LayoutStore.cs"))
    check("LayoutSerializer core", exists("Assets/CineQuest/Scripts/Core/LayoutSerializer.cs"))
    check("PresetCatalog", exists("Assets/CineQuest/Scripts/Core/PresetCatalog.cs"))
    presets = read("Assets/CineQuest/Scripts/Core/PresetCatalog.cs")
    for name in ("Neutral Lock", "Iris Evaluation", "Lighting Balance", "Skin Tone Check", "Reference Bypass"):
        check(f"Preset '{name}'", name in presets)
    check("RuntimeSceneBuilder", exists("Assets/CineQuest/Scripts/App/RuntimeSceneBuilder.cs"))
    builder = read("Assets/CineQuest/Scripts/App/RuntimeSceneBuilder.cs")
    check("Tally not parented under overlay go", 'EnsureChild(go, "Tally")' not in builder)
    check("Tally sibling of overlay", 'EnsureChild(panelGo, "Tally")' in builder)
    check("Tally has own CanvasGroup", "BuildTallyStrip" in builder)
    hud = read("Assets/CineQuest/Scripts/UI/StatusHud.cs")
    for field in ("resolution", "fps", "usb", "latency", "format", "battery", "warning"):
        check(f"HUD field mention '{field}'", field.lower() in hud.lower())

    # Criterion 4 — packaging + docs
    lines.append("")
    lines.append("== Criterion 4: Android + docs ==")
    man = read("Assets/Plugins/Android/AndroidManifest.xml")
    check("AndroidManifest", bool(man))
    check("CAMERA permission", "android.permission.CAMERA" in man)
    check("USB host feature", "android.hardware.usb.host" in man)
    # Lean sideload: no USB_DEVICE_ATTACHED (would steal HDMI Link); no unused mic/hands
    check("No USB auto-launch (HDMI Link safe)", "USB_DEVICE_ATTACHED" not in man)
    check("No unused HEADSET_CAMERA", "HEADSET_CAMERA" not in man)
    check("usb_device_filter.xml", exists("Assets/Plugins/Android/res/xml/usb_device_filter.xml"))
    check("BUILD_AND_DEPLOY.md", exists("Docs/BUILD_AND_DEPLOY.md"))
    check("UVC_INTEGRATION.md", exists("Docs/UVC_INTEGRATION.md"))
    check("SIGNAL_FIDELITY.md", exists("Docs/SIGNAL_FIDELITY.md"))
    check("TESTING_CHECKLIST.md", exists("Docs/TESTING_CHECKLIST.md"))
    check("MONDAY_FIRST_TEST.md", exists("Docs/MONDAY_FIRST_TEST.md"))
    check("README.md", exists("README.md"))
    mon = read("Docs/MONDAY_FIRST_TEST.md")
    check("Monday runbook: adb install", "adb install" in mon)
    check("Monday runbook: Bypass + iris", "Bypass" in mon and "iris" in mon.lower())
    check("Monday runbook: UVC plugin", "UVC4UnityAndroid" in mon or "UVC" in mon)
    check("Monday runbook: Meta XR", "Meta XR" in mon)
    check("Pure tests project", exists("Tools/PureTests/CineQuest.PureTests.csproj"))
    check("Pure tests Program", exists("Tools/PureTests/Program.cs"))

    lines.append("")
    lines.append("RESULT: " + ("PASS" if ok else "FAIL"))
    text = "\n".join(lines) + "\n"
    print(text)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
