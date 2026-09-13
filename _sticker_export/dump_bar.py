from PIL import Image
im = Image.open(r"C:\Users\윤성원\.cursor\projects\d-Unity-Projects-DungeonFront\assets\c__Users_____AppData_Roaming_Cursor_User_workspaceStorage_686c59cf4a3c28a0637822cdac2b2458_images_DF_MainImage__2_-edf36d99-89df-43d6-bcce-7cc54630e247.png").convert("RGB")
px=im.load()
print("y100 x390", px[390,100], "x402", px[402,100], "x420", px[420,100], "x500", px[500,100], "x530", px[530,100], "x560", px[560,100])
print("y87 x520", px[520,87], px[540,87], px[560,87])
