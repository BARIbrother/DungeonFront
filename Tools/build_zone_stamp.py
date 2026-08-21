#!/usr/bin/env python3
"""
Build 16x16 zone forest stamps + Cursor-visible map preview.

Mask bits (Floor/out on zone side): N=1 S=2 E=4 W=8
Writes:
  Assets/Art/Background/Tiles/Tree/ZoneTemplates/stamp_{mask:02d}.keys.txt
  Assets/Art/Background/Tiles/Tree/ZoneTemplates/stamp_{mask:02d}.png
  Assets/Art/Background/_preview/zone_stamp_map.png
  Assets/Art/Background/_preview/zone_stamp_atlas.png
"""

from __future__ import annotations

import os
from PIL import Image, ImageDraw

ROOT = r"d:\Unity\Projects\DungeonFront"
TILE = os.path.join(ROOT, "Assets", "Art", "Background", "Tiles", "Tree")
OUT = os.path.join(TILE, "ZoneTemplates")
PREV = os.path.join(ROOT, "Assets", "Art", "Background", "_preview")
LOCKED_SRC = ""
LOCKED_256 = os.path.join(OUT, "locked_zone.png")
CELL = 32
ZONE = 16
N, S, E, W = 1, 2, 4, 8

_locked_cells: dict[tuple[int, int], Image.Image] | None = None


def load_tile(prefix: str, tx: int, ty: int) -> Image.Image:
    path = os.path.join(TILE, f"{prefix}_{tx}_{ty}.png")
    return Image.open(path).convert("RGBA")


def find_locked_source() -> str:
    if LOCKED_SRC and os.path.isfile(LOCKED_SRC):
        return LOCKED_SRC

    home_assets = os.path.join(
        os.path.expanduser("~"),
        ".cursor",
        "projects",
        "d-Unity-Projects-DungeonFront",
        "assets",
    )
    if os.path.isdir(home_assets):
        for name in os.listdir(home_assets):
            if "grasstile_imsi" in name and name.endswith(".png"):
                return os.path.join(home_assets, name)

    if os.path.isfile(LOCKED_256):
        return LOCKED_256

    raise FileNotFoundError("locked grass source PNG not found")


def load_locked_cells() -> dict[tuple[int, int], Image.Image]:
    global _locked_cells
    if _locked_cells is not None:
        return _locked_cells

    os.makedirs(OUT, exist_ok=True)
    src = Image.open(find_locked_source()).convert("RGBA")
    img256 = src.resize((256, 256), Image.Resampling.NEAREST)
    img256.save(LOCKED_256)
    img512 = img256.resize((512, 512), Image.Resampling.NEAREST)
    cells: dict[tuple[int, int], Image.Image] = {}
    for ly in range(ZONE):
        for lx in range(ZONE):
            # ly=0 is south (image bottom). PIL y=0 is top.
            py = (ZONE - 1 - ly) * CELL
            cells[(lx, ly)] = img512.crop((lx * CELL, py, lx * CELL + CELL, py + CELL))
    _locked_cells = cells
    return cells


def mid_tile(lx: int, ly: int) -> tuple[str, Image.Image]:
    key = f"locked_{lx}_{ly}"
    return key, load_locked_cells()[(lx, ly)]


def build_stamp(mask: int) -> tuple[list[list[str]], Image.Image]:
    keys = [[""] * ZONE for _ in range(ZONE)]
    img = Image.new("RGBA", (ZONE * CELL, ZONE * CELL), (0, 0, 0, 255))

    for ly in range(ZONE):
        for lx in range(ZONE):
            key, tile = mid_tile(lx, ly)

            # South belt: bottom 16x4 (floor grad + trees)
            if (mask & S) and ly < 4:
                key = f"bottom_{lx}_{ly}"
                tile = load_tile("bottom", lx, ly)

            # West / East foliage sides (above south belt, or full if no S)
            elif (mask & W) and lx == 0:
                key = f"side_left_0_{ly % 4}"
                tile = load_tile("side_left", 0, ly % 4)
            elif (mask & E) and lx == ZONE - 1:
                key = f"side_right_0_{ly % 4}"
                tile = load_tile("side_right", 0, ly % 4)

            # North tip row
            if (mask & N) and ly == ZONE - 1 and not ((mask & W) and lx == 0) and not ((mask & E) and lx == ZONE - 1):
                key = f"mid_{lx % 2}_1"
                tile = load_tile("mid", lx % 2, 1)

            keys[ly][lx] = key
            # image: ly=0 at bottom of zone visually → PIL y from top = (ZONE-1-ly)
            img.paste(tile, (lx * CELL, (ZONE - 1 - ly) * CELL))

    return keys, img


