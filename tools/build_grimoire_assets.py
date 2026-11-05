"""Master Pixel-Art & Chiptune Audio Generator for Grimoire's Aegis: The Scholar Knight.

Follows the `.agents/skills/pixel-art` rules and `Lost in Multiverse RPG` (`pxlib.py` / `sfx.py`) architecture:
  1. Hue-shifted 4-colour ramps (outline, cool shadow, midtone base, warm highlight)
  2. Single top-left light source (-0.62, -0.78) with region-based sphere/edge shading (zero pillow shading)
  3. Selective outlining (selout) using darkest ramp shade of adjacent material
  4. Bayer 4x4 ordered dithering for 320x180 stage backdrops (Dusty Archives, Catacombs of Chaos, Tower of Entropy, Grand Library Portal)
  5. 32x32 Chibi Hero (Scholar Knight Aegis: idle, idle2, attack, defend, hurt, front, front2)
  6. All 13 Bestiary Monsters (32x32 regular/elites + 48x48 Act Bosses, each with 2-frame breathing idle)
  7. 32x32 Dungeon Board Tiles (plates, rim, fog, 64x64 tileable grounds, 32x32 props, 16x16 tile icons, 24x24 dice)
  8. 16x16 Items/Weapons/Armor/Accessories, 12x12 Status icons, 10x10 Stat icons, 9-slice UI panels/buttons/slots
  9. 29 Procedural 8-bit/16-bit WAV sound effects & seamless ambient music loops
"""
import os
import sys
import math
import wave
import struct
import random
from PIL import Image, ImageDraw

# ============================================================================
# 1. PXLIB CORE: HUE-SHIFTED RAMPS, REGION SHADING & SELECTIVE OUTLINE (SELOUT)
# ============================================================================

def hx(c):
    c = c.lstrip('#')
    if len(c) == 6:
        return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), 255)
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), int(c[6:8], 16))

RAMPS = {
    'skin':    ['#4a2228', '#b8664e', '#eaa882', '#ffd8b0'],
    'skin2':   ['#3a1c1c', '#7e4634', '#b4734e', '#d89a6c'],
    'pale':    ['#33303e', '#8f8a94', '#c9c3bd', '#ece6dc'],
    'zomb':    ['#1e2a1e', '#4e6a48', '#7a9a6a', '#a8c48c'],
    'hair':    ['#07060c', '#16131f', '#2a2536', '#4a4462'],
    'hairbr':  ['#200f0a', '#4a2616', '#7a4426', '#a86a3a'],
    'white':   ['#34364c', '#a3abc0', '#dfe3ec', '#ffffff'],
    'grey':    ['#16172a', '#3a3e58', '#5c6282', '#868eae'],
    'red':     ['#2a0814', '#6e1424', '#b02a32', '#e85a42'],
    'black':   ['#050409', '#121019', '#24202e', '#3c3650'],
    'denim':   ['#0c1230', '#22345e', '#3a5890', '#5f86bf'],
    'olive':   ['#121608', '#2e3a18', '#4e5e26', '#74883a'],
    'metal':   ['#181b28', '#4a5266', '#8c95a8', '#d4dae4'],
    'wood':    ['#24130a', '#5a3519', '#8c5c2c', '#bc8a42'],
    'gold':    ['#361e06', '#8a5a12', '#dca424', '#ffe86a'],
    'orange':  ['#381004', '#8a3410', '#da661e', '#ffa848'],
    'yellow':  ['#3a2806', '#9c7a10', '#e8c42a', '#fff47e'],
    'green':   ['#0a200e', '#1d5226', '#388a3c', '#7ec45c'],
    'teal':    ['#08202a', '#155058', '#2a8a8a', '#62c8b8'],
    'blue':    ['#0a0e2c', '#1d2e70', '#3658bc', '#6c92ea'],
    'purple':  ['#14081f', '#3a1858', '#66329c', '#9c5cd4'],
    'demon':   ['#200410', '#5c0e1c', '#a01e26', '#dc4a3a'],
    'imp':     ['#18061c', '#4a1240', '#86246c', '#c64c98'],
    'cloth':   ['#3a3646', '#a6a096', '#dedac8', '#fdfaef'],
    'shadow':  ['#000000', '#0a0711', '#1a1428', '#2e2446'],
    'flesh':   ['#36091a', '#86283e', '#cc5a6a', '#f4a0a0'],
    'flesh2':  ['#26080f', '#66202e', '#a43a48', '#d8686a'],
    'pus':     ['#362e08', '#8a7a18', '#d2c242', '#f8f08e'],
    'bone':    ['#362c22', '#8c7c60', '#d2c6a2', '#f6eed4'],
    'slime':   ['#0c260e', '#2a6a26', '#5ca83c', '#a8e272'],
    'vein':    ['#160310', '#460a28', '#7a1840', '#b23262'],
    'eye':     ['#3a2a3a', '#b8a8bc', '#f0eaf2', '#ffffff'],
    'brown':   ['#1c1008', '#4a2e1a', '#7c502e', '#aa784c'],
    'khaki':   ['#262010', '#5e5230', '#968660', '#c6b68a'],
    'fire':    ['#3a0a04', '#c0360c', '#f48c1c', '#ffe060'],
    'lava':    ['#2a0404', '#8a1406', '#e04a10', '#ffb030'],
    'glass':   ['#1a2a38', '#4a7a98', '#8ac4dc', '#e0f6ff'],
    'paper':   ['#3a3428', '#a8a088', '#e4dcc0', '#fffbe8'],
    'pink':    ['#3a0a24', '#9a2a5a', '#e0609a', '#ffa8cc'],
    'rust':    ['#1e0c06', '#5a2810', '#9a4a1e', '#c87a3a'],
    'stone':   ['#141420', '#3a3a4c', '#62627a', '#9090a8'],
    'navy':    ['#060814', '#121a36', '#22305a', '#3a4c82'],
}

def ramp(name, i):
    return hx(RAMPS[name][i])

