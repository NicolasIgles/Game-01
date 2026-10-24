"""Necrófago del Bosque de la Caída (enemigo común cuerpo a cuerpo).

Aldeano corrompido: encorvado, brazos largos con garras, piel gris hueso con
venas verdes y ojos verde tóxico. Canvas 64x48, pies en la fila 47, centro en
x=32 (pivote abajo al centro). Mira a la derecha. Paleta de la Tierra del GDD.

Duraciones sincronizadas con GhoulStats: ataque = 0.5 s de anticipación +
0.1 s activo + 0.6 s de recuperación; daño 0.25 s.
"""
import math
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from aseprite_writer import write_aseprite
from pixeldraw import blank, hx, limb, outline, poly, put, rect, stroke

W, H = 64, 48
GROUND = 47
CX = 32

OUTLINE = hx("0A0A14")
SKIN, SKIN_SH, SKIN_HI = hx("8A8A70"), hx("5A5A48"), hx("A8A890")
VEIN = hx("3A5A20")
EYE, EYE_HI = hx("60C820"), hx("C8FF90")
CLOTH, CLOTH_SH = hx("3A3A40"), hx("26262C")
CLAW, BLOOD = hx("E8E8D0"), hx("901820")
HAIR = hx("1A2A10")
WHITE = hx("FFFFF0")
PALETTE = [OUTLINE, SKIN, SKIN_SH, SKIN_HI, VEIN, EYE, EYE_HI, CLOTH, CLOTH_SH, CLAW, BLOOD, HAIR, WHITE]

LAYERS = ["outline", "far_arm", "far_leg", "near_leg", "body", "head", "near_arm", "fx"]
L = {n: i for i, n in enumerate(LAYERS)}

THIGH, SHIN = 6, 7
SPINE = 11
UPPER_ARM, FOREARM = 7, 8


def new():
    return blank(W, H)


def spine_top(p):
    a = math.radians(p["hunch"])
    hip = p["hip"]
    return hip[0] + SPINE * math.sin(a), hip[1] - SPINE * math.cos(a)


