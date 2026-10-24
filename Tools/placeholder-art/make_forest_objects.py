"""Props, objetos interactivos, personajes provisorios y partículas del Bosque.

Estáticos (hojas PNG con .sprites.json, pivote abajo al centro):
  Assets/Art/Environment/Props/forest_props.png     lápidas, cruz, mausoleo, árbol muerto,
                                                    cabaña, tronco, roca fundida, plumas
  Assets/Art/Environment/Props/common_objects.png   inscripción, relicario, puerta de jefe,
                                                    palanca, plataforma frágil, saliente de luz
  Assets/Art/Characters/Placeholders/forest_cast.png Sarah, Hueco, Cuervo, Fuego fatuo, Azazel
  Assets/Art/VFX/vfx_forest.png                     ceniza, brasas, fuegos fatuos, humo, polvo,
                                                    chispas, destello de parry
Animados (.aseprite, un tag por animación; Unity crea un clip por tag):
  Assets/Art/Environment/Props/altar.aseprite      idle (loop) · activo (loop)
  Assets/Art/Environment/Props/spirit_lantern.aseprite  loop
  Assets/Art/Environment/Props/spirit_veil.aseprite     loop (velo de 16×64)
  Assets/Art/Environment/Props/grace_fragment.aseprite  loop
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from forest_palette import *  # noqa: F401,F403
from aseprite_writer import write_aseprite

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Art")


def glow(img, cx, cy, accent, radius=2):
    """Glow del GDD: centro casi blanco, anillo 1 al 100 %, anillo 2 con dithering."""
    for y in range(cy - radius - 1, cy + radius + 2):
        for x in range(cx - radius - 1, cx + radius + 2):
            d = max(abs(x - cx), abs(y - cy))
            if d == radius + 1 and (x + y) % 2 == 0:
                put(img, x, y, accent)
            elif d <= radius and d > 0:
                put(img, x, y, accent)
    put(img, cx, cy, WHITE)


# ------------------------------------------------------------------ props estáticos

def gravestone(tall):
    h = 32 if tall else 24
    img = new(16, h)
    rect(img, 2, 4, 13, h - 1, ROCK)
    rect(img, 4, 2, 11, 3, ROCK)
    rect(img, 3, 3, 12, 3, ROCK)
    rect(img, 2, 4, 2, h - 1, ROCK_HI)
    rect(img, 13, 4, 13, h - 1, ROCK_SH)
    rect(img, 7, 7, 8, 14, ROCK_SH)
    rect(img, 5, 9, 10, 10, ROCK_SH)
    rect(img, 2, h - 5, 13, h - 1, GRASS_DK)
    for x in range(2, 14, 3):
        put(img, x, h - 6, GRASS)
    return outline(img)


def stone_cross():
    img = new(16, 32)
    rect(img, 6, 2, 9, 31, ROCK)
    rect(img, 1, 8, 14, 11, ROCK)
    rect(img, 6, 2, 6, 31, ROCK_HI)
    rect(img, 1, 8, 14, 8, ROCK_HI)
    rect(img, 9, 12, 9, 31, ROCK_SH)
    rect(img, 11, 9, 13, 9, GRASS)  # musgo
    return outline(img)


def mausoleum():
    w, h = 96, 96
    img = new(w, h)
    rect(img, 8, 36, 87, 95, ROCK)
    for i in range(22):
        rect(img, 4 + i * 2, 36 - i, 91 - i * 2, 36 - i, ROCK_SH if i % 4 == 0 else ROCK)
    rect(img, 8, 36, 8, 95, ROCK_HI)
    rect(img, 87, 36, 87, 95, ROCK_SH)
    for x in (16, 30, 66, 80):  # columnas
        rect(img, x, 40, x + 5, 95, ROCK_HI)
        rect(img, x + 5, 40, x + 5, 95, ROCK_SH)
    rect(img, 38, 52, 57, 95, OUT)      # puerta abierta, oscura
    rect(img, 38, 48, 57, 51, ROCK_SH)
    glow(img, 47, 22, SPIRIT, 1)        # sello de Azazel en el frontón
    for x in range(10, 86, 7):          # niebla verde al pie
        rect(img, x, 90, x + 3, 95, SPIRIT_DK)
    return outline(img)


def dead_tree():
    w, h = 128, 160
    img = new(w, h)
    cx = 64
    for y in range(40, h):
        half = 6 + max(0, (y - 120) // 3)
        rect(img, cx - half, y, cx + half, y, MID_TRUNK)
        put(img, cx - half, y, MID_TRUNK_HI)
        put(img, cx + half, y, MID_TRUNK_DK)
    for (sy, d, n) in ((44, -1, 50), (60, 1, 56), (90, -1, 40), (40, 1, 36)):
        x, y = cx, sy
        for i in range(n):
            x += d
            if i % 2 == 0:
                y -= 1
            rect(img, x, y, x, y + max(1, 3 - i // 18), MID_TRUNK)
    for (fx, fy) in ((cx - 2, 100), (cx + 3, 130)):  # rostros de espíritus atrapados
        put(img, fx, fy, SPIRIT_HI)
        put(img, fx + 3, fy, SPIRIT_HI)
        rect(img, fx + 1, fy + 3, fx + 2, fy + 3, SPIRIT)
    return outline(img)


def cabin():
    w, h = 112, 80
    img = new(w, h)
    rect(img, 8, 34, 103, 79, DIRT)
    for y in range(36, 80, 6):           # troncos horizontales
        rect(img, 8, y, 103, y, DIRT_SH)
    rect(img, 8, 34, 8, 79, DIRT_HI)
    for i in range(30):                  # techo
        rect(img, 2 + i * 2, 34 - i, 109 - i * 2, 34 - i, GRASS_DK if i % 3 else MID_TRUNK)
    rect(img, 76, 8, 83, 24, ROCK_SH)    # chimenea
    rect(img, 76, 8, 76, 24, ROCK)
    rect(img, 24, 46, 39, 59, OUT)       # ventana con luz cálida
    rect(img, 26, 48, 37, 57, GOLD)
    rect(img, 26, 48, 31, 52, GOLD_HI)
    rect(img, 31, 48, 31, 57, DIRT_DK)
    rect(img, 26, 52, 37, 52, DIRT_DK)
    rect(img, 60, 50, 77, 79, DIRT_DK)   # puerta
    put(img, 74, 64, GOLD)
    for x in (88, 92, 96):               # hierbas colgando (Sarah es curandera)
        rect(img, x, 36, x, 42, SARAH_GREEN)
    return outline(img)


def fallen_log():
    img = new(64, 24)
    rect(img, 2, 8, 61, 21, DIRT)
    rect(img, 2, 8, 61, 9, DIRT_HI)
    rect(img, 2, 19, 61, 21, DIRT_DK)
    rect(img, 54, 9, 61, 20, DIRT_SH)
    rect(img, 56, 11, 59, 18, DIRT_HI)
    rect(img, 20, 4, 22, 8, DIRT_SH)     # rama quebrada
    for x in range(8, 50, 9):
        put(img, x, 13, EMBER)
    return outline(img)


def molten_rock():
    img = new(32, 24)
    rect(img, 3, 8, 28, 23, ROCK_DK)
    rect(img, 6, 4, 22, 8, ROCK_DK)
    rect(img, 6, 4, 22, 4, ROCK_SH)
    rect(img, 3, 8, 3, 23, ROCK_SH)
    for (x, y) in ((8, 10), (9, 11), (10, 12), (11, 12), (18, 14), (19, 15), (20, 15), (14, 19)):
        put(img, x, y, EMBER)
    put(img, 10, 12, EMBER_HOT)
    return outline(img)


def burnt_feathers():
    """Plumas del protagonista quemadas en el cráter."""
    img = new(32, 16)
    for (x0, ang) in ((6, 0.3), (14, -0.2), (22, 0.5)):
        for i in range(11):
            x = int(x0 + i * math.cos(ang))
            y = int(14 - i * 0.8)
            put(img, x, y, SILVER if i < 7 else ASH)
            put(img, x + 1, y, SMOOTH_SH if i < 7 else COAL)
        put(img, x0 + 9, 5, EMBER)
    return outline(img)


# ------------------------------------------------------------------ objetos comunes

def inscription():
    img = new(32, 32)
    rect(img, 4, 4, 27, 31, ROCK)
    rect(img, 4, 4, 27, 4, ROCK_HI)
    rect(img, 4, 4, 4, 31, ROCK_HI)
    rect(img, 27, 5, 27, 31, ROCK_SH)
    for y in range(9, 26, 4):
        for x in range(8, 24):
            if (x * 7 + y) % 5:
                put(img, x, y, ROCK_SH)
    rect(img, 4, 2, 27, 3, CLEAR)
    put(img, 6, 6, GOLD_HI)  # borde dorado: se puede leer
    return outline(img)


def reliquary(open_):
    img = new(32, 32)
    rect(img, 4, 14, 27, 31, DIRT_DK)
    rect(img, 4, 14, 27, 15, GOLD)
    rect(img, 4, 30, 27, 31, GOLD)
    rect(img, 14, 18, 17, 23, GOLD_HI)
    if open_:
        rect(img, 4, 6, 27, 9, DIRT_DK)
        rect(img, 4, 6, 27, 6, GOLD)
        glow(img, 15, 12, GOLD_HI, 1)
    else:
        rect(img, 4, 10, 27, 13, DIRT_SH)
        rect(img, 4, 10, 27, 10, GOLD_HI)
    return outline(img)


def boss_door():
    """Reja de 2×4 tiles con el sello verde de Azazel."""
    img = new(32, 64)
    rect(img, 0, 0, 31, 3, ROCK_SH)
    rect(img, 0, 0, 31, 0, ROCK_HI)
    for x in range(3, 31, 6):
        rect(img, x, 4, x + 1, 63, ROCK_DK)
        put(img, x, 4, ROCK_HI)
    rect(img, 0, 30, 31, 31, ROCK_DK)
    glow(img, 15, 18, SPIRIT, 2)
    for i in range(6):
        put(img, 15, 22 + i, SPIRIT_DK)
    return outline(img)


def lever(down):
    img = new(16, 32)
    rect(img, 3, 24, 12, 31, ROCK_SH)
    rect(img, 3, 24, 12, 24, ROCK_HI)
    if down:
        for i in range(12):
            put(img, 7 + i // 2, 23 - i, DIRT)
            put(img, 8 + i // 2, 23 - i, DIRT_DK)
        glow(img, 13, 10, GOLD_HI, 1)
    else:
        for i in range(12):
            put(img, 8 - i // 2, 23 - i, DIRT)
            put(img, 9 - i // 2, 23 - i, DIRT_DK)
        rect(img, 1, 10, 3, 12, GOLD)
        put(img, 2, 11, GOLD_HI)  # dorado: se puede usar
    return outline(img)


def fragile(frame):
    """Tronco podrido de 2 tiles; frames 1–3 se parte al caer."""
    img = new(32, 16)
    off = frame * 2
    for half, dx in ((0, -off), (1, off)):
        x0 = half * 16 + dx
        rect(img, x0 + 1, 2 + frame, x0 + 14, 9 + frame, DIRT_SH)
        rect(img, x0 + 1, 2 + frame, x0 + 14, 2 + frame, DIRT)
        rect(img, x0 + 1, 9 + frame, x0 + 14, 9 + frame, DIRT_DK)
        for x in range(x0 + 3, x0 + 14, 4):
            put(img, x, 5 + frame, MID_TRUNK_DK)
    if frame == 0:
        rect(img, 15, 2, 16, 9, EMBER)  # grieta de brasa: avisa que se rompe
    return outline(img)


def light_ledge():
    """Saliente de luz para subir la pared lisa mientras no exista el salto de pared."""
    img = new(32, 8)
    rect(img, 2, 1, 29, 4, GOLD_HI)
    rect(img, 2, 1, 29, 1, WHITE)
    rect(img, 4, 5, 27, 5, GOLD)
    for x in range(6, 26, 2):
        put(img, x, 6, GOLD)
    return img


# ------------------------------------------------------------------ elenco provisorio

def sarah():
    img = new(28, 44)
    rect(img, 9, 18, 18, 43, SARAH_GREEN)     # vestido
    rect(img, 9, 18, 9, 43, hx("5A9A48"))
    rect(img, 7, 30, 20, 43, SARAH_GREEN)
    rect(img, 10, 20, 17, 26, CREAM)          # delantal
    rect(img, 10, 7, 17, 16, SKIN := hx("D4A870"))
    rect(img, 8, 4, 19, 8, DIRT)              # pelo
    rect(img, 8, 4, 9, 20, DIRT)
    rect(img, 12, 11, 12, 11, OUT)
    rect(img, 15, 11, 15, 11, OUT)
    rect(img, 19, 22, 21, 30, SKIN)           # brazo con canasta
    rect(img, 20, 28, 26, 33, DIRT_SH)
    put(img, 22, 27, hx("D04080"))            # flores
    put(img, 25, 27, GOLD_HI)
    put(img, 7, 2, hx("D04080"))
    put(img, 20, 3, GOLD_HI)
    return outline(img)


def hueco():
    """Aldeano muerto poseído por un espíritu de Azazel."""
    img = new(24, 32)
    rect(img, 8, 12, 15, 26, hx("4A4A3A"))
    rect(img, 6, 14, 7, 24, hx("3A3A30"))
    rect(img, 16, 13, 18, 22, hx("3A3A30"))
    rect(img, 9, 4, 15, 11, hx("8A8A70"))
    put(img, 11, 7, SPIRIT_HI)
    put(img, 14, 7, SPIRIT_HI)
    rect(img, 8, 27, 10, 31, hx("2A2A20"))
    rect(img, 13, 27, 15, 31, hx("2A2A20"))
    for (x, y) in ((10, 16), (12, 19), (14, 15)):  # grietas con luz verde
        put(img, x, y, SPIRIT)
    return outline(img)


def crow():
    img = new(24, 16)
    rect(img, 8, 6, 16, 10, hx("26262C"))
    rect(img, 16, 5, 19, 8, hx("26262C"))
    put(img, 18, 6, EMBER_HOT)
    put(img, 20, 7, ASH)
    for i in range(7):
        put(img, 7 - i, 6 - i // 2, ASH if i % 2 else hx("26262C"))
        put(img, 12 - i // 2, 5 - i, hx("26262C"))
    rect(img, 3, 9, 7, 10, hx("3A3A40"))
    return outline(img)


def wisp():
    img = new(16, 16)
    glow(img, 8, 9, SPIRIT, 3)
    rect(img, 7, 2, 9, 5, SPIRIT_DK)
    put(img, 8, 1, SPIRIT)
    return img


def azazel():
    """Rey de los Espíritus, fase 1: armadura de huesos y metal oxidado, alas de espíritus."""
    w, h = 96, 96
    img = new(w, h)
    bone, bone_sh, rust = hx("8A8A70"), hx("5A5A48"), hx("6A3A20")
    for side in (-1, 1):                       # alas: cada pluma es una cara
        for k in range(6):
            bx = 48 + side * (14 + k * 5)
            by = 22 + k * 3
            for i in range(30 - k * 3):
                put(img, bx + side * (i // 4), by + i, SPIRIT_DK if i % 5 else SPIRIT)
            put(img, bx, by + 2, SPIRIT_HI)
            put(img, bx + side * 2, by + 2, SPIRIT_HI)
    rect(img, 38, 30, 57, 70, bone)            # torso
    rect(img, 38, 30, 38, 70, hx("A8A890"))
    rect(img, 57, 30, 57, 70, bone_sh)
    for y in range(34, 64, 5):                 # costillas
        rect(img, 41, y, 54, y, bone_sh)
    rect(img, 44, 44, 51, 52, rust)
    glow(img, 47, 48, SPIRIT, 1)               # núcleo verde
    rect(img, 40, 14, 55, 29, bone)            # cráneo con casco
    rect(img, 38, 12, 57, 16, rust)
    for (x, d) in ((38, -1), (57, 1)):         # cuernos
        for i in range(10):
            put(img, x + d * i, 12 - i // 2, bone_sh)
    put(img, 44, 21, SPIRIT_HI)
    put(img, 51, 21, SPIRIT_HI)
    rect(img, 40, 71, 45, 95, bone_sh)         # piernas
    rect(img, 50, 71, 55, 95, bone_sh)
    rect(img, 30, 34, 36, 62, rust)            # brazos
    rect(img, 59, 34, 65, 62, rust)
    for i in range(36):                        # guadaña de hueso
        put(img, 67 + i // 6, 28 + i, bone)
    for i in range(16):
        put(img, 72 - i, 28 - i // 3, bone)
    return outline(img)


# ------------------------------------------------------------------ partículas

def vfx_sheet():
    s = Sheet(128, 64)
    a = new(2, 2, ASH)
    put(a, 1, 1, STAR_DIM)
    s.add("ash_0", a, 0, 0, pivot=(0.5, 0.5))
    s.add("ash_1", new(1, 1, ASH), 4, 0, pivot=(0.5, 0.5))
    s.add("ember_0", new(1, 1, EMBER_HOT), 8, 0, pivot=(0.5, 0.5))
    e = new(2, 2, EMBER)
    put(e, 0, 0, EMBER_HOT)
    s.add("ember_1", e, 12, 0, pivot=(0.5, 0.5))
    for i, (acc, name) in enumerate(((SPIRIT, "wisp"), (GOLD_HI, "wisp_gold"))):
        g = new(7, 7)
        glow(g, 3, 3, acc, 1)
        s.add(f"{name}_0", g, 16 + i * 8, 0, pivot=(0.5, 0.5))
        g2 = new(7, 7)
        put(g2, 3, 3, WHITE)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            put(g2, 3 + dx, 3 + dy, acc)
        s.add(f"{name}_1", g2, 32 + i * 8, 0, pivot=(0.5, 0.5))
    for i, size in enumerate((6, 9, 12)):     # humo
        sm = new(12, 12)
        c = size / 2
        for y in range(12):
            for x in range(12):
                d = math.hypot(x - 5.5, y - 5.5) / c
                if d < 1 and bayer(x, y) > d * 0.9:
                    put(sm, x, y, with_alpha(ASH, 150))
        s.add(f"smoke_{i}", sm, 48 + i * 12, 0, pivot=(0.5, 0.5))
    for f in range(4):                         # polvo de aterrizaje / dash
        d = new(16, 8)
        spread = 2 + f * 2
        for k in range(5):
            x = 8 + (k - 2) * spread // 2
            y = 6 - (k % 2) - f // 2
            if f < 3 or k % 2 == 0:
                rect(d, x, y, x + 1, y + 1, with_alpha(DIRT_HI, 220 - f * 50))
        s.add(f"dust_{f}", d, f * 16, 16, pivot=(0.5, 0.0))
    for f in range(3):                         # chispa de golpe
        sp = new(9, 9)
        r_ = 1 + f * 2
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
            if f < 2 or (dx and dy):
                put(sp, 4 + dx * r_, 4 + dy * r_, WHITE)
        put(sp, 4, 4, WHITE if f == 0 else CLEAR)
        s.add(f"spark_{f}", sp, 64 + f * 9, 16, pivot=(0.5, 0.5))
    for f in range(4):                         # destello de parry, el más brillante
        p = new(16, 16)
        rr = 2 + f * 2
        for ang in range(0, 360, 45):
            for k in range(rr - 1, rr + 2):
                put(p, int(8 + k * math.cos(math.radians(ang))), int(8 + k * math.sin(math.radians(ang))),
                    GOLD_HI if k < rr + 1 else GOLD)
        if f < 2:
            glow(p, 8, 8, GOLD_HI, 1)
        s.add(f"parry_{f}", p, 96 + (f % 2) * 16, 16 + (f // 2) * 16, pivot=(0.5, 0.5))
    leaf = new(3, 2, GRASS_BURNT)
    put(leaf, 2, 0, EMBER)
    s.add("leaf_0", leaf, 0, 32, pivot=(0.5, 0.5))
    return s


# ------------------------------------------------------------------ animados (.aseprite)

def anim(path, w, h, frames, tags):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    write_aseprite(path, w, h, ["sprite"],
                   [{"duration": d, "cels": {0: img}} for (img, d) in frames], tags)


def altar_frame(active, f):
    img = new(32, 48)
    rect(img, 6, 30, 25, 47, ROCK)            # pedestal
    rect(img, 6, 30, 25, 30, ROCK_HI)
    rect(img, 6, 30, 6, 47, ROCK_HI)
    rect(img, 25, 31, 25, 47, ROCK_SH)
    rect(img, 4, 44, 27, 47, ROCK_SH)
    rect(img, 12, 35, 19, 41, ROCK_SH)        # runa dorada
    rect(img, 15, 34, 16, 42, GOLD)
    rect(img, 12, 37, 19, 38, GOLD)
    rect(img, 3, 26, 28, 29, ROCK_SH)         # cuenco
    rect(img, 3, 26, 28, 26, ROCK_HI)
    outline(img)
    flicker = (0, 1, 0, -1, 1, 0)[f % 6]
    if active:
        height = 14 + flicker
        for y in range(26 - height, 26):
            t = (26 - y) / height
            half = max(1, int((1 - t) * 5))
            rect(img, 15 - half, y, 16 + half, y, GOLD_HI if t > 0.3 else WHITE)
        glow(img, 15, 18 + flicker, GOLD_HI, 2)
        for k in range(3):                    # chispas que suben
            put(img, 9 + k * 7, (20 - f * 3 - k * 5) % 22 + 2, GOLD_HI)
    else:
        rect(img, 14, 23 + flicker, 17, 25, GOLD)
        put(img, 15, 22 + flicker, GOLD_HI)
    return img


def lantern_frame(f):
    img = new(16, 32)
    rect(img, 7, 12, 8, 31, MID_TRUNK)         # poste
    rect(img, 7, 12, 7, 31, MID_TRUNK_HI)
    rect(img, 3, 2, 12, 12, ROCK_SH)           # jaula
    rect(img, 4, 3, 11, 11, OUT)
    rect(img, 3, 2, 12, 2, ROCK_HI)
    outline(img)
    flick = (0, 1, 1, 0)[f]
    rect(img, 6, 6 - flick, 9, 10, SPIRIT)
    rect(img, 7, 5 - flick, 8, 9, SPIRIT_HI)
    put(img, 7 + (f % 2), 4 - flick, WHITE)
    return img


def veil_frame(f):
    img = new(16, 64)
    for y in range(64):
        for x in range(16):
            wave = math.sin((y + f * 4) / 5.0 + x / 3.0)
            if wave > 0.1 and (x + y + f) % 2 == 0:
                put(img, x, y, with_alpha(SPIRIT, 150))
            elif wave > 0.75:
                put(img, x, y, with_alpha(SPIRIT_HI, 200))
    for y in range(64):               # bordes más densos: se lee como barrera
        put(img, 0, y, with_alpha(SPIRIT_DK, 220))
        put(img, 15, y, with_alpha(SPIRIT_DK, 220))
    for k in range(3):                # caras de espíritus que se asoman
        y = (k * 21 + f * 3) % 58 + 3
        put(img, 5, y, SPIRIT_HI)
        put(img, 9, y, SPIRIT_HI)
        put(img, 7, y + 3, SPIRIT_HI)
    return img


def fragment_frame(f):
    img = new(16, 24)
    bob = (0, 0, 1, 1, 2, 1, 1, 0)[f]
    cy = 10 + bob
    # rombo de luz con glow que late entre dorado y blanco (GDD: núcleo del pecho)
    for i in range(6):
        rect(img, 8 - i // 2, cy - 5 + i, 7 + i // 2 + 1, cy - 5 + i, GOLD_HI)
        rect(img, 8 - i // 2, cy + 5 - i, 7 + i // 2 + 1, cy + 5 - i, GOLD)
    rect(img, 7, cy - 3, 8, cy + 3, WHITE if f in (2, 3, 4) else GOLD_HI)
    ring = 1 + f % 4 // 2
    for ang in range(0, 360, 30):
        x = int(7.5 + (6 + ring) * math.cos(math.radians(ang + f * 10)))
        y = int(cy + (6 + ring) * math.sin(math.radians(ang + f * 10)))
        if (x + y) % 2 == 0:
            put(img, x, y, CRACK_2)
    return img


# ------------------------------------------------------------------ armado

def build():
    props = Sheet(560, 160)
    x = 0
    for name, img in (("gravestone_a", gravestone(False)), ("gravestone_b", gravestone(True)),
                      ("stone_cross", stone_cross()), ("burnt_feathers", burnt_feathers()),
                      ("molten_rock", molten_rock()), ("fallen_log", fallen_log()),
                      ("mausoleum", mausoleum()), ("cabin", cabin()), ("dead_tree", dead_tree())):
        props.add(name, img, x, 0)
        x += img.size[0] + 2
    props.save(os.path.join(ROOT, "Environment", "Props", "forest_props.png"))

    items = [("inscription", inscription()), ("reliquary_closed", reliquary(False)),
             ("reliquary_open", reliquary(True)), ("boss_door", boss_door()),
             ("lever_up", lever(False)), ("lever_down", lever(True))]
    items += [(f"fragile_{f}", fragile(f)) for f in range(4)]
    items += [("light_ledge", light_ledge())]
    common = Sheet(256, 128)
    x, y, rowh = 0, 0, 0
    for name, img in items:
        if x + img.size[0] > 256:
            x, y, rowh = 0, y + rowh + 1, 0
        common.add(name, img, x, y)
        x += img.size[0] + 1
        rowh = max(rowh, img.size[1])
    common.save(os.path.join(ROOT, "Environment", "Props", "common_objects.png"))

    cast = Sheet(200, 100)
    x = 0
    for name, img in (("sarah", sarah()), ("hueco", hueco()), ("crow", crow()),
                      ("wisp_enemy", wisp()), ("azazel", azazel())):
        cast.add(name, img, x, 0)
        x += img.size[0] + 2
    cast.save(os.path.join(ROOT, "Characters", "Placeholders", "forest_cast.png"))

    vfx_sheet().save(os.path.join(ROOT, "VFX", "vfx_forest.png"))

    props_dir = os.path.join(ROOT, "Environment", "Props")
    anim(os.path.join(props_dir, "altar.aseprite"), 32, 48,
         [(altar_frame(False, f), 160) for f in range(4)] + [(altar_frame(True, f), 100) for f in range(6)],
         [("idle", 0, 3, 0), ("activo", 4, 9, 0)])
    anim(os.path.join(props_dir, "spirit_lantern.aseprite"), 16, 32,
         [(lantern_frame(f), 140) for f in range(4)], [("loop", 0, 3, 0)])
    anim(os.path.join(props_dir, "spirit_veil.aseprite"), 16, 64,
         [(veil_frame(f), 110) for f in range(6)], [("loop", 0, 5, 0)])
    anim(os.path.join(props_dir, "grace_fragment.aseprite"), 16, 24,
         [(fragment_frame(f), 110) for f in range(8)], [("loop", 0, 7, 0)])


if __name__ == "__main__":
    build()
    print("Objetos, elenco, partículas y animados listos en", os.path.abspath(ROOT))
