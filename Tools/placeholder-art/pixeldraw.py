"""Utilidades de dibujo pixel a pixel compartidas por los generadores de arte provisorio."""
import math
from PIL import Image, ImageDraw


def hx(s):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


def blank(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def line_points(x0, y0, x1, y1):
    """Bresenham entre dos puntos (se redondean)."""
    x0, y0, x1, y1 = map(round, (x0, y0, x1, y1))
    pts = []
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        pts.append((x0, y0))
        if x0 == x1 and y0 == y1:
            break
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy
    return pts


def put(img, x, y, color):
    w, h = img.size
    if 0 <= x < w and 0 <= y < h:
        img.putpixel((int(x), int(y)), color)


def stroke(img, p0, p1, width, color):
    """Trazo grueso con pincel redondo."""
    w, h = img.size
    px = img.load()
    r = width / 2.0
    for (x, y) in line_points(*p0, *p1):
        for oy in range(-width, width + 1):
            for ox in range(-width, width + 1):
                if ox * ox + oy * oy <= r * r:
                    X, Y = x + ox, y + oy
                    if 0 <= X < w and 0 <= Y < h:
                        px[X, Y] = color


def rect(img, x0, y0, x1, y1, color):
    ImageDraw.Draw(img).rectangle([x0, y0, x1, y1], fill=color)


def poly(img, pts, color):
    ImageDraw.Draw(img).polygon([tuple(map(round, p)) for p in pts], fill=color)


def limb(origin, a_upper, a_lower, l1, l2):
    """Extremidad de dos segmentos. Ángulos en grados desde la vertical hacia
    abajo; positivo = hacia adelante (derecha). Devuelve (articulación, extremo)."""
    t = math.radians(a_upper)
    joint = (origin[0] + l1 * math.sin(t), origin[1] + l1 * math.cos(t))
    s = math.radians(a_lower)
    end = (joint[0] + l2 * math.sin(s), joint[1] + l2 * math.cos(s))
    return joint, end


def outline(layers, target, color):
    """Pinta en `target` un contorno de 1 px alrededor de la unión de `layers`."""
    w, h = target.size
    union = blank(w, h)
    for img in layers:
        union.alpha_composite(img)
    up, op = union.load(), target.load()
    for y in range(h):
        for x in range(w):
            if up[x, y][3]:
                continue
            for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                X, Y = x + ox, y + oy
                if 0 <= X < w and 0 <= Y < h and up[X, Y][3]:
                    op[x, y] = color
                    break
