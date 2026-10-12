# M2 — Conducción: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"). Pruebas automáticas en `Tools/SpikeTests/`.

## Qué hay

- **Física de conducción** (`TrainDriveSettings`, `TrainControls`, `TrainDynamics`): reversor adelante/neutro/atrás (solo se mueve con el tren detenido), 4 posiciones de potencia (10/20/30/40 km/h, editables), 4 de freno, rozamiento. Potencia y freno se manejan como una palanca combinada: nunca hay potencia y freno a la vez. El tren arranca estacionado con todo el freno puesto.
- **Cabina** (`TrainCab`, `CabInstruments`): pupitre con tres palancas (reversor, potencia, freno) y velocímetro de aguja (0–60 km/h, marca roja en 40). Mirar el pupitre + E toma los controles: W/S potencia-freno, R/F reversor, E suelta. Si se intenta mover el reversor en marcha, la palanca se sacude y vuelve (sección 18: feedback físico antes que texto).
- **Interacción mínima** (`IInteractable`, `PlayerInteractor`): mirar + E, punto de mira en el centro. Base del sistema completo del M3.
- **Cambio de vía** (`RailSwitch`, `SwitchLever`): nodo de la red (Anexo C.3) que elige la salida según su palanca; palanca física al costado de la vía con indicador (cara clara = directa, cara ámbar = desvío). Trabado si hay un tren encima. Pasar el cambio "de espaldas" siempre vuelve al tramo de entrada (cambio a resorte).
- **Vía muerta con tope**: el tren se detiene solo antes del final (`TrainMover` mira si la punta pasaría un final de vía).
- **Cartel de parada** a 150 m del inicio de la recta para practicar.

## Mediciones (Editor, 60 FPS)

| Prueba (`conduccion_fisica.cs`) | Resultado |
|---|---|
| 0 → 10 / 10 → 20 / 20 → 30 / 30 → 40 km/h | 8,0 / 7,3 / 7,5 / 7,5 s |
| Velocidad estable en potencia 1 y 4 | 9,9 y 40,0 km/h |
| Sin potencia ni freno, 10 s desde 40 km/h | baja a 38,9 km/h (inercia) |
| Frenado desde 40 km/h | freno 2: 110 m en 20 s; freno 4: 49 m en 9 s |
| Reversor con el tren en marcha | bloqueado; la palanca se sacude y vuelve (30°–41°, vuelve a 35°) |
| Marcha atrás en potencia 1 | -9,5 km/h |

| Prueba (`m2_cambio_y_parada.cs`) | Resultado |
|---|---|
| Palanca con el tren lejos | cambia a desvío |
| Recorrido con el cambio en desvío | Tramo_1a_Recta > Tramo_Desvio_ViaMuerta |
| Palanca con el tren encima del cambio | trabada |
| Tope de la vía muerta (a 20 km/h) | se detiene con la punta a 1,6 m del final |
| Marcha atrás por el cambio | Tramo_Desvio_ViaMuerta > Tramo_1a_Recta |
| Temblor del jugador en la cabina pasando el cambio | 0,03 mm |
| Parada en el cartel (freno 2 desde 20 km/h, frenando en el punto calculado) | la punta queda a 0,14 m del cartel |

La prueba de parada demuestra que la física permite parar con precisión; la prueba real de la sección 24 ("conducir y detener el tren en un punto sin errores") es manual: la hace Tomi.

## Pendientes y riesgos

- Contra el tope el tren frena en seco. Qué pasa en un choque fuerte (daños, sacudón al jugador) queda para más adelante.
- Cambio "a resorte": se puede endurecer (por ejemplo, descarrilar o trabarse) cuando se diseñe el rail gating (M10).
- Sonidos de palancas, motor y frenos: M13.
- Mientras se maneja el jugador queda parado donde apretó E (no se sienta). Revisar en M3 con el sistema de interacción completo.

## Tope con resorte (pedido de Tomi: "un mini rebote")

Antes el tren frenaba en seco contra el tope y todo salía volando. Ahora el tope es un resorte amortiguado (`TrainDriveSettings`: rigidez 30, amortiguación 10, la amortiguación entra en los primeros 12 cm) y la placa del tope se hunde (`BufferStop`). Medido (`Tools/SpikeTests/m2_tope_ajuste.cs`):

| Choque a | Compresión | Golpe máx. | Rebote |
|---|---|---|---|
| 5 km/h | 12 cm | 0,8 g | 1,2 km/h |
| 10 km/h | 22 cm | 2,5 g | 1,7 km/h |
| 20 km/h | 40 cm | 5,6 g | 3,1 km/h |
| 30 km/h | 58 cm | 8,9 g | 4,6 km/h |

A velocidad de maniobra es un toque; a 20–30 km/h es un choque (frenar en medio metro es violento) y las cajas se sacuden. El freno de seguridad (si un bogie llegara al final de la vía) no se activó en ninguna prueba. Por indicación de Tomi no se pule más: no es prioritario.
