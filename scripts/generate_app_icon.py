#!/usr/bin/env python3
"""Generate TdmsViewer app icon: squircle, edge-to-edge art, transparent outside."""

from __future__ import annotations

import math
import struct
import zlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "TdmsViewer" / "Assets"

SIZE = 1024
CORNER_RADIUS = int(SIZE * 0.223)  # ~iOS/macOS squircle proportion

ACCENT = (0, 122, 255)
ACCENT_DEEP = (0, 86, 204)
PURPLE = (88, 86, 214)
WHITE = (255, 255, 255)
WAVE_ALPHA = 235


def squircle_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def lerp_color(c1: tuple[int, int, int], c2: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return (
        int(lerp(c1[0], c2[0], t)),
        int(lerp(c1[1], c2[1], t)),
        int(lerp(c1[2], c2[2], t)),
    )


def draw_gradient_background(img: Image.Image, mask: Image.Image) -> None:
    px = img.load()
    mpx = mask.load()
    for y in range(SIZE):
        for x in range(SIZE):
            if mpx[x, y] == 0:
                continue
            t = (x / (SIZE - 1)) * 0.55 + (y / (SIZE - 1)) * 0.45
            t = max(0.0, min(1.0, t))
            base = lerp_color(ACCENT, PURPLE, t * 0.35)
            highlight = lerp_color(ACCENT, (120, 180, 255), (1 - y / SIZE) * 0.25)
            r = int(lerp(base[0], highlight[0], 0.4))
            g = int(lerp(base[1], highlight[1], 0.4))
            b = int(lerp(base[2], highlight[2], 0.4))
            depth = 0.92 + 0.08 * (y / SIZE)
            px[x, y] = (int(r * depth), int(g * depth), int(b * depth), mpx[x, y])


def wave_points(
    width: float,
    height: float,
    center_y: float,
    amplitude: float,
    frequency: float,
    phase: float,
    samples: int = 200,
) -> list[tuple[float, float]]:
    left = (SIZE - width) / 2
    pts: list[tuple[float, float]] = []
    for i in range(samples + 1):
        t = i / samples
        x = left + t * width
        y = center_y + math.sin(t * math.pi * 2 * frequency + phase) * amplitude
        y += math.sin(t * math.pi * 6 + phase * 1.3) * amplitude * 0.18
        pts.append((x, y))
    return pts


def draw_waves(img: Image.Image) -> None:
    overlay = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay, "RGBA")

    configs = [
        (0.78, 0.36, 3.2, 0.0, 5.5, (*WHITE, 200)),
        (0.78, 0.36, 2.4, 1.4, 6.0, (*WHITE, WAVE_ALPHA)),
        (0.78, 0.36, 1.8, 2.6, 6.5, (*WHITE, 255)),
    ]

    cy = SIZE * 0.52
    for w_frac, amp_frac, freq, phase, stroke, color in configs:
        w = SIZE * w_frac
        amp = SIZE * amp_frac
        pts = wave_points(w, 0, cy, amp, freq, phase)
        draw.line(pts, fill=color, width=int(stroke), joint="curve")

    # Subtle baseline grid ticks (data channel metaphor)
    tick_y = int(SIZE * 0.82)
    tick_w = int(SIZE * 0.62)
    left = (SIZE - tick_w) // 2
    for i in range(5):
        x = left + int(i * tick_w / 4)
        h = 12 if i % 2 == 0 else 20
        draw.line([(x, tick_y), (x, tick_y - h)], fill=(*WHITE, 90), width=4)

    img.alpha_composite(overlay)


def draw_gloss(img: Image.Image, mask: Image.Image) -> None:
    gloss = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(gloss, "RGBA")
    gdraw.ellipse(
        (-SIZE * 0.15, -SIZE * 0.55, SIZE * 1.15, SIZE * 0.55),
        fill=(255, 255, 255, 38),
    )
    img.alpha_composite(Image.composite(gloss, gloss, mask))


def build_master() -> Image.Image:
    mask = squircle_mask(SIZE, CORNER_RADIUS)
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw_gradient_background(img, mask)
    draw_waves(img)
    draw_gloss(img, mask)
    # Clip to squircle so corners outside the icon body stay fully transparent.
    clipped = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    clipped.paste(img, (0, 0), mask)
    return clipped


def save_png_sizes(master: Image.Image) -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    master.save(ASSETS / "app-icon-1024.png", "PNG")
    for s in (16, 24, 32, 48, 64, 128, 256, 512):
        resized = master.resize((s, s), Image.Resampling.LANCZOS)
        resized.save(ASSETS / f"app-icon-{s}.png", "PNG")
    master.save(ASSETS / "app-icon.png", "PNG")


def png_chunk(tag: bytes, data: bytes) -> bytes:
    chunk = tag + data
    crc = zlib.crc32(chunk) & 0xFFFFFFFF
    return struct.pack(">I", len(data)) + chunk + struct.pack(">I", crc)


def write_ico(path: Path, images: list[Image.Image]) -> None:
    entries: list[tuple[int, bytes]] = []
    for im in images:
        if im.mode != "RGBA":
            im = im.convert("RGBA")
        w, h = im.size
        # ICO stores PNG payloads for sizes >= 256
        import io

        buf = io.BytesIO()
        im.save(buf, format="PNG")
        png_data = buf.getvalue()
        entries.append((w, png_data))

    offset = 6 + 16 * len(entries)
    parts = [struct.pack("<HHH", 0, 1, len(entries))]
    for w, png_data in entries:
        bw = 0 if w >= 256 else w
        bh = 0 if w >= 256 else w
        parts.append(
            struct.pack("<BBBBHHII", bw, bh, 0, 0, 1, 32, len(png_data), offset)
        )
        offset += len(png_data)
    for _, png_data in entries:
        parts.append(png_data)
    path.write_bytes(b"".join(parts))


def save_ico(master: Image.Image) -> None:
    sizes = [16, 24, 32, 48, 64, 128, 256]
    images = [master.resize((s, s), Image.Resampling.LANCZOS) for s in sizes]
    write_ico(ASSETS / "app.ico", images)


def main() -> None:
    master = build_master()
    save_png_sizes(master)
    save_ico(master)
    print(f"Wrote icons to {ASSETS}")


if __name__ == "__main__":
    main()
