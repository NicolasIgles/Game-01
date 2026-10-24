"""Azazel, Rey de los Espíritus (jefe del Bosque de la Caída). Arte provisorio.

GDD: armadura de huesos y metal oxidado, verde tóxico #60C820 + gris hueso,
alas de espíritus que gritan, fantasmas de 8×8 que orbitan y atacan.

Genera tres archivos:
- azazel.aseprite        cuerpo, 112×96, pies en la fila 95, centro x=56.
- azazel_wings.aseprite  alas de la fase 2, 160×112, mismo pivote (pies) que el cuerpo.
- azazel_spirit.aseprite espíritu de 16×16 (pivote al centro, lo ajusta Unity).

Los tiempos de cada animación coinciden con AzazelStats.
"""
import math
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from aseprite_writer import write_aseprite
from pixeldraw import blank, hx, limb, outline, poly, put, rect, stroke

W, H = 112, 96
GROUND = 95
CX = 56

OUTLINE = hx("0A0A14")
BONE, BONE_HI, BONE_SH = hx("8A8A70"), hx("B8B8A0"), hx("5A5A48")
RUST, RUST_HI = hx("6A3A20"), hx("8A5030")
METAL, METAL_HI, METAL_SH = hx("4A4A52"), hx("6A6A7A"), hx("2A2A30")
GREEN, GREEN_HI, GREEN_DK = hx("60C820"), hx("C8FF90"), hx("2A6010")
CAPE, CAPE_SH = hx("1A2A10"), hx("0E180A")
SPIRIT, SPIRIT_SH = hx("D8F0C0"), hx("90B880")
WHITE = hx("FFFFF0")
PALETTE = [OUTLINE, BONE, BONE_HI, BONE_SH, RUST, RUST_HI, METAL, METAL_HI, METAL_SH, GREEN, GREEN_HI,
           GREEN_DK, CAPE, CAPE_SH, SPIRIT, SPIRIT_SH, WHITE]

LAYERS = ["outline", "cape", "far_arm", "far_leg", "near_leg", "body", "head", "near_arm", "fx"]
L = {n: i for i, n in enumerate(LAYERS)}

THIGH, SHIN = 14, 14
SPINE = 26
UPPER, FORE = 14, 14


def new():
    return blank(W, H)


def ground_hip(far, near, base_y=66):
    lowest = max(limb((CX, base_y), *far, THIGH, SHIN)[1][1], limb((CX, base_y), *near, THIGH, SHIN)[1][1])
    return (CX, base_y + (GROUND - round(lowest)))


def spine(p):
    a = math.radians(p["lean"])
    hip = p["hip"]
    fwd = (math.sin(a), -math.cos(a))
    side = (math.cos(a), math.sin(a))
    top = (hip[0] + fwd[0] * SPINE, hip[1] + fwd[1] * SPINE)
    return fwd, side, top


def head_center(p):
    fwd, side, top = spine(p)
    return (top[0] + fwd[0] * 8 + side[0] * 4, top[1] + fwd[1] * 8 + side[1] * 4 + p.get("head_dy", 0))


