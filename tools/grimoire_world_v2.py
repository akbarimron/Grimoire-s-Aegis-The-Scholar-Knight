"""Grimoire's Aegis — adventure map art: road tiles, scenery props, the 3-key boss gate, key & puzzle icons.

Each Act has its own vocabulary (GDD §4 / §17):
  Act I   Sunken Archive  — plank floor road, bookshelves, book piles, candles, reading desks, globes
  Act II  Ossuary Keep    — flagstone road, coffins, bone piles, tombstones, candle clusters, skull pillars
  Act III Tower of Division — cracked crystal road, crystal clusters, broken pillars, rune stones, void cracks
"""
import math
import os
import random

from PIL import Image

from grimoire_art_v2 import Cv
from build_grimoire_assets import hx, ramp


def put(img, pts, col):
    p = img.load(); w, h = img.size
    c = hx(col) if isinstance(col, str) else col
    for (x, y) in pts:
        x, y = int(x), int(y)
        if 0 <= x < w and 0 <= y < h:
            p[x, y] = c


# ---------------------------------------------------------------- road tiles (32x32, seamless)
def road(act, v=0):
    rng = random.Random(act * 31 + v)
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    p = img.load()
    if act == 1:      # warm planks running horizontally
        base = ['#5a3519', '#6a4020', '#7a4a26']
        for y in range(32):
            plank = y // 8
            c = hx(base[(plank + v) % 3])
            for x in range(32):
                p[x, y] = c
            if y % 8 == 0:
                for x in range(32): p[x, y] = hx('#2e1a0c')
        for k in range(4):
            x = (k * 13 + v * 7) % 32
            for y in range(k * 8 + 1, k * 8 + 8): p[x, y] = hx('#3e2412')
        for _ in range(10):
            p[rng.randrange(32), rng.randrange(32)] = hx('#8c5c2c')
    elif act == 2:    # grey flagstones with mortar
        for y in range(32):
            for x in range(32): p[x, y] = hx('#3a3a4a')
        stones = [(0, 0, 15, 10), (16, 0, 15, 14), (0, 11, 9, 10), (10, 11, 6, 10), (16, 15, 15, 9), (0, 22, 13, 9), (14, 25, 17, 6)]
        for (sx, sy, sw, sh) in stones:
            shade = ['#4a4a5c', '#50506a', '#46465a'][(sx + sy + v) % 3]
            for y in range(sy, min(32, sy + sh)):
                for x in range(sx, min(32, sx + sw)):
                    p[x, y] = hx(shade)
            for x in range(sx, min(32, sx + sw)): p[x, sy] = hx('#64647c')
        for _ in range(8):
            p[rng.randrange(32), rng.randrange(32)] = hx('#3a3a4c')
    else:             # dark violet tiles with glowing cracks
        for y in range(32):
            for x in range(32):
                p[x, y] = hx('#2a1a44' if (x // 16 + y // 16 + v) % 2 else '#30204e')
        for x in range(32): p[x, 15] = hx('#1a0e2c'); p[x, 31] = hx('#1a0e2c')
        for y in range(32): p[15, y] = hx('#1a0e2c'); p[31, y] = hx('#1a0e2c')
        x, y = rng.randrange(4, 28), rng.randrange(4, 28)
        for _ in range(9):
            x = max(0, min(31, x + rng.choice((-1, 0, 1)))); y = max(0, min(31, y + rng.choice((-1, 1))))
            p[x, y] = hx('#b48aff')
    return img


# ---------------------------------------------------------------- props
def bookshelf():
    c = Cv(32, 48)
    c.rrect(2, 2, 28, 44, 'wood', shade='edge')
    for k in range(4):
        y = 6 + k * 10
        c.rect(5, y, 22, 8, 'obsid', shade='flat')
        x = 6
        rng = random.Random(k)
        while x < 26:
            w = rng.choice((2, 2, 3))
            col = rng.choice(('red', 'aegis', 'moss', 'tome', 'gold'))
            h = rng.randint(5, 8)
            c.rect(x, y + 8 - h, w, h, col, shade='edge_soft')
            x += w + (1 if rng.random() < 0.3 else 0)
        c.rect(4, y + 8, 24, 1, 'wood', shade='light')
    return c.render()


def book_pile():
    c = Cv(32, 32)
    for i, (col, w) in enumerate((('red', 18), ('aegis', 16), ('moss', 17), ('tome', 14))):
        c.rrect(8 - i % 2, 24 - i * 4, w, 4, col, shade='edge')
        c.rect(9 - i % 2, 25 - i * 4, w - 3, 1, 'paper', shade='flat')
    return c.render()


def candle_stand():
    c = Cv(32, 32)
    c.rect(15, 12, 2, 16, 'gold', shade='edge_soft')
    c.ellipse(16, 28, 6, 2, 'gold', shade='sphere')
    c.rect(13, 6, 6, 7, 'paper', shade='edge')
    img = c.render()
    put(img, [(16, 3), (16, 4), (15, 4), (16, 5)], '#ffd040')
    put(img, [(16, 2)], '#fff8c0')
    return img


def desk():
    c = Cv(32, 32)
    c.rect(4, 14, 24, 4, 'wood', shade='edge')
    c.rect(6, 18, 3, 10, 'wood', shade='dark'); c.rect(23, 18, 3, 10, 'wood', shade='dark')
    c.poly([(9, 13), (16, 11), (16, 14), (9, 15)], 'paper', shade='flat')
    c.poly([(16, 11), (23, 13), (23, 15), (16, 14)], 'paper', shade='light')
    c.rect(24, 8, 2, 6, 'paper', shade='flat')
    return c.render()


def globe():
    c = Cv(32, 32)
    c.rect(15, 20, 2, 7, 'gold', shade='flat'); c.ellipse(16, 27, 5, 2, 'gold', shade='sphere')
    c.ellipse(16, 14, 7, 7, 'aegis', shade='sphere')
    c.poly([(12, 10), (16, 9), (18, 12), (14, 15)], 'moss', shade='flat')
    c.line([(9, 8), (23, 20)], 'gold', width=1, shade='light')
    return c.render()


def coffin():
    c = Cv(32, 48)
    c.poly([(10, 4), (22, 4), (26, 14), (22, 44), (10, 44), (6, 14)], 'wood', shade='edge')
    for y in (14, 30):
        c.line([(8, y), (24, y)], 'rust', width=2, shade='dark')
    c.dots([(16, 22)], ramp('gold', 3))
    return c.render()


def bone_pile():
    c = Cv(32, 32)
    c.ellipse(16, 26, 12, 4, 'bone', shade='sphere')
    c.ellipse(13, 19, 5, 4.5, 'bone', shade='sphere')
    c.line([(18, 18), (27, 22)], 'bone', width=2, shade='light')
    c.line([(6, 22), (14, 25)], 'bone', width=2, shade='light')
    img = c.render()
    put(img, [(11, 19), (14, 19)], '#140c1c')
    return img


def tombstone():
    c = Cv(32, 32)
    c.poly([(9, 28), (9, 12), (12, 7), (20, 7), (23, 12), (23, 28)], 'stone', shade='edge')
    c.rect(13, 13, 6, 2, 'stone', shade='dark'); c.rect(15, 11, 2, 8, 'stone', shade='dark')
    c.dots([(10, 27), (12, 27), (21, 27)], ramp('moss', 3))
    return c.render()


def candle_cluster():
    c = Cv(32, 32)
    for (x, h) in ((9, 10), (14, 14), (19, 8), (23, 11)):
        c.rect(x, 28 - h, 3, h, 'bone', shade='edge_soft')
    img = c.render()
    for (x, h) in ((9, 10), (14, 14), (19, 8), (23, 11)):
        put(img, [(x + 1, 26 - h), (x + 1, 25 - h)], '#ffb030')
        put(img, [(x + 1, 24 - h)], '#fff47e')
    return img


def skull_pillar():
    c = Cv(32, 48)
    c.rrect(10, 10, 12, 34, 'stone', shade='edge')
    for y in (16, 26, 36):
        c.ellipse(16, y, 4, 3.5, 'bone', shade='sphere')
        c.dots([(14, y), (17, y)], '#140c1c')
    c.rrect(8, 6, 16, 5, 'stone', shade='edge'); c.rrect(8, 42, 16, 4, 'stone', shade='edge')
    return c.render()


def crystal_cluster():
    c = Cv(32, 48)
    for (x, top, w) in ((10, 14, 6), (16, 6, 7), (22, 18, 5)):
        c.poly([(x - w // 2, 44), (x, top), (x + w // 2 + 1, 44)], 'crystal', shade='edge')
    img = c.render()
    put(img, [(16, 9), (16, 10), (10, 17)], '#f0e0ff')
    return img


def broken_pillar():
    c = Cv(32, 48)
    c.rrect(9, 12, 14, 32, 'stone', shade='edge')
    c.poly([(9, 12), (13, 6), (16, 10), (19, 4), (23, 12)], 'stone', shade='edge')
    c.rrect(7, 42, 18, 4, 'stone', shade='edge')
    c.line([(12, 18), (14, 28)], 'stone', width=1, shade='dark')
    c.dots([(13, 40), (24, 45)], ramp('crystal', 3))
    return c.render()


def rune_stone():
    c = Cv(32, 32)
    c.ellipse(16, 18, 8, 9, 'obsid', shade='sphere')
    img = c.render()
    put(img, [(14, 14), (15, 15), (16, 16), (17, 15), (18, 14), (16, 18), (16, 20)], '#b48aff')
    return img


def void_crack():
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    rng = random.Random(5)
    x, y = 6, 16
    for _ in range(26):
        x = min(30, x + 1); y = max(4, min(28, y + rng.choice((-1, 0, 1))))
        put(img, [(x, y), (x, y + 1)], '#05040a')
        put(img, [(x, y - 1)], '#8a4ad8')
    return img


PROPS = {
    1: [('bookshelf', bookshelf), ('book_pile', book_pile), ('candle', candle_stand), ('desk', desk), ('globe', globe)],
    2: [('coffin', coffin), ('bones', bone_pile), ('tombstone', tombstone), ('candles', candle_cluster), ('skull_pillar', skull_pillar)],
    3: [('crystals', crystal_cluster), ('pillar', broken_pillar), ('rune_stone', rune_stone), ('void_crack', void_crack), ('crystals2', lambda: crystal_cluster().transpose(Image.FLIP_LEFT_RIGHT))],
}


# ---------------------------------------------------------------- gate (48x48): 0..3 keys lit, then open
def gate(keys, open_=False):
    c = Cv(48, 48)
    c.rrect(2, 4, 44, 42, 'stone', shade='edge')
    c.poly([(6, 46), (6, 16), (24, 6), (42, 16), (42, 46)], 'obsid' if open_ else 'rust', shade='edge')
    if not open_:
        c.rect(23, 14, 2, 32, 'rust', shade='dark')
        for k in range(3):
            y = 22 + k * 7
            c.rrect(19, y, 10, 5, 'gold' if k < keys else 'stone', shade='edge')
    img = c.render()
    if open_:
        put(img, [(x, y) for x in range(12, 37) for y in range(20, 46) if (x + y) % 5 == 0], '#3a2c58')
    else:
        for k in range(keys):
            put(img, [(23, 24 + k * 7), (24, 24 + k * 7)], '#fff47e')
    return img


def key_icon(k=0):
    """16x16 Index Key (4-frame spin glint)."""
    c = Cv(16, 16)
    c.ellipse(5, 6, 3.5, 3.5, 'gold', shade='sphere')
    c.rect(8, 5, 6, 2, 'gold', shade='edge_soft')
    c.rect(11, 7, 1, 2, 'gold', shade='flat'); c.rect(13, 7, 1, 3, 'gold', shade='flat')
    img = c.render()
    put(img, [(5, 6)], '#140c1c')
    if k % 4 == 0: put(img, [(4, 4)], '#ffffff')
    if k % 4 == 2: put(img, [(12, 5)], '#ffffff')
    return img


def puzzle_icon():
    """16x16 locked tome (no-timer puzzle node, GDD §4)."""
    c = Cv(16, 16)
    c.rrect(2, 3, 12, 11, 'tome', shade='edge')
    c.rect(3, 4, 2, 9, 'paper', shade='flat')
    c.rrect(8, 6, 5, 5, 'gold', shade='edge')
    img = c.render()
    put(img, [(10, 8)], '#140c1c')
    return img


def node_ring(col):
    """32x32 soft ring under special nodes."""
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    p = img.load()
    c = hx(col)
    for y in range(32):
        for x in range(32):
            d = math.hypot(x - 15.5, (y - 15.5) * 1.6)
            if 11 <= d <= 13:
                p[x, y] = (c[0], c[1], c[2], 200)
            elif d < 11:
                p[x, y] = (0, 0, 0, 70)
    return img


def export_all(roots, manifest=None):
    def save(rel, im):
        for base in roots:
            path = os.path.join(base, rel)
            os.makedirs(os.path.dirname(path), exist_ok=True)
            im.save(path)

    for act in (1, 2, 3):
        for v in range(3):
            save(f'world/road_s{act}_{v}.png', road(act, v))
        for i, (name, fn) in enumerate(PROPS[act]):
            save(f'world/prop_s{act}_{i}.png', fn())
    for k in range(4):
        save(f'world/gate_{k}.png', gate(k))
    save('world/gate_open.png', gate(3, True))
    strip = Image.new('RGBA', (64, 16), (0, 0, 0, 0))
    for k in range(4):
        strip.alpha_composite(key_icon(k), (k * 16, 0))
    save('icons/key.png', key_icon(0))
    save('icons/key_spin.png', strip)
    save('tiles/icon_puzzle.png', puzzle_icon())
    save('world/ring_gold.png', node_ring('#ffd86a'))
    save('world/ring_red.png', node_ring('#f05a50'))
    save('world/ring_blue.png', node_ring('#7fd8ff'))


def preview(out):
    imgs = []
    for act in (1, 2, 3):
        imgs += [road(act, 0), road(act, 1)] + [fn() for _, fn in PROPS[act]]
    imgs += [gate(k) for k in range(4)] + [gate(3, True), key_icon(), puzzle_icon()]
    S = 3
    sheet = Image.new('RGBA', (900, 640), (40, 40, 52, 255))
    x, y, rowh = 8, 8, 0
    for im in imgs:
        b = im.resize((im.size[0] * S, im.size[1] * S), Image.NEAREST)
        if x + b.size[0] > 890: x = 8; y += rowh + 8; rowh = 0
        sheet.alpha_composite(b, (x, y)); x += b.size[0] + 8; rowh = max(rowh, b.size[1])
    sheet.save(out)
