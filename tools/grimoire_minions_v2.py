"""Grimoire's Aegis — common minions ("kroco"), 2 per Act, 48x48, facing left.

Style references (not copies): Fear & Hunger for gaunt, unsettling silhouettes and grim
palettes; Undertale for simple readable shapes with very expressive faces.
Same pose contract as grimoire_monsters_v2: f(t, a) with t = idle phase 0..3 and
a = 0 none, 1 anticipation, 2 hold (bullet-time), 3 strike, 4 impact, 5 recover.
"""
import math
import random

from grimoire_art_v2 import Cv, offset, shadow_ellipse, arc_pts
from build_grimoire_assets import hx, ramp

EYE = '#140c1c'


def put(img, pts, col):
    p = img.load(); w, h = img.size
    c = hx(col) if isinstance(col, str) else col
    for (x, y) in pts:
        x, y = int(round(x)), int(round(y))
        if 0 <= x < w and 0 <= y < h:
            p[x, y] = c
    return img


# ---------------------------------------------------------------------------
# ACT I
# ---------------------------------------------------------------------------
def ink_zombie(t=0, a=0):
    """Zombi Tinta: a librarian drowned in ink, still dragging the book chained to its belt."""
    c = Cv(48, 48)
    sway = [0, 1, 1, 0][t]
    reach = {0: 0, 1: -3, 2: -4, 3: 6, 4: 7, 5: 2}[a]          # + = arms forward (left)
    lean = {0: 0, 1: 2, 2: 3, 3: -4, 4: -5, 5: -1}[a]
    # legs (one dragging)
    c.rect(24, 34, 4, 10, 'brown', shade='edge'); c.rect(30, 35, 4, 9, 'brown', shade='edge')
    c.rrect(22, 43, 7, 2, 'obsid', shade='flat'); c.rrect(30, 43, 6, 2, 'obsid', shade='flat')
    # chained book dangling at the hip
    c.line([(32, 30), (35, 36)], 'steel', width=1, shade='light')
    c.rrect(33, 35, 6, 7, 'red', shade='edge'); c.rect(34, 36, 1, 5, 'paper', shade='flat')
    # hunched torso + torn vest
    tx = 20 + lean + sway
    c.poly([(tx, 18), (tx + 14, 17), (tx + 16, 35), (tx + 2, 36)], 'zomb', shade='edge')
    c.poly([(tx + 2, 19), (tx + 13, 18), (tx + 14, 34), (tx + 3, 35)], 'brown', shade='edge')
    c.line([(tx + 8, 20), (tx + 9, 33)], 'brown', width=1, shade='outline')
    c.dots([(tx + 5, 24), (tx + 11, 27), (tx + 6, 31)], ramp('ink', 2))      # ink stains
    # far arm + near arm (reaching)
    for k, (sx, sy) in enumerate(((tx + 3, 21), (tx + 1, 23))):
        hx_ = sx - 9 - reach - k * 2
        hy_ = sy + 7 - (3 if a in (1, 2) else 0) - k
        c.line([(sx, sy), (sx - 4 - reach // 2, sy + 5), (hx_, hy_)], 'zomb', width=2, shade='edge_soft')
        c.dots([(hx_ - 1, hy_ - 1), (hx_ - 2, hy_), (hx_ - 1, hy_ + 1)], ramp('bone', 3))   # claws
    # lolling head
    hdx, hdy = tx + 2 - (1 if t in (1, 2) else 0), 12 + (1 if t == 2 else 0)
    c.ellipse(hdx, hdy, 6, 6, 'zomb', shade='sphere')
    c.poly([(hdx - 6, hdy - 3), (hdx + 2, hdy - 7), (hdx + 7, hdy - 2), (hdx + 6, hdy - 6)], 'hair', shade='flat')
    c.dots([(hdx - 3, hdy), (hdx - 2, hdy)], '#e8f0ff')                       # one milky eye
    c.dots([(hdx + 1, hdy), (hdx + 2, hdy)], EYE)                             # empty socket
    c.dots([(hdx - 3, hdy + 3), (hdx - 2, hdy + 4), (hdx - 1, hdy + 3), (hdx, hdy + 4)], EYE)  # slack jaw
    img = c.render()
    drip = (t + a) % 4
    put(img, [(hdx - 2, hdy + 5 + drip), (hdx - 2, hdy + 6 + drip)], '#2a3a8a')  # ink drool
    if a == 4:
        put(img, arc_pts(tx - 12, 24, 9, 200, 330, step=10, thick=1), '#e8f0ff')
    return shadow_ellipse(img, 27, 45, 12, 1.5)


def paper_bat(t=0, a=0):
    """Kelelawar Kertas: a bat folded out of torn catalogue pages; it shrieks in ink."""
    c = Cv(48, 48)
    fy = {0: 0, 1: -5, 2: -6, 3: 6, 4: 7, 5: 2}[a] + [0, -1, -2, -1][t]
    fx = {3: -12, 4: -14, 5: -4}.get(a, 0)
    cx, cy = 26 + fx, 20 + fy
    flap = [0, 1, 2, 1][t] if a == 0 else (2 if a in (1, 2) else 0)
    wing_tip = [-10, -4, 4][flap]
    for d in (-1, 1):
        base = (cx + d * 3, cy)
        tip = (cx + d * 16, cy + wing_tip)
        mid = (cx + d * 10, cy + wing_tip * 0.5 + 6)
        c.poly([base, (cx + d * 8, cy - 4 + wing_tip * 0.4), tip, mid, (cx + d * 5, cy + 6)], 'paper', shade='edge')
        c.line([(cx + d * 6, cy + 1), (tip[0] - d * 2, tip[1] + 1)], 'paper', width=1, shade='dark')
    c.ellipse(cx, cy + 1, 5, 6, 'paper', shade='sphere')
    c.poly([(cx - 4, cy - 4), (cx - 3, cy - 10), (cx - 1, cy - 5)], 'paper', shade='edge')   # ears
    c.poly([(cx + 1, cy - 5), (cx + 3, cy - 10), (cx + 4, cy - 4)], 'paper', shade='edge')
    img = c.render()
    # ink lettering, one big red eye, fangs
    put(img, [(cx - 8, cy + 2), (cx - 10, cy + 3), (cx + 9, cy + 2), (cx + 11, cy + 4), (cx + 2, cy + 4)], '#3a2c48')
    put(img, [(cx - 2, cy - 1), (cx - 1, cy - 1), (cx - 2, cy), (cx - 1, cy)], '#e8303a')
    put(img, [(cx - 2, cy - 1)], '#ffd0c0')
    open_ = a in (3, 4)
    put(img, [(cx - 3, cy + 4), (cx, cy + 4)] + ([(cx - 3, cy + 5), (cx, cy + 5)] if open_ else []), '#ffffff')
    if open_: put(img, [(cx - 2, cy + 4), (cx - 1, cy + 4), (cx - 2, cy + 5), (cx - 1, cy + 5)], EYE)
    return shadow_ellipse(img, cx, 45, 7 - max(0, -fy) // 3, 1.2, alpha=60)


# ---------------------------------------------------------------------------
# ACT II
# ---------------------------------------------------------------------------
def bone_soldier(t=0, a=0):
    """Prajurit Tulang: a rattling footsoldier with a rusty helm, a chipped sword and a cracked shield."""
    c = Cv(48, 48)
    fy = 1 if t in (1, 2) else 0
    lean = {0: 0, 1: 2, 2: 2, 3: -4, 4: -5, 5: -1}[a]
    # legs
    c.line([(26, 31), (23, 44)], 'bone', width=2, shade='flat'); c.line([(30, 31), (33, 44)], 'bone', width=2, shade='flat')
    c.rect(21, 44, 4, 1, 'bone', shade='dark'); c.rect(32, 44, 4, 1, 'bone', shade='dark')
    # tattered tabard over the ribs
    x0 = 23 + lean
    c.rrect(x0, 18 + fy, 11, 10, 'bone', shade='edge')
    for y in (20, 22, 24, 26): c.line([(x0 + 1, y + fy), (x0 + 9, y + fy)], 'ink', width=1, shade='light')
    c.poly([(x0, 27 + fy), (x0 + 11, 27 + fy), (x0 + 10, 34), (x0 + 6, 32), (x0 + 2, 35)], 'demon', shade='edge')
    # sword arm (behind) — raised for the wind-up, sweeping down on the strike
    ang = {0: 200, 1: -20, 2: -35, 3: 235, 4: 250, 5: 200}[a]
    hand = (x0 + 9, 22 + fy) if a in (1, 2) else (x0 + 2, 26 + fy) if a in (3, 4) else (x0 + 10, 26 + fy)
    c.line([(x0 + 9, 19 + fy), hand], 'bone', width=2, shade='flat')
    c.blade(hand, ang, 13, 3, 'rust', guard_mat='steel', grip_mat='wood', blunt=False)
    # skull + rusty helmet
    c.ellipse(x0 + 5, 12 + fy, 5.5, 5, 'bone', shade='sphere')
    c.rrect(x0 + 1, 14 + fy, 7, 3, 'bone', shade='edge')
    c.poly([(x0 - 1, 11 + fy), (x0 + 1, 5 + fy), (x0 + 10, 5 + fy), (x0 + 12, 11 + fy)], 'rust', shade='edge')
    c.rect(x0 + 4, 3 + fy, 2, 3, 'demon', shade='flat')                         # crest stub
    c.dots([(x0 + 2, 12 + fy), (x0 + 5, 12 + fy)], '#ff3a2a')
    c.dots([(x0 + 2, 16 + fy), (x0 + 4, 16 + fy), (x0 + 6, 16 + fy)], ramp('ink', 1))
    # round shield on the near arm (in front, facing the hero)
    sx = 15 + lean - (2 if a == 3 else 0)
    c.ellipse(sx, 25 + fy, 6, 7, 'wood', shade='sphere', edge=True)
    c.ellipse(sx, 25 + fy, 2, 2, 'steel', shade='sphere')
    c.line([(sx - 4, 21 + fy), (sx + 1, 27 + fy)], 'wood', width=1, shade='outline')   # crack
    img = c.render()
    if a == 3:
        put(img, arc_pts(x0 + 2, 26, 13, 200, 300, step=7, thick=2), '#ffe8d0')
    return shadow_ellipse(img, 28, 45, 11, 1.5)


def crypt_ghoul(t=0, a=0):
    """Ghoul Kripta: crawls on long limbs with a hunched, knobbly spine; lank hair, pinprick eyes. It pounces."""
    c = Cv(48, 48)
    breathe = [0, 1, 1, 0][t]
    jx = {0: 0, 1: 3, 2: 4, 3: -10, 4: -12, 5: -3}[a]
    jy = {0: 0, 1: 2, 2: 3, 3: -6, 4: -1, 5: 0}[a]
    bx, by = 26 + jx, 30 + jy
    # hind leg (folded, knee high) + far hind leg
    c.line([(bx + 9, by - 2), (bx + 14, by - 9), (bx + 15, 44)], 'pale', width=2, shade='edge_soft')
    c.line([(bx + 6, by - 1), (bx + 10, by - 7), (bx + 10, 44)], 'pale', width=1, shade='dark')
    # hunched, knobbly spine: a chain of overlapping ellipses rising to the shoulders
    for k in range(6):
        u = k / 5.0
        x = bx + 9 - u * 15
        y = by - 3 - math.sin(u * math.pi) * (7 + breathe) - u * 3
        c.ellipse(x, y, 4.2 - u * 0.6, 3.6, 'pale', shade='sphere')
    for k in range(4):
        u = (k + 1) / 5.0
        x = bx + 9 - u * 15
        c.dot(int(x), int(by - 3 - math.sin(u * math.pi) * (7 + breathe) - u * 3 - 3), ramp('pale', 3))   # vertebra knobs
    # ribs under the spine
    for k in range(3):
        x = bx + 3 - k * 4
        c.line([(x, by - 8 - breathe), (x - 1, by - 2)], 'pale', width=1, shade='dark')
    # long front arms (reach far out on the pounce)
    reach = {3: 10, 4: 12}.get(a, 0)
    for k, d in enumerate((0, 3)):
        sx, sy = bx - 5 + d, by - 9
        ex, ey = sx - 8 - reach, (by + 2 if a not in (3, 4) else by - 6)
        fx_, fy_ = ex - 3, 44 if a not in (3, 4) else ey + 5
        c.line([(sx, sy), (ex, ey), (fx_, fy_)], 'pale', width=2 if k == 0 else 1, shade='edge_soft' if k == 0 else 'dark')
        c.dots([(fx_ - 1, fy_), (fx_ - 2, fy_ - 1), (fx_ - 2, fy_ + 1)], ramp('bone', 3))
    # head: pale skull-like face, low and forward; hair strands hang from the crown
    hx_, hy_ = bx - 11, by - 12 + (1 if t == 2 else 0)
    c.ellipse(hx_, hy_, 5, 5.5, 'pale', shade='sphere')
    open_ = a in (3, 4)
    c.rrect(hx_ - 5, hy_ + 2, 7, 3 + (2 if open_ else 0), 'ink', shade='flat')                   # gaping mouth
    c.poly([(hx_ - 3, hy_ - 6), (hx_ + 6, hy_ - 5), (hx_ + 5, hy_ + 4), (hx_ + 2, hy_ - 1)], 'hair', shade='flat')
    for i, sx2 in enumerate((hx_ - 2, hx_ + 1, hx_ + 4)):
        c.line([(sx2, hy_ - 5), (sx2 + (i - 1), hy_ + 3 + i * 2)], 'hair', width=1, shade='flat')
    img = c.render()
    put(img, [(hx_ - 3, hy_ - 1), (hx_ - 1, hy_ - 1)], '#f0f8ff')               # pinprick eyes
    teeth = [(hx_ - 4 + i * 2, hy_ + 2) for i in range(3)] + ([(hx_ - 3 + i * 2, hy_ + 6) for i in range(3)] if open_ else [])
    put(img, teeth, ramp('bone', 3))
    return shadow_ellipse(img, bx, 45, 15, 1.5)


# ---------------------------------------------------------------------------
# ACT III
# ---------------------------------------------------------------------------
def glitch_imp(t=0, a=0):
    """Imp Glitch: a gleeful little imp made of corrupted pixels. It lobs glitch orbs and laughs."""
    c = Cv(48, 48)
    bounce = [0, -2, -3, -1][t]
    cx, cy = 27, 30 + bounce + (2 if a in (1, 2) else 0)
    # tail
    c.line([(cx + 6, cy + 6), (cx + 12, cy + 2), (cx + 13, cy - 4)], 'imp', width=1, shade='flat')
    c.poly([(cx + 12, cy - 4), (cx + 15, cy - 6), (cx + 14, cy - 2)], 'imp', shade='light')
    # feet + round body
    c.ellipse(cx - 4, 44, 3, 1.5, 'imp', shade='dark'); c.ellipse(cx + 4, 44, 3, 1.5, 'imp', shade='dark')
    c.ellipse(cx, cy + 4, 9, 10, 'imp', shade='sphere')
    c.ellipse(cx - 1, cy + 7, 5, 5, 'pink', shade='sphere')                       # belly
    # horns
    c.poly([(cx - 7, cy - 4), (cx - 10, cy - 11), (cx - 4, cy - 6)], 'bone', shade='edge')
    c.poly([(cx + 4, cy - 6), (cx + 8, cy - 12), (cx + 7, cy - 4)], 'bone', shade='edge')
    # arms: raised holding the orb on wind-up
    if a in (1, 2):
        c.line([(cx - 6, cy + 2), (cx - 6, cy - 10)], 'imp', width=2, shade='flat')
        c.line([(cx + 5, cy + 2), (cx + 2, cy - 10)], 'imp', width=2, shade='flat')
    else:
        c.line([(cx - 7, cy + 3), (cx - 12 - (4 if a == 3 else 0), cy + (0 if a == 3 else 6))], 'imp', width=2, shade='flat')
        c.line([(cx + 7, cy + 3), (cx + 10, cy + 8)], 'imp', width=2, shade='flat')
    img = c.render()
    # huge expressive face: wide eyes, toothy grin
    put(img, [(cx - 5, cy - 1), (cx - 4, cy - 1), (cx - 5, cy), (cx - 4, cy), (cx + 1, cy - 1), (cx + 2, cy - 1), (cx + 1, cy), (cx + 2, cy)], '#ffffff')
    put(img, [(cx - 5, cy), (cx + 1, cy)], EYE)
    grin = [(cx - 6 + i, cy + 4 + (1 if 1 <= i <= 6 else 0)) for i in range(9)]
    put(img, grin, EYE)
    put(img, [(cx - 4, cy + 4), (cx - 2, cy + 5), (cx, cy + 5), (cx + 2, cy + 4)], '#ffffff')
    if a == 5:   # laughing squint
        put(img, [(cx - 5, cy - 1), (cx - 4, cy - 1), (cx + 1, cy - 1), (cx + 2, cy - 1)], '#86246c')
    # glitch tearing on alternate frames (pixel rows shifted)
    p = img.load()
    if t % 2 == 1 or a == 4:
        row = cy + 2 + t
        line = [p[x, row] for x in range(48)]
        for x in range(48):
            p[x, row] = line[(x + 2) % 48]
        put(img, [(cx + 10, cy - 2), (cx - 11, cy + 8)], '#7ff0d0')
    # the orb
    if a in (1, 2, 3, 4):
        ox, oy, r = {1: (cx - 2, cy - 13, 2), 2: (cx - 2, cy - 14, 3), 3: (cx - 16, cy - 4, 3), 4: (4, cy - 2, 5)}[a]
        put(img, [(ox + r * math.cos(q / 3.0), oy + r * math.sin(q / 3.0)) for q in range(19)], '#d6a8ff')
        put(img, [(ox, oy), (ox + 1, oy), (ox, oy + 1)], '#7ff0d0')
    return shadow_ellipse(img, cx, 45, 9, 1.4)


def crystal_beetle(t=0, a=0):
    """Kumbang Kristal: a squat beetle growing violet crystal shards. It rams with its horn."""
    c = Cv(48, 48)
    rush = {0: 0, 1: 3, 2: 4, 3: -9, 4: -11, 5: -2}[a]
    cx, cy = 27 + rush, 34
    # legs (shuffle)
    for i in range(3):
        lift = (t + i) % 2
        x = cx - 6 + i * 6
        c.line([(x, cy + 3), (x - 3, cy + 6 - lift), (x - 4, 44)], 'obsid', width=1, shade='light')
    # carapace + head + horn
    c.ellipse(cx + 2, cy, 12, 8, 'obsid', shade='sphere')
    c.line([(cx + 2, cy - 7), (cx + 2, cy + 7)], 'obsid', width=1, shade='outline')
    c.ellipse(cx - 10, cy + 1, 5, 4.5, 'obsid', shade='sphere')
    horn = 2 if a in (3, 4) else 0
    c.poly([(cx - 13, cy - 1), (cx - 20 - horn, cy - 6), (cx - 14, cy + 2)], 'crystal', shade='light')
    # crystal shards on the back (glow brighter on the wind-up)
    glow = a in (1, 2)
    for (dx, h, w) in ((-4, 10, 3), (2, 14, 4), (8, 9, 3)):
        c.poly([(cx + dx - w, cy - 5), (cx + dx, cy - 5 - h), (cx + dx + w, cy - 5)], 'crystal', shade='light' if glow else 'edge')
    img = c.render()
    put(img, [(cx - 12, cy), (cx - 11, cy)], '#ffe86a')                          # eyes
    if glow or t == 0:
        put(img, [(cx + 2, cy - 17), (cx - 4, cy - 13)], '#f0e0ff')
    if a == 4:
        rng = random.Random(3)
        put(img, [(cx - 22 + rng.randint(-3, 3), cy - 4 + rng.randint(-6, 6)) for _ in range(10)], '#d6a8ff')
    return shadow_ellipse(img, cx + 1, 45, 14, 1.5)


MINIONS = {
    'ink_zombie': (ink_zombie, 48),
    'paper_bat': (paper_bat, 48),
    'bone_soldier': (bone_soldier, 48),
    'crypt_ghoul': (crypt_ghoul, 48),
    'glitch_imp': (glitch_imp, 48),
    'crystal_beetle': (crystal_beetle, 48),
}

MINION_CAST = {
    'ink_zombie': '#4a6ad8', 'paper_bat': '#e9dcb8', 'bone_soldier': '#ff9a40',
    'crypt_ghoul': '#b8c8e0', 'glitch_imp': '#7ff0d0', 'crystal_beetle': '#d6a8ff',
}
