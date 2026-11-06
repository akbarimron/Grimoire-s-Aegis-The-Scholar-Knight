"""Grimoire's Aegis — Art Pipeline v2 (animated sprites).

Builds on the region-shaded pixel library in build_grimoire_assets.py
(hue-shifted 4-step ramps, single top-left light, selective outline) and adds:

  * Pose-driven drawing so every frame of an animation is redrawn, not just shifted.
  * Hero "Aegis" at 48x48: idle, walk, attack, defend, parry, cast, hurt, victory, death.
  * 13 GDD monsters (48x48 regular, 64x64 bosses): idle, attack, hurt, death.
  * Horizontal strip sheets (square frames) + manifest.json with per-frame durations.

Run:  python tools/build_grimoire_assets.py   (calls build_v2 at the end)
  or: python tools/grimoire_art_v2.py          (art only)
"""
import json
import math
import os
import random

import numpy as np
from PIL import Image

import build_grimoire_assets as px
from build_grimoire_assets import hx, ramp

# ---------------------------------------------------------------------------
# Extra ramps (hue-shifted: cool shadows, warm highlights)
# ---------------------------------------------------------------------------
px.RAMPS.update({
    'steel':   ['#141626', '#3e4560', '#7c87a2', '#c8d2e2'],
    'mantle':  ['#0a0f26', '#1c2d5c', '#2f4f94', '#5a83c8'],
    'tabard':  ['#0b0c1e', '#1a1f40', '#2a3262', '#454f8a'],
    'plume':   ['#22060e', '#5e1020', '#9a1e2a', '#d4483a'],
    'leather': ['#1a0d08', '#3e2414', '#64391e', '#8c5a30'],
    'tome':    ['#16081e', '#3c1450', '#62247a', '#9048a8'],
    'aegis':   ['#081830', '#14407a', '#2a74c0', '#7ec8ff'],
    'ink':     ['#06040c', '#140e22', '#241a3a', '#3a2c58'],
    'ectoplasm': ['#06201e', '#0f4e4a', '#1f8c7e', '#7ff0d0'],
    'chitin':  ['#120814', '#2e1230', '#52204e', '#82386e'],
    'moss':    ['#0e1a0c', '#24381c', '#42602c', '#6e8a42'],
    'robe2':   ['#100818', '#2a1440', '#46246a', '#6c3e98'],
    'obsid':   ['#05040a', '#100c1c', '#1e1832', '#34284e'],
    'crystal': ['#1c0a34', '#4a1e86', '#8a4ad8', '#d6a8ff'],
    'sand':    ['#2a1a0a', '#7a5626', '#c09048', '#f0d088'],
    'web':     ['#3a3a4a', '#8a8aa0', '#c8c8da', '#f4f4ff'],
})

