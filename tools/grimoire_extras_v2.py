"""Grimoire's Aegis — v2 extras: NPC portraits, battle VFX, themed array pieces, icons.

GDD references:
  §3  supporting cast: Archivist Lyra (mentor), Mordecai the Smith (shop), Shade of the Order (hints / Mercy).
  §7  result feedback: hexagonal blue shield for Perfect Guard, metal clang for Guard, shattering glass for Break.
  §17 array shown as themed objects whose height matches the value: books (Act I),
      candles (Act II), split crystals (Act III).
"""
import math
import os
import random

from PIL import Image, ImageDraw

from grimoire_art_v2 import Cv, shadow_ellipse
from build_grimoire_assets import hx, ramp


def put(img, pts, col):
    p = img.load(); w, h = img.size
    c = hx(col) if isinstance(col, str) else col
    for (x, y) in pts:
        x, y = int(round(x)), int(round(y))
        if 0 <= x < w and 0 <= y < h:
            p[x, y] = c


# ===========================================================================
# NPC PORTRAITS (64x64 bust, 2-frame: neutral + blink/talk)
# ===========================================================================
def lyra(f=0):
    """Archivist Lyra — last keeper of the Great Catalog. Spectacles, index-card hairpins, teal robe."""
    c = Cv(64, 64)
    c.poly([(8, 64), (14, 44), (26, 38), (38, 38), (50, 44), (56, 64)], 'teal', shade='edge')
    c.poly([(26, 40), (32, 50), (38, 40), (38, 64), (26, 64)], 'paper', shade='flat')
    c.poly([(28, 42), (32, 50), (36, 42)], 'gold', shade='light')
    c.ellipse(32, 24, 13, 14, 'hairbr', shade='sphere')                     # hair volume
    c.poly([(19, 26), (17, 46), (24, 44), (22, 30)], 'hairbr', shade='edge')
    c.poly([(45, 26), (47, 46), (40, 44), (42, 30)], 'hairbr', shade='edge')
    c.ellipse(32, 27, 9, 10.5, 'skin', shade='sphere')
    c.rrect(21, 13, 22, 7, 'hairbr', shade='edge')                          # fringe
    c.rect(40, 10, 4, 6, 'paper', shade='flat'); c.rect(42, 12, 4, 6, 'paper', shade='light')   # index-card pins
    c.dots([(41, 12), (43, 14)], ramp('red', 2))
    # spectacles
    for ex in (27, 37):
        c.dots([(ex - 2, 26), (ex + 2, 26), (ex - 2, 28), (ex + 2, 28), (ex - 1, 25), (ex, 25), (ex + 1, 25), (ex - 1, 29), (ex, 29), (ex + 1, 29)], ramp('gold', 2))
    c.dots([(30, 26), (31, 26), (32, 26), (33, 26), (34, 26)], ramp('gold', 1))
    if f == 1:
        c.dots([(26, 27), (27, 27), (28, 27), (36, 27), (37, 27), (38, 27)], ramp('skin', 0))
    else:
        c.dots([(27, 27), (37, 27)], '#140c1c'); c.dots([(26, 26), (36, 26)], '#bfe8ff')
    c.dots([(31, 33), (32, 33), (33, 33)] if f == 0 else [(31, 33), (32, 34), (33, 33)], ramp('flesh2', 1))
    c.dots([(25, 31), (39, 31)], ramp('pink', 2))
    img = c.render()
    return img


