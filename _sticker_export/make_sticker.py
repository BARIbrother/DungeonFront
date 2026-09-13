from collections import deque
from PIL import Image, ImageFilter, ImageChops

SRC = r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
OUT_DIR = r"d:\Unity\Projects\DungeonFront\_sticker_export"

# 84mm at 300dpi
MM = 84.0
DPI = 300
CANVAS = int(round(MM / 25.4 * DPI))  # 992
# keep ~2mm bleed inside 84mm; character fills ~76mm height (safety) wait:
# organizer cuts 80mm; we want character as large as possible inside 80mm
# Place character in 80mm (945px) with small padding, canvas 84mm with transparent bleed.

SAFE_MM = 72.0  # leave room for a thicker white rim inside 80mm trim
SAFE_PX = int(round(SAFE_MM / 25.4 * DPI))


def color_dist(p, ref=(32, 32, 32)):
    return abs(p[0] - ref[0]) + abs(p[1] - ref[1]) + abs(p[2] - ref[2])


def is_grayish(p, spread=14):
    return max(p[0], p[1], p[2]) - min(p[0], p[1], p[2]) <= spread


def repair_head(im):
    """Fill the notched top-left of the hair into a solid block."""
    px = im.load()
    hair = px[450, 130]
    for y in range(84, 120):
        for x in range(374, 528):
            px[x, y] = (0, 0, 0, 255)
    for y in range(120, 150):
        for x in range(374, 410):
            r, g, b, a = px[x, y]
            luma = (r + g + b) / 3.0
            if luma < 50 or (abs(r - 32) + abs(g - 32) + abs(b - 32) <= 24):
                px[x, y] = (0, 0, 0, 255)
    for y in range(120, 148):
        for x in range(410, 500):
            r, g, b, a = px[x, y]
            if r < 20 and g < 20 and b < 20:
                px[x, y] = hair
    return im


def flood_bg(im, thresh):
    w, h = im.size
    px = im.load()
    vis = bytearray(w * h)
    q = deque()

    def try_push(x, y):
        i = y * w + x
        if vis[i]:
            return
        p = px[x, y]
        luma = (p[0] + p[1] + p[2]) / 3.0
        # Keep dark outline fringes; only true mid-gray canvas is background.
        if 26 <= luma <= 42 and color_dist(p) <= thresh and is_grayish(p):
            vis[i] = 1
            q.append((x, y))

    for x in range(w):
        try_push(x, 0)
        try_push(x, h - 1)
    for y in range(h):
        try_push(0, y)
        try_push(w - 1, y)
    while q:
        x, y = q.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h:
                try_push(nx, ny)
    return vis


def bbox_from_mask(vis, w, h):
    minx, miny, maxx, maxy = w, h, -1, -1
    for y in range(h):
        row = y * w
        for x in range(w):
            if not vis[row + x]:
                if x < minx:
                    minx = x
                if y < miny:
                    miny = y
                if x > maxx:
                    maxx = x
                if y > maxy:
                    maxy = y
    return minx, miny, maxx, maxy


def fill_neck_gap(im):
    """Fill canvas pixels in the notches between head and shoulders."""
    px = im.load()
    black = (0, 0, 0, 255)

    def canvas(p):
        r, g, b, a = p
        return abs(r - 32) + abs(g - 32) + abs(b - 32) <= 24 and max(r, g, b) - min(r, g, b) <= 14

    for y in range(372, 466):
        for x in range(261, 320):
            if canvas(px[x, y]):
                px[x, y] = black
        for x in range(724, 753):
            if canvas(px[x, y]):
                px[x, y] = black
    return im


def close_neck_mask(vis, w, h):
    """Close small silhouette gaps at the neck only."""
    y0, y1 = 350, 470
    img = Image.new("L", (w, h), 0)
    p = img.load()
    for y in range(y0, y1):
        row = y * w
        for x in range(w):
            if not vis[row + x]:
                p[x, y] = 255
    band = img.crop((0, y0, w, y1))
    for _ in range(14):
        band = band.filter(ImageFilter.MaxFilter(3))
    for _ in range(14):
        band = band.filter(ImageFilter.MinFilter(3))
    img.paste(band, (0, y0))
    p = img.load()
    for y in range(y0, y1):
        row = y * w
        for x in range(w):
            if p[x, y] > 128:
                vis[row + x] = 0
    return vis


