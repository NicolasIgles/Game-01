"""Sprite base del protagonista, forma humana (fase 1 del GDD).

Canvas 96x64, pies en la fila 63 y centro en x=48 (pivote abajo al centro).
Cuerpo de ~44 px de alto, mirando a la derecha. Cada parte va en su capa para
poder retocarla en Aseprite o sumar piezas de armadura encima.
"""
import math
import os
import sys
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from aseprite_writer import write_aseprite

W, H = 96, 64
GROUND = 63
CX = 48


def hx(s):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


# Paleta (GDD: paleta de la Tierra + plata del protagonista + contorno no negro)
OUTLINE = hx("0A0A14")
SKIN, SKIN_SH, SKIN_HI = hx("D4A870"), hx("A87850"), hx("E8C498")
HAIR, HAIR_SH, HAIR_HI, HAIR_DK = hx("D0D0E0"), hx("8A8AA0"), hx("F0F0FF"), hx("6A6A80")
ROBE, ROBE_SH, ROBE_DK, ROBE_HI = hx("E8E8F0"), hx("A8A8C0"), hx("6A6A80"), hx("F5F5FF")
PANTS, PANTS_SH = hx("3A3A40"), hx("26262C")
WRAP, WRAP_SH = hx("6A4020"), hx("4A2C16")
ROPE = hx("5A3A20")
EYE = hx("3A3A40")  # fase 1: ojos sin brillo
PALETTE = [OUTLINE, SKIN, SKIN_SH, SKIN_HI, HAIR, HAIR_SH, HAIR_HI, HAIR_DK, ROBE, ROBE_SH,
           ROBE_DK, ROBE_HI, PANTS, PANTS_SH, WRAP, WRAP_SH, ROPE, EYE]

LAYERS = ["outline", "far_arm", "far_leg", "near_leg", "robe", "head", "hair", "near_arm"]
L = {n: i for i, n in enumerate(LAYERS)}


def new():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def line_points(x0, y0, x1, y1):
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


def stroke(img, p0, p1, width, color, shade=None):
    """Trazo grueso. Si hay `shade`, el borde inferior/trasero va más oscuro."""
    px = img.load()
    r = width / 2.0
    for (x, y) in line_points(*p0, *p1):
        for oy in range(-width, width + 1):
            for ox in range(-width, width + 1):
                if (ox + 0.5 - 0.5) ** 2 + (oy) ** 2 <= r * r:
                    X, Y = x + ox, y + oy
                    if 0 <= X < W and 0 <= Y < H:
                        px[X, Y] = color
    if shade:
        for (x, y) in line_points(*p0, *p1):
            X, Y = x - int(r) + 0, y + int(r) - 0
            if 0 <= X < W and 0 <= Y < H and px[X, Y][3]:
                px[X, Y] = shade


def rect(img, x0, y0, x1, y1, color):
    ImageDraw.Draw(img).rectangle([x0, y0, x1, y1], fill=color)


def poly(img, pts, color):
    ImageDraw.Draw(img).polygon([tuple(map(round, p)) for p in pts], fill=color)


def put(img, x, y, color):
    if 0 <= x < W and 0 <= y < H:
        img.putpixel((int(x), int(y)), color)


def limb(hip, a_thigh, a_shin, l1, l2):
    """Ángulos en grados desde la vertical; positivo = hacia adelante (derecha)."""
    t = math.radians(a_thigh)
    knee = (hip[0] + l1 * math.sin(t), hip[1] + l1 * math.cos(t))
    s = math.radians(a_shin)
    ankle = (knee[0] + l2 * math.sin(s), knee[1] + l2 * math.cos(s))
    return knee, ankle


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

    # ----- contorno de 1 px alrededor de la silueta completa
    union = new()
    for i in range(1, len(LAYERS)):
        union.alpha_composite(cels[i])
    up, op = union.load(), cels[L["outline"]].load()
    for y in range(H):
        for x in range(W):
            if up[x, y][3]:
                continue
            for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                X, Y = x + ox, y + oy
                if 0 <= X < W and 0 <= Y < H and up[X, Y][3]:
                    op[x, y] = OUTLINE
                    break
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


def main(out_dir):
    idle, run = idle_frames(), run_frames()
    frames = idle + run
    tags = [("idle", 0, len(idle) - 1), ("run", len(idle), len(frames) - 1)]
    path = os.path.join(out_dir, "player.aseprite")
    write_aseprite(path, W, H, LAYERS, frames, tags, PALETTE)

    # vistas previas (hoja ×4 y GIFs ×6) junto al script, fuera de Assets
    out_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "previews")
    os.makedirs(out_dir, exist_ok=True)
    def compose(frame):
        img = new()
        for i in range(len(LAYERS)):
            img.alpha_composite(frame["cels"][i])
        return img

    sheet = Image.new("RGBA", (W * len(frames), H), (40, 44, 60, 255))
    for i, fr in enumerate(frames):
        sheet.alpha_composite(compose(fr), (i * W, 0))
    sheet.resize((sheet.width * 4, sheet.height * 4), Image.NEAREST).save(os.path.join(out_dir, "preview_sheet.png"))
    for name, seq in (("idle", idle), ("run", run)):
        imgs = []
        for fr in seq:
            bg = Image.new("RGBA", (W, H), (40, 44, 60, 255))
            bg.alpha_composite(compose(fr))
            imgs.append(bg.resize((W * 6, H * 6), Image.NEAREST).convert("P"))
        imgs[0].save(os.path.join(out_dir, f"preview_{name}.gif"), save_all=True, append_images=imgs[1:],
                     duration=seq[0]["duration"], loop=0)
    print("ok", path, len(frames), "frames")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__)))
