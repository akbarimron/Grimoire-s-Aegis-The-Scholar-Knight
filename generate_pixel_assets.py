"""
Generate Authentic 16-bit Pixel Art Sprites for Grimoire's Aegis: The Scholar Knight.
Adheres strictly to the Pixel Art Mastery guidelines:
- Strict limited palette with hue-shifting (cool shadows, warm highlights).
- Hard pixel edges, zero blurry anti-aliasing to background.
- Clean readability at 1x resolution (480x270 viewport target).
- Explicit pixel clustering and readable silhouettes.
"""

import os
from PIL import Image, ImageDraw

BASE_DIR = r"D:\Project\The-Scholar-Knight_EDU-GAME"

def create_knight_spritesheet():
    # 4 frames: Idle breathing (32x32 per frame -> 128x32)
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    
    # Palette:
    # Steel Armor: Highlight #c0cde3, Mid #7a889b, Dark #434c5b, Outline #1e2229
    # Blue Cloak: Highlight #3a88e9, Mid #2255aa, Dark #142e61, Shadow #0b1736
    # Gold/Bronze: Highlight #ffe478, Mid #c99e32, Dark #6b4d0e
    # Skin/Visor glow: #4deeea, #ffffff
    # Sword: #e8ecf8, #8f9db5, #3b4252
    # Grimoire: #661818, #942929, #ffd700
    
    C_OUTLINE = (30, 34, 41, 255)
    C_STEEL_HI = (192, 205, 227, 255)
    C_STEEL_MID = (122, 136, 155, 255)
    C_STEEL_DARK = (67, 76, 91, 255)
    C_CLOAK_HI = (58, 136, 233, 255)
    C_CLOAK_MID = (34, 85, 170, 255)
    C_CLOAK_DARK = (20, 46, 97, 255)
    C_GOLD = (201, 158, 50, 255)
    C_GOLD_HI = (255, 228, 120, 255)
    C_CYAN_GLOW = (77, 238, 234, 255)
    C_WHITE = (255, 255, 255, 255)
    C_BOOK_RED = (148, 41, 41, 255)
    C_BOOK_DARK = (80, 16, 16, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        
        # Bob offset for breathing cycle: 0, 1, 0, -1
        bob = 1 if frame in (1, 2) else 0
        
        # Cloak back layer
        for y in range(12 + bob, 27):
            w = 4 + (y - 12) // 3
            d.line([(8 - w // 2, y), (14, y)], fill=C_CLOAK_DARK)
            d.point((7 - w // 2, y), fill=C_OUTLINE)
        
        # Legs / Boots
        # Left leg
        d.rectangle([11, 24, 13, 28], fill=C_STEEL_MID)
        d.rectangle([11, 28, 14, 29], fill=C_STEEL_DARK)
        d.line([(10, 24), (10, 29)], fill=C_OUTLINE)
        d.line([(11, 30), (14, 30)], fill=C_OUTLINE)
        
        # Right leg
        d.rectangle([16, 24, 18, 28], fill=C_STEEL_MID)
        d.rectangle([16, 28, 19, 29], fill=C_STEEL_DARK)
        d.line([(19, 24), (19, 29)], fill=C_OUTLINE)
        d.line([(16, 30), (19, 30)], fill=C_OUTLINE)

        # Torso / Breastplate
        ty = 13 + bob
        d.rectangle([11, ty, 18, ty + 9], fill=C_STEEL_MID)
        # Highlight on chest
        d.rectangle([12, ty + 1, 15, ty + 4], fill=C_STEEL_HI)
        # Gold trim on cuirass
        d.line([(11, ty + 9), (18, ty + 9)], fill=C_GOLD)
        d.point((14, ty + 9), fill=C_GOLD_HI)
        # Belt
        d.line([(11, ty + 10), (18, ty + 10)], fill=C_OUTLINE)
        
        # Helmet
        hy = 5 + bob
        d.rectangle([11, hy, 18, hy + 7], fill=C_STEEL_MID)
        d.rectangle([12, hy + 1, 15, hy + 3], fill=C_STEEL_HI)
        # Helmet crest / plume
        d.rectangle([13, hy - 3, 16, hy], fill=C_CLOAK_HI)
        d.point((14, hy - 4), fill=C_CLOAK_HI)
        d.line([(12, hy - 3), (12, hy)], fill=C_OUTLINE)
        d.line([(17, hy - 3), (17, hy)], fill=C_OUTLINE)
        # Visor slit with cyan magic glow
        d.line([(13, hy + 4), (17, hy + 4)], fill=C_OUTLINE)
        d.line([(14, hy + 4), (16, hy + 4)], fill=C_CYAN_GLOW)
        d.point((15, hy + 4), fill=C_WHITE)
        # Helmet outline
        d.line([(10, hy + 1), (10, hy + 7)], fill=C_OUTLINE)
        d.line([(19, hy + 1), (19, hy + 7)], fill=C_OUTLINE)
        d.line([(11, hy), (18, hy)], fill=C_OUTLINE)

        # Left Arm holding Grimoire (front view left = screen right or left)
        # Left Hand holding glowing Grimoire at chest (x: 7..11, y: ty+3..ty+8)
        d.rectangle([6, ty + 2, 10, ty + 8], fill=C_BOOK_RED)
        d.rectangle([7, ty + 3, 9, ty + 7], fill=C_BOOK_DARK)
        # Gold Grimoire Emblem
        d.point((8, ty + 5), fill=C_GOLD_HI)
        d.point((8, ty + 4), fill=C_CYAN_GLOW)
        # Book pages
        d.line([(10, ty + 3), (10, ty + 7)], fill=C_STEEL_HI)
        # Arm pauldron
        d.rectangle([8, ty, 11, ty + 2], fill=C_STEEL_HI)

        # Right Hand holding Sword (pointing downwards)
        sy = ty + 3
        # Hand / Gauntlet
        d.rectangle([19, sy, 21, sy + 2], fill=C_STEEL_MID)
        # Sword Crossguard
        d.line([(18, sy + 3), (23, sy + 3)], fill=C_GOLD)
        # Sword Blade
        d.line([(20, sy + 4), (20, sy + 14)], fill=C_STEEL_HI)
        d.line([(21, sy + 4), (21, sy + 13)], fill=C_STEEL_MID)
        d.point((20, sy + 15), fill=C_WHITE) # Tip
        
        sheet.paste(img, (frame * width, 0))
        
    out_path = os.path.join(BASE_DIR, "assets", "sprites", "characters", "scholar_knight_idle.png")
    sheet.save(out_path)
    # Also save single static frame
    single = sheet.crop((0, 0, width, height))
    single.save(os.path.join(BASE_DIR, "assets", "sprites", "characters", "scholar_knight.png"))
    print(f"Saved Knight sprite to {out_path}")

def create_slime_spritesheet():
    # 4 frames: Corrupted Glitch Slime (32x32 per frame -> 128x32)
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))

    C_OUTLINE = (20, 10, 35, 255)
    C_SLIME_DARK = (45, 27, 78, 255)
    C_SLIME_MID = (90, 42, 145, 255)
    C_SLIME_HI = (150, 75, 220, 255)
    C_GLITCH_GREEN = (0, 255, 136, 255)
    C_GLITCH_HI = (180, 255, 220, 255)
    C_EYE_YELLOW = (255, 220, 0, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)

        # Squish cycle:
        # frame 0: normal, frame 1: squish, frame 2: stretch, frame 3: wobble
        sx = 0
        sy = 0
        if frame == 1:
            sx = 2; sy = -2
        elif frame == 2:
            sx = -1; sy = 2
        elif frame == 3:
            sx = 1; sy = -1

        bx = 7 - sx
        by = 13 - sy
        bw = 18 + (sx * 2)
        bh = 14 + sy

        # Slime body ellipse
        d.ellipse([bx, by, bx + bw, by + bh], fill=C_SLIME_MID, outline=C_OUTLINE)
        # Inner highlight
        d.ellipse([bx + 3, by + 2, bx + bw - 5, by + bh - 6], fill=C_SLIME_HI)
        
        # Glitch binary runes floating inside
        d.point((bx + 5, by + 5), fill=C_GLITCH_GREEN)
        d.point((bx + 6, by + 5), fill=C_GLITCH_HI)
        d.point((bx + 11 + frame, by + 4), fill=C_GLITCH_GREEN)
        d.point((bx + 8, by + 9), fill=C_GLITCH_GREEN)
        
        # Central corrupted single glowing eye
        ey = by + 5
        ex = bx + bw // 2
        d.ellipse([ex - 3, ey - 2, ex + 3, ey + 4], fill=C_OUTLINE)
        d.ellipse([ex - 2, ey - 1, ex + 2, ey + 3], fill=C_EYE_YELLOW)
        d.rectangle([ex, ey, ex + 1, ey + 2], fill=C_OUTLINE) # Slit pupil

        # Base puddle
        d.ellipse([bx - 1, by + bh - 3, bx + bw + 1, by + bh + 1], fill=C_SLIME_DARK)

        sheet.paste(img, (frame * width, 0))

    out_path = os.path.join(BASE_DIR, "assets", "sprites", "monsters", "corrupted_slime_idle.png")
    sheet.save(out_path)
    single = sheet.crop((0, 0, width, height))
    single.save(os.path.join(BASE_DIR, "assets", "sprites", "monsters", "corrupted_slime.png"))
    print(f"Saved Corrupted Slime sprite to {out_path}")

def create_skeleton_archer_spritesheet():
    # Skeleton Archer (32x32 per frame -> 128x32)
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))

    C_OUTLINE = (25, 20, 20, 255)
    C_BONE_HI = (240, 230, 210, 255)
    C_BONE_MID = (200, 185, 155, 255)
    C_BONE_DARK = (130, 115, 90, 255)
    C_LEATHER = (100, 65, 40, 255)
    C_RED_EYE = (255, 50, 60, 255)
    C_BOW_WOOD = (140, 90, 50, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)

        bob = 1 if frame in (1, 2) else 0

        # Ribcage & Spine
        ry = 12 + bob
        d.line([(16, ry), (16, ry + 10)], fill=C_BONE_DARK)
        # Ribs
        for rib_y in range(ry + 2, ry + 8, 2):
            d.line([(13, rib_y), (19, rib_y)], fill=C_BONE_MID)
        # Tattered leather tunic
        d.rectangle([13, ry + 4, 19, ry + 9], fill=C_LEATHER)

        # Skull
        sy = 4 + bob
        d.rectangle([13, sy, 19, sy + 7], fill=C_BONE_HI, outline=C_OUTLINE)
        # Eye sockets glowing crimson
        d.point((14, sy + 3), fill=C_RED_EYE)
        d.point((17, sy + 3), fill=C_RED_EYE)
        # Nasal cavity & Teeth
        d.point((16, sy + 5), fill=C_OUTLINE)
        d.line([(14, sy + 6), (18, sy + 6)], fill=C_BONE_DARK)

        # Pelvis & Legs
        d.line([(14, ry + 10), (18, ry + 10)], fill=C_BONE_MID)
        # Left leg
        d.line([(14, ry + 11), (13, 28)], fill=C_BONE_MID)
        d.line([(12, 29), (14, 29)], fill=C_BONE_DARK)
        # Right leg
        d.line([(18, ry + 11), (19, 28)], fill=C_BONE_MID)
        d.line([(18, 29), (20, 29)], fill=C_BONE_DARK)

        # Left Arm holding recurve bow
        d.line([(12, ry + 1), (8, ry + 5)], fill=C_BONE_HI)
        # Bow arc
        by = ry + 2
        d.arc([4, by - 6, 10, by + 12], 270, 90, fill=C_BOW_WOOD, width=2)
        # Bowstring
        d.line([(7, by - 5), (7, by + 11)], fill=C_BONE_HI)

        # Right Arm drawing arrow
        d.line([(19, ry + 1), (22, ry + 3)], fill=C_BONE_MID)
        d.line([(22, ry + 3), (12, ry + 3)], fill=C_BOW_WOOD) # Arrow shaft
        d.point((11, ry + 3), fill=C_BONE_HI) # Arrowhead

        sheet.paste(img, (frame * width, 0))

    out_path = os.path.join(BASE_DIR, "assets", "sprites", "monsters", "skeleton_archer_idle.png")
    sheet.save(out_path)
    single = sheet.crop((0, 0, width, height))
    single.save(os.path.join(BASE_DIR, "assets", "sprites", "monsters", "skeleton_archer.png"))
    print(f"Saved Skeleton Archer sprite to {out_path}")

def create_lost_wisp_spritesheet():
    # Lost Wisp (Lantern ghost) (32x32 per frame -> 128x32)
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))

    C_OUTLINE = (15, 25, 40, 255)
    C_BRASS_HI = (235, 195, 90, 255)
    C_BRASS_MID = (175, 125, 45, 255)
    C_BRASS_DARK = (95, 65, 20, 255)
    C_CYAN_GHOST = (0, 229, 255, 220)
    C_CYAN_CORE = (200, 248, 255, 255)
    C_CYAN_TRAIL = (0, 140, 200, 160)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)

        float_y = 1 if frame in (0, 2) else -1

        # Ghostly vapor aura
        cx, cy = 16, 15 + float_y
        d.ellipse([cx - 9, cy - 8, cx + 9, cy + 9], fill=C_CYAN_TRAIL)
        d.ellipse([cx - 6, cy - 5, cx + 6, cy + 6], fill=C_CYAN_GHOST)

        # Lantern cage
        lx, ly = cx - 5, cy - 6
        # Lantern roof
        d.polygon([(lx + 5, ly - 3), (lx, ly), (lx + 10, ly)], fill=C_BRASS_HI, outline=C_BRASS_DARK)
        # Ring on top
        d.ellipse([lx + 3, ly - 7, lx + 7, ly - 3], outline=C_BRASS_HI)
        # Glass frame
        d.rectangle([lx, ly, lx + 10, ly + 10], outline=C_BRASS_MID)
        # Glowing soul flame inside
        d.ellipse([lx + 3, ly + 2, lx + 7, ly + 8], fill=C_CYAN_CORE)
        # Lantern base
        d.rectangle([lx - 1, ly + 10, lx + 11, ly + 12], fill=C_BRASS_DARK)

        # Ghostly tendrils flowing down
        tendril_offset = (frame % 2) * 2
        d.line([(cx - 3, ly + 13), (cx - 4 + tendril_offset, ly + 20)], fill=C_CYAN_GHOST, width=2)
        d.line([(cx + 3, ly + 13), (cx + 2 - tendril_offset, ly + 19)], fill=C_CYAN_TRAIL, width=2)

        sheet.paste(img, (frame * width, 0))

    out_path = os.path.join(BASE_DIR, "assets", "sprites", "monsters", "lost_wisp_idle.png")
    sheet.save(out_path)
    single = sheet.crop((0, 0, width, height))
    single.save(os.path.join(BASE_DIR, "assets", "sprites", "monsters", "lost_wisp.png"))
    print(f"Saved Lost Wisp sprite to {out_path}")

