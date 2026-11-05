"""
Generate Authentic 16-bit Pixel Art Sprites for Grimoire's Aegis: The Scholar Knight.
Adheres strictly to the Pixel Art Mastery guidelines:
- Strict limited palette with hue-shifting (cool shadows, warm highlights).
- Hard pixel edges, zero blurry anti-aliasing to background (strict 0-alpha background).
- Clean readability at 1x resolution (480x270 viewport target).
- Generates all 13 Bestiary Monsters (Prolog, Act I, Act II, Act III, Final),
  Scholar Knight protagonist, Aegis Hexagonal Shield, Parchment 9-slice, and Themed Array Objects.
"""

import os
import math
from PIL import Image, ImageDraw

BASE_DIR = r"D:\Project\The-Scholar-Knight_EDU-GAME"

def ensure_dirs():
    for sub in [
        os.path.join(BASE_DIR, "assets", "sprites", "characters"),
        os.path.join(BASE_DIR, "assets", "sprites", "monsters"),
        os.path.join(BASE_DIR, "assets", "sprites", "ui"),
    ]:
        os.makedirs(sub, exist_ok=True)

def save_sheet_and_single(sheet: Image.Image, folder: str, base_name: str, frame_w: int = 32, frame_h: int = 32):
    idle_path = os.path.join(BASE_DIR, "assets", "sprites", folder, f"{base_name}_idle.png")
    single_path = os.path.join(BASE_DIR, "assets", "sprites", folder, f"{base_name}.png")
    sheet.save(idle_path)
    single = sheet.crop((0, 0, frame_w, frame_h))
    single.save(single_path)
    print(f"Saved {base_name} (.png + _idle.png)")

