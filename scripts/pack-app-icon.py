#!/usr/bin/env python3
"""Copy the user-provided PNG into Assets and build app-icon.ico for Windows.

The source PNG is never resized, re-encoded, or edited — only copied byte-for-byte.
ICO generation reads the PNG and writes a separate .ico file for ApplicationIcon.
"""

from __future__ import annotations

import shutil
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "TdmsViewer" / "Assets"
PNG = ASSETS / "app-icon.png"
ICO = ASSETS / "app-icon.ico"
ICO_SIZES = (256, 128, 64, 48, 32, 16)


def copy_source_png(source: Path) -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, PNG)


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


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        if not PNG.is_file():
            print(
                "Usage: python3 scripts/pack-app-icon.py <path-to-your-uploaded-icon.png>\n"
                "Or place your uploaded PNG at src/TdmsViewer/Assets/app-icon.png and run without arguments.",
                file=sys.stderr,
            )
            return 1
        write_ico_from_png()
        print(f"Generated {ICO} from existing {PNG} ({PNG.stat().st_size} bytes, PNG unchanged).")
        return 0

    source = Path(argv[1]).expanduser().resolve()
    if not source.is_file():
        print(f"Source file not found: {source}", file=sys.stderr)
        return 1

    copy_source_png(source)
    write_ico_from_png()
    print(f"Copied {source} -> {PNG} ({PNG.stat().st_size} bytes, byte-identical).")
    print(f"Generated {ICO} for Windows ApplicationIcon.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