def mordecai(f=0):
    """Mordecai the Smith — soot, braided beard, leather apron, hammer over the shoulder."""
    c = Cv(64, 64)
    c.poly([(4, 64), (10, 42), (24, 36), (40, 36), (54, 42), (60, 64)], 'skin2', shade='edge')     # shoulders
    c.poly([(18, 44), (46, 44), (48, 64), (16, 64)], 'leather', shade='edge')                       # apron
    c.rect(20, 46, 24, 2, 'brown', shade='dark')
    c.line([(52, 10), (40, 50)], 'wood', width=3, shade='edge_soft')                                 # hammer
    c.rrect(46, 4, 14, 9, 'steel', shade='edge')
    c.ellipse(32, 24, 11, 12, 'skin2', shade='sphere')
    c.ellipse(32, 14, 11, 5, 'hairbr', shade='sphere')
    c.poly([(21, 28), (43, 28), (40, 40), (35, 46), (32, 52), (29, 46), (24, 40)], 'hairbr', shade='edge')  # beard
    c.rect(30, 44, 4, 8, 'hairbr', shade='edge'); c.rect(30, 48, 4, 1, 'gold', shade='flat')
    c.rrect(25, 30, 14, 3, 'hairbr', shade='dark')                                                   # moustache
    c.rect(22, 21, 8, 2, 'hairbr', shade='flat'); c.rect(34, 21, 8, 2, 'hairbr', shade='flat')       # brows
    if f == 1:
        c.dots([(25, 25), (26, 25), (27, 25), (37, 25), (38, 25), (39, 25)], ramp('skin2', 0))
    else:
        c.dots([(26, 24), (26, 25), (38, 24), (38, 25)], '#140c1c')
    c.dots([(24, 27), (25, 28), (40, 27)], ramp('ink', 2))                                           # soot
    c.dots([(31, 27), (32, 28), (33, 27)], ramp('skin2', 1))
    img = c.render()
    put(img, [(10, 50), (14, 54), (50, 56)], '#ffb030')                                              # forge embers
    return img


def shade_of_order(f=0):
    """Shade of the Order — ghost of a fallen knight who appears at save statues to teach."""
    c = Cv(64, 64)
    c.poly([(8, 64), (14, 40), (26, 34), (38, 34), (50, 40), (56, 64)], 'ectoplasm', shade='edge')
    c.poly([(22, 38), (42, 38), (44, 64), (20, 64)], 'steel', shade='edge')
    c.dots([(28, 46), (36, 46), (32, 50), (28, 54), (36, 54)], ramp('ectoplasm', 3))
    c.ellipse(32, 22, 11, 12, 'steel', shade='sphere')
    c.rect(22, 21, 20, 3, 'ink', shade='flat')
    c.rect(31, 24, 2, 6, 'ink', shade='flat')
    c.poly([(30, 10), (34, 10), (40, 2), (36, 2)], 'ectoplasm', shade='light')                       # spectral plume
    glow = '#bffff0' if f == 0 else '#7ff0d0'
    c.dots([(25, 22), (26, 22), (37, 22), (38, 22)], glow)
    img = c.render()
    # ghostly translucency + rising motes
    px_ = img.load()
    for y in range(64):
        for x in range(64):
            r, g, b, a = px_[x, y]
            if a:
                px_[x, y] = (r, g, b, int(a * (0.55 + 0.45 * (1 - y / 64.0)) if y > 40 else a * 0.92))
    rng = random.Random(f + 3)
    put(img, [(rng.randint(10, 54), rng.randint(4, 60)) for _ in range(12)], '#7ff0d0')
    return img


