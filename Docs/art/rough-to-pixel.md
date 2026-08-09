# Agent prompt: rough sketch → pixel sprite

You are converting a user-provided **rough sketch** into a game-ready pixel sprite for DungeonFront.
Follow this file exactly. Do not invent style rules from unrelated chat context (e.g. item material themes like darksteel/brightsteel/greysteel) unless the user or the sketch specifies them.

Item icons (16×16): also obey `Docs/art/item-icon-rules.md` and `.cursor/rules/item-icon-art.mdc`.
Machine footprint sizes: `Docs/dev-plan/week5/week5-art/01-machine-visual-ai.md`.

---

## Hard constraints

1. Keep the sketch **silhouette, viewpoint, and part placement**. Add detail only; do not redesign.
2. Output **PNG RGBA**, **transparent background**, **hard pixels** (no blur, no anti-aliasing).
3. Do **not** create or edit `*.meta` (Unity owns them).
4. Machine size is **not always 64×64**. One grid tile = **32×32** px. Sprite size = `(footprint_w × 32) × (footprint_h × 32)`.
5. Do not apply item-icon material palettes or themes unless asked.

| footprint | pixel size |
|-----------|------------|
| 1×1 | 32×32 |
| 1×2 | 32×64 |
| 2×2 | 64×64 |
| n×m | (n×32)×(m×32) |

---

## Machine / rough pipeline (required order)

1. Read the attached rough PNG.
2. Resolve `footprint` from user or machine table → compute `W = n*32`, `H = m*32`.
3. Call image generation with the rough as **reference image** and the **GenerateImage description** below (English).
4. Post-process the high-res result to exact `W×H` (see Post-process).
5. Save:
   - `Assets/Art/Machines/{name}.png` — final `W×H`
   - `Assets/Art/Machines/_preview/{name}.png` — Nearest ×8
6. Visually verify preview against the rough; if silhouette drifted, regenerate or redraw and repeat.

### Item-icon path (when task is an item, not a machine)

1. Optional: generate a concept image for silhouette only.
2. Produce final **16×16** hard pixels per `item-icon-rules.md` (often paint with PIL / pixel map).
3. Update `Assets/Art/Items/{id}_icon.png`, `_preview/` (Nearest ×16), and `item_icon_map.txt`.

---

## GenerateImage description (machines)

Always attach the rough as `reference_image_paths`. Fill placeholders; keep the rest verbatim.

```text
{W}x{H} pixel art game sprite of a {SUBJECT}, {VIEW} matching the reference sketch outline exactly.
Same silhouette: {KEY_PARTS}.
Clean hard-edged pixel art, no anti-aliasing, transparent background.
{MATERIAL_COLORS}. Compact stocky proportions filling most of the canvas.
No text, no UI, single object centered.
```

| placeholder | rule |
|-------------|------|
| `{W}` `{H}` | `footprint_w*32`, `footprint_h*32` |
| `{SUBJECT}` | English name of the machine |
| `{VIEW}` | Match sketch (e.g. `front three-quarter view`) |
| `{KEY_PARTS}` | List visible parts from the sketch only |
| `{MATERIAL_COLORS}` | User-specified; else match sketch (stone/iron/etc.). Do not import unrelated themes |

### Filled example (furnace, footprint 2×2 → 64×64)

```text
64x64 pixel art game sprite of a sturdy faceted industrial furnace/kiln, front three-quarter view matching the reference sketch outline exactly. Same silhouette: wide multi-faced body, arched firebox with orange flame inside, horizontal brick/plate lines on angled side panels, trapezoid hood panels tapering up, small tiered chimney on top, stepped flared base. Clean hard-edged pixel art, no anti-aliasing, transparent background. Dark iron and soot-stained stone colors with metal frame posts at facet corners. Compact stocky proportions filling most of the canvas. No text, no UI, single object centered.
```

### GenerateImage description (item concept only)

```text
16x16 pixel art game item icon of {ITEM}. Single centered {SHAPE}, {COLORS},
clear hard pixels, {LIGHTING}, transparent background, no text, no UI frame,
minimalist retro inventory icon.
```

Then force final asset to true 16×16 per item-icon rules (do not ship the raw high-res concept).

---

## Post-process (required)

From generated high-res RGBA:

1. Crop to content bbox (drop near-white margin).
2. Pad to square (or target aspect for non-square footprint); ~4% inset so outline is not clipped.
3. Near-white background → alpha 0.
4. Resize to exact `W×H` with **BOX** (or equivalent area filter).
5. Drop pixels with alpha < 40; set remaining opaque pixels to alpha 255.
6. Optional: snap RGB to coarse steps to reduce muddy blends.
7. Write final PNG + Nearest `_preview`.

### Accept only if

- Silhouette / view / parts match the rough
- Sprite fills the footprint canvas without large empty margins
- Fully transparent background
- Hard pixels only
- Distinct from other machines by silhouette

---

## Forbidden

- Redesigning proportions or relocating parts for aesthetics
- Shipping anti-aliased or non-target-resolution finals
- Skipping `_preview`
- Creating `*.meta`
- Pulling color/theme rules from other asset families without user request

---

## Paths

| asset | path |
|-------|------|
| machine | `Assets/Art/Machines/{name}.png` |
| machine preview | `Assets/Art/Machines/_preview/{name}.png` |
| item | `Assets/Art/Items/{id}_icon.png` |
| item preview / map | `Assets/Art/Items/_preview/`, `item_icon_map.txt` |

Use importable English `snake_case` filenames.
