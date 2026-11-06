"""Grimoire's Aegis — pixel UI kit (9-slice panels, buttons, icons, status icons, font).

Same technique as Lost in Multiverse RPG's UI (hard-edged 9-slice frames, 1 px highlight
row, rivets, 5x7 pixel font) re-coloured for Grimoire: ink-navy panels with steel rims,
gold for focus/selection, and a clean parchment panel for questions (GDD §17: dark,
gritty frame but a high-contrast reading area).
All images are 1x; the game scales the whole 640x360 canvas by an integer factor.
"""
import os

from PIL import Image, ImageDraw

from build_grimoire_assets import hx


# ===========================================================================
# 9-SLICE PANELS & BUTTONS
# ===========================================================================
def panel(fill, rim, hi, dark, rivet, size=24, inner=None):
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
        px[x, y] = hx(rivet)
    if inner:
        for x in range(4, s - 3):
            px[x, 3] = hx(inner)
    return img


def parchment(size=32):
    """Question scroll: ink-stained dark frame, clean warm reading area."""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = size - 1
    d.rectangle([1, 0, s - 1, s], fill=hx('#1a0f08'))
    d.rectangle([0, 1, s, s - 1], fill=hx('#1a0f08'))
    d.rectangle([1, 1, s - 1, s - 1], fill=hx('#7a5226'))
    d.rectangle([2, 2, s - 2, s - 2], fill=hx('#3a2412'))
    d.rectangle([4, 4, s - 4, s - 4], fill=hx('#e9dcb8'))
    px = img.load()
    for x in range(2, s - 1):
        px[x, 1] = hx('#b88a44')
    # torn / burnt inner edge + ink stains only on the frame
    for (x, y) in ((4, 4), (s - 4, 4), (4, s - 4), (s - 4, s - 4), (9, 4), (s - 9, s - 4)):
        px[x, y] = hx('#c9b78e')
    for (x, y) in ((3, 9), (3, 10), (s - 3, 14), (12, s - 3), (13, s - 3), (s - 2, 6)):
        px[x, y] = hx('#140a06')
    for (x, y) in ((6, 2), (7, 2), (s - 8, 2)):
        px[x, y] = hx('#6e1424')          # dried ink-blood flecks on the frame
    return img


def button(state):
    pal = {
        'normal':   ('#1c2140', '#3c4a78', '#6f82b8', '#05060c'),
        'hover':    ('#2a2f52', '#d8a830', '#ffe27a', '#05060c'),
        'pressed':  ('#121530', '#8a6418', '#c8941e', '#05060c'),
        'disabled': ('#16171f', '#2a2c38', '#3a3c48', '#05060c'),
        'gold':     ('#3a2e12', '#d8a830', '#ffe27a', '#05060c'),
        'danger':   ('#3a1018', '#b02a32', '#e85a42', '#05060c'),
    }[state]
    fill, rim, hi, dark = pal
    img = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([1, 0, 14, 15], fill=hx(dark))
    d.rectangle([0, 1, 15, 14], fill=hx(dark))
    d.rectangle([1, 1, 14, 14], fill=hx(rim))
    d.rectangle([2, 2, 13, 13], fill=hx(fill))
    px = img.load()
    if state != 'pressed':
        for x in range(2, 14):
            px[x, 2] = hx(hi)
            px[x, 13] = hx(dark)
    else:
        for x in range(2, 14):
            px[x, 2] = hx(dark)
    return img


def slot(state):
    fill = {'normal': '#0c0e1a', 'hover': '#1c2140', 'equip': '#2a2410'}[state]
    rim = {'normal': '#2a3256', 'hover': '#d8a830', 'equip': '#dca424'}[state]
    img = Image.new('RGBA', (20, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 19, 19], fill=hx('#05060c'))
    d.rectangle([1, 1, 18, 18], fill=hx(rim))
    d.rectangle([2, 2, 17, 17], fill=hx(fill))
    px = img.load()
    for x in range(2, 18):
        px[x, 2] = hx('#05060c')
    for y in range(2, 18):
        px[2, y] = hx('#05060c')
    return img


def bar_frame():
    img = Image.new('RGBA', (8, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 7, 7], fill=hx('#05060c'))
    d.rectangle([1, 1, 6, 6], fill=hx('#151a2e'))
    return img