# ---------------------------------------------------------------------------
# Faster canvas: numpy mask application (pixel output identical to base Canvas)
# ---------------------------------------------------------------------------
class Cv(px.Canvas):
    def __init__(self, w, h):
        super().__init__(w, h)
        self.rid = np.full((h, w), -1, dtype=np.int32)

    def _apply_mask(self, mask, mat, shade, edge, clip=None):
        r = self._new(mat, shade, edge)
        m = np.asarray(mask, dtype=np.uint8) > 0
        if clip is not None:
            m &= np.isin(self.rid, list(clip))
        self.rid[m] = r
        return r

    def ellipse(self, cx, cy, rx, ry, mat, shade='sphere', edge=False, clip=None):
        yy, xx = np.mgrid[0:self.h, 0:self.w]
        dx = (xx + 0.5 - cx) / max(0.1, rx)
        dy = (yy + 0.5 - cy) / max(0.1, ry)
        m = Image.fromarray(((dx * dx + dy * dy) <= 1.0).astype(np.uint8) * 255)
        return self._apply_mask(m, mat, shade, edge, clip)

    def erase_rect(self, x, y, w, h):
        x0, y0 = max(0, x), max(0, y)
        self.rid[y0:max(0, y + h), x0:max(0, x + w)] = -1

    def erase_px(self, pts):
        for (x, y) in pts:
            if 0 <= x < self.w and 0 <= y < self.h:
                self.rid[y, x] = -1
                self.detail.pop((x, y), None)

    def _inside(self, x, y, r):
        return 0 <= x < self.w and 0 <= y < self.h and self.rid[y, x] == r

    def render(self, outline=True, light=(-0.62, -0.78)):
        # base render indexes rid[y][x]; numpy supports that too.
        return super().render(outline=outline, light=light)

    # --- helpers ---------------------------------------------------------
    def blade(self, hand, ang, length, width, mat, guard_mat='gold', grip_mat='leather', guard=5, grip=3, blunt=True):
        """Sword from a hand point. ang: degrees clockwise from straight up."""
        a = math.radians(ang)
        ux, uy = math.sin(a), -math.cos(a)          # along blade
        nx, ny = -uy, ux                            # perpendicular
        hx0, hy0 = hand
        # grip + pommel behind the hand
        gx, gy = hx0 - ux * grip, hy0 - uy * grip
        self.line([(gx, gy), (hx0, hy0)], grip_mat, width=2)
        self.ellipse(gx - ux * 0.8, gy - uy * 0.8, 1.3, 1.3, guard_mat, shade='flat')
        # blade
        bx, by = hx0 + ux * 1.5, hy0 + uy * 1.5
        tx, ty = bx + ux * length, by + uy * length
        hw = width / 2.0
        if blunt:
            pts = [(bx + nx * hw, by + ny * hw), (tx + nx * hw * 0.8, ty + ny * hw * 0.8),
                   (tx + ux * 0.8, ty + uy * 0.8), (tx - nx * hw * 0.8, ty - ny * hw * 0.8), (bx - nx * hw, by - ny * hw)]
        else:
            pts = [(bx + nx * hw, by + ny * hw), (tx, ty), (bx - nx * hw, by - ny * hw)]
        self.poly(pts, mat, shade='edge')
        # crossguard
        g = guard / 2.0
        self.line([(hx0 + nx * g, hy0 + ny * g), (hx0 - nx * g, hy0 - ny * g)], guard_mat, width=2, shade='flat')
        return (tx, ty)


def rot(pts, cx, cy, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for (x, y) in pts]


# ---------------------------------------------------------------------------
# Image post-processing used by many animations
# ---------------------------------------------------------------------------
def offset(img, dx, dy):
    out = Image.new('RGBA', img.size, (0, 0, 0, 0))
    out.paste(img, (int(dx), int(dy)), img)
    return out


def flash(img, col=(255, 255, 255), amt=1.0):
    a = np.asarray(img).astype(np.float32)
    mask = a[..., 3] > 0
    for i in range(3):
        a[..., i][mask] = a[..., i][mask] * (1 - amt) + col[i] * amt
    return Image.fromarray(a.astype(np.uint8)).copy()


def tint(img, col, amt):
    return flash(img, col, amt)


