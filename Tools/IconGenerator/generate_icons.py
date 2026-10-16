#!/usr/bin/env python3
"""
hamoaz UI icon generator.

Regenerates the custom vector-style icons in Assets/UI/Icons in the pack's
established style: pure-white solid glyphs, chunky rounded forms, ~226px ink
on a 256x256 transparent canvas (see Assets/UI/Icons/README.md for the full
style contract this script implements).

Usage:
    python3 Tools/IconGenerator/generate_icons.py            # write PNGs
    python3 Tools/IconGenerator/generate_icons.py --sheet    # also write a contact sheet to /tmp/opencode/icon_sheet_new.png

Only icons that are fully defined BELOW are (re)written - the original hand-
drawn pack icons (Play, Multiplayer, Settings, About) are never touched.
Add new icons by appending a draw_*() function and registering it in ICONS.
"""
import math
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

# ----- style contract (keep in sync with Assets/UI/Icons/README.md) -----
SS = 4              # supersample factor while drawing (antialiasing)
CANVAS = 256        # final canvas, px
INK = 226           # max ink dimension after normalize (pack range: 225-230)
WHITE = (255, 255, 255, 255)

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(REPO_ROOT, "Assets", "UI", "Icons")


def new_canvas(w=CANVAS, h=CANVAS):
    """Drawing canvas in design units (x/y may exceed 0..CANVAS; normalize crops)."""
    im = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def cap(d, x, y, r):
    """Round cap: filled circle of radius r at (x, y). Coords in draw space."""
    d.ellipse([x - r, y - r, x + r, y + r], fill=WHITE)


def limb(d, points, thickness):
    """Round-capped, round-jointed stroke through the given 256-space points."""
    t = thickness * SS
    pts = [(x * SS, y * SS) for x, y in points]
    d.line(pts, fill=WHITE, width=t, joint="curve")
    for p in pts:
        cap(d, p[0], p[1], t / 2)


def normalize(im):
    """Crop to ink, uniform-scale so max ink dim = INK*SS, recenter, to CANVAS."""
    bbox = im.split()[3].getbbox()
    im = im.crop(bbox)
    w, h = im.size
    scale = (INK * SS) / max(w, h)
    im = im.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS * SS, CANVAS * SS), (0, 0, 0, 0))
    canvas.paste(im, ((CANVAS * SS - im.size[0]) // 2, (CANVAS * SS - im.size[1]) // 2), im)
    return canvas.resize((CANVAS, CANVAS), Image.LANCZOS)


# ----- icon definitions (design coords: 256-space, pre-normalize) -----
# Design coords MAY exceed 0-255: normalize() crops to ink and rescales, so
# wider layouts are the way to add breathing room between glyph elements.

def icon_back():
    """Left arrow: slim chevron + shaft meeting at ONE tip vertex (connected)."""
    im, d = new_canvas()
    limb(d, [(108, 60), (38, 128), (108, 196)], 30)   # chevron head
    limb(d, [(222, 128), (38, 128)], 30)              # shaft, into the tip
    return normalize(im)


def icon_code():
    """</> source-code mark: chevrons + slash with generous gaps."""
    im, d = new_canvas(340, 256)
    limb(d, [(96, 58), (40, 128), (96, 198)], 36)     # left <
    limb(d, [(142, 200), (186, 56)], 36)              # slash /
    limb(d, [(232, 58), (288, 128), (232, 198)], 36)  # right >
    return normalize(im)


ICONS = {
    "IconBack.png": icon_back,
    "IconCode.png": icon_code,
}

# ----- non-icon UI sprites -----

ROUNDED_PANEL_PATH = os.path.join(OUT_DIR, "..", "RoundedPanel.png")
ROUNDED_PANEL_RADIUS = 44   # baked corner radius at 512 (importer border = 48)
ROUNDED_PANEL_BORDER = 48   # keep in sync with AboutUIBuilder.LoadSprite call


def rounded_panel():
    """512x512 white rounded rect for 9-sliced panels (crisp at any size)."""
    s = 2
    size = 512 * s
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, size - 1, size - 1], radius=ROUNDED_PANEL_RADIUS * s, fill=WHITE)
    return im.resize((512, 512), Image.LANCZOS)


# Painted brush panel for the About card - same torn-edge language as
# Assets/UI/BrushStrip.png (the menu buttons). Keep BRUSH_CARD size in sync with
# CardWidth in Assets/Editor/AboutUIBuilder.cs (height = content, ~525 design px).
BRUSH_CARD_PATH = os.path.join(OUT_DIR, "..", "BrushCard.png")
BRUSH_CARD_OUTLINE_PATH = os.path.join(OUT_DIR, "..", "BrushCardOutline.png")
BRUSH_CARD_W, BRUSH_CARD_H = 980, 530
BRUSH_CARD_SEED = 20261016          # fixed: regeneration is deterministic
BRUSH_CARD_OUTLINE_MAX_ALPHA = 140  # matches BrushStripOutline.png


