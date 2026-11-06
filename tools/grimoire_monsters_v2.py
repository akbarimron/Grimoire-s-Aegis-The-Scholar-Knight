"""Grimoire's Aegis — 13 GDD monsters, redrawn at 48x48 (regular) / 64x64 (bosses).

Every monster faces LEFT (towards the hero) and is drawn by a pose function
f(t, a) where  t = idle phase 0..3  and  a = attack stage 0..5:
    0 none, 1 anticipation, 2 hold (used for the GDD 0.6s bullet-time),
    3 strike, 4 impact, 5 recover.
Each attack pose mimics the algorithm the monster represents (GDD section 9).
Hurt and death frames are shared post-processes (white flash + recoil, Glitch dissolve).
"""
import math
import random

from PIL import Image

from grimoire_art_v2 import Cv, flash, tint, offset, glitch_dissolve, shadow_ellipse, sparkle, rot
from build_grimoire_assets import hx, ramp

EYE = '#140c1c'
W = '#ffffff'

BOB = [0, -1, -1, 0]          # gentle 4-phase float
SQ = [0, 1, 2, 1]             # squash amount


def put(img, pts, col):
    p = img.load()
    w, h = img.size
    c = hx(col) if isinstance(col, str) else col
    for (x, y) in pts:
        x, y = int(x), int(y)
        if 0 <= x < w and 0 <= y < h:
            p[x, y] = c
    return img


# ---------------------------------------------------------------------------
# ACT I — SEARCHING
# ---------------------------------------------------------------------------
def corrupted_slime(t=0, a=0):
    """Ink-green ooze that swallowed index cards; the numbers still float inside it."""
    c = Cv(48, 48)
    s = SQ[t]
    rx, ry, cy = 15 + s * 0.6, 11 - s * 0.5, 33 + s * 0.4
    if a in (1, 2):
        rx, ry, cy = 17 + a, 8.5 - a * 0.5, 36
    elif a == 3:
        rx, ry, cy = 13, 13, 29
    elif a == 4:
        rx, ry, cy = 19, 7, 37
    c.ellipse(25, cy, rx, ry, 'slime', shade='sphere')
    c.ellipse(25, cy + ry * 0.55, rx * 0.95, ry * 0.45, 'slime', shade='dark')
    # crown blob + drips
    c.ellipse(25 + (s - 1), cy - ry * 0.7, rx * 0.55, ry * 0.55, 'slime', shade='sphere')
    for dx in (-10, -3, 8):
        c.rect(int(25 + dx), int(cy + ry * 0.7), 2, 3 + (t + dx) % 3, 'slime', shade='dark')
    # half-digested index cards inside the body
    c.rrect(int(18), int(cy - 1), 7, 5, 'paper', shade='edge')
    c.rrect(int(27), int(cy + 1), 6, 5, 'paper', shade='edge')
    c.dots([(20, cy + 1), (21, cy + 1), (21, cy + 2), (20, cy + 3), (21, cy + 3)], ramp('red', 2))   # "2"
    c.dots([(29, cy + 2), (29, cy + 3), (29, cy + 4), (30, cy + 2)], ramp('ink', 2))                   # "1"
    # eyes (big, glossy, looking left) + mouth
    ey = cy - ry * 0.35
    c.ellipse(17, ey, 3.2, 3.6, 'eye', shade='flat')
    c.ellipse(26, ey - 1, 2.8, 3.2, 'eye', shade='flat')
    c.dots([(16, ey), (16, ey + 1), (25, ey - 1), (25, ey)], EYE)
    c.dots([(17, ey - 2), (26, ey - 3)], W)
    if a in (3, 4):
        c.ellipse(20, ey + 6, 5, 3, 'ink', shade='flat')
        c.dots([(17, ey + 4), (19, ey + 4), (21, ey + 4), (23, ey + 4)], ramp('bone', 3))
    else:
        c.dots([(18, ey + 5), (19, ey + 6), (20, ey + 6), (21, ey + 5)], ramp('ink', 1))
    img = c.render()
    put(img, [(14, cy - ry + 3), (15, cy - ry + 2), (16, cy - ry + 2)], '#d8ff9a')   # specular
    # corrupted glitch pixels drifting off the body
    put(img, [(38 - t, 22 + t), (40, 26 - t), (11 + t, 24)], '#c6ffef')
    if a == 3:
        img = offset(img, -6, -3)
    elif a == 4:
        img = offset(img, -7, 0)
    return shadow_ellipse(img, 25, 45, 14, 1.6)


def lost_wisp(t=0, a=0):
    """A drowned scholar's lantern. Its ghost-light sweeps the shelves one by one (linear search)."""
    c = Cv(48, 48)
    by = 20 + BOB[t] * 2 + (2 if a in (1, 2) else 0)
    sw = [-1, 0, 1, 0][t] if a == 0 else (3 if a in (1, 2) else -2)
    # ghost tail
    tail = [(22, by + 10), (32, by + 10), (35 + sw, by + 18), (31 + sw, by + 23), (37 + sw * 2, by + 26),
            (28 + sw, by + 25), (24, by + 20)]
    c.poly(tail, 'ectoplasm', shade='edge')
    # chain + hook
    for k in range(4):
        c.dot(27, by - 10 + k * 2, ramp('steel', 2))
    # lantern cage
    c.rrect(19, by - 4, 17, 3, 'rust', shade='edge')
    c.rrect(19, by + 10, 17, 3, 'rust', shade='edge')
    flame = 1 if t % 2 else 0
    c.ellipse(27.5, by + 4, 7 + flame, 7.5 + flame, 'ectoplasm', shade='sphere')
    c.ellipse(27, by + 5, 4.5, 5, 'glass', shade='sphere')
    for x in (20, 27, 34):
        c.rect(x, by - 2, 2, 12, 'rust', shade='edge_soft')
    c.poly([(23, by - 4), (27, by - 9), (31, by - 4)], 'rust', shade='edge')
    # ghost face inside the flame (looking left)
    c.dots([(24, by + 3), (25, by + 3), (29, by + 3)], EYE)
    c.dots([(25, by + 7), (26, by + 8), (27, by + 7)], EYE)
    # torn index tag tied to the bottom
    c.poly([(31, by + 13), (37, by + 14), (36, by + 19), (31, by + 18)], 'paper', shade='flat')
    c.dots([(33, by + 15), (34, by + 16), (33, by + 17)], ramp('ink', 2))
    img = c.render()
    # searchlight sweeping left (attack)
    if a >= 3:
        L = {3: 18, 4: 26, 5: 10}[a]
        for x in range(max(0, 19 - L), 19):
            spread = (19 - x) // 5
            for y in range(by + 3 - spread, by + 6 + spread):
                if (x + y) % (2 if a == 4 else 3) == 0:
                    put(img, [(x, y)], '#bffff0' if a == 4 else '#7ff0d0')
    # orbiting will-o-wisps
    for k in range(2):
        ang = (t * 90 + k * 180) * math.pi / 180
        put(img, [(27 + 13 * math.cos(ang), by + 4 + 6 * math.sin(ang))], '#7ff0d0')
    return shadow_ellipse(img, 27, 45, 9, 1.4, alpha=60)


