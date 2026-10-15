# Producción P2 — Lugares con arte: resultados

Parte del plan de la noche que Tomi aprobó al irse ("te quiero dejar funcionando toda la noche"). Los lugares siguen siendo los del juego: **mismas cajas, mismos colliders, mismas posiciones** (nada de lo jugable cambia), pero se visten con los materiales de la librería MR-04, a tamaño real.

## Qué hay
- **Todos los lugares con la librería** (`Scripts/Editor/TestScenesBuilder.Places.cs`): cada material gris del armado pasa a uno de la librería (la tabla `MaterialMap`, que se puede cambiar) y cada caja recibe coordenadas de textura en metros, así la textura se ve a su tamaño real (`catalogo.json`: `escala_m`). 281 objetos vestidos:
  - **Estación de mantenimiento**: paredes de hormigón manchado, piso de hormigón, techo de chapa corrugada, puerta verde ferroviaria, hielo transparente, mesa de madera.
  - **Taller ferroviario**: chapa corrugada, puerta de chapa verde.
  - **Refugio**: tablas a la intemperie, piso de tablas, puerta de madera pintada.
  - **Lo que se desarma**: pallets de madera a la intemperie, armarios gris de maquinaria, colchones, tableros.
  - **Postes**: de madera; los de cada 100 m, naranja de seguridad. **Combustible** en ocre (color de lectura de la guía de arte).
- **La vía**: balasto, **durmientes de madera** y **rieles oxidados** con textura (antes un solo gris).
- **Nieve**: los montones contra la estación y el taller eran cajas inclinadas; ahora se ven redondeados (la colisión de la caja queda). El refugio ganó nieve en el techo y un montón contra la pared.
- **La masa metálica** (la señal que confunde a la sonda, M5): un montón de chatarra oxidada en vez de una caja negra.
- **La camioneta abandonada**: ruedas medio enterradas, parabrisas sucio y nieve en el techo y el capó.
- **Ficha para el chat de arte**: `Docs/Props_Pedido_Arte.md`, con las medidas exactas de los props que faltan (pallet, armario, colchón, tablero, barreta, perchero con el traje, depósito, palanca de cambio, aguja de cambio, camioneta, tope), más dos pendientes de la entrega anterior (la consola de la sonda movida y la cama y el estante en piezas por fase).

## Mediciones

REGRESION_PRODUCCION_PLACEHOLDER

## Cómo probarlo vos en Unity
1. Abrí `M2_Conduccion` y dale Play. Recorré la vía (sobre todo los durmientes y los rieles) y caminá a la estación, al taller y al refugio con la sonda (Q).
2. Para cambiar qué material lleva cada cosa: la tabla `MaterialMap` en `TestScenesBuilder.Places.cs`, y reconstruir la escena (**Survival en tren > M2 > Construir escena de conducción**).

## Pendientes
- Los lugares siguen siendo cajas (con buena textura): los modelos del chat de arte las reemplazan sin cambiar el juego cuando lleguen.
- Los bultos de material (lo que se suelta de la mochila) siguen con sus colores de lectura (madera, metal, tela, componentes).
