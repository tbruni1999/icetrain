# M11 — Mini-región: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"). Pruebas automáticas en `Tools/SpikeTests/m11_*.cs`. Este milestone lo planifiqué yo sin pedir aprobación, porque Tomi pidió seguir sin preguntar (2026-10-14). Todo lo que sigue es PROPUESTA y se puede cambiar.

## Qué hay

El Anexo G pide "2–4 POIs pequeños para reunir recursos del proyecto de vía; loop completo repetible hasta abrir la ruta". Con la estación del M6 ahora son **tres lugares**. Cada uno tiene una función clara y un botín con lógica (sección 15: "un quirófano no es un cofre random").

| Lugar | Dónde (la sonda lo muestra como…) | A pie desde la vía | Qué hay |
|---|---|---|---|
| **Estación de mantenimiento** (M6) | estructura bajo nieve, junto a la recta del inicio | ~160 m | depósito de combustible, bomba manual, ración, pallet, armario, colchón, tablero, bultos sueltos y, en la segunda sala, el plano del estante reforzado. La aguja de cambio **ya no está acá** |
| **Taller ferroviario** (nuevo) | masa metálica, junto a la recta este | ~270 m | un galpón de chapa con la puerta hacia la vía: **2 armarios metálicos (chapas ×6)**, **1 tablero (componentes ×2)** y, sobre la mesa de trabajo, **la aguja de cambio** (la pieza especial del M10). Rieles viejos contra la pared. Está dentro de la zona de interferencia del metal: **la sonda portátil tiembla adentro** |
| **Refugio** (nuevo) | calor residual (señal débil: se detecta recién a ~625 m), en la recta oeste | ~320 m | una casilla de madera con la puerta hacia la vía y **una estufa con brasas** que todavía da luz: **2 pallets (madera ×8)**, **1 colchón (tela ×3)** y **2 raciones** en un estante |

- Los dos lugares nuevos son galpones con **puerta corrediza** (cerrada al llegar, E para abrir) y **sin viento adentro**: el aire está a −10 °C contra −25 °C afuera, como en la estación.
- Se **quitaron** los objetos provisorios que había puesto junto a la bifurcación en el M10. Ahora lo del proyecto de vía se junta en los lugares.
- **Cuentas del mundo** (todo lo que hay contra todo lo que se puede gastar):

  | Material | En el mundo | Usos (cama, estante, sierra, bidón, bifurcación) | Sobra |
  |---|---|---|---|
  | madera | 22 | 15 | 7 |
  | chapas | 11 | 10 | 1 |
  | tela | 8 | 5 | 3 |
  | componentes | 4 | 1 | 3 |

  Las chapas están **casi justas** (sobra una). Si se usan en otra cosa, no alcanzan para todo. Es una decisión de diseño de la sección 6.3 ("usar recursos en mejorar la casa ahora o guardarlos para abrir una región nueva"); si molesta, se agrega un armario más.

### El loop
1. **Viaje 1, estación:** chapas y componentes (y combustible, si hace falta).
2. **Viaje 2, taller:** la aguja de cambio (y más chapas).
3. **Viaje 3, bifurcación:** romper el hielo y aportar. Ir al refugio por la madera, volver y terminar. Accionar la palanca y entrar a la ruta desconocida.

La mochila es chica (6×5 casillas), así que hay que elegir qué llevar en cada viaje.

## Mediciones (Editor, 60 FPS)