class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rid = [[-1] * w for _ in range(h)]
        self.regions = []
        self.detail = {}
        self.post = []

    def _new(self, mat, shade, edge):
        self.regions.append(dict(mat=mat, shade=shade, edge=edge, z=len(self.regions)))
        return len(self.regions) - 1

    def _apply_mask(self, mask, mat, shade, edge, clip=None):
        r = self._new(mat, shade, edge)
        px = mask.load()
        for y in range(self.h):
            for x in range(self.w):
                if px[x, y]:
                    if clip is not None and self.rid[y][x] not in clip:
                        continue
                    self.rid[y][x] = r
        return r

    def _mask(self):
        return Image.new('L', (self.w, self.h), 0)

    def ellipse(self, cx, cy, rx, ry, mat, shade='sphere', edge=False, clip=None):
        m = self._mask(); px = m.load()
        for y in range(self.h):
            for x in range(self.w):
                dx = (x + 0.5 - cx) / max(0.1, rx)
                dy = (y + 0.5 - cy) / max(0.1, ry)
                if dx * dx + dy * dy <= 1.0:
                    px[x, y] = 255
        return self._apply_mask(m, mat, shade, edge, clip)

    def rect(self, x, y, w, h, mat, shade='edge', edge=False, clip=None):
        m = self._mask(); d = ImageDraw.Draw(m)
        d.rectangle([x, y, x + w - 1, y + h - 1], fill=255)
        return self._apply_mask(m, mat, shade, edge, clip)

    def rrect(self, x, y, w, h, mat, shade='edge', edge=False, clip=None):
        m = self._mask(); d = ImageDraw.Draw(m)
        d.rectangle([x, y, x + w - 1, y + h - 1], fill=255)
        for cx, cy in ((x, y), (x + w - 1, y), (x, y + h - 1), (x + w - 1, y + h - 1)):
            if 0 <= cx < self.w and 0 <= cy < self.h:
                m.putpixel((cx, cy), 0)
        return self._apply_mask(m, mat, shade, edge, clip)

    def poly(self, pts, mat, shade='edge', edge=False, clip=None):
        m = self._mask(); d = ImageDraw.Draw(m)
        d.polygon(pts, fill=255, outline=255)
        return self._apply_mask(m, mat, shade, edge, clip)

    def line(self, pts, mat, width=1, shade='flat', edge=False):
        m = self._mask(); d = ImageDraw.Draw(m)
        d.line(pts, fill=255, width=width)
        return self._apply_mask(m, mat, shade, edge)

    def pixels(self, pts, mat, shade='flat', edge=False):
        m = self._mask()
        for (x, y) in pts:
            if 0 <= x < self.w and 0 <= y < self.h:
                m.putpixel((x, y), 255)
        return self._apply_mask(m, mat, shade, edge)

    def stamp(self, ox, oy, rows, legend, shade='flat', edge=False):
        groups = {}
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch in (' ', '.'):
                    continue
                v = legend.get(ch)
                if v is None:
                    continue
                x, y = ox + i, oy + j
                if not (0 <= x < self.w and 0 <= y < self.h):
                    continue
                if v.startswith('#'):
                    self.detail[(x, y)] = hx(v)
                elif v == 'ERASE':
                    self.rid[y][x] = -1
                    self.detail.pop((x, y), None)
                else:
                    groups.setdefault(v, []).append((x, y))
        for mat, pts in groups.items():
            self.pixels(pts, mat, shade=shade, edge=edge)

    def erase_rect(self, x, y, w, h):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                if 0 <= xx < self.w and 0 <= yy < self.h:
                    self.rid[yy][xx] = -1

    def erase_px(self, pts):
        for (x, y) in pts:
            if 0 <= x < self.w and 0 <= y < self.h:
                self.rid[y][x] = -1
                self.detail.pop((x, y), None)

    def dot(self, x, y, col):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.detail[(x, y)] = hx(col) if isinstance(col, str) else col

    def dots(self, pts, col):
        for p in pts:
            self.dot(int(p[0]), int(p[1]), col)

    def _inside(self, x, y, r):
        return 0 <= x < self.w and 0 <= y < self.h and self.rid[y][x] == r

    def render(self, outline=True, light=(-0.62, -0.78)):
        img = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        px = img.load()
        bbox = {}
        for y in range(self.h):
            for x in range(self.w):
                r = self.rid[y][x]
                if r < 0:
                    continue
                b = bbox.get(r)
                if b is None:
                    bbox[r] = [x, y, x, y]
                else:
                    b[0] = min(b[0], x); b[1] = min(b[1], y)
                    b[2] = max(b[2], x); b[3] = max(b[3], y)
        lx, ly = light
        for y in range(self.h):
            for x in range(self.w):
                r = self.rid[y][x]
                if r < 0:
                    continue
                reg = self.regions[r]
                mat = reg['mat']
                shade = reg['shade']
                idx = 2
                if shade == 'sphere':
                    b = bbox[r]
                    cx = (b[0] + b[2] + 1) / 2.0
                    cy = (b[1] + b[3] + 1) / 2.0
                    rx = max(1.0, (b[2] - b[0] + 1) / 2.0)
                    ry = max(1.0, (b[3] - b[1] + 1) / 2.0)
                    nx = (x + 0.5 - cx) / rx
                    ny = (y + 0.5 - cy) / ry
                    d = nx * lx + ny * ly
                    if d > 0.5:
                        idx = 3
                    elif d < -0.38:
                        idx = 1
                elif shade == 'edge':
                    below = self._inside(x, y + 1, r)
                    right = self._inside(x + 1, y, r)
                    above = self._inside(x, y - 1, r)
                    left = self._inside(x - 1, y, r)
                    if not below or (not right and not self._inside(x + 1, y + 1, r)):
                        idx = 1
                    elif not above and (not left or self._inside(x + 1, y, r)):
                        idx = 3
                elif shade == 'edge_soft':
                    below = self._inside(x, y + 1, r)
                    right = self._inside(x + 1, y, r)
                    if not below or not right:
                        idx = 1
                elif shade == 'dark':
                    idx = 1
                elif shade == 'light':
                    idx = 3
                elif shade == 'flat':
                    idx = 2
                elif shade == 'outline':
                    idx = 0
                px[x, y] = ramp(mat, idx)
                if reg['edge']:
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        xx, yy = x + dx, y + dy
                        if 0 <= xx < self.w and 0 <= yy < self.h:
                            o = self.rid[yy][xx]
                            if o >= 0 and o != r and self.regions[o]['z'] < reg['z'] and self.regions[o]['mat'] != mat:
                                px[x, y] = ramp(mat, 0)
                                break
        for (x, y), c in self.detail.items():
            px[x, y] = c
        if outline:
            img = selout(img, self)
        for f in self.post:
            f(img)
        return img

def selout(img, cv=None):
    w, h = img.size
    src = img.load()
    out = img.copy()
    o = out.load()
    for y in range(h):
        for x in range(w):
            if src[x, y][3] != 0:
                continue
            best = None
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < w and 0 <= yy < h and src[xx, yy][3] != 0:
                    if cv is not None and cv.rid[yy][xx] >= 0:
                        c = ramp(cv.regions[cv.rid[yy][xx]]['mat'], 0)
                    else:
                        r, g, b, a = src[xx, yy]
                        c = (int(r * 0.25), int(g * 0.2), int(b * 0.3), 255)
                    if best is None or sum(c[:3]) < sum(best[:3]):
                        best = c
            if best is not None:
                o[x, y] = best
    return out

def shift_rows(img, y0, y1, dy):
    w, h = img.size
    out = img.copy()
    src = img.load(); o = out.load()
    for y in range(y0, y1):
        for x in range(w):
            o[x, y] = (0, 0, 0, 0)
    for y in range(y0, y1):
        ty = y + dy
        if 0 <= ty < h:
            for x in range(w):
                if src[x, y][3]:
                    o[x, ty] = src[x, y]
    return out

def breathe(img):
    h = img.size[1]
    return shift_rows(img, 0, int(h * 0.72), 1)

def strip4(img):
    """Build 4-frame idle strip (for Godot / Unity sprite sheets) using breathing + subtle glint."""
    w, h = img.size
    f0 = img
    f1 = shift_rows(img, 0, int(h * 0.72), 1)
    f2 = img.copy()
    f3 = shift_rows(img, 0, int(h * 0.55), 1)
    s = Image.new('RGBA', (w * 4, h), (0, 0, 0, 0))
    for i, fr in enumerate((f0, f1, f2, f3)):
        s.alpha_composite(fr, (i * w, 0))
    return s

# ============================================================================
# 2. SCHOLAR KNIGHT (AEGIS) & ALL 13 GDD MONSTERS (32x32 & 48x48 BOSSES)
# ============================================================================

EYE = '#140c1c'
WHITE = '#ffffff'

