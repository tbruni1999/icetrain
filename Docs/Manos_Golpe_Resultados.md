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
