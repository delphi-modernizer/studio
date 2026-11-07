#!/usr/bin/env python3
"""Builds the app icon and the package logos from assets/app-icon.

Usage: uv run --with resvg-py==0.5.0 --with pillow==12.1.1 scripts/build-app-icon.py

Every size is drawn from an SVG, so small sizes stay sharp instead of being scaled down from 256 px. Sizes of 24 px
and below use the small masters (wider crest, no nose guard). Adapted from contr-windows' scripts/build-app-icon.py.
"""

import io
import pathlib

import resvg_py
from PIL import Image

# The sizes Windows picks from for the taskbar, title bar, Start and Explorer at 100-200 % scaling.
ICO_SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)
TARGET_SIZES = (16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256)
SCALES = (100, 125, 150, 200, 400)
SMALL = 24

root = pathlib.Path(__file__).resolve().parent.parent
masters = root / "assets" / "app-icon"
assets = root / "src" / "App" / "DelphiModernizer.Studio.App" / "Assets"


def render(name: str, size: int) -> Image.Image:
    svg = masters / f"{name}{'-small' if size <= SMALL else ''}.svg"
    png = resvg_py.svg_to_bytes(svg_path=str(svg), width=size, height=size)
    return Image.open(io.BytesIO(bytes(png))).convert("RGBA")


assets.mkdir(parents=True, exist_ok=True)
frames = {size: render("app", size) for size in ICO_SIZES}
largest = frames[max(ICO_SIZES)]
largest.save(
    assets / "app.ico",
    format="ICO",
    sizes=[(size, size) for size in ICO_SIZES],
    append_images=[frames[size] for size in ICO_SIZES if size != max(ICO_SIZES)],
)
for size in TARGET_SIZES:
    # Windows shows the unplated images on the taskbar and in Start; the plated ones where it wants a tile.
    render("app", size).save(assets / f"Square44x44Logo.targetsize-{size}_altform-unplated.png", optimize=True)
    render("app-plated", size).save(assets / f"Square44x44Logo.targetsize-{size}.png", optimize=True)
for scale in SCALES:
    render("app-plated", round(44 * scale / 100)).save(assets / f"Square44x44Logo.scale-{scale}.png", optimize=True)
    render("app-plated", round(150 * scale / 100)).save(assets / f"Square150x150Logo.scale-{scale}.png", optimize=True)
    render("app-plated", round(50 * scale / 100)).save(assets / f"StoreLogo.scale-{scale}.png", optimize=True)
render("app", 256).save(assets / "StudioLogo.png", optimize=True)

print(f"{(assets / 'app.ico').relative_to(root)}: {len(ICO_SIZES)} sizes")
