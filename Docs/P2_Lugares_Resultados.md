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

### Regresión completa de producción (39 pruebas, 2026-10-15)

Las 35 pruebas del M1 al M13 más las 4 de producción (nieve, escarcha, inicio del juego y rendimiento), una por una, cada una con su escena recién puesta en Play (`Tools/SpikeTests/regresion.sh`, unas 2 h 30), con el arte, la atmósfera, el terreno, los lugares vestidos y el menú en el juego. Las 39 devolvieron resultado y **dan lo mismo que en el M13**, salvo una prueba que hubo que ajustar (abajo):

- **Tren y jugador (M1 a M3):** parada a +0,15 m del cartel; temblor del jugador pasando el cambio 0,03 mm; tope a 10 km/h: 23 cm, 2,6 g, rebote 1,8 km/h; puertas y llevar cosas igual (temblor en la mano 0,2 mm). Parado sobre una caja a 60 km/h: corrimiento 0,00 cm. Re-centrado del origen: 6 veces, costo máx. 0,66 ms, temblor del jugador 0,03 mm, deriva 0,02 cm.
- **Supervivencia (M4):** adentro 13,7 °C; con las dos puertas abiertas baja a −20 °C y al cerrarlas vuelve a 13,7 °C en 2 h; caída de 6 m: vida 100 → 38.
- **Sonda (M5):** la portátil lleva al objetivo caminando un 96 % de la línea recta (después del ajuste); la interferencia junto a la masa metálica, igual.
- **Combustible (M6):** gasto exacto (1,5 / 5,5 / 9,5 L/h); la expedición, idéntica: 4,88 h de juego.
- **Inventario, obras y taller (M7 a M9):** cuentas exactas, sin duplicar ni perder; obras en 4 viajes; el taller fabrica la sierra y el bidón.
- **Bifurcación (M10):** de cero a la ruta desconocida en 227 s reales (3,8 h de juego).
- **Mini-región (M11):** el loop en tres viajes, 263 s reales (4,4 h de juego), combustible 48,8 → 17,3 L, 16,7 ms por cuadro.
- **Guardado (M12):** guardar y cargar da IGUAL en las tres comparaciones.
- **Pulido (M13):** sonido, día y noche, menú de pausa y final, igual.
- **Producción:** nieve (0 copos adentro del tren a 31 km/h; al re-centrar el mundo los copos no saltan: 0,0 cm); escarcha (0,25 con calefacción, 0,98 después de 3 h sin ella, y vuelve a bajar); inicio (menú → nueva partida con el despertar de 7 s → guardar → salir al menú → continuar, todo bien).

**Ajuste de una prueba (no del juego):** la de la sonda portátil (`m5_sonda_portatil`) fallaba: el caminante automático "se moría" a mitad de camino y despertaba en el tren. Causa: la prueba empuja al jugador en horizontal por fuera del motor; en las bajadas de las dunas nuevas eso lo despegaba del piso a cada paso y el motor sumaba velocidad de caída (hasta 33 m/s), como si cayera de un edificio. A un jugador real no le puede pasar: en el juego solo el motor mueve al jugador, y empuja hacia abajo para seguir el piso. Ahora la prueba camina con el mismo empuje hacia abajo que el motor (2 m/s), igual que `m10_sonda_red`, y anota los desmayos (deben ser 0). Corrida sola: 96 % de la línea recta, 0 desmayos.

**Rendimiento en el Editor** (`m13_rendimiento`): 3,4 a 4,0 ms por cuadro (antes de producción, 1,6 ms); en la build, 2,1 a 3,5 ms (tabla de P3). La meta es 16,7 ms. Adentro del tren la prueba cuenta 1,2 millones de triángulos, pero lo que se ve son 235 mil: medido apagando cosas, las dos luces del techo dibujan el tren 6 veces cada una para su sombra (630 mil) y el sol 4 veces más por sus cascadas (345 mil). Si en una PC más débil hiciera falta, lo primero es apagar la sombra de una de las dos luces del techo.

## Cómo probarlo vos en Unity
1. Abrí `M2_Conduccion` y dale Play. Recorré la vía (sobre todo los durmientes y los rieles) y caminá a la estación, al taller y al refugio con la sonda (Q).
2. Para cambiar qué material lleva cada cosa: la tabla `MaterialMap` en `TestScenesBuilder.Places.cs`, y reconstruir la escena (**Survival en tren > M2 > Construir escena de conducción**).

## Pendientes
- Los lugares siguen siendo cajas (con buena textura): los modelos del chat de arte las reemplazan sin cambiar el juego cuando lleguen.
- Los bultos de material (lo que se suelta de la mochila) siguen con sus colores de lectura (madera, metal, tela, componentes).
