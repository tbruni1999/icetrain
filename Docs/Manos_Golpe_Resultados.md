# Brazos, herramientas y golpe en primera persona

Pedido de Tomi (2026-10-16): "mejorá un montón los modelos; no es nada inmersivo, ni la barreta ni el golpe; se siente raro; tal vez se podría llegar a ver el brazo".

Decisiones de Tomi:
- Los modelos los hace Claude ahora y se le pasa una ficha al chat de arte.
- Los brazos cambian según el traje.
- Los brazos se ven con una herramienta en la mano y cargando cosas.

## Qué cambió

### 1. El golpe pesa (`ToolBelt`, `ViewmodelRig`)
- **Cámara aparte para las manos**: lo que tenés en las manos se dibuja encima de la escena. La barreta ya no se mete en las paredes aunque estés pegado a una. Tiene el mismo ángulo de visión que la cámara principal, así las manos calzan con lo que cargás.
- **Cada herramienta tiene su golpe**, en datos (`Data/Equipment/Herramientas.asset`, campos de carga y de golpe):
  - la barreta sube sobre el hombro derecho y baja en diagonal hacia el centro, con las dos manos;
  - el martillo hace un golpe corto y seco;
  - la sierra va de ida y vuelta;
  - la llave da un giro de muñeca.
- **Peso**:
  - la carga arranca lenta y frena arriba;
  - la bajada acelera hasta pegar;
  - al pegar, la herramienta queda frenada un instante contra lo que golpeó.
- **Después de pegar**:
  - si el golpe sirve, hay un rebotito;
  - si rebota (herramienta equivocada o algo duro), salta para atrás, la pausa es más larga y la herramienta tiembla;
  - si no pega en nada, sigue de largo un poco.
- **Empujón de la vista**: al pegar, la cámara se mueve un poco (más si rebota) y vuelve sola con un resorte.
- **Peso al moverse**:
  - lo que tenés en las manos se balancea al caminar (más corriendo);
  - al girar la cabeza se queda un poquito atrás.
  
  Los valores están en `Data/Equipment/Manos.asset`.
- **Sacar y guardar**: la herramienta sube desde abajo y girada.

### 2. Modelos nuevos (provisorios hasta el arte final)
Están hechos en Blender con scripts (`Fuentes/Scripts_Manos/`) y usan los materiales de la librería MR-04.
- **Barreta**: hierro hexagonal de 75 cm, cuello de ganso, uña partida en V, punta de cincel, pintura roja y puntas de metal gastado.
- **Martillo de herrero**: pena cruzada, mango de madera.
- **Llave combinada**: boca abierta y estrella.
- **Brazos**: brazo, antebrazo y manos en puño o abiertas.
  - Con el traje: campera de trabajo acolchada y guante grueso de goma.
  - Sin el traje: buzo de lana y guante de trabajo de cuero.

### 3. Los brazos en el juego (`FirstPersonArms`)
- La mano se pone sola en el mango de la herramienta (el vacío `Agarre_Derecha` del modelo).
- El codo se calcula para que el brazo llegue desde el hombro, sin animaciones hechas a mano.
- La barreta va con las dos manos; el martillo, la sierra y la llave con la derecha.
- **Cargando una caja o un kit**: las dos manos abiertas, contra los costados.
  - **Cambio de juego**: la caja se sostiene ahora a 75 cm de los ojos (antes 1,1 m, más de lo que llega un brazo). Está en `Data/Interaction/CarrySettings.asset`.
- **Bidón**: la mano derecha en la manija.
- **Con las manos libres o con la sonda** no se ven brazos.
- La mano del modelo es una izquierda; la derecha es la misma espejada.

## Pruebas
- `h7_brazos.cs` (nueva, en `regresion.sh`):
  - la cámara principal no dibuja las manos y la de las manos dibuja solo eso;
  - con las manos libres no hay brazos;
  - con la barreta van las dos manos, cada una a 0,0 cm de su agarre;
  - sin el traje cambian a buzo y guante de cuero;
  - un golpe a la pared empuja la vista 1,2° y a los 0,8 s volvió a 0;
  - girando 25° las manos quedan 5° atrás y después vuelven;
  - con el martillo, una mano en el mango;
  - con la caja, las palmas a 1,2 cm de los costados;
  - al soltarla, los brazos desaparecen.
