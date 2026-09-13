from PIL import Image

im = Image.open(
    r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
).convert("RGB")
px = im.load()

def is_bg(p):
    return abs(p[0]-32)+abs(p[1]-32)+abs(p[2]-32) <= 24 and max(p)-min(p) <= 14

# scan center columns for bg runs between y=250 and 450
for y in range(250, 450):
    p = px[512, y]
    if is_bg(p):
        print("bg at 512,", y, p)

print("--- x range of fg at each y 300-420")
for y in range(300, 420, 4):
    xs = [x for x in range(300, 720) if not is_bg(px[x, y])]
    if xs:
        print(y, min(xs), max(xs), "n", len(xs), "mid", px[512,y])
    else:
        print(y, "ALL BG")