def write_stamp(mask: int, keys: list[list[str]], img: Image.Image) -> None:
    os.makedirs(OUT, exist_ok=True)
    key_path = os.path.join(OUT, f"stamp_{mask:02d}.keys.txt")
    with open(key_path, "w", encoding="utf-8", newline="\n") as f:
        for ly in range(ZONE):
            f.write(" ".join(keys[ly]) + "\n")
    img.save(os.path.join(OUT, f"stamp_{mask:02d}.png"))


def build_map_preview() -> Image.Image:
    """3x4 zones, center (0,0) = Floor grass, others Locked with neighbor-aware stamps."""
    zones_x, zones_y = 3, 4
    cx, cy = 0, 0
    grass = (88, 192, 52, 255)
    map_w, map_h = zones_x * ZONE, zones_y * ZONE
    sheet = Image.new("RGBA", (map_w * CELL, map_h * CELL), grass)

    unlocked = {(cx, cy)}

    def floor_or_out(zx: int, zy: int, dx: int, dy: int) -> bool:
        nx, ny = zx + dx, zy + dy
        if nx < 0 or ny < 0 or nx >= zones_x or ny >= zones_y:
            return True
        return (nx, ny) in unlocked

    for zy in range(zones_y):
        for zx in range(zones_x):
            ox = zx * ZONE * CELL
            # zone y=0 at bottom of map in game; PIL top is north → flip zone rows
            oy = (zones_y - 1 - zy) * ZONE * CELL

            if (zx, zy) in unlocked:
                Image.Image.paste(sheet, Image.new("RGBA", (ZONE * CELL, ZONE * CELL), grass), (ox, oy))
                continue

            mask = 0
            if floor_or_out(zx, zy, 0, 1):
                mask |= N
            if floor_or_out(zx, zy, 0, -1):
                mask |= S
            if floor_or_out(zx, zy, 1, 0):
                mask |= E
            if floor_or_out(zx, zy, -1, 0):
                mask |= W

            _, stamp = build_stamp(mask)
            sheet.paste(stamp, (ox, oy))

    return sheet


def build_atlas(stamps: dict[int, Image.Image]) -> Image.Image:
    cols = 4
    rows = 4
    pad = 8
    label_h = 16
    cell_w = ZONE * CELL // 2
    cell_h = ZONE * CELL // 2
    atlas = Image.new(
        "RGBA",
        (cols * (cell_w + pad) + pad, rows * (cell_h + label_h + pad) + pad),
        (30, 50, 30, 255),
    )
    draw = ImageDraw.Draw(atlas)
    for mask in range(16):
        cx = mask % cols
        cy = mask // cols
        ox = pad + cx * (cell_w + pad)
        oy = pad + cy * (cell_h + label_h + pad)
        flags = []
        if mask & N:
            flags.append("N")
        if mask & S:
            flags.append("S")
        if mask & E:
            flags.append("E")
        if mask & W:
            flags.append("W")
        label = f"{mask:02d}:" + ("".join(flags) if flags else "mid")
        draw.text((ox, oy), label, fill=(255, 255, 200, 255))
        thumb = stamps[mask].resize((cell_w, cell_h), Image.Resampling.NEAREST)
        atlas.paste(thumb, (ox, oy + label_h))
    return atlas


def main() -> None:
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREV, exist_ok=True)

    stamps = {}
    for mask in range(16):
        keys, img = build_stamp(mask)
        write_stamp(mask, keys, img)
        stamps[mask] = img
        print(f"stamp_{mask:02d} ok")

    atlas = build_atlas(stamps)
    atlas.save(os.path.join(PREV, "zone_stamp_atlas.png"))

    full = build_map_preview()
    # Cursor-friendly size: half scale still readable
    preview = full.resize((full.size[0] // 2, full.size[1] // 2), Image.Resampling.NEAREST)
    preview.save(os.path.join(PREV, "zone_stamp_map.png"))
    # zoom crop around start zone for detail
    # start zone at grid (0,0) → PIL bottom-left region of map
    zw = ZONE * CELL
    # full image: zone (0,0) is at oy = (4-1-0)*zw = 3*zw from top
    detail = full.crop((0, 2 * zw, 2 * zw, 4 * zw))  # zones (0,0)-(1,1) area
    detail.resize((detail.size[0] * 2, detail.size[1] * 2), Image.Resampling.NEAREST).save(
        os.path.join(PREV, "zone_stamp_detail.png")
    )
    print("preview: zone_stamp_map.png / zone_stamp_atlas.png / zone_stamp_detail.png")


if __name__ == "__main__":
    main()
