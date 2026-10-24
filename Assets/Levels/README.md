# Salas escritas como texto

Cada zona tiene una carpeta (`Bosque/`) con un archivo `.txt` por sala. El menú
**Tools → Game01 → Construir Bosque de la Caída** los lee en orden alfabético y
arma la escena `Assets/Scenes/Bosque.unity`: tilemaps, fondos, objetos, enemigos,
efectos y límites de cámara. Para cambiar una sala se edita el texto y se vuelve a
construir; la escena no se toca a mano (se pisa en cada construcción).

## Formato

```
nombre: El Cráter
origen: 0 0              tile de abajo a la izquierda, en coordenadas del mundo
fondo: bosque            bosque | cementerio
efectos: ceniza, brasas  ceniza, brasas, fatuos, niebla (separados por coma)
jefe: si                 opcional: cámara fija y puerta que se cierra
texto 1: ...             texto de la inscripción 1 (también: texto S, texto K, texto X)
---
(filas del mapa, la de arriba es la más alta)
```

1 carácter = 1 tile de 16×16 px. Todas las filas tienen el mismo largo. Los objetos
se apoyan sobre el borde inferior de su casilla, así que van en la fila justo
encima del suelo.

## Leyenda

| Carácter | Qué es |
| --- | --- |
| `.` | Vacío |
| `#` | Tierra con pasto (sólido) |
| `R` | Roca (sólido) |
| `C` | Suelo quemado del cráter (sólido) |
| `W` | Pared lisa con vetas de plata: obstáculo del salto de pared (sólido) |
| `=` | Tronco: plataforma que se atraviesa desde abajo |
| `~` | Brasas (peligro) |
| `^` | Estacas quemadas (peligro) |
| `P` | Inicio del jugador |
| `A` | Altar de gracia: guarda, recarga las curas y es el punto de reaparición |
| `F` | Plataforma frágil (escribir `FF`: ocupa 2 tiles) |
| `L` | Palanca: se activa con un golpe y hace aparecer todos los `E` |
| `E` | Escalón de raíces, aparece con la palanca |
| `w` | Saliente de luz, aparece con 1 fragmento (provisorio hasta tener salto de pared) |
| `V` | Velo de espíritus, se abre con 2 fragmentos (columna de 1×4) |
| `B` | Puerta de jefe (bloque de 2×4) |
| `K` | Relicario (usa `texto K`) |
| `1`–`9` | Inscripción (usa `texto 1`…) |
| `S` | Sarah (usa `texto S`) |
| `X` | Fin del slice (usa `texto X`) |
| `h` `c` `f` | Hueco poseído, Cuervo de ceniza, Fuego fatuo |
| `Z` | Azazel |
| `t` `+` | Lápida, cruz |
| `M` `T` `H` | Mausoleo, árbol muerto, cabaña |
| `o` `r` `p` | Tronco caído, roca fundida, plumas quemadas |
| `l` | Farol de espíritu (con luz) |
| `s` | Fuente de humo |

## Reglas de diseño

Las medidas salen de la Biblia de niveles: escalón cómodo ≤ 3 tiles, hueco cómodo
≤ 4, hueco exigente 6–7 (salto + dash), pasillos de al menos 4 de alto y ningún
enemigo a menos de 8 tiles de la entrada de la sala.
