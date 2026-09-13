from PIL import Image

im = Image.open(
    r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
).convert("RGB")
px = im.load()

def ch(p):
    r,g,b = p
    if abs(r-32)+abs(g-32)+abs(b-32) <= 18 and max(p)-min(p) <= 12:
        return "."
    if r < 28 and g < 28 and b < 28:
        return "#"
    if r >= g and r >= 50:
        return "H"
    if g > r and g > 80:
        return "E"  # goggles/eyes teal
    return "?"

print("x  370+")
for y in range(84, 200, 2):
    row = "".join(ch(px[x,y]) for x in range(370, 560, 2))
    print(f"{y:3d}{row}")

# unique colors in the left spike box
from collections import Counter
c = Counter()
for y in range(86, 120):
    for x in range(377, 400):
        c[px[x,y]] += 1
print("spike colors", c.most_common(8))
