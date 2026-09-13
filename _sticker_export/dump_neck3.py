from PIL import Image
im = Image.open(r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png").convert("RGB")
px=im.load()

def is_canvas(p):
    return abs(p[0]-32)+abs(p[1]-32)+abs(p[2]-32) <= 20 and max(p)-min(p) <= 12

print("LEFT y410-490 x230-400 canvas")
for y in range(410, 490):
    xs=[x for x in range(230, 400) if is_canvas(px[x,y])]
    if xs and not (xs[0]==230 and xs[-1]<=260 and len(xs)<=32):
        print(y, xs[0], xs[-1], 'n', len(xs), 'inner', [x for x in xs if x>260][:8])

print("RIGHT y400-490 x680-800")
for y in range(400, 490):
    xs=[x for x in range(680, 800) if is_canvas(px[x,y])]
    if xs:
        print(y, xs[0], xs[-1], 'n', len(xs))