def draw_pose(p):
    cels = {i: new() for i in range(len(LAYERS))}
    hip = p["hip"]
    fwd, side, sh = spine(p)

    # ----- capa raída detrás del cuerpo
    img = cels[L["cape"]]
    fl = p.get("cape", 0)
    back = (sh[0] - side[0] * 8, sh[1] - side[1] * 8)
    hem_y = hip[1] + 18
    poly(img, [(back[0] + 6, back[1] - 3), (back[0] - 3, back[1]), (hip[0] - 15 - fl, hem_y),
               (hip[0] - 3, hem_y + 2), (hip[0] + 3, hip[1] + 2)], CAPE)
    for k in range(8):
        x = round(hip[0] - 15 - fl + k * 2)
        put(img, x, hem_y + 1 + (k % 2), CAPE_SH)
        put(img, x, hem_y + 2 + (k % 3), CAPE)

    # ----- piernas acorazadas
    for name, (at, as_), (thigh_c, shin_c, spike) in (
        ("far_leg", p["far_leg"], (METAL_SH, BONE_SH, BONE_SH)),
        ("near_leg", p["near_leg"], (METAL, BONE, BONE_HI)),
    ):
        img = cels[L[name]]
        h = (hip[0] + (-3 if name == "far_leg" else 3), hip[1])
        knee, ankle = limb(h, at, as_, THIGH, SHIN)
        stroke(img, h, knee, 7, thigh_c)
        stroke(img, knee, ankle, 6, shin_c)
        put(img, round(knee[0]) + 2, round(knee[1]) - 1, spike)  # rodillera con púa
        put(img, round(knee[0]) + 3, round(knee[1]) - 2, spike)
        ax, ay = round(ankle[0]), round(ankle[1])
        rect(img, ax - 3, ay - 2, ax + 4, ay, shin_c)
        put(img, ax + 5, ay, spike)

    # ----- torso: peto oxidado con costillas de hueso y núcleo verde
    img = cels[L["body"]]
    stroke(img, hip, sh, 14, METAL)
    stroke(img, (hip[0] - side[0] * 4, hip[1] - side[1] * 4), (sh[0] - side[0] * 4, sh[1] - side[1] * 4), 4, METAL_SH)
    stroke(img, (hip[0] + side[0] * 3, hip[1] + side[1] * 3), (sh[0] + side[0] * 3, sh[1] + side[1] * 3), 6, RUST)
    for k in (7, 11, 15, 19, 23):  # costillas
        c = (hip[0] + fwd[0] * k, hip[1] + fwd[1] * k)
        stroke(img, (c[0] - side[0] * 4, c[1] - side[1] * 4), (c[0] + side[0] * 6, c[1] + side[1] * 6), 1, BONE_HI)
    core = (hip[0] + fwd[0] * 17 + side[0] * 3, hip[1] + fwd[1] * 17 + side[1] * 3)
    glow = p.get("glow", 0)
    rect(img, round(core[0]) - 1, round(core[1]) - 1, round(core[0]) + 1, round(core[1]) + 1, GREEN_HI if glow else GREEN)
    put(img, round(core[0]), round(core[1]), WHITE if glow else GREEN_HI)
    # cinturón y faldón
    for k in range(-7, 8):
        put(img, round(hip[0] + side[0] * k), round(hip[1] + side[1] * k + 1), RUST_HI)
        put(img, round(hip[0] + side[0] * k), round(hip[1] + side[1] * k + 2), RUST)
    poly(img, [(hip[0] - 6, hip[1] + 3), (hip[0] + 7, hip[1] + 3), (hip[0] + 4, hip[1] + 12), (hip[0] - 1, hip[1] + 9), (hip[0] - 5, hip[1] + 11)], CAPE)

    # ----- cabeza: yelmo-calavera con cuernos
    img = cels[L["head"]]
    hc = head_center(p)
    x0, y0 = round(hc[0]) - 6, round(hc[1]) - 7
    rect(img, x0, y0, x0 + 12, y0 + 13, BONE)
    rect(img, x0 - 1, y0 + 2, x0, y0 + 9, BONE_SH)                     # nuca
    rect(img, x0, y0 + 10, x0 + 12, y0 + 13, BONE_SH)                 # mandíbula
    for x in range(x0 + 3, x0 + 13, 2):
        put(img, x, y0 + 11, BONE_HI)                                  # dientes
    rect(img, x0 + 7, y0 + 4, x0 + 11, y0 + 6, OUTLINE)               # cuenca
    put(img, x0 + 8, y0 + 5, GREEN_HI if glow else GREEN)
    put(img, x0 + 9, y0 + 5, GREEN)
    put(img, x0 + 10, y0 + 5, GREEN_DK)
    put(img, x0 + 12, y0 + 8, OUTLINE)                                # nariz
    for x in range(x0 + 1, x0 + 11):
        put(img, x, y0, BONE_HI)
    # cuernos curvos hacia atrás
    for side_k, col in ((0, RUST), (4, RUST_HI)):
        base = (x0 + 2 + side_k, y0)
        pts = [base]
        for t in range(1, 7):
            pts.append((base[0] - t * 1.2, base[1] - 4 * math.sin(t / 6 * math.pi * 0.8) - t * 0.6))
        for a, b in zip(pts, pts[1:]):
            stroke(img, a, b, 2 if a == base else 1, col)

    # ----- brazos con hombrera y garras largas
    for name, (au, al), (arm_c, fore_c, claw_c) in (
        ("far_arm", p["far_arm"], (METAL_SH, BONE_SH, BONE_SH)),
        ("near_arm", p["near_arm"], (METAL, BONE, BONE_HI)),
    ):
        img = cels[L[name]]
        s0 = (sh[0] + side[0] * 2 + (-3 if name == "far_arm" else 0), sh[1] + side[1] * 2 + 2)
        elbow, hand = limb(s0, au, al, UPPER, FORE)
        stroke(img, s0, elbow, 6, arm_c)
        stroke(img, elbow, hand, 5, fore_c)
        # hombrera de hueso con púas
        sx, sy = round(s0[0]), round(s0[1])
        rect(img, sx - 5, sy - 4, sx + 5, sy + 2, fore_c)
        rect(img, sx - 5, sy + 1, sx + 5, sy + 2, BONE_SH if name == "near_arm" else METAL_SH)
        for kx, ky in ((-4, -5), (-1, -7), (2, -6)):
            put(img, sx + kx, sy + ky, claw_c)
            put(img, sx + kx, sy + ky + 1, claw_c)
        d = math.radians(al)
        dx, dy = math.sin(d), math.cos(d)
        for spread in (-0.5, 0.0, 0.5):
            tip = (hand[0] + dx * 9 + dy * spread * 5, hand[1] + dy * 9 - dx * spread * 5)
            stroke(img, hand, tip, 1, claw_c)
            if name == "near_arm":
                put(img, round(tip[0]), round(tip[1]), GREEN if glow else claw_c)

    # ----- efectos
    img = cels[L["fx"]]
    for fx in p.get("fx", []):
        kind = fx[0]
        if kind == "slash":  # ("slash", cx, cy, radio, ang_desde, ang_hasta, grosor) — 0 abajo, 90 adelante
            _, cx, cy, r, a_from, a_to, thick = fx
            for y in range(H):
                for x in range(W):
                    ddx, ddy = x + 0.5 - cx, y + 0.5 - cy
                    dist = math.hypot(ddx, ddy)
                    if dist > r + 0.5 or dist < r - thick - 1:
                        continue
                    ang = math.degrees(math.atan2(ddx, ddy))
                    if ang < -90:
                        ang += 360
                    f = (ang - a_from) / (a_to - a_from)
                    if 0 <= f <= 1 and r - (1 + (thick - 1) * f) <= dist:
                        put(img, x, y, WHITE if dist >= r - 0.6 else GREEN)
        elif kind == "ring":  # onda expansiva
            _, cx, cy, r, col = fx
            for k in range(72):
                a = k / 72 * 2 * math.pi
                put(img, round(cx + r * math.cos(a)), round(cy + r * math.sin(a) * 0.6), col)
        elif kind == "dots":
            for (x, y, c) in fx[1]:
                put(img, x, y, c)
        elif kind == "lines":
            for (x0_, x1_, y, c) in fx[1]:
                for x in range(x0_, x1_ + 1):
                    put(img, x, y, c)

    outline([cels[i] for i in range(1, L["fx"])], cels[L["outline"]], OUTLINE)
    return cels


