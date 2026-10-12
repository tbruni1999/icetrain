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
