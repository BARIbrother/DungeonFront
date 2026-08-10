#!/usr/bin/env python3
"""Crop tree border master PNGs into 32px slices (no pixel edits) and build preview."""

from __future__ import annotations

import os
from PIL import Image, ImageDraw, ImageFont

ROOT = r"d:\Unity\Projects\DungeonFront"
BASE = os.path.join(ROOT, "Assets", "Art", "Background")
TILE = os.path.join(BASE, "Tiles", "Tree")
PREV = os.path.join(BASE, "_preview")

MASTERS = [
    ("tree_fill_64.png", 2, 2, "fill"),
    ("tree_edge_left_96x128.png", 3, 4, "edge_left"),
    ("tree_edge_right_96x128.png", 3, 4, "edge_right"),
    ("tree_fringe_left_64x128.png", 2, 4, "fringe_left"),
    ("tree_fringe_right_64x128.png", 2, 4, "fringe_right"),
]

CELL = 32


def slice_master(filename: str, cols: int, rows: int, prefix: str) -> None:
    path = os.path.join(BASE, filename)
    im = Image.open(path).convert("RGBA")
    for ty in range(rows):
        for tx in range(cols):
            px = tx * CELL
            py = im.height - (ty + 1) * CELL
            cell = im.crop((px, py, px + CELL, py + CELL))
            out = os.path.join(TILE, f"{prefix}_{tx}_{ty}.png")
            cell.save(out)


def simulate_autotile() -> None:
    """Mini map: grass rectangle inside forest — verify edges/corners."""
    # G=grass(floor), T=tree(locked interior), .=locked border (auto)
    layout = [
        "TTTTTTTTTT",
        "TTTTTTTTTT",
        "TTTTTTTTTT",
        "TGGGGGGGGT",
        "TGGGGGGGGT",
        "TGGGGGGGGT",
        "TGGGGGGGGT",
        "TTTTTTTTTT",
    ]
    h = len(layout)
    w = len(layout[0])

    def is_grass(x: int, y: int) -> bool:
        if x < 0 or y < 0 or x >= w or y >= h:
            return False
        row = layout[h - 1 - y]
        return row[x] == "G"

    def is_tree(x: int, y: int) -> bool:
        if x < 0 or y < 0 or x >= w or y >= h:
            return False
        row = layout[h - 1 - y]
        return row[x] == "T"

    def is_floor_or_out(x: int, y: int) -> bool:
        if x < 0 or y < 0 or x >= w or y >= h:
            return True
        return is_grass(x, y)

    def dist_floor(x, y, dx, dy):
        for step in range(1, 20):
            nx, ny = x + dx * step, y + dy * step
            if nx < 0 or ny < 0 or nx >= w or ny >= h:
                return step
            row = layout[h - 1 - ny]
            if row[nx] == "G":
                return step
        return 99

    def classify(x: int, y: int) -> str:
        if not is_tree(x, y):
            return "grass"
        n = is_floor_or_out(x, y + 1)
        s = is_floor_or_out(x, y - 1)
        e = is_floor_or_out(x + 1, y)
        wst = is_floor_or_out(x - 1, y)
        ds = dist_floor(x, y, 0, -1)
        dw = dist_floor(x, y, -1, 0)
        de = dist_floor(x, y, 1, 0)
        if n and wst:
            return "TL"
        if n and e:
            return "TR"
        if 1 <= ds <= 3:
            ly = ds - 1
            if ds == 1 and s and wst:
                return ("BL", ly)
            if ds == 1 and s and e:
                return ("BR", ly)
            if dw == 1:
                return ("L", ly)
            if de == 1:
                return ("R", ly)
            return ("B", ly)
        cn = (n, s, wst, e).count(True)
        if cn == 1:
            if n:
                return ("T", None)
            if wst:
                return ("L", None)
            if e:
                return ("R", None)
        if cn == 3:
            if not s:
                return ("T", None)
            if not e:
                return ("L", None)
            if not wst:
                return ("R", None)
        return ("F", None)

    def repeat_y(axis: int) -> int:
        return 1 if axis % 2 == 0 else 2

    kind_map = {
        "F": ("fill", 0, 0),
        "L": ("edge_right", 0, None),
        "R": ("edge_left", 2, None),
        "T": ("edge_left", 1, None),
        "B": ("edge_left", 1, None),
        "TL": ("edge_left", 1, 3),
        "TR": ("edge_right", 1, 3),
        "BL": ("edge_left", 1, 0),
        "BR": ("edge_right", 1, 0),
    }

    sheet = Image.new("RGBA", (w * CELL, h * CELL), (0, 0, 0, 0))
    draw = ImageDraw.Draw(sheet)
    for y in range(h):
        for x in range(w):
            key = classify(x, y)
            if key == "grass":
                draw.rectangle(
                    (x * CELL, (h - 1 - y) * CELL, (x + 1) * CELL, (h - y) * CELL),
                    fill=(88, 192, 52, 255),
                )
                continue
            if isinstance(key, tuple):
                label, ly = key
                prefix, tx, ty = kind_map[label]
                if ty is None:
                    ty = ly if ly is not None else repeat_y(x if label in ("T", "B") else y)
                else:
                    ty = ly if ly is not None else ty
            else:
                prefix, tx, ty = kind_map[key]
                if ty is None:
                    ty = repeat_y(x if key in ("T", "B") else y)
            p = os.path.join(TILE, f"{prefix}_{tx}_{ty}.png")
            if os.path.isfile(p):
                sheet.paste(Image.open(p).convert("RGBA"), (x * CELL, (h - 1 - y) * CELL))
            draw.rectangle(
                (x * CELL, (h - 1 - y) * CELL, (x + 1) * CELL - 1, (h - y) * CELL - 1),
                outline=(255, 255, 0, 80),
            )
    sheet = sheet.resize((w * CELL * 4, h * CELL * 4), Image.Resampling.NEAREST)
    sheet.save(os.path.join(PREV, "tree_autotile_preview.png"))