def pose(far, near, far_arm, near_arm, lean=10, lift=0, **extra):
    hip = ground_hip(far, near)
    p = {"hip": (hip[0], hip[1] - lift), "lean": lean, "far_leg": far, "near_leg": near,
         "far_arm": far_arm, "near_arm": near_arm}
    p.update(extra)
    return p


def frame(duration, p):
    return {"duration": duration, "cels": draw_pose(p)}


STANCE = ((-12, 8), (16, -2))


def shoulder(p):
    fwd, side, sh = spine(p)
    return sh[0] + side[0] * 2, sh[1] + side[1] * 2 + 2


# ---------------------------------------------------------------- animaciones del cuerpo

def idle():
    far, near = STANCE
    return [frame(200, pose(far, near, (-10 + k, 10), (10 - k, -10 + 2 * k), lean=10 + (k % 2), glow=k == 2, cape=k % 2))
            for k in range(4)]


def walk():
    out = []
    for i in range(6):
        ph = i / 6 * 2 * math.pi
        legs = []
        for phase in (ph, ph + math.pi):
            thigh = 6 + 22 * math.sin(phase)
            bend = 10 + 40 * max(0.0, math.cos(phase))
            legs.append((thigh, thigh - bend))
        arm = 15 * math.sin(ph)
        out.append(frame(140, pose(legs[0], legs[1], (-10 + arm, 5 + arm), (10 - arm, -5 - arm), lean=14, cape=1 + i % 2)))
    return out


