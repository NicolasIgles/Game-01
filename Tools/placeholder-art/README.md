# Arte provisorio generado por código

`make_player.py` genera `Assets/Art/Characters/Player/player.aseprite`: el
protagonista en forma humana (fase 1 del GDD) con 12 animaciones. Sirve para
probar escala, proporciones y timing en el juego.

| Tag | Frames | Duración | Repite |
|---|---|---|---|
| idle | 4 | 720 ms | sí |
| run | 8 | 480 ms | sí |
| jump / fall | 2 / 2 | 200 ms | sí |
| wall_slide | 2 | 240 ms | sí |
| attack_01 / attack_02 | 6 / 6 | 400 ms | no |
| dash | 3 | 160 ms | no |
| block | 2 | 150 ms | no (queda en la guardia) |
| parry | 3 | 200 ms | no |
| heal | 8 | 800 ms | no |
| hurt | 2 | 300 ms | no |

Ritmo: dentro de cada ataque el tiempo no se reparte parejo. La anticipación
se demora y termina en un tirón, el golpe es un frame corto con estela y la
recuperación arranca rápida y se asienta lenta. Respetar ese contraste al
dibujar el arte final.

Las duraciones de ataque, dash, cura y daño coinciden con `PlayerStats`
(anticipación + golpe activo + recuperación). Si se cambian esos valores, hay
que ajustar los frames para que la animación y la hitbox sigan sincronizadas. El arte final se dibuja a mano en Aseprite sobre este archivo o desde
cero, respetando lo mismo:

- Lienzo de 96×64, pies en la última fila, cuerpo centrado en x = 48.
- Cuerpo de ~44 px de alto, mirando a la derecha.
- Un tag por animación; Unity crea un clip por tag.
- Capas separadas: contorno, brazo y pierna lejanos, pierna cercana, túnica,
  cabeza, pelo, brazo cercano, espada y efectos (sin contorno).
- En Aseprite, "repeat" del tag en 0 = loop; 1 = se reproduce una vez.

## Necrófago (`make_ghoul.py`)

Genera `Assets/Art/Enemies/Ghoul/ghoul.aseprite`: enemigo común del Bosque de
la Caída. Lienzo de 64×48, cuerpo encorvado de ~28 px, pies en la fila 47.

| Tag | Frames | Duración | Repite |
|---|---|---|---|
| idle | 4 | 800 ms | sí |
| walk | 6 | 720 ms | sí |
| attack | 7 | 1200 ms (0.5 aviso + 0.1 golpe + 0.6 recuperación) | no |
| hurt | 2 | 250 ms | no |
| stunned | 4 | 600 ms | sí |
| death | 6 | 890 ms | no |

Los tiempos del ataque y del daño tienen que coincidir con
`Assets/Data/Enemies/GhoulStats`.

## Azazel (`make_azazel.py`)

Genera tres archivos en `Assets/Art/Bosses/Azazel`:

- `azazel.aseprite`: cuerpo, 112×96, pies en la fila 95, cuerpo de ~74 px.
- `azazel_wings.aseprite`: alas de espíritus de la fase 2, 160×112, mismo pivote
  (pies) que el cuerpo; es un objeto aparte que se activa en la fase 2.
- `azazel_spirit.aseprite`: espíritu de 16×16 (tags float, flash, marker);
  Unity le pone el pivote al centro.

| Tag | Duración | Repite |
|---|---|---|
| idle, walk, charge, stunned, hover | libre | sí |
| claw | 1420 ms (0.6 aviso + 0.12 golpe + 0.7 recuperación) | no |
| claw_fast | 970 ms (0.35 + 0.12 + 0.5) | no |
| summon | 800 ms | no |
| charge_windup | 700 ms | no |
| roar | 1500 ms | no |
| death | 1350 ms | no |

Los tiempos tienen que coincidir con `Assets/Data/Enemies/AzazelBossStats`. En la
fase 2 el código acelera las animaciones de ataque en la misma proporción que los
tiempos (`phase2TimeMultiplier`).

## Efectos de sonido (`make_sfx.py`)

Sintetiza 16 efectos provisorios en `Assets/Audio/SFX` (WAV mono 44.1 kHz):
swing, hit, hurt, block, parry, dash, jump, land, heal_start, heal,
enemy_windup, enemy_windup_unblockable, enemy_death, player_death, boss_roar y
spirit_launch. Necesita
numpy. Para reemplazar uno por el definitivo, se guarda con el mismo nombre;
Unity lo asigna solo en `Assets/Resources/Feedback/FeedbackSettings`.

## Regenerar

Necesita Python 3 y Pillow. `pixeldraw.py` tiene las utilidades de dibujo
compartidas y `aseprite_writer.py` escribe el formato .aseprite.

    python make_player.py ../../Assets/Art/Characters/Player
    python make_ghoul.py ../../Assets/Art/Enemies/Ghoul
    python make_azazel.py ../../Assets/Art/Bosses/Azazel
    python make_sfx.py ../../Assets/Audio/SFX