| Prueba | Resultado |
|---|---|
| `m11_lugares.cs` (paso 1) | taller a 9 m de la señal de masa metálica; refugio sobre la señal de calor residual (intensidad 0,25: se detecta desde 625 m); **taller:** 3 desmontables (chapas ×6, componentes ×2) y la aguja; **refugio:** 3 desmontables (madera ×8, tela ×3) y 2 raciones; **estación:** igual que antes sin la aguja; objetos provisorios de la bifurcación: 0. **Entrar caminando de verdad** (teclado virtual, W): en los dos la puerta está cerrada al llegar, apuntándola dice "abrir", se abre y se entra caminando. Aire adentro **−10 °C** (afuera −25 °C). Sonda portátil adentro del taller: **interferencia 0,58, calidad 0,42**. La aguja quedó sobre la mesa (a 1,01 m; la mesa mide 0,90). Al desarmar un armario con la mochila casi llena, las 3 chapas quedaron en bultos **adentro del galpón** (ninguna afuera) |
| `m11_loop.cs` (paso 2, loop completo en tres viajes, tren manejado de verdad) | **viaje 1** (estación): manejó 24 s y frenó a 2 m del punto elegido; juntó chapas 5 y componentes 2 (22/30 casillas); 3,3 L. **Viaje 2** (taller): 52 s, a 2 m del punto, el tren a 271 m del taller; la aguja (25/30 casillas); 7,1 L. **Viaje 3:** 112 s por la curva, a 2 m del punto, el tren a 67 m del cambio y a 320 m del refugio; rompe el hielo, aporta (nivel 1); trae la madera del refugio, vuelve y termina (**nivel 3/3, Reparado**); palanca en desvío; el tren entra 163 m a la ruta desconocida; 21,2 L. **Total: 264 s reales (4,4 h de juego), combustible 50,0 → 18,4 L**, temblor del jugador manejando máx. 0,07 mm; cuerpo: vida 100, comida 68, energía 53, calor 100. **Tiempo por cuadro: 16,7 ms promedio en los tres viajes** (máx. 22, 21, 21): repetir el loop no degrada el rendimiento |
REGRESION_PLACEHOLDER

Una nota sobre las pruebas: en esta sesión vi que la consola de Unity **no siempre se limpia** entre corridas, y el lanzador podía leer un resultado viejo con la misma etiqueta. Ahora `Tools/SpikeTests/run_test.sh` cuenta los mensajes que ya había y espera uno **nuevo**. El lanzador y la regresión completa (`Tools/SpikeTests/regresion.sh`) quedaron en el repositorio.

## Cómo probarlo vos en Unity (paso a paso)

1. Abrí la escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion` y dale Play.
2. En la consola de la cabina fijá las señales con **E** y mirá cuáles hay. La de calor residual no aparece hasta acercarte a la recta oeste (es débil).
3. **Estación:** como siempre. Fijate que en la segunda sala ya no está la aguja; queda solo el plano.
4. **Taller ferroviario:** frená sobre la recta este más o menos a la altura de la señal de masa metálica (la consola te dice cuántos metros "por la vía"). Caminá con la sonda portátil (**Q**) hacia el este, unos 270 m. Abrí la puerta (**E**) y entrá. Mirá cómo tiembla la aguja de la sonda adentro, desarmá los armarios con la barreta y tomá la **aguja de cambio** de la mesa.
5. **Refugio y bifurcación:** seguí con el tren por la curva hasta la recta oeste y frená unos 100 m antes del cambio. El refugio está unos 320 m al oeste de la vía (la señal de calor residual). Adentro hay leña (pallets), un colchón y dos raciones, y la estufa con brasas.
6. Reparación, palanca y ruta: como en el M10.

## Pendientes y riesgos

- **El refugio no es más cálido que la estación:** la estufa da luz pero no calor extra. La zona sin viento da el mismo −10 °C en los tres lugares. Si querés que el "calor residual" se sienta, se puede sumar un valor de calor por lugar (es poco trabajo).
- **El combustible alcanza para una vuelta:** el loop completo gastó 31,6 L de los 50 L iniciales. Para repetir viajes hay que cargar en la estación (depósito de 150 L con la bomba) o en el vehículo abandonado (15 L).
- **Las chapas están casi justas** (11 en el mundo, 10 de usos posibles). Ver arriba.
- **Las caminatas largas a pie** (270 y 320 m) son la mitad del tiempo de una expedición. Están bien para la sensación de vacío (sección 14.3), pero si se sienten largas se pueden acercar los lugares a la vía.
- Sin amenazas, sin animales y sin más regiones (fuera de la V1). Lo tomado de los lugares no se guarda hasta el M12. Todo tiene ID estable: `salv_taller_*`, `salv_refugio_*` y los IDs de las señales.
