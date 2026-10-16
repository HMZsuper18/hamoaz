# hamoaz UI Icon Pack — Style Guide

This folder is a **fully custom icon pack**. Every icon is drawn to the same
visual contract so the menu reads as one family. Follow this document exactly
when adding icons — whether you are a human designer or an AI agent.

## Inventory

| File | Used by | Meaning |
|---|---|---|
| `IconPlay.png` | Main menu — Singleplayer | Start / play |
| `IconMultiplayer.png` | Main menu — Multiplayer | Players together |
| `IconSettings.png` | Main menu — Settings | Gear / options |
| `IconAbout.png` | Main menu — About | Info / about |
| `IconBack.png` | About — Back button | Return / previous |
| `IconCode.png` | About — View on GitHub | Source code / repository |

Spare, older drawings kept for reference only (not part of the style contract,
do not reuse in new UI): `IconInfo.png`, `IconPeople.png`.

## The style contract

1. **Canvas**: exactly `256 x 256 px`, fully transparent background.
2. **Ink**: pure white `#FFFFFF` via alpha only. No second color, no gradient,
   no shadow, no outline-behind-fill.
3. **Ink box**: after drawing, the glyph is cropped and uniformly scaled so its
   **largest dimension is ~226 px** (pack range 225–230), then recentered.
   Uniform padding falls out automatically — never hand-place the glyph.
4. **Form language**: **solid, filled shapes** (not hairline icons). Friendly
   and rounded — think Fredoka-type letterforms: every corner and stroke end is
   **round**. Limb thickness **after normalize ~28–46 px** (roughly 12–20% of
   the ink box — keep it light, compare against the gear's spokes). Design
   coordinates may exceed 0–255: `normalize()` crops to ink, so widen the
   layout to add breathing room between glyph elements.
5. **One glyph per icon**, centered, no text, no counters/badges.
6. **Legibility floor**: must still read at the in-game size of **40 x 40 px**.
   Check this before shipping (downscale to 40 and look at it).

Rejected styles (seen in the old spares — do not repeat): hairline strokes
(`IconInfo`), asymmetric weight (`IconPeople`), tight ink boxes under ~215 px.

## Adding an icon — agents (PNG pipeline)

Use the generator, do not hand-edit PNGs:

1. Open `Tools/IconGenerator/generate_icons.py`.
2. Add a `icon_<name>()` function using the helpers:
   - `new_canvas()` — 4x supersampled transparent canvas + its ImageDraw.
   - `limb(d, points, thickness)` — round-capped/jointed stroke (design coords
     in 256-space, thickness in 256-space px).
   - `cap(d, x, y, r)` — free-standing round dot.
   - `normalize(im)` — **always end with this**: crops to ink, scales so max
     ink dim = 226, recenters, downsamples to 256.
3. Register it in the `ICONS` dict as `"Icon<Name>.png": icon_<name>`.
4. Run `python3 Tools/IconGenerator/generate_icons.py --sheet`, inspect
   `/tmp/opencode/icon_sheet_new.png` next to the existing pack, and also check
   a 40 px downscale.
5. Reference the sprite from the UI builder with
   `LoadSprite("Assets/UI/Icons/Icon<Name>.png")` (the builder's importer
   configures the texture; see Import settings below).

## Adding an icon — designers (vector pipeline)

1. Copy an SVG from `SVG/` as a starting point. The SVGs use the same
   round-capped stroke construction as the generator (`stroke-linecap/linejoin:
   round`, white stroke, no fill).
2. Draw in a `0 0 256 256` viewBox, keep limb thickness 46–56 and round caps.
3. Export PNG at 256 x 256 (transparent), then run the generator's `normalize`
   step on it — or hand-scale so the cropped ink's max dimension is ~226 px and
   it is centered. When in doubt, ask an agent to normalize via the generator.
4. Drop the final `Icon<Name>.png` in this folder and update the table above.

## Import settings (enforced by the builders)

The builders reimport icons with these exact settings (if you add a texture by
hand in Unity, match them): Sprite (2D and UI), Single mode, sprites PPU 100,
Bilinear filter, no mipmaps, Compression None, Alpha Is Transparency on.

## Reference measurements of the original four

| Icon | Ink box (w x h) | Notes |
|---|---|---|
| IconPlay | 195 x 227 | filled rounded triangle |
| IconMultiplayer | 230 x 189 | filled silhouettes |
| IconSettings | 226 x 224 | filled gear, ~54 px teeth |
| IconAbout | 225 x 225 | filled disc, counter cut out |
