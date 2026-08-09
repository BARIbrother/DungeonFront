from PIL import Image
import os
import random

BG = r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_bg-9c0f4dc8-553b-4fb3-b47e-7c5f77eff2f5.png"
BASE = r"d:\Unity\Projects\DungeonFront\Assets\Art\Background"
PREV = os.path.join(BASE, "_preview")
TILE = os.path.join(BASE, "Tiles", "Tree")

CANOPY_CROPS = [
    (0, 200, 96, 128),
    (0, 240, 96, 128),
    (8, 220, 96, 128),
    (0, 280, 96, 128),
]


def is_leaf(p):
    r, g, b, a = p[:4]
    return g > r + 10 and g > 75


def wrap_v_mse(tile):
    tw, th = tile.size
    err = n = 0
    for x in range(tw):
        a = tile.getpixel((x, th - 1))
        b = tile.getpixel((x, 0))
        err += sum((a[i] - b[i]) ** 2 for i in range(3))
        n += 3
    return (err / max(n, 1)) ** 0.5


def pick_canopy(im):
    best, best_e = None, 1e9
    for x, y, w, h in CANOPY_CROPS:
        t = im.crop((x, y, x + w, y + h))
        pix = t.load()
        bright = 0
        for yy in range(0, h, 2):
            for xx in range(12):
                r, g, b, a = pix[xx, yy]
                if g > 160 and r > 80 and b < 120:
                    bright += 1
        if bright > (12 * h // 2) * 0.35:
            continue
        e = wrap_v_mse(t)
        if e < best_e:
            best_e, best = e, t
    if best is None:
        x, y, w, h = CANOPY_CROPS[1]
        best = im.crop((x, y, x + w, y + h))
    return best


def feather_v(tile, band=5):
    out = tile.copy()
    pix = out.load()
    w, h = out.size
    for i in range(band):
        t = (i + 1) / (band + 1) * 0.5
        for x in range(w):
            a = pix[x, h - 1 - i]
            b = pix[x, i]
            pix[x, h - 1 - i] = tuple(int(a[c] * (1 - t) + b[c] * t) for c in range(3)) + (255,)
            pix[x, i] = tuple(int(b[c] * (1 - t) + a[c] * t) for c in range(3)) + (255,)
    return out


def deepen_under_leaves(tile):
    out = tile.copy()
    pix = out.load()
    w, h = out.size
    for x in range(w):
        y = 0
        while y < h:
            while y < h and not is_leaf(pix[x, y]):
                y += 1
            if y >= h:
                break
            while y < h and is_leaf(pix[x, y]):
                y += 1
            for dy in range(11):
                yy = y + dy
                if yy >= h or is_leaf(pix[x, yy]):
                    break
                r, g, b, a = pix[x, yy]
                if r > 55 and r > g + 8 and g < 95:
                    continue
                if r + g + b < 175:
                    f = 0.45 + dy * 0.04
                    pix[x, yy] = (int(r * f), int(g * f), int(b * f), 255)
    return out


def blend_with_fill(canopy: Image.Image, fill: Image.Image, forest_on_right: bool) -> Image.Image:
    """
    Crossfade canopy into fill on the forest-interior side so EdgeDepth=2
    doesn't show a hard vertical cut when LocalX advances inward.
    """
    out = canopy.copy()
    op = out.load()
    fp = fill.load()
    fw, fh = fill.size
    w, h = out.size
    # Interior third blends toward fill
    if forest_on_right:
        # grass face LEFT → blend RIGHT into fill
        x0, x1 = 56, 96
    else:
        x0, x1 = 0, 40
    span = max(1, x1 - x0)
    for y in range(h):
        for x in range(x0, x1):
            t = (x - x0) / span if forest_on_right else (x1 - 1 - x) / span
            t = t * t  # ease-in toward interior
            cur = op[x, y]
            src = fp[x % fw, y % fh]
            if is_leaf(cur) and t < 0.35:
                continue
            mix = 0.25 + 0.7 * t
            op[x, y] = tuple(int(cur[c] * (1 - mix) + src[c] * mix) for c in range(3)) + (255,)
    return out


def add_face_leaf_noise(tile: Image.Image, grass_on_left: bool, seed: int):
    """Break rigid silhouette with irregular 1–2px leaf tips toward grass."""
    out = tile.copy()
    pix = out.load()
    w, h = out.size
    rng = random.Random(seed)
    face = 0 if grass_on_left else w - 1
    step = 1 if grass_on_left else -1
    for y in range(h):
        found = None
        for i in range(w):
            x = face + step * i
            if is_leaf(pix[x, y]):
                found = x
                break
        if found is None:
            continue
        if rng.random() < 0.4:
            tip = found - step * rng.randint(1, 3)
            if 0 <= tip < w:
                pix[tip, y] = pix[found, y]
                if rng.random() < 0.5:
                    yy = min(h - 1, max(0, y + rng.choice([-1, 0, 1])))
                    pix[tip, yy] = pix[found, y]
    return out


def make_edge(canopy, fill, grass_on_left):
    src = canopy if not grass_on_left else canopy.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    # forest interior is opposite of grass face
    forest_on_right = grass_on_left
    out = blend_with_fill(src, fill, forest_on_right=forest_on_right)
    out = deepen_under_leaves(out)
    out = feather_v(out)
    out = add_face_leaf_noise(out, grass_on_left, seed=7 if grass_on_left else 13)
    return out


def make_fringe(edge, dark_left):
    fringe = edge.crop((0, 0, 64, 128) if dark_left else (32, 0, 96, 128))
    pix = fringe.load()
    w, h = fringe.size
    for y in range(h):
        for x in range(w):
            dist = x if dark_left else (w - 1 - x)
            if dist < 16:
                fade = 0.48 + 0.52 * (dist / 16.0)
                r, g, b, a = pix[x, y]
                pix[x, y] = (int(r * fade), int(g * fade), int(b * fade), 255)
    return fringe


def tile_preview(tile, n=3, scale=2, bg=(88, 168, 52, 255)):
    sheet = Image.new("RGBA", (tile.width + 12, tile.height * n), bg)
    for i in range(n):
        sheet.paste(tile, (6, i * tile.height))
    return sheet.resize((sheet.width * scale, sheet.height * scale), Image.Resampling.NEAREST)


def slice_tiles(src, cols, rows, prefix):
    for ty in range(rows):
        for tx in range(cols):
            y0 = src.height - (ty + 1) * 32
            src.crop((tx * 32, y0, (tx + 1) * 32, y0 + 32)).save(
                os.path.join(TILE, f"{prefix}_{tx}_{ty}.png")
            )


def main():
    os.makedirs(PREV, exist_ok=True)
    os.makedirs(TILE, exist_ok=True)
    im = Image.open(BG).convert("RGBA")
    fill = Image.open(os.path.join(BASE, "tree_fill_64.png")).convert("RGBA")
    canopy = pick_canopy(im)
    canopy.save(os.path.join(PREV, "_canopy_src.png"))

    edge_left = make_edge(canopy, fill, grass_on_left=True)
    edge_right = make_edge(canopy, fill, grass_on_left=False)
    fringe_left = make_fringe(edge_left, dark_left=True)
    fringe_right = make_fringe(edge_right, dark_left=False)

    edge_left.save(os.path.join(BASE, "tree_edge_left_96x128.png"))
    edge_right.save(os.path.join(BASE, "tree_edge_right_96x128.png"))
    fringe_left.save(os.path.join(BASE, "tree_fringe_left_64x128.png"))
    fringe_right.save(os.path.join(BASE, "tree_fringe_right_64x128.png"))

    tile_preview(edge_left).save(os.path.join(PREV, "tree_edge_left_tiled.png"))
    tile_preview(edge_right).save(os.path.join(PREV, "tree_edge_right_tiled.png"))
    tile_preview(fringe_left, bg=(18, 28, 18, 255)).save(os.path.join(PREV, "tree_fringe_left_tiled.png"))
    tile_preview(fringe_right, bg=(18, 28, 18, 255)).save(os.path.join(PREV, "tree_fringe_right_tiled.png"))

    cells = {}
    for ty in range(4):
        for tx in range(3):
            y0 = 128 - (ty + 1) * 32
            cells[(tx, ty)] = edge_left.crop((tx * 32, y0, (tx + 1) * 32, y0 + 32))
    hh = 10
    sheet = Image.new("RGBA", (72, 32 * hh + 8), (90, 170, 55, 255))
    for y in range(hh):
        ly = y % 4
        sheet.paste(cells[(0, ly)], (4, 4 + (hh - 1 - y) * 32))
        sheet.paste(cells[(1, ly)], (36, 4 + (hh - 1 - y) * 32))
    sheet.save(os.path.join(PREV, "_sim_edge_natural.png"))

    overview = Image.new("RGBA", (420, 160), (45, 45, 45, 255))
    overview.paste(edge_left, (8, 16))
    overview.paste(edge_right, (112, 16))
    overview.paste(fringe_left, (216, 16))
    overview.paste(fringe_right, (288, 16))
    overview.save(os.path.join(PREV, "_sheet_tree_tiles.png"))

    slice_tiles(fill, 2, 2, "fill")
    slice_tiles(edge_left, 3, 4, "edge_left")
    slice_tiles(edge_right, 3, 4, "edge_right")
    slice_tiles(fringe_left, 2, 4, "fringe_left")
    slice_tiles(fringe_right, 2, 4, "fringe_right")
    print("done")


if __name__ == "__main__":
    main()
