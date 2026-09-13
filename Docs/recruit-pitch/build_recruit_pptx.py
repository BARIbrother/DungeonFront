# -*- coding: utf-8 -*-
"""DungeonFront 팀원 모집 PPT — 8–10분 / 10슬라이드.

초반: 이 게임이 무엇인지.
후반: 새로 개발할 던전 컨텐츠.
"""

from __future__ import annotations

from pathlib import Path

from lxml import etree
from PIL import Image
from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.oxml.ns import qn
from pptx.util import Inches, Pt

NN = Image.Resampling.NEAREST

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
ART = REPO / "Assets" / "Art"
OUT_PPTX = HERE / "DungeonFront_recruit_pitch.pptx"
COMPOSITES = HERE / "_composites"

FONT = "Malgun Gothic"

# Parchment workshop palette
INK = RGBColor(0x2B, 0x21, 0x18)
INK_SOFT = RGBColor(0x6A, 0x58, 0x48)
CREAM = RGBColor(0xF6, 0xEF, 0xD9)
CREAM_DEEP = RGBColor(0xEB, 0xDC, 0xB8)
CARD = RGBColor(0xFF, 0xF8, 0xEA)
LEATHER = RGBColor(0x3D, 0x2A, 0x22)
LEATHER_DEEP = RGBColor(0x24, 0x18, 0x12)
GOLD = RGBColor(0xC4, 0xA3, 0x5A)
RUST = RGBColor(0xA8, 0x5A, 0x2A)
LEAF = RGBColor(0x4F, 0x7A, 0x45)
MAGIC = RGBColor(0x6B, 0x4A, 0x8A)
WHITE = RGBColor(0xFF, 0xF8, 0xEA)
PILL_BG = RGBColor(0x5A, 0x3A, 0x2A)

SLIDE_W = Inches(13.333)
SLIDE_H = Inches(7.5)


def set_run_font(run, size=18, bold=False, color=INK, name=FONT):
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.name = name
    if color is not None:
        run.font.color.rgb = color
    r_pr = run._r.get_or_add_rPr()
    for tag in ("latin", "ea", "cs"):
        el = r_pr.find(qn(f"a:{tag}"))
        if el is None:
            el = etree.SubElement(r_pr, qn(f"a:{tag}"))
        el.set("typeface", name)


def add_text(
    slide,
    left,
    top,
    width,
    height,
    text,
    size=18,
    bold=False,
    color=INK,
    align=PP_ALIGN.LEFT,
    anchor=MSO_ANCHOR.TOP,
):
    box = slide.shapes.add_textbox(Inches(left), Inches(top), Inches(width), Inches(height))
    tf = box.text_frame
    tf.word_wrap = True
    tf.auto_size = None
    tf.paragraphs[0].alignment = align
    try:
        tf._txBody.bodyPr.set("anchor", {MSO_ANCHOR.TOP: "t", MSO_ANCHOR.MIDDLE: "ctr", MSO_ANCHOR.BOTTOM: "b"}[anchor])
    except Exception:
        pass
    p = tf.paragraphs[0]
    run = p.add_run()
    run.text = text
    set_run_font(run, size, bold, color)
    return box


def add_lines(
    slide,
    left,
    top,
    width,
    height,
    lines,
    size=16,
    bold=False,
    color=INK,
    align=PP_ALIGN.LEFT,
    spacing=1.15,
):
    box = slide.shapes.add_textbox(Inches(left), Inches(top), Inches(width), Inches(height))
    tf = box.text_frame
    tf.word_wrap = True
    for i, line in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = align
        p.space_after = Pt(6)
        try:
            p.line_spacing = spacing
        except Exception:
            pass
        run = p.add_run()
        if isinstance(line, tuple):
            run.text, line_bold, line_color, line_size = line
            set_run_font(run, line_size, line_bold, line_color)
        else:
            run.text = line
            set_run_font(run, size, bold, color)
    return box


def card(slide, left, top, width, height, fill=CARD, line=None, radius=0.06):
    shape = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE,
        Inches(left),
        Inches(top),
        Inches(width),
        Inches(height),
    )
    shape.fill.solid()
    shape.fill.fore_color.rgb = fill
    if line is None:
        shape.line.fill.background()
    else:
        shape.line.color.rgb = line
        shape.line.width = Pt(1.15)
    try:
        shape.adjustments[0] = radius
    except Exception:
        pass
    shape.shadow.inherit = False
    return shape


