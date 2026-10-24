# GDD Visual — El Ángel Caído
### Guía de Producción de Arte Pixel Art · v1.0
> Basado en análisis de 220+ imágenes de referencia + documentos de historia y diseño

---

## ÍNDICE

1. [Visión General del Estilo](#1-visión-general-del-estilo)
2. [Especificaciones Técnicas de Sprites](#2-especificaciones-técnicas-de-sprites)
3. [Paleta de Colores Global](#3-paleta-de-colores-global)
4. [Personaje Principal — El Guerrero de Luz](#4-personaje-principal--el-guerrero-de-luz)
5. [Antagonistas Principales](#5-antagonistas-principales)
6. [Los 7 Reyes Demonios](#6-los-7-reyes-demonios)
7. [Aliados Humanos](#7-aliados-humanos)
8. [Ambiente y Escenarios](#8-ambiente-y-escenarios)
9. [Sistema de Evolución Visual](#9-sistema-de-evolución-visual)
10. [Pipeline de Producción para Aseprite](#10-pipeline-de-producción-para-aseprite)

---

## 1. Visión General del Estilo

### Referencia cruzada
| Aspecto | Referencia | Aplicación al juego |
|---------|-----------|---------------------|
| Jugabilidad / movimiento | Blasphemous | Peso, hitboxes, animaciones de combate |
| Atmósfera / narrativa | Bloodborne | Ambiente oscuro, lore fragmentado |
| Estética pixel art | Diseños propios + AI concept art | Fusión de lo divino y lo infernal |
| Proporción de personajes | Blasphemous (64px alto) | Base de 64×64 px para personajes |

### Identidad visual del juego
Las imágenes analizadas establecen una identidad muy clara:

- **Pixel art de alta densidad** — no pixel art simple/minimalista. Los sprites tienen gran cantidad de detalle por pixel, comparable a Blasphemous o Shovel Knight pero con más elementos "cósmicos"
- **Dualidad como eje visual** — cada personaje importante combina dos elementos opuestos: luz/oscuridad, divino/infernal, plumas blancas/alas negras, aura dorada/grietas rojas
- **Glow y efectos de energía** — los ojos brillantes (rojo, naranja, azul cyan), los núcleos de luz en el pecho, los halos distorsionados son elementos de identidad únicos del juego
- **Escala épica en los bosses** — los jefes son significativamente más grandes que el jugador, con múltiples capas (cuerpo + alas + tentáculos + satélites de energía), como en los diseños de Metatrón y los Reyes Demonios

---

## 2. Especificaciones Técnicas de Sprites

### Resolución base recomendada

```
Resolución de juego base: 320×180 px (escala 16:9)
Escala de display: ×4 → 1280×720 (HD) · ×6 → 1920×1080 (Full HD)
Pixel size en pantalla: 4px o 6px de lado (se ve limpio, con peso visual)
```

> **Por qué 320×180:** Es el estándar usado por Blasphemous, Dead Cells y similares.
> Da suficiente espacio de pantalla para mostrar al jugador + entorno + enemigos.
> Al escalar ×4 se obtiene 720p exacto, sin interpolación ni blur.

### Tamaño de sprites por categoría

| Tipo de entidad | Canvas en Aseprite | Espacio visual real |
|----------------|-------------------|---------------------|
| Jugador (forma humana) | 32×48 px | Ocupa ~27% de la altura de pantalla |
| Jugador (forma angelical) | 48×64 px | Más grande, con alas que salen del canvas |
| Enemigos comunes | 24×32 px | Más pequeños que el jugador |
| Enemigos élite | 40×56 px | Misma escala que el jugador |
| Jefes medianos (Reyes fase 1) | 96×96 px | 3× el tamaño del jugador |
| Jefes grandes (Reyes fase 2) | 128×144 px | Llena ~80% de la pantalla en altura |
| Jefes épicos (Metatrón, Lucifer) | 160×160 px | Ocupa la pantalla completa |
| NPCs (Sarah, Emma, Grimm) | 28×44 px | Ligeramente más pequeños que el jugador |
| Tiles de escenario | 16×16 px | Unidad base de construcción del mapa |

### Animaciones — cantidad de frames mínima por acción

| Acción | Frames mínimos | Frames ideales |
|--------|---------------|----------------|
| Idle | 4 | 6–8 |
| Caminar | 6 | 8 |
| Correr | 6 | 8 |
| Ataque básico | 5 | 8 |
| Ataque especial / habilidad | 8 | 12 |
| Saltar (subida) | 3 | 4 |
| Caer | 3 | 4 |
| Aterrizar | 2 | 3 |
| Recibir daño | 3 | 4 |
| Morir | 6 | 10 |
| Transformación / cambio de forma | 12 | 16–20 |
| Boss — entrada / intro | 16 | 24 |

---

## 3. Paleta de Colores Global

Las 220+ imágenes analizadas muestran tres paletas consistentes según el plano narrativo.
Cada zona del juego debe usar principalmente su paleta de origen.

### 3.1 Paleta del Cielo — Lo Divino

Usada para el protagonista, ángeles aliados y zonas celestiales.

```
PLATA PURO          #E8E8F0   ← base de armadura angelical, plumas limpias
BLANCO CELESTIAL    #F5F5FF   ← highlights y destellos máximos
AZUL GLACIAL        #8BB8D4   ← alas secundarias, energía divina fría
AZUL PROFUNDO       #2A4A7F   ← sombras de alas, armadura en penumbra
DORADO SAGRADO      #C8A832   ← halo, ornamentos divinos, runes
DORADO CÁLIDO       #E8C860   ← highlight de halo, energía de gracia activa
GRIS ACERO          #6A6A7A   ← articulaciones de armadura, bordes
NEGRO PROFUNDO      #0A0A14   ← contornos principales (no negro puro)
```

### 3.2 Paleta del Infierno — Lo Demoníaco

Usada para los Reyes Demonios y zonas infernales.

```
ROJO LAVA           #CC2200   ← base cuerpo demoníaco, grietas de fuego
ROJO BRILLANTE      #FF4420   ← energía activa, ataques de fuego
NARANJA BRASA       #E85A00   ← degradado en alas de fuego, lava
AMARILLO NÚCLEO     #FFB820   ← núcleos de energía interna, ojos de fuego
MAGENTA OSCURO      #8A0050   ← alas de Lucifer, energía caótica
PÚRPURA ABISMO      #3A0060   ← sombras infernales profundas
MARRÓN HUESO        #5A3A20   ← escamas, cuernos, estructuras óseas
NEGRO ABISMO        #080408   ← fondo de zonas infernales, void
```

### 3.3 Paleta de la Tierra — Lo Mortal

Usada para humanos, NPCs y zonas del mundo mortal.

```
BEIGE PIEL          #D4A870   ← piel humana base
MARRÓN TIERRA       #6A4020   ← ropa de NPCs, madera, tierra
VERDE CORRUPTO      #3A5A20   ← vegetación contaminada por el infierno
VERDE OSCURO        #1A2A10   ← sombras de vegetación, musgo
GRIS PIEDRA         #7A7A80   ← arquitectura en ruinas, castillos
GRIS OSCURO         #3A3A40   ← sombras de arquitectura
AZUL MÁGICO         #2080C0   ← magia de Sarah, agua
ROJO SANGRE         #901820   ← corrupción infernal en la tierra
```

### 3.4 Regla de los colores de acento (IMPORTANTE)

Cada personaje tiene **un color de acento único** que lo identifica en combate y en la narrativa:

| Personaje | Color de acento | HEX | Función |
|-----------|----------------|-----|---------|
| Protagonista (humano) | Rojo carmesí | `#CC1830` | Grietas en su armadura caída |
| Protagonista (angelical) | Azul cyan | `#40D0FF` | Energía de gracia recuperada |
| Lucifer | Rojo-magenta | `#D01850` | Fuego trágico |
| Metatrón | Dorado-blanco | `#FFE880` | Falsa pureza divina |
| Samael | Cyan frío | `#00C8FF` | Tiempo y espacio |
| Azazel | Verde tóxico | `#60C820` | Veneno y traición |
| Iblis | Naranja volcánico | `#FF6800` | Fuego e ira |
| Belial | Oro corrompido | `#C8A000` | Envidia y oro falso |
| Asmodeus | Rosado oscuro | `#D04080` | Seducción y engaño |
| Leviathan | Azul abismo | `#002888` | Profundidad y vacío |

---

## 4. Personaje Principal — El Guerrero de Luz

### 4.1 Descripción visual base (del concept art analizado)

El protagonista está diseñado con una **dualidad visual explícita**: parte angelical (plumas blancas, armadura plateada, aura de luz) y parte corrupta/caída (grietas rojas en la armadura, ojos que brillan en rojo, alas que se fragmentan).

Esta dualidad visual **cambia progresivamente** según la fase de la historia.

### 4.2 Formas del personaje — evolución visual

```
FASE 1 — FORMA HUMANA (inicio del juego)
├── Sin armadura de luz → harapos o ropa humana básica
├── Sin alas
├── Cabello largo plateado/gris
├── Ojos sin brillo (vacíos, perdidos)
├── Complexión atlética pero sin poder visible
└── Canvas: 32×48 px

FASE 2 — FRAGMENTOS DE GRACIA (primeras recuperaciones)
├── Aparecen fragmentos de armadura de luz en hombros y manos
├── Grietas naranjas/doradas en la piel (no rojas aún)
├── Alas mínimas — muñones de plumas apenas visibles
├── Ojos comienzan a brillar levemente en dorado
└── Canvas: 36×52 px

FASE 3 — FORMA SEMI-ANGELICAL (mitad del juego)
├── Armadura de escamas de luz en torso completo
├── Alas desplegadas — blancas con puntas oscuras
├── Grietas rojas en la armadura (tras contacto con el infierno)
├── Ojos brillando en rojo/naranja al activar poderes infernales
└── Canvas: 48×64 px

FASE 4 — FORMA ANGELICAL COMPLETA (final)
├── Armadura completa de luz corpórea con textura de escamas
├── Alas completamente blancas O completamente negras (según decisiones del jugador)
├── Halo presente (completo o fracturado según el camino)
├── Núcleo de luz en el pecho brillando constantemente
└── Canvas: 48×72 px (con alas dentro del canvas +20px lateral)
```

### 4.3 Especificaciones de armadura

Basado en el concept art de referencia (ángel con grietas de rayo rojo):

- **Textura:** Escamas superpuestas, cada escama de 2×3 px en el sprite final
- **Color base:** `#D0D0E0` (plata fría)
- **Highlight:** `#F0F0FF` (blanco casi puro) — solo en el borde superior de cada escama
- **Sombra:** `#6A6A80` — en el borde inferior y laterales
- **Grietas de caída:** color `#CC1830`, ancho de 1px, con un pixel de `#FF8040` al centro para el glow
- **Joints/articulaciones:** `#404050` — partes más oscuras para dar profundidad

### 4.4 Animaciones clave con notas de diseño

**Idle (humano):**
- Respiración lenta, el pecho sube/baja 1px cada 4 frames
- Cabello se mueve suavemente 1px hacia un lado

**Idle (angelical):**
- Alas hacen micro-movimientos (±1px en las puntas)
- El núcleo de pecho pulsa entre `#FFD060` y `#FFFFFF` cada 8 frames
- Partículas de luz de 1px aparecen y desaparecen alrededor

**Ataque básico:**
- Frame 1: Levanta la espada (motion blur hacia arriba — 2px de estela)
- Frame 2–3: Preparación (carga), el arma brilla
- Frame 4–5: Descenso rápido — la estela tiene 4px de ancho y desaparece rápido
- Frame 6: Impacto — flash de 1 frame blanco en el arma
- Frame 7–8: Recovery, vuelve a idle

**Transformación Humano → Angelical:**
- 16 frames mínimo
- La luz "explota" desde el centro del pecho hacia afuera
- Las escamas de armadura aparecen una a una con destellos de 1px
- Las alas se materializan de adentro hacia afuera

---

## 5. Antagonistas Principales

### 5.1 Lucifer — El Rey del Inframundo

**Visual concept:** Figura masculina que refleja lo que fue el protagonista. Trágico y poderoso.

```
Canvas sprite de combate: 128×128 px (boss mediano)
Canvas forma colosal: 160×160 px (fase final)
```

**Paleta específica:**
- Cuerpo: rojo oscuro `#800010` base, `#CC2020` highlight
- Cuernos: negro abismo con degradado a rojo en la base
- Alas: rojo-magenta `#D01850` con puntas negras
- Ojos: blanco puro con pupila en negro — diferente a todos los demás (símbolo de lucidez)
- Núcleo de pecho: como el protagonista, pero en rojo — conexión visual intencional

**Elementos visuales únicos:**
- 4 alas (2 grandes + 2 pequeñas) — diferente al protagonista que tiene 2
- Cuernos con textura de roca volcánica
- Serpientes/tentáculos saliendo de la parte inferior (referencia al diseño analizado)
- Símbolo de infinito formado por las serpientes en la pose idle

### 5.2 Metatrón — El Antagonista Oculto

**El personaje más complejo visualmente.** Parece la máxima expresión de lo divino, pero oculta la corrupción más profunda.

```
Canvas sprite de combate: 96×96 px (fase 1 — disfrazado de aliado)
Canvas forma verdadera: 160×160 px (fase final revelada)
```

**Paleta — Fase Disfrazada (aliado aparente):**
- Armadura: dorado brillante `#C8A832`, muy ornamental
- Alas: blancas perfectas `#F8F8FF` — demasiado perfectas para ser reales
- Ojos: dorado suave `#E8C060` — nunca brilla con rojo
- Geometría sagrada en la armadura: patrones hexagonales en `#FFE880`

**Paleta — Forma Verdadera (boss final):**
- Armadura se fragmenta mostrando negro abismo debajo
- Las alas se vuelven púrpura oscuro `#3A0060` con tentáculos negros
- Ojos cambian a blanco absoluto `#FFFFFF` sin pupila — lo más perturbador visualmente
- Tentáculos con ojos rojos `#CC2200` emergiendo del cuerpo
- El halo se convierte en un círculo de geometría sagrada corrupta

**Elementos visuales únicos de Metatrón:**
- Es el único personaje con **dos estéticas completamente opuestas** según fase
- Su sprite "aliado" tiene líneas limpias y simétricas — proyecta orden
- Su sprite "verdadero" es asimétrico y caótico — el orden se revela como ilusión
- Referencia directa a los diseños analizados: halo dorado, alas blancas, armadura negra y tentáculos rojos en la base

---

## 6. Los 7 Reyes Demonios

Cada rey es un boss de zona. El diseño sigue una regla: **forma humanoide con elemento sobrenatural masivo**.

### Especificaciones generales para todos los Reyes

```
Canvas fase 1 (forma contenida): 96×96 px
Canvas fase 2 (forma liberada):  128×144 px
Color de fondo de boss room:     Único por cada rey (ver tabla)
```

### 6.1 Ficha visual por Rey

---

**SAMAEL — Dominio del Espacio-Tiempo**
- Paleta dominante: Azul cyan `#00C8FF` + negro `#050510`
- Cuerpo: armadura negra con fisuras que revelan el espacio estelar interior
- Ojos: cyan brillante, el único efecto de glow que "sale" del sprite (halo de 2px)
- Elementos únicos: relojes rotos orbitando su cuerpo como satélites, distorsión visual en el entorno
- Fondo de boss room: negro con estrellas, el tiempo "se congela" visualmente

---

**AZAZEL — Dominio de los Espíritus**
- Paleta dominante: Verde tóxico `#60C820` + gris hueso `#8A8A70`
- Cuerpo: armadura hecha de huesos y metal oxidado
- Alas: hechas de espíritus gritando (cada pluma es una cara)
- Elementos únicos: fantasmas pequeños de 8×8 px que orbitan y atacan
- Fondo de boss room: verde oscuro con niebla, ruinas de cementerio

---

**IBLIS — Dominio del Fuego**
- Paleta dominante: Naranja volcánico `#FF6800` + rojo lava `#CC2200`
- Cuerpo: piel de lava (textura pixel con grietas naranjas sobre negro)
- Elementos únicos: cuerpo gigante dragón-humanoide (referencia al diseño analizado — dragon angel con halo)
- Alas: fuego sólido, no plumas — cada "pluma" es una llama de 4–6 px
- Fondo de boss room: rojo oscuro, lava en el suelo animada en tiles de 16×16

---

**EGYN — Dominio del Agua**
- Paleta dominante: Azul profundo `#001A5A` + turquesa `#00A8A8`
- Cuerpo: armadura líquida que cambia de forma cada frame (4 variantes que hacen loop)
- Elementos únicos: el sprite nunca tiene un contorno fijo — sus bordes se "derraman"
- Fondo de boss room: azul abismo, corrientes de agua en los tiles

---

**BELIAL — Dominio de las Emociones**
- Paleta dominante: Oro corrompido `#C8A000` + sangre oscura `#6A0010`
- Cuerpo: armadura de oro con detalles lujosos pero manchada de rojo oscuro
- Elementos únicos: múltiples caras propias creciendo del cuerpo (representan identidades robadas)
- Mecánica visual: durante el combate, la armadura de Belial imita el color del jugador durante 2 seg.

---

**ASMODEUS — Dominio del Deseo**
- Paleta dominante: Rosado oscuro `#D04080` + negro `#100008`
- Cuerpo: el más delgado y ágil de los Reyes — sprite más vertical que ancho
- Elementos únicos: rastros de movimiento (afterimages) de color rosado al atacar
- Alas: pequeñas y elegantes, no masivas — contrario a los demás Reyes

---

**LEVIATHAN — Dominio del Abismo**
- Paleta dominante: Azul abismo `#002888` + negro-verde `#001808`
- El más grande de todos — canvas de `160×200 px`
- Cuerpo: colosal y serpentiforme, la parte inferior se pierde en el abismo
- Elementos únicos: tentáculos que ocupan todo el fondo de la pantalla como elementos del escenario
- Fondo de boss room: negro absoluto — el único boss sin fondo visible

---

## 7. Aliados Humanos

Los humanos contrastan intencionalmente con los seres divinos/infernales: **más pequeños, más "ruidosos" visualmente, menos simétricos**.

### 7.1 Ficha visual de aliados

| Personaje | Canvas | Paleta base | Elemento identidad |
|-----------|--------|-------------|-------------------|
| Sarah | 28×44 | Verde natural `#3A7A30` + blanco crema `#F0E8D0` | Flores pequeñas que orbitan su cabeza en idle |
| Emma | 28×44 | Marrón cuero `#5A3A20` + naranja `#C84820` | Ballesta siempre visible, postura siempre en guardia |
| Grimm | 36×48 | Gris hierro `#5A5A60` + rojo forja `#AA2010` | Cicatrices visibles incluso en sprite pequeño, delantal de herrero |
| Silas | 28×44 | Púrpura oscuro `#3A2050` + dorado suave `#B09030` | Sonrisa ambigua, siempre tiene un objeto misterioso en la mano |

**Regla de diseño para humanos:** Sus sprites tienen más "ruido" visual (más colores, menos simetría, ropa con más texturas) que los seres celestiales, que son más limpios y simétricos. Esto refuerza visualmente la diferencia entre lo divino y lo mortal.

---

## 8. Ambiente y Escenarios

### 8.1 Zonas del juego y su estética

| Zona | Paleta dominante | Elementos visuales únicos |
|------|-----------------|---------------------------|
| Bosque de la Caída (inicio) | Azul oscuro + verde corrupto | Árboles quemados, cráter de impacto, cielo irreal |
| Aldea Mortal | Beige tierra + gris piedra | Arquitectura medieval en ruinas, grietas con luz infernal |
| Catacumbas Terrestres | Gris hueso + verde tóxico | Huesos en las paredes, velas con fuego verde |
| Fronteras Infernales | Rojo lava + negro | Lava animada, pilares de basalto, fuego constante |
| Torres del Cielo | Blanco + dorado | Mármol perfecto, geometría sagrada, luz dura |
| El Infierno (zonas profundas) | Negro + púrpura + rojo | Void, criaturas de energía pura, sin suelo visible |

### 8.2 Especificaciones de tiles

```
Tile base:         16×16 px
Tile decorativo:   32×32 px (pueden apilarse encima de tiles base)
Paralax layers:    3 capas mínimas
  ├── Fondo lejano:    muy oscuro, movimiento lento (×0.2)
  ├── Fondo medio:     arquitectura/naturaleza, movimiento normal (×0.5)
  └── Foreground:      elementos decorativos delante del jugador (×1.2)
```

### 8.3 Elementos de ambiente animados

Cada zona debe tener al menos 3 elementos animados de fondo para dar vida:

- **Bosque Caído:** partículas de ceniza que caen, el cráter humea suavemente
- **Aldea:** antorchas con fuego (4–6 frames, loop), humo que sube
- **Infierno:** embers (brasas) que suben, lava que burbujea (tile animado de 8 frames)
- **Torres del Cielo:** haces de luz que se mueven lentamente por el suelo, polvo dorado

---

## 9. Sistema de Evolución Visual

### La armadura como indicador de progreso

La armadura del protagonista **cambia visualmente** a medida que recupera fragmentos de gracia. Esto debe ser visible de un vistazo:

```
Fragmentos 0/7  →  Ropa humana básica. Sin brillo. Sin alas.
Fragmentos 1/7  →  Guantelete derecho de luz aparece. Primer destello.
Fragmentos 2/7  →  Hombros y peto parcial. Micro-alas mínimas.
Fragmentos 3/7  →  Armadura de torso completa. Alas pequeñas pero reales.
Fragmentos 4/7  →  Piernas armadas. Alas medianas. Halo fragmentado.
Fragmentos 5/7  →  Armadura casi completa. Alas grandes. Halo restaurado.
Fragmentos 6/7  →  Todo restaurado, pero con grietas rojas visibles (contacto infernal).
Fragmentos 7/7  →  Elección del jugador determina la forma final (ver abajo).
```

### Bifurcación visual al 7/7 (decisión del jugador)

```
CAMINO DE LUZ PURA
└── Armadura: plata brillante sin grietas
    Alas: blancas perfectas, plumas densas
    Halo: completo y dorado
    Ojos: dorado cálido
    Efecto idle: partículas de luz blanca

CAMINO DE LA CAÍDA ELEGIDA
└── Armadura: negra con grietas doradas
    Alas: negras con borde dorado en las puntas
    Halo: partido en 3, flotando en distintas posiciones
    Ojos: rojo con pupila dorada
    Efecto idle: cenizas negras que suben + partículas doradas
```

---

## 10. Pipeline de Producción para Aseprite

### Configuración de proyecto en Aseprite

```
Nuevo archivo:
├── Modo de color:  Indexed (256 colores máximo por sprite)
├── Tamaño canvas:  Según tabla de la sección 2
├── Fondo:          Transparente
└── Pixel grid:     Activado, 1px

Paleta:
├── Guardar paleta global del juego como .ase
├── Importar al inicio de cada sprite nuevo
└── NUNCA usar colores fuera de la paleta salvo efectos especiales de luz
```

### Orden de trabajo para cada sprite nuevo

```
1. BOCETO ESTRUCTURAL
   └── En capa nueva, con color plano (sin sombras), definir la silueta
       La silueta debe leerse como icono incluso en 16×16 px reducido

2. BLOQUE DE COLORES BASE
   └── Un color por área (piel, armadura, alas, armas)
       Sin sombras, sin highlights todavía

3. SOMBREADO — Solo 2 niveles
   └── Sombra 1: color base oscurecido ~30%
       Sombra 2: para zonas muy profundas, oscurecido ~55%
       REGLA: no más de 3 tonos por material (base + sombra + highlight)

4. HIGHLIGHTS
   └── Solo en los bordes superiores e izquierdos (luz viene de arriba-izquierda siempre)
       Color: base aclarado ~40%, o blanco si es metal pulido

5. EFECTOS DE GLOW
   └── Para ojos, núcleos de energía, halos:
       Pixel central: casi blanco (#FFFFF0)
       Ring 1 (1px alrededor): color acento 100% saturado
       Ring 2 (2px alrededor): color acento 50% opacidad (usar dithering)

6. ANIMACIÓN
   └── Animar primero el frame clave (pose de máxima acción)
       Luego agregar frames intermedios (inbetweens)
       Loop test: siempre ver en preview antes de exportar

7. EXPORTACIÓN para Unity
   └── File → Export Sprite Sheet
       Formato: PNG, modo: Horizontal Strip
       Naming: NombrePersonaje_Accion_##.png
       Ejemplo: Player_Attack01_01.png → Player_Attack01_08.png
```

### Convención de nombres de archivos

```
sprites/
├── player/
│   ├── player_idle_human.png
│   ├── player_idle_angel.png
│   ├── player_walk.png
│   ├── player_run.png
│   ├── player_attack_01.png
│   ├── player_transform.png
│   └── ...
├── enemies/
│   ├── common/
│   └── elite/
├── bosses/
│   ├── lucifer/
│   ├── metatron/
│   ├── samael/
│   └── ...
├── npcs/
│   ├── sarah/
│   ├── emma/
│   ├── grimm/
│   └── silas/
└── environment/
    ├── tiles/
    │   ├── forest/
    │   ├── village/
    │   ├── hell/
    │   └── heaven/
    └── props/
```

---

## CHECKLIST DE CONSISTENCIA VISUAL

Antes de dar por terminado cualquier sprite, verificar:

- [ ] La silueta se lee claramente en negro sobre blanco
- [ ] El personaje usa solo colores de la paleta global + su color acento personal
- [ ] Los highlights vienen de arriba-izquierda
- [ ] No hay más de 3 tonos por material
- [ ] El efecto de glow está hecho con los 3 niveles (centro, ring 1, ring 2)
- [ ] El sprite fue testeado en fondo negro Y fondo del color de su zona
- [ ] La animación en loop no tiene "saltos" entre el último y primer frame
- [ ] El canvas fue exportado en PNG sin interpolación (pixel perfect)

---

*Documento generado como base de producción. Actualizar con cada decisión de diseño tomada en equipo.*
*Versión: 1.0 — Octubre 2024*