def create_aegis_shield():
    # 48x48 Aegis Hexagonal Barrier Shield (GDD section 7: Hexagonal blue shield)
    size = 48
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    C_CORE = (255, 255, 255, 255)
    C_CYAN = (0, 229, 255, 240)
    C_BLUE = (0, 110, 220, 180)
    C_FAINT = (0, 60, 140, 80)

    # Hexagon coordinates centered at (24, 24), radius 20
    cx, cy, r = 24, 24, 20
    import math
    pts = []
    for i in range(6):
        angle = math.radians(60 * i + 30)
        pts.append((int(cx + r * math.cos(angle)), int(cy + r * math.sin(angle))))

    # Filled outer shield glow
    d.polygon(pts, fill=C_FAINT, outline=C_BLUE)

    # Inner hexagon
    inner_pts = []
    for i in range(6):
        angle = math.radians(60 * i + 30)
        inner_pts.append((int(cx + (r - 4) * math.cos(angle)), int(cy + (r - 4) * math.sin(angle))))
    d.polygon(inner_pts, outline=C_CYAN)

    # Central Aegis Emblem
    d.line([(cx - 8, cy), (cx + 8, cy)], fill=C_CORE, width=2)
    d.line([(cx, cy - 8), (cx, cy + 8)], fill=C_CORE, width=2)
    d.ellipse([cx - 4, cy - 4, cx + 4, cy + 4], fill=C_CORE)

    out_path = os.path.join(BASE_DIR, "assets", "sprites", "ui", "aegis_shield.png")
    img.save(out_path)
    print(f"Saved Aegis Shield to {out_path}")