def rect(slide, left, top, width, height, fill, line=None):
    shape = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE,
        Inches(left),
        Inches(top),
        Inches(width),
        Inches(height),
    )
    shape.fill.solid()
    shape.fill.fore_color.rgb = fill
    if line is None:
        shape.line.fill.background()
    else:
        shape.line.color.rgb = line
        shape.line.width = Pt(1)
    shape.shadow.inherit = False
    return shape


def pill(slide, left, top, width, height, text, fill=PILL_BG, color=WHITE, size=12):
    card(slide, left, top, width, height, fill=fill, radius=0.5)
    add_text(slide, left, top, width, height, text, size=size, bold=True, color=color, align=PP_ALIGN.CENTER, anchor=MSO_ANCHOR.MIDDLE)


def add_pic(slide, path, left, top, width=None, height=None):
    kwargs = {}
    if width is not None:
        kwargs["width"] = Inches(width)
    if height is not None:
        kwargs["height"] = Inches(height)
    return slide.shapes.add_picture(str(path), Inches(left), Inches(top), **kwargs)


def set_notes(slide, text: str):
    ns = slide.notes_slide
    tf = ns.notes_text_frame
    tf.text = text
    for p in tf.paragraphs:
        for run in p.runs:
            set_run_font(run, 14, False, INK)


def chrome(slide, chapter: str, title: str, num: int, dark=False):
    bg = LEATHER_DEEP if dark else CREAM
    rect(slide, 0, 0, 13.333, 7.5, bg)
    if dark:
        rect(slide, 0, 0, 13.333, 0.08, GOLD)
        rect(slide, 0, 7.42, 13.333, 0.08, GOLD)
        add_text(slide, 0.5, 0.16, 9, 0.28, chapter, 11, False, GOLD)
        add_text(slide, 11.2, 7.12, 1.6, 0.28, f"{num} / 10", 11, False, GOLD, PP_ALIGN.RIGHT)
        return
    rect(slide, 0, 0, 13.333, 0.46, LEATHER)
    add_text(slide, 0.45, 0.08, 8.5, 0.32, chapter, 12, False, GOLD, anchor=MSO_ANCHOR.MIDDLE)
    add_text(slide, 10.4, 0.08, 2.45, 0.32, f"{num} / 10", 12, False, CREAM, PP_ALIGN.RIGHT, MSO_ANCHOR.MIDDLE)
    rect(slide, 0.45, 7.22, 12.43, 0.015, GOLD)
    add_text(slide, 0.45, 7.24, 8, 0.22, "DungeonFront  ·  게임 소개 + 새 던전", 10, False, INK_SOFT)
    add_text(slide, 9.5, 7.24, 3.4, 0.22, "8–10분 피칭", 10, False, INK_SOFT, PP_ALIGN.RIGHT)
    if title:
        add_text(slide, 0.5, 0.62, 12.3, 0.55, title, 28, True, INK)


def knock_bg(im: Image.Image, tol: int = 28) -> Image.Image:
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    cr, cg, cb, ca = px[0, 0]
    if ca < 8:
        return im
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if abs(r - cr) <= tol and abs(g - cg) <= tol and abs(b - cb) <= tol:
                px[x, y] = (0, 0, 0, 0)
    return im


def nn_fit(im: Image.Image, max_w: int, max_h: int) -> Image.Image:
    w, h = im.size
    scale = min(max_w / w, max_h / h)
    nw = max(1, int(round(w * scale)))
    nh = max(1, int(round(h * scale)))
    return im.resize((nw, nh), NN)


def save_nn(src: Path, dest: Path, max_w: int, max_h: int, knock: bool = False) -> Path:
    im = Image.open(src)
    if knock:
        im = knock_bg(im)
    else:
        im = im.convert("RGBA")
    im = nn_fit(im, max_w, max_h)
    dest.parent.mkdir(parents=True, exist_ok=True)
    im.save(dest)
    return dest


def paste_nn(canvas: Image.Image, src: Path, xy: tuple[int, int], max_w: int, max_h: int, knock: bool = True):
    im = Image.open(src).convert("RGBA")
    if knock:
        im = knock_bg(im)
    im = nn_fit(im, max_w, max_h)
    canvas.alpha_composite(im, xy)


