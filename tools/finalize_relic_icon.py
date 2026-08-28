"""Resize relic color icon to 200x200 and derive outline silhouette layer."""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageFilter, ImageOps


def create_outline(color: Image.Image, outline_rgb: tuple[int, int, int] = (42, 26, 58)) -> Image.Image:
    """Derive a dark silhouette outline from the color icon alpha channel."""
    rgba = color.convert("RGBA")
    w, h = rgba.size
    alpha = rgba.split()[3]

    # Prefer transparency mask; fall back to luminance for opaque backgrounds
    if alpha.getextrema()[1] < 255:
        mask = alpha.point(lambda p: 255 if p > 48 else 0)
    else:
        gray = ImageOps.grayscale(rgba)
        mask = gray.point(lambda p: 255 if p > 28 else 0)

    mask = mask.filter(ImageFilter.MaxFilter(3))
    mask = mask.filter(ImageFilter.GaussianBlur(0.8))
    mask = mask.point(lambda p: 255 if p > 64 else 0)

    outline = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    solid = Image.new("RGBA", (w, h), (*outline_rgb, 255))
    outline.paste(solid, mask=mask)
    return outline


def finalize(source: Path, color_out: Path, outline_out: Path, size: int = 200) -> None:
    img = Image.open(source).convert("RGBA")
    color = img.resize((size, size), Image.Resampling.LANCZOS)
    outline = create_outline(color)

    color_out.parent.mkdir(parents=True, exist_ok=True)
    color.save(color_out, format="PNG", optimize=True)
    outline.save(outline_out, format="PNG", optimize=True)
    print(f"Wrote {color_out} ({color.size[0]}x{color.size[1]})")
    print(f"Wrote {outline_out} ({outline.size[0]}x{outline.size[1]})")


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[1]
    source = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "art/relic/variations/refined_gem_v03_ruby_bottle.png"
    assets = root / "assets"
    finalize(source, assets / "refined_gem_relic.png", assets / "refined_gem_relic_outline.png")
