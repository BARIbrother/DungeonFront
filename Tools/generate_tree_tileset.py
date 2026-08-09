#!/usr/bin/env python3
"""Generate TreeBorderTileSet.asset and wire ProductionScene from slice PNG metas."""

import os
import re
import uuid

ROOT = r"d:\Unity\Projects\DungeonFront"
TILE_FOLDER = os.path.join(ROOT, "Assets", "Art", "Background", "Tiles", "Tree")
TILESET_PATH = os.path.join(ROOT, "Assets", "Data", "TreeBorderTileSet.asset")
TILESET_META = TILESET_PATH + ".meta"
SCENE_PATH = os.path.join(ROOT, "Assets", "Scenes", "ProductionScene.unity")
TILESET_SCRIPT_GUID = "cda3efdb3fac5684896bb540994065d6"
RENDERER_SCRIPT_GUID = "180f4ace787ca6f4dbbdc97ee681e575"

SLICES = [
    (2, 2, "fill", "fill2x2Sprites"),
    (3, 4, "edge_left", "edgeLeft3x4Sprites"),
    (3, 4, "edge_right", "edgeRight3x4Sprites"),
    (2, 4, "fringe_left", "fringeLeft2x4Sprites"),
    (2, 4, "fringe_right", "fringeRight2x4Sprites"),
]


def ensure_meta(meta_path: str) -> str:
    if os.path.isfile(meta_path) and os.path.getsize(meta_path) > 0:
        return read_guid(meta_path)

    guid = uuid.uuid4().hex
    png_name = os.path.basename(meta_path).replace(".meta", "")
    meta = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
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
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  spritePackingTag: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)
    return guid


def fix_slice_meta(meta_path: str) -> None:
    if not os.path.isfile(meta_path) or os.path.getsize(meta_path) == 0:
        return
    with open(meta_path, "r", encoding="utf-8") as f:
        text = f.read()
    text = re.sub(r"filterMode: 1", "filterMode: 0", text)
    text = re.sub(r"spritePixelsToUnits: 100", "spritePixelsToUnits: 32", text)
    text = re.sub(r"spriteMode: 2", "spriteMode: 1", text)
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def read_guid(meta_path: str) -> str:
    with open(meta_path, "r", encoding="utf-8") as f:
        for line in f:
            if line.startswith("guid:"):
                return line.split(":", 1)[1].strip()
    raise ValueError(f"no guid in {meta_path}")


def sprite_ref(guid: str) -> str:
    return f"  - {{fileID: 21300000, guid: {guid}, type: 3}}"


def build_tileset_yaml() -> tuple[str, str]:
    sections = []
    for cols, rows, prefix, field in SLICES:
        lines = [f"  {field}:"]
        for ty in range(rows):
            for tx in range(cols):
                png = os.path.join(TILE_FOLDER, f"{prefix}_{tx}_{ty}.png")
                meta = png + ".meta"
                if not os.path.isfile(png):
                    raise FileNotFoundError(png)
                guid = ensure_meta(meta)
                fix_slice_meta(meta)
                lines.append(sprite_ref(guid))
        sections.append("\n".join(lines))

    tileset_guid = uuid.uuid4().hex
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
  m_Script: {{fileID: 11500000, guid: {TILESET_SCRIPT_GUID}, type: 3}}
  m_Name: TreeBorderTileSet
  m_EditorClassIdentifier: Assembly-CSharp::TreeBorderTileSet
{chr(10).join(sections)}
"""
    meta = f"""fileFormatVersion: 2
guid: {tileset_guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    return yaml, meta, tileset_guid


def wire_scene(tileset_guid: str) -> None:
    with open(SCENE_PATH, "r", encoding="utf-8") as f:
        scene = f.read()

    ref = f"  treeBorderTileSet: {{fileID: 11400000, guid: {tileset_guid}, type: 2}}\n"
    if "treeBorderTileSet:" in scene:
        scene = re.sub(
            r"  treeBorderTileSet: .*?\n",
            ref,
            scene,
            count=1,
        )
    else:
        scene = scene.replace(
            "  m_EditorClassIdentifier: Assembly-CSharp::GridTilemapRenderer\n",
            "  m_EditorClassIdentifier: Assembly-CSharp::GridTilemapRenderer\n" + ref,
            1,
        )

    with open(SCENE_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write(scene)


def main() -> None:
    os.makedirs(os.path.dirname(TILESET_PATH), exist_ok=True)
    yaml, meta, tileset_guid = build_tileset_yaml()
    with open(TILESET_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write(yaml)
    with open(TILESET_META, "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)
    wire_scene(tileset_guid)
    print(f"TreeBorderTileSet guid={tileset_guid}")
    print("Done: asset + scene wired")


if __name__ == "__main__":
    main()
