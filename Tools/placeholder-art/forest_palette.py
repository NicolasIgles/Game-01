"""Paleta y utilidades de dibujo compartidas por el arte provisorio del Bosque.

Colores del GDD: paleta de la Tierra (verde corrupto, marrón tierra, gris
piedra), azules del Cielo para la noche y la grieta celeste, brasas del
Infierno y el verde tóxico de Azazel como acento de la zona.
"""
import json
import os
import random
from PIL import Image


def hx(s, a=255):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


CLEAR = (0, 0, 0, 0)
OUT = hx("0A0A14")            # contorno (nunca negro puro)

# Cielo nocturno y grieta celeste
SKY0, SKY1, SKY2, SKY3 = hx("0A0A14"), hx("0F1730"), hx("1A2850"), hx("2A4A7F")
SKY_VIOLET = hx("2A1E48")
CRACK_CORE, CRACK_1, CRACK_2, CRACK_3 = hx("FFFFF0"), hx("F5F5FF"), hx("8BB8D4"), hx("40D0FF")
STAR, STAR_DIM = hx("E8E8F0"), hx("6A6A7A")

# Siluetas de fondo (cada capa más oscura cuanto más lejos)
FAR_HILL, FAR_PINE, FAR_PINE_HI = hx("121C2E"), hx("0F1A12"), hx("1A2A1C")
MID_TRUNK, MID_TRUNK_HI, MID_TRUNK_DK = hx("2A1A0C"), hx("3E2814"), hx("1A1008")

# Tierra
GRASS, GRASS_HI, GRASS_DK = hx("3A5A20"), hx("587A30"), hx("1A2A10")
GRASS_BURNT = hx("4A3A2A")
DIRT, DIRT_SH, DIRT_DK, DIRT_HI = hx("6A4020"), hx("4A2C16"), hx("2A1A0C"), hx("8A5A30")
ROCK, ROCK_SH, ROCK_HI, ROCK_DK = hx("7A7A80"), hx("3A3A40"), hx("9A9AA4"), hx("26262C")
SMOOTH, SMOOTH_SH, SILVER, SILVER_HI = hx("B8B8C8"), hx("8A8AA0"), hx("E8E8F0"), hx("F5F5FF")

# Brasas
EMBER, EMBER_HOT, EMBER_CORE, LAVA = hx("E85A00"), hx("FFB820"), hx("FFFFF0"), hx("CC2200")
COAL, ASH = hx("1E1414"), hx("6A6A7A")

# Acento de Azazel (espíritus) y dorado de gracia
SPIRIT, SPIRIT_HI, SPIRIT_DK = hx("60C820"), hx("B0F070"), hx("2E6A10")
GOLD, GOLD_HI, WHITE = hx("C8A832"), hx("E8C860"), hx("FFFFF0")
BLOOD = hx("901820")
CREAM, SARAH_GREEN = hx("F0E8D0"), hx("3A7A30")


def new(w, h, color=CLEAR):
    return Image.new("RGBA", (w, h), color)


def put(img, x, y, c):
    w, h = img.size
    if 0 <= x < w and 0 <= y < h:
        img.putpixel((int(x), int(y)), c)


def rect(img, x0, y0, x1, y1, c):
    """Rectángulo relleno, coordenadas inclusivas."""
    for y in range(int(y0), int(y1) + 1):
        for x in range(int(x0), int(x1) + 1):
            put(img, x, y, c)


def with_alpha(c, a):
    return (c[0], c[1], c[2], a)


def lerp(c0, c1, t):
    return tuple(int(round(c0[i] + (c1[i] - c0[i]) * t)) for i in range(4))


def bayer(x, y):
    """Umbral de dithering ordenado 4×4 en [0, 1)."""
    m = ((0, 8, 2, 10), (12, 4, 14, 6), (3, 11, 1, 9), (15, 7, 13, 5))
    return (m[y % 4][x % 4] + 0.5) / 16.0


def outline(img, color=OUT):
    """Contorno de 1 px alrededor de todo lo opaco."""
    w, h = img.size
    src = img.copy()
    for y in range(h):
        for x in range(w):
            if src.getpixel((x, y))[3] != 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src.getpixel((nx, ny))[3] > 128:
                    img.putpixel((x, y), color)
                    break
    return img


class Sheet:
    """Hoja de sprites con nombres. Guarda el PNG y un .json con los recortes,
    que el constructor de Unity usa para cortar la hoja sin depender del orden."""

    def __init__(self, width, height):
        self.img = new(width, height)
        self.rects = []

    def add(self, name, sprite, x, y, pivot=(0.5, 0.0)):
        self.img.alpha_composite(sprite, (x, y))
        w, h = sprite.size
        # Unity mide y desde abajo.
        self.rects.append({"name": name, "x": x, "y": self.img.size[1] - y - h, "w": w, "h": h,
                           "px": pivot[0], "py": pivot[1]})

    def save(self, path):
        os.makedirs(os.path.dirname(path), exist_ok=True)
        self.img.save(path)
        with open(os.path.splitext(path)[0] + ".sprites.json", "w", encoding="utf-8") as f:
            json.dump({"sprites": self.rects}, f, indent=1)


def rng(seed):
    return random.Random(seed)
