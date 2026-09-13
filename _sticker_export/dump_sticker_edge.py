from PIL import Image

st = Image.open(r"d:\Unity\Projects\DungeonFront\_sticker_export\dungeonfront_sticker_84mm_300dpi.png")
px = st.load()
w, h = st.size

print("y minx maxx left-edge pattern")
prev = None
for y in range(36, 220):
    xs = [x for x in range(w) if px[x, y][3] > 20]
    if not xs:
        continue
    mn, mx = min(xs), max(xs)
    jump = "" if prev is None else f" dmin={mn-prev}"
    prev = mn
    print(f"{y:3d} {mn:4d}-{mx:4d}{jump}")