def create_parchment_ui():
    # Parchment Box 9-slice texture (64x64) with dark medieval border & rivets
    size = 64
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    C_BORDER_DARK = (30, 24, 18, 255)
    C_BORDER_MID = (70, 52, 36, 255)
    C_PARCH_BASE = (228, 218, 180, 255)
    C_PARCH_DARK = (205, 192, 150, 255)
    C_GOLD_RIVET = (210, 165, 45, 255)

    # Parchment center
    d.rectangle([4, 4, size - 5, size - 5], fill=C_PARCH_BASE)
    # Subtle vintage shading around edges
    d.rectangle([5, 5, size - 6, size - 6], outline=C_PARCH_DARK)
    
    # Outer dark iron border
    d.rectangle([0, 0, size - 1, size - 1], outline=C_BORDER_DARK)
    d.rectangle([1, 1, size - 2, size - 2], outline=C_BORDER_MID)
    d.rectangle([2, 2, size - 3, size - 3], outline=C_BORDER_DARK)

    # Rivets at four corners
    corners = [(4, 4), (size - 6, 4), (4, size - 6), (size - 6, size - 6)]
    for rx, ry in corners:
        d.rectangle([rx, ry, rx + 1, ry + 1], fill=C_GOLD_RIVET)

    out_path = os.path.join(BASE_DIR, "assets", "sprites", "ui", "parchment_box.png")
    img.save(out_path)
    print(f"Saved Parchment UI to {out_path}")