def build_composites() -> dict[str, Path]:
    COMPOSITES.mkdir(parents=True, exist_ok=True)
    paths: dict[str, Path] = {}

    paths["sticker"] = save_nn(
        REPO / "_sticker_export" / "dungeonfront_sticker_84mm_300dpi.png",
        COMPOSITES / "sticker.png",
        900,
        900,
    )
    paths["sheet"] = save_nn(ART / "Items" / "_preview" / "_sheet.png", COMPOSITES / "item_sheet.png", 1600, 1000)
    machines = Image.open(ART / "Machines" / "_preview" / "machines_tier_sheet.png").convert("RGBA")
    machines = machines.crop((0, 42, machines.width, machines.height))
    machines = nn_fit(machines, 1400, 1100)
    machines_path = COMPOSITES / "machines_sheet.png"
    machines.save(machines_path)
    paths["machines"] = machines_path
    paths["miner_sheet"] = save_nn(
        ART / "Machines" / "_preview" / "miner_tier_sheet.png",
        COMPOSITES / "miner_sheet.png",
        1200,
        700,
    )
    paths["tree"] = save_nn(
        ART / "Background" / "_preview" / "_tree_L70_200.png",
        COMPOSITES / "tree_scene.png",
        900,
        900,
        knock=False,
    )
    paths["tree_tiles"] = save_nn(
        ART / "Background" / "_preview" / "_sheet_tree_tiles.png",
        COMPOSITES / "tree_tiles.png",
        1200,
        500,
    )
    paths["walk"] = save_nn(ART / "Player" / "P_MoveForth.png", COMPOSITES / "walk.png", 900, 220)
    paths["ui_panel"] = save_nn(
        ART / "UI" / "OrnateFantasy" / "LightFantasy_panel_creamOrnate.png",
        COMPOSITES / "ui_panel.png",
        400,
        400,
    )
    paths["techtree"] = save_nn(
        ART / "UI" / "TechTree" / "_preview" / "_sheet.png",
        COMPOSITES / "machine_icons.png",
        1100,
        600,
    )

    for key, src in {
        "prot": ART / "UI" / "Portraits" / "protagonist_portrait.png",
        "eve": ART / "UI" / "Portraits" / "eve_portrait.png",
        "ray": ART / "UI" / "Portraits" / "ray_portrait.png",
    }.items():
        paths[key] = save_nn(src, COMPOSITES / f"{key}.png", 420, 420)

    factory = Image.new("RGBA", (1400, 1050), (200, 230, 160, 255))
    paste_nn(factory, ART / "Machines" / "_preview" / "furnace_1.png", (430, 40), 540, 540)
    paste_nn(factory, ART / "Machines" / "_preview" / "miner_1.png", (40, 120), 420, 420)
    paste_nn(factory, ART / "Machines" / "_preview" / "handmade_assembler_1.png", (820, 70), 540, 280)
    paste_nn(factory, ART / "Machines" / "_preview" / "mana_handmade_1.png", (800, 400), 560, 300)
    paste_nn(factory, ART / "ResourceNodes" / "_preview" / "iron_ore_with_drill.png", (80, 560), 420, 420)
    factory_path = COMPOSITES / "factory_cast.png"
    factory.save(factory_path)
    paths["factory"] = factory_path

    dungeon = Image.new("RGBA", (1400, 1050), (186, 172, 214, 255))
    paste_nn(dungeon, ART / "UI" / "Portraits" / "eve_portrait.png", (20, 50), 500, 500, knock=False)
    paste_nn(dungeon, ART / "UI" / "Portraits" / "protagonist_portrait.png", (410, 180), 420, 420, knock=False)
    paste_nn(dungeon, ART / "UI" / "Portraits" / "ray_portrait.png", (800, 20), 560, 560, knock=False)
    paste_nn(dungeon, ART / "Items" / "_preview" / "dark_magic_staff_icon.png", (40, 580), 300, 300, knock=False)
    paste_nn(dungeon, ART / "Items" / "_preview" / "iron_sword_icon.png", (300, 610), 250, 250, knock=False)
    paste_nn(dungeon, ART / "Items" / "_preview" / "iron_chestplate_icon.png", (540, 620), 230, 230, knock=False)
    paste_nn(dungeon, ART / "Items" / "_preview" / "mana_crystal_icon.png", (790, 590), 260, 260, knock=False)
    paste_nn(dungeon, ART / "Items" / "_preview" / "dungeon_master_essence_icon.png", (1070, 600), 270, 270, knock=False)
    dungeon_path = COMPOSITES / "dungeon_cast.png"
    dungeon.save(dungeon_path)
    paths["dungeon"] = dungeon_path

    portraits = Image.new("RGBA", (1400, 500), (255, 248, 234, 255))
    paste_nn(
        portraits,
        REPO / "_sticker_export" / "dungeonfront_sticker_84mm_300dpi.png",
        (20, 10),
        280,
        480,
        knock=True,
    )
    paste_nn(portraits, ART / "UI" / "Portraits" / "eve_portrait.png", (340, 70), 340, 340, knock=False)
    paste_nn(portraits, ART / "UI" / "Portraits" / "protagonist_portrait.png", (680, 70), 340, 340, knock=False)
    paste_nn(portraits, ART / "UI" / "Portraits" / "ray_portrait.png", (1020, 40), 360, 360, knock=False)
    portraits_path = COMPOSITES / "portraits_cast.png"
    portraits.save(portraits_path)
    paths["portraits"] = portraits_path

    for key, src in {
        "stone": ART / "Items" / "_preview" / "stone_icon.png",
        "mana": ART / "Items" / "_preview" / "mana_crystal_icon.png",
        "scroll": ART / "Items" / "_preview" / "ritual_scroll_icon.png",
        "altar": ART / "Items" / "_preview" / "altar_icon.png",
        "essence": ART / "Items" / "_preview" / "dungeon_master_essence_icon.png",
        "chest": ART / "Items" / "_preview" / "iron_chestplate_icon.png",
        "furnace_gold": ART / "Machines" / "_preview" / "furnace_3.png",
    }.items():
        paths[key] = save_nn(src, COMPOSITES / f"ico_{key}.png", 256, 256, knock=True)

    return paths


