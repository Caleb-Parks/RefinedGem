"""Composite the production relic icon onto a dark square for Steam Workshop."""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image


BG_RGB = (26, 18, 36)
SIZE = 512
PADDING = 48


def make_preview(source: Path, dest: Path, size: int = SIZE, padding: int = PADDING) -> None:
    gem = Image.open(source).convert("RGBA")
    inner = size - padding * 2
    gem = gem.resize((inner, inner), Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", (size, size), (*BG_RGB, 255))
    canvas.alpha_composite(gem, (padding, padding))

    dest.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(dest, format="PNG", optimize=True)
    print(f"Wrote {dest} ({size}x{size})")


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[1]
    source = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "art/relic/variations/refined_gem_v03_ruby_bottle.png"
    dest = Path(sys.argv[2]) if len(sys.argv) > 2 else root / "workshop/image.png"
    make_preview(source, dest)
