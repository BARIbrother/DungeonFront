#!/usr/bin/env python3
"""Wire TreeZoneStampSet.asset from ZoneTemplates keys + tile PNG metas."""

import os
import re
import uuid

ROOT = r"d:\Unity\Projects\DungeonFront"
TILE_FOLDER = os.path.join(ROOT, "Assets", "Art", "Background", "Tiles", "Tree")
TEMPLATE_FOLDER = os.path.join(TILE_FOLDER, "ZoneTemplates")
STAMP_PATH = os.path.join(ROOT, "Assets", "Data", "TreeZoneStampSet.asset")
STAMP_META = STAMP_PATH + ".meta"
SCENE_PATH = os.path.join(ROOT, "Assets", "Scenes", "ProductionScene.unity")

# Will be filled after Unity creates .cs.meta; fallback random for first write.
SCRIPT_META = os.path.join(ROOT, "Assets", "Scripts", "Grid", "TreeZoneStampSet.cs.meta")
ZONE = 16
MASKS = 16


def read_guid(meta_path: str) -> str:
    with open(meta_path, "r", encoding="utf-8") as f:
        for line in f:
            if line.startswith("guid:"):
                return line.split(":", 1)[1].strip()
    raise ValueError(f"no guid in {meta_path}")


def ensure_png_meta(png_path: str) -> str:
    meta = png_path + ".meta"
    if os.path.isfile(meta) and os.path.getsize(meta) > 0:
        try:
            return read_guid(meta)
        except ValueError:
            pass
    guid = uuid.uuid4().hex
    text = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spritePixelsToUnits: 32
  textureType: 8
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteMeshType: 1
  alphaIsTransparency: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {guid[:16]}0800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    return guid


def sprite_ref(guid: str) -> str:
    return f"  - {{fileID: 21300000, guid: {guid}, type: 3}}"


def load_keys(mask: int) -> list[str]:
    path = os.path.join(TEMPLATE_FOLDER, f"stamp_{mask:02d}.keys.txt")
    cells = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            cells.extend(line.split())
    if len(cells) != ZONE * ZONE:
        raise ValueError(f"{path} expected {ZONE*ZONE} keys, got {len(cells)}")
    return cells


def main() -> None:
    if os.path.isfile(SCRIPT_META):
        script_guid = read_guid(SCRIPT_META)
    else:
        script_guid = "b1c2d3e4f5a6478890abcdef12345678"
        print(f"WARN: TreeZoneStampSet.cs.meta missing, using placeholder guid={script_guid}")

    refs = []
    for mask in range(MASKS):
        for key in load_keys(mask):
            png = os.path.join(TILE_FOLDER, f"{key}.png")
            if not os.path.isfile(png):
                raise FileNotFoundError(png)
            guid = ensure_png_meta(png)
            refs.append(sprite_ref(guid))

    asset_guid = uuid.uuid4().hex
    yaml = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name: TreeZoneStampSet
  m_EditorClassIdentifier: Assembly-CSharp::TreeZoneStampSet
  stampSprites:
{chr(10).join(refs)}
"""
    os.makedirs(os.path.dirname(STAMP_PATH), exist_ok=True)
    with open(STAMP_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write(yaml)
    with open(STAMP_META, "w", encoding="utf-8", newline="\n") as f:
        f.write(
            f"""fileFormatVersion: 2
guid: {asset_guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        )

    # Wire scene
    with open(SCENE_PATH, "r", encoding="utf-8") as f:
        scene = f.read()
    ref = f"  treeZoneStampSet: {{fileID: 11400000, guid: {asset_guid}, type: 2}}\n"
    if "treeZoneStampSet:" in scene:
        scene = re.sub(r"  treeZoneStampSet: .*?\n", ref, scene, count=1)
    else:
        scene = scene.replace(
            "  m_EditorClassIdentifier: Assembly-CSharp::GridTilemapRenderer\n",
            "  m_EditorClassIdentifier: Assembly-CSharp::GridTilemapRenderer\n" + ref,
            1,
        )
    with open(SCENE_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write(scene)

    print(f"TreeZoneStampSet guid={asset_guid} sprites={len(refs)}")


if __name__ == "__main__":
    main()
