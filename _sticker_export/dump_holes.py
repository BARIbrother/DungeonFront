from PIL import Image

st = Image.open(r"d:\Unity\Projects\DungeonFront\_sticker_export\dungeonfront_sticker_84mm_300dpi.png")
px = st.load()
w, h = st.size

print("internal holes")
for y in range(250, 480):
    inside = False
    hole0 = None
    holes = []
    for x in range(w):
        on = px[x, y][3] > 20
        if on:
            if hole0 is not None:
                holes.append((hole0, x - 1))
                hole0 = None
            inside = True
        elif inside and hole0 is None:
            hole0 = x
    if holes:
        print(y, holes)
