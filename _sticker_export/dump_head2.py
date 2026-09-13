from PIL import Image

im = Image.open(
    r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
).convert("RGB")
px = im.load()

def is_bg(p):
    return abs(p[0]-32)+abs(p[1]-32)+abs(p[2]-32) <= 24 and max(p)-min(p) <= 14

print("leftmost fg per band")
for y0 in range(80, 320, 8):
    minx = 9999
    for y in range(y0, y0+8):
        for x in range(150, 500):
            if not is_bg(px[x, y]):
                minx = min(minx, x)
    print(y0, minx)

# crop actual head: x 300-700, y 70-320
crop = im.crop((300, 70, 700, 340))
z = crop.resize((crop.size[0]*2, crop.size[1]*2), Image.NEAREST)
z.save(r"d:\Unity\Projects\DungeonFront\_sticker_export\_head_src_full.png")
print("saved full head", crop.size)