def slide_title(prs, paths):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "01  ·  이 게임", "", 1, dark=True)
    add_pic(s, paths["sticker"], 0.35, 0.7, height=6.4)
    pill(s, 6.15, 1.55, 3.15, 0.38, "공장  +  판타지  +  던전", fill=RUST, size=13)
    add_text(s, 6.15, 2.1, 6.7, 1.15, "DungeonFront", 48, True, WHITE)
    add_text(s, 6.15, 3.25, 6.6, 1.15, "최고의 대장간을 위해 성장하는\n공장 경영 게임", 24, True, GOLD)
    add_text(
        s,
        6.15,
        4.85,
        6.5,
        0.9,
        "Team 던전프론트\nFrom 2026 여름방학 스타터 프로젝트",
        16,
        False,
        CREAM,
    )
    set_notes(
        s,
        "던전프론트입니다. 던전 앞 대장간을 공장으로 키우는 게임입니다. "
        "앞에서는 이 게임이 무엇인지, 뒤에서는 지금 새로 붙는 던전 컨텐츠를 말씀드리겠습니다.",
    )


def slide_oneline(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "01  ·  이 게임", "", 2)
    add_text(s, 0.7, 1.7, 12.0, 1.1, "공장 X 판타지의 융합", 36, True, INK, PP_ALIGN.CENTER)
    rect(s, 4.4, 2.95, 4.5, 0.02, GOLD)
    add_text(
        s,
        1.2,
        3.25,
        10.9,
        1.8,
        "공장에서 장비와 아이템을 생산하고,\n모험가를 고용해 던전을 탐사한 보상으로\n더 좋은 기계, 더 좋은 아이템을 만드는 게임",
        20,
        False,
        INK,
        PP_ALIGN.CENTER,
    )
    add_text(
        s,
        0.8,
        5.5,
        11.7,
        0.7,
        "팩토리오  ×  명일방주: 엔드필드  ×  새티스팩토리  ×  Shapez  ×  …",
        14,
        False,
        RUST,
        PP_ALIGN.CENTER,
    )
    set_notes(
        s,
        "한 줄입니다. 공장에서 장비를 만들고, 그 장비를 들고 던전에 들어갑니다. "
        "납품만 하는 경영 게임이 아닙니다. 플레이어가 대장장이이자 탐험가입니다. "
        "물류는 Factorio 쪽에서, 전투 시점은 탑다운 액션 쪽에서 빌려 오지만, 둘을 복제하려는 게임은 아닙니다.",
    )


