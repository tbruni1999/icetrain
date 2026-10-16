# Pedido al chat de arte: brazos y herramientas en primera persona

Pedido de Tomi (2026-10-16): "mejorá un montón los modelos; no es nada inmersivo, ni la barreta ni el golpe; tal vez se podría llegar a ver el brazo".

Desde el 2026-10-16 los brazos son **el rig que pasó Tomi**:
- "fps arms (rigged only)", de para en OpenGameArt, hecho con MakeHuman.
- Licencia CC0. El original está en `Fuentes/Brazos_OGA`, junto con `LICENCIA.txt`.

Claude lo vistió en Blender con `Fuentes/Scripts_Manos/brazos_rig.py` (Blender sin ventana). Esta ficha dice cómo tiene que venir una versión final para que **se cambie sin tocar código**: mismo archivo, mismos nombres de huesos y de piezas, y las poses en los mismos cuadros.

## Decisiones de Tomi
- Los brazos se ven **con una herramienta en la mano y cargando** (cajas, kits, bidón). Con las manos libres no se ven.
- **Según el traje**:
  - con el traje puesto: campera de trabajo acolchada y guante grueso;
  - sin el traje: buzo de lana y guante de trabajo.
- "Agregale la ropa, pero necesito que la ropa se vea impecable."
- Todas las herramientas se agarran con **una mano**.
- Regla de la guía MR-04: lo que el jugador agarra o usa nunca lleva nieve.

## Cómo se arma en el juego
- Cada cuadro, el juego mueve los huesos del brazo:
  - la mano queda **fija al mango** de la herramienta, como en los juegos en primera persona;
  - el antebrazo sale en línea con la mano, con la muñeca apenas doblada hacia el meñique (20 grados, ajustable);
  - cargando cajas o el bidón, el codo sale del hombro.
- Los dedos toman **poses guardadas** en el modelo. No hay animaciones hechas a mano.
- Todo se dibuja con su propia cámara, encima de la escena: no hace falta cuidar choques con paredes.

## Archivo: `Assets/_Project/Art/Manos/BrazosFPS.fbx`
Exportar en metros, con el rig en el origen y sin girar (ver la exportación al final de `brazos_rig.py`).

**Huesos.** Se usan los nombres del rig de MakeHuman, con `.R` y `.L`:
- `clavicle`, `deltoid`, `upper_arm`, `forearm`, `hand`;
- en cada dedo, `palm_<dedo>` y `f_<dedo>.01` a `.03`;
- en el pulgar, `thumb.01` a `.03`.

La mano cuelga del antebrazo (`hand` es hija de `forearm`). Además van tres huesos que no deforman, hijos de `hand`:

| Hueso | Dónde |
|---|---|
| `agarre.R` / `.L` | El centro del mango que agarra el puño (2,9 cm de diámetro). |
| `agarre_pulgar.R` / `.L` | Unos 3 cm más allá sobre el mango, del lado del pulgar. El juego saca de estos dos huesos por dónde cruza el mango la palma. |
| `palma.R` / `.L` | El centro de la palma, sobre la piel: se apoya en el costado de una caja. |

**Piezas** (mallas con pesos sobre esos huesos, una por brazo):

| Pieza | Qué es | Materiales hoy |
|---|---|---|
| `Manga_Campera_R` / `_L` | Manga de campera acolchada (bultos entre costuras cada 7 cm), con el puño de tejido adentro | `MT_LonaMarron`, `MT_TelaGris` |
| `Manga_Buzo_R` / `_L` | Manga de buzo de lana con puño elástico de canaletas | `MT_TelaManta` |
| `Guante_Grueso_R` / `_L` | Guante grueso de invierno, negro y mate, con puño largo | `MT_Goma` |
| `Guante_Trabajo_R` / `_L` | Guante de trabajo de tejido gris con los dedos y la palma bañados en goma negra, puño tejido | `MT_TelaGris`, `MT_Goma` |

- La piel no se exporta: siempre queda tapada. El puño del guante entra adentro de la manga, y la manga nunca es más angosta que el guante que tiene adentro.
- Hoy cada pieza tiene de 3.800 a 3.950 triángulos.

**Poses de los dedos.** Una sola animación con una pose por cuadro:

| Cuadro | Pose |
|---|---|
| 1 | Reposo |
| 11 | Puño cerrado sobre el mango, con el guante de trabajo |
| 21 | Puño cerrado con el guante grueso (cierra un poco menos) |
| 31 | Mano abierta, con los dedos juntos |

**Ojo con los colores de piel:** el cuero marrón (`MT_Cuero`) bajo la luz cálida del vagón se leía como una mano desnuda. Desde la cámara se ven sobre todo el dorso, el pulgar y el costado de la palma.

## Archivos de herramientas: `Assets/_Project/Art/Herramientas/<Nombre>.fbx`
- Cada herramienta tiene el **origen donde la agarra la mano derecha** y su largo a lo largo de +Z.
- Vacíos que lee el juego:
  - `Agarre_Derecha`: en el origen. **Su orientación cuenta**:
    - su +Y (en Unity; +Z en Blender) va a lo largo del mango hacia la cabeza de la herramienta, donde queda el pulgar;
    - su +Z (en Unity; +Y en Blender) apunta hacia donde miran los nudillos: la cara del martillo, la uña de la barreta.

    Hoy, en todas coincide con los ejes de la herramienta.
  - `Agarre_Izquierda`: dónde iría la otra mano. Hoy ninguna herramienta se agarra con dos.
  - `Punta`: lo que golpea, para efectos futuros.

| Archivo | Qué es | Medidas | Materiales hoy |
|---|---|---|---|
| `Barreta.fbx` | Barreta de demolición: hierro hexagonal de 22 mm, cuello de ganso de unos 150°, uña aplanada partida en V, punta de cincel aplanada y doblada unos 12°, cinta negra en el mango. | Unos 78 cm. `Agarre_Derecha` en el medio de la cinta, a unos 15 cm de la punta de cincel. | `MT_PinturaRoja`, `MT_MetalGastado` en las puntas, `MT_Goma` en la cinta. Hoy tiene 568 triángulos y la hizo `Fuentes/Scripts_Manos/barreta_mr04.py`. |
| `Martillo.fbx` | Martillo de herrero de pena cruzada, de alrededor de 1 kg. | 33 cm; agarre a 6 cm del extremo del mango. | `MT_Madera`, `MT_AceroOscuro` |
| `Llave.fbx` | Llave combinada: boca abierta y estrella. | 28 cm; agarre a 10 cm de la boca abierta. | `MT_AceroGalvanizado` |

La sierra sigue siendo la del arte del vagón (`Sierra`). Su mango es una tabla lisa, sin agujero para los dedos, y el juego le pone el agarre en la parte de atrás. Si se rehace para la mano, que traiga el mango con agujero y su `Agarre_Derecha`.

## Cómo se ve hoy
Fotos en `Docs/Capturas_Manos`, desde la 24 en adelante.
