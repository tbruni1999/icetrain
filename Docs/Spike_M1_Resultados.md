# Spike M1 — Resultados de medición

Registro de las pruebas del spike "tren + jugador + mundo" (Anexo C). Cada estrategia se mide con las mismas pruebas para poder compararlas (paso 6 del M1).

**Cómo se mide** (componentes en `Assets/_Project/Scripts/Spike/`):
- **Temblor**: cuánto salta la cámara (o una caja) de un cuadro al siguiente respecto del tren, sin contar el movimiento suave (segunda diferencia de la posición). Máximo de ventanas de 2 s, en milímetros. 0 = perfectamente suave.
- **Deriva**: cuánto se corre el jugador quieto respecto del tren, en centímetros.
- **Corrimiento de cajas**: cuánto se movió cada caja respecto del tren desde que se asentó, en centímetros (máximo de todas).
- Escena `Spike_TrenEstable`, vía en óvalo (rectas de 600 m, curvas de radio 200 m), Editor 6000.6.5f1, física a 50 Hz, ejecución forzada en segundo plano.

## Estrategia A — el tren se mueve por el mundo

Implementación: el tren es un Rigidbody cinemático movido con `MovePosition` en FixedUpdate (con interpolación). El jugador (CharacterController) se calcula en FixedUpdate antes que el tren: si pisa una plataforma, primero se lo lleva con ella y después camina; en el aire conserva la inercia. La cámara se suaviza en coordenadas del tren. Las cajas son Rigidbody dinámicos normales (fricción por defecto), sin ningún truco.

### Jugador quieto dentro del tren (2026-10-12)

| Situación | Temblor máx. | Deriva acumulada |
|---|---|---|
| Acelerando 0 → 60 km/h y recta a 60 (≈700 FPS) | 0,02 mm | 0 cm |
| Curva completa a 60 km/h (≈700 FPS) | 0,06 mm | 0,07 cm en 100 s |
| Frenado 60 → 0 a 1 m/s² (60 FPS fijos) | 0,04 mm | 0,05 cm |

### Cajas apoyadas (9 cajas: workbench, estante en 3 alturas, cama, piso, cabina; 60 FPS fijos)

| Situación | Corrimiento máx. | Temblor máx. | Caídas |
|---|---|---|---|
| Acelerando 0 → 60 km/h | 0,01 cm | 0,14 mm | 0 |
| Curva a 60 km/h | 0,10 cm | 1,17 mm | 0 |
| Frenado 60 → 0 a 1 m/s² | 0,10 cm | 0,82 mm | 0 |
| 3 cajas soltadas con G a 60 km/h | se asientan normalmente | 0,07 mm | 0 |
| Frenazo fuerte 3 m/s² en curva | 0,08 cm | 0,25 mm | 0 |

### Bajar y subir (prueba automática `Tools/SpikeTests/bajar_subir.cs`, 60 FPS fijos)

Un "piloto automático" maneja al jugador con un teclado virtual: sale por la puerta derecha, baja los escalones al suelo y vuelve a subir. Con el tren a 10 km/h, después de bajar corre por el costado hasta alcanzar la puerta y sube en marcha. La cámara se muestrea al final de cada cuadro (lo que se dibuja); se marca "salto" todo cuadro donde la cámara se mueva a más de 10 m/s aparentes (correr + tren = 9,3 m/s).

| Situación | Resultado | Saltos de cámara |
|---|---|---|
| Tren detenido: bajar y subir, 5 veces | 10/10 OK | 0 |
| Tren a 10 km/h: bajar, correr y subir en marcha, 3 veces | 6/6 OK | 0 |

Correcciones que salieron de esta prueba:
- Al cambiar de referencia (tren ↔ suelo) la cámara perdía el suavizado durante un tic y adelantaba unos cm. Ahora las dos posiciones del suavizado se pasan a la nueva referencia.
- Al subir escalones el CharacterController sube el cuerpo de golpe (hasta 0,32 m). Ahora la cámara lo alcanza en ~0,1 s (`stepSmoothingSpeed` en PlayerMotorSettings).

**Riesgo probable (no verificado todavía)**: el CharacterController no recibe empujes de objetos cinemáticos, así que un tren en marcha que choca a un jugador parado a su costado probablemente lo atraviese o lo desplace de golpe. Solo importa si uno se queda pegado a un tren que arranca. No afecta a M1; anotarlo para cuando se diseñe qué pasa si el tren te atropella.

