#!/usr/bin/env python3
"""Slice SIDE/MID/BOTTOM + floor gradients into 32px tiles."""

from __future__ import annotations

import os
from PIL import Image, ImageDraw

ROOT = r"d:\Unity\Projects\DungeonFront"
BASE = os.path.join(ROOT, "Assets", "Art", "Background")
SRC = os.path.join(BASE, "TreeParts")
TILE = os.path.join(BASE, "Tiles", "Tree")
PREV = os.path.join(BASE, "_preview")
CELL = 32

SIDE_SRC = r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_SIDE-a824f2af-c1e8-460d-b2c2-4d008a9ff275.png"
MID_SRC = r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_MID-f2e964f8-5480-4bba-94d8-9ca666b01056.png"
BOTTOM_SRC = r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_BOTTOM-10853177-0c98-4358-b6ad-9bbf63f7c70d.png"
FLOOR_SRC = r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_floor-c209b259-f15a-49ce-bfa5-cdc385f9cbbd.png"

# 16-wide bottom strip: [floorL][7 trees x 2][floorR], height 4 = floorSouth + tree 3
BOTTOM_W = 16
BOTTOM_H = 4
TREE_H = 3


def ensure_dirs() -> None:
    for path in (SRC, TILE, PREV):
        os.makedirs(path, exist_ok=True)


def save_grid(prefix: str, cells: dict[tuple[int, int], Image.Image], cols: int, rows: int) -> None:
    for ty in range(rows):
        for tx in range(cols):
            cells[(tx, ty)].save(os.path.join(TILE, f"{prefix}_{tx}_{ty}.png"))


def slice_side() -> dict[tuple[int, int], Image.Image]:
    im = Image.open(SIDE_SRC).convert("RGBA")
    crop = im.crop((0, 0, 32, 128))
    cells = {}
    for ty in range(4):
        py = crop.size[1] - (ty + 1) * CELL
        cells[(0, ty)] = crop.crop((0, py, CELL, py + CELL))
    save_grid("side_left", cells, 1, 4)
    flip = { (0, ty): cells[(0, ty)].transpose(Image.Transpose.FLIP_LEFT_RIGHT) for ty in range(4) }
    save_grid("side_right", flip, 1, 4)
    return cells


def slice_mid() -> dict[tuple[int, int], Image.Image]:
    im = Image.open(MID_SRC).convert("RGBA")
    x0 = (im.size[0] - 64) // 2
    y0 = (im.size[1] - 64) // 2
    crop = im.crop((x0, y0, x0 + 64, y0 + 64))
    cells = {}
    for ty in range(2):
        for tx in range(2):
            py = crop.size[1] - (ty + 1) * CELL
            cells[(tx, ty)] = crop.crop((tx * CELL, py, (tx + 1) * CELL, py + CELL))
    save_grid("mid", cells, 2, 2)
    save_grid("fill", cells, 2, 2)
    return cells


def strip_bright_grass(cell: Image.Image) -> Image.Image:
    """Remove bright walkable grass from trunk tiles (floor grad handles that)."""
    out = cell.copy()
    pix = out.load()
    dark = (40, 72, 40, 255)
    for y in range(CELL):
        for x in range(CELL):
            r, g, b, a = pix[x, y]
            if g > 140 and g > r + 25 and g > b + 20:
                pix[x, y] = dark
    return out


