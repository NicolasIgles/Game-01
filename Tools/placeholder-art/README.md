# Arte provisorio generado por código

`make_player.py` genera `Assets/Art/Characters/Player/player.aseprite`: el
protagonista en forma humana (fase 1 del GDD) con 11 animaciones. Sirve para
probar escala, proporciones y timing en el juego.

| Tag | Frames | Duración | Repite |
|---|---|---|---|
| idle | 4 | 720 ms | sí |
| run | 8 | 640 ms | sí |
| jump / fall | 2 / 2 | 200 ms | sí |
| attack_01 / attack_02 | 6 / 6 | 400 ms | no |
| dash | 3 | 160 ms | no |
| block | 2 | 150 ms | no (queda en la guardia) |
| parry | 3 | 200 ms | no |
| heal | 8 | 800 ms | no |
| hurt | 2 | 300 ms | no |

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

## Efectos de sonido (`make_sfx.py`)

Sintetiza 14 efectos provisorios en `Assets/Audio/SFX` (WAV mono 44.1 kHz):
swing, hit, hurt, block, parry, dash, jump, land, heal_start, heal,
enemy_windup, enemy_windup_unblockable, enemy_death y player_death. Necesita
numpy. Para reemplazar uno por el definitivo, se guarda con el mismo nombre;
Unity lo asigna solo en `Assets/Resources/Feedback/FeedbackSettings`.

## Regenerar

Necesita Python 3 y Pillow. `pixeldraw.py` tiene las utilidades de dibujo
compartidas y `aseprite_writer.py` escribe el formato .aseprite.

    python make_player.py ../../Assets/Art/Characters/Player
    python make_ghoul.py ../../Assets/Art/Enemies/Ghoul
    python make_sfx.py ../../Assets/Audio/SFX
