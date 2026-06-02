#!/usr/bin/env python3
"""Generate TdmsViewer app icon: full-bleed rounded square, transparent outside."""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw

SIZE = 1024
CORNER_RADIUS = int(SIZE * 0.2237)
ACCENT = (0, 122, 255)
ACCENT_DARK = (0, 86, 204)
OUTPUT_DIR = Path(__file__).resolve().parents[1] / "src" / "TdmsViewer" / "Assets"


def rounded_square_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def wave_y(x: float, span: float, phase: float, amp: float, freq: float) -> float:
    t = x / span
    return (
        math.sin(t * math.pi * 2 * freq + phase) * amp
        + math.sin(t * math.pi * 4 + phase * 1.3) * amp * 0.35
        + math.cos(t * math.pi * 6 + phase * 0.7) * amp * 0.15
    )


def draw_waveform(
    draw: ImageDraw.ImageDraw,
    size: int,
    cy: float,
    phase: float,
    amp: float,
    line_width: int,
    color: tuple,
) -> None:
    points: list[tuple[float, float]] = []
    margin_x = int(size * 0.12)
    span = size - 2 * margin_x
    step = max(2, int(4 * size / SIZE))
    for x in range(margin_x, size - margin_x, step):
        y = cy + wave_y(x - margin_x, span, phase, amp, 1.8)
        points.append((x, y))
    if len(points) >= 2:
        draw.line(points, fill=color, width=line_width, joint="curve")


def build_icon(size: int) -> Image.Image:
    scale = size / SIZE
    radius = max(1, int(CORNER_RADIUS * scale))

    rgba = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    mask = rounded_square_mask(size, radius)

    bg = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    bg_draw = ImageDraw.Draw(bg)
    bg_draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=ACCENT)

    grad = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    for y in range(size):
        t = y / max(size - 1, 1)
        alpha = int(38 * t)
        ImageDraw.Draw(grad).line([(0, y), (size, y)], fill=(*ACCENT_DARK, alpha))
    bg = Image.alpha_composite(bg, grad)
    rgba.paste(bg, mask=mask)

    draw = ImageDraw.Draw(rgba)
    cy1 = size * 0.38
    cy2 = size * 0.52
    cy3 = size * 0.66
    w_main = max(2, int(14 * scale))
    w_mid = max(2, int(11 * scale))
    w_thin = max(1, int(9 * scale))

    draw_waveform(draw, size, cy1, 0.0, size * 0.11, w_main, (255, 255, 255, 245))
    draw_waveform(draw, size, cy2, 1.4, size * 0.09, w_mid, (255, 255, 255, 200))
    draw_waveform(draw, size, cy3, 2.8, size * 0.08, w_thin, (255, 255, 255, 165))

    dot_r = max(2, int(10 * scale))
    dot_cx = int(size * 0.78)
    dot_cy = int(size * 0.28)
    draw.ellipse(
        (dot_cx - dot_r, dot_cy - dot_r, dot_cx + dot_r, dot_cy + dot_r),
        fill=(255, 255, 255, 230),
    )

    return rgba


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    master = build_icon(SIZE)
    png_path = OUTPUT_DIR / "app-icon.png"
    master.save(png_path, "PNG")

    ico_sizes = [16, 32, 48, 64, 128, 256]
    ico_images = [build_icon(s) for s in ico_sizes]
    ico_path = OUTPUT_DIR / "app-icon.ico"
    ico_images[0].save(
        ico_path,
        format="ICO",
        sizes=[(s, s) for s in ico_sizes],
        append_images=ico_images[1:],
    )

    print(f"Wrote {png_path} ({SIZE}x{SIZE}, transparent corners)")
    print(f"Wrote {ico_path} (sizes: {ico_sizes})")


if __name__ == "__main__":
    main()
