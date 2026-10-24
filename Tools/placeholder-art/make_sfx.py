"""Efectos de sonido provisorios, sintetizados por código (numpy).

Genera WAV mono de 44.1 kHz / 16 bit en la carpeta indicada. Son placeholders
para probar la sensación del combate; se reemplazan por sonidos finales con
el mismo nombre de archivo y Unity los vuelve a asignar solo.

    python make_sfx.py ../../Assets/Audio/SFX
"""
import os
import sys
import wave

import numpy as np

SR = 44100
rng = np.random.default_rng(7)


def t(dur):
    return np.arange(int(SR * dur)) / SR


def env(n, attack=0.005, decay=None, curve=4.0):
    """Envolvente: ataque lineal y caída exponencial."""
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    total = n / SR
    d = np.exp(-curve * x / (decay or total))
    return a * d


def noise(dur):
    return rng.uniform(-1, 1, int(SR * dur))


def lowpass(x, cutoff, order=2):
    """Pasabajos de `order` polos en cascada; `cutoff` puede variar en el tiempo."""
    cutoff = np.broadcast_to(cutoff, x.shape)
    a = 1 - np.exp(-2 * np.pi * cutoff / SR)
    y = np.asarray(x, dtype=float)
    for _ in range(order):
        out = np.zeros_like(y)
        acc = 0.0
        for i in range(len(y)):
            acc += a[i] * (y[i] - acc)
            out[i] = acc
        y = out
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def sweep(f0, f1, dur, shape="sine"):
    tt = t(dur)
    f = np.geomspace(f0, f1, len(tt))
    phase = 2 * np.pi * np.cumsum(f) / SR
    if shape == "square":
        return np.sign(np.sin(phase))
    if shape == "saw":
        return 2 * ((phase / (2 * np.pi)) % 1) - 1
    return np.sin(phase)


def bell(freqs, dur, decays, amps):
    tt = t(dur)
    out = np.zeros_like(tt)
    for f, d, a in zip(freqs, decays, amps):
        out += a * np.sin(2 * np.pi * f * tt) * np.exp(-tt / d)
    return out


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[: len(p)] += p
    return out


def finish(x, peak=0.9, fade=0.01):
    x = x - np.mean(x)
    m = np.max(np.abs(x)) or 1
    x = x / m * peak
    f = int(SR * fade)
    x[-f:] *= np.linspace(1, 0, f)
    return x


# ---------------------------------------------------------------- sonidos

def swing():  # tajo de espada: soplido que sube y baja
    d = 0.16
    n = noise(d)
    cut = np.concatenate([np.geomspace(800, 5000, int(SR * d * 0.4)), np.geomspace(5000, 900, int(SR * d) - int(SR * d * 0.4))])
    x = highpass(lowpass(n, cut), 300) * env(len(n), attack=0.03, curve=3)
    return finish(x, 0.6)


def hit():  # golpe que conecta: impacto sordo + chasquido
    thud = sweep(160, 50, 0.14) * env(int(SR * 0.14), 0.001, curve=5)
    crack = lowpass(highpass(noise(0.04), 1500), 5000) * env(int(SR * 0.04), 0.001, curve=8)
    flesh = lowpass(noise(0.12), 900) * env(int(SR * 0.12), 0.002, curve=6) * 0.8
    return finish(mix(thud, crack * 0.35, flesh))


def hurt():  # el jugador recibe daño: golpe grave y saturado
    thud = sweep(110, 35, 0.25) * env(int(SR * 0.25), 0.001, curve=4)
    grit = lowpass(noise(0.2), 900) * env(int(SR * 0.2), 0.001, curve=5)
    x = np.tanh(3 * mix(thud, grit * 0.7))
    return finish(x)


def block():  # metal contra garra: golpe metálico corto
    ring = bell([620, 1340, 2210, 3150], 0.3, [0.08, 0.06, 0.04, 0.03], [1, 0.6, 0.4, 0.25])
    clank = highpass(noise(0.04), 1500) * env(int(SR * 0.04), 0.001, curve=7)
    return finish(mix(ring, clank))


