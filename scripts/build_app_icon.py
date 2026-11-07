#!/usr/bin/env python3
"""Builds the app icon and the package logos from assets/app-icon.

Usage: `uv run python scripts/build_app_icon.py`.

Every size is drawn from an SVG, so that small sizes stay sharp instead of
being scaled down from 256 px. Sizes of 24 px and below use the small masters
(wider crest, no nose guard). Adapted from contr-windows'
`scripts/build-app-icon.py`.
"""

from __future__ import annotations

import io
import pathlib

from PIL import Image
import resvg_py

# The sizes Windows picks from for the taskbar, title bar, Start and Explorer
# at 100 to 200 % scaling.
ICO_SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)
TARGET_SIZES = (16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256)
SCALES = (100, 125, 150, 200, 400)
SMALL = 24
ROOT = pathlib.Path(__file__).resolve().parent.parent
MASTERS = ROOT / "assets" / "app-icon"
ASSETS = ROOT / "src" / "App" / "DelphiModernizer.Studio.App" / "Assets"


def _render(name: str, size: int) -> Image.Image:
    """Renders a master at `size` pixels, the small one at 24 px and below."""
    small = "-small" if size <= SMALL else ""
    svg = MASTERS / f"{name}{small}.svg"
    png = resvg_py.svg_to_bytes(svg_path=str(svg), width=size, height=size)
    return Image.open(io.BytesIO(png)).convert("RGBA")


def _save(name: str, size: int, file_name: str) -> None:
    _render(name, size).save(ASSETS / file_name, optimize=True)


def main() -> int:
    """Writes `app.ico`, the package logos and the studio's own logo."""
    ASSETS.mkdir(parents=True, exist_ok=True)
    frames = {size: _render("app", size) for size in ICO_SIZES}
    largest = max(ICO_SIZES)
    frames[largest].save(
        ASSETS / "app.ico",
        format="ICO",
        sizes=[(size, size) for size in ICO_SIZES],
        append_images=[frames[size] for size in ICO_SIZES if size != largest],
    )
    for size in TARGET_SIZES:
        # Windows shows the unplated images on the taskbar and in Start; the
        # plated ones where it wants a tile.
        _save(
            "app",
            size,
            f"Square44x44Logo.targetsize-{size}_altform-unplated.png",
        )
        _save("app-plated", size, f"Square44x44Logo.targetsize-{size}.png")
    for scale in SCALES:
        for logo, points in (
            ("Square44x44Logo", 44),
            ("Square150x150Logo", 150),
            ("StoreLogo", 50),
        ):
            _save(
                "app-plated",
                round(points * scale / 100),
                f"{logo}.scale-{scale}.png",
            )
    _save("app", 256, "StudioLogo.png")
    print(f"{(ASSETS / 'app.ico').relative_to(ROOT)}: {len(ICO_SIZES)} sizes")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