def claw():
    """Zarpazo: anticipación 3×200 = 600 ms, golpe 120 ms, recuperación 240+230+230 = 700 ms."""
    out = []
    far, near = (-18, 6), (24, -4)
    for d, lean, up, lo, glow in [(260, 0, 150, 185, 0), (240, -6, 170, 205, 1), (100, -10, 185, 220, 1)]:
        out.append(frame(d, pose(far, near, (-20, -10), (up, lo), lean=lean, glow=glow, cape=1)))
    p = pose((-28, 0), (34, 6), (-40, -30), (85, 65), lean=28, glow=1, cape=3)
    sx, sy = shoulder(p)
    p["fx"] = [("slash", sx, sy, 31, 200, 55, 7)]
    out.append(frame(50, p))
    p = pose((-28, 0), (34, 6), (-40, -30), (70, 50), lean=30, glow=1, cape=3)
    sx, sy = shoulder(p)
    p["fx"] = [("slash", sx, sy, 31, 110, 40, 3)]
    out.append(frame(70, p))
    for d, lean, up, lo in [(120, 26, 50, 25), (260, 18, 30, 10), (320, 12, 15, 0)]:
        out.append(frame(d, pose((-24, 2), (30, 2), (-25, -15), (up, lo), lean=lean, cape=2)))
    return out


def claw_fast():
    """Segundo zarpazo, ascendente: anticipación 2×175 = 350 ms, golpe 120 ms, recuperación 2×250 = 500 ms."""
    out = []
    far, near = (-20, 4), (26, -2)
    for d, up, lo in [(230, -40, -20), (120, -60, -45)]:
        out.append(frame(d, pose(far, near, (-10, 0), (up, lo), lean=-4, glow=1, cape=1)))
    p = pose((-26, 0), (32, 8), (-35, -25), (140, 165), lean=22, glow=1, cape=3)
    sx, sy = shoulder(p)
    p["fx"] = [("slash", sx, sy, 31, -10, 160, 7)]
    out.append(frame(50, p))
    p = pose((-26, 0), (32, 8), (-35, -25), (155, 175), lean=20, glow=1, cape=3)
    sx, sy = shoulder(p)
    p["fx"] = [("slash", sx, sy, 31, 90, 170, 3)]
    out.append(frame(70, p))
    for d, up, lo in [(150, 110, 130), (350, 40, 30)]:
        out.append(frame(d, pose(far, near, (-20, -10), (up, lo), lean=14, cape=2)))
    return out


