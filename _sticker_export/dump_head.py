from PIL import Image

im = Image.open(
    r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
).convert("RGB")
px = im.load()

def is_bg(p):
    return abs(p[0]-32)+abs(p[1]-32)+abs(p[2]-32) <= 24 and max(p)-min(p) <= 14

# first non-bg rows
for y in range(70, 140):
    xs = [x for x in range(200, 400) if not is_bg(px[x, y])]
    if xs:
        print("y", y, "x", min(xs), max(xs), "n", len(xs), "sample", px[xs[0], y], px[xs[len(xs)//2], y])

print("--- vertical at x=250")
for y in range(80, 160):
    p = px[250, y]
    if not is_bg(p):
        print(y, p)
print("--- x=270")
for y in range(80, 160):
    p = px[270, y]
    if not is_bg(p):
        print(y, p)