def build_kind_sheet() -> None:
    """Reference: each border kind sample at 4x."""
    samples = [
        ("Fill", "fill", 0, 0),
        ("EdgeTop(rep)", "edge_left", 1, 1),
        ("BottomL1", "edge_left", 1, 0),
        ("BottomL2", "edge_left", 1, 1),
        ("BottomL3", "edge_left", 1, 2),
        ("EdgeLeft(rep)", "edge_right", 0, 1),
        ("EdgeRight(rep)", "edge_left", 2, 1),
        ("CornerTL", "edge_left", 1, 3),
        ("CornerTR", "edge_right", 1, 3),
        ("CornerBL", "edge_left", 1, 0),
        ("CornerBR", "edge_right", 1, 0),
        ("FringeL(rep)", "fringe_left", 0, 1),
        ("FringeR(rep)", "fringe_right", 1, 1),
    ]
    scale = 4
    pad = 8
    label_h = 14
    cols = 4
    rows = (len(samples) + cols - 1) // cols
    sheet = Image.new(
        "RGBA",
        (cols * (CELL * scale + pad) + pad, rows * (CELL * scale + label_h + pad) + pad),
        (40, 70, 40, 255),
    )
    draw = ImageDraw.Draw(sheet)
    for i, (label, prefix, tx, ty) in enumerate(samples):
        cx = i % cols
        cy = i // cols
        ox = pad + cx * (CELL * scale + pad)
        oy = pad + cy * (CELL * scale + label_h + pad)
        p = os.path.join(TILE, f"{prefix}_{tx}_{ty}.png")
        if os.path.isfile(p):
            c = Image.open(p).convert("RGBA").resize((CELL * scale, CELL * scale), Image.Resampling.NEAREST)
            sheet.paste(c, (ox, oy + label_h))
        draw.text((ox, oy), label, fill=(255, 255, 200, 255))
    sheet.save(os.path.join(PREV, "tree_border_kinds.png"))


def main() -> None:
    os.makedirs(TILE, exist_ok=True)
    os.makedirs(PREV, exist_ok=True)
    for filename, cols, rows, prefix in MASTERS:
        slice_master(filename, cols, rows, prefix)
        print(f"sliced {prefix}: {cols}x{rows}")
    simulate_autotile()
    build_kind_sheet()
    print("preview saved to _preview/")


if __name__ == "__main__":
    main()
