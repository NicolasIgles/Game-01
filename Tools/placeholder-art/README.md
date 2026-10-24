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

Para regenerarlo (necesita Python 3 y Pillow):

    python make_player.py ../../Assets/Art/Characters/Player