def bookworm_horror(t=0, a=0):
    """Segmented maggot erupting from a swollen tome; a ring maw of teeth bites twice (TP-03)."""
    c = Cv(48, 48)
    # tome at the base
    c.poly([(8, 37), (24, 34), (24, 44), (8, 44)], 'red', shade='edge')
    c.poly([(24, 34), (40, 37), (40, 44), (24, 44)], 'red', shade='edge')
    c.poly([(10, 36), (24, 33), (24, 41), (10, 42)], 'paper', shade='flat')
    c.poly([(24, 33), (38, 36), (38, 42), (24, 41)], 'paper', shade='light')
    for k in range(3):
        c.line([(12, 38 + k), (21, 36 + k)], 'paper', width=1, shade='dark')
    # body segments, swaying; attack pulls back then lunges left
    sway = [0, 1, 2, 1][t]
    lean = {0: 0, 1: 3, 2: 4, 3: -7, 4: -8, 5: -2}[a]
    seg = []
    for i in range(6):
        k = i / 5.0
        x = 25 + math.sin(k * 2.4 + t * 0.6) * (2 + sway * 0.5) + lean * k * k
        y = 34 - i * 4.4 + (2 if a in (1, 2) else 0) * k
        seg.append((x, y, 5.2 - i * 0.25))
    for (x, y, r) in seg:
        c.ellipse(x, y, r, r * 0.85, 'chitin', shade='sphere')
    for (x, y, r) in seg[:-1]:
        c.line([(x - r + 1, y + r * 0.6), (x + r - 1, y + r * 0.6)], 'chitin', width=1, shade='outline')
    hx_, hy_, hr = seg[-1]
    open_ = a in (3, 4)
    c.ellipse(hx_ - 1, hy_, 5.5, 5, 'flesh2', shade='sphere')
    c.ellipse(hx_ - 3, hy_, 2.5 + (1.5 if open_ else 0), 3 + (1.5 if open_ else 0), 'vein', shade='dark')
    teeth = [(hx_ - 3 + 3.6 * math.cos(q), hy_ + 3.6 * math.sin(q)) for q in [i * math.pi / 4 for i in range(8)]]
    c.dots([(int(x), int(y)) for (x, y) in teeth], ramp('bone', 3))
    # many small eyes along the head
    c.dots([(int(hx_ + 1), int(hy_ - 4)), (int(hx_ + 3), int(hy_ - 3)), (int(hx_ + 3), int(hy_ - 1))], '#ffe86a')
    img = c.render()
    if a == 4:
        put(img, [(int(hx_) - 9, int(hy_) - 2 + i) for i in range(5)] + [(int(hx_) - 10, int(hy_) + 0)], '#ffffff')
    # loose page scraps
    put(img, [(6 + t, 30), (7 + t, 30), (41, 28 - t), (42, 28 - t)], ramp('paper', 3))
    return shadow_ellipse(img, 24, 45, 16, 1.4)


def twin_faced_librarian(t=0, a=0):
    """Two masks, one curator: the tragic face reads every shelf, the grinning face halves them.
    Attack tears a tome in two — the search space splits (Binary Search)."""
    c = Cv(48, 48)
    fy = BOB[t]
    # robe
    c.poly([(14, 20 + fy), (34, 20 + fy), (39, 45), (31, 44), (24, 46), (17, 44), (9, 45)], 'robe2', shade='edge')
    c.poly([(21, 22 + fy), (27, 22 + fy), (28, 45), (20, 45)], 'gold', shade='edge')
    for y in (27, 32, 37, 42):
        c.dots([(23, y + fy), (24, y + fy), (25, y + fy)], ramp('tome', 1))
    # high collar
    c.poly([(14, 19 + fy), (24, 23 + fy), (34, 19 + fy), (34, 22 + fy), (24, 26 + fy), (14, 22 + fy)], 'gold', shade='edge')
    # two-faced head: left mask (silver, sorrow) faces the hero, right (gold, grin) faces away
    c.ellipse(24, 12 + fy, 7.5, 8, 'robe2', shade='sphere')                 # hood
    c.ellipse(19, 13 + fy, 5, 6, 'pale', shade='sphere')
    c.ellipse(29, 13 + fy, 5, 6, 'gold', shade='sphere')
    c.line([(24, 6 + fy), (24, 19 + fy)], 'ink', width=1, shade='flat')
    c.dots([(17, 12 + fy), (20, 12 + fy)], EYE)
    c.dots([(17, 13 + fy), (20, 13 + fy)], '#7ec8ff')                       # tear lines
    c.dots([(17, 17 + fy), (18, 16 + fy), (19, 16 + fy), (20, 17 + fy)], EYE)
    c.dots([(28, 12 + fy), (31, 12 + fy)], '#b02a32')
    c.dots([(28, 16 + fy), (29, 17 + fy), (30, 17 + fy), (31, 16 + fy)], '#b02a32')
    # hands holding a tome — attack rips it apart
    split = {0: 0, 1: 1, 2: 2, 3: 7, 4: 9, 5: 4}[a]
    lift = {0: 0, 1: -3, 2: -5, 3: -4, 4: -2, 5: 0}[a]
    bx, by = 17, 28 + fy + lift
    c.rrect(bx - split, by, 7, 9, 'tome', shade='edge')
    c.rect(bx - split + 5, by + 1, 2, 7, 'paper', shade='flat')
    c.rrect(bx + 7 + split, by, 7, 9, 'tome', shade='edge')
    c.rect(bx + 7 + split, by + 1, 2, 7, 'paper', shade='flat')
    c.ellipse(bx - split - 1, by + 4, 2, 2, 'pale', shade='sphere')
    c.ellipse(bx + 15 + split, by + 4, 2, 2, 'pale', shade='sphere')
    img = c.render()
    if a >= 3:   # page confetti from the tear
        rng = random.Random(a)
        put(img, [(bx + 7 + rng.randint(-3, 3), by + rng.randint(-6, 4)) for _ in range(8)], ramp('paper', 3))
    return shadow_ellipse(img, 24, 45, 14, 1.6)


