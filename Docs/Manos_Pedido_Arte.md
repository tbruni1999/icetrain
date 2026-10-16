# Pedido al chat de arte: brazos y herramientas en primera persona

Pedido de Tomi (2026-10-16): "mejorá un montón los modelos; no es nada inmersivo, ni la barreta ni el golpe; tal vez se podría llegar a ver el brazo".

Claude hizo versiones provisorias en Blender (`Fuentes/Scripts_Manos/`). Ya están en el juego. Esta ficha dice cómo tienen que venir las finales para que **se cambien sin tocar código**: mismo archivo, mismos nombres de objetos, mismos orígenes y mismos vacíos.

## Decisiones de Tomi
- Los brazos se ven **con una herramienta en la mano y cargando** (cajas, kits, bidón). Con las manos libres no se ven.
- **Según el traje**:
  - con el traje puesto: campera de trabajo acolchada y guante grueso;
  - sin el traje: buzo de lana y guante de trabajo de cuero.
- Regla de la guía MR-04: lo que el jugador agarra o usa nunca lleva nieve.

## Cómo se arma en el juego (para entender las convenciones)
- Son **piezas rígidas**, sin esqueleto: brazo, antebrazo y mano. El juego las acomoda solo cada cuadro:
  - pone la mano en el mango de la herramienta;
  - calcula el codo para que el brazo llegue desde el hombro.
- Se modela **un solo brazo**. Desde el 2026-10-16 todas las herramientas se agarran con una mano. Hoy la mano del modelo es una **mano izquierda** y la derecha es la misma espejada. Si las finales vienen como mano derecha, avisar: es un solo valor en el armado.
- Todo se dibuja con su propia cámara, encima de la escena. No hace falta cuidar choques con paredes.

## Archivo de brazos: `Assets/_Project/Art/Manos/Manos.fbx`
Exportar como los props: escala y ejes horneados, igual que `Fuentes/Scripts_Pulido/pulir.py`. Medidas en Blender (Z arriba):

| Objeto | Origen | Largo | Notas |
|---|---|---|---|
| `Brazo_Campera`, `Brazo_Buzo` | el hombro | 0,29 m a lo largo de +Z | casi nunca se ve; puede ser simple |
| `Antebrazo_Campera`, `Antebrazo_Buzo` | el codo | 0,26 m a lo largo de +Z | se ve mucho; puño de la manga en la punta (muñeca) |
| `Mano_Grueso_Puno`, `Mano_Trabajo_Puno` | la muñeca | dedos hacia +Z | cerrada alrededor de un mango de 2,7 cm de diámetro que cruza la palma a lo largo de X; vacío **`Agarre`** en el centro del mango |
| `Mano_Grueso_Abierta`, `Mano_Trabajo_Abierta` | la muñeca | dedos hacia +Z | para agarrar cajas de costado; vacío **`Agarre`** en el centro de la palma |

- En todas las manos: el dorso mira a +Y, la palma a -Y, y el pulgar va del lado -X (mano izquierda).
- Los extremos de las mangas, redondeados: el codo y el hombro no tienen que verse como un tubo cortado.
- Materiales: nombres de la librería MR-04. Hoy se usan:
  - campera: `MT_LonaMarron`;
  - buzo: `MT_TelaManta`;
  - puños: `MT_TelaGris`;
  - guante grueso: `MT_Goma`;
  - guante de trabajo: `MT_Cuero`.
  
  Una franja reflectiva en la campera sería un buen detalle (`MT_FranjasSeguridad`).
- Polígonos: es lo que más se ve del juego, se puede invertir.
  - Una mano completa: hasta 4.000 triángulos.
  - Un antebrazo: hasta 1.500.
  - Sin LOD (está siempre cerca de la cámara).

## Archivos de herramientas: `Assets/_Project/Art/Herramientas/<Nombre>.fbx`
- Cada herramienta tiene el **origen donde la agarra la mano derecha**, y su largo a lo largo de +Z.
- Vacíos que lee el juego:
  - `Agarre_Derecha`: en el origen.
  - `Agarre_Izquierda`: dónde iría la otra mano (hoy ninguna herramienta se agarra con dos).
  - `Punta`: lo que golpea, para efectos futuros.

| Archivo | Qué es | Medidas | Materiales hoy |
|---|---|---|---|
| `Barreta.fbx` | barreta de demolición: hierro hexagonal de 22 mm, cuello de ganso de unos 150°, uña aplanada partida en V, punta de cincel aplanada y doblada unos 12°, cinta negra en el mango | unos 78 cm; se agarra con UNA mano (decisión de Tomi del 2026-10-16): `Agarre_Derecha` en el medio de la cinta, a unos 15 cm de la punta de cincel | `MT_PinturaRoja`, `MT_MetalGastado` en las puntas, `MT_Goma` en la cinta (hoy: 568 triángulos, hecha con el skill de props: `Fuentes/Scripts_Manos/barreta_mr04.py`) |
| `Martillo.fbx` | martillo de herrero de pena cruzada, de alrededor de 1 kg | 33 cm; agarre a 6 cm del extremo del mango | `MT_Madera`, `MT_AceroOscuro` |
| `Llave.fbx` | llave combinada: boca abierta y estrella | 28 cm; agarre a 10 cm de la boca abierta | `MT_AceroGalvanizado` |

La sierra sigue siendo la del arte del vagón (`Sierra`). Si se rehace para la mano, que traiga su `Agarre_Derecha` en el mango.

## Cómo se ve hoy
Fotos en `Docs/Capturas_Manos`. Los scripts provisorios (`Fuentes/Scripts_Manos/manos.py` y `herramientas.py`) sirven de referencia de medidas y de orígenes.
