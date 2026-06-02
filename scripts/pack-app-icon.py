#!/usr/bin/env python3
"""Build app-icon.ico from Assets/app-icon.png for Windows ApplicationIcon."""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
PNG = ROOT / "src" / "TdmsViewer" / "Assets" / "app-icon.png"
ICO = ROOT / "src" / "TdmsViewer" / "Assets" / "app-icon.ico"
ICO_SIZES = (256, 128, 64, 48, 32, 16)


def write_ico_from_png() -> None:
    if not PNG.is_file():
        raise SystemExit(f"Missing {PNG}")

    with Image.open(PNG) as img:
        rgba = img.convert("RGBA")
        icons = [rgba.resize((s, s), Image.Resampling.LANCZOS) for s in ICO_SIZES]
        icons[0].save(
            ICO,
            format="ICO",
            sizes=[(s, s) for s in ICO_SIZES],
            append_images=icons[1:],
        )


def main() -> int:
    write_ico_from_png()
    print(f"Generated {ICO} from {PNG}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
