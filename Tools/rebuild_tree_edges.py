from PIL import Image
import os

BASE = r"d:\Unity\Projects\DungeonFront\Assets\Art\Background"
PREV = os.path.join(BASE, "_preview")
TILE = os.path.join(BASE, "Tiles", "Tree")
BG = r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_bg-9c0f4dc8-553b-4fb3-b47e-7c5f77eff2f5.png"

TRUNK = (78, 48, 28, 255)
TRUNK_D = (52, 32, 18, 255)
TRUNK_H = (98, 62, 36, 255)


def wrap_v_mse(tile):
    tw, th = tile.size
    err = n = 0
    for x in range(tw):
        a = tile.getpixel((x, th - 1))
        b = tile.getpixel((x, 0))
        err += sum((a[i] - b[i]) ** 2 for i in range(3))
        n += 3
    return (err / n) ** 0.5


def best_y(im, x0, w, h, y0=180, y1=420):
    best = None
    best_e = 1e9
    for oy in range(y0, y1):
        if oy + h > im.size[1]:
            break
        t = im.crop((x0, oy, x0 + w, oy + h))
        e = wrap_v_mse(t)
        if e < best_e:
            best_e = e
            best = t
    return best


def draw_trunk_shaft(im, x_center, y0, y1, half_w=2):
    pix = im.load()
    w, h = im.size
    for y in range(max(0, y0), min(h, y1)):
        for dx in range(-half_w, half_w + 1):
            x = x_center + dx
            if not (0 <= x < w):
                continue
            r, g, b, a = pix[x, y]
            if g > r + 15 and g > 155:
                continue
            if abs(dx) == 0:
                pix[x, y] = TRUNK_H if (y % 5 == 0) else TRUNK
            elif abs(dx) == 1:
                pix[x, y] = TRUNK
            else:
                pix[x, y] = TRUNK_D


def paint_leaf_bottom_block(im, y_leaf_bottom, trunk_xs, y_extent=14):
    """Under a leaf scallop line, fill with trunk so boundary is blocked."""
    pix = im.load()
    w, h = im.size
    for x in range(w):
        # detect if this x has leaf just above y_leaf_bottom
        if y_leaf_bottom <= 0 or y_leaf_bottom >= h:
            continue
        above = pix[x, y_leaf_bottom - 1]
        if not (above[1] > above[0] + 15 and above[1] > 70):
            continue
        for dy in range(y_extent):
            y = y_leaf_bottom + dy
            if y >= h:
                break
            r, g, b, a = pix[x, y]
            if g > r + 15 and g > 70:
                break
            # prefer trunk near shafts; elsewhere darker trunk shadow
            near = min(abs(x - t) for t in trunk_xs)
            if near <= 3:
                pix[x, y] = TRUNK_H if near == 0 else TRUNK
            elif near <= 6:
                pix[x, y] = TRUNK_D
            else:
                # block gap with dark bark/shadow so grass doesn't show through
                pix[x, y] = (40, 32, 22, 255)


def build_edge(src, fill, grass_left):
    # 96x128: top tip | mid seamless+trunk | bottom base
    out = Image.new("RGBA", (96, 128))
    out.paste(src.crop((0, 0, 96, 32)), (0, 0))
    out.paste(src.crop((0, 96, 96, 128)), (0, 96))

    mid = Image.new("RGBA", (96, 64))
    mid.paste(fill, (0, 0))
    mid.paste(fill, (32, 0))
    # only a thin grass-facing veil from source (no full mid scallops)
    if grass_left:
        veil = src.crop((0, 40, 28, 88))
        mid.paste(veil, (0, 8))
        trunk_xs = (16, 22)
    else:
        veil = src.crop((68, 40, 96, 88))
        mid.paste(veil, (68, 8))
        trunk_xs = (74, 80)
    out.paste(mid, (0, 32))

    for i, tx in enumerate(trunk_xs):
        draw_trunk_shaft(out, tx, 26, 118, half_w=2 if i == 0 else 1)

    # Block under leaf bottoms on the BOTTOM cap only (unique ending)
    paint_leaf_bottom_block(out, 100, trunk_xs, y_extent=16)
    # Also ensure mid has shaft continuity visible under any mid veil leaves
    paint_leaf_bottom_block(out, 70, trunk_xs, y_extent=8)

    return out