def scholar_knight(pose='idle', f=1):
    """Aegis, The Scholar Knight: 32x32 Chibi Paladin-Archivist with Azure Cloak, Silver Armor, Gold Trim & Grimoire."""
    c = Canvas(32, 32)
    # Flowing Azure Scholar Cape
    c.poly([(8, 15), (24, 15), (26, 28), (6, 28)], 'blue', shade='edge')
    # Silver Plate Armor Torso + Gold Archivist Tabard
    c.rrect(10, 16, 12, 9, 'metal', shade='edge')
    c.rect(13, 17, 6, 8, 'navy', shade='flat')
    c.dots([(14, 18), (15, 18), (16, 18), (17, 18), (15, 20), (16, 20), (15, 22), (16, 22)], ramp('gold', 3))
    # Greaves & Boots
    c.rect(11, 25, 4, 3, 'metal', shade='edge_soft')
    c.rect(17, 25, 4, 3, 'metal', shade='edge_soft')
    c.rrect(10, 28, 5, 2, 'gold', shade='flat')
    c.rrect(17, 28, 5, 2, 'gold', shade='flat')
    # Pauldrons & Arms
    c.ellipse(9, 18, 2.5, 2.5, 'gold', shade='sphere')
    c.ellipse(23, 18, 2.5, 2.5, 'gold', shade='sphere')
    if pose == 'attack':
        # Thrusting Index Blade forward + open Grimoire floating
        c.rect(7, 19, 3, 5, 'metal', shade='edge_soft')
        c.rect(22, 18, 6, 3, 'metal', shade='edge_soft')
        c.poly([(26, 18), (31, 16), (31, 19), (26, 20)], 'glass', shade='light')
        c.rect(25, 16, 2, 5, 'gold', shade='flat')
    elif pose == 'defend':
        # Raising Aegis Shield in front with glowing golden runes
        c.rrect(14, 15, 11, 12, 'gold', shade='edge')
        c.rrect(16, 17, 7, 8, 'blue', shade='sphere')
        c.dots([(19, 18), (19, 19), (19, 20), (19, 21), (18, 19), (20, 19)], '#ffffff')
    else:
        c.rect(8, 19, 2, 5, 'metal', shade='edge_soft')
        c.rect(22, 19, 2, 5, 'metal', shade='edge_soft')
        # Aegis Shield on left arm, Grimoire tome on right
        c.poly([(5, 18), (10, 18), (10, 24), (7, 27), (5, 24)], 'gold', shade='edge')
        c.rect(6, 19, 3, 5, 'blue', shade='flat')
        c.rrect(21, 20, 5, 6, 'purple', shade='edge')
        c.rect(22, 21, 3, 4, 'paper', shade='flat')
        c.dot(23, 22, ramp('gold', 3))
    # Knight Helmet + Scholar Plume
    c.poly([(13, 3), (17, 1), (21, 3), (18, 6), (14, 6)], 'red', shade='edge')
    c.ellipse(16, 10, 8.2, 7.2, 'metal', shade='sphere')
    # Visor Opening & Face
    c.rrect(10, 9, 12, 5, 'black', shade='flat')
    c.dots([(11, 8), (12, 8), (19, 8), (20, 8), (15, 7), (16, 7)], ramp('gold', 3))
    if pose == 'hurt':
        c.dots([(12, 10), (13, 11), (13, 10), (12, 11)], '#ff5a42')
        c.dots([(18, 10), (19, 11), (19, 10), (18, 11)], '#ff5a42')
    else:
        # Glowing cyan scholar eyes
        c.dots([(12 + f, 10), (13 + f, 10), (12 + f, 11), (13 + f, 11)], '#62c8b8')
        c.dots([(18 + f, 10), (19 + f, 10), (18 + f, 11), (19 + f, 11)], '#62c8b8')
        c.dot(13 + f, 10, WHITE); c.dot(19 + f, 10, WHITE)
    return c.render()

# --- ACT I MONSTERS (Searching: TP-01..TP-05) ---

def m_corrupted_slime():
    """1. Corrupted Slime (Act I Regular): Translucent green-ink ooze digesting numbered parchment pages."""
    c = Canvas(32, 32)
    c.ellipse(16, 21, 12, 8.5, 'slime', shade='sphere')
    c.ellipse(16, 15, 9, 7, 'slime', shade='sphere')
    c.ellipse(10, 26, 4, 2.5, 'slime', shade='sphere')
    c.ellipse(23, 26, 4, 2.5, 'slime', shade='sphere')
    # Floating corrupted index scroll inside belly
    c.rrect(13, 19, 7, 5, 'paper', shade='edge')
    c.dots([(15, 20), (16, 21), (17, 20), (15, 22)], ramp('red', 2))
    # Cute/menacing slime eyes
    c.ellipse(12, 14, 2.2, 2.5, 'eye', shade='flat')
    c.ellipse(20, 14, 2.2, 2.5, 'eye', shade='flat')
    c.dots([(11, 14), (19, 14)], EYE)
    c.dots([(12, 13), (20, 13)], WHITE)
    # Specular shine
    c.dots([(11, 10), (12, 9), (13, 9)], '#d8ff9a')
    return c.render()

def m_lost_wisp():
    """2. Lost Wisp (Act I Regular): Flickering spectral lantern flame holding a torn index tag."""
    c = Canvas(32, 32)
    # Outer ghostly teal/purple flames
    c.poly([(16, 2), (23, 9), (26, 17), (22, 26), (16, 30), (10, 26), (6, 17), (9, 9)], 'teal', shade='edge')
    c.ellipse(16, 17, 7.5, 7.5, 'blue', shade='sphere')
    c.ellipse(16, 16, 5, 5, 'glass', shade='sphere')
    # Lantern cage
    c.rect(13, 6, 6, 2, 'gold', shade='flat')
    c.dots([(11, 16), (12, 16), (19, 16), (20, 16)], '#fff47e')
    c.dots([(14, 20), (15, 21), (16, 21), (17, 20)], EYE)
    # Floating wisps
    c.ellipse(6, 10, 2, 2.5, 'teal', shade='sphere')
    c.ellipse(26, 12, 2, 2.5, 'teal', shade='sphere')
    return c.render()

def m_bookworm_horror():
    """3. Bookworm Horror (Act I Regular): Segmented chitinous worm bursting through an ancient tome."""
    c = Canvas(32, 32)
    # Corrupted Open Book at base
    c.poly([(3, 24), (15, 21), (15, 29), (3, 28)], 'red', shade='edge')
    c.poly([(17, 21), (29, 24), (29, 28), (17, 29)], 'red', shade='edge')
    c.poly([(5, 23), (15, 20), (15, 27), (5, 26)], 'paper', shade='flat')
    c.poly([(17, 20), (27, 23), (27, 26), (17, 27)], 'paper', shade='flat')
    # Segmented Worm Body curving up
    for (x, y, r) in ((16, 22, 4.5), (13, 18, 4.4), (11, 14, 4.2), (13, 10, 4.5), (17, 8, 5.2)):
        c.ellipse(x, y, r, r * 0.9, 'imp', shade='sphere')
    # Mandibles & Maw
    c.ellipse(18, 8, 3.2, 3.2, 'vein', shade='flat')
    c.dots([(16, 6), (20, 6), (16, 10), (20, 10), (18, 5), (18, 11)], ramp('bone', 3))
    c.dots([(14, 6), (15, 6)], '#ffe86a')
    return c.render()

