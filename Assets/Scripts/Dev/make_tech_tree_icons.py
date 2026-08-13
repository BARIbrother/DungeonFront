# One-shot 16x16 tech-tree icons. Transparent, hard pixels, 1px outline.
import os
import struct
import zlib

SIZE = 16
OUT = (24, 18, 16, 255)
CLEAR = (0, 0, 0, 0)

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
RES_DIR = os.path.join(ROOT, "Assets", "Resources", "UI", "TechTree")
ART_DIR = os.path.join(ROOT, "Assets", "Art", "UI", "TechTree")
PREV_DIR = os.path.join(ART_DIR, "_preview")

# Palettes
IRON = [(74, 76, 84, 255), (122, 124, 132, 255), (196, 198, 204, 255)]
WOOD = [(58, 36, 16, 255), (106, 68, 32, 255), (160, 112, 56, 255)]
FIRE = [(196, 74, 24, 255), (240, 160, 48, 255), (255, 232, 160, 255)]
MANA = [(42, 58, 136, 255), (58, 168, 216, 255), (200, 240, 255, 255)]
DARK = [(26, 10, 32, 255), (58, 26, 74, 255), (106, 42, 122, 255)]
BRIGHT = [(90, 90, 104, 255), (176, 176, 192, 255), (236, 236, 244, 255)]
GREY = [(48, 24, 64, 255), (140, 132, 148, 255), (220, 220, 228, 255)]
GOLD = [(138, 104, 24, 255), (212, 164, 48, 255), (240, 224, 128, 255)]
STONE = [(64, 60, 52, 255), (120, 112, 96, 255), (176, 168, 148, 255)]
INK = [(36, 28, 22, 255), (72, 56, 40, 255)]


def blank():
    return [[CLEAR for _ in range(SIZE)] for _ in range(SIZE)]


def put(px, x, y, color):
    if 0 <= x < SIZE and 0 <= y < SIZE and color[3] > 0:
        px[y][x] = color


def fill(px, cells, color):
    for x, y in cells:
        put(px, x, y, color)


def rect(px, x0, y0, x1, y1, color):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(px, x, y, color)


def diamond(px, cx, cy, r, color):
    for y in range(cy - r, cy + r + 1):
        for x in range(cx - r, cx + r + 1):
            if abs(x - cx) + abs(y - cy) <= r:
                put(px, x, y, color)


def disk(px, cx, cy, r, color):
    for y in range(cy - r, cy + r + 1):
        for x in range(cx - r, cx + r + 1):
            if (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r:
                put(px, x, y, color)


def outline(px):
    filled = {
        (x, y)
        for y in range(SIZE)
        for x in range(SIZE)
        if px[y][x][3] > 0
    }
    out = blank()
    for y in range(SIZE):
        for x in range(SIZE):
            if (x, y) in filled:
                out[y][x] = px[y][x]
            elif any((x + dx, y + dy) in filled for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))):
                out[y][x] = OUT
    return out