- **Regresión de las pruebas que tocan herramientas, golpes y cargar**: `h1_herramientas` (actualizada: los modelos ahora viven en "Manos"), `h2_hielo_a_golpes`, `m7_desmontaje`, `h4_forja`, `m3_llevar`, `m6_equipo_y_bidon`, `m8_almacenamiento` y `h6_mejoras`. **Todas pasan.** `m3_llevar` con la caja más cerca: no se cae a 25 km/h, al lanzarla sale a 4,1 m/s y contra la pared no la atraviesa.
- **Rendimiento** (`m13_rendimiento`, en el Editor): 4,1 a 4,6 ms por cuadro, contra 3,4 a 4,0 en la medición del H4. Dibujar las manos con su cámara cuesta alrededor de medio milisegundo.

## Fotos (`Docs/Capturas_Manos`)
1. Barreta en reposo, con las dos manos.
2. Cargando el golpe sobre el hombro.
3. Pegando.
4. Sin el traje: buzo y guante de cuero.
5. Martillo.
6. Llave.
7. Cargando una caja.

## Para el arte final
Ver `Docs/Manos_Pedido_Arte.md`: medidas, orígenes, vacíos, nombres de objetos y materiales. Con esos nombres, los modelos finales reemplazan a los provisorios sin tocar código.

## Pendiente o posible después
- Animación de los dedos (hoy hay dos poses: puño y abierta).
- La sonda portátil sostenida con la mano.
- Golpes distintos al azar (dos o tres variantes por herramienta).
- Sonido de esfuerzo al cargar el golpe.
- El arte final de todo.

## Segunda vuelta (2026-10-16, después de que Tomi lo probara)
Tomi: "que la barreta la agarre con una mano; además al correr la animación queda rara, como si fuera en velocidad x2".

- **La barreta va con una mano** (DECISIÓN de Tomi):
  - se sostiene como un garrote, del lado derecho, con la uña arriba y adelante: no tapa el centro de la vista;
  - el golpe sube sobre el hombro derecho y baja en diagonal hacia el centro;
  - la casilla dejó de ser "de dos manos" en `Data/Equipment/Herramientas.asset`.
- **El ritmo al caminar y correr**:
  - **Qué pasaba**: el balanceo avanzaba por metros, con un paso de 75 cm. El jugador camina a 3,5 m/s y corre a 6,5 m/s, así que daba unos 4,7 pasos por segundo caminando y casi 9 corriendo. Por eso parecía a velocidad doble.
  - **Ahora** sigue un ritmo de pasos de persona: 1,9 por segundo caminando y 2,7 corriendo, con un poco más de balanceo al correr.
  - **Pose de correr**: corriendo, la herramienta baja, se acerca y se inclina, como en los juegos en primera persona.
  - Los valores están en `Data/Equipment/Manos.asset`.
- **El agarre y el encuadre**:
  - las herramientas de una mano (barreta, martillo, llave, sierra) subieron para que la mano se vea entera;
  - los hombros bajaron y fueron más atrás: el antebrazo entra en diagonal desde la esquina, en vez de verse de punta.
- **Barreta nueva con el kit de arte del proyecto** (skill de props, categoría A: de mano, sin nieve, pivote en el agarre). La hizo `Fuentes/Scripts_Manos/barreta_mr04.py`:
  - una sola pieza continua: punta de cincel aplanada y doblada unos 12°, hierro hexagonal de 22 mm, cuello de ganso de 150°, uña aplanada partida en V;
  - **la cinta negra del mango** (`MT_Goma`), donde va la mano: la guía de arte la sugiere para los objetos de mano;
  - el cuerpo en pintura roja y las puntas de metal gastado;
  - 568 triángulos (el límite de un objeto de mano es 1.500), malla cerrada, sin caras rotas.
- **Pruebas**: `h7_brazos` mide ahora el ritmo caminando y corriendo de verdad (con W y Shift): 1,9 y 2,7 pasos por segundo, pose de correr 1,0. La barreta va con una mano, a 0,0 cm del agarre. `h1_herramientas` y `m7_desmontaje` pasan igual que antes.

## Tercera vuelta (2026-10-16): el agarre derecho y guantes de verdad
Tomi: "sigue agarrando re chueca la barreta, y la mano se ve re mal".

**Qué pasaba** (foto `10_antes_agarre_chueco.png`):
- La mano se giraba sola según dónde quedaba el hombro. Con la barreta, la barra salía casi en la misma línea que el antebrazo, como una prolongación del brazo, y la muñeca quedaba quebrada de costado.
- Los dedos eran tubos sueltos.
- El guante de cuero marrón, con la luz cálida del vagón, parecía una mano desnuda.