def the_blind_archivist(t=0, a=0):
    """ACT I BOSS (64x64). Blindfolded keeper of the sunken archive; guesses where you hide
    on a sorted shelf. Its lantern-ledger slams down on the 'mid' it predicted."""
    c = Cv(64, 64)
    fy = BOB[t]
    lean = {0: 0, 1: 2, 2: 3, 3: -4, 4: -5, 5: -1}[a]
    # robe and tattered hem
    c.poly([(18 + lean, 22 + fy), (46 + lean, 22 + fy), (54, 60), (47, 58), (42, 61), (35, 58), (29, 61), (22, 58), (12, 60)], 'mantle', shade='edge')
    c.poly([(26 + lean, 24 + fy), (38 + lean, 24 + fy), (41, 60), (23, 60)], 'robe2', shade='edge')
    # golden stole with index runes
    c.poly([(29 + lean, 24 + fy), (35 + lean, 24 + fy), (35, 58), (29, 58)], 'gold', shade='edge')
    for y in range(29, 57, 5):
        c.dots([(31, y), (32, y), (33, y)], ramp('aegis', 3))
    # shoulder candles
    for sx in (19, 44):
        c.rect(sx + lean, 15 + fy, 3, 7, 'paper', shade='edge_soft')
    # hood + pale face + blindfold
    c.ellipse(32 + lean, 15 + fy, 10, 10, 'mantle', shade='sphere')
    c.ellipse(31 + lean, 17 + fy, 7, 7.5, 'pale', shade='sphere')
    c.rrect(23 + lean, 13 + fy, 16, 5, 'cloth', shade='edge')
    c.poly([(38 + lean, 15 + fy), (44 + lean, 20 + fy), (42 + lean, 22 + fy)], 'cloth', shade='edge')
    c.dots([(27 + lean, 15 + fy), (28 + lean, 15 + fy), (33 + lean, 15 + fy), (34 + lean, 15 + fy)], ramp('red', 2))
    c.dots([(28 + lean, 21 + fy), (29 + lean, 22 + fy), (30 + lean, 22 + fy), (31 + lean, 21 + fy)], ramp('ink', 1))
    # long arm + giant ledger held aloft (attack raises then slams it)
    ly = {0: 28, 1: 18, 2: 14, 3: 26, 4: 34, 5: 30}[a] + fy
    lx = {0: 6, 1: 8, 2: 9, 3: 3, 4: 2, 5: 5}[a]
    c.line([(22 + lean, 27 + fy), (lx + 10, ly + 6)], 'pale', width=3, shade='edge_soft')
    c.rrect(lx, ly, 14, 16, 'red', shade='edge')
    c.rect(lx + 2, ly + 2, 10, 12, 'paper', shade='flat')
    for k in range(4):
        c.line([(lx + 3, ly + 4 + k * 3), (lx + 10, ly + 4 + k * 3)], 'ink', width=1, shade='light')
    c.dots([(lx, ly), (lx + 13, ly), (lx, ly + 15), (lx + 13, ly + 15)], ramp('gold', 3))
    # fog lantern on chain (right side)
    sw = [-1, 0, 1, 0][t]
    c.line([(46 + lean, 28 + fy), (52 + sw, 36 + fy)], 'steel', width=1)
    c.ellipse(52 + sw, 41 + fy, 5, 6, 'ectoplasm', shade='sphere')
    c.rect(49 + sw, 34 + fy, 7, 2, 'gold', shade='flat')
    img = c.render()
    put(img, [(20 + lean, 13 + fy), (45 + lean, 13 + fy)], '#ffe060')        # candle flames
    put(img, [(20 + lean, 12 + fy - (t % 2)), (45 + lean, 12 + fy - ((t + 1) % 2))], '#fff47e')
    if a == 4:   # shockwave at impact
        put(img, [(x, 61) for x in range(0, 20, 2)] + [(x, 60) for x in range(1, 16, 3)], '#9fe8ff')
    return shadow_ellipse(img, 33, 61, 22, 1.8)


# ---------------------------------------------------------------------------
# ACT II — BASIC SORTING
# ---------------------------------------------------------------------------
def skeleton_archer(t=0, a=0):
    """Hooded skeleton; fires 3 numbered bone arrows, one per bubble-sort pass (TP-06)."""
    c = Cv(48, 48)
    fy = 1 if t in (1, 2) else 0
    # tattered hood/cape
    c.poly([(22, 10 + fy), (34, 10 + fy), (36, 34), (31, 32), (28, 36), (24, 33), (20, 35)], 'moss', shade='edge')
    # legs
    c.line([(25, 31), (23, 44)], 'bone', width=2, shade='flat')
    c.line([(29, 31), (31, 44)], 'bone', width=2, shade='flat')
    c.rect(21, 44, 4, 1, 'bone', shade='dark'); c.rect(30, 44, 4, 1, 'bone', shade='dark')
    # ribcage + spine
    c.rrect(22, 19 + fy, 10, 10, 'bone', shade='edge')
    for y in (21, 23, 25, 27):
        c.line([(23, y + fy), (31, y + fy)], 'ink', width=1, shade='light')
    c.rect(26, 29 + fy, 2, 3, 'bone', shade='flat')
    c.rrect(23, 30 + fy, 8, 3, 'bone', shade='edge')                       # pelvis
    # skull (hood shading over it)
    c.ellipse(27, 13 + fy, 6, 5.5, 'bone', shade='sphere')
    c.rrect(23, 16 + fy, 7, 3, 'bone', shade='edge')
    c.poly([(21, 9 + fy), (29, 6 + fy), (34, 10 + fy), (33, 13 + fy), (24, 10 + fy)], 'moss', shade='edge')
    c.dots([(24, 13 + fy), (25, 13 + fy), (28, 13 + fy)], '#ff3a2a')
    c.dots([(24, 17 + fy), (26, 17 + fy), (28, 17 + fy)], ramp('ink', 1))
    # bow arm and bow — draw pulls the string back to the right
    draw = {0: 0, 1: 3, 2: 5, 3: 0, 4: 0, 5: 1}[a]
    bowx = 14
    c.line([(bowx + 3, 8), (bowx - 1, 22), (bowx + 3, 36)], 'bone', width=2, shade='edge_soft')
    c.dots([(bowx + 3, 8), (bowx + 3, 36)], ramp('red', 2))
    sx = bowx + 4 + draw
    c.line([(bowx + 3, 9), (sx, 22), (bowx + 3, 35)], 'web', width=1, shade='light')
    c.line([(23, 22 + fy), (bowx, 22)], 'bone', width=2, shade='flat')   # front arm
    c.line([(23, 21 + fy), (sx + 1, 22)], 'bone', width=2, shade='flat')  # draw arm
    if a in (1, 2, 5):
        c.line([(bowx - 4, 22), (sx, 22)], 'bone', width=1, shade='light')
        c.poly([(bowx - 7, 22), (bowx - 4, 20), (bowx - 4, 24)], 'ectoplasm', shade='light')
    img = c.render()
    if a in (3, 4):   # arrow in flight with a glowing pass number
        ax = {3: 8, 4: 0}[a]
        put(img, [(x, 22) for x in range(ax, ax + 10)], ramp('bone', 3))
        put(img, [(ax - 1, 21), (ax - 1, 23), (ax - 2, 22)], '#7ff0d0')
        put(img, [(ax + 10, 21), (ax + 10, 23)], ramp('red', 3))
    return shadow_ellipse(img, 27, 45, 10, 1.4)