def build_fringe(src, fill, dark_right):
    out = Image.new("RGBA", (64, 128))
    out.paste(src.crop((0, 0, 64, 32)), (0, 0))
    out.paste(src.crop((0, 96, 64, 128)), (0, 96))

    mid = fill.copy()
    # thin dark outer veil only
    src_mid = src.crop((0, 40, 64, 88))
    mp, sp = mid.load(), src_mid.load()
    for y in range(48):
        for x in range(64):
            if dark_right and x >= 44:
                mp[x, y + 8] = sp[x, y]
            elif (not dark_right) and x < 20:
                mp[x, y + 8] = sp[x, y]
    out.paste(mid, (0, 32))

    # trunks in the column we actually place (LocalX=0 left / LocalX=1 right)
    trunk_xs = (12, 18) if not dark_right else (20, 26)
    if dark_right:
        # FringeLeft uses LocalX=0 (left 32) — keep trunks left
        trunk_xs = (12, 18)
    else:
        # FringeRight uses LocalX=1 (right 32) — trunks on right half
        trunk_xs = (44, 50)

    for i, tx in enumerate(trunk_xs):
        draw_trunk_shaft(out, tx, 26, 118, half_w=2 if i == 0 else 1)

    paint_leaf_bottom_block(out, 100, trunk_xs, y_extent=16)
    paint_leaf_bottom_block(out, 70, trunk_xs, y_extent=8)
    return out


def sim_stack(cells, local_x, path, h=14):
    sheet = Image.new("RGBA", (40, 32 * h + 8), (12, 28, 12, 255))
    for y in range(h):
        ly = 0 if y == 0 else (3 if y == h - 1 else 1 + (y % 2))
        sheet.paste(cells[(local_x, ly)], (4, 4 + (h - 1 - y) * 32))
    sheet.save(path)


def main():
    os.makedirs(PREV, exist_ok=True)
    os.makedirs(TILE, exist_ok=True)
    bg = Image.open(BG).convert("RGBA")
    fill = Image.open(os.path.join(BASE, "tree_fill_64.png")).convert("RGBA")

    edge_l = build_edge(best_y(bg, 20, 96, 128), fill, True)
    edge_r = build_edge(best_y(bg, 710, 96, 128), fill, False)
    fringe_l = build_fringe(best_y(bg, 30, 64, 128), fill, True)
    fringe_r = build_fringe(best_y(bg, 740, 64, 128), fill, False)

    edge_l.save(os.path.join(BASE, "tree_edge_left_96x128.png"))
    edge_r.save(os.path.join(BASE, "tree_edge_right_96x128.png"))
    fringe_l.save(os.path.join(BASE, "tree_fringe_left_64x128.png"))
    fringe_r.save(os.path.join(BASE, "tree_fringe_right_64x128.png"))

    def slice_cells(im, cols, rows):
        cells = {}
        for ty in range(rows):
            for tx in range(cols):
                y0 = im.height - (ty + 1) * 32
                cells[(tx, ty)] = im.crop((tx * 32, y0, (tx + 1) * 32, y0 + 32))
        return cells

    sim_stack(slice_cells(edge_l, 3, 4), 0, os.path.join(PREV, "_sim_edge_localy.png"))
    sim_stack(slice_cells(fringe_l, 2, 4), 0, os.path.join(PREV, "_sim_fringe_localy.png"))

    for fname, cols, rows, prefix in [
        ("tree_fill_64.png", 2, 2, "fill"),
        ("tree_edge_left_96x128.png", 3, 4, "edge_left"),
        ("tree_edge_right_96x128.png", 3, 4, "edge_right"),
        ("tree_fringe_left_64x128.png", 2, 4, "fringe_left"),
        ("tree_fringe_right_64x128.png", 2, 4, "fringe_right"),
    ]:
        src = Image.open(os.path.join(BASE, fname)).convert("RGBA")
        for ty in range(rows):
            for tx in range(cols):
                y0 = src.height - (ty + 1) * 32
                src.crop((tx * 32, y0, (tx + 1) * 32, y0 + 32)).save(
                    os.path.join(TILE, f"{prefix}_{tx}_{ty}.png")
                )
    print("ok")


if __name__ == "__main__":
    main()
