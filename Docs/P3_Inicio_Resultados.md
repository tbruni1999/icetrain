# Producción P3 — Inicio del juego: resultados

Parte del plan de la noche. Que el juego arranque como un juego: un menú principal, opciones, el despertar de la sección 20.1 y una versión exportada (build) de Windows con su medición de rendimiento.

## Qué hay
- **Menú principal** (`Scenes/Menu_Principal`, primera escena del juego): el vagón al atardecer en la nieve, con la nevada cayendo y las ventanas encendidas; la cámara gira despacio. **Nueva partida**, **Continuar** (si hay una partida guardada), **Opciones** y **Salir**. Mouse o flechas + Enter. Se regenera con **Survival en tren > Juego > Construir menú principal**.
- **Opciones** (en el menú principal y en la pausa): volumen, sensibilidad del mouse, calidad (alta / media / baja: sombras, cuánta nieve cae y el detalle del terreno lejano) y pantalla completa. Se guardan en `opciones.json`, al lado de las partidas (nada de PlayerPrefs).
- **El despertar** (sección 20.1): con "Nueva partida", la pantalla arranca negra con el viento y el tren; el jugador está acostado junto a la cama mirando el techo, la vista se abre y se levanta despacio (7 s). Sin textos. No aparece al continuar una partida ni en las pruebas.
- **Menú de pausa** (Escape): suma **Opciones** y **Salir al menú**.
- **Continuar** carga la partida guardada (como F9).
- **Build de Windows**: **Survival en tren > Juego > Exportar build de Windows** → `Builds/Windows/SurvivalEnTren.exe` (fuera de Git). Con `SurvivalEnTren.exe -medicion` el juego entra solo, recorre las situaciones de la prueba de rendimiento y escribe `medicion.txt` en su carpeta de datos.

## Mediciones

| Prueba (`p3_inicio.cs`, del menú a la partida y de vuelta) | Resultado |
|---|---|
| Opciones | se abren; volumen 0,55, sensibilidad 1,7 y calidad media se guardan y se vuelven a leer iguales |
| Nueva partida | carga la escena de juego; al empezar el jugador está quieto, mirando el techo (−75°), **a 0,00 m del lugar de despertar de la cama**; el despertar dura 7,0 s; después se camina, mirando al frente (8°), con la sensibilidad de las opciones (1,7) |
| Guardar y salir al menú | guardó; volvió al menú con el tiempo corriendo; "Continuar" disponible |
| Continuar | vuelve a la escena de juego **con la partida aplicada** (13,1 h, la hora guardada) y sin el despertar |

### La build de Windows (medida en esta PC, no en la de referencia)

`SurvivalEnTren.exe -medicion` a 1920 × 1080, calidad alta, en un **AMD Ryzen 7 5800X3D (16 hilos), NVIDIA GeForce RTX 3070 (8 GB), 32 GB de RAM**. La build pesa 662 MB y se exportó en 2,4 minutos.

| Situación | Cuadro promedio | FPS promedio | Peor cuadro | 1 % más lento |
|---|---|---|---|---|
| Tren quieto, adentro | 3,0 ms | 333 | 8,6 ms | 3,5 ms |
| Manejando a 38 km/h, adentro | 3,5 ms | 286 | 4,3 ms | 3,9 ms |
| Afuera, junto a la estación | 2,8 ms | 361 | 5,1 ms | 3,3 ms |
| Adentro del taller | 2,3 ms | 426 | 3,3 ms | 2,8 ms |
| En la nieve, mirando el horizonte | 2,1 ms | 467 | 4,2 ms | 2,5 ms |

La meta del Anexo E.1 es 16,7 ms por cuadro (60 FPS a 1080p): el juego usa entre un 13 y un 21 % de ese tiempo, con el arte, la atmósfera, el terreno y las sombras nuevas. Esta PC es potente; la de referencia (todavía sin definir) puede dar números más altos.

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
1. Abrí `Assets/_Project/Scenes/Menu_Principal` y dale Play.
2. **Opciones**: cambiá la sensibilidad y el volumen (clic en la barra o flechas izquierda/derecha); "Volver".
3. **Nueva partida**: mirá el despertar. Después jugá un rato, apretá Escape → **Guardar partida** → **Salir al menú**.
4. **Continuar**: volvés a donde guardaste.
5. Para probar la build: abrí `Builds/Windows/SurvivalEnTren.exe` (doble clic).

## Pendientes
- El nombre del juego en el menú es el de trabajo ("Survival en tren": la sección 27 deja el nombre abierto).
- Una sola partida guardada (varias ranuras quedan para más adelante).
- La medición de la build es en esta PC: la PC de referencia todavía no está definida (Anexo E.1).
