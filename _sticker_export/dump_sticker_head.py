from PIL import Image

st = Image.open(r"d:\Unity\Projects\DungeonFront\_sticker_export\dungeonfront_sticker_84mm_300dpi.png")
px = st.load()
w, h = st.size

# find top of opaque
for y in range(h):
    xs = [x for x in range(w) if px[x, y][3] > 20]
    if xs:
        print("top y", y, "x", min(xs), max(xs))
        break

y0 = y
x0 = min(xs)
print("map from", x0, y0)
for y in range(y0, y0 + 40):
    row = []
    for x in range(x0, x0 + 50):
        a = px[x, y][3]
        r, g, b, _ = px[x, y]
        if a < 20:
            row.append(".")
        elif r > 240 and g > 240 and b > 240:
            row.append("W")
        elif r < 40 and g < 40 and b < 40:
            row.append("#")
        elif r > 70:
            row.append("H")
        else:
            row.append("?")
    print(f"{y:3d}{''.join(row)}")