def slide_two_spaces(prs, paths):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "01  ·  이 게임", "게임의 흐름", 3)
    card(s, 0.5, 1.35, 6.0, 5.55, fill=CARD, line=GOLD)
    add_pic(s, paths["factory"], 0.7, 1.5, width=5.6)
    pill(s, 0.75, 5.85, 1.35, 0.34, "공장", fill=LEAF)
    add_text(s, 2.2, 5.82, 4.0, 0.38, "주인공 캐릭터의 무대", 14, True, INK, anchor=MSO_ANCHOR.MIDDLE)
    add_text(s, 0.75, 6.25, 5.5, 0.45, "기계 · 벨트 · 구역을 넓히며 성장", 13, False, INK_SOFT)

    card(s, 6.8, 1.35, 6.0, 5.55, fill=CARD, line=GOLD)
    add_pic(s, paths["dungeon"], 7.0, 1.5, width=5.6)
    pill(s, 7.05, 5.85, 1.35, 0.34, "던전", fill=MAGIC)
    add_text(s, 8.5, 5.82, 4.0, 0.38, "모험가 캐릭터의 무대", 14, True, INK, anchor=MSO_ANCHOR.MIDDLE)
    add_text(s, 7.05, 6.25, 5.5, 0.45, "좋은 장비를 만들어 더 어려운 던전을 탐사", 13, False, INK_SOFT)
    set_notes(
        s,
        "공간이 둘입니다. 왼쪽 공장은 한 세이브에서 계속 커집니다. 런이 리셋되지 않습니다. "
        "오른쪽 던전은 한 번 들어가면 오 분에서 십 분입니다. "
        "짧은 호흡을 타이머로 공장을 가두지 않습니다. 들어갔다 나오는 탐사가 쉼표입니다. "
        "탐사 중에는 공장이 멈춥니다. 플레이어는 던전에 집중합니다.",
    )


def slide_factory(prs, paths):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "01  ·  이 게임", "현재 개발된 공장 컨텐츠", 4)
    add_pic(s, paths["machines"], 0.45, 1.3, height=5.6)
    caps = [
        ("기계 업그레이드", "초반은 수동으로 옮기고 속도도 느리지만, 발전할수록 더욱 더 빨라지고 자동화할 수 있게 된다"),
        ("장비 업그레이드", "더 높은 테크의 기계는 새로운 장비를 제작할 수 있게 된다. 이전보다 훨씬 더 많은 자원이 필요해 공장을 지속적으로 확장해야 한다."),
        ("영역 확장", "던전 보상으로 획득한 골드로 맵을 확장할 수 있다. 맵은 랜덤 생성이고 위치에 따라 자원 분포가 다르다."),
    ]
    for i, (h, b) in enumerate(caps):
        y = 1.32 + i * 1.88
        card(s, 8.45, y, 4.45, 1.75, fill=CARD, line=GOLD)
        add_text(s, 8.65, y + 0.12, 4.1, 0.4, h, 15, True, INK)
        add_text(s, 8.65, y + 0.55, 4.1, 1.08, b, 12, False, INK_SOFT)
    set_notes(
        s,
        "공장 쪽입니다. 채굴기, 용광로, 제작대, 마나 포집기, 벨트, 창고를 놓고 장비를 만듭니다. "
        "처음에는 모든 기계가 수동입니다. 테크가 오르면 자동화가 열립니다. "
        "맵은 새 게임마다 한 번 랜덤으로 깔리고, 골드로 구역을 넓힙니다. "
        "공장은 런이 리셋되지 않습니다. 한 세이브에서 계속 키웁니다.",
    )


