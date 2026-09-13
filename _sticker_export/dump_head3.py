from PIL import Image

im = Image.open(
    r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
).convert("RGB")
px = im.load()

def ch(p):
    r,g,b = p
    if abs(r-32)+abs(g-32)+abs(b-32) <= 18 and max(p)-min(p) <= 12:
        return "."
    if r < 20 and g < 20 and b < 20:
        return "#"
    if r > 80 and r > g + 10:
        return "H"
    return "?"

# downsample mentally: print every pixel in a small window
print("x  360+")
header = "".join(str((x//10)%10) for x in range(360, 430))
print("   "+header)
for y in range(82, 145):
    row = "".join(ch(px[x,y]) for x in range(360, 430))
    print(f"{y:3d}{row}")