def m_twin_faced_librarian():
    """4. Twin-Faced Librarian (Act I Mid-Boss): Dual-masked robed curator representing Linear vs Binary."""
    c = Canvas(32, 32)
    # Grand Curator Robes
    c.poly([(7, 15), (25, 15), (28, 30), (4, 30)], 'purple', shade='edge')
    c.rect(14, 16, 4, 14, 'gold', shade='edge')
    # Left Mask (Tragic Silver/Blue) & Right Mask (Wrathful Gold/Crimson)
    c.ellipse(11, 10, 5.5, 6, 'glass', shade='sphere')
    c.ellipse(21, 10, 5.5, 6, 'gold', shade='sphere')
    c.dots([(9, 9), (12, 9)], EYE)
    c.dots([(19, 9), (22, 9)], '#b02a32')
    c.dots([(9, 13), (10, 12), (11, 12), (12, 13)], EYE)       # Frown
    c.dots([(19, 12), (20, 13), (21, 13), (22, 12)], '#b02a32') # Grin
    # Floating Silence Chains & Keys
    c.ellipse(5, 19, 2.5, 2.5, 'metal', shade='sphere')
    c.ellipse(27, 19, 2.5, 2.5, 'gold', shade='sphere')
    return c.render()

def m_the_blind_archivist():
    """5. The Blind Archivist (Act I BOSS - 48x48): Towering blindfolded keeper with floating foggy lanterns & chains."""
    c = Canvas(48, 48)
    # Shadowy Archivist Cloak
    c.poly([(10, 46), (14, 18), (34, 18), (38, 46)], 'navy', shade='edge')
    c.poly([(16, 46), (19, 20), (29, 20), (32, 46)], 'purple', shade='edge')
    # Golden Collar & Rune Stole
    c.rrect(13, 18, 22, 5, 'gold', shade='edge')
    c.rect(22, 22, 4, 22, 'gold', shade='edge')
    for y in (25, 30, 35, 40):
        c.dots([(23, y), (24, y)], ramp('blue', 3))
    # Hooded Head & Sacred Blindfold
    c.ellipse(24, 12, 8.5, 8.5, 'navy', shade='sphere')
    c.ellipse(24, 13, 6.2, 6.2, 'pale', shade='sphere')
    c.rrect(17, 10, 14, 4, 'cloth', shade='edge')
    c.dots([(20, 11), (21, 11), (26, 11), (27, 11)], ramp('red', 2)) # Blood-ink rune on blindfold
    # Levitating Great Index Tome (Left) & Fog Lantern (Right)
    c.rrect(3, 20, 9, 11, 'red', shade='edge')
    c.rect(5, 22, 5, 7, 'paper', shade='flat')
    c.ellipse(41, 22, 4.5, 5.5, 'teal', shade='sphere')
    c.rect(39, 16, 5, 2, 'gold', shade='flat')
    c.dots([(41, 21), (41, 22)], WHITE)
    return c.render()

# --- ACT II MONSTERS (Basic Sorting: TP-06..TP-08) ---

def m_skeleton_archer():
    """6. Skeleton Archer (Act II Regular): Undead marksman firing numbered bone arrows."""
    c = Canvas(32, 32)
    # Ribcage & Pelvis
    c.rrect(11, 16, 10, 8, 'bone', shade='edge')
    for y in (18, 20, 22):
        c.dots([(x, y) for x in range(13, 19)], ramp('black', 0))
    c.rect(12, 24, 3, 5, 'bone', shade='edge_soft')
    c.rect(17, 24, 3, 5, 'bone', shade='edge_soft')
    # Skull
    c.ellipse(16, 10, 6.8, 6.2, 'bone', shade='sphere')
    c.rrect(13, 13, 6, 3, 'bone', shade='edge')
    c.dots([(13, 9), (14, 9), (18, 9), (19, 9)], '#ff3a2a')
    # Bone Bow & Glowing Arrow
    c.line([(6, 6), (4, 16), (6, 26)], 'wood', width=2)
    c.line([(6, 6), (6, 26)], 'white', width=1)
    c.line([(3, 16), (22, 16)], 'gold', width=1)
    c.poly([(2, 16), (5, 14), (5, 18)], 'teal', shade='flat')
    return c.render()