**Ahora:**
- **El agarre queda fijo a la herramienta**, como en los juegos en primera persona (`FirstPersonArms`):
  - el vacío `Agarre_Derecha` de cada herramienta dice dónde está el mango, hacia dónde va (el lado del pulgar) y hacia dónde miran los nudillos (la uña de la barreta, la cara del martillo);
  - el puño del modelo trae dos vacíos sobre su mango (`Agarre` y `Agarre_Pulgar`), y el juego hace coincidir los dos mangos;
  - durante el golpe la mano no "patina" alrededor del mango: gira con la herramienta.
- **El antebrazo sigue a la mano**, con la muñeca apenas doblada hacia el meñique, como al sostener un martillo (20 grados, en el componente: `wristDeviation`). Ya no se quiebra para buscar el hombro.
  - Cargando cajas y el bidón, el codo se sigue calculando desde el hombro.
- **El mango cruza la palma en diagonal**, como en una mano de verdad: la barra no sale a 90 grados del antebrazo.
- **Poses de reposo nuevas** para la barreta, el martillo, la llave y la sierra (`TestScenesBuilder.Tools.cs`, con `GripPose`):
  - el puño abajo a la derecha, entero a la vista, y el antebrazo entra desde la esquina;
  - la barreta va casi parada, con la uña arriba a la vista;
  - en la sierra, el agarre pasó a la parte de atrás del mango de madera (antes la mano quedaba delante de la tabla).
- **Guantes nuevos** (`Fuentes/Scripts_Manos/manos.py`):
  - cada guante es **una sola pieza blanda**: palma, dedos y pulgar se funden como en un guante de verdad, con pliegues entre dedo y dedo, nudillos marcados y el pulgar cruzado sobre los dedos;
  - se hicieron con metaballs (bolas y cápsulas que se funden) y se redujeron a 3.300 a 3.900 triángulos (el límite de la ficha es 4.000);
  - **sin el traje**: guante de trabajo de tejido gris con la palma bañada en goma negra y puño tejido; no se confunde con piel;
  - **con el traje**: guante grueso negro y mate, más abultado, con su puño negro;
  - la mano del modelo pasó a ser **derecha**; la izquierda (solo se ve cargando cajas) es la espejada;
  - los bordes entre materiales son rectos (cortes por planos), sin dientes aunque la malla esté reducida.
- **El bidón**: la mano lo agarra de arriba, con el dorso hacia arriba.

**Prueba** `h7_brazos` (actualizada): con la barreta, la mano a 0,0 cm del agarre, **el mango cruza el puño desviado 0,0°** (cero es derecho) y **la muñeca doblada 20°** (lo configurado). Lo demás, igual que antes:
- el ritmo caminando: 1,9 pasos por segundo; corriendo: 2,7;
- el empujón del golpe: 1,1 a 1,3°;
- la caja a 0,75 m, con las palmas a 0,0 cm.

**Fotos** (`Docs/Capturas_Manos`):
- 11 y 11b: barreta con el guante de trabajo (de cerca);
- 12: barreta con el guante grueso;
- 13 y 14: cargando el golpe y pegando;
- 15 y 15b: martillo (de cerca);
- 16: llave;
- 17: sierra;
- 18: bidón;
- 19: caja;
- 20 a 23: los guantes en Blender.

**Regresión** de las 9 pruebas que tocan herramientas, golpes y cargar:
- `h1_herramientas`, `h2_hielo_a_golpes`, `m7_desmontaje`, `h4_forja`, `m3_llevar`, `m6_equipo_y_bidon`, `m8_almacenamiento`, `h6_mejoras` y `h7_brazos`: **iguales a la vuelta anterior**.
- `h4_forja` no contestó en la tanda y se corrió sola.
- `h6_mejoras` mostró una vez otra indicación al mirar el banco ("tomar"); corrida sola dio la de siempre. Fue una casualidad de la física.
- Rendimiento (`m13_rendimiento`): de 3,4 a 4,1 ms por cuadro (antes, de 4,1 a 4,6).

**Después**: Tomi (2026-10-16) no quedó conforme con la mano ("una poronga la mano que tenemos") y pasó un rig de brazos: ver la cuarta vuelta.

## Cuarta vuelta (2026-10-16): el rig de Tomi, vestido
Tomi pasó un rig de brazos: "fps arms (rigged only)", de para en OpenGameArt. Está hecho con MakeHuman, la licencia es **CC0** (verificada en la página) y el original quedó en `Fuentes/Brazos_OGA`.

Lo pidió así: "agregale la ropa, pero necesito que la ropa se vea impecable".

**En Blender** (`Fuentes/Scripts_Manos/brazos_rig.py`, sin ventana):
- **El rig, limpio y en metros**:
  - sin las restricciones de Blender (IK y control de dedos), porque en el juego los huesos los mueve el código;
  - la mano colgada del antebrazo;
  - el brazo derecho y el izquierdo como piezas aparte.