def venomous_arachnid(t=0, a=0):
    """Catacomb spider that always devours the smallest prey and drags it to the front
    of its web (Selection Sort). Rears up, then strikes with dripping fangs."""
    c = Cv(48, 48)
    rear = {0: 0, 1: 3, 2: 5, 3: -1, 4: -2, 5: 1}[a]
    lunge = {3: -5, 4: -6}.get(a, 0)
    cx, cy = 27 + lunge, 30 - rear
    # legs: 4 pairs, alternating gait in idle
    for i in range(3):
        ph = (t + i) % 2
        lift = ph
        # far legs (right): knee high, foot planted
        kx, ky = cx + 5 + i * 4, cy - 10 + i * 2 - lift
        c.line([(cx + 2, cy - 1 + i), (kx, ky)], 'chitin', width=1, shade='light')
        c.line([(kx, ky), (kx + 2 + i * 2, 44)], 'chitin', width=1, shade='flat')
        c.dot(kx, ky, ramp('chitin', 3))
        # near legs (left) — front pair rises when rearing
        up = rear * 2 if i == 0 else (rear if i == 1 else 0)
        kx, ky = cx - 6 - i * 4, cy - 11 + i * 2 - lift - up
        c.line([(cx - 3, cy - 1 + i), (kx, ky)], 'chitin', width=1, shade='light')
        c.line([(kx, ky), (kx - 2 - i * 2 + up // 2, 44 - up * 2)], 'chitin', width=1, shade='flat')
        c.dot(kx, ky, ramp('chitin', 3))
    # abdomen (behind, right) with neon hourglass
    c.ellipse(cx + 8, cy - 4 + rear // 2, 9, 8, 'chitin', shade='sphere')
    c.poly([(cx + 6, cy - 9), (cx + 10, cy - 9), (cx + 8, cy - 5), (cx + 10, cy - 1), (cx + 6, cy - 1), (cx + 8, cy - 5)], 'slime', shade='light')
    # cephalothorax
    c.ellipse(cx - 3, cy, 6.5, 5.5, 'obsid', shade='sphere')
    # eyes cluster (8)
    c.dots([(cx - 7, cy - 2), (cx - 5, cy - 3), (cx - 3, cy - 3), (cx - 6, cy), (cx - 4, cy - 1)], '#ff2a2a')
    c.dots([(cx - 5, cy - 3)], '#ffb0a0')
    # fangs
    fo = 2 if a in (3, 4) else 0
    c.poly([(cx - 8, cy + 3), (cx - 9 - fo, cy + 7 + fo), (cx - 7, cy + 4)], 'bone', shade='light')
    c.poly([(cx - 5, cy + 4), (cx - 5 - fo, cy + 8 + fo), (cx - 3, cy + 4)], 'bone', shade='light')
    img = c.render()
    drip = (t % 3)
    put(img, [(cx - 9 - fo, cy + 8 + fo + drip), (cx - 5 - fo, cy + 9 + fo + (drip + 1) % 3)], '#a8e272')
    # a thread of web up to the ceiling
    put(img, [(cx + 8, y) for y in range(0, max(0, cy - 12), 2)], ramp('web', 2))
    return shadow_ellipse(img, cx + 2, 45, 17, 1.6)


def grave_digger(t=0, a=0):
    """Hunched ghoul that slides each coffin into its proper grave (Insertion Sort).
    Attack: shovel overhead, then buries the hero's item bag (Buried)."""
    c = Cv(48, 48)
    fy = BOB[t]
    lean = {0: 0, 1: 2, 2: 3, 3: -3, 4: -4, 5: -1}[a]
    # small coffin at the back
    c.poly([(36, 34), (41, 32), (45, 34), (44, 44), (38, 44)], 'wood', shade='edge')
    c.line([(40, 35), (40, 41)], 'gold', width=1, shade='flat')
    c.line([(38, 37), (42, 37)], 'gold', width=1, shade='flat')
    # legs + boots
    c.rect(20, 35, 4, 8, 'leather', shade='edge'); c.rect(27, 35, 4, 8, 'leather', shade='edge')
    c.rrect(18, 42, 7, 3, 'obsid', shade='edge_soft'); c.rrect(26, 42, 7, 3, 'obsid', shade='edge_soft')
    # coat (hunched)
    c.poly([(16 + lean, 20 + fy), (33 + lean, 18 + fy), (35, 38), (14, 38)], 'brown', shade='edge')
    c.line([(24 + lean, 21 + fy), (24, 37)], 'brown', width=1, shade='outline')
    c.dots([(26, 26 + fy), (26, 30 + fy), (26, 34)], ramp('gold', 2))
    # head with wide-brim hat and a candle on top
    c.ellipse(21 + lean, 15 + fy, 6, 5.5, 'zomb', shade='sphere')
    c.rrect(12 + lean, 9 + fy, 18, 3, 'obsid', shade='edge')
    c.rrect(15 + lean, 4 + fy, 11, 6, 'obsid', shade='edge')
    c.rect(19 + lean, 1 + fy, 2, 3, 'paper', shade='flat')
    c.dots([(17 + lean, 15 + fy), (20 + lean, 15 + fy)], '#ffe060')
    c.dots([(17 + lean, 18 + fy), (18 + lean, 19 + fy), (19 + lean, 18 + fy), (20 + lean, 19 + fy)], ramp('bone', 3))
    # shovel (raised in anticipation, slammed on strike)
    ang = {0: 20, 1: -40, 2: -60, 3: -150, 4: -170, 5: -10}[a]
    hand = (14 + lean, 26 + fy) if a in (0, 5) else (20 + lean, 18 + fy) if a in (1, 2) else (12 + lean, 28 + fy)
    rad = math.radians(ang)
    ux, uy = math.sin(rad), -math.cos(rad)
    tip = (hand[0] + ux * 18, hand[1] + uy * 18)
    butt = (hand[0] - ux * 5, hand[1] - uy * 5)
    c.line([butt, tip], 'wood', width=2, shade='edge_soft')
    nx, ny = -uy, ux
    blade = [(tip[0] + nx * 3, tip[1] + ny * 3), (tip[0] - nx * 3, tip[1] - ny * 3),
             (tip[0] - nx * 2 + ux * 6, tip[1] - ny * 2 + uy * 6), (tip[0] + ux * 7, tip[1] + uy * 7), (tip[0] + nx * 2 + ux * 6, tip[1] + ny * 2 + uy * 6)]
    c.poly(blade, 'steel', shade='edge')
    c.ellipse(hand[0], hand[1], 2, 2, 'zomb', shade='sphere')
    img = c.render()
    put(img, [(19 + lean, 0 + fy), (19 + lean, 1 + fy - t % 2)], '#ffe060')
    if a == 4:   # dirt burst
        rng = random.Random(9)
        put(img, [(rng.randint(0, 14), rng.randint(36, 46)) for _ in range(14)], ramp('brown', 2))
    return shadow_ellipse(img, 25, 45, 14, 1.6)


def the_abyss_golem(t=0, a=0):
    """ACT II BOSS (64x64). Ossuary colossus; its chest holds candle-bars of uneven height
    that it re-sorts while it fights. Attack: both fists raised, then a ground slam."""
    c = Cv(64, 64)
    fy = [0, 0, 1, 1][t]
    up = {0: 0, 1: 6, 2: 9, 3: -2, 4: -4, 5: 1}[a]
    # legs
    c.rrect(17, 46, 11, 15, 'stone', shade='edge'); c.rrect(36, 46, 11, 15, 'stone', shade='edge')
    c.rrect(15, 58, 14, 4, 'stone', shade='dark'); c.rrect(35, 58, 14, 4, 'stone', shade='dark')
    # torso monolith
    c.poly([(12, 22 + fy), (52, 22 + fy), (50, 50), (14, 50)], 'stone', shade='edge')
    c.rrect(18, 27 + fy, 28, 18, 'obsid', shade='edge')
    # rib-bones embedded
    for k in range(3):
        c.line([(14, 25 + k * 6 + fy), (19, 27 + k * 6 + fy)], 'bone', width=1, shade='light')
        c.line([(50, 25 + k * 6 + fy), (45, 27 + k * 6 + fy)], 'bone', width=1, shade='light')
    # sorting candles inside the chest (heights cycle as it "sorts")
    hs = [[5, 11, 7, 14, 9], [5, 7, 11, 9, 14], [5, 7, 9, 11, 14], [5, 7, 9, 11, 14]][t]
    if a >= 3:
        hs = [14, 5, 11, 7, 9]
    for i, h in enumerate(hs):
        x = 21 + i * 5
        c.rect(x, 43 + fy - h, 3, h, 'bone', shade='edge_soft')
    # arms: raised for windup, slammed on strike
    for side, ax in ((0, 4), (1, 50)):
        ay = 24 + fy - up
        c.rrect(ax, ay, 10, 20, 'stone', shade='edge')
        c.rrect(ax - 1, ay + 18 if up < 6 else ay - 6, 12, 9, 'stone', shade='sphere')
    # head with molten visor
    c.rrect(23, 9 + fy, 18, 13, 'stone', shade='edge')
    c.poly([(23, 9 + fy), (27, 4 + fy), (37, 4 + fy), (41, 9 + fy)], 'stone', shade='edge')
    c.rrect(26, 13 + fy, 12, 4, 'lava', shade='light')
    c.dots([(28, 14 + fy), (34, 14 + fy)], '#fff47e')
    # moss
    c.dots([(14, 23 + fy), (15, 23 + fy), (49, 23 + fy), (24, 9 + fy), (25, 9 + fy)], ramp('moss', 3))
    img = c.render()
    for i, h in enumerate(hs):
        x = 21 + i * 5
        put(img, [(x + 1, 42 + fy - h), (x + 1, 41 + fy - h - (t + i) % 2)], '#ffe060' if i % 2 else '#ffb030')
    if a == 4:
        put(img, [(x, 62) for x in range(0, 64, 3)] + [(x, 61) for x in range(1, 64, 5)], ramp('lava', 3))
        img = offset(img, 0, 1)
    return shadow_ellipse(img, 32, 61, 24, 2)


# ---------------------------------------------------------------------------
# ACT III — DIVIDE & CONQUER + BIG-O
# ---------------------------------------------------------------------------
def hydra_of_division(t=0, a=0):
    """Every severed head splits in two (divide) and they rejoin to strike (merge)."""
    c = Cv(48, 48)
    # coiled body
    c.ellipse(28, 37, 14, 7.5, 'teal', shade='sphere')
    c.ellipse(28, 39, 9, 4, 'sand', shade='sphere')
    c.poly([(40, 36), (46, 32), (45, 40), (40, 42)], 'teal', shade='edge')     # tail
    for k in range(4):
        c.dot(22 + k * 4, 34, ramp('teal', 3))
    # heads: 3 in idle; attack converges them to one point (merge)
    base = [(-11, -22), (0, -27), (10, -21)]
    conv = {0: 0, 1: -0.2, 2: -0.3, 3: 0.75, 4: 0.9, 5: 0.3}[a]
    target = (-16, -14)
    heads = []
    for i, (dx, dy) in enumerate(base):
        sx = math.sin((t + i) * 1.4) * 1.5
        sy = math.cos((t + i) * 1.2) * 1
        if conv < 0:
            dx, dy = dx * (1 - conv * 0.3) + 4 * -conv, dy * (1 - conv * 0.4)
        else:
            dx, dy = dx + (target[0] - dx) * conv, dy + (target[1] - dy) * conv * 0.8 + i * 3 * conv
        heads.append((28 + dx + sx, 34 + dy + sy))
    for (hx_, hy_) in heads:
        c.line([(28, 33), ((28 + hx_) / 2 + 2, (33 + hy_) / 2), (hx_ + 2, hy_ + 3)], 'teal', width=3, shade='edge_soft')
    for (hx_, hy_) in heads:
        c.ellipse(hx_, hy_, 4.6, 3.8, 'teal', shade='sphere')
        c.poly([(hx_ - 4, hy_ + 1), (hx_ - 8, hy_ + 2), (hx_ - 4, hy_ + 3)], 'teal', shade='edge')   # snout
        c.poly([(hx_ + 1, hy_ - 3), (hx_ + 4, hy_ - 7), (hx_ + 3, hy_ - 2)], 'sand', shade='light')  # horn
        c.dot(int(hx_ - 2), int(hy_ - 1), '#ff3a2a')
        if a in (3, 4):
            c.dots([(int(hx_ - 7), int(hy_ + 3)), (int(hx_ - 6), int(hy_ + 4))], ramp('bone', 3))
    img = c.render()
    if a in (1, 2):   # split hint: faint ghost heads (dividing)
        for (hx_, hy_) in heads:
            put(img, [(int(hx_ - 2), int(hy_ - 6)), (int(hx_ + 1), int(hy_ - 7))], '#7ff0d0')
    return shadow_ellipse(img, 28, 45, 15, 1.6)


def pivot_wraith(t=0, a=0):
    """Hooded wraith balanced on a golden pivot. Smaller values fall left, larger right.
    Attack: picks a victim as pivot and cleaves with twin partition scythes."""
    c = Cv(48, 48)
    fy = BOB[t] * 2
    spin = {0: 0, 1: -25, 2: -40, 3: 70, 4: 110, 5: 20}[a]
    # golden fulcrum
    c.poly([(24, 34), (31, 44), (17, 44)], 'gold', shade='edge')
    c.rect(16, 44, 16, 1, 'gold', shade='dark')
    # tattered cloak
    c.poly([(16, 14 + fy), (32, 14 + fy), (30, 28 + fy), (27, 34 + fy), (24, 30 + fy), (21, 35 + fy), (18, 28 + fy)], 'robe2', shade='edge')
    c.poly([(20, 16 + fy), (28, 16 + fy), (26, 30 + fy), (22, 30 + fy)], 'obsid', shade='flat')
    # hood with crown
    c.ellipse(24, 10 + fy, 6, 6, 'obsid', shade='sphere')
    c.poly([(18, 6 + fy), (20, 1 + fy), (22, 4 + fy), (24, 0 + fy), (26, 4 + fy), (28, 1 + fy), (30, 6 + fy)], 'gold', shade='edge')
    c.dots([(21, 10 + fy), (24, 10 + fy)], '#ffd86a')
    # twin scythes (rotate around the hands)
    for side, (hxx, col) in enumerate(((14, 'glass'), (34, 'demon'))):
        hand = (hxx, 22 + fy)
        d = -1 if side == 0 else 1
        pole = rot([(hand[0], hand[1] - 9), (hand[0], hand[1] + 9)], hand[0], hand[1], spin * d)
        c.line(pole, 'leather', width=1, shade='flat')
        top = pole[0]
        bl = rot([(top[0], top[1]), (top[0] - 9 * d, top[1] + 2), (top[0] - 6 * d, top[1] + 4), (top[0], top[1] + 2)], top[0], top[1], spin * d)
        c.poly(bl, col, shade='light')
        c.ellipse(hand[0], hand[1], 1.6, 1.6, 'pale', shade='flat')
    img = c.render()
    if a in (3, 4):
        from grimoire_art_v2 import arc_pts
        put(img, arc_pts(14, 22 + fy, 11, 200, 340, step=8, thick=1), '#bff8ff')
    return shadow_ellipse(img, 24, 45, 9, 1.4, alpha=70)


def the_sphinx_of_entropy(t=0, a=0):
    """ACT III BOSS (64x64). Riddle-keeper of Big-O. Three Death Seals hover above;
    each wrong answer lights one. Attack: wings flare, seals blaze, eye-beams."""
    c = Cv(64, 64)
    fy = [0, 0, 1, 1][t]
    flare = {0: 0, 1: 3, 2: 5, 3: 6, 4: 6, 5: 2}[a]
    # wings (behind)
    for d, base in ((1, 40),):
        c.poly([(base, 30 + fy), (base + 18, 10 - flare * 2 + fy), (base + 20, 18 - flare + fy), (base + 22, 26 + fy), (base + 14, 38 + fy)], 'crystal', shade='edge')
        for k in range(3):
            c.line([(base + 4, 32 + fy), (base + 16 + k * 2, 14 + k * 5 - flare + fy)], 'crystal', width=1, shade='dark')
    c.poly([(22, 30 + fy), (8 - flare, 14 - flare * 2 + fy), (6 - flare, 22 + fy), (10, 34 + fy)], 'crystal', shade='edge')
    # lion body lying down
    c.ellipse(38, 46 + fy, 18, 10, 'sand', shade='sphere')
    c.rrect(14, 48 + fy, 18, 8, 'sand', shade='edge')                          # front paws
    c.rrect(10, 54, 10, 5, 'sand', shade='edge'); c.rrect(22, 54, 10, 5, 'sand', shade='edge')
    c.dots([(11, 58), (13, 58), (15, 58), (23, 58), (25, 58), (27, 58)], ramp('sand', 0))
    c.poly([(54, 44), (60, 38 + fy), (62, 40 + fy), (56, 48)], 'sand', shade='edge')   # tail
    # chest + nemes headdress
    c.ellipse(22, 40 + fy, 9, 10, 'sand', shade='sphere')
    c.poly([(12, 18 + fy), (32, 18 + fy), (36, 38 + fy), (28, 34 + fy), (16, 34 + fy), (8, 38 + fy)], 'aegis', shade='edge')
    for k in range(4):
        c.line([(14 + k, 22 + k * 4 + fy), (30 - k, 22 + k * 4 + fy)], 'gold', width=1, shade='light')
    c.ellipse(21, 25 + fy, 6.5, 7.5, 'sand', shade='sphere')
    c.rect(19, 30 + fy, 3, 5, 'gold', shade='edge')                              # beard
    eye = '#7ff0d0' if a < 3 else '#ffffff'
    c.dots([(18, 24 + fy), (19, 24 + fy), (23, 24 + fy)], eye)
    c.dots([(18, 28 + fy), (19, 28 + fy), (20, 28 + fy)], ramp('sand', 0))
    c.poly([(16, 17 + fy), (21, 12 + fy), (26, 17 + fy)], 'gold', shade='edge')       # uraeus crest
    img = c.render()
    # three Death Seals above the crown
    for i, sx in enumerate((12, 21, 30)):
        lit = (a >= 2 and i <= a - 2) or a == 4
        col = '#ff4a3a' if lit else '#6e1424'
        sy = 4 + (1 if (t + i) % 2 else 0)
        put(img, [(sx + dx, sy + dy) for dx in range(-2, 3) for dy in range(-2, 3) if abs(dx) + abs(dy) <= 3], col)
        put(img, [(sx, sy)], '#ffe86a' if lit else '#b02a32')
    if a in (3, 4):   # eye-beam
        put(img, [(x, 24 + fy) for x in range(0, 17)] + [(x, 25 + fy) for x in range(0, 17, 2)], '#bffff0')
    return shadow_ellipse(img, 34, 59, 26, 1.8)


def entropy_chaos_king(t=0, a=0):
    """FINAL BOSS (64x64). Entropy, who cannot be destroyed, only sorted.
    Crown of shattered pointers, cloak that is half Glitch, orbiting scrambled tomes."""
    c = Cv(64, 64)
    fy = BOB[t] * 2
    raise_ = {0: 0, 1: 6, 2: 9, 3: 2, 4: -1, 5: 1}[a]
    # glitch cloak
    c.poly([(16, 22 + fy), (48, 22 + fy), (58, 60), (50, 57), (45, 61), (38, 58), (32, 62), (26, 58), (19, 61), (12, 57), (6, 60)], 'obsid', shade='edge')
    c.poly([(22, 24 + fy), (42, 24 + fy), (46, 58), (18, 58)], 'demon', shade='edge')
    c.poly([(26, 26 + fy), (38, 26 + fy), (39, 46 + fy), (25, 46 + fy)], 'gold', shade='edge')
    c.ellipse(32, 34 + fy, 4.5, 4.5, 'crystal', shade='sphere')                 # heart-core
    # shoulders
    c.ellipse(19, 24 + fy, 6, 4.5, 'gold', shade='sphere'); c.ellipse(45, 24 + fy, 6, 4.5, 'gold', shade='sphere')
    # head: cracked porcelain mask in a void hood
    c.ellipse(32, 14 + fy, 9, 9, 'obsid', shade='sphere')
    c.ellipse(31, 15 + fy, 6, 7, 'pale', shade='sphere')
    c.line([(32, 9 + fy), (30, 14 + fy), (33, 17 + fy), (31, 21 + fy)], 'ink', width=1, shade='light')  # crack
    c.dots([(28, 14 + fy), (29, 14 + fy), (34, 14 + fy)], '#ff3030')
    c.dots([(28, 18 + fy), (29, 19 + fy), (30, 19 + fy), (31, 19 + fy), (32, 18 + fy)], ramp('ink', 1))
    # crown of shattered pointers (arrows pointing in scrambled directions)
    for i, (px_, ang) in enumerate(((22, -30), (27, 10), (32, -5), (37, 25), (42, -20))):
        tip = rot([(px_, 0 + fy)], px_, 8 + fy, ang + (t * 6 if i % 2 else -t * 6))[0]
        c.poly([(px_ - 2, 8 + fy), tip, (px_ + 2, 8 + fy)], 'gold', shade='edge')
    # scepter arm (raises for the void burst)
    hand = (13, 30 + fy - raise_)
    c.line([(19, 26 + fy), hand], 'obsid', width=3, shade='edge_soft')
    c.line([(hand[0], hand[1] - 12), (hand[0], hand[1] + 10)], 'gold', width=2, shade='edge_soft')
    c.ellipse(hand[0], hand[1] - 14, 3.5, 3.5, 'crystal', shade='sphere')
    # orbiting scrambled tomes
    for i, mat in enumerate(('red', 'aegis', 'gold', 'tome')):
        ang = math.radians(t * 22 + i * 90)
        bx = 32 + 27 * math.cos(ang); by = 34 + fy + 10 * math.sin(ang)
        if math.sin(ang) < 0:
            c.rrect(int(bx) - 2, int(by) - 3, 5, 6, mat, shade='edge')
    img = c.render()
    # the near-side tomes drawn over the body
    for i, mat in enumerate(('red', 'aegis', 'gold', 'tome')):
        ang = math.radians(t * 22 + i * 90)
        bx = 32 + 27 * math.cos(ang); by = 34 + fy + 10 * math.sin(ang)
        if math.sin(ang) >= 0:
            sub = Cv(64, 64); sub.rrect(int(bx) - 2, int(by) - 3, 5, 6, mat, shade='edge'); img.alpha_composite(sub.render())
    # glitch tearing along the cloak edge
    rng = random.Random(t + a * 7)
    put(img, [(rng.randint(8, 56), rng.randint(40, 62)) for _ in range(10)], '#7ff0d0')
    put(img, [(rng.randint(8, 56), rng.randint(40, 62)) for _ in range(8)], '#ff4a6a')
    if a in (2, 3, 4):   # void orb charge / burst
        r = {2: 3, 3: 6, 4: 9}[a]
        cx, cy = hand[0], hand[1] - 14
        put(img, [(cx + r * math.cos(q / 8.0), cy + r * math.sin(q / 8.0)) for q in range(0, 50, 2)], '#d6a8ff')
    return shadow_ellipse(img, 32, 61, 24, 1.8)


# ---------------------------------------------------------------------------
MONSTERS = {
    'corrupted_slime': (corrupted_slime, 48),
    'lost_wisp': (lost_wisp, 48),
    'bookworm_horror': (bookworm_horror, 48),
    'twin_faced_librarian': (twin_faced_librarian, 48),
    'the_blind_archivist': (the_blind_archivist, 64),
    'skeleton_archer': (skeleton_archer, 48),
    'venomous_arachnid': (venomous_arachnid, 48),
    'grave_digger': (grave_digger, 48),
    'the_abyss_golem': (the_abyss_golem, 64),
    'hydra_of_division': (hydra_of_division, 48),
    'pivot_wraith': (pivot_wraith, 48),
    'the_sphinx_of_entropy': (the_sphinx_of_entropy, 64),
    'entropy_chaos_king': (entropy_chaos_king, 64),
}


import numpy as _np

BOSSES = ('the_blind_archivist', 'the_abyss_golem', 'the_sphinx_of_entropy', 'entropy_chaos_king', 'twin_faced_librarian')
# status-skill aura colour per monster (matches the status it inflicts)
CAST_COLOR = {
    'corrupted_slime': '#a8e272', 'lost_wisp': '#7ff0d0', 'bookworm_horror': '#e9dcb8',
    'twin_faced_librarian': '#d8dee8', 'the_blind_archivist': '#b8c8e0', 'skeleton_archer': '#7ff0d0',
    'venomous_arachnid': '#7ec45c', 'grave_digger': '#d8a860', 'the_abyss_golem': '#ff9a40',
    'hydra_of_division': '#8ae05a', 'pivot_wraith': '#e0a8ff', 'the_sphinx_of_entropy': '#ff4a3a',
    'entropy_chaos_king': '#d6a8ff',
}


def aura(img, col, width=1, alpha=255, pulse=0):
    """Coloured outline glow around the sprite silhouette (dilated alpha mask)."""
    a = _np.asarray(img)[..., 3] > 150   # ignore the soft ground shadow
    m = a.copy()
    for _ in range(width + pulse):
        n = m.copy()
        n[1:, :] |= m[:-1, :]; n[:-1, :] |= m[1:, :]; n[:, 1:] |= m[:, :-1]; n[:, :-1] |= m[:, 1:]
        m = n
    ring = m & ~a
    c = hx(col)
    out = _np.zeros((img.size[1], img.size[0], 4), dtype=_np.uint8)
    out[ring] = (c[0], c[1], c[2], alpha)
    base = Image.fromarray(out)
    base.alpha_composite(img)
    return base


def runes(img, col, k, n=6, r=None):
    w, h = img.size
    r = r or w * 0.38
    pts = []
    for i in range(n):
        ang = math.radians(i * (360 / n) + k * 25)
        pts.append((w / 2 + r * math.cos(ang), h * 0.55 + r * 0.45 * math.sin(ang) - k * 1.5))
    put(img, pts, col)
    put(img, [(x + 1, y) for (x, y) in pts[::2]], '#ffffff')
    return img


def monster_animations(mid):
    fn, size = MONSTERS[mid]
    raw = [fn(t, 0) for t in range(4)]
    # 6-frame ping-pong idle (0,1,2,3,2,1) for smoother breathing
    idle = [raw[0], raw[1], raw[2], raw[3], raw[2], raw[1]]
    atk = [fn(0, k) for k in (1, 2, 3, 4, 5)]
    base = raw[0]
    recoil = tint(offset(base, 3, 0), (255, 60, 60), 0.25)
    hurt = [flash(base, amt=0.9), recoil, offset(base, 1, 0)]
    seed = sum(map(ord, mid))
    death = [flash(base, amt=0.9), tint(offset(base, 2, 1), (200, 120, 255), 0.35)] +             [glitch_dissolve(base, k, seed=seed) for k in (0.25, 0.5, 0.75, 1.0)]
    # spawn: the Glitch assembles the monster (reverse dissolve) then a white pop
    spawn = [glitch_dissolve(base, k, seed=seed + 1) for k in (0.95, 0.7, 0.45, 0.2)] + [flash(base, amt=0.6), base]
    # cast: status skill — wind-up pose wrapped in a pulsing aura + orbiting runes
    col = CAST_COLOR.get(mid, '#d6a8ff')
    w1, w2 = fn(0, 1), fn(0, 2)
    cast = [aura(w1, col, 1), runes(aura(w2, col, 1, pulse=1), col, 0), runes(aura(w2, col, 2), col, 1),
            runes(aura(fn(1, 2), col, 1, pulse=1), col, 2), aura(raw[1], col, 1, alpha=140)]
    A = {
        'idle': (idle, [160, 140, 160, 160, 140, 160]),
        'attack': (atk, [120, 220, 60, 140, 180]),
        'hurt': (hurt, [60, 140, 120]),
        'death': (death, [70, 140, 110, 110, 120, 160]),
        'spawn': (spawn, [80, 80, 80, 80, 70, 120]),
        'cast': (cast, [120, 110, 110, 110, 160]),
    }
    if mid in BOSSES:
        # enrage (phase change): shake + red-hot aura growing; then a phase-2 idle loop
        enr = []
        for k in range(6):
            f = offset(raw[k % 4], [-2, 2, -1, 1, 0, 0][k], 0)
            f = tint(f, (255, 80, 40), [0.0, 0.15, 0.3, 0.4, 0.25, 0.1][k])
            enr.append(aura(f, '#ff4a2a', 1 + (k % 2), pulse=k // 3))
        A['enrage'] = (enr, [70, 70, 80, 90, 120, 160])
        p2 = [aura(f, '#ff5a2a' if i % 2 == 0 else '#ffb030', 1, alpha=210) for i, f in enumerate(idle)]
        A['idle2'] = (p2, [140, 120, 140, 140, 120, 140])
    return A


def all_previews():
    return {m: monster_animations(m)['idle'] for m in MONSTERS}


def export_all(roots, manifest, godot_sprites, export_anims, strip):
    import os
    for mid in MONSTERS:
        A = monster_animations(mid)
        export_anims(mid, A, roots, manifest)
        i0, i2 = A['idle'][0][0], A['idle'][0][2]
        for base in roots:
            os.makedirs(os.path.join(base, 'enemies'), exist_ok=True)
            i0.save(os.path.join(base, 'enemies', f'{mid}.png'))
            i2.save(os.path.join(base, 'enemies', f'{mid}_2.png'))
        if godot_sprites:
            os.makedirs(os.path.join(godot_sprites, 'monsters'), exist_ok=True)
            i0.save(os.path.join(godot_sprites, 'monsters', f'{mid}.png'))
            strip(A['idle'][0]).save(os.path.join(godot_sprites, 'monsters', f'{mid}_idle.png'))