def parry():  # parry: campanazo brillante y largo, con brillo agudo
    ring = bell([880, 1760, 2637, 3520, 4699], 0.7, [0.35, 0.25, 0.18, 0.12, 0.08], [1, 0.7, 0.5, 0.35, 0.3])
    shimmer = sweep(2400, 4800, 0.4) * env(int(SR * 0.4), 0.005, curve=5) * 0.25
    clank = highpass(noise(0.03), 2500) * env(int(SR * 0.03), 0.001, curve=7)
    return finish(mix(ring, shimmer, clank))


def dash():  # soplido corto y grave
    d = 0.18
    n = noise(d)
    x = lowpass(n, np.geomspace(2500, 400, len(n))) * env(len(n), attack=0.01, curve=3)
    return finish(x, 0.6)


def jump():  # tela y pie: soplido muy corto
    n = noise(0.08)
    return finish(lowpass(n, 1200) * env(len(n), 0.002, curve=6), 0.4)


def land():  # caída: golpe sordo de tierra
    thud = sweep(90, 40, 0.1) * env(int(SR * 0.1), 0.001, curve=6)
    dirt = lowpass(noise(0.09), 450) * env(int(SR * 0.09), 0.001, curve=6)
    return finish(mix(thud, dirt * 0.8), 0.55)


def heal_start():  # canalización: brillo que sube durante 0.6 s
    d = 0.6
    tone = sum(sweep(f, f * 1.5, d) * a for f, a in ((523, 1), (659, 0.6), (784, 0.4)))
    x = tone * np.linspace(0.1, 1, int(SR * d)) * env(int(SR * d), 0.2, curve=0.6)
    return finish(x, 0.45)


def heal():  # vida recuperada: acorde de campana
    return finish(bell([1047, 1319, 1568, 2093], 0.7, [0.4, 0.35, 0.3, 0.2], [1, 0.7, 0.6, 0.3]), 0.6)


def enemy_windup():  # aviso del enemigo: gruñido áspero que crece
    d = 0.45
    growl = sweep(70, 110, d, "saw") * (1 + 0.5 * np.sin(2 * np.pi * 23 * t(d)))
    rasp = lowpass(noise(d), 1200) * 0.5
    x = lowpass(mix(growl, rasp), 1500) * np.linspace(0.2, 1, int(SR * d)) * env(int(SR * d), 0.05, curve=0.8)
    return finish(np.tanh(2 * x), 0.7)


def enemy_windup_unblockable():  # ataque imparable: chirrido disonante
    d = 0.5
    a = sweep(600, 1400, d, "saw")
    b = sweep(640, 1500, d, "saw")
    x = lowpass(mix(a, b) * 0.5, 3000) * np.linspace(0.3, 1, int(SR * d)) * env(int(SR * d), 0.03, curve=0.6)
    return finish(np.tanh(2 * x), 0.65)


def enemy_death():  # gemido que cae y se deshace en polvo
    d = 0.7
    groan = sweep(220, 60, d, "saw") * env(int(SR * d), 0.02, curve=2.5)
    dust = lowpass(noise(d), np.geomspace(3000, 300, int(SR * d))) * env(int(SR * d), 0.1, curve=3) * 0.6
    return finish(lowpass(mix(groan, dust), 1800))


def player_death():  # tono grave que se apaga
    d = 1.0
    x = mix(sweep(220, 55, d) * 0.8, sweep(330, 82, d) * 0.4) * env(int(SR * d), 0.01, curve=2.5)
    return finish(x, 0.7)


SOUNDS = {
    "swing": swing, "hit": hit, "hurt": hurt, "block": block, "parry": parry,
    "dash": dash, "jump": jump, "land": land, "heal_start": heal_start, "heal": heal,
    "enemy_windup": enemy_windup, "enemy_windup_unblockable": enemy_windup_unblockable,
    "enemy_death": enemy_death, "player_death": player_death,
}


def write(path, x):
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def main(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for name, fn in SOUNDS.items():
        x = fn()
        write(os.path.join(out_dir, name + ".wav"), x)
        print(f"{name:26s} {len(x) / SR:.2f} s")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "sfx")
