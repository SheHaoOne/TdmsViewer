#!/usr/bin/env python3
"""Generate TdmsViewer app icon: rounded square, transparent outside."""

from __future__ import annotations

import math
import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

SIZE = 1024
CORNER_RADIUS = int(SIZE * 0.2237)
ACCENT = (0, 122, 255)
ACCENT_DARK = (0, 86, 204)
LENS_FILL = (255, 255, 255, 42)
OUTPUT_DIR = Path(__file__).resolve().parents[1] / "src" / "TdmsViewer" / "Assets"

FONT_BOLD_CANDIDATES = [
    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
    "C:/Windows/Fonts/segoeuib.ttf",
    "C:/Windows/Fonts/arialbd.ttf",
]
FONT_REGULAR_CANDIDATES = [
    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
    "C:/Windows/Fonts/segoeui.ttf",
    "C:/Windows/Fonts/arial.ttf",
]


def load_font(candidates: list[str], px: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    px = max(8, px)
    for path in candidates:
        if os.path.isfile(path):
            return ImageFont.truetype(path, px)
    return ImageFont.load_default()


def rounded_square_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def wave_y(x: float, span: float, phase: float, amp: float, freq: float) -> float:
    t = x / max(span, 1)
    return (
        math.sin(t * math.pi * 2 * freq + phase) * amp
        + math.sin(t * math.pi * 4 + phase * 1.3) * amp * 0.32
        + math.cos(t * math.pi * 6 + phase * 0.7) * amp * 0.14
    )


def wave_points(x0: int, x1: int, y0: float, span: float, phase: float, amp: float, step: int) -> list[tuple[float, float]]:
    pts: list[tuple[float, float]] = []
    for x in range(x0, x1, step):
        y = y0 + wave_y(x - x0, span, phase, amp, 1.75)
        pts.append((x, y))
    return pts


def draw_centered_text(
    draw: ImageDraw.ImageDraw,
    text: str,
    y: float,
    canvas: int,
    font: ImageFont.ImageFont,
    fill: tuple,
) -> None:
    bbox = draw.textbbox((0, 0), text, font=font)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    x = (canvas - tw) / 2 - bbox[0]
    draw.text((x, y - bbox[1]), text, font=font, fill=fill)


def draw_chart_curves(
    draw: ImageDraw.ImageDraw,
    size: int,
    left: int,
    right: int,
    top: int,
    bottom: int,
) -> None:
    axis_w = max(2, int(3 * size / SIZE))
    axis_color = (255, 255, 255, 210)
    grid_color = (255, 255, 255, 70)

    draw.line([(left, bottom), (right, bottom)], fill=axis_color, width=axis_w)
    draw.line([(left, top), (left, bottom)], fill=axis_color, width=axis_w)

    for i in range(1, 4):
        gy = top + (bottom - top) * i / 4
        draw.line([(left, gy), (right, gy)], fill=grid_color, width=1)

    span = right - left
    step = max(2, int(5 * size / SIZE))
    plot_h = bottom - top
    mid = top + plot_h * 0.52
    lw1 = max(2, int(12 * size / SIZE))
    lw2 = max(2, int(9 * size / SIZE))
    lw3 = max(1, int(7 * size / SIZE))

    p1 = wave_points(left, right, mid, span, 0.0, plot_h * 0.30, step)
    p2 = wave_points(left, right, mid, span, 1.35, plot_h * 0.24, step)
    p3 = wave_points(left, right, mid, span, 2.7, plot_h * 0.20, step)

    if len(p1) >= 2:
        draw.line(p1, fill=(255, 255, 255, 245), width=lw1, joint="curve")
    if len(p2) >= 2:
        draw.line(p2, fill=(255, 255, 255, 205), width=lw2, joint="curve")
    if len(p3) >= 2:
        draw.line(p3, fill=(255, 255, 255, 170), width=lw3, joint="curve")


def draw_magnifier(
    base: Image.Image,
    size: int,
    cx: float,
    cy: float,
    lens_r: float,
) -> None:
    scale = size / SIZE
    stroke = max(3, int(14 * scale))
    handle_w = max(3, int(12 * scale))
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)

    x0, y0, x1, y1 = cx - lens_r, cy - lens_r, cx + lens_r, cy + lens_r
    draw.ellipse((x0, y0, x1, y1), fill=LENS_FILL, outline=(255, 255, 255, 245), width=stroke)

    lens = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ldraw = ImageDraw.Draw(lens)
    inner_l = int(left := cx - lens_r * 0.72)
    inner_r = int(cx + lens_r * 0.72)
    inner_t = int(cy - lens_r * 0.55)
    inner_b = int(cy + lens_r * 0.55)
    span = inner_r - inner_l
    step = max(2, int(4 * size / SIZE))
    mid = (inner_t + inner_b) / 2
    mini = wave_points(inner_l, inner_r, mid, span, 0.6, lens_r * 0.42, step)
    if len(mini) >= 2:
        ldraw.line(mini, fill=(255, 255, 255, 250), width=max(2, int(8 * scale)), joint="curve")

    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).ellipse((x0, y0, x1, y1), fill=255)
    layer = Image.alpha_composite(layer, Image.composite(lens, Image.new("RGBA", (size, size), (0, 0, 0, 0)), mask))

    draw = ImageDraw.Draw(layer)
    angle = math.radians(42)
    hx0 = cx + math.cos(angle) * lens_r * 0.72
    hy0 = cy + math.sin(angle) * lens_r * 0.72
    hx1 = cx + math.cos(angle) * lens_r * 1.55
    hy1 = cy + math.sin(angle) * lens_r * 1.55
    draw.line([(hx0, hy0), (hx1, hy1)], fill=(255, 255, 255, 245), width=handle_w)

    base.alpha_composite(layer)


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
        alpha = int(40 * t)
        ImageDraw.Draw(grad).line([(0, y), (size, y)], fill=(*ACCENT_DARK, alpha))
    bg = Image.alpha_composite(bg, grad)
    rgba.paste(bg, mask=mask)

    draw = ImageDraw.Draw(rgba)

    if size >= 16:
        tdms_px = max(6, int(size * 0.20))
        viewer_px = max(5, int(size * 0.095))
        font_tdms = load_font(FONT_BOLD_CANDIDATES, tdms_px)
        font_viewer = load_font(FONT_REGULAR_CANDIDATES, viewer_px)

        draw_centered_text(draw, "Tdms", size * 0.07, size, font_tdms, (255, 255, 255, 252))
        draw_centered_text(draw, "TdmsViewer", size * 0.84, size, font_viewer, (255, 255, 255, 235))

    chart_left = int(size * 0.10)
    chart_right = int(size * 0.58) if size >= 96 else int(size * 0.90)
    chart_top = int(size * 0.28)
    chart_bottom = int(size * 0.76)
    draw_chart_curves(draw, size, chart_left, chart_right, chart_top, chart_bottom)

    if size >= 24:
        lens_r = size * (0.15 if size < 96 else 0.17)
        cx = size * 0.74
        cy = size * 0.50
        draw_magnifier(rgba, size, cx, cy, lens_r)

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

    print(f"Wrote {png_path} ({SIZE}x{SIZE})")
    print(f"Wrote {ico_path} (sizes: {ico_sizes})")


if __name__ == "__main__":
    main()