def create_game_icon():
    # 64x64 Godot project icon featuring Scholar Knight helmet & Grimoire
    size = 64
    img = Image.new("RGBA", (size, size), (18, 20, 26, 255))
    d = ImageDraw.Draw(img)

    # Gold circular border
    d.ellipse([3, 3, size - 4, size - 4], outline=(201, 158, 50, 255), width=2)
    d.ellipse([5, 5, size - 6, size - 6], outline=(70, 50, 15, 255))

    # Helmet in center
    cx, cy = 32, 28
    d.rectangle([cx - 10, cy - 10, cx + 10, cy + 4], fill=(122, 136, 155, 255), outline=(30, 34, 41, 255))
    d.rectangle([cx - 8, cy - 8, cx - 2, cy - 3], fill=(192, 205, 227, 255))
    # Glowing visor slit
    d.rectangle([cx - 6, cy - 2, cx + 6, cy], fill=(0, 229, 255, 255))
    d.point((cx, cy - 1), fill=(255, 255, 255, 255))
    # Plume
    d.polygon([(cx - 4, cy - 11), (cx + 4, cy - 11), (cx + 1, cy - 20)], fill=(58, 136, 233, 255))

    # Grimoire book below
    by = 40
    d.rectangle([cx - 14, by, cx + 14, by + 12], fill=(148, 41, 41, 255), outline=(30, 10, 10, 255))
    d.line([(cx, by), (cx, by + 12)], fill=(201, 158, 50, 255), width=2)
    # Magic rune glow from book
    d.point((cx - 5, by + 5), fill=(0, 255, 180, 255))
    d.point((cx + 5, by + 5), fill=(0, 255, 180, 255))

    out_path = os.path.join(BASE_DIR, "icon.png")
    img.save(out_path)
    print(f"Saved Game Icon to {out_path}")

if __name__ == "__main__":
    create_knight_spritesheet()
    create_slime_spritesheet()
    create_skeleton_archer_spritesheet()
    create_lost_wisp_spritesheet()
    create_aegis_shield()
    create_parchment_ui()
    create_game_icon()
    print("All pixel art assets generated successfully!")