def draw_pose(p):
    cels = {i: new() for i in range(len(LAYERS))}
    hip = p["hip"]
    a = math.radians(p["hunch"])
    fwd = (math.sin(a), -math.cos(a))           # dirección de la columna (hacia el hombro)
    side = (math.cos(a), math.sin(a))           # perpendicular, hacia el pecho
    sh = spine_top(p)

    # ----- piernas flacas con rodillas flexionadas
    for name, (at, as_), (c, toe) in (
        ("far_leg", p["far_leg"], (SKIN_SH, SKIN_SH)),
        ("near_leg", p["near_leg"], (SKIN, CLAW)),
    ):
        img = cels[L[name]]
        h = (hip[0] + (-1 if name == "far_leg" else 0), hip[1])
        knee, ankle = limb(h, at, as_, THIGH, SHIN)
        stroke(img, h, knee, 2, c)
        stroke(img, knee, ankle, 1, c)
        ax, ay = round(ankle[0]), round(ankle[1])
        rect(img, ax - 1, ay, ax + 1, ay, c)
        put(img, ax + 2, ay, toe)

    # ----- torso encorvado + taparrabos rotoss
    img = cels[L["body"]]
    stroke(img, hip, sh, 5, SKIN)
    # espalda en sombra y vértebras marcadas
    for k in range(1, SPINE):
        x = hip[0] + fwd[0] * k - side[0] * 2.5
        y = hip[1] + fwd[1] * k - side[1] * 2.5
        put(img, round(x), round(y), SKIN_SH)
        if k % 3 == 0:
            put(img, round(x - side[0]), round(y - side[1]), SKIN_HI)
    # costillas y venas en el pecho
    for k in (4, 6, 8):
        x = hip[0] + fwd[0] * k + side[0] * 1.5
        y = hip[1] + fwd[1] * k + side[1] * 1.5
        put(img, round(x), round(y), SKIN_SH)
        put(img, round(x + side[0]), round(y + side[1]), SKIN_SH)
    put(img, round(hip[0] + fwd[0] * 7 + side[0] * 0.5), round(hip[1] + fwd[1] * 7 + side[1] * 0.5), VEIN)
    put(img, round(hip[0] + fwd[0] * 8 + side[0] * 1), round(hip[1] + fwd[1] * 8 + side[1] * 1), VEIN)
    # taparrabos
    hx_, hy_ = round(hip[0]), round(hip[1])
    flutter = p.get("cloth", 0)
    poly(img, [(hx_ - 3, hy_ - 2), (hx_ + 3, hy_ - 2), (hx_ + 3, hy_ + 3), (hx_ + 1, hy_ + 5),
               (hx_ - 1, hy_ + 3), (hx_ - 3 + flutter, hy_ + 5)], CLOTH)
    for x in range(hx_ - 3, hx_ + 4):
        put(img, x, hy_ - 2, CLOTH_SH)
    put(img, hx_ - 3 + flutter, hy_ + 5, CLOTH_SH)

    # ----- cabeza adelantada y baja
    img = cels[L["head"]]
    hc = (sh[0] + side[0] * 3 + fwd[0] * 2, sh[1] + side[1] * 3 + fwd[1] * 2 + p.get("head_dy", 0))
    x0, y0 = round(hc[0]) - 3, round(hc[1]) - 3
    rect(img, x0, y0, x0 + 5, y0 + 5, SKIN)
    rect(img, x0, y0 + 4, x0 + 5, y0 + 5, SKIN_SH)        # mandíbula
    put(img, x0 + 6, y0 + 3, SKIN)                          # hocico
    put(img, x0 + 5, y0 + 5, CLAW)                          # dientes
    put(img, x0 + 3, y0 + 5, CLAW)
    glow = p.get("glow", 0)
    put(img, x0 + 4, y0 + 2, EYE_HI if glow else EYE)
    put(img, x0 + 5, y0 + 2, EYE)
    # mechones oscuros que cuelgan de la nuca
    for k, ln in ((0, 4), (1, 6), (2, 3)):
        for t in range(ln):
            put(img, x0 + k - (t // 2) - p.get("hair", 0) * (t // 3), y0 + t, HAIR)

    # ----- brazos largos con garras
    for name, (au, al), (c, cl) in (
        ("far_arm", p["far_arm"], (SKIN_SH, SKIN_SH)),
        ("near_arm", p["near_arm"], (SKIN, CLAW)),
    ):
        img = cels[L[name]]
        s0 = (sh[0] + side[0] * 1.5 + (-1 if name == "far_arm" else 0), sh[1] + side[1] * 1.5 + 1)
        elbow, hand = limb(s0, au, al, UPPER_ARM, FOREARM)
        stroke(img, s0, elbow, 2, c)
        stroke(img, elbow, hand, 2 if name == "near_arm" else 1, c)
        d = math.radians(al)
        dx, dy = math.sin(d), math.cos(d)
        for k, spread in ((-1, -0.6), (0, 0), (1, 0.6)):
            tip = (hand[0] + dx * 3 + dy * spread, hand[1] + dy * 3 - dx * spread)
            put(img, round(tip[0]), round(tip[1]), cl)
        if name == "near_arm" and p.get("bloody"):
            put(img, round(hand[0] + dx * 2), round(hand[1] + dy * 2), BLOOD)

    # ----- efectos
    img = cels[L["fx"]]
    for fx in p.get("fx", []):
        if fx[0] == "slash":
            _, cx, cy, r, a_from, a_to, thick = fx
            for y in range(H):
                for x in range(W):
                    dx, dyy = x + 0.5 - cx, y + 0.5 - cy
                    dist = math.hypot(dx, dyy)
                    if dist > r + 0.5 or dist < r - thick - 1:
                        continue
                    ang = math.degrees(math.atan2(dx, dyy))
                    if ang < -90:
                        ang += 360
                    f = (ang - a_from) / (a_to - a_from)
                    if 0 <= f <= 1 and r - (1 + (thick - 1) * f) <= dist:
                        put(img, x, y, WHITE if dist >= r - 0.6 else EYE)
        elif fx[0] == "glint":  # destello en los ojos: aviso del ataque
            _, x, y, size = fx
            for k in range(-size, size + 1):
                c = WHITE if abs(k) <= 1 else EYE_HI
                put(img, x + k, y, c)
                put(img, x, y + k, c)
        elif fx[0] == "dots":
            for (x, y, c) in fx[1]:
                put(img, x, y, c)

    outline([cels[i] for i in range(1, L["fx"])], cels[L["outline"]], OUTLINE)
    return cels


def ground_hip(far, near, x=CX, base_y=34):
    lowest = max(limb((x, base_y), *far, THIGH, SHIN)[1][1], limb((x, base_y), *near, THIGH, SHIN)[1][1])
    return (x, base_y + (GROUND - round(lowest)))


def pose(far, near, far_arm, near_arm, hunch=30, x=CX, lift=0, **extra):
    hip = ground_hip(far, near, x)
    p = {"hip": (hip[0], hip[1] - lift), "hunch": hunch, "far_leg": far, "near_leg": near,
         "far_arm": far_arm, "near_arm": near_arm}
    p.update(extra)
    return p


def frame(duration, p):
    return {"duration": duration, "cels": draw_pose(p)}


CROUCH = ((-10, 25), (20, -5))   # rodillas flexionadas, pierna cercana adelante


def head_pos(p):
    a = math.radians(p["hunch"])
    sh = spine_top(p)
    return sh[0] + math.cos(a) * 3 + math.sin(a) * 2, sh[1] + math.sin(a) * 3 - math.cos(a) * 2


# ---------------------------------------------------------------- animaciones

def idle_frames():
    far, near = CROUCH
    out = []
    for i, (hunch, sway, glow) in enumerate([(30, 0, 0), (31, 1, 0), (32, 2, 1), (31, 1, 0)]):
        out.append(frame(200, pose(far, near, (5 - sway, -5 + sway), (12 + sway, 2 + sway), hunch=hunch,
                                   glow=glow, hair=sway // 2)))
    return out


def walk_frames():
    out = []
    for i in range(6):
        ph = i / 6 * 2 * math.pi
        legs = []
        for phase in (ph, ph + math.pi):
            thigh = 8 + 26 * math.sin(phase)
            bend = 20 + 45 * max(0.0, math.cos(phase))
            legs.append((thigh, thigh - bend))
        arm = 18 * math.sin(ph)
        out.append(frame(120, pose(legs[0], legs[1], (5 + arm, -10 + arm), (10 - arm, 0 - arm),
                                   hunch=33 + (i % 3 == 0), cloth=-1 - (i % 2), hair=1)))
    return out


def attack_frames():
    """Anticipación 150+175+175 = 500 ms, golpe 100 ms, recuperación 3×200 = 600 ms."""
    far, near = (-20, 10), (30, 0)
    out = []
    # anticipación: se echa hacia atrás y levanta la garra; los ojos se encienden
    for d, hunch, up, lo, glow in [(230, 20, 150, 175, 0), (190, 10, 170, 200, 1), (80, 5, 180, 210, 1)]:
        p = pose(far, near, (-20, -30), (up, lo), hunch=hunch, glow=glow, hair=1)
        if hunch == 5:
            hxp, hyp = head_pos(p)
            p["fx"] = [("glint", round(hxp + 2), round(hyp - 1), 3)]
        out.append(frame(d, p))
    # golpe: zarpazo descendente con estela
    p = pose((-30, 0), (40, 10), (-30, -40), (85, 70), hunch=50, glow=1, cloth=-2, hair=2, bloody=True)
    sx, sy = spine_top(p)
    p["fx"] = [("slash", sx + 1.5, sy + 1, 15, 190, 75, 4)]
    out.append(frame(100, p))
    # recuperación: el brazo queda colgando adelante y vuelve de a poco
    for d, hunch, up, lo in [(110, 48, 45, 25), (210, 42, 30, 15), (280, 35, 18, 5)]:
        out.append(frame(d, pose((-25, 5), (35, 5), (-10, -20), (up, lo), hunch=hunch, hair=1, bloody=True)))
    return out


def hurt_frames():
    """0.25 s de retroceso."""
    return [frame(100, pose((-15, 15), (10, -5), (-50, -30), (-30, -10), hunch=8, head_dy=-1, hair=2, cloth=2)),
            frame(150, pose((-12, 20), (15, -5), (-20, -15), (0, 5), hunch=18, hair=1, cloth=1))]


def stunned_frames():
    """Aturdido después de un parry: se tambalea con la cabeza gacha."""
    out = []
    for i, (hunch, dx) in enumerate([(55, 0), (60, 1), (55, 0), (50, -1)]):
        p = pose(CROUCH[0], CROUCH[1], (-5, -5), (0, -5), hunch=hunch, x=CX + dx, head_dy=1)
        hxp, hyp = head_pos(p)
        dots = []
        for k in range(3):
            ang = (i / 4 + k / 3) * 2 * math.pi
            dots.append((round(hxp + 4 * math.cos(ang)), round(hyp - 6 + 1.5 * math.sin(ang)),
                         EYE_HI if k == 0 else WHITE))
        p["fx"] = [("dots", dots)]
        out.append(frame(150, p))
    return out


def death_frames():
    out = [frame(100, pose((-15, 15), (10, -5), (-60, -40), (-40, -20), hunch=5, head_dy=-1, hair=2)),
           frame(100, pose((40, -60), (60, -50), (-10, -10), (10, 10), hunch=35)),
           frame(120, pose((80, -90), (85, -85), (20, 30), (40, 50), hunch=70, lift=1))]
    # en el piso: cuerpo tendido y restos que se deshacen en ceniza
    for d, k in [(120, 0), (150, 1), (300, 2)]:
        p = pose((88, -92), (90, -90), (60, 80), (80, 90), hunch=95, lift=3)
        rng = [(-6, -1), (-2, -3), (3, -2), (7, -4), (0, -6), (5, -7)]
        p["fx"] = [("dots", [(CX + x, GROUND - 6 + y - k * 2, CLOTH_SH if (i + k) % 2 else SKIN_SH)
                             for i, (x, y) in enumerate(rng[: 2 + k * 2])])] if k else []
        out.append(frame(d, p))
    return out


ANIMATIONS = [
    ("idle", idle_frames, 0),
    ("walk", walk_frames, 0),
    ("attack", attack_frames, 1),
    ("hurt", hurt_frames, 1),
    ("stunned", stunned_frames, 0),
    ("death", death_frames, 1),
]


def main(out_dir, preview_dir=None):
    frames, tags, groups = [], [], []
    for name, build, repeat in ANIMATIONS:
        seq = build()
        tags.append((name, len(frames), len(frames) + len(seq) - 1, repeat))
        groups.append((name, seq))
        frames += seq
    path = os.path.join(out_dir, "ghoul.aseprite")
    write_aseprite(path, W, H, LAYERS, frames, tags, PALETTE)
    print("ok", path, len(frames), "frames,", len(tags), "animaciones")

    preview_dir = preview_dir or os.path.join(os.path.dirname(os.path.abspath(__file__)), "previews")
    os.makedirs(preview_dir, exist_ok=True)

    def compose(fr):
        img = Image.new("RGBA", (W, H), (40, 44, 60, 255))
        for i in range(len(LAYERS)):
            img.alpha_composite(fr["cels"][i])
        return img

    sheet = Image.new("RGBA", (W * 7, H * len(groups)), (30, 33, 45, 255))
    for r, (name, seq) in enumerate(groups):
        for i, fr in enumerate(seq[:7]):
            sheet.alpha_composite(compose(fr), (i * W, r * H))
        imgs = [compose(fr).resize((W * 6, H * 6), Image.NEAREST).convert("P") for fr in seq]
        imgs[0].save(os.path.join(preview_dir, f"ghoul_{name}.gif"), save_all=True, append_images=imgs[1:],
                     duration=[fr["duration"] for fr in seq], loop=0)
    sheet.resize((sheet.width * 4, sheet.height * 4), Image.NEAREST).save(os.path.join(preview_dir, "ghoul_sheet.png"))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__)))