# ===========================================================================
# VFX (64x64 frames, unoutlined, bright additive-looking colours)
# ===========================================================================
def vfx_slash():
    frames = []
    for k in range(5):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        a0, a1 = -70 + k * 12, -70 + 40 + k * 38
        r = 24
        thick = [2, 4, 5, 3, 1][k]
        for a in range(a0, a1, 2):
            for t in range(thick):
                rr = r - t
                x = 32 + rr * math.sin(math.radians(a)); y = 34 - rr * math.cos(math.radians(a))
                col = '#ffffff' if t < thick - 1 else '#7ec8ff'
                if k >= 3 and (a // 2) % 2:
                    col = '#9fe8ff'
                put(im, [(x, y)], col)
        frames.append(im)
    return frames, [40, 40, 50, 60, 70]


def hexagon(cx, cy, r, rotd=30):
    return [(cx + r * math.cos(math.radians(a + rotd)), cy + r * math.sin(math.radians(a + rotd))) for a in range(0, 360, 60)]


def vfx_hex_shield():
    """GDD Perfect Guard: hexagonal blue Aegis that reflects the blow."""
    frames = []
    for k in range(6):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
        r = [8, 16, 22, 24, 25, 26][k]
        alpha = [200, 170, 140, 120, 80, 40][k]
        d.polygon(hexagon(32, 32, r), fill=(60, 140, 255, alpha // 2))
        d.polygon(hexagon(32, 32, r), outline=(190, 240, 255, 255 if k < 5 else 140))
        if r > 10:
            d.polygon(hexagon(32, 32, r - 5), outline=(126, 200, 255, alpha))
            # honeycomb cells
            for q in range(6):
                ang = math.radians(q * 60 + 30)
                d.polygon(hexagon(32 + (r - 9) * math.cos(ang), 32 + (r - 9) * math.sin(ang), 4), outline=(126, 200, 255, alpha))
        if k in (1, 2):
            put(im, [(32 + r * math.cos(math.radians(a)), 32 + r * math.sin(math.radians(a))) for a in range(-20, 20, 4)], '#ffffff')
        frames.append(im)
    return frames, [40, 50, 60, 80, 90, 110]


def vfx_shatter():
    """GDD Break: the guard shatters like glass."""
    rng = random.Random(11)
    shards = [(rng.uniform(0, 2 * math.pi), rng.uniform(0.6, 1.4), rng.randint(2, 4)) for _ in range(18)]
    frames = []
    for k in range(6):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
        if k <= 1:
            for q in range(7):   # crack star
                ang = q * 0.9 + 0.3
                L = 10 + k * 10
                pts = [(32, 32)]
                for s in range(1, 4):
                    pts.append((32 + math.cos(ang + rng.uniform(-0.2, 0.2) * s) * L * s / 3, 32 + math.sin(ang) * L * s / 3))
                d.line(pts, fill=(230, 245, 255, 255))
        for (ang, sp, sz) in shards:
            dist = (k + 0.5) * 6 * sp
            x = 32 + math.cos(ang) * dist; y = 32 + math.sin(ang) * dist + k * k * 0.6
            a = 255 if k < 4 else 140
            tri = [(x, y - sz), (x + sz, y + sz * 0.6), (x - sz * 0.7, y + sz * 0.4)]
            d.polygon(tri, fill=(150, 210, 240, a), outline=(255, 255, 255, a))
        frames.append(im)
    return frames, [50, 60, 60, 70, 80, 100]


def vfx_arcane():
    """Grimoire spell circle with rotating algorithm runes."""
    frames = []
    runes = ['#7ec8ff', '#d6a8ff', '#ffe86a']
    for k in range(6):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
        r = [6, 14, 20, 22, 22, 18][k]
        a = [255, 255, 230, 200, 150, 80][k]
        d.ellipse([32 - r, 32 - r, 32 + r, 32 + r], outline=(160, 120, 255, a))
        d.ellipse([32 - r + 4, 32 - r + 4, 32 + r - 4, 32 + r - 4], outline=(126, 200, 255, a)) if r > 6 else None
        for q in range(8):
            ang = math.radians(q * 45 + k * 15)
            x = 32 + (r - 2) * math.cos(ang); y = 32 + (r - 2) * math.sin(ang)
            put(im, [(x, y), (x + 1, y), (x, y + 1)], runes[q % 3])
        if k in (2, 3):
            d.polygon(hexagon(32, 32, r - 8, rotd=k * 10), outline=(255, 232, 106, a))
        frames.append(im)
    return frames, [50, 60, 70, 80, 90, 100]


def vfx_spark():
    frames = []
    for k in range(4):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        L = [6, 12, 16, 10][k]
        col = ['#ffffff', '#fff47e', '#ffb030', '#e85a42'][k]
        for q in range(8):
            ang = math.radians(q * 45 + (22 if k % 2 else 0))
            ln = L if q % 2 == 0 else L // 2
            put(im, [(32 + math.cos(ang) * s, 32 + math.sin(ang) * s) for s in range(max(1, k * 2), ln)], col)
        put(im, [(31, 31), (32, 31), (31, 32), (32, 32)], '#ffffff') if k < 2 else None
        frames.append(im)
    return frames, [40, 50, 60, 70]


def vfx_heal():
    rng = random.Random(5)
    motes = [(rng.randint(14, 50), rng.randint(36, 60), rng.choice(['#a8e272', '#7ff0d0', '#ffffff'])) for _ in range(14)]
    frames = []
    for k in range(6):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        for i, (x, y, col) in enumerate(motes):
            yy = y - k * 5 - (i % 3) * 2
            if i % 4 == 0:   # small plus signs
                put(im, [(x, yy), (x - 1, yy), (x + 1, yy), (x, yy - 1), (x, yy + 1)], col)
            else:
                put(im, [(x, yy)], col)
        frames.append(im)
    return frames, [80] * 6


def vfx_poison():
    frames = []
    for k in range(4):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
        for i, (x, y) in enumerate(((26, 44), (36, 40), (31, 34), (40, 30))):
            r = 2 + ((k + i) % 3)
            yy = y - k * 3
            d.ellipse([x - r, yy - r, x + r, yy + r], outline=(168, 226, 114, 255), fill=(56, 138, 60, 160))
        frames.append(im)
    return frames, [110] * 4


# ===========================================================================
# THEMED ARRAY PIECES (GDD §17) — tiles stack vertically so height ~ value
# ===========================================================================
def book_pieces():
    """Act I: book spines on a shelf. body tile tints per element in-engine."""
    top = Cv(16, 6); top.rrect(1, 1, 14, 5, 'paper', shade='edge'); top.rect(2, 1, 12, 1, 'paper', shade='light')
    body = Cv(16, 4); body.rect(1, 0, 14, 4, 'pale', shade='flat'); body.rect(1, 0, 1, 4, 'pale', shade='light'); body.rect(14, 0, 1, 4, 'pale', shade='dark')
    body_img = body.render(outline=False)
    put(body_img, [(0, y) for y in range(4)] + [(15, y) for y in range(4)], '#140c1c')
    band = Cv(16, 4); band.rect(1, 0, 14, 4, 'pale', shade='flat'); band.rect(1, 1, 14, 2, 'gold', shade='light')
    band_img = band.render(outline=False)
    put(band_img, [(0, y) for y in range(4)] + [(15, y) for y in range(4)], '#140c1c')
    base = Cv(16, 4); base.rect(0, 0, 16, 4, 'wood', shade='edge')
    return {'top': top.render(), 'body': body_img, 'band': band_img, 'base': base.render(outline=False)}


def candle_pieces():
    """Act II: bone-wax candles; the flame is a 3-frame strip."""
    flames = []
    for k in range(3):
        f = Image.new('RGBA', (16, 12), (0, 0, 0, 0))
        h = [7, 9, 8][k]; sway = [0, 1, -1][k]
        for y in range(h):
            w = max(1, int(3 * math.sin(math.pi * (y + 1) / (h + 1))))
            for x in range(-w, w + 1):
                col = '#fff47e' if abs(x) < w - 1 or w == 1 else '#ffb030'
                if y < 2:
                    col = '#e85a42' if abs(x) == w else '#ffb030'
                put(f, [(8 + x + (sway if y > h // 2 else 0), 11 - y)], col)
        put(f, [(8, 11)], '#3a2806')
        flames.append(f)
    strip = Image.new('RGBA', (48, 12), (0, 0, 0, 0))
    for i, f in enumerate(flames):
        strip.alpha_composite(f, (i * 16, 0))
    body = Cv(16, 4); body.rect(3, 0, 10, 4, 'bone', shade='flat'); body.rect(3, 0, 2, 4, 'bone', shade='light'); body.rect(11, 0, 2, 4, 'bone', shade='dark')
    body_img = body.render(outline=False)
    put(body_img, [(2, y) for y in range(4)] + [(13, y) for y in range(4)], '#2a1f18')
    drip = body_img.copy(); put(drip, [(6, 0), (6, 1), (6, 2), (7, 0)], ramp('bone', 3))
    base = Cv(16, 4); base.rrect(0, 0, 16, 4, 'rust', shade='edge')
    return {'top': strip, 'body': body_img, 'band': drip, 'base': base.render(outline=False)}


def crystal_pieces():
    """Act III: split violet crystals that refract the tower's broken magic."""
    top = Cv(16, 8); top.poly([(2, 7), (8, 0), (14, 7)], 'crystal', shade='edge')
    body = Cv(16, 4); body.rect(2, 0, 12, 4, 'crystal', shade='flat'); body.rect(2, 0, 3, 4, 'crystal', shade='light'); body.rect(11, 0, 3, 4, 'crystal', shade='dark')
    body_img = body.render(outline=False)
    put(body_img, [(1, y) for y in range(4)] + [(14, y) for y in range(4)], '#1c0a34')
    crack = body_img.copy(); put(crack, [(6, 0), (7, 1), (8, 2), (9, 3)], ramp('crystal', 3))
    base = Cv(16, 4); base.rrect(0, 0, 16, 4, 'obsid', shade='edge')
    return {'top': top.render(), 'body': body_img, 'band': crack, 'base': base.render(outline=False)}


# ===========================================================================
# ICONS
# ===========================================================================
def logic_fragment(k):
    """GDD Logic Fragment: a spinning shard of the Great Catalog (4-frame spin)."""
    c = Cv(16, 16)
    w = [5, 3, 1, 3][k]
    c.poly([(8, 1), (8 + w, 7), (8, 14), (8 - w, 7)], 'aegis', shade='sphere' if w > 1 else 'light')
    c.line([(8, 2), (8, 13)], 'glass', width=1, shade='light')
    img = c.render()
    if k == 0:
        put(img, [(5, 4)], '#ffffff')
    return img


def panic_icon(n):
    """Panic stacks 1..3 (GDD §15): a cracked mind-gem filling with red."""
    c = Cv(12, 12)
    c.ellipse(6, 6, 5, 5, 'obsid', shade='sphere')
    img = c.render()
    reds = [(4, 7), (6, 8), (8, 7), (5, 5), (7, 5), (6, 3)]
    put(img, reds[: n * 2], '#ff4a3a')
    put(img, [(6, 2), (5, 4), (7, 6)], '#ffffff')
    return img


# ===========================================================================
def export_all(roots, manifest):
    def save(rel, im):
        for base in roots:
            p = os.path.join(base, rel)
            os.makedirs(os.path.dirname(p), exist_ok=True)
            im.save(p)

    def strip(frames):
        w, h = frames[0].size
        s = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
        for i, f in enumerate(frames):
            s.alpha_composite(f, (i * w, 0))
        return s

    for name, fn in (('lyra', lyra), ('mordecai', mordecai), ('shade', shade_of_order)):
        save(f'npc/{name}.png', strip([fn(0), fn(0), fn(0), fn(1)]))
        manifest.setdefault('npc_' + name, {})['idle'] = {'frames': 4, 'size': 64, 'durations_ms': [900, 600, 900, 140], 'loop': True}
    for name, fn in (('slash', vfx_slash), ('hex_shield', vfx_hex_shield), ('shatter', vfx_shatter),
                     ('arcane', vfx_arcane), ('spark', vfx_spark), ('heal', vfx_heal), ('poison', vfx_poison)):
        frames, durs = fn()
        save(f'vfx/{name}.png', strip(frames))
        manifest.setdefault('vfx', {})[name] = {'frames': len(frames), 'size': 64, 'durations_ms': durs, 'loop': False}
    for theme, fn in (('book', book_pieces), ('candle', candle_pieces), ('crystal', crystal_pieces)):
        for part, im in fn().items():
            save(f'array/{theme}_{part}.png', im)
    save('items/logic_fragment.png', strip([logic_fragment(k) for k in range(4)]))
    for n in (1, 2, 3):
        save(f'ui/panic_{n}.png', panic_icon(n))


def preview():
    rows = {}
    for name, fn in (('lyra', lyra), ('mordecai', mordecai), ('shade', shade_of_order)):
        rows['npc ' + name] = ([fn(0), fn(1)], [0, 0])
    for name, fn in (('slash', vfx_slash), ('hex_shield', vfx_hex_shield), ('shatter', vfx_shatter),
                     ('arcane', vfx_arcane), ('spark', vfx_spark), ('heal', vfx_heal), ('poison', vfx_poison)):
        rows['vfx ' + name] = fn()
    return rows
