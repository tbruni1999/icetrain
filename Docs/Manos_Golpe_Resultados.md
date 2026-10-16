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