def slide_dungeon_new(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "02  ·  새 던전", "이번 학기의 개발 목적: 던전 컨텐츠", 5)
    points = [
        (
            MAGIC,
            "목적",
            "플레이어의 공장 설계의 목적이 되는 컨텐츠. 기존에는 5분 간격으로 플레이를 강제로 중단하는 방식을 썼으나, 그 역할을 던전이 대체한다. 더 자연스럽고 불편함을 줄인 개선안.",
        ),
        (
            GOLD,
            "방식",
            "고용한 모험가 중 4명으로 파티를 짜서 던전에 들어간다. 몬스터 웨이브를 처치하고, 최종 웨이브까지 처치하면 던전 클리어.",
        ),
        (
            LEAF,
            "보상",
            "골드, 기계·아이템 제작 재료가 되는 희귀 자원, 명성. 명성은 스테이지를 클리어하면 딱 1번 영구히 올라가고, 게임 진행도를 대략적으로 표시한다.",
        ),
    ]
    for i, (col, h, b) in enumerate(points):
        y = 1.32 + i * 1.88
        card(s, 0.55, y, 12.25, 1.72, fill=CARD, line=GOLD)
        rect(s, 0.55, y, 0.14, 1.72, col)
        add_text(s, 1.0, y + 0.14, 11.4, 0.4, h, 18, True, INK)
        add_text(s, 1.0, y + 0.58, 11.4, 0.98, b, 14, False, INK_SOFT)
    set_notes(
        s,
        "이제 새 컨텐츠입니다. 던전입니다. 공장은 키우는 공간이고, 던전은 그 장비를 시험하고 파밍하는 공간입니다. "
        "둘 다 본전입니다. 한쪽으로 기울이지 않습니다. "
        "한 번 탐사는 오 분에서 십 분입니다. 짧은 호흡을 타이머로 공장을 가두지 않고, 들어갔다 나오는 탐사가 쉼표를 맡습니다.",
    )


def slide_battle(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "02  ·  새 던전", "전투 시스템", 6)
    cells = [
        (
            LEAF,
            "4인 편성 / 메인 캐릭터 1명",
            "고용한 모험가 중 4명을 편성. 그 중 메인 캐릭터 1명을 정해서 그 캐릭터만 필드에서 싸움. 나머지 셋은 서브 캐릭터.",
        ),
        (
            RUST,
            "조작",
            "우클릭 이동, 좌클릭 기본공격,\nqwer/asdf : 서브 스킬, 스페이스바: 메인스킬",
        ),
        (
            MAGIC,
            "스킬",
            "캐릭터는 직업을 선택할 수 있고, 직업에 따라 서브 스킬과 메인 스킬 여러 개를 가짐.",
        ),
        (
            GOLD,
            "스킬 편성",
            "메인 캐릭터의 [부적] 아이템의 스킬 슬롯에 메인 캐릭터와 서브 캐릭터들의 서브 스킬을 선택해서 장착 가능.",
        ),
    ]
    for i, (col, h, b) in enumerate(cells):
        r, c = divmod(i, 2)
        x = 0.5 + c * 6.4
        y = 1.28 + r * 2.4
        card(s, x, y, 6.15, 2.22, fill=CARD, line=col)
        rect(s, x, y, 0.12, 2.22, col)
        add_text(s, x + 0.35, y + 0.18, 5.55, 0.45, h, 18, True, INK)
        add_text(s, x + 0.35, y + 0.7, 5.55, 1.3, b, 14, False, INK_SOFT)
    card(s, 0.5, 6.15, 12.35, 0.85, fill=CARD, line=GOLD)
    add_text(
        s,
        0.7,
        6.2,
        12.0,
        0.75,
        "메인 캐릭터의 hp가 0이 되면 공략 실패. 이번 보상을 일부만 가져갈 수 있다.",
        15,
        True,
        INK,
        anchor=MSO_ANCHOR.MIDDLE,
    )
    set_notes(
        s,
        "배틀입니다. 모험가 네 명으로 파티를 짜고, 필드에는 한 명만 나옵니다. "
        "이동하고 좌클릭으로 때리고, 스페이스가 직업 주 스킬, 우클릭이 부적입니다. "
        "부적에는 스킬석을 끼우고, 우클릭하면 앞에서부터 차례로 나갑니다. "
        "큐이 이로 캐릭터를 바꿉니다. 쓰러지면 그 캐릭터만 빠지고 다른 멤버로 싸웁니다. "
        "한 방에서 웨이브가 이어지고, 마지막 웨이브까지 잡으면 클리어입니다. "
        "넷이 다 쓰러지면 실패고, 이번 루팅만 일부 잃습니다. 장비는 그대로입니다.",
    )