def write_png(path, pixels, scale=1):
    w = SIZE * scale
    h = SIZE * scale
    rows = []
    for y in range(h):
        row = [0]
        src_y = y // scale
        for x in range(w):
            row.extend(pixels[src_y][x // scale])
        rows.append(bytes(row))
    raw = b"".join(rows)
    comp = zlib.compress(raw, 9)
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)

    def chunk(tag, data):
        crc = zlib.crc32(tag + data) & 0xFFFFFFFF
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", crc)

    data = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", comp) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as handle:
        handle.write(data)


def conveyor():
    px = blank()
    rect(px, 3, 7, 12, 9, IRON[1])
    rect(px, 4, 8, 11, 8, IRON[2])
    disk(px, 4, 8, 2, IRON[0])
    disk(px, 11, 8, 2, IRON[0])
    put(px, 4, 8, IRON[2])
    put(px, 11, 8, IRON[2])
    fill(px, [(6, 6), (7, 7), (8, 6), (9, 7), (10, 6)], GOLD[1])
    return outline(px)


def drill_2():
    px = blank()
    fill(px, [(4, 12), (5, 11), (6, 10), (7, 9), (8, 8), (8, 7)], WOOD[1])
    fill(px, [(5, 12), (6, 11), (7, 10)], WOOD[2])
    rect(px, 7, 4, 12, 6, IRON[1])
    rect(px, 8, 4, 12, 5, IRON[2])
    put(px, 13, 5, IRON[0])
    put(px, 7, 3, IRON[0])
    put(px, 11, 8, MANA[1])
    put(px, 12, 9, DARK[1])
    put(px, 13, 8, BRIGHT[2])
    return outline(px)


def manaext():
    px = blank()
    diamond(px, 8, 6, 4, MANA[1])
    diamond(px, 8, 6, 2, MANA[2])
    put(px, 8, 6, BRIGHT[2])
    rect(px, 7, 10, 9, 12, STONE[1])
    rect(px, 6, 13, 10, 13, STONE[0])
    return outline(px)


def manastore():
    px = blank()
    disk(px, 8, 9, 4, MANA[0])
    disk(px, 8, 9, 3, MANA[1])
    disk(px, 7, 8, 1, MANA[2])
    rect(px, 7, 4, 9, 5, STONE[1])
    rect(px, 7, 3, 9, 3, WOOD[1])
    put(px, 8, 2, WOOD[2])
    return outline(px)


def manacraft_1():
    px = blank()
    rect(px, 3, 11, 12, 13, WOOD[0])
    rect(px, 4, 11, 11, 12, WOOD[1])
    put(px, 3, 10, WOOD[2])
    put(px, 12, 10, WOOD[2])
    diamond(px, 8, 7, 2, MANA[1])
    put(px, 8, 7, MANA[2])
    fill(px, [(11, 7), (12, 6), (13, 5)], IRON[1])
    put(px, 13, 4, IRON[2])
    return outline(px)


def enchant():
    px = blank()
    fill(px, [(4, 6), (5, 7), (6, 8), (7, 9), (7, 10), (6, 11), (5, 12)], INK[1])
    fill(px, [(11, 6), (10, 7), (9, 8), (8, 9), (8, 10), (9, 11), (10, 12)], INK[1])
    fill(px, [(5, 8), (6, 9), (6, 10)], BRIGHT[2])
    fill(px, [(10, 8), (9, 9), (9, 10)], BRIGHT[1])
    put(px, 7, 7, GOLD[2])
    put(px, 8, 6, GOLD[1])
    put(px, 9, 7, GOLD[2])
    put(px, 4, 5, MANA[2])
    put(px, 12, 5, MANA[2])
    put(px, 8, 4, MANA[1])
    return outline(px)


def furnace_2():
    px = blank()
    rect(px, 4, 7, 11, 13, STONE[1])
    rect(px, 5, 8, 10, 12, STONE[0])
    rect(px, 10, 3, 12, 7, STONE[1])
    rect(px, 11, 3, 12, 4, STONE[2])
    rect(px, 6, 10, 9, 12, FIRE[0])
    rect(px, 7, 10, 8, 11, FIRE[1])
    put(px, 7, 11, DARK[2])
    put(px, 8, 11, BRIGHT[2])
    put(px, 11, 2, FIRE[1])
    return outline(px)


def crafter_2():
    px = blank()
    disk(px, 7, 8, 4, IRON[0])
    disk(px, 7, 8, 3, IRON[1])
    disk(px, 7, 8, 1, IRON[2])
    for x, y in ((7, 4), (7, 12), (3, 8), (11, 8), (4, 5), (10, 5), (4, 11), (10, 11)):
        put(px, x, y, IRON[2])
    disk(px, 11, 11, 2, IRON[1])
    put(px, 11, 11, GOLD[1])
    return outline(px)


def manacraft_2():
    px = blank()
    rect(px, 3, 12, 12, 13, WOOD[0])
    rect(px, 4, 12, 11, 12, WOOD[1])
    diamond(px, 8, 7, 3, MANA[0])
    diamond(px, 8, 7, 2, MANA[1])
    put(px, 8, 7, MANA[2])
    put(px, 6, 5, DARK[2])
    put(px, 10, 5, BRIGHT[2])
    rect(px, 7, 10, 9, 11, STONE[1])
    return outline(px)


def foundry():
    px = blank()
    rect(px, 2, 9, 13, 13, STONE[1])
    rect(px, 3, 10, 12, 12, STONE[0])
    rect(px, 4, 7, 6, 9, STONE[1])
    rect(px, 9, 5, 11, 9, STONE[1])
    put(px, 5, 6, FIRE[1])
    put(px, 10, 4, FIRE[1])
    put(px, 11, 4, FIRE[0])
    rect(px, 6, 11, 9, 12, STONE[2])
    return outline(px)


def crafter_3():
    px = blank()
    rect(px, 3, 4, 4, 13, IRON[0])
    rect(px, 11, 4, 12, 13, IRON[0])
    rect(px, 3, 4, 12, 5, IRON[1])
    rect(px, 4, 4, 11, 4, IRON[2])
    rect(px, 6, 8, 9, 13, IRON[1])
    rect(px, 7, 9, 8, 12, GOLD[1])
    put(px, 7, 7, GOLD[2])
    return outline(px)


def drill_3():
    px = blank()
    rect(px, 6, 2, 9, 8, IRON[1])
    rect(px, 7, 3, 8, 7, IRON[2])
    fill(px, [(7, 9), (8, 9), (6, 10), (9, 10), (7, 11), (8, 11), (8, 12)], IRON[0])
    put(px, 7, 13, IRON[2])
    put(px, 3, 12, STONE[1])
    put(px, 4, 13, STONE[2])
    put(px, 12, 12, STONE[1])
    put(px, 11, 13, STONE[0])
    return outline(px)


def furnace_3():
    px = blank()
    rect(px, 4, 6, 11, 13, STONE[0])
    rect(px, 5, 7, 10, 12, GREY[0])
    rect(px, 10, 2, 12, 6, STONE[1])
    rect(px, 6, 9, 9, 12, FIRE[1])
    rect(px, 7, 9, 8, 11, FIRE[2])
    put(px, 7, 10, GREY[2])
    put(px, 8, 10, DARK[2])
    put(px, 11, 1, FIRE[2])
    put(px, 12, 2, FIRE[1])
    return outline(px)


def manacraft_3():
    px = blank()
    rect(px, 5, 3, 10, 12, INK[1])
    rect(px, 6, 4, 9, 11, GOLD[0])
    rect(px, 6, 5, 9, 6, GOLD[2])
    put(px, 7, 8, MANA[2])
    put(px, 8, 9, MANA[1])
    fill(px, [(4, 4), (11, 4), (4, 11), (11, 11)], GREY[2])
    return outline(px)


def altar():
    px = blank()
    rect(px, 3, 12, 12, 13, STONE[0])
    rect(px, 5, 10, 10, 11, STONE[1])
    rect(px, 6, 8, 9, 9, STONE[2])
    rect(px, 4, 6, 5, 12, GREY[0])
    rect(px, 10, 6, 11, 12, GREY[0])
    rect(px, 4, 6, 11, 6, GREY[1])
    diamond(px, 8, 4, 2, GOLD[1])
    put(px, 8, 4, FIRE[2])
    return outline(px)


def fuel_1():
    px = blank()
    disk(px, 8, 8, 5, GOLD[0])
    disk(px, 8, 8, 4, GOLD[1])
    disk(px, 8, 8, 3, BRIGHT[2])
    rect(px, 8, 5, 8, 8, INK[0])
    rect(px, 8, 8, 10, 8, INK[0])
    put(px, 8, 8, GOLD[2])
    put(px, 8, 2, GOLD[2])
    return outline(px)


def fuel_2():
    px = blank()
    disk(px, 8, 8, 5, GOLD[0])
    disk(px, 8, 8, 4, GOLD[1])
    disk(px, 8, 8, 3, FIRE[2])
    rect(px, 8, 5, 8, 8, INK[0])
    rect(px, 8, 8, 11, 8, INK[0])
    put(px, 10, 9, INK[0])
    put(px, 8, 8, GOLD[2])
    put(px, 8, 2, FIRE[1])
    put(px, 13, 8, FIRE[1])
    return outline(px)


ICONS = {
    "m_conveyor_1": conveyor,
    "m_drill_2": drill_2,
    "m_manaext_1": manaext,
    "m_manastore_1": manastore,
    "m_manacraft_1": manacraft_1,
    "m_enchant_1": enchant,
    "m_furnace_2": furnace_2,
    "m_crafter_2": crafter_2,
    "m_manacraft_2": manacraft_2,
    "m_foundry_1": foundry,
    "m_crafter_3": crafter_3,
    "m_drill_3": drill_3,
    "m_furnace_3": furnace_3,
    "m_manacraft_3": manacraft_3,
    "m_altar_1": altar,
    "fuel_1": fuel_1,
    "fuel_2": fuel_2,
}

SHEET_BG = (200, 230, 160, 255)
SCALE = 16


def sheet(icons):
    cols = 6
    rows = (len(icons) + cols - 1) // cols
    cell = SIZE * SCALE + 8
    w = cols * cell
    h = rows * cell
    canvas = [[SHEET_BG for _ in range(w)] for _ in range(h)]
    for i, pixels in enumerate(icons):
        cx = (i % cols) * cell + 4
        cy = (i // cols) * cell + 4
        for y in range(SIZE * SCALE):
            for x in range(SIZE * SCALE):
                color = pixels[y // SCALE][x // SCALE]
                if color[3] > 0:
                    canvas[cy + y][cx + x] = color
    return canvas, w, h


def write_png_any(path, pixels):
    h = len(pixels)
    w = len(pixels[0])
    rows = []
    for y in range(h):
        row = [0]
        for x in range(w):
            row.extend(pixels[y][x])
        rows.append(bytes(row))
    raw = b"".join(rows)
    comp = zlib.compress(raw, 9)
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)

    def chunk(tag, data):
        crc = zlib.crc32(tag + data) & 0xFFFFFFFF
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", crc)

    data = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", comp) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as handle:
        handle.write(data)


def main():
    rendered = []
    for name, builder in ICONS.items():
        pixels = builder()
        rendered.append(pixels)
        write_png(os.path.join(RES_DIR, f"{name}.png"), pixels)
        write_png(os.path.join(ART_DIR, f"{name}.png"), pixels)
        write_png(os.path.join(PREV_DIR, f"{name}.png"), pixels, scale=SCALE)
        print(name)
    canvas, _, _ = sheet(rendered)
    write_png_any(os.path.join(PREV_DIR, "_sheet.png"), canvas)


if __name__ == "__main__":
    main()