def summon():
    """Invoca espíritus: 4×200 = 800 ms."""
    out = []
    far, near = STANCE
    for k in range(4):
        raise_ = 120 + k * 15
        p = pose(far, near, (-raise_, -raise_ - 20), (raise_, raise_ + 20), lean=-4, glow=1, cape=k % 2)
        sx, sy = shoulder(p)
        dots = []
        for j in range(5 + k * 2):
            ang = j / (5 + k * 2) * 2 * math.pi + k
            r = 10 + k * 4
            dots.append((round(sx + r * math.cos(ang)), round(sy - 8 + r * 0.6 * math.sin(ang)), GREEN_HI if j % 2 else SPIRIT))
        p["fx"] = [("dots", dots)]
        out.append(frame(200, p))
    return out


def charge_windup():
    """Se agazapa antes de la embestida imparable: 230+230+240 = 700 ms."""
    return [frame(d, pose((-30, 30), (50, -20), (-70, -50), (-60, -40), lean=lean, glow=g, cape=2))
            for d, lean, g in [(320, 30, 0), (260, 38, 1), (120, 44, 1)]]


def charge():
    out = []
    for k in range(2):
        p = pose((-45, -20) if k else (-30, 10), (40, 0) if k else (55, -15), (-80, -70), (-70, -55), lean=52, glow=1, cape=4)
        hip = p["hip"]
        p["fx"] = [("lines", [(CX - 34 - 4 * k, CX - 22, hip[1] - 20, SPIRIT_SH), (CX - 30, CX - 20, hip[1] - 10, GREEN_DK),
                              (CX - 36 + 4 * k, CX - 24, hip[1] + 2, SPIRIT_SH)])]
        out.append(frame(100, p))
    return out


def stunned():
    out = []
    for k, lean in enumerate([34, 38, 34, 30]):
        p = pose(STANCE[0], STANCE[1], (0, -5), (5, 0), lean=lean, head_dy=2, cape=0)
        hc = head_center(p)
        dots = []
        for j in range(3):
            ang = (k / 4 + j / 3) * 2 * math.pi
            dots.append((round(hc[0] + 7 * math.cos(ang)), round(hc[1] - 10 + 2 * math.sin(ang)), GREEN_HI if j == 0 else WHITE))
        p["fx"] = [("dots", dots)]
        out.append(frame(150, p))
    return out


def roar():
    """Transición a la fase 2: 200+300+300+300+400 = 1500 ms."""
    out = []
    for k, d in enumerate([200, 300, 300, 300, 400]):
        spread = 60 + k * 15
        p = pose((-20, 0), (24, -4), (-spread, -spread - 20), (spread, spread + 20), lean=-10 - k * 2, glow=1, cape=k % 2 + 2)
        hc = head_center(p)
        p["fx"] = [("ring", hc[0], hc[1] + 8, 8 + k * 9, GREEN_HI if k % 2 else GREEN)] if k else []
        out.append(frame(d, p))
    return out


def hover():
    """Flota (lluvia de almas); el código sube el objeto, el sprite solo cuelga las piernas."""
    out = []
    for k in range(4):
        p = pose((-6, -2), (10, 4), (-100, -125 + k * 5), (100, 125 - k * 5), lean=4, glow=1, cape=k % 2 + 1, lift=0)
        hip = p["hip"]
        p["fx"] = [("dots", [(CX - 6 + (k * 3 + j * 5) % 12, GROUND - (k + j * 3) % 8, GREEN if j % 2 else SPIRIT_SH)
                             for j in range(4)])]
        out.append(frame(150, p))
    return out