def create_knight_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    
    C_OUTLINE = (30, 34, 41, 255)
    C_STEEL_HI = (192, 205, 227, 255)
    C_STEEL_MID = (122, 136, 155, 255)
    C_STEEL_DARK = (67, 76, 91, 255)
    C_CLOAK_HI = (58, 136, 233, 255)
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
        bob = 1 if frame in (1, 2) else 0
        
        for y in range(12 + bob, 27):
            w = 4 + (y - 12) // 3
            d.line([(8 - w // 2, y), (14, y)], fill=C_CLOAK_DARK)
            d.point((7 - w // 2, y), fill=C_OUTLINE)
        
        d.rectangle([11, 24, 13, 28], fill=C_STEEL_MID)
        d.rectangle([11, 28, 14, 29], fill=C_STEEL_DARK)
        d.line([(10, 24), (10, 29)], fill=C_OUTLINE)
        d.line([(11, 30), (14, 30)], fill=C_OUTLINE)
        
        d.rectangle([16, 24, 18, 28], fill=C_STEEL_MID)
        d.rectangle([16, 28, 19, 29], fill=C_STEEL_DARK)
        d.line([(19, 24), (19, 29)], fill=C_OUTLINE)
        d.line([(16, 30), (19, 30)], fill=C_OUTLINE)

        ty = 13 + bob
        d.rectangle([11, ty, 18, ty + 9], fill=C_STEEL_MID)
        d.rectangle([12, ty + 1, 15, ty + 4], fill=C_STEEL_HI)
        d.line([(11, ty + 9), (18, ty + 9)], fill=C_GOLD)
        d.point((14, ty + 9), fill=C_GOLD_HI)
        d.line([(11, ty + 10), (18, ty + 10)], fill=C_OUTLINE)
        
        hy = 5 + bob
        d.rectangle([11, hy, 18, hy + 7], fill=C_STEEL_MID)
        d.rectangle([12, hy + 1, 15, hy + 3], fill=C_STEEL_HI)
        d.rectangle([13, hy - 3, 16, hy], fill=C_CLOAK_HI)
        d.point((14, hy - 4), fill=C_CLOAK_HI)
        d.line([(12, hy - 3), (12, hy)], fill=C_OUTLINE)
        d.line([(17, hy - 3), (17, hy)], fill=C_OUTLINE)
        d.line([(13, hy + 4), (17, hy + 4)], fill=C_OUTLINE)
        d.line([(14, hy + 4), (16, hy + 4)], fill=C_CYAN_GLOW)
        d.point((15, hy + 4), fill=C_WHITE)
        d.line([(10, hy + 1), (10, hy + 7)], fill=C_OUTLINE)
        d.line([(19, hy + 1), (19, hy + 7)], fill=C_OUTLINE)
        d.line([(11, hy), (18, hy)], fill=C_OUTLINE)

        d.rectangle([6, ty + 2, 10, ty + 8], fill=C_BOOK_RED)
        d.rectangle([7, ty + 3, 9, ty + 7], fill=C_BOOK_DARK)
        d.point((8, ty + 5), fill=C_GOLD_HI)
        d.point((8, ty + 4), fill=C_CYAN_GLOW)
        d.line([(10, ty + 3), (10, ty + 7)], fill=C_STEEL_HI)
        d.rectangle([8, ty, 11, ty + 2], fill=C_STEEL_HI)

        sy = ty + 3
        d.rectangle([19, sy, 21, sy + 2], fill=C_STEEL_MID)
        d.line([(18, sy + 3), (23, sy + 3)], fill=C_GOLD)
        d.line([(20, sy + 4), (20, sy + 14)], fill=C_STEEL_HI)
        d.line([(21, sy + 4), (21, sy + 13)], fill=C_STEEL_MID)
        d.point((20, sy + 15), fill=C_WHITE)
        
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "characters", "scholar_knight")

def create_slime_spritesheet():
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
        sx = 2 if frame == 1 else (-1 if frame == 2 else (1 if frame == 3 else 0))
        sy = -2 if frame == 1 else (2 if frame == 2 else (-1 if frame == 3 else 0))
        bx, by = 7 - sx, 13 - sy
        bw, bh = 18 + (sx * 2), 14 + sy

        d.ellipse([bx, by, bx + bw, by + bh], fill=C_SLIME_MID, outline=C_OUTLINE)
        d.ellipse([bx + 3, by + 2, bx + bw - 5, by + bh - 6], fill=C_SLIME_HI)
        d.point((bx + 5, by + 5), fill=C_GLITCH_GREEN)
        d.point((bx + 6, by + 5), fill=C_GLITCH_HI)
        d.point((bx + 11 + frame, by + 4), fill=C_GLITCH_GREEN)
        d.point((bx + 8, by + 9), fill=C_GLITCH_GREEN)
        
        ey, ex = by + 5, bx + bw // 2
        d.ellipse([ex - 3, ey - 2, ex + 3, ey + 4], fill=C_OUTLINE)
        d.ellipse([ex - 2, ey - 1, ex + 2, ey + 3], fill=C_EYE_YELLOW)
        d.rectangle([ex, ey, ex + 1, ey + 2], fill=C_OUTLINE)
        d.ellipse([bx - 1, by + bh - 3, bx + bw + 1, by + bh + 1], fill=C_SLIME_DARK)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "corrupted_slime")

def create_lost_wisp_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
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
        cx, cy = 16, 15 + float_y
        d.ellipse([cx - 9, cy - 8, cx + 9, cy + 9], fill=C_CYAN_TRAIL)
        d.ellipse([cx - 6, cy - 5, cx + 6, cy + 6], fill=C_CYAN_GHOST)
        lx, ly = cx - 5, cy - 6
        d.polygon([(lx + 5, ly - 3), (lx, ly), (lx + 10, ly)], fill=C_BRASS_HI, outline=C_BRASS_DARK)
        d.ellipse([lx + 3, ly - 7, lx + 7, ly - 3], outline=C_BRASS_HI)
        d.rectangle([lx, ly, lx + 10, ly + 10], outline=C_BRASS_MID)
        d.ellipse([lx + 3, ly + 2, lx + 7, ly + 8], fill=C_CYAN_CORE)
        d.rectangle([lx - 1, ly + 10, lx + 11, ly + 12], fill=C_BRASS_DARK)
        tendril_offset = (frame % 2) * 2
        d.line([(cx - 3, ly + 13), (cx - 4 + tendril_offset, ly + 20)], fill=C_CYAN_GHOST, width=2)
        d.line([(cx + 3, ly + 13), (cx + 2 - tendril_offset, ly + 19)], fill=C_CYAN_TRAIL, width=2)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "lost_wisp")

def create_bookworm_horror_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (24, 14, 18, 255)
    C_WORM_DARK = (92, 42, 62, 255)
    C_WORM_MID = (148, 68, 92, 255)
    C_WORM_HI = (196, 108, 128, 255)
    C_PAGE = (232, 220, 185, 255)
    C_BOOK = (72, 46, 30, 255)
    C_TOOTH = (250, 245, 230, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        wiggle = (frame % 2) * 2 - 1
        # Corrupted open tome at bottom
        d.rectangle([5, 21, 27, 29], fill=C_BOOK, outline=C_OUTLINE)
        d.rectangle([7, 22, 25, 26], fill=C_PAGE)
        # Segmented worm bursting out
        for seg in range(4):
            sx = 16 + wiggle * (seg % 2)
            sy = 20 - seg * 4
            r = 5 - seg // 2
            d.ellipse([sx - r, sy - r, sx + r, sy + r], fill=C_WORM_MID, outline=C_OUTLINE)
            d.ellipse([sx - r + 1, sy - r + 1, sx, sy], fill=C_WORM_HI)
        # Maw with teeth
        hx, hy = 16 + wiggle, 6
        d.ellipse([hx - 5, hy - 4, hx + 5, hy + 4], fill=C_WORM_DARK, outline=C_OUTLINE)
        for tx in range(hx - 3, hx + 4, 2):
            d.point((tx, hy - 2), fill=C_TOOTH)
            d.point((tx, hy + 2), fill=C_TOOTH)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "bookworm_horror")

def create_twin_faced_librarian_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (18, 20, 32, 255)
    C_ROBE_LEFT = (45, 72, 130, 255)
    C_ROBE_RIGHT = (120, 38, 68, 255)
    C_MASK_HI = (235, 230, 215, 255)
    C_MASK_DARK = (95, 90, 105, 255)
    C_CYAN = (80, 230, 255, 255)
    C_RED = (255, 75, 90, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame in (1, 2) else 0
        # Split robe (Binary Search divide motif)
        d.polygon([(16, 10 + bob), (7, 29), (16, 29)], fill=C_ROBE_LEFT, outline=C_OUTLINE)
        d.polygon([(16, 10 + bob), (25, 29), (16, 29)], fill=C_ROBE_RIGHT, outline=C_OUTLINE)
        # Twin masks (left calm ivory, right dark shadow)
        hy = 5 + bob
        d.rectangle([10, hy, 16, hy + 7], fill=C_MASK_HI, outline=C_OUTLINE)
        d.rectangle([16, hy, 22, hy + 7], fill=C_MASK_DARK, outline=C_OUTLINE)
        d.point((13, hy + 3), fill=C_CYAN)
        d.point((19, hy + 3), fill=C_RED)
        # Floating torn book halves
        d.rectangle([3, 14 - bob, 8, 20 - bob], fill=C_MASK_HI, outline=C_OUTLINE)
        d.rectangle([24, 14 + bob, 29, 20 + bob], fill=C_MASK_HI, outline=C_OUTLINE)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "twin_faced_librarian")

def create_blind_archivist_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (15, 18, 28, 255)
    C_ROBE = (36, 48, 76, 255)
    C_GOLD = (218, 175, 62, 255)
    C_BANDAGE = (225, 220, 205, 255)
    C_LANTERN = (95, 240, 255, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame in (1, 3) else 0
        # Grand Archivist Boss Robes & Stole
        d.polygon([(16, 7 + bob), (5, 30), (27, 30)], fill=C_ROBE, outline=C_OUTLINE)
        d.line([(14, 11 + bob), (12, 29)], fill=C_GOLD, width=2)
        d.line([(18, 11 + bob), (20, 29)], fill=C_GOLD, width=2)
        # Hooded head with blindfold
        hy = 4 + bob
        d.ellipse([11, hy, 21, hy + 9], fill=C_ROBE, outline=C_OUTLINE)
        d.rectangle([12, hy + 3, 20, hy + 5], fill=C_BANDAGE)
        # Staff + Pointer Lantern on left, Floating Scrolls on right
        d.line([(6, 6), (6, 30)], fill=C_GOLD, width=2)
        d.ellipse([3, 7 + bob, 9, 13 + bob], fill=C_LANTERN, outline=C_OUTLINE)
        d.rectangle([23, 10 - bob, 28, 16 - bob], fill=C_BANDAGE, outline=C_GOLD)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "the_blind_archivist")

def create_skeleton_archer_spritesheet():
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
        ry = 12 + bob
        d.line([(16, ry), (16, ry + 10)], fill=C_BONE_DARK)
        for rib_y in range(ry + 2, ry + 8, 2):
            d.line([(13, rib_y), (19, rib_y)], fill=C_BONE_MID)
        d.rectangle([13, ry + 4, 19, ry + 9], fill=C_LEATHER)
        sy = 4 + bob
        d.rectangle([13, sy, 19, sy + 7], fill=C_BONE_HI, outline=C_OUTLINE)
        d.point((14, sy + 3), fill=C_RED_EYE)
        d.point((17, sy + 3), fill=C_RED_EYE)
        d.point((16, sy + 5), fill=C_OUTLINE)
        d.line([(14, sy + 6), (18, sy + 6)], fill=C_BONE_DARK)
        d.line([(14, ry + 10), (18, ry + 10)], fill=C_BONE_MID)
        d.line([(14, ry + 11), (13, 28)], fill=C_BONE_MID)
        d.line([(12, 29), (14, 29)], fill=C_BONE_DARK)
        d.line([(18, ry + 11), (19, 28)], fill=C_BONE_MID)
        d.line([(18, 29), (20, 29)], fill=C_BONE_DARK)
        d.line([(12, ry + 1), (8, ry + 5)], fill=C_BONE_HI)
        by = ry + 2
        d.arc([4, by - 6, 10, by + 12], 270, 90, fill=C_BOW_WOOD, width=2)
        d.line([(7, by - 5), (7, by + 11)], fill=C_BONE_HI)
        d.line([(19, ry + 1), (22, ry + 3)], fill=C_BONE_MID)
        d.line([(22, ry + 3), (12, ry + 3)], fill=C_BOW_WOOD)
        d.point((11, ry + 3), fill=C_BONE_HI)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "skeleton_archer")

def create_venomous_arachnid_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (15, 22, 15, 255)
    C_CHITIN_DARK = (32, 48, 36, 255)
    C_CHITIN_MID = (52, 82, 56, 255)
    C_VENOM = (130, 255, 65, 255)
    C_EYE = (255, 60, 60, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame % 2 == 1 else 0
        # 8 Spider legs
        for i, ly in enumerate([14, 17, 20, 23]):
            off = (frame + i) % 2
            d.line([(11, 18 + bob), (3, ly + off)], fill=C_CHITIN_MID, width=1)
            d.line([(21, 18 + bob), (29, ly + off)], fill=C_CHITIN_MID, width=1)
        # Abdomen + Thorax
        d.ellipse([9, 10 + bob, 23, 22 + bob], fill=C_CHITIN_DARK, outline=C_OUTLINE)
        d.ellipse([13, 13 + bob, 19, 17 + bob], fill=C_VENOM)
        d.ellipse([11, 18 + bob, 21, 26 + bob], fill=C_CHITIN_MID, outline=C_OUTLINE)
        # Eyes & Fangs
        for ex in [13, 15, 17, 19]:
            d.point((ex, 21 + bob), fill=C_EYE)
        d.line([(14, 25 + bob), (14, 28 + bob)], fill=C_VENOM)
        d.line([(18, 25 + bob), (18, 28 + bob)], fill=C_VENOM)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "venomous_arachnid")

def create_grave_digger_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (20, 18, 16, 255)
    C_COAT = (62, 54, 46, 255)
    C_WOOD = (120, 78, 42, 255)
    C_STEEL = (170, 182, 195, 255)
    C_LANTERN = (255, 205, 85, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame in (1, 2) else 0
        # Hunched gravedigger cloak + hat
        d.polygon([(15, 9 + bob), (7, 29), (23, 29)], fill=C_COAT, outline=C_OUTLINE)
        d.polygon([(9, 8 + bob), (21, 8 + bob), (15, 3 + bob)], fill=C_OUTLINE)
        d.point((14, 10 + bob), fill=C_LANTERN)
        # Coffin on back + Iron Shovel in hand
        d.rectangle([6, 12 + bob, 11, 24 + bob], fill=C_WOOD, outline=C_OUTLINE)
        d.line([(23, 7 + bob), (23, 24 + bob)], fill=C_WOOD, width=2)
        d.polygon([(20, 24 + bob), (26, 24 + bob), (23, 29 + bob)], fill=C_STEEL, outline=C_OUTLINE)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "grave_digger")

def create_abyss_golem_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (22, 20, 26, 255)
    C_STONE_DARK = (72, 68, 78, 255)
    C_STONE_MID = (115, 110, 122, 255)
    C_STONE_HI = (162, 156, 168, 255)
    C_RUNE = (255, 190, 50, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame in (1, 2) else 0
        # Massive stacked stone slabs (Sorting blocks motif)
        d.rectangle([7, 8 + bob, 25, 24 + bob], fill=C_STONE_MID, outline=C_OUTLINE)
        d.rectangle([9, 10 + bob, 23, 14 + bob], fill=C_STONE_HI)
        # Glowing sorting runes on chest
        d.line([(12, 16 + bob), (20, 16 + bob)], fill=C_RUNE, width=2)
        d.line([(14, 19 + bob), (18, 19 + bob)], fill=C_RUNE, width=2)
        # Golem head & fists
        d.rectangle([12, 3 + bob, 20, 9 + bob], fill=C_STONE_HI, outline=C_OUTLINE)
        d.point((14, 6 + bob), fill=C_RUNE)
        d.point((18, 6 + bob), fill=C_RUNE)
        d.rectangle([2, 12 - bob, 7, 22 - bob], fill=C_STONE_DARK, outline=C_OUTLINE)
        d.rectangle([25, 12 + bob, 30, 22 + bob], fill=C_STONE_DARK, outline=C_OUTLINE)
        d.rectangle([9, 24, 14, 30], fill=C_STONE_DARK, outline=C_OUTLINE)
        d.rectangle([18, 24, 23, 30], fill=C_STONE_DARK, outline=C_OUTLINE)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "the_abyss_golem")

def create_hydra_of_division_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (22, 12, 35, 255)
    C_SCALE_MID = (105, 45, 160, 255)
    C_SCALE_HI = (165, 85, 225, 255)
    C_CYAN = (80, 245, 255, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame % 2 == 1 else 0
        # Body base
        d.ellipse([8, 18, 24, 30], fill=C_SCALE_MID, outline=C_OUTLINE)
        # 3 Dividing necks & heads (Divide & Conquer motif)
        heads = [(7, 7 + bob), (16, 4 - bob), (25, 7 + bob)]
        for hx, hy in heads:
            d.line([(16, 20), (hx, hy + 3)], fill=C_SCALE_MID, width=3)
            d.ellipse([hx - 4, hy - 3, hx + 4, hy + 3], fill=C_SCALE_HI, outline=C_OUTLINE)
            d.point((hx - 1, hy), fill=C_CYAN)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "hydra_of_division")

def create_pivot_wraith_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (20, 12, 30, 255)
    C_WRAITH = (78, 36, 118, 230)
    C_GOLD_PIVOT = (255, 215, 70, 255)
    C_LEFT_CYAN = (70, 210, 255, 255)
    C_RIGHT_RED = (255, 80, 110, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame in (1, 2) else -1
        # Central golden Pivot axis blade
        d.line([(16, 3), (16, 29)], fill=C_GOLD_PIVOT, width=2)
        d.polygon([(16, 6 + bob), (8, 26), (24, 26)], fill=C_WRAITH, outline=C_OUTLINE)
        # Left (< pivot) cyan orb, Right (> pivot) crimson orb
        d.ellipse([3, 14 + bob, 8, 19 + bob], fill=C_LEFT_CYAN, outline=C_OUTLINE)
        d.ellipse([24, 14 - bob, 29, 19 - bob], fill=C_RIGHT_RED, outline=C_OUTLINE)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "pivot_wraith")

def create_sphinx_of_entropy_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (24, 16, 32, 255)
    C_GOLD = (225, 180, 65, 255)
    C_LAPIS = (42, 68, 142, 255)
    C_SEAL_RED = (255, 55, 75, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame in (1, 2) else 0
        # Winged Sphinx body + Pharaoh nemes headdress
        d.rectangle([7, 16 + bob, 25, 28], fill=C_GOLD, outline=C_OUTLINE)
        d.polygon([(10, 6 + bob), (22, 6 + bob), (24, 17 + bob), (8, 17 + bob)], fill=C_LAPIS, outline=C_OUTLINE)
        d.rectangle([12, 8 + bob, 20, 15 + bob], fill=C_GOLD, outline=C_OUTLINE)
        # 3 Floating Death Seals above head
        for sx in [8, 16, 24]:
            d.ellipse([sx - 2, 1 + bob, sx + 2, 5 + bob], fill=C_SEAL_RED, outline=C_OUTLINE)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "the_sphinx_of_entropy")

def create_entropy_chaos_king_spritesheet():
    width, height = 32, 32
    sheet = Image.new("RGBA", (width * 4, height), (0, 0, 0, 0))
    C_OUTLINE = (12, 8, 20, 255)
    C_VOID = (38, 18, 62, 255)
    C_CRIMSON = (230, 45, 75, 255)
    C_GLITCH_CYAN = (0, 245, 255, 255)
    C_CROWN = (255, 215, 75, 255)

    for frame in range(4):
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = 1 if frame % 2 == 0 else -1
        # Armored Demon King + Glitching Crown + Broken Grand Catalog pages
        d.polygon([(16, 7 + bob), (4, 29), (28, 29)], fill=C_VOID, outline=C_OUTLINE)
        d.line([(8, 12 + bob), (24, 12 + bob)], fill=C_CRIMSON, width=2)
        d.polygon([(11, 6 + bob), (13, 2 + bob), (16, 5 + bob), (19, 2 + bob), (21, 6 + bob)], fill=C_CROWN, outline=C_OUTLINE)
        d.point((14, 9 + bob), fill=C_GLITCH_CYAN)
        d.point((18, 9 + bob), fill=C_CRIMSON)
        # Orbiting corrupted index fragments
        d.rectangle([2, 8 - bob, 5, 11 - bob], fill=C_GLITCH_CYAN)
        d.rectangle([26, 15 + bob, 29, 18 + bob], fill=C_CRIMSON)
        sheet.paste(img, (frame * width, 0))
    save_sheet_and_single(sheet, "monsters", "entropy_chaos_king")

def create_aegis_shield():
    size = 48
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    C_CORE = (255, 255, 255, 255)
    C_CYAN = (0, 229, 255, 240)
    C_BLUE = (0, 110, 220, 180)
    C_FAINT = (0, 60, 140, 80)

    cx, cy, r = 24, 24, 20
    pts = [(int(cx + r * math.cos(math.radians(60 * i + 30))), int(cy + r * math.sin(math.radians(60 * i + 30)))) for i in range(6)]
    d.polygon(pts, fill=C_FAINT, outline=C_BLUE)
    inner_pts = [(int(cx + (r - 4) * math.cos(math.radians(60 * i + 30))), int(cy + (r - 4) * math.sin(math.radians(60 * i + 30)))) for i in range(6)]
    d.polygon(inner_pts, outline=C_CYAN)
    d.line([(cx - 8, cy), (cx + 8, cy)], fill=C_CORE, width=2)
    d.line([(cx, cy - 8), (cx, cy + 8)], fill=C_CORE, width=2)
    d.ellipse([cx - 4, cy - 4, cx + 4, cy + 4], fill=C_CORE)
    img.save(os.path.join(BASE_DIR, "assets", "sprites", "ui", "aegis_shield.png"))

def create_parchment_ui():
    size = 64
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    C_BORDER_DARK = (30, 24, 18, 255)
    C_BORDER_MID = (70, 52, 36, 255)
    C_PARCH_BASE = (228, 218, 180, 255)
    C_PARCH_DARK = (205, 192, 150, 255)
    C_GOLD_RIVET = (210, 165, 45, 255)

    d.rectangle([4, 4, size - 5, size - 5], fill=C_PARCH_BASE)
    d.rectangle([5, 5, size - 6, size - 6], outline=C_PARCH_DARK)
    d.rectangle([0, 0, size - 1, size - 1], outline=C_BORDER_DARK)
    d.rectangle([1, 1, size - 2, size - 2], outline=C_BORDER_MID)
    d.rectangle([2, 2, size - 3, size - 3], outline=C_BORDER_DARK)
    for rx, ry in [(4, 4), (size - 6, 4), (4, size - 6), (size - 6, size - 6)]:
        d.rectangle([rx, ry, rx + 1, ry + 1], fill=C_GOLD_RIVET)
    img.save(os.path.join(BASE_DIR, "assets", "sprites", "ui", "parchment_box.png"))

def create_game_icon():
    size = 64
    img = Image.new("RGBA", (size, size), (18, 20, 26, 255))
    d = ImageDraw.Draw(img)
    d.ellipse([3, 3, size - 4, size - 4], outline=(201, 158, 50, 255), width=2)
    d.ellipse([5, 5, size - 6, size - 6], outline=(70, 50, 15, 255))
    cx, cy = 32, 28
    d.rectangle([cx - 10, cy - 10, cx + 10, cy + 4], fill=(122, 136, 155, 255), outline=(30, 34, 41, 255))
    d.rectangle([cx - 8, cy - 8, cx - 2, cy - 3], fill=(192, 205, 227, 255))
    d.rectangle([cx - 6, cy - 2, cx + 6, cy], fill=(0, 229, 255, 255))
    d.point((cx, cy - 1), fill=(255, 255, 255, 255))
    d.polygon([(cx - 4, cy - 11), (cx + 4, cy - 11), (cx + 1, cy - 20)], fill=(58, 136, 233, 255))
    by = 40
    d.rectangle([cx - 14, by, cx + 14, by + 12], fill=(148, 41, 41, 255), outline=(30, 10, 10, 255))
    d.line([(cx, by), (cx, by + 12)], fill=(201, 158, 50, 255), width=2)
    d.point((cx - 5, by + 5), fill=(0, 255, 180, 255))
    d.point((cx + 5, by + 5), fill=(0, 255, 180, 255))
    img.save(os.path.join(BASE_DIR, "icon.png"))

if __name__ == "__main__":
    ensure_dirs()
    create_knight_spritesheet()
    create_slime_spritesheet()
    create_lost_wisp_spritesheet()
    create_bookworm_horror_spritesheet()
    create_twin_faced_librarian_spritesheet()
    create_blind_archivist_spritesheet()
    create_skeleton_archer_spritesheet()
    create_venomous_arachnid_spritesheet()
    create_grave_digger_spritesheet()
    create_abyss_golem_spritesheet()
    create_hydra_of_division_spritesheet()
    create_pivot_wraith_spritesheet()
    create_sphinx_of_entropy_spritesheet()
    create_entropy_chaos_king_spritesheet()
    create_aegis_shield()
    create_parchment_ui()
    create_game_icon()
    print("All 13 monster sprites, knight sprites, and UI assets generated successfully!")
