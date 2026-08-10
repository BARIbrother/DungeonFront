#!/usr/bin/env python3
"""Build 16x4 bottom wall from edge_left master (crop only) and validate seams."""

from __future__ import annotations

import math
import os
from PIL import Image, ImageDraw

ROOT = r"d:\Unity\Projects\DungeonFront"
BASE = os.path.join(ROOT, "Assets", "Art", "Background")
TILE = os.path.join(BASE, "Tiles", "Tree")
PREV = os.path.join(BASE, "_preview")
CELL = 32
TW, TH = 16, 4


def seam_h(a: Image.Image, b: Image.Image) -> float:
    err = n = 0
    for y in range(CELL):
        for i in range(3):
            pa = a.getpixel((CELL - 1, y))
            pb = b.getpixel((0, y))
            err += (pa[i] - pb[i]) ** 2
            n += 1
    return math.sqrt(err / n)


def seam_v(a: Image.Image, b: Image.Image) -> float:
    err = n = 0
    for x in range(CELL):
        for i in range(3):
            pa = a.getpixel((x, CELL - 1))
            pb = b.getpixel((x, 0))
            err += (pa[i] - pb[i]) ** 2
            n += 1
    return math.sqrt(err / n)


def band_py(band_row: int, sheet_h: int) -> int:
    return sheet_h - (band_row + 1) * CELL


def build_wall() -> dict[tuple[int, int], Image.Image]:
    left = Image.open(os.path.join(BASE, "tree_edge_left_96x128.png")).convert("RGBA")
    right = Image.open(os.path.join(BASE, "tree_edge_right_96x128.png")).convert("RGBA")

    master = Image.new("RGBA", (TW * CELL, TH * CELL))
    for i in range(0, TW * CELL, left.size[0]):
        master.paste(left, (i, 0))

    cells: dict[tuple[int, int], Image.Image] = {}
    for ty in range(TH):
        py = band_py(ty, TH * CELL)
        for tx in range(TW):
            if tx == 0:
                cell = right.crop((0, band_py(ty, right.size[1]), CELL, band_py(ty, right.size[1]) + CELL))
            elif tx == TW - 1:
                cell = left.crop((2 * CELL, band_py(ty, left.size[1]), 3 * CELL, band_py(ty, left.size[1]) + CELL))
            else:
                cell = master.crop((tx * CELL, py, (tx + 1) * CELL, py + CELL))
            cells[(tx, ty)] = cell
            master.paste(cell, (tx * CELL, (TH - 1 - ty) * CELL))

    os.makedirs(TILE, exist_ok=True)
    for ty in range(TH):
        for tx in range(TW):
            cells[(tx, ty)].save(os.path.join(TILE, f"bottom_wall_{tx}_{ty}.png"))

    master.save(os.path.join(BASE, "tree_bottom_wall_512x128.png"))
    return cells


def validate(cells: dict[tuple[int, int], Image.Image]) -> tuple[float, float, float, float]:
    max_h = max_v = 0.0
    int_h = int_v = 0.0
    for ty in range(TH):
        for tx in range(1, TW):
            h = seam_h(cells[(tx - 1, ty)], cells[(tx, ty)])
            max_h = max(max_h, h)
            if 1 <= tx <= TW - 2:
                int_h = max(int_h, h)
        for tx in range(TW):
            if ty == 0:
                continue
            v = seam_v(cells[(tx, ty - 1)], cells[(tx, ty)])
            max_v = max(max_v, v)
            if 1 <= tx <= TW - 2:
                int_v = max(int_v, v)
    return max_h, max_v, int_h, int_v


def save_previews(cells: dict[tuple[int, int], Image.Image]) -> None:
    os.makedirs(PREV, exist_ok=True)
    sheet = Image.new("RGBA", (TW * CELL * 2, TH * CELL * 2 + 24), (40, 60, 40, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((4, 4), "bottom wall 16x4 (4 layers)", fill=(255, 255, 200, 255))
    for ty in range(TH):
        for tx in range(TW):
            c = cells[(tx, ty)].resize((CELL * 2, CELL * 2), Image.Resampling.NEAREST)
            sheet.paste(c, (tx * CELL * 2, (TH - 1 - ty) * CELL * 2 + 24))
    sheet.save(os.path.join(PREV, "bottom_wall_16x4_final.png"))

    wide = Image.new("RGBA", (TW * CELL * 4, TH * CELL * 2 + 24), (40, 60, 40, 255))
    wide.paste(sheet, (0, 0))
    wide.paste(sheet, (TW * CELL * 2, 0))
    wide.save(os.path.join(PREV, "bottom_wall_16x4_x2test.png"))


def main() -> None:
    cells = build_wall()
    max_h, max_v, int_h, int_v = validate(cells)
    save_previews(cells)
    print(f"seams maxH={max_h:.1f} maxV={max_v:.1f} interiorH={int_h:.1f} interiorV={int_v:.1f}")
    if int_v > 40:
        print("WARN: interior vertical seams still high on trunk column")


if __name__ == "__main__":
    main()
