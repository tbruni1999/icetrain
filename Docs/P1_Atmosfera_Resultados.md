# Producción P1 — Atmósfera y clima: resultados

Después de la V1 (el documento maestro termina en el M13), Tomi pidió "seguir produciendo el juego porque lo veo super verde" y eligió empezar por **atmósfera y clima**. Después pidió también **revisar la iluminación y las sombras** ("las sombras en general se ven recontra mal"). Base: sección 19, Anexo E.2 y E.3, y la guía de arte (`Fuentes/Guia_arte_MR04.md`). Todos los valores son PROPUESTA y están en datos.

## Qué hay

### Post-proceso (la guía de arte)
- Tonemapping (ACES), bloom bajo, viñeta leve y una corrección leve hacia frío, en `Data/World/Atmosfera_Postproceso.asset`.
- El feedback del cuerpo (M4: escarcha en los bordes con frío, oscuro con sueño) ahora **se suma** a esta base en vez de pisarla.
- **Sombras de contacto (SSAO)**: un efecto de URP que oscurece los rincones y las uniones.

### Cielo de invierno
- Un cielo propio (`Shaders/CieloNiebla.shader`): en el horizonte toma **el color de la niebla**, así la nieve lejana se funde con el cielo sin línea; arriba, gris azulado. El sol, a tamaño real con su halo. El día y la noche (M13) lo siguen manejando.

### Nieve que cae y viento (`Data/World/Atmosfera.asset`)
- Copos alrededor del jugador y rachas finas a ras del suelo; el viento sopla con ráfagas y la nevada sube y baja despacio con las horas.
- **Adentro del tren no nieva** (una caja invisible del tamaño del tren apaga los copos, aunque el tren se mueva), y en los galpones los techos los frenan.
- Con el tren andando, la nieve se suelta donde va a estar el jugador: se ve pasar por las ventanas.

### Escarcha en las ventanas del tren
- Dos capas por vidrio (la escarcha del borde y la que cubre todo, de la librería), del lado de adentro. **Con la calefacción andando queda un poco en los bordes; si el tren se enfría, crece hasta cubrir los vidrios**, y al volver la calefacción se derrite despacio.

### Iluminación y sombras (pedido de Tomi)
- **El serrucho de luz bajo el techo**: el sol se colaba por la unión fina entre el techo y las paredes del modelo. Ahora las cajas grises del tren (invisibles) hacen sombra: son gruesas y cerradas, y la luz entra y sale solo por las ventanas y las puertas.
- **Sombras del sol**: estaban con una sola "cascada" para 50 m (cada punto de sombra cubría ~2,4 cm) y sin sombras suaves: bordes en escalera. Ahora: 4 cascadas (mucho detalle cerca), sombras suaves, menos desfase.
- **Luces de adentro**: las dos del techo hacen sombra (la luz cálida sale a la nieve solo por las ventanas); una luz chica sobre el pupitre (la cabina quedaba muy oscura); y **el resplandor naranja de la estufa**, que tiembla como un fuego y se apaga si la calefacción se queda sin combustible.

### Suelo con relieve y restos en el horizonte (`Data/World/Terreno.asset`)
- El plano blanco pasó a un **terreno de Unity** con dunas suaves alargadas por el viento, con la textura de nieve de la librería (compacta junto a la vía, pisada cerca de los lugares).
- **Plano cerca de la vía (12 m) y de los lugares (14 m)**: lo que se camina no cambia. Y un tope de pendiente: si las dunas quedan más empinadas que 15°, se achatan (quedaron en 13,3°).
- **12 restos altos medio enterrados** lejos de la vía (torres de alta tensión, chimeneas, postes de una línea caída, un tanque de agua), que se recortan en la niebla.
- Para el terreno se activó el **módulo de terreno de Unity** (viene con Unity, no se descarga nada). Justificación (Anexo A.4, "paquetes Unity oficiales cuando resuelven una necesidad real"): da el relieve con detalle según la distancia, su colisión y la mezcla de texturas, y Tomi lo puede esculpir y pintar a mano en el editor.

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

## Cómo probarlo vos en Unity (paso a paso)

1. Abrí `Assets/_Project/Scenes/Pruebas/M2_Conduccion` y dale Play.
2. **Adentro**: mirá la unión del techo con las paredes (ya no hay luz colada), la estufa naranja y las manchas de sol de las ventanas.
3. **Afuera**: bajá del tren; cae nieve, hay rachas a ras del suelo, el suelo tiene dunas suaves y a lo lejos se recortan torres y chimeneas en la niebla.
4. **Escarcha**: con **F1** (panel de pruebas) y **F2** (acelerar el tiempo), apagá la calefacción dejando el tanque vacío o abrí las puertas un rato: los vidrios se escarchan; al volver el calor, se despejan.
5. **De noche** (F2 hasta las 18:00): el cielo y la niebla se oscurecen juntos y las ventanas del tren iluminan la nieve.
6. Para ajustar: `Data/World/Atmosfera.asset` (nieve, viento, escarcha), `Data/World/Atmosfera_Postproceso.asset` (post-proceso), `Data/World/Terreno.asset` (dunas, zonas planas, restos; reconstruir la escena después) y `Data/World/Cielo.mat` (colores del cielo).

## Pendientes y riesgos
- **Tormentas** como sistema (con efecto en el juego), huellas en la nieve y montañas de fondo (Megascans: licencia sin verificar) quedan fuera.
- El terreno se genera al armar la escena: si Tomi lo esculpe a mano, reconstruir la escena lo pisa (habría que guardar el terreno aparte).
- La textura de nieve se repite a lo lejos; se puede mezclar una segunda escala más adelante.
