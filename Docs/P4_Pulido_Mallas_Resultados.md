# Producción P4 — Pulido de las mallas del arte: resultados

Pedido de Tomi (2026-10-15): "meté un mega pulido de los assets después, busca que la malla quede impecable". Con su autorización ("si dale") se instaló Blender para trabajar las mallas en su origen.

## Qué se hizo, en dos capas

### 1. En Unity: revisión y arreglo automático (lo que se ve en el juego)
- **"Survival en tren > Arte > Revisar mallas"** (`Scripts/Editor/MeshAudit.cs`) mira cada malla del arte (el vagón y los 6 props) y escribe `Docs/Revision_Mallas.md`: triángulos sin área, caras repetidas (parpadean), normales al revés (se ven oscuras o desaparecen), bordes abiertos, texturas aplastadas (rayas), medidas.
- **"Survival en tren > Arte > Arreglar mallas"** (`Scripts/Editor/MeshFix.cs`) hace una copia corregida de cada malla en `Art/Generado/Mallas/` y las escenas usan esas copias. En las 58 mallas: 22 triángulos sin área quitados, 430 caras repetidas quitadas, 62 triángulos dados vuelta, 836 triángulos con la textura rehecha (proyección a tamaño real) y 1.602 vértices soldados. Las caras dobles a propósito (un mismo triángulo mirando a los dos lados) no se tocan.

### 2. En Blender: el modelo limpio en su origen
- **Blender 5.2.2 LTS** portable en `C:/Users/tomas/Tools/blender` (el zip oficial de download.blender.org, con su huella SHA256 verificada). No se instala nada en Windows: se puede borrar la carpeta.
- **`Fuentes/Scripts_Pulido/pulir.py`** abre cada `.blend` del chat de arte y, en cada malla: **suelda** los vértices que están en el mismo lugar (las caras venían sueltas, cada una con sus propios vértices), saca las caras sin área y los vértices sueltos, y exporta el FBX. **No cambia el sombreado del artista**: sus normales hechas a mano se conservan, sus bordes marcados duros siguen duros, y lo que estaba separado (y por eso se veía como un canto duro) queda marcado duro al soldarlo.
- Resultado en el vagón: la carrocería tenía **38.396 bordes abiertos → 0**, con un tercio de los vértices (38.504 → 12.416); igual en el chasis, los bogies, la cabina, la cama y el estante (informe por pieza en `Fuentes/Exportado/*_pulido.txt`). Los props ya venían soldados: casi no cambian.
- **Los FBX del juego son ahora los pulidos.** Los originales quedaron en `Fuentes/Tren/RedBlanca_MR04_v03_invierno_original.fbx` y `Fuentes/Props/<prop>/<prop>_original.fbx`. Los `.blend` pulidos no se suben (se rehacen en un minuto con el script y ocupan ~60 MB del cupo de Git LFS).

## Cómo se verificó que no se rompió nada
- **Fotos en Blender, original contra pulido, píxel por píxel** (`render_comparar.py` para el vagón, 3 vistas; `render_prop.py` para cada prop, cada nivel de detalle, 2 vistas): el vagón cambia **4 píxeles de 921.600**; los props, **0**, salvo el bidón visto de lejos (LOD2), donde el asa cambia un poco de brillo (1,4 % de la imagen).
- **En Unity, las 74 piezas** del vagón y de los props quedan **idénticas** en lugar, giro, escala, tamaño, materiales y niveles de detalle (comparadas una por una antes y después).
- Escenas reconstruidas (M1, M2 y el menú) con las mallas nuevas, y regresión del arte (18 pruebas): ver abajo.

### Dos errores del primer intento (corregidos antes de usarlo)
- **Giro de 90° y escala 100 en cada pieza.** La exportación por defecto de Blender deja así cada pieza en Unity: en el mismo lugar, pero las palancas, las agujas, las puertas y las ruedas (que se animan girando su pieza) se habrían roto. Ahora el script "hornea" los ejes y la escala en la malla, como el FBX original.
- **Facetaba lo liso.** La primera versión ponía sombreado por ángulo (30°) y los carámbanos y la costra de nieve, que el artista dejó lisos a propósito, quedaban en facetas. Ahora el script no toca el sombreado.

## Lo que no se arregla solo (para el chat de arte)
Queda en `Docs/Revision_Mallas.md`: algunos bordes "no-manifold" (bordes compartidos por más de dos caras, típicos de piezas que se tocan) y bordes abiertos en los props (bocas y asas). No se ven en el juego; importan si el artista quiere subdividir o hornear esas mallas.

## Regresión del arte con las mallas pulidas (18 pruebas)

`Tools/SpikeTests/regresion_arte.sh`, con las escenas reconstruidas: las 18 devolvieron resultado y **dan lo mismo que en la regresión completa de esta mañana**:
- Parado sobre una caja a 60 km/h: 0,00 cm; re-centrado: 6 veces, costo máx. 0,55 ms, temblor del jugador 0,02 mm.
- Parada a +0,15 m del cartel; tope a 10 km/h: 23 cm, 2,6 g (las cajas sueltas se corrieron 52 cm: varía entre 30 y 52 cm de una corrida a otra desde el M10).
- Puertas, llevar cosas (temblor en la mano 0,1 mm), temperatura, comer, dormir y desmayo, equipo, bidón y sifón: igual.
- Las dos expediciones, las obras, el estante, el banco (sierra y bidón), el guardado (IGUAL en las tres comparaciones) y el sonido: igual.
- Rendimiento en el Editor: 2,9 a 3,6 ms por cuadro (esta mañana, 3,4 a 4,0).

## Capturas
- `Docs/Capturas_P4/3_afuera_vagon_pulido.png` y `4_interior_vagon_pulido.png`: el vagón pulido en el juego (a las 11:00), comparables con `1_` y `2_` (antes del pulido en Blender, con el arreglo de Unity).
- `Docs/Capturas_P2/`: los lugares vestidos de P2 (estación, taller ferroviario, refugio, camioneta, chatarra), que se habían perdido.

## Cómo probarlo vos
1. En Unity, abrí `M2_Conduccion` y dale Play: el tren se tiene que ver igual que antes (esa era la meta: limpio por dentro, igual por fuera). Probá las palancas, las agujas, las puertas y mirá las ruedas andando.
2. Si querés ver la malla limpia: abrí `Fuentes/Exportado/RedBlanca_MR04_v03_invierno_pulido.blend` en Blender (o rehacelo con el comando de abajo) y compará la carrocería con el original en modo edición: ya no hay caras sueltas.
3. Para rehacer el pulido de un modelo: `blender.exe -b <original.blend> --python Fuentes/Scripts_Pulido/pulir.py -- <salida.fbx> <salida.blend> <informe.txt>`.
