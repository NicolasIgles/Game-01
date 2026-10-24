# Arte provisorio generado por código

`make_player.py` genera `Assets/Art/Characters/Player/player.aseprite`: el
protagonista en forma humana (fase 1 del GDD) con las animaciones `idle` (4
frames) y `run` (8 frames). Sirve para probar escala, proporciones y timing en
el juego. El arte final se dibuja a mano en Aseprite sobre este archivo o desde
cero, respetando lo mismo:

- Lienzo de 96×64, pies en la última fila, cuerpo centrado en x = 48.
- Cuerpo de ~44 px de alto, mirando a la derecha.
- Un tag por animación; Unity crea un clip por tag.
- Capas separadas: contorno, brazo y pierna lejanos, pierna cercana, túnica,
  cabeza, pelo, brazo cercano.

Para regenerarlo (necesita Python 3 y Pillow):

    python make_player.py ../../Assets/Art/Characters/Player