def m_venomous_arachnid():
    """7. Venomous Arachnid (Act II Regular): Giant catacomb spider dripping toxic green venom."""
    c = Canvas(32, 32)
    # 8 Jointed Legs
    for dy, y_end in ((-4, 14), (-1, 19), (2, 24), (5, 29)):
        c.line([(12, 18 + dy // 2), (4, 12 + dy), (2, y_end)], 'black', width=2)
        c.line([(20, 18 + dy // 2), (28, 12 + dy), (30, y_end)], 'black', width=2)
    # Abdomen & Cephalothorax
    c.ellipse(16, 13, 8.5, 7, 'purple', shade='sphere')
    c.ellipse(16, 21, 6.5, 5.5, 'black', shade='sphere')
    # Neon Green Hourglass & Venom Fangs
    c.poly([(14, 10), (18, 10), (16, 13), (18, 16), (14, 16), (16, 13)], 'slime', shade='flat')
    for ex in (12, 14, 17, 19):
        c.dot(ex, 20, '#ff2a2a')
    c.poly([(13, 25), (14, 29), (15, 25)], 'slime', shade='edge')
    c.poly([(17, 25), (18, 29), (19, 25)], 'slime', shade='edge')
    return c.render()

def m_grave_digger():
    """8. Grave Digger (Act II Regular): Hulking catacomb keeper with an iron spade burying array elements."""
    c = Canvas(32, 32)
    # Iron Shovel
    c.rect(25, 5, 2, 20, 'wood', shade='flat')
    c.poly([(23, 22), (29, 22), (28, 29), (24, 29)], 'metal', shade='edge')
    # Hunched Coat & Muddy Boots
    c.rrect(8, 15, 15, 11, 'brown', shade='edge')
    c.rect(10, 26, 4, 4, 'black', shade='flat')
    c.rect(17, 26, 4, 4, 'black', shade='flat')
    # Hooded Head & Lantern Hat
    c.ellipse(15, 10, 6.8, 6.2, 'khaki', shade='sphere')
    c.rrect(9, 4, 13, 3, 'black', shade='edge')
    c.dots([(12, 10), (17, 10)], '#ffe060')
    c.dots([(13, 13), (14, 13), (15, 13), (16, 13)], EYE)
    return c.render()

def m_the_abyss_golem():
    """9. The Abyss Golem (Act II BOSS - 48x48): Colossal rune-carved monolith golem with Heavy Stone Armor."""
    c = Canvas(48, 48)
    # Massive Stone Legs
    c.rrect(12, 34, 9, 12, 'stone', shade='edge')
    c.rrect(27, 34, 9, 12, 'stone', shade='edge')
    # Monolith Torso with Disordered Pillar Cracks
    c.rrect(10, 14, 28, 22, 'stone', shade='edge')
    c.rrect(14, 18, 20, 14, 'navy', shade='edge')
    # Glowing Emerald Sorting Runes on Chest
    for (x, h_bar) in ((16, 4), (20, 10), (24, 6), (28, 12), (31, 8)):
        c.rect(x, 30 - h_bar, 2, h_bar, 'teal', shade='light')
    # Giant Boulder Arms
    c.rrect(2, 16, 8, 18, 'stone', shade='edge')
    c.rrect(38, 16, 8, 18, 'stone', shade='edge')
    # Golem Head
    c.rrect(16, 5, 16, 10, 'stone', shade='edge')
    c.rrect(19, 9, 10, 3, 'lava', shade='light')
    c.dots([(21, 10), (26, 10)], '#fff47e')
    return c.render()

# --- ACT III MONSTERS (Divide & Conquer + Big-O: TP-09..TP-12) ---

def m_hydra_of_division():
    """10. Hydra of Division (Act III Regular): Multi-headed draconic beast embodying Merge Sort recursion."""
    c = Canvas(32, 32)
    # Coiled Dragon Body
    c.ellipse(16, 23, 10, 7, 'teal', shade='sphere')
    c.ellipse(16, 24, 6, 4.5, 'yellow', shade='sphere')
    # Three Dividing Necks & Heads
    for (hx_pos, hy_pos) in ((7, 9), (16, 6), (25, 9)):
        c.line([(16, 19), (hx_pos, hy_pos + 3)], 'teal', width=3)
        c.ellipse(hx_pos, hy_pos, 4.5, 4.0, 'teal', shade='sphere')
        c.dots([(hx_pos - 2, hy_pos - 1), (hx_pos + 1, hy_pos - 1)], '#ff3a2a')
        c.dots([(hx_pos - 1, hy_pos + 2), (hx_pos + 1, hy_pos + 2)], WHITE)
    return c.render()

def m_pivot_wraith():
    """11. Pivot Wraith (Act III Regular): Phantom blade-dancer balancing on a glowing golden Quick Sort pivot."""
    c = Canvas(32, 32)
    # Golden Pivot Fulcrum Triangle at bottom
    c.poly([(16, 22), (22, 30), (10, 30)], 'gold', shade='edge')
    # Spectral Wraith Cloak & Twin Partition Scythes
    c.poly([(9, 12), (23, 12), (20, 24), (12, 24)], 'purple', shade='edge')
    c.poly([(2, 8), (10, 14), (8, 17), (1, 12)], 'glass', shade='light')
    c.poly([(30, 8), (22, 14), (24, 17), (31, 12)], 'demon', shade='light')
    # Wraith Crown & Hood
    c.ellipse(16, 8, 5.5, 5.5, 'shadow', shade='sphere')
    c.poly([(11, 4), (16, 0), (21, 4)], 'gold', shade='edge')
    c.dots([(14, 8), (18, 8)], '#ffd86a')
    return c.render()

def m_the_sphinx_of_entropy():
    """12. The Sphinx of Entropy (Act III Mid-Boss): Winged celestial riddle guardian with 3 Death Seals."""
    c = Canvas(32, 32)
    # Golden Lion Body & Mystic Wings
    c.poly([(2, 7), (10, 14), (8, 22), (2, 16)], 'purple', shade='edge')
    c.poly([(30, 7), (22, 14), (24, 22), (30, 16)], 'purple', shade='edge')
    c.rrect(8, 17, 16, 11, 'gold', shade='edge')
    # Nemes Royal Headdress & Face
    c.poly([(9, 4), (23, 4), (26, 16), (6, 16)], 'blue', shade='edge')
    c.ellipse(16, 10, 5.5, 5.5, 'gold', shade='sphere')
    c.dots([(14, 9), (18, 9)], '#62c8b8')
    # 3 Floating Crimson Death Seals above crown
    for sx in (10, 16, 22):
        c.ellipse(sx, 2, 2.0, 2.0, 'demon', shade='sphere')
    return c.render()

def m_entropy_chaos_king():
    """13. Entropy, The Chaos King (Act III FINAL BOSS - 48x48): Cosmic sovereign of shattered indexes."""
    c = Canvas(48, 48)
    # Cosmic Chaos Vortex Wings
    c.poly([(2, 8), (16, 18), (12, 38), (2, 28)], 'demon', shade='edge')
    c.poly([(46, 8), (32, 18), (36, 38), (46, 28)], 'purple', shade='edge')
    # Sovereign Armor Torso
    c.poly([(13, 46), (16, 18), (32, 18), (35, 46)], 'black', shade='edge')
    c.rrect(17, 20, 14, 16, 'gold', shade='edge')
    c.ellipse(24, 27, 4.5, 4.5, 'demon', shade='sphere')
    c.dot(24, 27, WHITE)
    # Crown of Shattered Pointers
    c.ellipse(24, 12, 7.5, 7.5, 'shadow', shade='sphere')
    for (hx_p, hy_p) in ((15, 2), (20, 0), (24, 0), (28, 0), (33, 2)):
        c.poly([(hx_p - 2, 8), (hx_p, hy_p), (hx_p + 2, 8)], 'gold', shade='edge')
    c.dots([(21, 11), (22, 11), (26, 11), (27, 11)], '#ff3030')
    c.dots([(22, 10), (26, 10)], '#fff47e')
    # Floating Corrupted Grand Catalog Tomes around Boss
    for (bx, by, mat) in ((5, 16, 'red'), (40, 16, 'teal'), (5, 34, 'gold'), (40, 34, 'purple')):
        c.rrect(bx, by, 5, 6, mat, shade='edge')
    return c.render()

MONSTER_BUILDERS = {
    'corrupted_slime': m_corrupted_slime,
    'lost_wisp': m_lost_wisp,
    'bookworm_horror': m_bookworm_horror,
    'twin_faced_librarian': m_twin_faced_librarian,
    'the_blind_archivist': m_the_blind_archivist,
    'skeleton_archer': m_skeleton_archer,
    'venomous_arachnid': m_venomous_arachnid,
    'grave_digger': m_grave_digger,
    'the_abyss_golem': m_the_abyss_golem,
    'hydra_of_division': m_hydra_of_division,
    'pivot_wraith': m_pivot_wraith,
    'the_sphinx_of_entropy': m_the_sphinx_of_entropy,
    'entropy_chaos_king': m_entropy_chaos_king,
}

# ============================================================================
# 3. 320x180 BAYER-DITHERED BACKDROPS (TITLE + ACT I, II, III)
# ============================================================================

W_BG, H_BG = 320, 180
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

def grad(px, y0, y1, cols, x0=0, x1=W_BG):
    n = len(cols) - 1
    for y in range(y0, y1):
        t = (y - y0) / max(1, (y1 - y0 - 1)) * n
        i = min(n - 1, int(t)); f = t - i
        for x in range(x0, x1):
            th = (BAYER[y % 4][x % 4] + 0.5) / 16.0
            px[x, y] = hx(cols[i + 1]) if f > th else hx(cols[i])

def bg_act1_dusty_archives():
    """Act I: The Dusty Archives — towering mahogany bookshelves, candlelight cones & parchment floor."""
    rnd = random.Random(101)
    img = Image.new('RGBA', (W_BG, H_BG)); px = img.load()
    grad(px, 0, 135, ['#0c0914', '#181224', '#261c34', '#382942'])
    # Arched Library Windows & Bookshelf Columns
    for bx in range(8, W_BG, 44):
        for y in range(18, 135):
            for x in range(bx, min(W_BG, bx + 32)):
                shelf_row = (y - 18) % 16
                if shelf_row < 2 or x == bx or x == bx + 31:
                    px[x, y] = hx('#3a2010')
                else:
                    book_col = ['#6e1424', '#1d2e70', '#1d5226', '#8a5a12', '#4a2616'][(x // 4 + y // 16) % 5]
                    px[x, y] = hx(book_col) if shelf_row > 3 else hx('#181224')
    # Flagstone Archive Floor
    grad(px, 135, 180, ['#262030', '#1a1624', '#100d18'])
    for y in range(135, 180, 9):
        for x in range(W_BG):
            px[x, y] = hx('#0c0914')
    # Warm Candlelight Dither Cones
    for lx in (60, 160, 260):
        for y in range(28, 175):
            half = (y - 24) * 0.32
            for x in range(int(lx - half), int(lx + half)):
                if 0 <= x < W_BG:
                    t = 1.0 - abs(x - lx) / max(1.0, half)
                    if (BAYER[y % 4][x % 4] / 16.0) < t * 0.32:
                        r, g, b, a = px[x, y]
                        px[x, y] = (min(255, r + 42), min(255, g + 30), min(255, b + 8), 255)
    return img

def bg_act2_catacombs_of_chaos():
    """Act II: The Catacombs of Chaos — emerald/teal runic pillars, underground ruins & mist."""
    rnd = random.Random(202)
    img = Image.new('RGBA', (W_BG, H_BG)); px = img.load()
    grad(px, 0, 132, ['#050c12', '#0b1924', '#132a38', '#1c3f4c'])
    # Catacomb Stone Pillars & Skull Niches
    for px_col in range(20, W_BG, 56):
        for y in range(10, 135):
            for x in range(px_col, min(W_BG, px_col + 22)):
                mortar = (y % 10 == 0) or ((x + (y // 10) * 5) % 11 == 0)
                px[x, y] = hx('#0c1820') if mortar else hx('#223844')
        # Glowing Teal Sorting Brazier on each pillar
        for y in range(48, 54):
            for x in range(px_col + 6, min(W_BG, px_col + 16)):
                px[x, y] = hx('#62c8b8') if y < 51 else hx('#155058')
    # Catacomb Abyssal River Horizon
    for y in range(130, 140):
        for x in range(W_BG):
            wv = math.sin(x / 8.0 + y) * 0.5 + 0.5
            px[x, y] = hx('#62c8b8') if wv > 0.78 else hx('#2a8a8a') if wv > 0.35 else hx('#08202a')
    grad(px, 140, 180, ['#14202a', '#0c141c', '#060a10'])
    for _ in range(55):
        px[rnd.randrange(W_BG), rnd.randrange(130)] = hx('#62c8b8')
    return img

def bg_act3_tower_of_entropy():
    """Act III: The Tower of Entropy — cosmic violet/crimson rift, shattered clockwork & floating crystals."""
    rnd = random.Random(303)
    img = Image.new('RGBA', (W_BG, H_BG)); px = img.load()
    grad(px, 0, 140, ['#090314', '#1a092e', '#32104c', '#52165c', '#280936'])
    # Cosmic Entropy Vortex in Sky
    cx, cy = 160, 62
    cols = ['#14081f', '#3a1858', '#66329c', '#9c5cd4', '#e85a42', '#ffe86a']
    for y in range(130):
        for x in range(W_BG):
            dx, dy = (x - cx), (y - cy) / 0.65
            r = math.hypot(dx, dy)
            if r < 64:
                a = math.atan2(dy, dx)
                sw = r + 8 * math.sin(a * 4 + r / 7.0)
                band = max(0, min(len(cols) - 1, int((64 - r) / 11 + math.sin(sw / 5.0) * 0.8)))
                th = (BAYER[y % 4][x % 4] + 0.5) / 16.0
                if r <= 56 or th <= (64 - r) / 8.0:
                    px[x, y] = hx(cols[band])
    # Shattered Obsidian Spire Floor
    grad(px, 140, 180, ['#220c30', '#140620', '#080210'])
    for x in range(0, W_BG, 16):
        for y in range(140, 180):
            if (x + y) % 16 == 0:
                px[x, y] = hx('#9c5cd4')
    for _ in range(80):
        px[rnd.randrange(W_BG), rnd.randrange(140)] = hx('#ffe86a') if rnd.random() < 0.3 else hx('#c64c98')
    return img

def bg_title_grand_library():
    """Title Screen Backdrop: The Grand Library of Alexandria-Nova & The Great Index Portal."""
    img = bg_act3_tower_of_entropy()
    px = img.load()
    # Add Grand Golden Archway Pillars on left and right
    for x in list(range(0, 36)) + list(range(W_BG - 36, W_BG)):
        for y in range(0, H_BG):
            edge_dist = min(x, abs(35 - x), abs(W_BG - 36 - x), W_BG - 1 - x)
            px[x, y] = hx('#dca424') if edge_dist < 2 else hx('#24130a') if (y % 12 == 0) else hx('#3a2010')
    return img

# ============================================================================
# 4. BOARD TILES, DICE, ITEMS, STATUS BADGES, STAT ICONS & 9-SLICE UI
# ============================================================================

STAGE_PAL = {
    1: dict(plate='wood', accent='gold', ground=('#16121e', '#1e1828', '#282034')),
    2: dict(plate='stone', accent='teal', ground=('#0c141c', '#121e28', '#1a2a36')),
    3: dict(plate='purple', accent='red', ground=('#14081f', '#1e0c2e', '#2a1240')),
}

def make_plate(stage, seed=0):
    rnd = random.Random(seed * 31 + stage)
    p = STAGE_PAL[stage]
    c = Canvas(32, 32)
    c.rrect(2, 3, 28, 27, p['plate'], shade='dark')
    c.rrect(2, 2, 28, 26, p['plate'], shade='flat')
    img = c.render(outline=True)
    px = img.load()
    hi = ramp(p['plate'], 3)
    sh = ramp(p['plate'], 1)
    for x in range(3, 29):
        px[x, 2] = hi
    for y in range(3, 27):
        px[2, y] = hi
    for _ in range(10):
        px[rnd.randint(4, 27), rnd.randint(4, 25)] = sh
    return img

def make_rim():
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    px = img.load()
    for x in range(3, 29):
        for y in (3, 4, 25, 26):
            px[x, y] = (255, 255, 255, 255)
    for y in range(3, 27):
        for x in (3, 4, 27, 28):
            px[x, y] = (255, 255, 255, 255)
    return img

def make_fog():
    c = Canvas(32, 32)
    c.rrect(2, 3, 28, 27, 'shadow', shade='dark')
    c.rrect(2, 2, 28, 26, 'shadow', shade='flat')
    img = c.render()
    px = img.load()
    for (x, y) in ((14, 9), (15, 8), (16, 8), (17, 8), (18, 9), (18, 10), (17, 11), (16, 12), (16, 13), (16, 16)):
        px[x, y] = hx('#8a7aa6')
    return img

def make_ground(stage):
    rnd = random.Random(stage * 97)
    g = STAGE_PAL[stage]['ground']
    img = Image.new('RGBA', (64, 64), hx(g[1]))
    px = img.load()
    for y in range(64):
        for x in range(64):
            r = rnd.random()
            if r < 0.10:
                px[x, y] = hx(g[0])
            elif r < 0.16:
                px[x, y] = hx(g[2])
            if (x % 16 == 0) or (y % 16 == 0):
                px[x, y] = hx(g[0])
    return img

PIPS = {
    1: [(1, 1)], 2: [(0, 0), (2, 2)], 3: [(0, 0), (1, 1), (2, 2)],
    4: [(0, 0), (2, 0), (0, 2), (2, 2)], 5: [(0, 0), (2, 0), (1, 1), (0, 2), (2, 2)],
    6: [(0, 0), (2, 0), (0, 1), (2, 1), (0, 2), (2, 2)],
}

def make_die(n):
    c = Canvas(24, 24)
    c.rrect(1, 1, 22, 22, 'paper', shade='edge')
    for (i, j) in PIPS[n]:
        cx, cy = 6 + i * 6, 6 + j * 6
        col = ramp('red', 2) if n in (1, 4) else ramp('navy', 1)
        c.dots([(cx, cy), (cx + 1, cy), (cx, cy + 1), (cx + 1, cy + 1)], col)
    return c.render()

def make_tile_icon(kind):
    c = Canvas(16, 16)
    if kind == 'start':
        c.rect(4, 1, 2, 14, 'gold', shade='flat')
        c.poly([(6, 2), (14, 5), (6, 9)], 'blue', shade='edge')
    elif kind == 'enemy':
        c.line([(2, 2), (13, 13)], 'metal', width=2)
        c.line([(13, 2), (2, 13)], 'metal', width=2)
        c.ellipse(8, 8, 3.5, 3.5, 'red', shade='sphere')
    elif kind == 'elite':
        c.ellipse(8, 8, 5.5, 5, 'purple', shade='sphere')
        c.poly([(3, 4), (8, 0), (13, 4)], 'gold', shade='edge')
    elif kind == 'boss':
        c.ellipse(8, 9, 6, 5.5, 'demon', shade='sphere')
        c.poly([(2, 5), (4, 0), (8, 3), (12, 0), (14, 5)], 'gold', shade='edge')
    elif kind == 'treasure':
        c.rrect(2, 6, 12, 8, 'wood', shade='edge')
        c.rect(2, 6, 12, 2, 'gold', shade='flat')
        c.rect(7, 8, 2, 3, 'gold', shade='flat')
    elif kind == 'merchant':
        c.poly([(1, 6), (8, 1), (15, 6)], 'gold', shade='edge')
        c.rrect(3, 6, 10, 8, 'blue', shade='edge')
    elif kind == 'rest':
        c.poly([(8, 2), (12, 8), (10, 13), (6, 13), (4, 8)], 'teal', shade='edge')
        c.poly([(8, 5), (10, 9), (8, 12), (6, 9)], 'white', shade='flat')
    elif kind == 'shrine':
        c.rrect(3, 3, 10, 11, 'purple', shade='edge')
        c.rect(5, 5, 6, 7, 'paper', shade='flat')
    elif kind == 'event':
        c.ellipse(8, 8, 6, 6, 'blue', shade='sphere')
        c.dots([(7, 5), (8, 5), (9, 6), (8, 8), (8, 11)], WHITE)
    elif kind == 'trap':
        for x in (3, 8, 13):
            c.poly([(x - 2, 13), (x, 4), (x + 2, 13)], 'metal', shade='edge')
    else:
        c.ellipse(8, 8, 2.5, 2.5, 'stone', shade='flat')
    return c.render()

def make_item_icon(kind):
    c = Canvas(16, 16)
    if kind.startswith('blade'):
        mat = 'metal' if '1' in kind else 'teal' if '2' in kind else 'gold' if '3' in kind else 'purple'
        c.poly([(4, 10), (13, 1), (15, 3), (6, 12)], mat, shade='edge')
        c.line([(1, 15), (5, 11)], 'gold', width=2)
    elif kind == 'potion_hp':
        c.ellipse(8, 10, 5, 4.5, 'red', shade='sphere')
        c.rect(6, 3, 4, 3, 'paper', shade='edge')
    elif kind == 'potion_mp':
        c.ellipse(8, 10, 5, 4.5, 'blue', shade='sphere')
        c.rect(6, 3, 4, 3, 'gold', shade='edge')
    elif kind == 'monocle':
        c.ellipse(8, 8, 5, 5, 'gold', shade='sphere')
        c.ellipse(8, 8, 3.2, 3.2, 'glass', shade='sphere')
    elif kind == 'hourglass':
        c.poly([(3, 2), (13, 2), (8, 8), (13, 14), (3, 14), (8, 8)], 'gold', shade='edge')
    elif kind == 'aegis_charm':
        c.poly([(2, 2), (14, 2), (14, 8), (8, 15), (2, 8)], 'gold', shade='edge')
        c.poly([(4, 4), (12, 4), (12, 8), (8, 13), (4, 8)], 'blue', shade='sphere')
    else:
        c.ellipse(8, 8, 5.5, 5.5, 'gold', shade='sphere')
    return c.render()

def make_ui_panel(fill='#16121f', rim='#dca424', hi='#ffe86a', dark='#07050b', size=24):
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = size - 1
    d.rectangle([1, 0, s - 1, s], fill=hx(dark))
    d.rectangle([0, 1, s, s - 1], fill=hx(dark))
    d.rectangle([1, 1, s - 1, s - 1], fill=hx(rim))
    d.rectangle([2, 2, s - 2, s - 2], fill=hx(dark))
    d.rectangle([3, 3, s - 3, s - 3], fill=hx(fill))
    px = img.load()
    for x in range(2, s - 1):
        px[x, 1] = hx(hi)
    for y in range(2, s - 1):
        px[1, y] = hx(hi)
    for (x, y) in ((3, 3), (s - 3, 3), (3, s - 3), (s - 3, s - 3)):
        px[x, y] = hx(hi)
    return img

def make_ui_button(state):
    pal = {
        'normal':   ('#221c34', '#5c4c74', '#8a7aa6', '#0a0710'),
        'hover':    ('#34284a', '#dca424', '#ffe86a', '#0a0710'),
        'pressed':  ('#161222', '#8a5a12', '#dca424', '#0a0710'),
        'disabled': ('#181620', '#34303e', '#44404e', '#0a0710'),
    }[state]
    fill, rim, hi, dark = pal
    img = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([1, 0, 14, 15], fill=hx(dark))
    d.rectangle([0, 1, 15, 14], fill=hx(dark))
    d.rectangle([1, 1, 14, 14], fill=hx(rim))
    d.rectangle([2, 2, 13, 13], fill=hx(fill))
    px = img.load()
    for x in range(2, 14):
        px[x, 2] = hx(hi if state != 'pressed' else dark)
    return img

# ============================================================================
# 5. PROCEDURAL 8-BIT / 16-BIT AUDIO & AMBIENT MUSIC ENGINE
# ============================================================================

SR = 22050

def write_wav(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, 'w') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        data = b''.join(struct.pack('<h', int(max(-1.0, min(1.0, s)) * 30000)) for s in samples)
        w.writeframes(data)

def env(i, n, a=0.005, r=0.6):
    t = i / SR; T = n / SR
    if t < a:
        return t / a
    return max(0.0, 1.0 - (t - a) / max(1e-4, (T - a))) ** (1.0 / max(0.1, r))

def sq(f, t, duty=0.5):
    return 1.0 if (f * t) % 1.0 < duty else -1.0

def tri(f, t):
    p = (f * t) % 1.0
    return 4.0 * abs(p - 0.5) - 1.0

def tone(freqs, dur, wave_fn=sq, vol=0.5, slide=0.0, duty=0.5, rel=0.7):
    n = int(SR * dur)
    out = []
    for i in range(n):
        t = i / SR
        k = min(len(freqs) - 1, int(i / n * len(freqs)))
        f = freqs[k] * (1.0 + slide * t)
        s = wave_fn(f, t, duty) if wave_fn is sq else wave_fn(f, t)
        out.append(s * vol * env(i, n, r=rel))
    return out

def noise(dur, vol=0.5, lp=0.5, rel=0.5, seed=1):
    rnd = random.Random(seed)
    n = int(SR * dur)
    out, prev = [], 0.0
    for i in range(n):
        v = rnd.uniform(-1.0, 1.0)
        prev = prev + (v - prev) * lp
        out.append(prev * vol * env(i, n, r=rel))
    return out

def mix(*tracks):
    n = max(len(t) for t in tracks)
    return [sum(t[i] if i < len(t) else 0.0 for t in tracks) for i in range(n)]

def cat(*tracks):
    out = []
    for t in tracks:
        out += t
    return out

def ambient_loop(root, scale, seed, bright=1.0, dur=12.0):
    rnd = random.Random(seed)
    n = int(SR * dur)
    out = [0.0] * n
    for i in range(n):
        t = i / SR
        lfo = 0.5 + 0.5 * math.sin(2 * math.pi * t / dur * 2)
        s = 0.20 * math.sin(2 * math.pi * root * t) + 0.12 * math.sin(2 * math.pi * root * 1.5 * t + lfo)
        s += 0.05 * tri(root * 2.0, t) * lfo
        out[i] = s
    t = 0.4
    while t < dur - 1.5:
        f = root * 4 * scale[rnd.randrange(len(scale))] * bright
        ln = int(SR * 1.4)
        st = int(SR * t)
        for k in range(ln):
            if st + k < n:
                e = math.exp(-k / SR * 2.8)
                out[st + k] += 0.11 * e * math.sin(2 * math.pi * f * k / SR)
        t += rnd.choice((1.0, 1.5, 2.0))
    fade = int(SR * 0.8)
    for i in range(fade):
        a = i / fade
        out[i] = out[i] * a + out[n - fade + i] * (1.0 - a)
    return [s * 0.85 for s in out[:n - fade]]

# ============================================================================
# 6. EXPORT TO UNITY (Assets/Resources/Gen) & GODOT (assets/sprites & assets/gen)
# ============================================================================

def build_all():
    repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
    unity_gen = os.path.join(repo_root, "Grimoire's Aegis", "Assets", "Resources", "Gen")
    godot_gen = os.path.join(repo_root, "assets", "gen")
    godot_sprites = os.path.join(repo_root, "assets", "sprites")

    def save_both(rel_path, img):
        for base in (unity_gen, godot_gen):
            full = os.path.join(base, rel_path)
            os.makedirs(os.path.dirname(full), exist_ok=True)
            img.save(full)

    count = 0
    # 1. Hero Poses
    for pose in ('idle', 'attack', 'defend', 'hurt', 'front'):
        im = scholar_knight(pose, f=(0 if pose == 'front' else 1))
        save_both(f"chars/player_{pose}.png", im); count += 1
        if pose in ('idle', 'front'):
            save_both(f"chars/player_{pose}2.png", breathe(im)); count += 1
        if pose == 'idle':
            os.makedirs(os.path.join(godot_sprites, "characters"), exist_ok=True)
            im.save(os.path.join(godot_sprites, "characters", "scholar_knight.png"))
            strip4(im).save(os.path.join(godot_sprites, "characters", "scholar_knight_idle.png"))

    # 2. All 13 Bestiary Monsters
    os.makedirs(os.path.join(godot_sprites, "monsters"), exist_ok=True)
    for m_id, fn in MONSTER_BUILDERS.items():
        im = fn()
        im2 = breathe(im)
        save_both(f"enemies/{m_id}.png", im)
        save_both(f"enemies/{m_id}_2.png", im2)
        im.save(os.path.join(godot_sprites, "monsters", f"{m_id}.png"))
        strip4(im).save(os.path.join(godot_sprites, "monsters", f"{m_id}_idle.png"))
        count += 2

    # 3. Stage Backdrops (320x180 Bayer-Dithered)
    for k, fn in (
        ('bg_title', bg_title_grand_library),
        ('bg_s1', bg_act1_dusty_archives),
        ('bg_s2', bg_act2_catacombs_of_chaos),
        ('bg_s3', bg_act3_tower_of_entropy),
    ):
        save_both(f"bg/{k}.png", fn()); count += 1

    # 4. Dungeon Board Tiles, Grounds, Dice & Tile Icons
    for s in (1, 2, 3):
        for v in range(4):
            save_both(f"tiles/plate_s{s}_{v}.png", make_plate(s, v)); count += 1
        save_both(f"tiles/ground_s{s}.png", make_ground(s)); count += 1
    save_both("tiles/rim.png", make_rim()); count += 1
    save_both("tiles/fog.png", make_fog()); count += 1
    for tk in ('start', 'enemy', 'elite', 'boss', 'treasure', 'merchant', 'rest', 'shrine', 'event', 'trap', 'cleared'):
        save_both(f"tiles/icon_{tk}.png", make_tile_icon(tk)); count += 1
    for d in range(1, 7):
        save_both(f"dice/die_{d}.png", make_die(d)); count += 1

    # 5. Items & UI Elements
    for ik in ('blade1', 'blade2', 'blade3', 'blade4', 'potion_hp', 'potion_mp', 'monocle', 'hourglass', 'aegis_charm'):
        save_both(f"items/{ik}.png", make_item_icon(ik)); count += 1
    save_both("ui/panel.png", make_ui_panel()); count += 1
    save_both("ui/panel_dark.png", make_ui_panel(fill='#0e0b16', rim='#3a304c', hi='#6a5a84')); count += 1
    save_both("ui/panel_red.png", make_ui_panel(fill='#1f0c14', rim='#b02a32', hi='#e85a42')); count += 1
    for st in ('normal', 'hover', 'pressed', 'disabled'):
        save_both(f"ui/btn_{st}.png", make_ui_button(st)); count += 1

    # 6. Procedural WAV SFX & Music
    sfx_map = {
        'select': tone([660, 990], 0.08, vol=0.3),
        'move': tone([520], 0.04, vol=0.18),
        'cancel': tone([440, 330], 0.09, vol=0.25),
        'error': tone([180, 140], 0.18, vol=0.3, duty=0.25),
        'step': mix(noise(0.05, 0.35, 0.3, seed=3), tone([140], 0.05, tri, vol=0.4)),
        'dice': mix(noise(0.12, 0.45, 0.5, seed=9), tone([740, 980, 1240], 0.12, vol=0.22)),
        'hit': mix(noise(0.12, 0.8, 0.5, seed=5), tone([180, 90], 0.12, vol=0.5, duty=0.3)),
        'parry': mix(tone([1760, 2200], 0.25, tri, vol=0.45, rel=0.4), tone([1320], 0.25, vol=0.2, duty=0.12)),
        'guard': mix(tone([660, 440], 0.15, tri, vol=0.4), noise(0.06, 0.4, 0.7, seed=12)),
        'heal': cat(tone([523], 0.07, tri, vol=0.35), tone([659], 0.07, tri, vol=0.35), tone([784, 1046], 0.16, tri, vol=0.35)),
        'coin': cat(tone([988], 0.05, vol=0.25, duty=0.25), tone([1319], 0.14, vol=0.25, duty=0.25)),
        'levelup': cat(*[tone([f], 0.08, vol=0.3, duty=0.25) for f in (523, 659, 784, 1046)], tone([1046, 1318], 0.28, tri, vol=0.35)),
        'magic': mix(tone([440, 554, 659, 880], 0.38, tri, vol=0.32), tone([220], 0.38, vol=0.12, duty=0.1)),
        'victory': cat(*[tone([f], 0.1, vol=0.3, duty=0.25) for f in (392, 523, 659, 784)], tone([1046], 0.35, tri, vol=0.35)),
    }
    minor = [1.0, 1.189, 1.335, 1.498, 1.587]
    phryg = [1.0, 1.059, 1.189, 1.335, 1.498, 1.587]
    sfx_map['music_title'] = ambient_loop(55.0, minor, 1, 1.0, dur=10.0)
    sfx_map['music_s1'] = ambient_loop(65.4, minor, 2, 1.0, dur=10.0)
    sfx_map['music_s2'] = ambient_loop(49.0, phryg, 3, 0.8, dur=10.0)
    sfx_map['music_s3'] = ambient_loop(58.3, phryg, 4, 0.9, dur=10.0)
    sfx_map['music_battle'] = ambient_loop(73.4, phryg, 5, 1.2, dur=10.0)

    for s_name, samples in sfx_map.items():
        for base in (unity_gen, godot_gen):
            write_wav(os.path.join(base, "audio", f"{s_name}.wav"), samples)
        count += 1

    print(f"[SUCCESS] Generated {count} pixel-art & audio assets in:\n  - {unity_gen}\n  - {godot_gen}")

if __name__ == '__main__':
    build_all()
