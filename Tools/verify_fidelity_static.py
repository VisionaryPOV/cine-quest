#!/usr/bin/env python3
"""Static fidelity scan: forbidden auto-grade signals on video path."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    lines = []
    shader = (ROOT / "Assets/CineQuest/Shaders/LockedVideo.shader").read_text(encoding="utf-8")
    app = (ROOT / "Assets/CineQuest/Scripts/App/CineQuestApp.cs").read_text(encoding="utf-8")
    core = (ROOT / "Assets/CineQuest/Scripts/Core/ImageParameterState.cs").read_text(encoding="utf-8")

    lines.append("=== Fidelity static analysis ===")

    # Positive requirements
    checks = [
        ("shader has Bypass", "_Bypass" in shader),
        ("shader contrast formula", re.search(r"\(c\s*-\s*0\.5\).*_Contrast", shader) is not None),
        ("shader no ACES tonemap apply", "Do NOT apply ACES" in shader or "No tone map" in shader or "filmic" not in shader.lower().split("Do NOT")[0]),
        ("app disables post-processing", "renderPostProcessing = false" in app),
        ("app disables volumes", "Volume" in app and "enabled = false" in app),
        ("core bypass identity grade", "Bypass" in core and "Contrast = 1f" in core),
        ("core lock blocks TrySet", "if (locked) return false" in core),
    ]
    # Forbidden patterns in LockedVideo executable code (ignore // comments)
    code_only = "\n".join(
        line.split("//", 1)[0] for line in shader.splitlines()
    )
    forbidden = [
        (r"AutoExposure", "AutoExposure"),
        (r"\bbloom\b", "bloom"),
        (r"ACESTonemap|Tonemap\(", "tonemap function"),
    ]
    for pat, name in forbidden:
        hit = re.search(pat, code_only, re.I)
        checks.append((f"shader free of {name}", hit is None))

    ok = True
    for label, cond in checks:
        status = "OK" if cond else "FAIL"
        if not cond:
            ok = False
        lines.append(f"[{status}] {label}")

    lines.append("")
    lines.append("Notes:")
    lines.append("- Video content path is LockedVideo Unlit + explicit params only.")
    lines.append("- CineQuestApp forces urp.renderPostProcessing=false and disables Volume components.")
    lines.append("- System CABC / adaptive brightness is OS-level; documented in SIGNAL_FIDELITY.md.")
    lines.append("")
    lines.append("RESULT: " + ("PASS" if ok else "FAIL"))
    print("\n".join(lines))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