def slide_themes(prs, paths):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "02  ·  새 던전", "다양한 테마 던전", 7)
    add_text(
        s,
        0.5,
        1.18,
        12.3,
        0.7,
        "던전은 각 테마별로 여러 층으로 구성. 스토리 진행에 따라 차례차례 열리는 구조.\n던전이 개방됨에 따라 해당 테마의 기계 제작도 열림.",
        14,
        False,
        INK_SOFT,
    )
    themes = [
        (paths["stone"], LEAF, "뒷산 동굴"),
        (paths["mana"], RGBColor(0x4A, 0x7A, 0x9A), "혹한의 설원"),
        (paths["scroll"], RGBColor(0x5A, 0x8A, 0x4A), "슬라임 서식지"),
        (paths["furnace_gold"], GOLD, "깊이 잠든 황금향"),
        (paths["chest"], RUST, "좋은 대장간?"),
        (paths["essence"], MAGIC, "태고의 땅"),
    ]
    for i, (img, col, name) in enumerate(themes):
        r, c = divmod(i, 3)
        x = 0.5 + c * 4.2
        y = 1.95 + r * 2.45
        card(s, x, y, 4.0, 2.25, fill=CARD, line=col)
        rect(s, x, y, 0.12, 2.25, col)
        add_pic(s, img, x + 0.3, y + 0.3, height=1.65)
        add_text(s, x + 1.9, y + 0.55, 1.95, 1.15, name, 16, True, INK, anchor=MSO_ANCHOR.MIDDLE)
    set_notes(
        s,
        "던전은 여섯 테마입니다. 메인과 서브가 테마 문을 엽니다. 테마 안 층은 일 층부터 순차입니다. "
        "이미 열린 테마는 다시 들어가 파밍할 수 있습니다. "
        "뒷산 동굴이 입문입니다. 혹한의 설원, 슬라임 서식지, 깊이 잠든 황금향, 좋은 대장간, 태고의 땅. "
        "테마가 곧 타일셋과 적과 보스 세트입니다. 황금향은 용 스우마그가 주인입니다.",
    )


def slide_loop(prs, paths):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "01  ·  이 게임", "현재까지 개발된 컨텐츠", 8)
    steps = [
        (LEAF, "공장", "기계 · 라인 · 구역"),
        (RUST, "아이템 제작", "투구 · 흉갑 · 무기"),
        (MAGIC, "의뢰 납품", "모험가에게 넘긴다"),
        (GOLD, "보상 획득", "골드 · 명성"),
    ]
    for i, (col, h, b) in enumerate(steps):
        x = 0.5 + i * 3.2
        card(s, x, 1.4, 2.95, 2.15, fill=CARD, line=col)
        rect(s, x, 1.4, 2.95, 0.12, col)
        add_text(s, x + 0.15, 1.65, 2.65, 0.3, f"0{i + 1}", 12, True, col)
        add_text(s, x + 0.15, 1.95, 2.65, 0.5, h, 20, True, INK)
        add_text(s, x + 0.15, 2.5, 2.65, 0.75, b, 14, False, INK_SOFT)
        if i < 3:
            add_text(s, x + 2.7, 2.05, 0.55, 0.5, "→", 24, True, GOLD, PP_ALIGN.CENTER, MSO_ANCHOR.MIDDLE)
    card(s, 0.5, 3.8, 12.35, 3.05, fill=CARD, line=GOLD)
    add_pic(s, paths["sheet"], 0.7, 3.95, height=2.75)
    add_text(s, 7.55, 4.0, 5.0, 0.45, "하루 시스템 (삭제 예정)", 16, True, RUST)
    add_text(
        s,
        7.55,
        4.5,
        5.0,
        2.05,
        "준비 / 생산 / 결산으로 끊어서 5분 템포를 만들었으나, 공장 게임에 맞지 않다고 판단했다. 이 시스템을 폐기하고 던전 탐사 컨텐츠를 새로 개발한다.",
        15,
        False,
        INK,
    )
    set_notes(
        s,
        "한 바퀴입니다. 공장에서 장비를 만들고, 그 장비를 들고 던전에 들어가고, 골드와 희귀 자원과 명예를 들고 돌아옵니다. "
        "명예는 층을 처음 깼을 때만 줍니다. 여기까지가 이 게임의 골격입니다. "
        "이제부터는 그 루프의 오른쪽, 새로 만들 던전 컨텐츠를 말하겠습니다.",
    )