- **Mangas prolijas, hechas a medida del brazo**. Se mide el contorno de la piel en cada altura (rayos desde el eje del brazo), se suaviza (la tela no copia los músculos) y se le suma la holgura. Resultado:
  - un tubo con el codo redondeado;
  - el puño doblado hacia adentro, con espesor (no se ve como una hoja de papel);
  - la costura abajo, del lado del cuerpo;
  - la trama de la tela derecha a lo largo del brazo.
- **Las dos mangas:**
  - **Campera**: acolchada, con bultos entre costuras cada 7 cm, arrugas en el pliegue del codo y el puño de tejido gris adentro.
  - **Buzo**: lana con pliegues blandos y puño elástico de canaletas.
- **Guantes hechos de la propia mano del rig**: inflada 2,2 mm (trabajo) o 4,5 mm (grueso), sin uñas ni arrugas de piel, con las UV originales de la mano llevadas a tamaño real.
  - De trabajo: tejido gris con los dedos y la palma bañados en goma negra; la línea va recta sobre los nudillos.
  - Grueso: negro y mate, con puño largo.
- **Nada asoma**:
  - la piel no se exporta (siempre queda tapada);
  - el puño del guante entra en la manga;
  - la manga mide el guante que tiene adentro y nunca queda más angosta que él.
- **Los pesos** (de qué hueso depende cada punto de la ropa) se copian de la piel que hay debajo, así la ropa se dobla igual que el brazo. Los 4 vértices sin peso que traía el original tomaron los del vecino más cercano.
- **El puño**:
  - primero se buscó dónde cruza el mango la mano: delante de la palma, apenas antes de los nudillos y en diagonal hacia el meñique;
  - después, para cada dedo, los tres ángulos juntos: las tres falanges apoyadas sobre el mango, sin atravesarlo, con los nudillos bien doblados y la última falange acompañando a la del medio;
  - el pulgar cruza por encima del índice;
  - hay un puño para cada guante, porque el grueso cierra un poco menos.
- **Las poses** (reposo, puño, puño con guante grueso y mano abierta) van en una sola animación, una por cuadro. El exportador de FBX de Blender 5 dejaba vacías las acciones sueltas.

**En Unity:**
- `FirstPersonArms` mueve los huesos del rig:
  - el puño fijo al mango con los huesos `agarre` y `agarre_pulgar`;
  - el antebrazo en línea con la mano, con la muñeca doblada 20°;
  - el hombro se acomoda para que el brazo llegue;
  - el giro del antebrazo se reparte entre el codo y la muñeca (`forearmTwist`), para que la tela no se retuerza.
- Las poses de los dedos se leen del modelo al armar la escena. Al cambiar de herramienta a caja, los dedos pasan de una pose a otra.
- `ArtImport` deja importar animaciones solo a este modelo.
- Se borró la mano anterior (`Manos.fbx` y `manos.py`).

**Prueba** `h7_brazos` (adaptada al rig):
- con la barreta, la mano a 0,0 cm del agarre, el mango derecho en el puño (0,0°) y la muñeca doblada 17°;
- con el traje, `Guante_Grueso_R` y `Manga_Campera_R`; sin el traje, `Guante_Trabajo_R` y `Manga_Buzo_R`;
- el golpe, el balanceo y el ritmo de pasos, igual que antes;
- con la caja, las palmas a 0,0 cm de los costados.

**Regresión** de las 10 pruebas que tocan herramientas, golpes y cargar (`h1`, `h2`, `m7_desmontaje`, `h4`, `m3_llevar`, `m6_equipo_y_bidon`, `m8_almacenamiento`, `h6`, `h7`, `m13_rendimiento`): iguales a la vuelta anterior.
- En la tanda, `m3_llevar` dio una vez un salto de 52 cm de la caja con el tren a 25 km/h. Corrida sola dos veces dio 0,2 y 0,1 mm, lo normal: fue un tirón del Editor, no los brazos.
- Rendimiento: de 4,0 a 4,3 ms por cuadro (la vuelta anterior, de 3,4 a 4,1). Los brazos con piel que se deforma cuestan un poco más que las piezas sueltas.

**Fotos** (`Docs/Capturas_Manos`):
- 24 a 25b: barreta con buzo y con campera (de cerca, afuera de día);
- 26 a 28: martillo, llave y sierra;
- 29 y 29b: cargando el golpe y pegando;
- 30 y 31: caja y bidón;
- 32 a 35: la ropa y los puños en Blender.
