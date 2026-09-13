from PIL import Image

st = Image.open(r"d:\Unity\Projects\DungeonFront\_sticker_export\dungeonfront_sticker_84mm_300dpi.png")
px = st.load()
w, h = st.size

print("left edge y=250-450")
for y in range(250, 460, 2):
    xs = [x for x in range(w) if px[x, y][3] > 20]
    if not xs:
        continue
    print(f"{y:3d} {min(xs):4d}-{max(xs):4d} span={max(xs)-min(xs)+1}")
