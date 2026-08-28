"""Remove backgrounds from relic icons: dark backdrops and baked-in checkerboard."""

from __future__ import annotations

import sys
from collections import deque
from pathlib import Path

from PIL import Image


def is_background_pixel(r: int, g: int, b: int, a: int) -> bool:
    if a < 16:
        return True
    brightness = max(r, g, b)
    if brightness <= 36:
        return True
    # Fake transparency checkerboard (light gray ~204/255 or white)
    if abs(r - g) <= 8 and abs(g - b) <= 8 and r >= 180:
        return True
    return False


def flood_remove_background(img: Image.Image) -> Image.Image:
    rgba = img.convert("RGBA")
    w, h = rgba.size
    pixels = rgba.load()
    visited = [[False] * w for _ in range(h)]
    q: deque[tuple[int, int]] = deque()

    # Seed from all border pixels that look like background
    for x in range(w):
        for y in (0, h - 1):
            r, g, b, a = pixels[x, y]
            if is_background_pixel(r, g, b, a):
                q.append((x, y))
                visited[x][y] = True
    for y in range(h):
        for x in (0, w - 1):
            if visited[x][y]:
                continue
            r, g, b, a = pixels[x, y]
            if is_background_pixel(r, g, b, a):
                q.append((x, y))
                visited[x][y] = True

    while q:
        x, y = q.popleft()
        pixels[x, y] = (0, 0, 0, 0)
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h and not visited[nx][ny]:
                r, g, b, a = pixels[nx, ny]
                if is_background_pixel(r, g, b, a):
                    visited[nx][ny] = True
                    q.append((nx, ny))

    # Secondary pass: knock out remaining very dark isolated backdrop pixels
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            if max(r, g, b) <= 48:
                pixels[x, y] = (0, 0, 0, 0)

    return rgba


def process_file(path: Path) -> None:
    img = Image.open(path)
    result = flood_remove_background(img)
    result.save(path, format="PNG", optimize=True)
    rgba = result.convert("RGBA")
    px = rgba.load()
    transparent = sum(1 for y in range(rgba.height) for x in range(rgba.width) if px[x, y][3] == 0)
    total = rgba.width * rgba.height
    print(f"{path.name}: {transparent}/{total} transparent ({100 * transparent / total:.1f}%)")


def process_dir(directory: Path) -> None:
    for path in sorted(directory.glob("refined_gem_v*.png")):
        process_file(path)


if __name__ == "__main__":
    target = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[1] / "art/relic/variations"
    if target.is_file():
        process_file(target)
    else:
        process_dir(target)
