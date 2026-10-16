# Herramientas y forja — resultados

Pedido de Tomi (2026-10-15): "empezar a implementar los objetos utilizables, la barreta para desarmar artículos golpeando ... un nuevo sistema para los crafteos ... más survival, que tengas que utilizar una forja ... en algún momento vamos a usar el metal para las cosas del tren". La forja no está en el documento maestro: es idea de Tomi. Plan en 4 pasos (H1 a H4) y decisiones de Tomi: la forja vive **dentro del tren**, se trabaja con **golpes en el yunque**, el fuego se alimenta con **leña o combustible del tren**, y las herramientas se cambian con **números + rueda**.

## H1 — Herramientas en la mano (HECHO)
- **La barra de abajo**: casillas fijas (1 barreta, 2 sierra, 3 sonda). Con 1, 2, 3 o la rueda del mouse se elige qué se lleva en la mano; volver a apretar el mismo número deja las manos libres. Lo que todavía no se tiene se ve apagado. Bien marcada un rato después de cambiar y después tenue (como las barras del cuerpo).
- **En la mano**: la barreta (acero oscuro con el mango pintado; sin arte todavía, pedida al chat de arte) y la sierra (la del arte del vagón). La sierra colgada en el taller desaparece mientras está en la mano.
- **El golpe**: clic izquierdo (o mantenerlo para golpear seguido). La herramienta se echa atrás, baja y en el momento justo pega a lo que haya adelante (hasta 2,2 m con la barreta); se frena un instante contra lo que golpea.
- **Se guarda sola** con una caja en las manos, manejando, con un panel abierto o durante el despertar. **Q** sigue sacando la sonda y la barra la acompaña.
- Valores en `Data/Equipment/Herramientas.asset` (casillas, duración del golpe, alcance, ángulo).
- Prueba `h1_herramientas.cs` (en `regresion.sh`): elegir, volver a apretar, la sonda como casilla y con Q, la rueda, golpear una pared (pega) y al aire (no pega), guardarla con una caja, manejando y con un panel.

## H2 — Desarmar a golpes (HECHO)
- **Reemplaza el "mantener E"** (DECISIÓN de Tomi del 2026-10-13). Cada objeto pide una cantidad de golpes, en datos: pallet 5, colchón 4, tablero 6, armario 10 (`Data/Items/Desmontaje_*.asset`, campo "golpes").
- **Cada golpe se siente**: el objeto se sacude, sus piezas se van aflojando a la vista (casi firmes al principio, flojas al final), saltan astillas (madera, tela) o chispas (metal, tablero), y suena distinto según el material.
- **Al último golpe se rompe** y los materiales **caen al piso como bultos**; se juntan con E (lo que no entra en la mochila queda ahí). Nunca se pierde ni se duplica nada.
- **Herramienta equivocada**: el golpe rebota (con la sierra el pallet no avanza).
- **Mirando el objeto con la barreta al cinto, E la saca a la mano.** La indicación dice qué hace falta: "sacar la barreta para desarmar…" o "golpear para desarmar…".
- **Golpear cansa**: cada golpe que sirve gasta 0,25 de energía (de 100), en `Herramientas.asset`.
- **El hielo** (la puerta de la estación y el cambio de vía) también se rompe a golpes de barreta: 5 golpes. Mantener E sigue andando.
- Las astillas y pedazos no frenan los golpes ni la E (los atraviesan).
- Pruebas: `m7_desmontaje.cs` reescrita (golpes de verdad apuntando; la sierra rebota; el pallet de afuera con el clic izquierdo real mantenido y los bultos juntados con la E real; cuentas exactas) y `h2_hielo_a_golpes.cs` (nueva). Las pruebas que desarmaban con E ahora golpean y juntan lo que cae.

## Mediciones de H1 y H2

- `m7_desmontaje` (golpes de verdad, apuntando): pallet 5 golpes, colchón 4, tablero 6, armario 10, **exactos** (al principio sobraban 1 o 2: el golpe pegaba en las astillas del golpe anterior; ahora las atraviesa). La sierra rebota (avance 0 %). Energía: cada objeto cuesta de 1 a 2,5 puntos (de 100). Pallet de afuera con el clic izquierdo real mantenido: 5 golpes en 3,7 s, y la madera juntada con la E real. **Cuentas exactas: nada se duplica ni se pierde.**
- `h2_hielo_a_golpes`: el hielo de la estación cede en 5 golpes (3,7 s); la indicación dice "golpear o mantener E para romper el hielo".
- Al principio los objetos en el piso (pallet, colchón) no se alcanzaban: desde los ojos hay más de 1,9 m. El alcance de la barreta pasó a 2,2 m, igual que la E.

### Regresiones parciales
- **H1** (9 pruebas: herramientas, sonda, equipo, llevar, mochila, mouse, banco, taller, menú): todas iguales. En `m3_llevar` una vez la caja empujada contra la pared la atravesó; corrida dos veces más, bien. Ya había pasado en el M8: es una falla ocasional vieja, no de H1.
- **H2** (12 pruebas: las que desarman o rompen hielo, el guardado y el sonido): todas devuelven resultado y las cuentas cierran. Cambios esperables por el desarme a golpes: el ciclo de obras del M8 termina en **5 viajes en vez de 4** (lo desarmado cae al piso y el caminante automático lo junta en otro orden) y el recorrido completo del M11 tarda **278 s en vez de 263** (los golpes llevan su tiempo). El guardado da IGUAL en las tres comparaciones.

## Cómo probarlo vos en Unity
1. Abrí `M2_Conduccion` y dale Play. Agarrá la barreta del perchero (E).
2. Apretá **1**: aparece en la mano. **2** no hace nada hasta tener la sierra; **3** saca la sonda; la rueda pasa de una a otra.
3. Salí, andá al pallet de afuera y golpealo con **clic izquierdo** (o mantenelo). Mirá cómo se afloja, las astillas y el ruido. Al romperse, juntá la madera con **E**.
4. En la estación probá el armario (chispas y ruido metálico) y el hielo de la puerta.
5. Fijate que con una caja en las manos la barreta se guarda sola.

## Lo que sigue
- **H3**: el metal en bruto y el metal trabajado (lo del tren pide metal de forja).
- **H4**: la forja en el tren (obra por fases), el martillo, calentar y martillar en el yunque, y la primera mejora del tren con metal de forja.

## Pendientes
- Arte: la barreta y el martillo en la mano (por ahora barreta de cajas).
- Falla ocasional vieja (no de H1 ni H2): en `m3_llevar`, a veces la caja empujada contra la pared del tren en marcha la atraviesa (2 de 13 corridas; ya pasó en el M8).
