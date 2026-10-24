"""Tileset del Bosque de la Caída (16×16) y tiles de peligro.

Salidas (en la carpeta que se pase, por defecto Assets/Art/Environment/Tiles):
  forest_tiles.png + .sprites.json   tierra, roca, suelo quemado, pared lisa, tronco
  forest_hazards.png + .sprites.json brasas (4 frames) y estacas quemadas

Luz desde arriba a la izquierda: los bordes superiores e izquierdos se aclaran,
los inferiores y derechos se oscurecen.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from forest_palette import *  # noqa: F401,F403

T = 16


def speckle(img, r, colors, count, avoid_rows=()):
    for _ in range(count):
        x, y = r.randrange(T), r.randrange(T)
        if y in avoid_rows:
            continue
        put(img, x, y, r.choice(colors))


def stone(img, r, x, y, base, shade, hi):
    rect(img, x, y, x + 2, y + 1, base)
    put(img, x, y, hi)
    put(img, x + 2, y + 1, shade)


# ------------------------------------------------------------------ tierra

def dirt_inner(seed):
    r = rng(seed)
    img = new(T, T, DIRT)
    # manchas de sombra en grupos, para que no parezca ruido plano
    for _ in range(5):
        cx, cy = r.randrange(T), r.randrange(T)
        for _ in range(r.randint(3, 6)):
            put(img, (cx + r.randint(-1, 1)) % T, (cy + r.randint(-1, 1)) % T, DIRT_SH)
    speckle(img, r, [DIRT_HI], 4)
    speckle(img, r, [DIRT_DK], 3)
    if seed % 2 == 0:  # piedrita
        stone(img, r, r.randint(2, 11), r.randint(3, 12), ROCK_SH, DIRT_DK, ROCK)
    if seed % 3 == 0:  # raíz
        x = r.randint(2, 13)
        for y in range(r.randint(2, 6), r.randint(10, 15)):
            put(img, x, y, DIRT_DK)
            if r.random() < 0.3:
                x += r.choice((-1, 1))
    return img


def grass_cap(img, r, left_edge=False, right_edge=False):
    """Pasto corrompido sobre las primeras filas, con algunas briznas quemadas."""
    depth = [r.choice((3, 4, 4, 5)) for _ in range(T)]
    for x in range(T):
        for y in range(depth[x]):
            put(img, x, y, GRASS)
        put(img, x, depth[x], GRASS_DK)
        put(img, x, 0, GRASS_HI if r.random() < 0.6 else GRASS)
        # silueta irregular del borde superior
        if r.random() < 0.25:
            put(img, x, 0, CLEAR)
        if r.random() < 0.12:
            put(img, x, 1, GRASS_BURNT)
            put(img, x, 0, CLEAR)
    if left_edge:
        for y in range(3):
            put(img, 0, y, CLEAR)
        put(img, 1, 0, CLEAR)
    if right_edge:
        for y in range(3):
            put(img, T - 1, y, CLEAR)
        put(img, T - 2, 0, CLEAR)


def side_edges(img, left, right, top_start=0):
    for y in range(top_start, T):
        if left:
            put(img, 0, y, OUT)
            put(img, 1, y, DIRT_HI if y % 5 else DIRT)
        if right:
            put(img, T - 1, y, OUT)
            put(img, T - 2, y, DIRT_DK)


def dirt_top(seed, left=False, right=False):
    r = rng(1000 + seed)
    img = dirt_inner(seed)
    side_edges(img, left, right, top_start=3)
    grass_cap(img, r, left, right)
    return img


def dirt_side(seed, left, right):
    img = dirt_inner(seed)
    side_edges(img, left, right)
    return img


def dirt_bottom(seed):
    r = rng(2000 + seed)
    img = dirt_inner(seed)
    rect(img, 0, T - 3, T - 1, T - 2, DIRT_DK)
    rect(img, 0, T - 1, T - 1, T - 1, OUT)
    # raíces que asoman por abajo
    for x in (r.randint(1, 5), r.randint(8, 14)):
        for y in range(T - 6, T):
            put(img, x, y, DIRT_DK if y < T - 1 else MID_TRUNK_DK)
    return img


# ------------------------------------------------------------------ roca

def rock_inner(seed):
    r = rng(3000 + seed)
    img = new(T, T, ROCK_SH)
    # bloques de piedra irregulares con luz arriba a la izquierda
    for (x0, y0, x1, y1) in ((0, 0, 8, 6), (9, 0, 15, 7), (0, 7, 6, 15), (7, 8, 15, 15)):
        x0 += r.randint(0, 1)
        y0 += r.randint(0, 1)
        rect(img, x0, y0, x1 - 1, y1 - 1, ROCK)
        rect(img, x0, y0, x1 - 1, y0, ROCK_HI)
        rect(img, x0, y0, x0, y1 - 1, ROCK_HI)
        rect(img, x0, y1 - 1, x1 - 1, y1 - 1, ROCK_SH)
    speckle(img, r, [ROCK_DK, ROCK_SH], 6)
    if seed % 2:
        for i in range(5):  # grieta
            put(img, 4 + i, 3 + i + (i % 2), ROCK_DK)
    return img


def rock_top(seed):
    r = rng(3500 + seed)
    img = rock_inner(seed)
    rect(img, 0, 0, T - 1, 0, ROCK_HI)
    # musgo corrompido en el borde
    for x in range(T):
        if r.random() < 0.45:
            put(img, x, 0, GRASS)
            if r.random() < 0.5:
                put(img, x, 1, GRASS_DK)
    return img


def rock_side(seed, left, right):
    img = rock_inner(seed)
    for y in range(T):
        if left:
            put(img, 0, y, OUT)
            put(img, 1, y, ROCK_HI)
        if right:
            put(img, T - 1, y, OUT)
            put(img, T - 2, y, ROCK_DK)
    return img


def rock_bottom(seed):
    img = rock_inner(seed)
    rect(img, 0, T - 2, T - 1, T - 2, ROCK_DK)
    rect(img, 0, T - 1, T - 1, T - 1, OUT)
    return img


# ------------------------------------------------------------------ suelo quemado (cráter)

def crater_inner(seed):
    r = rng(4000 + seed)
    img = new(T, T, COAL)
    speckle(img, r, [DIRT_DK, ROCK_DK], 30)
    speckle(img, r, [ASH], 4)
    return img


def crater_top(seed):
    r = rng(4500 + seed)
    img = crater_inner(seed)
    rect(img, 0, 0, T - 1, 1, ROCK_DK)
    speckle(img, r, [ASH, STAR_DIM], 6, avoid_rows=range(2, T))
    # grietas de brasa: 1 px con centro caliente
    x, y = r.randint(1, 5), 2
    for _ in range(9):
        put(img, x, y, EMBER)
        if r.random() < 0.3:
            put(img, x, y, EMBER_HOT)
        x += 1
        y += r.choice((0, 0, 1))
        if y >= T - 2:
            break
    return img


# ------------------------------------------------------------------ pared lisa (obstáculo de salto de pared)

def smooth_wall(seed):
    r = rng(5000 + seed)
    img = new(T, T, SMOOTH)
    rect(img, 0, 0, 0, T - 1, SILVER_HI)
    rect(img, T - 1, 0, T - 1, T - 1, SMOOTH_SH)
    # vetas plateadas verticales: el código visual del salto de pared
    for vx in (4 + seed % 3, 10 + seed % 2):
        for y in range(T):
            put(img, vx, y, SILVER if (y + seed) % 6 else SILVER_HI)
            if r.random() < 0.2:
                vx += r.choice((-1, 1))
    speckle(img, r, [SMOOTH_SH], 5)
    return img


# ------------------------------------------------------------------ tronco (plataforma de un solo sentido)

def log_platform(kind):
    img = new(T, T)
    rect(img, 0, 1, T - 1, 6, DIRT)
    rect(img, 0, 0, T - 1, 0, OUT)
    rect(img, 0, 1, T - 1, 1, DIRT_HI)
    rect(img, 0, 6, T - 1, 6, DIRT_DK)
    rect(img, 0, 7, T - 1, 7, OUT)
    for x in range(1, T, 5):  # vetas de corteza
        put(img, x, 3, DIRT_SH)
        put(img, x + 2, 4, DIRT_SH)
    if kind == "l":
        rect(img, 0, 1, 0, 6, OUT)
        rect(img, 1, 2, 2, 5, DIRT_HI)
        put(img, 2, 3, DIRT_SH)
    if kind == "r":
        rect(img, T - 1, 1, T - 1, 6, OUT)
        rect(img, T - 3, 2, T - 2, 5, DIRT_SH)
    return img


def root_step():
    """Escalón de raíces que baja la palanca del Barranco."""
    img = new(T, T)
    rect(img, 0, 1, T - 1, 4, MID_TRUNK_HI)
    rect(img, 0, 0, T - 1, 0, OUT)
    rect(img, 0, 5, T - 1, 5, OUT)
    for x in range(0, T, 3):
        put(img, x, 2, DIRT_HI)
        put(img, x + 1, 6, MID_TRUNK_DK)
        put(img, x + 1, 7, MID_TRUNK_DK)
    return img


# ------------------------------------------------------------------ peligros

def embers(frame):
    r = rng(6000 + frame)
    img = new(T, T)
    rect(img, 0, 10, T - 1, T - 1, COAL)
    rect(img, 0, 9, T - 1, 9, OUT)
    for x in range(T):
        h = r.choice((0, 1, 1, 2, 3))
        for y in range(10 - h, 10):
            put(img, x, y, EMBER if y > 7 else EMBER_HOT)
        if h >= 3 and r.random() < 0.5:
            put(img, x, 10 - h - 1, EMBER_CORE)
    speckle(img, r, [EMBER, LAVA, EMBER_HOT], 18, avoid_rows=range(0, 10))
    return img


def stakes():
    img = new(T, T)
    for x0 in (1, 6, 11):
        for y in range(5, T):
            put(img, x0, y, MID_TRUNK_HI)
            put(img, x0 + 1, y, MID_TRUNK)
            put(img, x0 + 2, y, MID_TRUNK_DK)
        put(img, x0 + 1, 3, EMBER_HOT)
        put(img, x0 + 1, 4, EMBER)
        put(img, x0, 4, MID_TRUNK_HI)
        put(img, x0 + 2, 4, MID_TRUNK_DK)
    return outline(img)


# ------------------------------------------------------------------ armado de hojas

def build(out_dir):
    tiles = []
    for i in range(3):
        tiles.append((f"dirt_top_{i}", dirt_top(i)))
    tiles += [("dirt_top_l", dirt_top(7, left=True)), ("dirt_top_r", dirt_top(8, right=True)),
              ("dirt_top_lr", dirt_top(9, left=True, right=True))]
    for i in range(4):
        tiles.append((f"dirt_{i}", dirt_inner(20 + i)))
    tiles += [("dirt_l", dirt_side(30, True, False)), ("dirt_r", dirt_side(31, False, True)),
              ("dirt_bottom", dirt_bottom(32))]
    for i in range(2):
        tiles.append((f"rock_top_{i}", rock_top(i)))
    for i in range(2):
        tiles.append((f"rock_{i}", rock_inner(10 + i)))
    tiles += [("rock_l", rock_side(20, True, False)), ("rock_r", rock_side(21, False, True)),
              ("rock_bottom", rock_bottom(22))]
    for i in range(2):
        tiles.append((f"crater_top_{i}", crater_top(i)))
    tiles.append(("crater_0", crater_inner(5)))
    for i in range(2):
        tiles.append((f"smooth_{i}", smooth_wall(i)))
    tiles += [("log_l", log_platform("l")), ("log_m", log_platform("m")), ("log_r", log_platform("r")),
              ("root_step", root_step())]

    cols = 8
    rows = (len(tiles) + cols - 1) // cols
    sheet = Sheet(cols * T, rows * T)
    for i, (name, img) in enumerate(tiles):
        sheet.add(name, img, (i % cols) * T, (i // cols) * T, pivot=(0.5, 0.5))
    sheet.save(os.path.join(out_dir, "forest_tiles.png"))

    hz = Sheet(5 * T, T)
    for f in range(4):
        hz.add(f"embers_{f}", embers(f), f * T, 0, pivot=(0.5, 0.5))
    hz.add("stakes", stakes(), 4 * T, 0, pivot=(0.5, 0.5))
    hz.save(os.path.join(out_dir, "forest_hazards.png"))
    return len(tiles)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
        os.path.dirname(__file__), "..", "..", "Assets", "Art", "Environment", "Tiles")
    n = build(out)
    print(f"{n} tiles + 5 de peligro en {os.path.abspath(out)}")