def glitch_dissolve(img, t, seed=7, palette=('#7ff0d0', '#d6a8ff', '#ff4a6a', '#ffffff')):
    """The Glitch curse taking the sprite apart: row tearing, pixel decay, rising fragments.
    t in [0,1]."""
    rng = random.Random(seed)
    w, h = img.size
    src = np.asarray(img).copy()
    out = np.zeros_like(src)
    # row tearing
    for y in range(h):
        sh = 0
        if rng.random() < 0.18 + 0.5 * t:
            sh = int(rng.choice((-1, 1)) * rng.randint(1, max(1, int(2 + 7 * t))))
        row = np.roll(src[y], sh, axis=0)
        if sh > 0:
            row[:sh] = 0
        elif sh < 0:
            row[sh:] = 0
        out[y] = row
    # decay: pixels vanish bottom-up-ish with noise; some become bright glitch pixels
    pal = [hx(c) for c in palette]
    rng2 = np.random.default_rng(seed)
    noise = rng2.random((h, w))
    grad = (np.arange(h)[:, None] / h) * 0.35
    kill = noise + grad * (1 - t) < t * 1.15
    bright = (noise < t * 1.15) & (noise > t * 1.15 - 0.12) & (out[..., 3] > 0)
    out[kill] = 0
    for (y, x) in zip(*np.nonzero(bright)):
        out[y, x] = pal[(x + y) % len(pal)]
    res = Image.fromarray(out).copy()
    # floating fragments (little squares drifting up)
    p = res.load()
    for k in range(int(10 * t + 4)):
        x = rng.randint(w // 5, w * 4 // 5)
        y = int(h * 0.8 - rng.random() * h * 0.7 * (0.3 + t))
        c = pal[k % len(pal)]
        for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1))[: 1 + (k % 3)]:
            if 0 <= x + dx < w and 0 <= y + dy < h:
                p[x + dx, y + dy] = c
    if t > 0.85:
        a = np.asarray(res).copy()
        a[..., 3] = (a[..., 3] * max(0.0, (1 - t) / 0.15)).astype(np.uint8)
        res = Image.fromarray(a).copy()
    return res


def sparkle(img, pts, cols=('#ffffff', '#ffe86a')):
    p = img.load()
    w, h = img.size
    for i, (x, y) in enumerate(pts):
        c = hx(cols[i % len(cols)])
        for (dx, dy) in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            if 0 <= x + dx < w and 0 <= y + dy < h:
                p[x + dx, y + dy] = c if (dx, dy) == (0, 0) else hx(cols[(i + 1) % len(cols)])
    return img