def death():
    """Muerte: 150+150+200+200+250+400 = 1350 ms."""
    out = [frame(150, pose((-20, 0), (20, -5), (-80, -60), (-60, -40), lean=-15, glow=1, head_dy=-1)),
           frame(150, pose((50, -70), (65, -60), (-20, -10), (20, 30), lean=20, glow=1)),
           frame(200, pose((80, -95), (85, -90), (30, 50), (50, 70), lean=50, lift=6))]
    for k, d in enumerate([200, 250, 400]):
        p = pose((88, -95), (90, -92), (70, 90), (85, 95), lean=88, lift=13)
        p["fx"] = [("dots", [(CX - 14 + j * 5, GROUND - 14 - k * 6 - (j % 3) * 3, SPIRIT if j % 2 else GREEN_HI)
                             for j in range(2 + k * 2)])]
        out.append(frame(d, p))
    return out


BODY_ANIMS = [
    ("idle", idle, 0), ("walk", walk, 0), ("claw", claw, 1), ("claw_fast", claw_fast, 1),
    ("summon", summon, 1), ("charge_windup", charge_windup, 1), ("charge", charge, 0),
    ("stunned", stunned, 0), ("roar", roar, 1), ("hover", hover, 0), ("death", death, 1),
]


# ---------------------------------------------------------------- alas de la fase 2

WW, WH = 160, 112