def tab(active):
    img = Image.new('RGBA', (16, 12), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rim, fill, hi = ('#d8a830', '#2a2f52', '#ffe27a') if active else ('#3c4a78', '#141830', '#5c6a98')
    d.rectangle([1, 0, 14, 11], fill=hx('#05060c'))
    d.rectangle([0, 1, 15, 11], fill=hx('#05060c'))
    d.rectangle([1, 1, 14, 11], fill=hx(rim))
    d.rectangle([2, 2, 13, 11], fill=hx(fill))
    for x in range(2, 14):
        img.putpixel((x, 2), hx(hi))
    return img


# ===========================================================================
# PIXEL ICON PATTERNS
# ===========================================================================
def pattern(rows, legend, size=None):
    rows = [r for r in rows.strip('\n').split('\n')]
    h = len(rows); w = max(len(r) for r in rows)
    W, H = size or (w, h)
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ox, oy = (W - w) // 2, (H - h) // 2
    for j, r in enumerate(rows):
        for i, ch in enumerate(r):
            if ch in legend:
                img.putpixel((ox + i, oy + j), hx(legend[ch]))
    return img


# 10x10 stat icons ----------------------------------------------------------
STAT_ICONS = {
    'hp': ("""
.rr..rr..
rRRrrRRr.
rRWRRRRr.
rRRRRRRr.
.rRRRRr..
..rRRr...
...rr....""", {'r': '#6e1424', 'R': '#e04848', 'W': '#ffc0b0'}),
    'mp': ("""
...b...
..bBb..
.bBWBb.
bBBWBBb
.bBBBb.
..bBb..
...b...""", {'b': '#1d2e70', 'B': '#4a7ad8', 'W': '#bfe0ff'}),
    'focus': ("""
....y....
....Y....
...yYy...
yyyYWYyyy
.yYYYYYy.
..yYYYy..
.yYy.yYy.
.y.....y.""", {'y': '#8a5a12', 'Y': '#ffd040', 'W': '#fff8c0'}),
    'atk': ("""
.......ww
......wWw
.....wWw.
.g..wWw..
..gwWw...
...gw....
..g.g....
.g.......""", {'w': '#5c6282', 'W': '#d8dee8', 'g': '#c8941e'}),
    'def': ("""
.ggggggg.
gGbbbbbGg
gGbBBBbGg
gGbBWBbGg
.gGbBbGg.
..gGbGg..
...gGg...
....g....""", {'g': '#8a5a12', 'G': '#dca424', 'b': '#1d4a9a', 'B': '#4a8ae0', 'W': '#cfe8ff'}),
    'int': ("""
pppp.pppp
pWWp.pWWp
pWkp.pkWp
pWWpppWWp
pWkWgWkWp
ppppgpppp
....g....""", {'p': '#46246a', 'W': '#e9dcb8', 'k': '#5a4a3a', 'g': '#dca424'}),
    'spd': ("""
...bbb...
...bBb...
...bBb...
..bbBb...
.bBBBbbb.
bBBBBBBBb
bbbbbbbbb""", {'b': '#4a2e1a', 'B': '#a8703a'}),
    'gold': ("""
..yyyy..
.yYYYYy.
yYWYYYYy
yYYyyYYy
yYYYYYYy
.yYYYYy.
..yyyy..""", {'y': '#8a5a12', 'Y': '#ffd040', 'W': '#fff8c0'}),
    'fragment': ("""
....b....
...bBb...
..bBWBb..
.bBBWBBb.
..bBBBb..
...bBb...
....b....""", {'b': '#14407a', 'B': '#4ac0ff', 'W': '#e0f8ff'}),
    'exp': ("""
...g...
..gGg..
ggGWGgg
.gGGGg.
.gG.Gg.
.g...g.""", {'g': '#3a6a2a', 'G': '#8ae05a', 'W': '#e0ffd0'}),
    'timer': ("""
ggggggg
.wWWWw.
..wSw..
...s...
..wSw..
.wSSSw.
ggggggg""", {'g': '#8a5a12', 'w': '#5a6a8a', 'W': '#cfe0f0', 'S': '#e8c060', 's': '#b08830'}),
    'skull': ("""
.wwwww.
wWWWWWw
wWkWkWw
wWWWWWw
.wWkWw.
.w.w.w.""", {'w': '#5a5262', 'W': '#e8e0d8', 'k': '#140c1c'}),
    'heart_full': ("""
.rr.rr.
rRRrRRr
rRRRRRr
.rRRRr.
..rRr..
...r...""", {'r': '#6e1424', 'R': '#e04848'}),
}

# 12x12 command icons --------------------------------------------------------
CMD_ICONS = {
    'cmd_attack': ("""
.........ww
........wWw
.......wWw.
......wWw..
.g...wWw...
..g.wWw....
...gWw.....
...gg......
..g..g.....
.g.........""", {'w': '#5c6282', 'W': '#e8eef8', 'g': '#dca424'}),
    'cmd_spell': ("""
..pppppppp.
.pWWWWpWWp.
.pWkkWpWkp.
.pWWWWpWWp.
.pWkkWpkWp.
.pWWWWpWWp.
.ppppppppp.
.....y.....
....yYy....
.....y.....""", {'p': '#46246a', 'W': '#e9dcb8', 'k': '#6a5a8a', 'y': '#8a5a12', 'Y': '#ffd86a'}),
    'cmd_item': ("""
....kk....
....ww....
...wRRw...
..wRRRRw..
..wRWRRw..
..wRRRRw..
..wRRRRw..
...wwww...""", {'k': '#7a5226', 'w': '#5a6a8a', 'R': '#e04848', 'W': '#ffc0b0'}),
    'cmd_analyze': ("""
...gggg....
..gWWWWg...
.gW.bb.Wg..
.gWbBBbWg..
.gW.bb.Wg..
..gWWWWg...
...ggggg...
.......gg..
........gg.""", {'g': '#8a5a12', 'W': '#cfe8ff', 'b': '#1d4a9a', 'B': '#4ac0ff'}),
    'cmd_flee': ("""
....bbb...
....bBb...
....bBb...
...bbBb...
..bBBBbbb.
.bBBBBBBBb
.bbbbbbbbb
w.w.w.....""", {'b': '#4a2e1a', 'B': '#a8703a', 'w': '#6f82b8'}),
    'cmd_hint': ("""
...yyyy...
..yYYYYy..
..yYWYYy..
..yYYYYy..
...yYYy...
....ww....
....ww....
.....w....""", {'y': '#8a5a12', 'Y': '#ffd86a', 'W': '#fffbe0', 'w': '#5c6282'}),
    'cmd_guide': ("""
.gggggggg.
.gRRRRRRg.
.gRyyyyRg.
.gRRRRRRg.
.gRyyyRRg.
.gRRRRRRg.
.gggggggg.""", {'g': '#8a5a12', 'R': '#6e1424', 'y': '#ffd86a'}),
    'cmd_settings': ("""
....ww....
.w.wWWw.w.
..wWWWWw..
.wWWkkWWw.
wWWk..kWWw
.wWWkkWWw.
..wWWWWw..
.w.wWWw.w.
....ww....""", {'w': '#3c4a78', 'W': '#9aa8d0', 'k': '#1c2140'}),
}

# 12x12 status icons: (pattern, legend, kind) -- kind 'debuff' | 'buff' --------
STATUS_ICONS = {
    'poison': ("""
...gg...
..gGGg..
..gGGg..
.gGGGGg.
.gGWGGg.
.gGGGGg.
..gggg..""", {'g': '#1d5226', 'G': '#7ec45c', 'W': '#e0ffd0'}, 'debuff'),
    'silence': ("""
.wwwwww.
wWWWWWWw
wWkWWkWw
wWWWWWWw
wwwRRwww
..RRRR..
.R....R.""", {'w': '#5c6282', 'W': '#d8dee8', 'k': '#140c1c', 'R': '#e04848'}, 'debuff'),
    'torn_page': ("""
WWWW.WW.
WkkW.kW.
WWW.WWW.
WkkW.kW.
WWW..WW.
WWWW.WW.""", {'W': '#e9dcb8', 'k': '#6a5a4a'}, 'debuff'),
    'blind': ("""
..wwww..
.wWWWWw.
wWWkkWWw
wWkkkkWw
.wWWWWw.
R.wwww.R
.R....R.""", {'w': '#5c6282', 'W': '#d8dee8', 'k': '#140c1c', 'R': '#e04848'}, 'debuff'),
    'buried': ("""
...ww...
..wWWw..
..wWWw..
bbbbbbbb
bBBbBBBb
bBbBBbBb
bbbbbbbb""", {'w': '#7a5226', 'W': '#d8a860', 'b': '#2a1a0c', 'B': '#6a4a2a'}, 'debuff'),
    'crushed': ("""
.ssssss.
sSSSSSSs
sSkSSkSs
.ssssss.
..b..b..
.bBbbBb.
bBBBBBBb""", {'s': '#3a3a4c', 'S': '#9090a8', 'k': '#141420', 'b': '#1d4a9a', 'B': '#4a8ae0'}, 'debuff'),
    'split': ("""
...ww.ww
..wWw.Ww
.wWw.Ww.
wWw.Ww..
Ww.Ww...
w.wW....""", {'w': '#7a2a8a', 'W': '#e0a8ff'}, 'debuff'),
    'death_seal': ("""
..rrrr..
.rRRRRr.
rRkRRkRr
rRRkkRRr
rRkRRkRr
.rRRRRr.
..rrrr..""", {'r': '#5c0e1c', 'R': '#e04848', 'k': '#ffe86a'}, 'debuff'),
    'panic': ("""
..pppp..
.pPPPPp.
pPkPPkPp
pPPPPPPp
pPkkkkPp
.pPPPPp.
..pppp..""", {'p': '#5c0e1c', 'P': '#ff7a6a', 'k': '#140c1c'}, 'debuff'),
    'fog': ("""
........
.ww..ww.
wWWwwWWw
WWWWWWWW
.wwWWww.
wWWWWWWw
........""", {'w': '#5a6a8a', 'W': '#b8c8e0'}, 'debuff'),
    'false_pivot': ("""
...yy...
...YY...
..yYYy..
..yYYy..
.yYYYYy.
yyyyyyyy
R......R""", {'y': '#8a5a12', 'Y': '#ffd040', 'R': '#e04848'}, 'debuff'),
    'ward': ("""
.bbbbbb.
bBBBBBBb
bBWWWWBb
bBWBBWBb
.bBWWBb.
..bBBb..
...bb...""", {'b': '#14407a', 'B': '#4ac0ff', 'W': '#e0f8ff'}, 'buff'),
    'ordered': ("""
......g.
....g.G.
..g.G.G.
g.G.G.G.
G.G.G.G.
G.G.G.G.
gggggggg""", {'g': '#1d5226', 'G': '#8ae05a'}, 'buff'),
    'regen': ("""
...gg...
...GG...
.gGGGGg.
.gGGGGg.
...GG...
...gg...""", {'g': '#1d5226', 'G': '#8ae05a'}, 'buff'),
    'enraged': ("""
r..rr..r
.rRRRRr.
rRkRRkRr
rRRRRRRr
.rRkkRr.
..rrrr..""", {'r': '#8a1406', 'R': '#ff6a30', 'k': '#140c1c'}, 'buff'),
    'adrenaline': ("""
....yy..
...yY...
..yYyy..
.yYYYy..
...yY...
..yY....
..y.....""", {'y': '#8a5a12', 'Y': '#ffe86a'}, 'buff'),
    'limit': ("""
...y....
..yYy...
yyYWYyy.
.yYYYy..
.yY.Yy..
.y...y..""", {'y': '#8a5a12', 'Y': '#ffd040', 'W': '#fff8c0'}, 'buff'),
    'marked': ("""
...rr...
..r..r..
.r.RR.r.
rrRRRRrr
.r.RR.r.
..r..r..
...rr...""", {'r': '#b02a32', 'R': '#ff8a6a'}, 'debuff'),
    'armored': ("""
.ssssss.
sSSSSSSs
sSkkkkSs
sSSSSSSs
sSkkkkSs
.sSSSSs.
..ssss..""", {'s': '#3a3a4c', 'S': '#9090a8', 'k': '#62627a'}, 'buff'),
}


def status_icon(rows, legend, kind):
    """12x12: dark rounded backdrop + coloured rim (red debuff / teal buff) + glyph."""
    img = Image.new('RGBA', (12, 12), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rim = '#b02a32' if kind == 'debuff' else '#2a9a8a'
    d.rectangle([1, 0, 10, 11], fill=hx('#05060c'))
    d.rectangle([0, 1, 11, 10], fill=hx('#05060c'))
    d.rectangle([1, 1, 10, 10], fill=hx(rim))
    d.rectangle([2, 2, 9, 9], fill=hx('#10121e'))
    g = pattern(rows, legend, size=(8, 8))
    img.alpha_composite(g, (2, 2))
    return img


def cursor_arrow():
    return pattern("""
y.....
yy....
yYy...
yYYy..
yYy...
yy....
y.....""", {'y': '#8a5a12', 'Y': '#ffe27a'})


def arrow_down():
    return pattern("""
yyyyyyy
.yYYYy.
..yYy..
...y...""", {'y': '#8a5a12', 'Y': '#ffe27a'})


# ===========================================================================
def export_all(roots, manifest=None):
    def save(rel, im):
        for base in roots:
            p = os.path.join(base, rel)
            os.makedirs(os.path.dirname(p), exist_ok=True)
            im.save(p)

    save('ui2/panel.png', panel('#121426', '#3c4a78', '#7f93c4', '#05060c', '#d8b45a', inner='#1a1e36'))
    save('ui2/panel_dark.png', panel('#0a0b14', '#262c48', '#3c4468', '#030308', '#5c6282'))
    save('ui2/panel_gold.png', panel('#1e1a12', '#d8a830', '#ffe27a', '#05060c', '#fff4b0', inner='#2a2418'))
    save('ui2/panel_red.png', panel('#1f0c14', '#b02a32', '#e85a42', '#05060c', '#ffb0a0', inner='#2a1018'))
    save('ui2/panel_teal.png', panel('#0c1a1c', '#2a9a8a', '#7ff0d0', '#05060c', '#bffff0', inner='#12262a'))
    save('ui2/parchment.png', parchment())
    for st in ('normal', 'hover', 'pressed', 'disabled', 'gold', 'danger'):
        save(f'ui2/btn_{st}.png', button(st))
    for st in ('normal', 'hover', 'equip'):
        save(f'ui2/slot_{st}.png', slot(st))
    save('ui2/bar_frame.png', bar_frame())
    save('ui2/tab_on.png', tab(True))
    save('ui2/tab_off.png', tab(False))
    save('ui2/cursor.png', cursor_arrow())
    save('ui2/arrow_down.png', arrow_down())
    for k, (rows, leg) in STAT_ICONS.items():
        save(f'icons/{k}.png', pattern(rows, leg, size=(10, 10)))
    for k, (rows, leg) in CMD_ICONS.items():
        save(f'icons/{k}.png', pattern(rows, leg, size=(12, 12)))
    for k, (rows, leg, kind) in STATUS_ICONS.items():
        save(f'status/{k}.png', status_icon(rows, leg, kind))

    # Pixel font (BMFont). Unity loads the descriptor as a TextAsset, so it is saved as .txt too.
    import pixel_font
    import shutil, tempfile
    tmp = tempfile.mkdtemp()
    pixel_font.build(tmp, 'pixel')
    for base in roots:
        os.makedirs(os.path.join(base, 'fonts'), exist_ok=True)
        shutil.copy(os.path.join(tmp, 'pixel.png'), os.path.join(base, 'fonts', 'pixel.png'))
        shutil.copy(os.path.join(tmp, 'pixel.fnt'), os.path.join(base, 'fonts', 'pixel_fnt.txt'))


def preview(out):
    imgs = []
    imgs += [panel('#121426', '#3c4a78', '#7f93c4', '#05060c', '#d8b45a', inner='#1a1e36'), parchment(),
             panel('#1e1a12', '#d8a830', '#ffe27a', '#05060c', '#fff4b0'), panel('#1f0c14', '#b02a32', '#e85a42', '#05060c', '#ffb0a0')]
    imgs += [button(s) for s in ('normal', 'hover', 'pressed', 'disabled', 'gold', 'danger')]
    imgs += [pattern(r, l, size=(10, 10)) for r, l in STAT_ICONS.values()]
    imgs += [pattern(r, l, size=(12, 12)) for r, l in CMD_ICONS.values()]
    imgs += [status_icon(r, l, k) for r, l, k in STATUS_ICONS.values()]
    S = 6
    W = sum(i.size[0] * S + 8 for i in imgs[:14]) + 10
    sheet = Image.new('RGBA', (900, 420), (40, 40, 52, 255))
    x, y = 8, 8
    rowh = 0
    for im in imgs:
        b = im.resize((im.size[0] * S, im.size[1] * S), Image.NEAREST)
        if x + b.size[0] > 890:
            x = 8; y += rowh + 8; rowh = 0
        sheet.alpha_composite(b, (x, y)); x += b.size[0] + 8; rowh = max(rowh, b.size[1])
    sheet.save(out)