def slide_dungeon_build(prs, paths):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "02  ·  새 던전", "앞으로 할 일들", 9)
    cells = [
        (paths["tree_tiles"], "각 던전별 타일 디자인", "테마당 맵들을 구성할 수 있는 타일들"),
        (paths["walk"], "적 · 보스", "웨이브 잡몹 + 던전의 보스들"),
        (paths["portraits"], "캐릭터 비주얼", "여러 캐릭터들의 초상화와 스탠딩 일러스트"),
        (paths["machines"], "공장 기계 디자인", "다양한 기계들의 모션 작업"),
    ]
    for i, (img, h, b) in enumerate(cells):
        r, c = divmod(i, 2)
        x = 0.45 + c * 6.45
        y = 1.28 + r * 2.9
        card(s, x, y, 6.2, 2.7, fill=CARD, line=GOLD)
        add_pic(s, img, x + 0.18, y + 0.7, width=2.5)
        add_text(s, x + 2.85, y + 0.35, 3.15, 0.7, h, 18, True, INK)
        add_text(s, x + 2.85, y + 1.15, 3.15, 1.25, b, 14, False, INK_SOFT)
    set_notes(
        s,
        "개발로 보면 테마 하나가 세트 하나입니다. 타일 한 장, 그 방의 기믹, 적과 보스, 그 테마의 루팅 아이콘. "
        "지형을 층마다 새로 그리지 않습니다. 같은 방을 돌리고 스폰과 목표만 바꿉니다. "
        "입문은 뒷산이고, 그다음 테마부터가 비어 있습니다. "
        "이 여섯 세트를 채우는 일이 지금부터의 던전 작업입니다.",
    )


def slide_close(prs, paths):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    chrome(s, "02  ·  새 던전", "", 10, dark=True)
    add_pic(s, paths["sticker"], 0.4, 0.85, height=5.9)
    add_text(s, 5.9, 1.35, 6.8, 1.45, "DungeonFront 2.0에\n함께할 팀원을 구합니다", 26, True, WHITE)
    add_text(s, 5.9, 2.95, 6.8, 0.6, "초심자 지원 가능", 24, True, GOLD)
    card(s, 5.9, 3.75, 6.7, 1.9, fill=RGBColor(0x36, 0x26, 0x1C), line=GOLD)
    add_text(
        s,
        6.1,
        3.9,
        6.3,
        1.6,
        "개발 중 프로젝트의 진입 장벽? 스파게티 코드? 아예 없습니다!\n새 컨텐츠를 백지부터 같이 시작할 팀원들을 구합니다.",
        14,
        False,
        CREAM,
    )
    pill(s, 5.9, 5.85, 6.7, 0.5, "팀장 연락(윤성원) - 010-6667-1138", fill=RUST, size=13)
    set_notes(
        s,
        "정리합니다. 던전 앞에서 공장을 키우고, 그 장비를 들고 던전에 들어갑니다. "
        "지금 새로 만드는 것은 그 던전입니다. 테마 여섯, 한 층 오 분에서 십 분. "
        "타일 한 세트, 적 한 줄, 아이콘 한 줄. 셋 중 하나면 이 컨텐츠에 붙을 수 있습니다. "
        "질문은 지금 받겠습니다.",
    )


def main():
    paths = build_composites()
    prs = Presentation()
    prs.slide_width = SLIDE_W
    prs.slide_height = SLIDE_H

    slide_title(prs, paths)
    slide_oneline(prs)
    slide_two_spaces(prs, paths)
    slide_factory(prs, paths)
    slide_dungeon_new(prs)
    slide_battle(prs)
    slide_themes(prs, paths)
    slide_loop(prs, paths)
    slide_dungeon_build(prs, paths)
    slide_close(prs, paths)

    prs.save(OUT_PPTX)
    print(f"Wrote {len(prs.slides)} slides → {OUT_PPTX}")
    for i, slide in enumerate(prs.slides, 1):
        notes = slide.notes_slide.notes_text_frame.text.replace("\n", " ")
        print(f"  {i:02d}  notes {len(notes):3d}c  {notes[:72]}")


if __name__ == "__main__":
    main()