def keep_largest_fg(vis, w, h):
    """Drop stray pixels (sparkle). vis==0 is foreground."""
    seen = bytearray(w * h)
    best = None
    best_n = 0
    for y in range(h):
        for x in range(w):
            i = y * w + x
            if vis[i] or seen[i]:
                continue
            q = deque([(x, y)])
            seen[i] = 1
            cells = [(x, y)]
            while q:
                cx, cy = q.popleft()
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if 0 <= nx < w and 0 <= ny < h:
                        ni = ny * w + nx
                        if not vis[ni] and not seen[ni]:
                            seen[ni] = 1
                            q.append((nx, ny))
                            cells.append((nx, ny))
            if len(cells) > best_n:
                best_n = len(cells)
                best = cells
    new_vis = bytearray(b"\x01" * (w * h))
    for x, y in best:
        new_vis[y * w + x] = 0
    print("kept fg pixels", best_n)
    return new_vis


def make_rgba(im, vis):
    w, h = im.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    src = im.load()
    dst = out.load()
    for y in range(h):
        row = y * w
        for x in range(w):
            if not vis[row + x]:
                p = src[x, y]
                if abs(p[0] - 32) + abs(p[1] - 32) + abs(p[2] - 32) <= 24:
                    dst[x, y] = (0, 0, 0, 255)
                else:
                    dst[x, y] = p
    return out


def add_white_outline(im, radius):
    """Dilate binary alpha into a solid white rim without 1px holes."""
    alpha = im.split()[3].point(lambda p: 255 if p >= 128 else 0)
    glow = alpha
    steps = max(1, (radius + 1) // 2)
    for _ in range(steps):
        glow = glow.filter(ImageFilter.MaxFilter(5))
    # Close pinholes that MaxFilter can leave on diagonal stairs.
    glow = glow.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))
    white = Image.new("RGBA", im.size, (255, 255, 255, 255))
    outlined = Image.new("RGBA", im.size, (0, 0, 0, 0))
    outlined.paste(white, mask=glow)
    outlined.alpha_composite(im)
    return outlined


def main():
    im = Image.open(SRC).convert("RGBA")
    im = repair_head(im)
    im = fill_neck_gap(im)
    w, h = im.size
    chosen = None
    for t in (16, 20, 24, 28, 32, 36):
        vis = flood_bg(im, t)
        n = sum(vis)
        box = bbox_from_mask(vis, w, h)
        print("T", t, "bg_frac", round(n / (w * h), 3), "bbox", box, "wh", box[2] - box[0] + 1, box[3] - box[1] + 1)
        chosen = (t, vis, box)

    t, vis, box = chosen
    # prefer T that keeps a reasonable character (not full canvas, not tiny)
    best = None
    for t in (16, 20, 24, 28, 32, 36):
        vis = flood_bg(im, t)
        box = bbox_from_mask(vis, w, h)
        bw, bh = box[2] - box[0] + 1, box[3] - box[1] + 1
        if 200 < bw < 900 and 300 < bh < 1000:
            best = (t, vis, box)
    if best is None:
        best = chosen
    t, vis, box = best
    vis = keep_largest_fg(vis, w, h)
    vis = close_neck_mask(vis, w, h)
    box = bbox_from_mask(vis, w, h)
    print("using T", t, box)

    cut = make_rgba(im, vis)
    minx, miny, maxx, maxy = box
    pad = 4
    minx = max(0, minx - pad)
    miny = max(0, miny - pad)
    maxx = min(w - 1, maxx + pad)
    maxy = min(h - 1, maxy + pad)
    cropped = cut.crop((minx, miny, maxx + 1, maxy + 1))

    # scale to fit SAFE_PX height (character fills printable area)
    cw, ch = cropped.size
    scale = min(SAFE_PX / cw, SAFE_PX / ch)
    nw, nh = max(1, int(round(cw * scale))), max(1, int(round(ch * scale)))
    scaled = cropped.resize((nw, nh), Image.Resampling.NEAREST)

    # white sticker rim ~2.2mm at 300dpi
    rim = max(6, int(round(2.2 / 25.4 * DPI)))
    # pad for outline
    padded = Image.new("RGBA", (nw + rim * 2, nh + rim * 2), (0, 0, 0, 0))
    padded.paste(scaled, (rim, rim), scaled)
    outlined = add_white_outline(padded, rim)

    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    ox = (CANVAS - outlined.size[0]) // 2
    oy = (CANVAS - outlined.size[1]) // 2
    canvas.alpha_composite(outlined, (ox, oy))
    canvas.info["dpi"] = (DPI, DPI)

    out_path = f"{OUT_DIR}/dungeonfront_sticker_84mm_300dpi.png"
    canvas.save(out_path, "PNG", dpi=(DPI, DPI))
    print("saved", out_path, canvas.size, "dpi", DPI)
    print("character placed", outlined.size, "at", ox, oy)

    # also save a green-sheet preview on lime so silhouette is obvious
    preview = Image.new("RGBA", (CANVAS, CANVAS), (180, 230, 140, 255))
    preview.alpha_composite(canvas)
    preview.save(f"{OUT_DIR}/_preview_on_green.png", "PNG")
    print("preview saved")


if __name__ == "__main__":
    main()
