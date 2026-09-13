from PIL import Image

src = Image.open(
    r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png"
).convert("RGBA")
# bbox of character was roughly (233, 87, 778, 983)
crop = src.crop((220, 70, 420, 260))
crop = crop.resize((crop.size[0] * 4, crop.size[1] * 4), Image.Resampling.NEAREST)
crop.save(r"d:\Unity\Projects\DungeonFront\_sticker_export\_head_src_zoom.png")

st = Image.open(r"d:\Unity\Projects\DungeonFront\_sticker_export\dungeonfront_sticker_84mm_300dpi.png")
# character at ~210,36 size 572x920
head = st.crop((210, 30, 210 + 280, 30 + 220))
head.save(r"d:\Unity\Projects\DungeonFront\_sticker_export\_head_sticker_zoom.png")
print("src crop", crop.size, "sticker crop", head.size)