def _rounded_rect_loop(w, h, m, r, step):
    """Sample (x, y, nx, ny) points + outward normals around a rounded rect.

    Uniform step along the loop so a noise sequence indexed by position stays
    continuous (incl. across the wrap seam).
    """
    left, top, right, bottom = m, m, w - m, h - m
    r = min(r, (right - left) / 2, (bottom - top) / 2)
    pts = []

    def line(x0, y0, x1, y1, nx, ny):
        n = max(2, round(math.hypot(x1 - x0, y1 - y0) / step))
        for i in range(n):
            t = i / n
            pts.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, nx, ny))

    def arc(cx, cy, a0, a1):
        n = max(2, round(abs(a1 - a0) * r / step))
        for i in range(n):
            a = a0 + (a1 - a0) * (i / n)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a),
                        math.cos(a), math.sin(a)))

    line(left + r, top, right - r, top, 0, -1)          # top, L -> R
    arc(right - r, top + r, -math.pi / 2, 0)            # TR corner
    line(right, top + r, right, bottom - r, 1, 0)       # right, T -> B
    arc(right - r, bottom - r, 0, math.pi / 2)          # BR corner
    line(right - r, bottom, left + r, bottom, 0, 1)     # bottom, R -> L
    arc(left + r, bottom - r, math.pi / 2, math.pi)     # BL corner
    line(left, bottom - r, left, top + r, -1, 0)        # left, B -> T
    arc(left + r, top + r, math.pi, 3 * math.pi / 2)    # TL corner
    return pts


def _torn_offsets(rng, n, step, cap, bite_depth, bite_width, bite_count):
    """Periodic organic edge offsets, length n: harmonics + inward bite notches."""
    # harmonics: integer frequencies so the sequence wraps seamlessly
    harm = [(f, rng.uniform(0, 2 * math.pi), a)
            for f, a in ((1, 1.0), (2, 0.7), (3, 0.5), (5, 0.35), (8, 0.22),
                         (13, 0.14), (21, 0.10), (34, 0.07))]
    seq = [sum(a * math.sin(2 * math.pi * f * (i / n) + p) for f, p, a in harm)
           for i in range(n)]
    peak = max(abs(v) for v in seq) or 1.0
    seq = [v * cap / peak for v in seq]

    # occasional deep tears biting inward
    for _ in range(bite_count):
        c = rng.randrange(n)
        depth = rng.uniform(bite_depth * 0.4, bite_depth)
        sigma = rng.uniform(bite_width * 0.5, bite_width)
        for i in range(n):
            d = min(abs(i - c), n - abs(i - c)) * step
            if d < sigma * 3:
                seq[i] -= depth * math.exp(-(d * d) / (2 * sigma * sigma))
    return seq


def _brush_mask(w, h, seed, s, blur):
    """Alpha mask of a painted panel: rounded base, torn edges, soft feather."""
    rng = random.Random(seed)
    step = 10  # design px between loop samples
    loop = _rounded_rect_loop(w * s, h * s, 20 * s, 44 * s, step * s)
    offs = _torn_offsets(rng, len(loop), step * s,
                         cap=11 * s, bite_depth=11 * s, bite_width=8 * s, bite_count=22)
    poly = [(x + nx * o, y + ny * o) for (x, y, nx, ny), o in zip(loop, offs)]
    im = Image.new("L", (w * s, h * s), 0)
    ImageDraw.Draw(im).polygon(poly, fill=255)
    im = im.filter(ImageFilter.GaussianBlur(radius=blur * s))
    # tighten the feather: remap the 25-75% window to 0-255 so the final edge
    # is a crisp ~1px 10-90 cut (BrushStrip measures ~2px at 960 wide)
    return im.point(lambda v: 0 if v <= 64 else (255 if v >= 191 else round((v - 64) * 255 / 127)))


def brush_card(w=BRUSH_CARD_W, h=BRUSH_CARD_H):
    """White painted panel with torn soft edges (About card body)."""
    s = 2
    mask = _brush_mask(w, h, BRUSH_CARD_SEED, s, blur=0.5)
    mask = mask.resize((w, h), Image.LANCZOS)
    return Image.merge("RGBA", [Image.new("L", (w, h), 255)] * 3 + [mask])


def brush_card_outline(w=BRUSH_CARD_W, h=BRUSH_CARD_H):
    """Thin light rim tracing the card's torn shape (button-outline recipe)."""
    s = 2
    mask = _brush_mask(w, h, BRUSH_CARD_SEED, s, blur=0.5)
    eroded = mask.filter(ImageFilter.MinFilter(size=2 * round(2.0 * s) + 1))
    rim = ImageChops.subtract(mask, eroded)
    peak = rim.getextrema()[1]
    scale = BRUSH_CARD_OUTLINE_MAX_ALPHA / max(1, peak)
    rim = rim.point(lambda v: round(v * scale)).resize((w, h), Image.LANCZOS)
    return Image.merge("RGBA", [Image.new("L", (w, h), 255)] * 3 + [rim])


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for name, fn in ICONS.items():
        path = os.path.join(OUT_DIR, name)
        fn().save(path)
        print(f"wrote {path}")

    panel_path = os.path.abspath(ROUNDED_PANEL_PATH)
    rounded_panel().save(panel_path)
    print(f"wrote {panel_path}")

    for rel, fn in ((BRUSH_CARD_PATH, brush_card), (BRUSH_CARD_OUTLINE_PATH, brush_card_outline)):
        path = os.path.abspath(rel)
        fn().save(path)
        print(f"wrote {path}")

    if "--sheet" in sys.argv:
        sheet = Image.new("RGB", (len(ICONS) * (CANVAS + 16), CANVAS + 16), (40, 50, 60))
        for i, name in enumerate(ICONS):
            im = Image.open(os.path.join(OUT_DIR, name)).convert("RGBA")
            bg = Image.new("RGBA", im.size, (40, 50, 60, 255))
            bg.alpha_composite(im)
            sheet.paste(bg.convert("RGB"), (i * (CANVAS + 16) + 8, 8))
        out = "/tmp/opencode/icon_sheet_new.png"
        sheet.save(out)
        print(f"wrote {out}")


if __name__ == "__main__":
    main()
