"""Sprite base del protagonista, forma humana (fase 1 del GDD).

Canvas 96x64, pies en la fila 63 y centro en x=48 (pivote abajo al centro).
Cuerpo de ~44 px de alto, mirando a la derecha. Cada parte va en su capa para
poder retocarla en Aseprite o sumar piezas de armadura encima.
"""
import math
import os
import sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from aseprite_writer import write_aseprite
from pixeldraw import blank, hx, limb, line_points, outline, poly, put, rect, stroke

W, H = 96, 64
GROUND = 63
CX = 48


# Paleta (GDD: paleta de la Tierra + plata del protagonista + contorno no negro)
OUTLINE = hx("0A0A14")
SKIN, SKIN_SH, SKIN_HI = hx("D4A870"), hx("A87850"), hx("E8C498")
HAIR, HAIR_SH, HAIR_HI, HAIR_DK = hx("D0D0E0"), hx("8A8AA0"), hx("F0F0FF"), hx("6A6A80")
ROBE, ROBE_SH, ROBE_DK, ROBE_HI = hx("E8E8F0"), hx("A8A8C0"), hx("6A6A80"), hx("F5F5FF")
PANTS, PANTS_SH = hx("3A3A40"), hx("26262C")
WRAP, WRAP_SH = hx("6A4020"), hx("4A2C16")
ROPE = hx("5A3A20")
EYE = hx("3A3A40")  # fase 1: ojos sin brillo
BLADE, BLADE_SH = hx("E8E8F0"), hx("6A6A7A")
GOLD, GOLD_HI = hx("C8A832"), hx("E8C860")
WHITE = hx("FFFFF0")
PALETTE = [OUTLINE, SKIN, SKIN_SH, SKIN_HI, HAIR, HAIR_SH, HAIR_HI, HAIR_DK, ROBE, ROBE_SH,
           ROBE_DK, ROBE_HI, PANTS, PANTS_SH, WRAP, WRAP_SH, ROPE, EYE, BLADE, BLADE_SH, GOLD,
           GOLD_HI, WHITE]

# De abajo hacia arriba. "fx" (tajos, destellos, partículas) va sin contorno.
LAYERS = ["outline", "far_arm", "far_leg", "near_leg", "robe", "head", "hair", "near_arm", "sword", "fx"]
L = {n: i for i, n in enumerate(LAYERS)}


def new():
    return blank(W, H)


# ---------------------------------------------------------------- pose → capas

