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

**Lectura**: a 60 km/h no hay temblor ni deriva perceptibles para el jugador. Las cajas tiemblan como máximo ~1 mm en la curva (menos de un píxel a 2 m de distancia). Validación manual de Tomi: "todo increíble".

**Límites de esta medición**: el tren está a menos de 1 km del origen del mundo. La precisión a varios kilómetros (motivo de la estrategia C) todavía no está medida.