def shadow_ellipse(img, cx, cy, rx, ry=2, alpha=90):
    """Soft ground shadow drawn *under* the sprite."""
    w, h = img.size
    base = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    a = np.zeros((h, w, 4), dtype=np.uint8)
    yy, xx = np.mgrid[0:h, 0:w]
    d = ((xx + 0.5 - cx) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2
    a[d <= 1.0] = (8, 6, 16, alpha)
    base = Image.fromarray(a)
    base.alpha_composite(img)
    return base


# ===========================================================================
# HERO — "AEGIS", novice Scholar Knight of the Order of Syntax (48x48)
# Facing right. Light from top-left. Blunt steel sword, kite Aegis Shield,
# chained Grimoire at the hip, tattered scholar's mantle.
# ===========================================================================
HERO_W = 48


def hero_frame(P):
    """P keys: b (body bob), l (lean x), cr (crouch 0..3), legs ('stand'|'walkA'|'walkB'|'kneel'),
    hand (x,y), ang (sword angle), sword (bool), shield ('side'|'raised'|'low'|'none'),
    book ('hip'|'open'), cape (phase int), eyes ('calm'|'fierce'|'hurt'|'closed'),
    head_dy, glyphs (list of points), smear (list of points)"""
    c = Cv(HERO_W, HERO_W)
    b = P.get('b', 0); l = P.get('l', 0); cr = P.get('cr', 0)
    ub = b + cr  # upper-body vertical offset
    ph = P.get('cape', 0)
    legs = P.get('legs', 'stand')

    # ---- tattered cape (behind everything) ----
    sway = [0, 1, 1, 0][ph % 4]
    cape = [(18 + l, 22 + ub), (25 + l, 22 + ub), (24 + l, 34 + ub),
            (21 + l - sway, 41 + cr // 2), (18 + l - sway, 39 + cr // 2), (15 + l - sway, 42 + cr // 2),
            (12 + l - sway * 2, 38 + cr // 2), (13 + l - sway, 30 + ub)]
    c.poly(cape, 'mantle', shade='edge')
    c.poly([(15 + l, 26 + ub), (18 + l, 24 + ub), (17 + l, 36 + ub), (14 + l - sway, 38)], 'mantle', shade='dark')

    # ---- legs ----
    if legs == 'kneel':
        c.rrect(17 + l, 38, 7, 4, 'steel', shade='edge')            # back thigh, knee on ground
        c.rect(15 + l, 42, 6, 3, 'leather', shade='edge_soft')
        c.rect(25 + l, 35, 4, 7, 'steel', shade='edge')             # front shin
        c.rrect(25 + l, 42, 6, 3, 'leather', shade='edge_soft')
    else:
        spread = {'stand': (0, 0), 'walkA': (-4, 3), 'walkB': (3, -4), 'walkC': (-2, 1), 'walkD': (1, -2)}.get(legs, (0, 0))
        s = cr // 2
        bx = 19 + l - s + spread[0]
        fx = 25 + l + s + spread[1]
        lift_b = 1 if legs == 'walkB' else 0
        lift_f = 1 if legs == 'walkA' else 0
        c.rect(bx, 35 + cr, 4, 8 - cr - lift_b, 'steel', shade='edge', edge=True)
        c.rrect(bx - 1, 43 - lift_b, 6, 2, 'leather', shade='edge_soft')
        c.rect(fx, 35 + cr, 4, 8 - cr - lift_f, 'steel', shade='edge', edge=True)
        c.rrect(fx, 43 - lift_f, 6, 2, 'leather', shade='edge_soft')
        # knee cops
        c.dots([(bx + 1, 38 + cr), (fx + 2, 38 + cr)], ramp('steel', 3))

    # ---- grimoire on the hip (behind torso edge) ----
    if P.get('book', 'hip') == 'hip':
        c.rrect(13 + l, 30 + ub, 6, 8, 'tome', shade='edge')
        c.rect(14 + l, 31 + ub, 1, 6, 'paper', shade='flat')
        c.dots([(13 + l, 30 + ub), (18 + l, 30 + ub), (13 + l, 37 + ub), (18 + l, 37 + ub)], ramp('gold', 2))
        c.dot(16 + l, 33 + ub, ramp('aegis', 3))
        c.dots([(17 + l, 29 + ub), (18 + l, 28 + ub)], ramp('steel', 2))   # chain

    # ---- shield on the far arm (drawn before torso when low/side) ----
    sh = P.get('shield', 'side')

    def kite(x0, y0, s=1.0, glow=False):
        pts = [(x0, y0), (x0 + 9 * s, y0), (x0 + 9 * s, y0 + 7 * s), (x0 + 4.5 * s, y0 + 13 * s), (x0, y0 + 7 * s)]
        c.poly(pts, 'gold', shade='edge')
        inner = [(x0 + 1.5 * s, y0 + 1.5 * s), (x0 + 7.5 * s, y0 + 1.5 * s), (x0 + 7.5 * s, y0 + 6.5 * s),
                 (x0 + 4.5 * s, y0 + 11 * s), (x0 + 1.5 * s, y0 + 6.5 * s)]
        c.poly(inner, 'aegis', shade='sphere')
        # hexagonal Aegis emblem
        cx, cy = x0 + 4.5 * s, y0 + 5 * s
        hexp = [(cx + 2.2 * math.cos(math.radians(a)), cy + 2.2 * math.sin(math.radians(a))) for a in range(30, 390, 60)]
        c.poly(hexp, 'glass' if glow else 'paper', shade='light' if glow else 'flat')
        c.dot(int(cx), int(cy), '#ffffff' if glow else ramp('aegis', 1))

    def sword_arm():
        hand = P.get('hand', (20 + l, 31 + ub))
        sx, sy = 19 + l, 25 + ub                                    # shoulder
        c.line([(sx, sy), (hand[0], hand[1])], 'steel', width=3, shade='edge_soft')
        c.blade(hand, P.get('ang', 35), P.get('blen', 13), 3, 'steel')
        c.ellipse(hand[0], hand[1], 1.7, 1.7, 'leather', shade='sphere')   # gauntlet

    if P.get('sword', True) and P.get('behind', False):
        sword_arm()

    if sh == 'side':
        kite(27 + l, 25 + ub)
    elif sh == 'low':
        kite(25 + l, 30 + ub)

    # ---- torso: breastplate + tabard ----
    c.rrect(18 + l, 23 + ub, 10, 12, 'steel', shade='edge', edge=True)
    c.poly([(20 + l, 24 + ub), (26 + l, 24 + ub), (26 + l, 38 + cr), (23 + l, 39 + cr), (20 + l, 38 + cr)], 'tabard', shade='edge')
    # gold index sigil "[0]" on the tabard
    c.dots([(21 + l, 28 + ub), (22 + l, 27 + ub), (24 + l, 27 + ub), (25 + l, 28 + ub),
            (21 + l, 29 + ub), (25 + l, 29 + ub), (22 + l, 29 + ub), (24 + l, 29 + ub)], ramp('gold', 3))
    c.dots([(23 + l, 28 + ub), (23 + l, 29 + ub), (23 + l, 30 + ub)], ramp('gold', 1))
    # belt + buckle
    c.rect(18 + l, 33 + ub, 10, 2, 'leather', shade='flat')
    c.rect(22 + l, 33 + ub, 2, 2, 'gold', shade='light')
    # scholar's mantle collar over the shoulders
    c.ellipse(23 + l, 23 + ub, 7, 2.6, 'mantle', shade='sphere')
    # pauldron (near shoulder)
    c.ellipse(19 + l, 24 + ub, 3.2, 2.6, 'steel', shade='sphere')
    c.dot(18 + l, 23 + ub, ramp('gold', 3))

    # ---- head: bascinet helm with T-visor ----
    hd = P.get('head_dy', 0)
    hy = 16 + ub + hd
    # plume (behind helm)
    pl = [0, 1, 0, -1][ph % 4]
    c.poly([(21 + l, hy - 6), (25 + l, hy - 8), (22 + l, hy - 9), (16 + l, hy - 7 + pl), (12 + l, hy - 3 + pl), (15 + l, hy - 4)], 'plume', shade='edge')
    c.ellipse(23 + l, hy, 6.4, 6.6, 'steel', shade='sphere', edge=True)
    c.rect(17 + l, hy + 3, 12, 2, 'steel', shade='edge_soft')         # bevor / chin guard
    c.rect(18 + l, hy - 2, 11, 1, 'gold', shade='flat')               # brow band
    # visor slit (facing right) + vertical breath slit
    c.rect(23 + l, hy, 6, 2, 'ink', shade='flat')
    c.rect(25 + l, hy + 2, 1, 2, 'ink', shade='flat')
    eyes = P.get('eyes', 'calm')
    if eyes == 'hurt':
        c.dots([(25 + l, hy), (27 + l, hy + 1), (26 + l, hy), (26 + l, hy + 1)], '#ff4a3a')
    elif eyes == 'closed':
        c.dots([(25 + l, hy + 1), (26 + l, hy + 1), (27 + l, hy + 1)], ramp('aegis', 1))
    elif eyes == 'fierce':
        c.dots([(25 + l, hy), (26 + l, hy), (27 + l, hy), (28 + l, hy)], '#bff8ff')
        c.dots([(26 + l, hy + 1), (27 + l, hy + 1)], '#7ec8ff')
    else:
        c.dots([(26 + l, hy), (27 + l, hy)], '#7ef0e0')
        c.dot(27 + l, hy + 1, ramp('aegis', 2))
    c.dots([(19 + l, hy - 3), (20 + l, hy - 4)], ramp('steel', 3))      # specular

    # ---- raised shield goes in front of the torso ----
    if sh == 'raised':
        kite(29 + l, 19 + ub, s=1.25, glow=P.get('glow', False))

    # ---- sword arm (near side, in front) ----
    if P.get('sword', True) and not P.get('behind', False):
        sword_arm()

    # ---- open grimoire floating in front (cast) ----
    if P.get('book') == 'open':
        bx, by = P.get('book_pos', (31 + l, 22 + ub))
        c.poly([(bx, by + 1), (bx + 5, by), (bx + 5, by + 6), (bx, by + 7)], 'tome', shade='edge')
        c.poly([(bx + 5, by), (bx + 10, by + 1), (bx + 10, by + 7), (bx + 5, by + 6)], 'tome', shade='edge')
        c.poly([(bx + 1, by + 1), (bx + 5, by + 0.5), (bx + 5, by + 5), (bx + 1, by + 6)], 'paper', shade='flat')
        c.poly([(bx + 5, by + 0.5), (bx + 9, by + 1), (bx + 9, by + 6), (bx + 5, by + 5)], 'paper', shade='light')
        for k in range(3):
            c.dot(bx + 2, by + 2 + k * 1.5, ramp('ink', 2))
            c.dot(bx + 7, by + 2 + k * 1.5, ramp('ink', 2))

    img = c.render()
    # glyph particles + sword smear are drawn on top without outline
    p = img.load()
    for i, (gx, gy) in enumerate(P.get('glyphs', [])):
        col = hx(['#7ec8ff', '#d6a8ff', '#ffe86a'][i % 3])
        for (dx, dy) in ((0, 0), (1, 0), (0, 1)) if i % 2 else ((0, 0),):
            if 0 <= gx + dx < HERO_W and 0 <= gy + dy < HERO_W:
                p[gx + dx, gy + dy] = col
    for i, (sx_, sy_) in enumerate(P.get('smear', [])):
        if 0 <= sx_ < HERO_W and 0 <= sy_ < HERO_W:
            p[sx_, sy_] = hx('#ffffff') if i % 3 else hx('#9fe8ff')
    return shadow_ellipse(img, 23 + P.get('l', 0) * 0.5, 45, 10, 1.6)


def arc_pts(cx, cy, r, a0, a1, step=6, thick=2):
    pts = []
    for a in range(int(a0), int(a1), step if a1 > a0 else -step):
        for t in range(thick):
            rr = r - t
            pts.append((int(round(cx + rr * math.sin(math.radians(a)))), int(round(cy - rr * math.cos(math.radians(a))))))
    return pts


def hero_animations():
    I = dict(hand=(18, 32), ang=215, behind=True)
    A = {}
    # idle: 4-frame breathing, cape flutter, blink on the last frame
    A['idle'] = ([
        hero_frame(dict(I, b=0, cape=0)),
        hero_frame(dict(I, b=0, cape=1)),
        hero_frame(dict(I, b=1, cape=2, hand=(18, 33))),
        hero_frame(dict(I, b=1, cape=3, hand=(18, 33), eyes='closed')),
    ], [220, 160, 220, 120])
    # walk: 6-frame cycle (contact, pass, contact, pass)
    walk_legs = ['walkA', 'walkC', 'stand', 'walkB', 'walkD', 'stand']
    walk_b = [0, -1, 0, 0, -1, 0]
    A['walk'] = ([hero_frame(dict(I, legs=walk_legs[i], b=walk_b[i], cape=i % 4, hand=(18, 32 + walk_b[i]))) for i in range(6)],
                 [90] * 6)
    # attack: anticipation -> hold -> smear strike -> impact -> follow-through -> recover
    A['attack'] = ([
        hero_frame(dict(l=-1, cr=1, hand=(17, 27), ang=-40, cape=1, eyes='fierce')),
        hero_frame(dict(l=-2, cr=2, hand=(16, 23), ang=-75, cape=2, eyes='fierce', shield='low')),
        hero_frame(dict(l=3, cr=1, hand=(30, 27), ang=100, cape=3, eyes='fierce',
                        smear=arc_pts(24, 30, 15, -60, 95, step=5, thick=2))),
        hero_frame(dict(l=4, cr=2, hand=(31, 31), ang=140, cape=0, eyes='fierce',
                        smear=arc_pts(25, 31, 14, 60, 140, step=6, thick=1))),
        hero_frame(dict(l=3, cr=1, hand=(29, 34), ang=165, cape=1, eyes='fierce')),
        hero_frame(dict(l=1, cr=0, hand=(20, 33), ang=150, cape=2)),
    ], [110, 150, 50, 80, 100, 140])
    # defend: brace behind the raised Aegis Shield (GDD "Guard")
    A['defend'] = ([
        hero_frame(dict(I, cr=1, shield='raised', hand=(18, 32), ang=200, eyes='fierce')),
        hero_frame(dict(I, cr=2, l=-1, shield='raised', hand=(17, 33), ang=200, eyes='fierce', cape=1)),
        hero_frame(dict(I, cr=2, l=-1, shield='raised', hand=(17, 33), ang=200, eyes='fierce', cape=2)),
    ], [80, 140, 200])
    # parry: hex shield flares, then counter-thrust (GDD "Perfect Guard / AEGIS ACTIVE!")
    hexring = [(int(37 + 9 * math.cos(math.radians(a))), int(26 + 9 * math.sin(math.radians(a)))) for a in range(0, 360, 20)]
    A['parry'] = ([
        hero_frame(dict(I, cr=2, l=-1, shield='raised', glow=True, hand=(17, 33), ang=200, eyes='fierce', glyphs=hexring[::2])),
        hero_frame(dict(I, cr=1, l=0, shield='raised', glow=True, hand=(18, 32), ang=200, eyes='fierce', glyphs=hexring)),
        hero_frame(dict(cr=1, l=4, shield='side', hand=(33, 28), ang=90, eyes='fierce', blen=14,
                        smear=[(x, 28) for x in range(36, 47)] + [(x, 29) for x in range(38, 46, 2)])),
        hero_frame(dict(cr=0, l=2, shield='side', hand=(30, 29), ang=95, eyes='fierce')),
    ], [70, 110, 90, 160])
    # cast: Grimoire floats open, glyphs spiral up
    glyph_sets = []
    rng = random.Random(4)
    for k in range(6):
        glyph_sets.append([(34 + int(6 * math.cos(k * 0.9 + j)), 20 - k - j * 2 + int(3 * math.sin(k + j))) for j in range(3 + k)])
    A['cast'] = ([
        hero_frame(dict(hand=(19, 33), ang=200, book='open', book_pos=(30, 26), cape=0, eyes='calm')),
        hero_frame(dict(hand=(19, 33), ang=200, book='open', book_pos=(31, 23), cape=1, eyes='fierce', glyphs=glyph_sets[1])),
        hero_frame(dict(hand=(19, 33), ang=200, book='open', book_pos=(31, 21), cape=2, eyes='fierce', b=-1, glyphs=glyph_sets[2])),
        hero_frame(dict(hand=(19, 33), ang=200, book='open', book_pos=(31, 20), cape=3, eyes='fierce', b=-1, glyphs=glyph_sets[3])),
        hero_frame(dict(hand=(19, 33), ang=200, book='open', book_pos=(31, 20), cape=0, eyes='fierce', glyphs=glyph_sets[5])),
        hero_frame(dict(hand=(19, 33), ang=200, book='open', book_pos=(30, 23), cape=1, eyes='calm', glyphs=glyph_sets[0])),
    ], [100, 90, 90, 90, 140, 120])
    # hurt: white flash -> recoil -> recover
    h1 = hero_frame(dict(l=-3, cr=1, hand=(17, 31), ang=220, shield='low', eyes='hurt', head_dy=1))
    A['hurt'] = ([flash(h1, amt=0.85), h1, hero_frame(dict(l=-1, cr=1, hand=(18, 32), ang=215, behind=True, eyes='hurt'))], [60, 160, 140])
    # victory: sword raised, grimoire glints
    A['victory'] = ([
        hero_frame(dict(hand=(24, 22), ang=10, eyes='fierce', cape=0)),
        hero_frame(dict(hand=(24, 18), ang=0, eyes='fierce', cape=1, b=-1, glyphs=[(25, 2), (21, 4)])),
        sparkle(hero_frame(dict(hand=(24, 18), ang=0, eyes='closed', cape=2, b=-1)), [(25, 3), (30, 8)]),
        hero_frame(dict(hand=(24, 18), ang=0, eyes='fierce', cape=3, b=0, glyphs=[(28, 4)])),
    ], [140, 160, 200, 200])
    # death: stagger -> kneel -> sword planted -> Glitch takes the knight apart
    kneel = hero_frame(dict(legs='kneel', cr=3, l=-1, hand=(30, 34), ang=180, eyes='closed', head_dy=2, shield='low'))
    A['death'] = ([
        hero_frame(dict(l=-3, cr=1, hand=(17, 31), ang=220, shield='low', eyes='hurt', head_dy=1)),
        hero_frame(dict(legs='kneel', cr=3, l=-1, hand=(26, 35), ang=150, eyes='hurt', head_dy=1, shield='low')),
        kneel, glitch_dissolve(kneel, 0.35, seed=3), glitch_dissolve(kneel, 0.7, seed=3), glitch_dissolve(kneel, 1.0, seed=3),
    ], [120, 200, 400, 120, 120, 160])
    return A


# ===========================================================================
# EXPORT
# ===========================================================================
def strip(frames):
    w, h = frames[0].size
    s = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        s.alpha_composite(f, (i * w, 0))
    return s


def export_anims(name, anims, roots, manifest, godot_root=None):
    for anim, (frames, durs) in anims.items():
        sheet = strip(frames)
        for base in roots:
            path = os.path.join(base, 'anim', f'{name}_{anim}.png')
            os.makedirs(os.path.dirname(path), exist_ok=True)
            sheet.save(path)
        manifest.setdefault(name, {})[anim] = {
            'frames': len(frames), 'size': frames[0].size[0], 'durations_ms': durs,
            'loop': anim in ('idle', 'walk', 'defend'),
        }


def build_v2(roots, godot_sprites=None, only=None):
    manifest = {}
    hero = hero_animations()
    export_anims('hero', hero, roots, manifest)
    # keep legacy single-frame keys pointing at the new art
    legacy = {
        'player_idle': hero['idle'][0][0], 'player_idle2': hero['idle'][0][2],
        'player_attack': hero['attack'][0][3], 'player_defend': hero['defend'][0][2],
        'player_hurt': hero['hurt'][0][1], 'player_front': hero['walk'][0][0], 'player_front2': hero['walk'][0][3],
    }
    for k, im in legacy.items():
        for base in roots:
            im.save(os.path.join(base, 'chars', f'{k}.png'))
    if godot_sprites:
        os.makedirs(os.path.join(godot_sprites, 'characters'), exist_ok=True)
        hero['idle'][0][0].save(os.path.join(godot_sprites, 'characters', 'scholar_knight.png'))
        strip(hero['idle'][0]).save(os.path.join(godot_sprites, 'characters', 'scholar_knight_idle.png'))

    try:
        import grimoire_monsters_v2 as mv2
        mv2.export_all(roots, manifest, godot_sprites, export_anims, strip)
    except ImportError:
        pass
    try:
        import grimoire_extras_v2 as ex
        ex.export_all(roots, manifest)
    except ImportError:
        pass
    import grimoire_ui_v2 as ui
    ui.export_all(roots, manifest)

    # Flat index for Unity's JsonUtility (no dictionaries): key = "<actor>_<anim>"
    flat = {'entries': [{'key': f'{actor}_{anim}', 'frames': d['frames'], 'durations': d['durations_ms'], 'loop': d['loop']}
                        for actor, anims in manifest.items() for anim, d in anims.items()]}
    for base in roots:
        with open(os.path.join(base, 'anim', 'manifest.json'), 'w', encoding='utf-8') as f:
            json.dump(manifest, f, indent=1)
        with open(os.path.join(base, 'anim', 'anim_index.json'), 'w', encoding='utf-8') as f:
            json.dump(flat, f)
    return manifest


if __name__ == '__main__':
    repo = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
    roots = [os.path.join(repo, "Grimoire's Aegis", 'Assets', 'Resources', 'Gen'), os.path.join(repo, 'assets', 'gen')]
    m = build_v2(roots, os.path.join(repo, 'assets', 'sprites'))
    print('[v2] animated sheets:', sum(len(v) for v in m.values()), 'for', len(m), 'actors')