**Lectura**: a 60 km/h no hay temblor ni deriva perceptibles para el jugador. Las cajas tiemblan como máximo ~1 mm en la curva (menos de un píxel a 2 m de distancia). Validación manual de Tomi: "todo increíble".

### Lejos del origen del mundo (prueba `Tools/SpikeTests/lejos_del_origen.cs`, 60 FPS fijos)

Se corre todo el mundo X metros en el eje X (el tren va siempre a 60 km/h; la distancia no es velocidad) y se repite la medición: jugador quieto en el tren y 9 cajas, 75 s de recta y curva.

| Distancia al origen | Temblor jugador | Deriva jugador quieto | Temblor cajas | Corrimiento cajas |
|---|---|---|---|---|
| ~0 km | 0,06 mm | 0,07 cm | 1,2 mm | 0,10 cm |
| 5 km | 1,0 mm | 0,77 cm en 77 s | 3,0 mm | 0,14 cm |
| 10 km | (dato dudoso: 135 mm en un pico aislado; la deriva se reinició, el jugador se movió) | — | 5,9 mm | 0,27 cm |
| 20 km | 4,1 mm | 7,4 cm en 77 s | 11,6 mm | 1,2 cm |
| 100 km | 72 mm | 191 cm en 77 s | 47 mm | 64 cm |

**Lectura**: la estrategia A sola se degrada con la distancia (precisión de números con coma de 32 bits). A 5 km ya hay deriva medible del jugador (~1 cm por minuto); a 20 km es claramente visible (las cajas tiemblan ~1 cm, el jugador se corre 7 cm por minuto); a 100 km es inusable. Conclusión provisional: si la red ferroviaria va a superar unos pocos kilómetros, hace falta la estrategia C (A + re-centrar el mundo cada ~2 km). Repetir la medición de 10 km.

**Límites de esta medición**: B (mundo que se mueve) y C (re-centrado) todavía no están construidas.

## Análisis: qué tamaño de mundo pide el documento (PROPUESTA, para validar con Tomi)

El documento no fija distancias. Estimación a partir de lo que sí dice:
- Sección 14.3: "mucho espacio vacío entre señales; la soledad pierde fuerza si cada treinta segundos aparece otro edificio". A 60 km/h, 30 s son 500 m, así que los POIs deberían quedar a 1–3 km o más entre sí. Sección 2: "no colocar un POI cada pocos metros; el vacío y la espera son parte del tono".
- Sección 20: del despertar (00:00) a la estación de mantenimiento (10:00) hay un viaje; a 40–60 km/h, unos minutos de viaje ya son 2–5 km.
- Sección 21.2 (V1): estación + 2–4 POIs + bifurcación, con distancias de silencio entre ellos: del orden de **10–20 km de vía** solo para la slice.
- Sección 22.4 y Anexo D: el juego completo son regiones/chunks a lo largo de una red con bifurcaciones: **decenas o cientos de km**.

Conclusión: la V1 ya pasa el límite de ~5 km donde la estrategia A sola empieza a degradarse.

## Análisis: las tres estrategias según optimización (expectativa razonada, falta medir B y C)

| | A: tren se mueve | B: mundo se mueve | C: A + re-centrado |
|---|---|---|---|
| Costo por cuadro | Mínimo (se mueve 1 tren) | Alto: mover todo lo cargado (terreno, POIs, colisiones) cada cuadro | Mínimo entre re-centrados |
| Costo puntual | — | — | Un pico al re-centrar (cada ~2 km): mover las raíces de las regiones cargadas y reubicar físicas |
| Física | Normal | Los colisionadores "estáticos" pasan a ser móviles: más costo y más riesgo | Normal |
| Optimizaciones de Unity que asumen objetos quietos (batching estático, luz horneada, occlusion culling horneado, sondas de luz) | Compatibles | Se rompen o no sirven | Riesgo a verificar: hay que confirmar cuáles toleran que se muevan las regiones al re-centrar |
| Lejos del origen | Falla desde ~5 km (medido) | Sin problema | Sin problema |
| POIs, guardado, sonda (Anexo C.1) | Simple | Complicado | Simple si las posiciones se guardan como "tramo + distancia" (ya es así) |

Expectativa: C es la mejor para optimización y escala; B es la peor (paga el costo todos los cuadros y anula optimizaciones de objetos estáticos); A sola no alcanza para el tamaño del mundo. Esto es razonamiento, no medición: el Anexo C pide medir antes de elegir.

