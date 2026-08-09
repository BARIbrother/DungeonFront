from PIL import Image
import os
import re
import uuid
import random

BG = r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_bg-9c0f4dc8-553b-4fb3-b47e-7c5f77eff2f5.png"
BASE = r"d:\Unity\Projects\DungeonFront\Assets\Art\Background"
OUT = os.path.join(BASE, "Tiles", "Floor")
PREV = os.path.join(BASE, "_preview")
SCENE = r"d:\Unity\Projects\DungeonFront\Assets\Scenes\ProductionScene.unity"

MARGIN = 4
MAX_SIDE = 18


def is_plain_grass(r, g, b):
    return g > 148 and r > 60 and b < 120 and abs(r - 88) + abs(g - 192) < 70


def is_flower(r, g, b):
    if r > 145 and g > 140 and b > 130:
        return True  # white
    if b > 110 and b >= r and 90 < g < 200:
        return True  # blue/lavender
    if r > 165 and 90 < g < 165 and b < 145:
        return True  # pink
    return False


def is_tuft(r, g, b):
    return 75 < g < 165 and r < 100 and b < 85 and not is_plain_grass(r, g, b)


def is_stem(r, g, b):
    return 45 < r < 130 and 35 < g < 110 and b < 85


def main():
    os.makedirs(OUT, exist_ok=True)
    im = Image.open(BG).convert("RGBA")
    plain = Image.open(os.path.join(BASE, "floor_grass_32.png")).convert("RGBA")
    plain_px = plain.load()

    for f in os.listdir(OUT):
        if f.startswith("floor_deco_") and (f.endswith(".png") or f.endswith(".meta")):
            os.remove(os.path.join(OUT, f))

    # Seed on flower pixels primarily; also compact tufts
    seeds = []
    for y in range(210, 490):
        for x in range(260, 690):
            r, g, b, a = im.getpixel((x, y))
            if is_flower(r, g, b) or is_tuft(r, g, b):
                seeds.append((x, y))

    seed_set = set(seeds)
    visited = set()
    motifs = []

    for sx, sy in seeds:
        if (sx, sy) in visited:
            continue
        stack = [(sx, sy)]
        visited.add((sx, sy))
        comp = []
        while stack:
            x, y = stack.pop()
            comp.append((x, y))
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    if dx == 0 and dy == 0:
                        continue
                    nx, ny = x + dx, y + dy
                    if (nx, ny) in visited:
                        continue
                    if not (250 <= nx < 710 and 200 <= ny < 510):
                        continue
                    r, g, b, a = im.getpixel((nx, ny))
                    if is_flower(r, g, b) or is_tuft(r, g, b) or is_stem(r, g, b):
                        visited.add((nx, ny))
                        stack.append((nx, ny))

        if len(comp) < 4 or len(comp) > 70:
            continue
        xs = [p[0] for p in comp]
        ys = [p[1] for p in comp]
        x0, y0, x1, y1 = min(xs), min(ys), max(xs), max(ys)
        bw, bh = x1 - x0 + 1, y1 - y0 + 1
        if bw > MAX_SIDE or bh > MAX_SIDE or bw < 2 or bh < 2:
            continue

        # Must be an island: 2px ring around bbox is mostly plain grass
        ring_ok = ring_bad = 0
        for y in range(y0 - 2, y1 + 3):
            for x in range(x0 - 2, x1 + 3):
                if x0 <= x <= x1 and y0 <= y <= y1:
                    continue
                if not (0 <= x < im.size[0] and 0 <= y < im.size[1]):
                    ring_bad += 1
                    continue
                r, g, b, a = im.getpixel((x, y))
                if is_plain_grass(r, g, b):
                    ring_ok += 1
                elif is_flower(r, g, b) or is_tuft(r, g, b):
                    ring_bad += 1
        if ring_bad > ring_ok * 0.15:
            continue  # attached to larger foliage — would look cut

        flower_n = sum(1 for x, y in comp if is_flower(*im.getpixel((x, y))[:3]))
        tuft_n = sum(1 for x, y in comp if is_tuft(*im.getpixel((x, y))[:3]))
        score = flower_n * 8 + tuft_n * 2 + len(comp)
        motifs.append((score, flower_n, x0, y0, x1, y1, comp))

    motifs.sort(reverse=True)
    picked = []
    for score, flower_n, x0, y0, x1, y1, comp in motifs:
        cx = (x0 + x1) * 0.5
        cy = (y0 + y1) * 0.5
        if any(abs(cx - pcx) < 16 and abs(cy - pcy) < 16 for pcx, pcy, *_ in picked):
            continue
        # Prefer at least some flower, or compact tuft
        if flower_n == 0 and len(comp) > 35:
            continue
        picked.append((cx, cy, x0, y0, x1, y1, comp, score, flower_n))
        if len(picked) >= 8:
            break

    print("picked", len(picked))
    if len(picked) < 4:
        print("WARNING: few motifs")

    rng = random.Random(23)
    guids = []
    sheet = Image.new("RGBA", (32 * max(8, len(picked)) + 20, 48), (20, 20, 20, 255))
    base_meta = open(os.path.join(BASE, "floor_grass_32.png.meta"), encoding="utf-8").read()

    for i, (_cx, _cy, x0, y0, x1, y1, comp, score, flower_n) in enumerate(picked):
        bw, bh = x1 - x0 + 1, y1 - y0 + 1
        max_ox = 32 - MARGIN - bw
        max_oy = 32 - MARGIN - bh
        if max_ox < MARGIN or max_oy < MARGIN:
            continue
        # Bias toward center so nothing hugs the edge
        ox = rng.randint(MARGIN, max_ox)
        oy = rng.randint(MARGIN, max_oy)
        # pull toward center
        ox = int(round((ox + (32 - bw) / 2) / 2))
        oy = int(round((oy + (32 - bh) / 2) / 2))
        ox = max(MARGIN, min(max_ox, ox))
        oy = max(MARGIN, min(max_oy, oy))

        out = plain.copy()
        op = out.load()
        for x, y in comp:
            r, g, b, a = im.getpixel((x, y))
            px = ox + (x - x0)
            py = oy + (y - y0)
            op[px, py] = (r, g, b, 255)

        # Force clear margin ring
        for y in range(32):
            for x in range(32):
                if x < MARGIN or x >= 32 - MARGIN or y < MARGIN or y >= 32 - MARGIN:
                    op[x, y] = plain_px[x, y]

        # Verify full motif still present (wasn't wiped by margin clear)
        surviving = 0
        for x, y in comp:
            px = ox + (x - x0)
            py = oy + (y - y0)
            if op[px, py] != plain_px[px, py]:
                surviving += 1
        if surviving < len(comp) * 0.95:
            print("skip wiped", i, surviving, len(comp))
            continue

        path = os.path.join(OUT, f"floor_deco_{len(guids)}.png")
        out.save(path)
        guid = uuid.uuid4().hex
        meta = re.sub(r"guid: .*", "guid: " + guid, base_meta, count=1)
        with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
            f.write(meta)
        guids.append(guid)
        sheet.paste(out, (10 + (len(guids) - 1) * 34, 8))
        print(len(guids) - 1, "bbox", bw, bh, "at", ox, oy, "flower", flower_n)

    sheet.resize((sheet.width * 4, sheet.height * 4), Image.Resampling.NEAREST).save(
        os.path.join(PREV, "_floor_deco_sheet.png")
    )
    with open(os.path.join(OUT, "_guids.txt"), "w", encoding="utf-8") as f:
        for g in guids:
            f.write(g + "\n")

    with open(SCENE, "r", encoding="utf-8") as f:
        text = f.read()
    lines = ["  floorDecorationSprites:"] + [
        f"  - {{fileID: 21300000, guid: {g}, type: 3}}" for g in guids
    ]
    block = "\n".join(lines) + "\n"
    text = re.sub(
        r"  floorDecorationSprites:.*?(?=\n  [a-zA-Z]|\n---)",
        block.rstrip() + "\n",
        text,
        count=1,
        flags=re.S,
    )
    with open(SCENE, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print("done", len(guids))


if __name__ == "__main__":
    main()
