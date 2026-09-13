from PIL import Image

im = Image.open(
    r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
).convert("RGB")
px = im.load()

def ch(p):
    r,g,b=p
    if abs(r-32)+abs(g-32)+abs(b-32) <= 18 and max(p)-min(p) <= 12:
        return "."
    if r < 28 and g < 28 and b < 28:
        return "#"
    if r > 180 and g > 150:
        return "S"  # skin/shirt light
    if 40 < r < 90 and g < 80:
        return "n"  # neck/brown dark
    if g > r + 20:
        return "G"
    if r > 90:
        return "H"
    return "?"

print("x 400-620 step2")
for y in range(360, 430, 2):
    row = "".join(ch(px[x,y]) for x in range(400, 620, 2))
    print(f"{y:3d}{row}")

# also sticker alpha gap
st = Image.open(r"d:\Unity\Projects\DungeonFront\_sticker_export\dungeonfront_sticker_84mm_300dpi.png")
sp = st.load()
print("\nsticker alpha along center x=496")
for y in range(280, 420):
    a = sp[496, y][3]
    if a < 200:
        print(y, sp[496,y])