def draw_pose(pose):
    """pose: dict con hip (x,y), torso_dy, piernas, brazos, pelo, hem."""
    cels = {i: new() for i in range(len(LAYERS))}
    hip = pose["hip"]
    dy = pose["torso_dy"]
    lean = pose.get("lean", 0)

    # ----- piernas
    for name, (at, as_), (c, cs, fw, fws) in (
        ("far_leg", pose["far_leg"], (PANTS_SH, PANTS_SH, WRAP_SH, WRAP_SH)),
        ("near_leg", pose["near_leg"], (PANTS, PANTS_SH, WRAP, WRAP_SH)),
    ):
        img = cels[L[name]]
        h = (hip[0] + (-1 if name == "far_leg" else 1), hip[1])
        knee, ankle = limb(h, at, as_, 9, 9)
        stroke(img, h, knee, 3, c)
        stroke(img, knee, ankle, 2, c)
        # vendas en el tobillo y pie
        stroke(img, (knee[0] * 0.35 + ankle[0] * 0.65, knee[1] * 0.35 + ankle[1] * 0.65), ankle, 2, fw)
        ax, ay = round(ankle[0]), round(ankle[1])
        foot_up = pose.get(name + "_toe", 0)
        rect(img, ax - 1, ay, ax + 2, ay + 1, fw)
        put(img, ax + 3, ay + 1 - foot_up, fw)
        put(img, ax - 1, ay + 1, fws)

    # ----- torso / túnica (de hombros hasta debajo de la cadera, borde roto)
    img = cels[L["robe"]]
    sh_y = hip[1] - 14 + dy
    back, front = CX - 3 + lean, CX + 3 + lean
    hem_y = hip[1] + 3
    flutter = pose.get("hem", 0)
    body = [(back, sh_y), (front, sh_y), (front + 1, sh_y + 4), (front, hip[1] - 3), (CX + 4, hem_y),
            (CX - 4 + flutter, hem_y), (CX - 4 + flutter // 2, hip[1] - 1), (back - 1, sh_y + 4)]
    poly(img, body, ROBE)
    # sombra en la espalda
    poly(img, [(back, sh_y + 1), (back + 1, sh_y + 1), (CX - 2, hem_y), (CX - 4 + flutter, hem_y),
               (CX - 4 + flutter // 2, hip[1] - 1), (back - 1, sh_y + 4)], ROBE_SH)
    # dobladillo deshilachado
    for i, x in enumerate(range(CX - 4 + flutter, CX + 5)):
        put(img, x, hem_y, ROBE_SH)
        if i % 2 == 0:
            put(img, x, hem_y + 1, ROBE_DK if i % 4 == 0 else ROBE_SH)
    # brillo en el pecho
    for y in range(sh_y + 1, sh_y + 5):
        put(img, front, y, ROBE_HI)
    # cuerda en la cintura
    belt_y = hip[1] - 2
    for x in range(CX - 3 + flutter // 2, front + 1):
        put(img, x, belt_y, ROPE)
    put(img, CX - 4 + flutter // 2, belt_y + 1, ROPE)
    put(img, CX - 5 + flutter, belt_y + 2, ROPE)

    # ----- cabeza
    img = cels[L["head"]]
    hy = sh_y - 10  # arriba de la cabeza
    hx0 = CX - 3 + lean
    rect(img, hx0 + 1, sh_y - 2, hx0 + 4, sh_y, SKIN_SH)          # cuello
    rect(img, hx0, hy + 1, hx0 + 6, hy + 8, SKIN)                  # cara
    put(img, hx0 + 7, hy + 5, SKIN)                                 # nariz
    rect(img, hx0, hy + 7, hx0 + 5, hy + 8, SKIN_SH)               # mandíbula en sombra
    put(img, hx0 + 6, hy + 8, SKIN_SH)
    put(img, hx0 + 5, hy + 4, EYE)                                  # ojo
    put(img, hx0 + 4, hy + 3, SKIN_SH)                              # ceja
    put(img, hx0 + 5, hy + 3, SKIN_SH)

    # ----- pelo largo plateado
    img = cels[L["hair"]]
    sway = pose.get("hair", 0)
    poly(img, [(hx0 - 1, hy + 1), (hx0 + 1, hy - 1), (hx0 + 6, hy - 1), (hx0 + 7, hy + 1),
               (hx0 + 7, hy + 2), (hx0 + 3, hy + 2), (hx0 + 2, hy + 4), (hx0 + 1, hy + 9)], HAIR)
    # melena que cae por la espalda
    poly(img, [(hx0 - 1, hy + 1), (hx0 + 2, hy + 3), (hx0 + 1, hy + 10), (hx0 - 1 - sway, hy + 16),
               (hx0 - 3 - sway, hy + 15), (hx0 - 2, hy + 8)], HAIR)
    poly(img, [(hx0 - 1, hy + 4), (hx0, hy + 9), (hx0 - 2 - sway, hy + 15), (hx0 - 3 - sway, hy + 15),
               (hx0 - 2, hy + 8)], HAIR_SH)
    for x in range(hx0 + 1, hx0 + 6):
        put(img, x, hy - 1, HAIR_HI)
    put(img, hx0 + 6, hy, HAIR_HI)
    put(img, hx0 + 6, hy + 2, HAIR_SH)  # flequillo
    put(img, hx0 + 7, hy + 3, HAIR_SH)
    put(img, hx0 - 2 - sway, hy + 16, HAIR_DK)

    # ----- brazos (manga corta rota + antebrazo de piel)
    for name, (a_up, a_lo), (c, cs, skin) in (
        ("far_arm", pose["far_arm"], (ROBE_SH, ROBE_DK, SKIN_SH)),
        ("near_arm", pose["near_arm"], (ROBE, ROBE_SH, SKIN)),
    ):
        img = cels[L[name]]
        sh = (CX + lean + (-1 if name == "far_arm" else 0), sh_y + 2)
        elbow, hand = limb(sh, a_up, a_lo, 6, 5)
        mid = ((sh[0] + elbow[0]) / 2, (sh[1] + elbow[1]) / 2)
        stroke(img, sh, mid, 2, c)          # manga corta
        stroke(img, mid, elbow, 1, skin)
        stroke(img, elbow, hand, 1, skin)
        hx_, hy_ = round(hand[0]), round(hand[1])
        rect(img, hx_, hy_, hx_ + 1, hy_ + 1, skin)
        put(img, hx_, hy_ + 1, SKIN_SH)

    # ----- espada en la mano cercana
    if "sword" in pose:
        img = cels[L["sword"]]
        sh = (CX + lean, sh_y + 2)
        _, hand = limb(sh, *pose["near_arm"], 6, 5)
        a = math.radians(pose["sword"])
        d = (math.sin(a), math.cos(a))
        hand = (hand[0] + 0.5, hand[1] + 0.5)
        pommel = (hand[0] - 2 * d[0], hand[1] - 2 * d[1])
        tip = (hand[0] + 13 * d[0], hand[1] + 13 * d[1])
        base = (hand[0] + 2 * d[0], hand[1] + 2 * d[1])
        stroke(img, pommel, hand, 1, WRAP)
        for (x, y) in line_points(*base, *tip):
            put(img, x, y, BLADE)
        # filo en sombra del lado de abajo
        for (x, y) in line_points(base[0] - d[1], base[1] + d[0], tip[0] - d[1] * 0.4, tip[1] + d[0] * 0.4):
            put(img, x, y, BLADE_SH)
        # guarda perpendicular
        g = (hand[0] + 1.5 * d[0], hand[1] + 1.5 * d[1])
        for k in (-2, -1, 0, 1, 2):
            put(img, round(g[0] + k * d[1]), round(g[1] - k * d[0]), GOLD)

    # ----- efectos
    img = cels[L["fx"]]
    for fx in pose.get("fx", []):
        kind = fx[0]
        if kind == "arc":  # ("arc", cx, cy, r_in, r_out, ang_desde, ang_hasta, color, color_borde)
            _, cx, cy, r0, r1, a0, a1, col, edge = fx
            lo, hi = min(a0, a1), max(a0, a1)
            for y in range(H):
                for x in range(W):
                    dx, dyy = x + 0.5 - cx, cy - (y + 0.5)
                    dist = math.hypot(dx, dyy)
                    if r0 <= dist <= r1 and lo <= math.degrees(math.atan2(dyy, dx)) <= hi:
                        put(img, x, y, edge if dist > r1 - 1.2 else col)
        elif kind == "slash":  # ("slash", cx, cy, radio, ang_desde, ang_hasta, grosor_max, color, borde)
            # Ángulos como los de las extremidades: 0 = abajo, 90 = adelante, 180 = arriba.
            _, cx, cy, r, a_from, a_to, thick, col, edge = fx
            for y in range(H):
                for x in range(W):
                    dx, dyy = x + 0.5 - cx, y + 0.5 - cy
                    dist = math.hypot(dx, dyy)
                    if dist > r + 0.5 or dist < r - thick - 1:
                        continue
                    ang = math.degrees(math.atan2(dx, dyy))  # 0 = abajo, 90 = adelante
                    if ang < -90:
                        ang += 360
                    f = (ang - a_from) / (a_to - a_from) if a_to != a_from else 1
                    if not 0 <= f <= 1:
                        continue
                    t = 1 + (thick - 1) * f
                    if r - t <= dist <= r + 0.5:
                        put(img, x, y, edge if dist >= r - 0.6 else col)
        elif kind == "spark":  # ("spark", x, y, tamaño)
            _, x, y, size = fx
            for k in range(-size, size + 1):
                c = WHITE if abs(k) <= 1 else GOLD_HI
                put(img, x + k, y, c)
                put(img, x, y + k, c)
            for k in range(1, size // 2 + 1):
                for sx, sy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
                    put(img, x + sx * k, y + sy * k, GOLD)
        elif kind == "dots":  # ("dots", [(x, y, color), ...])
            for (x, y, c) in fx[1]:
                put(img, x, y, c)
        elif kind == "lines":  # ("lines", [(x0, x1, y, color), ...])
            for (x0, x1, y, c) in fx[1]:
                for x in range(x0, x1 + 1):
                    put(img, x, y, c)

    # ----- contorno de 1 px alrededor de la silueta (sin los efectos)
    outline([cels[i] for i in range(1, L["fx"])], cels[L["outline"]], OUTLINE)
    return cels


def ground_hip(far, near, base=(CX, 45)):
    """Ajusta la altura de la cadera para que el pie más bajo toque el piso."""
    lowest = max(limb(base, *far, 9, 9)[1][1], limb(base, *near, 9, 9)[1][1])
    return (base[0], base[1] + (GROUND - 1 - round(lowest)))


# ---------------------------------------------------------------- animaciones

def idle_frames():
    frames = []
    # respiración: el pecho baja 1 px a mitad del ciclo; el pelo acompaña con retraso
    for i, (dy, hair) in enumerate([(0, 0), (0, 0), (1, 1), (1, 1)]):
        far, near = (-4, -4), (4, 2)
        pose = {
            "hip": ground_hip(far, near), "torso_dy": dy,
            "far_leg": far, "near_leg": near,
            "far_arm": (-6, 4), "near_arm": (4, 10),
            "hair": hair, "hem": 0,
        }
        frames.append({"duration": 180, "cels": draw_pose(pose)})
    return frames


def run_frames():
    frames = []
    for i in range(8):
        p = i / 8 * 2 * math.pi
        legs = []
        for phase in (p, p + math.pi):  # pierna lejana y cercana, en oposición
            thigh = 42 * math.sin(phase)
            swing = max(0.0, math.cos(phase))          # pierna yendo hacia adelante
            knee = 10 + 85 * swing + 25 * max(0.0, -math.sin(phase))
            legs.append((thigh, thigh - knee))
        far, near = legs
        arm = 45 * math.sin(p)
        pose = {
            "hip": ground_hip(far, near), "torso_dy": 0, "lean": 1,
            "far_leg": far, "near_leg": near,
            "far_arm": (arm, arm + 80), "near_arm": (-arm, -arm + 80),
            "hair": 2 + (i % 2), "hem": -2 - (i % 2),
        }
        frames.append({"duration": 80, "cels": draw_pose(pose)})
    return frames


def pose(far, near, far_arm, near_arm, hip=None, **extra):
    p = {"hip": hip or ground_hip(far, near), "torso_dy": 0, "far_leg": far, "near_leg": near,
         "far_arm": far_arm, "near_arm": near_arm}
    p.update(extra)
    return p


def frame(duration, p):
    return {"duration": duration, "cels": draw_pose(p)}


def shoulder(p):
    """Hombro en coordenadas del lienzo, para ubicar los tajos."""
    return CX + p.get("lean", 0) + 0.5, p["hip"][1] - 14 + p["torso_dy"] + 2.5


STANCE = ((-18, -10), (22, 10))  # piernas abiertas para atacar


def attack_01_frames():
    """Tajo descendente hacia adelante. 70 + 40 + 40 + 90 + 80 + 80 = 400 ms
    (anticipación 0.07 s, golpe activo 0.08 s, recuperación 0.25 s en PlayerStats)."""
    far, near = STANCE
    keys = [
        (70, dict(far_arm=(-30, -10), near_arm=(160, 190), sword=165, lean=-1)),
        (40, dict(far_arm=(-40, -20), near_arm=(110, 100), sword=100, lean=2, hair=2, hem=-2,
                  slash=(160, 100, 5, BLADE, ROBE_HI))),
        (40, dict(far_arm=(-40, -25), near_arm=(70, 60), sword=50, lean=2, hair=2, hem=-2,
                  slash=(115, 45, 5, BLADE, ROBE_HI))),
        (90, dict(far_arm=(-30, -15), near_arm=(45, 35), sword=35, lean=1, hair=1,
                  slash=(60, 30, 2, ROBE_SH, ROBE_SH))),
        (80, dict(far_arm=(-20, -5), near_arm=(30, 25), sword=25, lean=1, hair=1)),
        (80, dict(far_arm=(-10, 0), near_arm=(15, 15), sword=12)),
    ]
    return [attack_frame(d, far, near, k) for d, k in keys]


def attack_02_frames():
    """Tajo ascendente de vuelta. Mismas duraciones que attack_01."""
    far, near = (-20, -12), (25, 15)
    keys = [
        (70, dict(far_arm=(-20, 0), near_arm=(-35, -15), sword=-30, lean=-1)),
        (40, dict(far_arm=(-35, -20), near_arm=(75, 85), sword=90, lean=2, hair=2, hem=-2,
                  slash=(15, 90, 5, BLADE, ROBE_HI))),
        (40, dict(far_arm=(-45, -30), near_arm=(130, 150), sword=150, lean=2, hair=3, hem=-3,
                  slash=(75, 150, 5, BLADE, ROBE_HI))),
        (90, dict(far_arm=(-35, -20), near_arm=(120, 135), sword=140, lean=1, hair=2,
                  slash=(125, 150, 2, ROBE_SH, ROBE_SH))),
        (80, dict(far_arm=(-20, -10), near_arm=(80, 90), sword=95, lean=1, hair=1)),
        (80, dict(far_arm=(-10, 0), near_arm=(35, 40), sword=40)),
    ]
    return [attack_frame(d, far, near, k) for d, k in keys]


def attack_frame(duration, far, near, k):
    slash = k.pop("slash", None)
    p = pose(far, near, **k)
    if slash:
        sx, sy = shoulder(p)
        a_from, a_to, thick, col, edge = slash
        p["fx"] = [("slash", sx, sy, 21, a_from, a_to, thick, col, edge)]
    return frame(duration, p)


def dash_frames():
    """Dash de 0.16 s: 50 + 60 + 50 ms."""
    frames = []
    for i, (d, streak) in enumerate([(50, 6), (60, 10), (50, 7)]):
        p = pose((-55, -35), (45, 5), (-75, -60), (-60, -35), lean=3, hair=4, hem=-4)
        sx, sy = shoulder(p)
        y0 = round(sy)
        p["fx"] = [("lines", [(CX - 8 - streak, CX - 9, y0 - 4, HAIR_HI),
                              (CX - 6 - streak, CX - 8, y0 + 3, ROBE_SH),
                              (CX - 9 - streak, CX - 10, y0 + 9, ROBE_SH),
                              (CX - 5 - streak // 2, CX - 7, y0 + 15, HAIR_SH)])]
        frames.append(frame(d, p))
    return frames


AIR_HIP = (CX, 44)


def jump_frames():
    return [frame(100, pose((-25, -60), (55, -5), (-40, -15), (110, 130), hip=AIR_HIP, hair=1, hem=1)),
            frame(100, pose((-20, -55), (50, 0), (-45, -20), (115, 140), hip=AIR_HIP, hair=0, hem=1))]


def fall_frames():
    return [frame(100, pose((-12, -30), (22, 0), (-110, -130), (100, 120), hip=AIR_HIP, hair=-1, hem=2)),
            frame(100, pose((-10, -25), (18, 5), (-115, -140), (105, 125), hip=AIR_HIP, hair=-2, hem=2))]


def wall_slide_frames():
    """Deslizándose de espaldas a la pared (que queda atrás, a la izquierda del
    lienzo): la mano lejana raspa la piedra y caen piedritas."""
    frames = []
    for i in range(2):
        p = pose((-40, -5), (35, -30), (25, 50), (-125, -155), hip=AIR_HIP, lean=-1, hair=-1 - i, hem=1)
        wall_x = CX - 8
        hip_y = AIR_HIP[1]
        p["fx"] = [("dots", [(wall_x + 1, hip_y - 10 + i * 3, ROBE_SH), (wall_x, hip_y - 4 + i * 4, HAIR_SH),
                             (wall_x + 2, hip_y + 2 + i * 3, ROBE_SH), (wall_x + 1, hip_y + 8 - i * 2, HAIR_DK)])]
        frames.append(frame(120, p))
    return frames


BLOCK_POSE = dict(far=(-20, -12), near=(18, 8), far_arm=(70, 130), near_arm=(85, 140), sword=150)


def block_frames():
    """Entra en guardia y queda quieto en el último frame."""
    b = BLOCK_POSE
    return [frame(50, pose(b["far"], b["near"], (20, 60), (40, 100), sword=120, lean=0)),
            frame(100, pose(b["far"], b["near"], b["far_arm"], b["near_arm"], sword=b["sword"], lean=-1))]


def parry_frames():
    """Destello del parry exitoso sobre la hoja."""
    b = BLOCK_POSE
    frames = []
    for d, size, tilt in [(40, 5, 140), (60, 7, 135), (100, 3, 148)]:
        p = pose(b["far"], b["near"], b["far_arm"], b["near_arm"], sword=tilt, lean=-1, hair=1)
        sx, sy = shoulder(p)
        a = math.radians(tilt)
        _, hand = limb((sx - 0.5, sy - 0.5), *b["near_arm"], 6, 5)
        p["fx"] = [("spark", round(hand[0] + 8 * math.sin(a)), round(hand[1] + 8 * math.cos(a)), size)]
        frames.append(frame(d, p))
    return frames


def heal_frames():
    """0.6 s de canalización (vulnerable) + 0.2 s de recuperación. La vida se
    recupera al empezar el frame 7, junto con el destello."""
    frames = []
    far, near = (-4, -4), (4, 2)
    for i in range(8):
        casting = i < 6
        flash = i == 6
        arms = ((30, 150), (45, 160)) if (casting or flash) else ((10, 60), (20, 80))
        p = pose(far, near, arms[0], arms[1], torso_dy=1 if casting else 0, lean=-1 if casting else 0,
                 hair=i % 2)
        sx, sy = shoulder(p)
        hx_, hy_ = round(sx + 3), round(sy + 3)
        dots = []
        if casting:
            # brillo en las manos y motas de luz que suben
            for ox in (0, 1):
                for oy in (0, 1):
                    dots.append((hx_ + ox, hy_ + oy, WHITE if (i + ox + oy) % 2 else GOLD_HI))
            for k in range(5):
                t = (i + k * 3) % 8
                x = hx_ - 4 + (k * 5 + i * 2) % 10
                y = hy_ - 1 - t * 2
                c = GOLD_HI if t < 4 else GOLD
                dots += [(x, y, c), (x + 1, y, c), (x, y - 1, c), (x + 1, y - 1, c)] if t < 3 else [(x, y, c)]
        p["fx"] = [("dots", dots)] if dots else []
        if flash:
            p["fx"].append(("arc", sx, sy + 4, 7, 9, -180, 180, GOLD_HI, GOLD))
        frames.append(frame(100, p))
    return frames


def hurt_frames():
    """0.3 s de retroceso."""
    return [frame(150, pose((-25, -15), (12, 0), (-70, -40), (-50, 10), lean=-2, torso_dy=1, hair=3, hem=2)),
            frame(150, pose((-20, -12), (10, 2), (-50, -20), (-30, 20), lean=-1, torso_dy=1, hair=2, hem=1))]


ANIMATIONS = [
    # nombre, frames, repeticiones (0 = loop)
    ("idle", idle_frames, 0),
    ("run", run_frames, 0),
    ("jump", jump_frames, 0),
    ("fall", fall_frames, 0),
    ("wall_slide", wall_slide_frames, 0),
    ("attack_01", attack_01_frames, 1),
    ("attack_02", attack_02_frames, 1),
    ("dash", dash_frames, 1),
    ("block", block_frames, 1),
    ("parry", parry_frames, 1),
    ("heal", heal_frames, 1),
    ("hurt", hurt_frames, 1),
]


def main(out_dir, preview_dir=None):
    frames, tags, groups = [], [], []
    for name, build, repeat in ANIMATIONS:
        seq = build()
        tags.append((name, len(frames), len(frames) + len(seq) - 1, repeat))
        groups.append((name, seq))
        frames += seq
    path = os.path.join(out_dir, "player.aseprite")
    write_aseprite(path, W, H, LAYERS, frames, tags, PALETTE)
    print("ok", path, len(frames), "frames,", len(tags), "animaciones")

    # Vistas previas junto al script, fuera de Assets.
    preview_dir = preview_dir or os.path.join(os.path.dirname(os.path.abspath(__file__)), "previews")
    os.makedirs(preview_dir, exist_ok=True)

    def compose(fr):
        img = Image.new("RGBA", (W, H), (40, 44, 60, 255))
        for i in range(len(LAYERS)):
            img.alpha_composite(fr["cels"][i])
        return img

    rows = []
    for name, seq in groups:
        row = Image.new("RGBA", (W * 8, H), (30, 33, 45, 255))
        for i, fr in enumerate(seq[:8]):
            row.alpha_composite(compose(fr), (i * W, 0))
        rows.append(row)
        imgs = [compose(fr).resize((W * 5, H * 5), Image.NEAREST).convert("P") for fr in seq]
        imgs[0].save(os.path.join(preview_dir, f"{name}.gif"), save_all=True, append_images=imgs[1:],
                     duration=[fr["duration"] for fr in seq], loop=0)
    sheet = Image.new("RGBA", (W * 8, H * len(rows)))
    for r, row in enumerate(rows):
        sheet.alpha_composite(row, (0, r * H))
    sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(os.path.join(preview_dir, "sheet.png"))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__)))
