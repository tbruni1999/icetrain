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

## H3 y H4 — El metal trabajado y la forja (HECHO)

Diseño aprobado por Tomi el 2026-10-16 ("seguí con la forja"). Todos los números son PROPUESTA y están en datos (`Data/Crafting/Forja.asset`, `Data/Build/Obra_Forja.asset`, `Data/Crafting/Plano_Martillo.asset`).

- **El metal (H3)**: la **chapa** que sale de desarmar sigue siendo el metal en bruto (el banco la usa para lo simple: sierra, bidón, martillo). La forja convierte **1 chapa en 1 planchuela** (metal trabajado, 1x3 casillas, 1,5 kg): es lo que va a pedir lo del tren.
- **La forja**: en el rincón derecho de atrás, contra la sala de máquinas (entre el motor y la puerta), y el **yunque** pasando la puerta. Se construye como la cama: **hogar** (chapa ×2 + tablón ×1), **fuelle** (tela ×2 + tablón ×1) y **yunque** (chapa ×2); lo que falta se ve como fantasma. Terminada, cada parte se usa con E por separado:
  - **La puerta del fogón**: echar un tablón (cada uno da 24 min de fuego; entran hasta 2 h).
  - **La válvula roja**: el combustible del tanque del tren (2 L/h). La leña rinde menos, como pidió Tomi. Si el tanque se vacía, la válvula se cierra sola.
  - **La boca del hogar**: meter una chapa al fuego; cuando está "al rojo vivo: lista", E la lleva al yunque.
  - **El yunque**: con el **martillo** en la mano (casilla **4**), cada martillazo la va estirando de chapa a barra; en frío **rebota**. Si se enfría, E la devuelve al fuego. Terminada, la planchuela queda sobre el yunque para juntar con E.
- **Calor**: la forja tarda en calentar (15 s reales hasta pleno) y se enfría apagada. Encendida **calienta el vagón**: con la calefacción apagada (sin combustible), el vagón se queda en ~8 °C en vez de bajar a −18 °C. Es una forma de sobrevivir sin gasoil, quemando madera.
- **El martillo**: plano inicial del banco (tablón ×1 + chapa ×1). Sin arte todavía (mango de madera y cabeza de acero de cajas).
- **Más metal en el mundo**: dos **restos de chatarra** junto a la masa metálica (al lado del taller ferroviario), 8 golpes de barreta cada uno, chapa ×2. Sin esto, las chapas del mundo apenas alcanzaban para la bifurcación del M10.
- **Guardado**: se guardan el fuego (leña, válvula, calor), la pieza (dónde está, su calor y los martillazos) y el martillo.
- **Sonidos**: el martillo en el yunque suena distinto en caliente y en frío; la planchuela terminada, como el banco.

### Mediciones (prueba `h4_forja.cs`, en `regresion.sh`)
- Obra de 3 fases construida con lo justo (chapas 4, tablones 2, tela 2). El martillo está en el banco y aparece en la casilla 4 al tenerlo.
- Fuego con 4 tablones: a la mitad a los 7,5 s, a pleno a los 15 s; calienta el vagón 16 °C/h.
- Chapa al fuego hasta "al rojo vivo: lista": 9,5 s. En el yunque: **9 golpes, 8 sirvieron, 1 rebotó** (se enfrió), volvió al fuego 1 vez: la planchuela salió en 8,7 s y se juntó en la mochila.
- En frío: rebota (0 golpes que sirven).
- Válvula 3 h: 6,0 L del tanque (2 L/h exactos). Calor del vagón sin calefacción: con la forja 7,7 °C; sin ella, −17,8 °C.
- Guardado: el archivo lleva la forja, el martillo y la obra.
- Ajuste en el camino: al principio la indicación invitaba a sacar la pieza al primer rojo y se enfriaba antes del primer martillazo. Ahora la indicación muestra el calor ("al rojo vivo: lista") y la pieza tarda 6 s en enfriarse desde el máximo (antes 4).

### Regresión parcial de H4 (11 pruebas)
La forja, herramientas, temperatura, banco, taller, reparación de la bifurcación, lugares, el recorrido completo, guardado, sonido y rendimiento: las 11 devolvieron resultado e iguales a antes. Con la forja apagada el vagón queda igual que siempre (13,7 °C). El banco ahora muestra 3 planos (sierra, bidón, martillo). Las chapas del mundo pasaron de 13 a **17** (los dos restos de chatarra). El guardado da IGUAL en las tres comparaciones. Rendimiento en el Editor: 3,4 a 4,0 ms por cuadro, igual que antes (la luz de la forja no hace sombra y está apagada mientras la forja está fría).

### Cómo probar la forja vos
1. Juntá los materiales desarmando (no hay atajo para dárselos): hacen falta chapas 4, tablones 2 y tela 2 para la obra, más tablones para el fuego y chapas para trabajar.
2. Andá al rincón derecho de atrás del tren y mantené E mirando el lugar de la forja para construirla (hogar, fuelle, yunque).
3. En el banco fabricá el **martillo**.
4. Echá tablones por la puerta del fogón (E), o abrí la válvula roja, y esperá que se caliente: la luz naranja crece.
5. Mirá la boca del hogar con una chapa en la mochila: E la mete al fuego. Cuando diga "al rojo vivo: lista", E la lleva al yunque.
6. Sacá el martillo (**4**) y martillá con clic izquierdo. Si se enfría, E la devuelve al fuego. Al final juntá la planchuela.

### Falta decidir (Tomi)
- **La primera mejora del tren con planchuelas** (opciones dadas: reforzar el quitanieves, un soporte para un segundo tanque, una estufa mejor).

## Cómo probarlo vos en Unity
1. Abrí `M2_Conduccion` y dale Play. Agarrá la barreta del perchero (E).
2. Apretá **1**: aparece en la mano. **2** no hace nada hasta tener la sierra; **3** saca la sonda; la rueda pasa de una a otra.
3. Salí, andá al pallet de afuera y golpealo con **clic izquierdo** (o mantenelo). Mirá cómo se afloja, las astillas y el ruido. Al romperse, juntá la madera con **E**.
4. En la estación probá el armario (chispas y ruido metálico) y el hielo de la puerta.
5. Fijate que con una caja en las manos la barreta se guarda sola.

## Lo que sigue
- La primera mejora del tren con planchuelas (la elige Tomi).

## Pendientes
- Arte: la barreta y el martillo en la mano, y la forja (por ahora de cajas, con materiales de la librería).
- Falla ocasional vieja (no de H1 ni H2): en `m3_llevar`, a veces la caja empujada contra la pared del tren en marcha la atraviesa (2 de 13 corridas; ya pasó en el M8).
