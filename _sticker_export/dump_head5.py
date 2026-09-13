from PIL import Image

im = Image.open(
    r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
).convert("RGB")
px = im.load()

for y in (118, 119, 120, 121, 145, 146, 147):
    xs = []
    for x in range(250, 520):
        p = px[x, y]
        xs.append((x, p))
    # first non-bg
    for x, p in xs:
        if abs(p[0]-32)+abs(p[1]-32)+abs(p[2]-32) > 20:
            print("y", y, "first", x, p)
            break
    # first brown
    for x, p in xs:
        if p[0] > 60 and p[0] > p[1]:
            print("  first brown", x, p)
            break