def slice_tree_unit() -> dict[tuple[int, int], Image.Image]:
    """One 2x3 tree (64x96) from BOTTOM ref — used 7 times in the strip."""
    im = Image.open(BOTTOM_SRC).convert("RGBA")
    # First tree roughly left half; take 64x96 from top
    x0 = max(0, (im.size[0] // 2 - 64) // 2)
    crop = im.crop((x0, 0, x0 + 64, 96))
    # Prefer a denser trunk-centered crop: try second tree too and pick by brown pixels
    alt = im.crop((im.size[0] // 2, 0, im.size[0] // 2 + 64, 96)) if im.size[0] >= 64 + im.size[0] // 2 else crop
    def trunk_score(c):
        n = 0
        for y in range(64, 96):
            for x in range(64):
                r, g, b, a = c.getpixel((x, y))
                if r > g + 10 and r > 40:
                    n += 1
        return n
    if trunk_score(alt) > trunk_score(crop):
        crop = alt
    # If width leftover, center on 64 from full centered 128 of two trees / 2
    two = im.crop(((im.size[0] - 128) // 2, 0, (im.size[0] - 128) // 2 + 128, 96))
    crop = two.crop((0, 0, 64, 96))  # left tree of pair
    cells = {}
    for ty in range(TREE_H):
        for tx in range(2):
            py = crop.size[1] - (ty + 1) * CELL
            cell = crop.crop((tx * CELL, py, (tx + 1) * CELL, py + CELL))
            if ty == 0:
                cell = strip_bright_grass(cell)
            cells[(tx, ty)] = cell
    save_grid("tree", cells, 2, TREE_H)
    crop.save(os.path.join(SRC, "tree_64x96.png"))
    # also keep second tree variation
    crop2 = two.crop((64, 0, 128, 96))
    cells2 = {}
    for ty in range(TREE_H):
        for tx in range(2):
            py = crop2.size[1] - (ty + 1) * CELL
            cell = crop2.crop((tx * CELL, py, (tx + 1) * CELL, py + CELL))
            if ty == 0:
                cell = strip_bright_grass(cell)
            cells2[(tx, ty)] = cell
    save_grid("tree_b", cells2, 2, TREE_H)
    return cells, cells2


def slice_floor_gradients() -> tuple[dict, dict, dict]:
    """
    From floor ref (dark floor | jagged | bright grass):
    - floor_side_right: dark left, grass right (Floor east of forest)
    - floor_side_left: grass left, dark right (Floor west)
    - floor_south: rotate so grass is at bottom (Floor south under trunks)
    """
    im = Image.open(FLOOR_SRC).convert("RGBA")
    # Transition ~x=80; take 32px window centered on edge
    edge_x = 80
    x0 = max(0, min(im.size[0] - 32, edge_x - 16))
    # Prefer mid-height strip away from canopy clutter
    y0 = 100
    h = 128
    if y0 + h > im.size[1]:
        y0 = max(0, im.size[1] - h)
    strip = im.crop((x0, y0, x0 + 32, y0 + h))  # 32x128
    strip.save(os.path.join(SRC, "floor_side_right_32x128.png"))

    side_r = {}
    for ty in range(4):
        py = strip.size[1] - (ty + 1) * CELL
        side_r[(0, ty)] = strip.crop((0, py, CELL, py + CELL))
    save_grid("floor_side_right", side_r, 1, 4)

    side_l = { (0, ty): side_r[(0, ty)].transpose(Image.Transpose.FLIP_LEFT_RIGHT) for ty in range(4) }
    save_grid("floor_side_left", side_l, 1, 4)

    # South: rotate 90° CW so former left(dark)→top, right(grass)→bottom... 
    # rotate -90 (CW in PIL is negative? PIL rotate is CCW): 
    # Want grass at BOTTOM of tile (toward Floor south).
    # strip has dark left, grass right. Rotate 90° CW: left→top, right→bottom → grass at bottom. ✓
    south_strip = strip.rotate(-90, expand=True)  # 128x32
    south_strip.save(os.path.join(SRC, "floor_south_128x32.png"))
    south = {}
    for tx in range(4):
        south[(tx, 0)] = south_strip.crop((tx * CELL, 0, (tx + 1) * CELL, CELL))
    save_grid("floor_south", south, 4, 1)
    return side_l, side_r, south


def build_bottom_strip(
    tree_a: dict,
    tree_b: dict,
    floor_l: dict,
    floor_r: dict,
    floor_s: dict,
) -> dict[tuple[int, int], Image.Image]:
    """16x4: row0=floor south, rows1-3=trees; col0/15=floor side grads."""
    cells = {}
    for ty in range(BOTTOM_H):
        for tx in range(BOTTOM_W):
            if ty == 0:
                cells[(tx, ty)] = floor_s[(tx % 4, 0)]
                continue
            tree_row = ty - 1  # 0..2
            if tx == 0:
                cells[(tx, ty)] = floor_l[(0, tree_row % 4)]
            elif tx == BOTTOM_W - 1:
                cells[(tx, ty)] = floor_r[(0, tree_row % 4)]
            else:
                slot = tx - 1  # 0..13
                tree_i = slot // 2
                lx = slot % 2
                src = tree_a if tree_i % 2 == 0 else tree_b
                cells[(tx, ty)] = src[(lx, tree_row)]
    save_grid("bottom", cells, BOTTOM_W, BOTTOM_H)
    return cells


def preview(cells: dict, mid: dict, side: dict) -> None:
    tw, th = 16, 7
    sheet = Image.new("RGBA", (tw * CELL * 2, th * CELL * 2 + 28), (40, 70, 40, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((4, 4), "floor grad + 7 trees + push-up", fill=(255, 255, 200, 255))
    grass = (88, 192, 52, 255)
    for ty in range(th):
        for tx in range(tw):
            ox = tx * CELL * 2
            oy = (th - 1 - ty) * CELL * 2 + 28
            if ty == 0:
                sheet.paste(Image.new("RGBA", (CELL * 2, CELL * 2), grass), (ox, oy))
                continue
            band = ty  # 1..6
            if band <= BOTTOM_H:
                cell = cells[(tx, band - 1)]
            elif tx == 0:
                cell = side[(0, (band - 1) % 4)]
            elif tx == tw - 1:
                cell = side[(0, (band - 1) % 4)].transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            else:
                cell = mid[(tx % 2, band % 2)]
            sheet.paste(cell.resize((CELL * 2, CELL * 2), Image.Resampling.NEAREST), (ox, oy))
    sheet.save(os.path.join(PREV, "tree_parts_layout_preview.png"))
    wide = Image.new("RGBA", (sheet.size[0] * 2, sheet.size[1]), (40, 70, 40, 255))
    wide.paste(sheet, (0, 0))
    wide.paste(sheet, (sheet.size[0], 0))
    wide.save(os.path.join(PREV, "tree_parts_layout_x2.png"))


def main() -> None:
    ensure_dirs()
    for name, path in (("side", SIDE_SRC), ("mid", MID_SRC), ("bottom", BOTTOM_SRC), ("floor", FLOOR_SRC)):
        Image.open(path).convert("RGBA").save(os.path.join(SRC, f"{name}_ref.png"))

    side = slice_side()
    mid = slice_mid()
    tree_a, tree_b = slice_tree_unit()
    floor_l, floor_r, floor_s = slice_floor_gradients()
    bottom = build_bottom_strip(tree_a, tree_b, floor_l, floor_r, floor_s)
    preview(bottom, mid, side)
    print("bottom strip 16x4: floorSouth + 7 trees + side grads")


if __name__ == "__main__":
    main()