def wings_frames():
    """Abanico de espíritus que nace en la espalda (a la altura de los hombros del cuerpo)."""
    root = (WW // 2 - 7, WH - 1 - (GROUND - 49))
    out = []
    for k in range(4):
        flap = [0, 8, 14, 8][k]
        layers = {0: blank(WW, WH), 1: blank(WW, WH), 2: blank(WW, WH)}  # contorno, ala lejana, ala cercana
        for li, offset, col, sh_col in ((1, (5, -3), SPIRIT_SH, GREEN_DK), (2, (0, 0), SPIRIT, GREEN)):
            img = layers[li]
            base = (root[0] + offset[0], root[1] + offset[1])
            # 0 = abajo, 90 = adelante, 180 = arriba, 270 = atrás: se abren hacia atrás y arriba
            for j, ang in enumerate(range(150, 262, 22)):
                a = math.radians(ang + (12 if li == 1 else 0) - flap * (1 if li == 2 else 0.6))
                ln = 36 + (j % 3) * 7
                tip = (base[0] + math.sin(a) * ln, base[1] + math.cos(a) * ln)
                stroke(img, base, tip, 4, col)
                stroke(img, ((base[0] + tip[0]) / 2, (base[1] + tip[1]) / 2), tip, 2, sh_col)
                # cara que grita en la punta de cada pluma
                tx, ty = round(tip[0]), round(tip[1])
                rect(img, tx - 2, ty - 2, tx + 2, ty + 2, col)
                put(img, tx - 1, ty - 1, OUTLINE)
                put(img, tx + 1, ty - 1, OUTLINE)
                put(img, tx, ty + 1, OUTLINE)
        outline([layers[1], layers[2]], layers[0], OUTLINE)
        out.append({"duration": 150, "cels": layers})
    return out


# ---------------------------------------------------------------- espíritu

SW = 16


def spirit_frames():
    def ghost(wobble, flash=False):
        img = blank(SW, SW)
        body, edge = (WHITE, GREEN_HI) if flash else (SPIRIT, GREEN)
        rect(img, 4, 3, 11, 9, body)
        rect(img, 5, 2, 10, 2, body)
        poly(img, [(4, 9), (11, 9), (9 + wobble, 13), (7, 11), (5 - wobble, 14)], body)
        for (x, y) in ((4, 3), (11, 3), (4, 9), (11, 9)):
            put(img, x, y, edge)
        put(img, 6, 5, OUTLINE)
        put(img, 9, 5, OUTLINE)
        rect(img, 7, 7, 8, 8, OUTLINE)  # boca abierta
        lay = {0: blank(SW, SW), 1: img}
        outline([img], lay[0], GREEN_DK)
        return lay

    def marker(k):
        img = blank(SW, SW)
        for x in range(2, 14):
            put(img, x, 15, GREEN if (x + k) % 2 else GREEN_HI)
        for x in range(4, 12):
            put(img, x, 14, GREEN_DK if (x + k) % 3 else GREEN)
        return {0: blank(SW, SW), 1: img}

    frames = [{"duration": 150, "cels": ghost(0)}, {"duration": 150, "cels": ghost(1)},
              {"duration": 60, "cels": ghost(0, True)}, {"duration": 60, "cels": ghost(1)},
              {"duration": 120, "cels": marker(0)}, {"duration": 120, "cels": marker(1)}]
    tags = [("float", 0, 1, 0), ("flash", 2, 3, 0), ("marker", 4, 5, 0)]
    return frames, tags


# ---------------------------------------------------------------- salida

def build(out_dir, preview_dir=None):
    preview_dir = preview_dir or os.path.join(os.path.dirname(os.path.abspath(__file__)), "previews")
    os.makedirs(preview_dir, exist_ok=True)

    frames, tags, groups = [], [], []
    for name, fn, repeat in BODY_ANIMS:
        seq = fn()
        tags.append((name, len(frames), len(frames) + len(seq) - 1, repeat))
        groups.append((name, seq))
        frames += seq
    write_aseprite(os.path.join(out_dir, "azazel.aseprite"), W, H, LAYERS, frames, tags, PALETTE)
    print("azazel.aseprite", len(frames), "frames,", len(tags), "animaciones")

    wings = wings_frames()
    write_aseprite(os.path.join(out_dir, "azazel_wings.aseprite"), WW, WH, ["outline", "far_wing", "near_wing"],
                   wings, [("flap", 0, len(wings) - 1, 0)], PALETTE)
    sp_frames, sp_tags = spirit_frames()
    write_aseprite(os.path.join(out_dir, "azazel_spirit.aseprite"), SW, SW, ["outline", "spirit"], sp_frames, sp_tags, PALETTE)

    # vistas previas: hoja del cuerpo con alas detrás del idle, y espíritu
    def compose(fr, size):
        img = Image.new("RGBA", size, (28, 36, 30, 255))
        for i in sorted(fr["cels"]):
            img.alpha_composite(fr["cels"][i])
        return img

    cols = 7
    sheet = Image.new("RGBA", (W * cols, H * len(groups)), (20, 26, 22, 255))
    for r, (name, seq) in enumerate(groups):
        for c, fr in enumerate(seq[:cols]):
            sheet.alpha_composite(compose(fr, (W, H)), (c * W, r * H))
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(os.path.join(preview_dir, "azazel_sheet.png"))

    phase2 = Image.new("RGBA", (WW * 4, WH), (20, 26, 22, 255))
    body_idle = groups[0][1]
    for k in range(4):
        img = compose(wings[k], (WW, WH))
        body = compose(body_idle[k % len(body_idle)], (W, H))
        mask = Image.new("RGBA", (W, H))
        for i in sorted(body_idle[k % len(body_idle)]["cels"]):
            mask.alpha_composite(body_idle[k % len(body_idle)]["cels"][i])
        img.alpha_composite(mask, ((WW - W) // 2, WH - H))
        phase2.alpha_composite(img, (k * WW, 0))
    phase2.resize((phase2.width * 2, phase2.height * 2), Image.NEAREST).save(os.path.join(preview_dir, "azazel_phase2.png"))

    sp = Image.new("RGBA", (SW * 6, SW), (20, 26, 22, 255))
    for k, fr in enumerate(sp_frames):
        sp.alpha_composite(compose(fr, (SW, SW)), (k * SW, 0))
    sp.resize((sp.width * 8, sp.height * 8), Image.NEAREST).save(os.path.join(preview_dir, "azazel_spirit.png"))


if __name__ == "__main__":
    build(sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__)))
