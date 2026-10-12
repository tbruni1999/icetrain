# M5 — Sondas: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"). Pruebas automáticas en `Tools/SpikeTests/m5_*.cs`. Valores en `Assets/_Project/Data/Probe/ProbeSettings` (Anexo B.3). Sin paquetes nuevos: las pantallas son piezas 3D y texto 3D (TextMesh).

## Qué hay

- **Señales** (`ProbeSignal`, sección 13.1): los 5 tipos (calor residual, estructura bajo nieve, masa metálica, emisión automática, hidrocarburo), con un ID estable, intensidad (una señal débil se detecta más cerca) y un **ancla en la vía**: el punto del tramo más cercano, de qué lado está y a cuántos metros. Tres señales de prueba en la escena; los POIs reales llegan en M6.
- **Sonda principal** (`ProbeScanner`, en el tren): lista de señales al alcance (2,5 km para intensidad 1) con dirección y distancia **aproximadas** (error de ±20° y ±15% en el borde del alcance, ±3° y ±3% muy cerca; distancia redondeada a 10/25/50/100 m), y la **distancia por la vía** al punto de la vía más cercano a la señal ("80 m adelante, 160 m a la izquierda"). Si la señal está en otra rama de vía o fuera de alcance por la vía: "fuera de la ruta actual". El error es estable por señal (no tiembla al mirarla).
- **Consola en la cabina** (`ProbeConsole`, secciones 13.1 y 19): pantalla verde tipo CRT con barrido, anillos de 1,25 y 2,5 km y puntos que se encienden al pasar el barrido. Debajo, la señal elegida. **E** en la consola fija la señal como objetivo (punto ámbar) y pasa a la siguiente.
- **Sonda portátil** (`PortableProbe`, sección 13.2): **Q** la saca y la guarda. Dial con arriba = hacia donde mira el jugador:
  - aguja **blanca** al tren, aguja **ámbar** al objetivo fijado en la consola (con el mismo tipo de error que la consola, más chico de cerca);
  - "TREN ~240 M" / "OBJ. ~80 M" (sin lectura a más de 3 km);
  - barra **SEÑAL** de 5 rayas: baja con la distancia y con la interferencia.
  - Con una caja en la mano se guarda sola y vuelve al soltarla.
- **Interferencias** (`ProbeInterference`): zona esférica donde las agujas tiemblan y la señal baja. En la escena, la masa metálica tiene una de 40 m. M6 la usa en interiores profundos y estructuras; tormentas, más adelante.

## Mediciones (Editor, 60 FPS)

| Prueba | Resultado |
|---|---|
| Consola desde el arranque | 2 señales detectadas; estructura: "~180 M ADELANTE IZQ.", "por la vía 80 m adelante, 160 m a la izq."; masa metálica: por la vía 430 m adelante, 280 m a la der.; calor residual (débil, 1 km atrás) no detectado hasta acercarse |
| `m5_sonda_portatil.cs`: IDA a la estructura solo con la aguja ámbar | 185 m en línea recta, caminó 179 m en 40 s y se frenó cuando la sonda dijo "~10 M" (a 15 m reales) |
| idem, VUELTA al tren solo con la aguja blanca | 164 m en línea recta, caminó 157 m en 31 s, terminó a 15 m del centro del tren (al costado) |
| idem, al lado del metal (10 m, dentro de la zona) | la aguja ámbar se mueve 17° en 3 s; interferencia 0,52; señal 0,48 |
| idem, a 70 m (fuera de la zona) | la aguja se mueve 0–4° (casi quieta); señal 0,98 |
| idem, tomar una caja | la sonda se guarda; al soltarla vuelve |
| idem, Q (guardar) | se guarda |

La prueba de la sección 24 ("ir a una señal y volver sin minimapa") la hace un "jugador" automático que solo mira la sonda: gira hacia donde apunta la aguja y camina. La prueba real es manual: la hace Tomi.

Regresión con las sondas en la escena: `m2_cambio_y_parada.cs` (parada a 0,15 m del cartel), `m3_puertas.cs`, `m3_llevar.cs` (temblor 0,1 mm), `m4_temperatura.cs`, `m4_comer_dormir_morir.cs`, `m4_feedback_y_caida.cs`, `parado_en_caja.cs` (0,00 cm) y `recentrado.cs` (6 re-centrados, 0,46 ms, temblor de cajas 1,06 mm) iguales que antes.

## Pendientes y riesgos

- El aparato portátil está colgado de la cámara: pegado a una pared puede meterse en ella (se ve "adentro" de la pared). Se resuelve con una cámara aparte para lo que se tiene en la mano cuando haya arte final (M13).
- Las agujas apuntan al centro del tren, no a la puerta.
- El "OBJ. ~10 M" mínimo: de cerca la sonda no da más precisión que 10 m; la señal hay que encontrarla mirando (la sección 25.1: "orientar sin revelar todo").
- Sin sonidos (pitido, estática): M13. Sin mapa ni balizas (fuera de M5).
- La ayuda en pantalla (F1) dice "Q sonda" también en la escena del spike, donde no hay sonda.
