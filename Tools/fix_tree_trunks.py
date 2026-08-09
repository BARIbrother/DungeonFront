from PIL import Image
import os

BASE = r"d:\Unity\Projects\DungeonFront\Assets\Art\Background"
PREV = os.path.join(BASE, "_preview")
TILE = os.path.join(BASE, "Tiles", "Tree")

TRUNK = (78, 48, 28, 255)
TRUNK_D = (52, 32, 18, 255)
TRUNK_H = (98, 62, 36, 255)
SHADOW = (28, 48, 28, 255)


def is_leaf(p):
    r, g, b, a = p
    return a > 200 and g > r + 15 and g > 70


def is_dark_gap(p):
    r, g, b, a = p
    return a > 200 and r + g + b < 120 and not (r > g + 10)


def is_brown(p):
    r, g, b, a = p
    return a > 200 and r > 45 and r > g + 8 and g < 100 and b < 80


def paint_trunk_under_leaves(im, col_x0, col_x1):
    pix = im.load()
    h = im.size[1]
    for x in range(col_x0, col_x1):
        y = 0
        while y < h:
            while y < h and not is_leaf(pix[x, y]):
                y += 1
            if y >= h:
                break
            while y < h and is_leaf(pix[x, y]):
                y += 1
            filled = 0
            while y < h and filled < 18:
                p = pix[x, y]
                if is_leaf(p):
                    break
                if is_dark_gap(p) or is_brown(p) or (p[0] + p[1] + p[2] < 160):
                    if filled < 2:
                        pix[x, y] = TRUNK_H
                    elif (x + y) % 3 == 0:
                        pix[x, y] = TRUNK_D
                    else:
                        pix[x, y] = TRUNK
                    filled += 1
                    y += 1
                else:
                    break


def clear_tips_in_middle(im, col_x0, col_x1):
    # localY1/2 = image y 32..95 — tip/base must not repeat here
    pix = im.load()
    for y in range(32, 96):
        for x in range(col_x0, col_x1):
            if is_brown(pix[x, y]):
                pix[x, y] = SHADOW


def process(name, trunk_cols):
    path = os.path.join(BASE, name)
    im = Image.open(path).convert("RGBA")
    for x0, x1 in trunk_cols:
        clear_tips_in_middle(im, x0, x1)
        paint_trunk_under_leaves(im, x0, x1)
    im.save(path)
    z = im.resize((im.width * 3, im.height * 3), Image.Resampling.NEAREST)
    z.save(os.path.join(PREV, "_fixed_" + name))
    print("fixed", name)


def main():
    os.makedirs(PREV, exist_ok=True)
    os.makedirs(TILE, exist_ok=True)

    # 잔디를 향한 바깥 열 + 줄기 열 모두 잎 아래를 줄기로 막는다.
    process("tree_edge_left_96x128.png", [(8, 56)])
    process("tree_edge_right_96x128.png", [(40, 88)])
    process("tree_fringe_left_64x128.png", [(8, 48)])
    process("tree_fringe_right_64x128.png", [(16, 56)])

    slices = [
        ("tree_fill_64.png", 2, 2, "fill"),
        ("tree_edge_left_96x128.png", 3, 4, "edge_left"),
        ("tree_edge_right_96x128.png", 3, 4, "edge_right"),
        ("tree_fringe_left_64x128.png", 2, 4, "fringe_left"),
        ("tree_fringe_right_64x128.png", 2, 4, "fringe_right"),
    ]
    for fname, cols, rows, prefix in slices:
        im = Image.open(os.path.join(BASE, fname)).convert("RGBA")
        for ty in range(rows):
            for tx in range(cols):
                y0 = im.height - (ty + 1) * 32
                im.crop((tx * 32, y0, (tx + 1) * 32, y0 + 32)).save(
                    os.path.join(TILE, f"{prefix}_{tx}_{ty}.png")
                )
    print("resliced")


if __name__ == "__main__":
    main()