## Estrategia C — A + re-centrado del mundo (medido)

Implementación: `Scripts/World/FloatingOrigin.cs`. Cuando el jugador se aleja más de `recenterDistanceMeters` (2000 m por defecto, en `Data/World/FloatingOriginSettings`) del origen, en el tic de física se mueven todos los objetos raíz de las escenas cargadas, se sincroniza la física y se avisa a quien guarda posiciones propias (`IOriginShiftListener`, hoy solo PlayerMotor). Lleva el origen absoluto en doble precisión.

### ¿Se nota el re-centrado? (`Tools/SpikeTests/recentrado.cs`, umbral bajado a 150 m para forzar muchos re-centrados, 60 km/h, 60 FPS)

| Medida | Resultado |
|---|---|
| Re-centrados en 75 s | 6 |
| Costo máximo de un re-centrado (escena del spike) | 0,55 ms |
| Velocidad aparente máxima de cámara y tren en los cuadros de re-centrado | 16,7 m/s = la velocidad real del tren (ningún salto) |
| Temblor jugador / deriva quieto | 0,03 mm / 0,02 cm en 77 s |
| Temblor / corrimiento de cajas | 1,12 mm / 0,10 cm, 0 caídas |

### Lejos del origen con C activa (`Tools/SpikeTests/lejos_del_origen.cs`)

| Distancia | Temblor jugador | Deriva quieto | Temblor cajas | Corrimiento cajas |
|---|---|---|---|---|
| 10 km | 0,13 mm | 0,07 cm | 1,2 mm | 0,09 cm |
| 100 km | 0,14 mm | 0,06 cm | 3,1 mm (pico del re-centrado inicial) | 0,09 cm |

Igual que en el origen (A sola a 100 km: 72 mm, 191 cm, 47 mm, 64 cm).

## Estrategia B — prototipo para medir costo (`Tools/SpikeTests/costo_b_vs_c.cs`)

Prototipo mínimo: en cada tic de física se mueve todo el mundo para que el tren quede quieto (solo traslación, sin rotar el mundo en las curvas). Mundo de prueba con 5.000 objetos con colisión. Sin límite de FPS, medido con ProfilerRecorder en el Editor.

| | C entre re-centrados | C al re-centrar | B |
|---|---|---|---|
| CPU hilo principal por cuadro | 1,55 ms | — | 1,96 ms (+26 %) |
| Física por cuadro | 0,008 ms | — | 0,025 ms (×3) |
| Mover el mundo | — | 1,15 ms promedio, 2,17 ms peor, una vez cada ~2 km | 1,07 ms en cada tic (50 por segundo) |

A 60 FPS, B gasta ~0,9 ms por cuadro (≈5 % del presupuesto de 16,7 ms) solo en mover 5.000 cubos simples, y crece con todo lo que tenga una región real. Además (no medido, razonado) impide usar optimizaciones que el documento quiere para POIs y exteriores (sección 23, Anexo E): geometría estática agrupada, luz horneada, occlusion culling horneado. Y complica POIs, físicas exteriores y persistencia (Anexo C.1).

## Elección: estrategia C (2026-10-12)

Tomi delegó la elección ("elegí lo mejor en base al documento"). Se elige **C**:
- A sola no alcanza: la V1 ya necesita ~10–20 km de vía y A se degrada desde ~5 km (medido).
- B cuesta procesador en cada tic y choca con las optimizaciones del documento (medido el costo; el resto razonado).
- C conserva la estabilidad de A (medido igual) a cualquier distancia (medido hasta 100 km), con un costo puntual de ~1–2 ms cada ~2 km.

Pendientes que quedan para milestones posteriores (no bloquean M1):
- Repetir las mediciones de costo en una build (las de arriba son en el Editor).
- Streaming (Anexo C.2): las regiones que se carguen deben aparecer ya desplazadas según el origen actual. FloatingOrigin mueve todas las escenas cargadas, así que las escenas aditivas encajan.
- Verificar con el primer POI con luz horneada (M6) que la iluminación y las sondas de luz toleren el re-centrado. Si no, re-centrar solo con el tren detenido o lejos de POIs.
- Guardado (M12): guardar la posición ferroviaria (tramo + distancia) y el origen absoluto.
- Riesgo de bajar/subir ya anotado: el tren no empuja al jugador parado a su costado.
