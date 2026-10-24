"""Fondos del Bosque de la Caída. Todas las capas que se repiten en horizontal
son continuas en los bordes (el último píxel empalma con el primero).

Salidas (Assets/Art/Environment/Backgrounds):
  forest_sky.png       480×270  cielo nocturno con la grieta celeste (capa fija)
  forest_far.png       480×270  colinas y pinos en silueta            (×0,2)
  forest_mid.png       960×270  troncos quemados con brasas           (×0,5)
  cemetery_mid.png     960×270  cruces, lápidas y el árbol muerto     (×0,5)
  forest_fog.png       480×64   banda de niebla verde                 (×0,7)
  forest_fg.png + .json         piezas de primer plano                (×1,2)
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from forest_palette import *  # noqa: F401,F403

W, H = 480, 270


def periodic(x, period, terms, seed):
    """Ruido suave que se repite cada `period` px (suma de senos con frecuencias enteras)."""
    r = rng(seed)
    v = 0.0
    for k, amp in terms:
        phase = r.random() * math.tau
        v += amp * math.sin(math.tau * k * x / period + phase)
    return v


# ------------------------------------------------------------------ cielo

def sky():
    img = new(W, H)
    stops = [(0.0, SKY0), (0.45, SKY1), (0.8, SKY2), (1.0, SKY3)]
    for y in range(H):
        t = y / (H - 1)
        for i in range(len(stops) - 1):
            if stops[i][0] <= t <= stops[i + 1][0]:
                lt = (t - stops[i][0]) / (stops[i + 1][0] - stops[i][0])
                c0, c1 = stops[i][1], stops[i + 1][1]
                break
        for x in range(W):
            # degradado con dithering ordenado: sin bandas suaves fuera de paleta
            put(img, x, y, c1 if bayer(x, y) < lt else c0)

    r = rng(11)
    crack_x = 318
    # resplandor violeta alrededor de la grieta: el "cielo irreal"
    for y in range(0, 190):
        for x in range(crack_x - 70, crack_x + 70):
            d = abs(x - crack_x) / 70 + y / 400
            if d < 1 and bayer(x, y) > d * 1.15:
                put(img, x, y, SKY_VIOLET)

    # estrellas (ninguna dentro del resplandor fuerte)
    for _ in range(140):
        x, y = r.randrange(W), r.randrange(int(H * 0.75))
        if abs(x - crack_x) < 24:
            continue
        put(img, x, y, STAR if r.random() < 0.25 else STAR_DIM)
    for _ in range(6):  # estrellas grandes en cruz
        x, y = r.randrange(8, W - 8), r.randrange(8, 120)
        if abs(x - crack_x) < 40:
            continue
        put(img, x, y, STAR)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            put(img, x + dx, y + dy, STAR_DIM)

    # la grieta celeste: cicatriz vertical dentada con glow en 3 niveles (GDD)
    path = []
    x = crack_x
    for y in range(0, 176):
        if y % 6 == 0:
            x += r.choice((-2, -1, 0, 1, 2))
            x = max(crack_x - 9, min(crack_x + 9, x))
        width = 3 if y < 120 else (2 if y < 150 else 1)
        path.append((x, y, width))
    for (cx, y, wdt) in path:   # anillo 2: dithering cyan al 50 %
        for x in range(cx - wdt - 4, cx + wdt + 5):
            if (x + y) % 2 == 0:
                put(img, x, y, CRACK_3)
    for (cx, y, wdt) in path:   # anillo 1: acento al 100 %
        rect(img, cx - wdt - 1, y, cx + wdt + 1, y, CRACK_2)
    for (cx, y, wdt) in path:   # núcleo casi blanco
        rect(img, cx - wdt + 1, y, cx + wdt - 1, y, CRACK_1)
        if wdt >= 2:
            put(img, cx, y, CRACK_CORE)
    # ramificaciones finas
    for (sy, dirx, length) in ((40, 1, 26), (78, -1, 34), (118, 1, 20)):
        cx = path[sy][0]
        x, y = cx, sy
        for i in range(length):
            x += dirx
            if i % 3 == 0:
                y += 1
            put(img, x, y, CRACK_2)
            if i < length // 2:
                put(img, x, y - 1, CRACK_3)
    return img


# ------------------------------------------------------------------ capa lejana

def pine(img, x0, base_y, height, body, hi):
    """Pino en silueta: triángulo escalonado con tronco corto."""
    for y in range(height):
        half = int((y / height) * height * 0.28) + 1
        if y % 5 == 4:
            half -= 1  # escalones de ramas
        yy = base_y - height + y
        for x in range(x0 - half, x0 + half + 1):
            put(img, x % W, yy, body)
        put(img, (x0 - half) % W, yy, hi)
    rect(img, x0 - 1, base_y, x0 + 1, base_y + 4, body)


def far():
    img = new(W, H)
    # colinas azuladas al fondo
    for x in range(W):
        top = int(170 + periodic(x, W, [(1, 14), (3, 6), (7, 2)], 21))
        for y in range(top, H):
            put(img, x, y, FAR_HILL)
    # pinos más oscuros delante
    r = rng(22)
    xs = sorted(r.randrange(W) for _ in range(46))
    for x in xs:
        base = int(205 + periodic(x, W, [(2, 8), (5, 3)], 23))
        pine(img, x, base, r.randint(28, 64), FAR_PINE, FAR_PINE_HI)
    for x in range(W):
        base = int(205 + periodic(x, W, [(2, 8), (5, 3)], 23))
        for y in range(base, H):
            put(img, x, y, FAR_PINE)
    return img


# ------------------------------------------------------------------ capa media: troncos quemados

def burnt_trunk(img, x0, width, top, r):
    for y in range(top, H):
        for x in range(x0, x0 + width):
            c = MID_TRUNK
            if x == x0:
                c = MID_TRUNK_HI          # luz desde la izquierda
            elif x >= x0 + width - 2:
                c = MID_TRUNK_DK
            put(img, x % 960, y, c)
    # copa rota: punta astillada
    for i in range(width):
        h = r.randint(0, 10)
        for y in range(top - h, top):
            put(img, (x0 + i) % 960, y, MID_TRUNK_DK)
    # ramas muertas
    for _ in range(r.randint(1, 3)):
        by = r.randint(top + 10, top + 70)
        d = r.choice((-1, 1))
        x = x0 + (width if d > 0 else -1)
        for i in range(r.randint(10, 26)):
            put(img, x % 960, by - i // 3, MID_TRUNK_DK)
            x += d
    # grietas con brasa
    for _ in range(r.randint(1, 3)):
        cx, cy = x0 + r.randint(1, max(1, width - 2)), r.randint(top + 20, H - 30)
        for i in range(r.randint(4, 9)):
            put(img, cx % 960, cy + i, EMBER if i % 3 else EMBER_HOT)


def mid():
    w = 960
    img = new(w, H)
    r = rng(31)
    x = 10
    while x < w - 10:
        width = r.randint(7, 15)
        burnt_trunk(img, x, width, r.randint(30, 120), r)
        x += width + r.randint(40, 110)
    # suelo lejano con brasas
    for x in range(w):
        top = int(238 + periodic(x, w, [(3, 4), (8, 2)], 32))
        for y in range(top, H):
            put(img, x, y, MID_TRUNK_DK)
        if r.random() < 0.05:
            put(img, x, top, EMBER)
    return img


# ------------------------------------------------------------------ capa media: cementerio

def cross(img, x, base, h, c):
    rect(img, x, base - h, x + 2, base, c)
    rect(img, x - 3, base - h + 4, x + 5, base - h + 6, c)


def cemetery():
    w = 960
    img = new(w, H)
    r = rng(41)
    dark, mid_c = hx("16201A"), hx("22302A")
    # árbol muerto gigante
    tx = 620
    for y in range(40, H):
        half = 10 + max(0, (y - 180) // 6)
        rect(img, tx - half, y, tx + half, y, dark)
    for (sy, d, n) in ((60, -1, 70), (90, 1, 90), (130, -1, 60), (52, 1, 50)):
        x, y = tx, sy
        for i in range(n):
            x += d
            if i % 2 == 0:
                y -= 1
            rect(img, x, y, x, y + max(1, 4 - i // 20), dark)
            if i % 17 == 16:  # ramitas
                for j in range(8):
                    put(img, x + d * (j // 2), y - j, dark)
    # mausoleo en silueta
    mx = 180
    rect(img, mx, 160, mx + 110, H, dark)
    for i in range(30):
        rect(img, mx - 6 + i * 2, 160 - i, mx + 116 - i * 2, 160 - i, dark)
    rect(img, mx + 45, 190, mx + 65, H, hx("0A0A14"))
    rect(img, mx + 52, 120, mx + 56, 136, dark)
    rect(img, mx + 47, 125, mx + 61, 128, dark)
    # cruces y lápidas
    for _ in range(26):
        x = r.randrange(w)
        base = 232 + r.randint(-4, 4)
        if r.random() < 0.5:
            cross(img, x, base, r.randint(14, 24), mid_c)
        else:
            hh = r.randint(10, 18)
            rect(img, x, base - hh, x + 8, base, mid_c)
            rect(img, x + 1, base - hh - 1, x + 7, base - hh - 1, mid_c)
    for x in range(w):
        top = int(232 + periodic(x, w, [(2, 3), (7, 1)], 42))
        rect(img, x, top, x, H - 1, dark)
    return img


# ------------------------------------------------------------------ niebla

def fog():
    w, h = 480, 64
    img = new(w, h)
    for y in range(h):
        for x in range(w):
            density = 0.5 + 0.5 * math.sin(math.tau * (x / w) * 3 + periodic(x, w, [(5, 1.2), (11, 0.6)], 51))
            vertical = 1 - abs(y - h * 0.55) / (h * 0.45)
            v = max(0.0, density * vertical)
            if v > bayer(x, y) * 1.4:
                put(img, x, y, with_alpha(SPIRIT_DK, 110 if v < 0.6 else 150))
    return img


# ------------------------------------------------------------------ primer plano

def fg_pieces():
    sheet = Sheet(160, 160)
    shade = with_alpha(OUT, 230)
    rim = with_alpha(hx("1A2A3A"), 230)
    # rama colgante (cuelga desde arriba: pivote arriba)
    br = new(64, 96)
    x = 6
    for y in range(96):
        if y < 60:
            rect(br, x, y, x + 3, y, shade)
            put(br, x, y, rim)
        if y % 4 == 0 and x < 50:
            x += 1
        if y % 14 == 7:
            for i in range(12):
                put(br, x + 2 + i, y + i // 2, shade)
    for (lx, ly) in ((20, 60), (30, 72), (40, 66)):
        for i in range(18):
            put(br, lx + (i % 3) - 1, ly + i, shade)
    sheet.add("fg_branch", br, 0, 0, pivot=(0.5, 1.0))
    # helecho
    fe = new(48, 32)
    for k, (bx, d) in enumerate(((24, -1), (24, 1), (22, -1), (26, 1), (24, 0))):
        x, y = bx, 31
        for i in range(26 - k * 2):
            put(fe, x, y, shade)
            if i % 2:
                put(fe, x + d, y - 1, shade)
            y -= 1
            if i % 3 == 0:
                x += d * 2 if d else 0
    sheet.add("fg_fern", fe, 64, 0)
    # tronco vertical en primer plano
    tr = new(24, 160)
    rect(tr, 2, 0, 21, 159, shade)
    rect(tr, 2, 0, 2, 159, rim)
    for y in range(10, 150, 23):
        rect(tr, 8, y, 9, y + 6, rim)
    sheet.add("fg_trunk", tr, 112, 0)
    return sheet


def build(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    sky().save(os.path.join(out_dir, "forest_sky.png"))
    far().save(os.path.join(out_dir, "forest_far.png"))
    mid().save(os.path.join(out_dir, "forest_mid.png"))
    cemetery().save(os.path.join(out_dir, "cemetery_mid.png"))
    fog().save(os.path.join(out_dir, "forest_fog.png"))
    fg_pieces().save(os.path.join(out_dir, "forest_fg.png"))


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
        os.path.dirname(__file__), "..", "..", "Assets", "Art", "Environment", "Backgrounds")
    build(out)
    print("Fondos listos en", os.path.abspath(out))
