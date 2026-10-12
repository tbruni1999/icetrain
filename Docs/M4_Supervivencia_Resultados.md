# M4 — Supervivencia: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"). La escena del spike no tiene supervivencia (sus pruebas no se ven afectadas). Pruebas automáticas en `Tools/SpikeTests/m4_*.cs`. Valores generosos en `Assets/_Project/Data/Survival/SurvivalSettings` (Anexo G: balance final posterior).

## Qué hay

- **Reloj del juego** (`GameClock`, PROPUESTA): 1 s real = 1 min de juego (un día = 24 min reales). Salta horas al dormir o desmayarse. F2 lo acelera x30 para probar.
- **Cuatro estados** (`PlayerVitals`, sección 7, DECISIÓN): vida, comida (saciedad), energía y calor del cuerpo, de 0 a 100.
  - Comida: se vacía en 36 h; con hambre temprana (< 25) la energía baja ×1,5 y no hay recuperación; vacía hace 4 de daño por hora.
  - Energía: 20 h despierto de 100 a 0; ×1,5 con frío, ×1,5 con hambre, ×2 corriendo. En 0: camina al 70% y no corre (nunca bloquea controles).
  - Calor: pierde 0,25 por hora por cada grado debajo de 12 °C (afuera a -25 °C: ~11 h de juego = ~11 min reales); recupera 40 por hora en un lugar templado. En 0: 10 de daño por hora.
  - Vida: daño por hipotermia, hambre y caídas (> 8 m/s ≈ 2 m; 12 por m/s de exceso); recuperación lenta (3/h) solo alimentado, abrigado y descansado.
- **HUD discreto** (`SurvivalHud`): cuatro barras chicas (VIDA, COMIDA, ENERGÍA, CALOR), casi transparentes si todo está bien; ámbar, rojo y azul frío (sección 18.4).
- **Clima del tren** (`TrainInteriorClimate`, `ThermalSensor`): la calefacción lleva el interior a ~15 °C; las puertas abiertas dejan escapar el calor (sección 20.7). En M6 la calefacción depende del combustible.
- **Comer** (`FoodItem`): tres raciones en el estante del taller, "E comer" (+35).
- **Dormir** (`Bed`): "E dormir" en la cama (lugar miserable: +6 de energía por hora, 8 h). No con el tren en marcha.
- **Desmayarse** (`SurvivalRespawn`, DECISIÓN de Tomi): se suelta lo que se lleva, pasan 6 h, se despierta junto a la cama con vida 30, comida 30, energía 20. No se pierde progreso.
- **Feedback del cuerpo** (`BodyFeedback`): escarcha desde los bordes, imagen más fría y apagada, vaho y temblor con frío; bordes oscuros y "pestañeos" con sueño; pulso rojizo con poca vida. Postproceso de URP (se activó en el renderer: faltaban sus datos desde M0).

## Mediciones (Editor, 60 FPS)

| Prueba | Resultado |
|---|---|
| 5,45 h de juego a -25 °C (sin calefacción, paso 1) | comida 80 → 65, energía 75 → 47, calor 100 → 50 |
| `m4_temperatura.cs`: puertas cerradas | interior ~14 °C |
| idem, las dos abiertas | -14 °C a los 15 min, -20 °C a la hora |
| idem, al cerrarlas | 1,5 °C a la hora, 14 °C a las 2 h; el cuerpo baja de 100 a 88 y se recupera |
| idem, afuera | -25 °C; calor 97 → 78 en 2 h |
| `m4_comer_dormir_morir.cs`: comer | saciedad 30 → 65 |
| idem, dormir 8 h | energía 20 → 68; saciedad 65 → 51 |
| idem, dormir con el tren en marcha | "dormir (primero frená el tren)", no duerme |
| idem, desmayarse con una caja | 6 h, despierta junto a la cama (vida 30, comida 30, energía 20), la caja queda donde cayó |
| `m4_feedback_y_caida.cs`: caída de 6 m / salto | -62 de vida / sin daño |
| idem, energía 0 | no corre, ×0,70, sin bloquear el movimiento |
| idem, calor 15 afuera | escarcha 0,75, temblor 0,40, vaho visible |

Regresión con la supervivencia activa: `m3_puertas.cs`, `m3_llevar.cs` y `m2_cambio_y_parada.cs` iguales que antes.

## Pendientes y riesgos

- Al dormir o desmayarse, el jugador mantiene durante el salto de horas la temperatura de aire que tenía al empezar (si se abre una puerta del tren mientras duerme, el cuerpo no lo nota hasta despertar). Aceptable para M4.
- Sonidos (respiración, bostezo, estómago, pasos en la nieve): M13.
- Morir: la sección 27 deja abiertas otras opciones (permadeath, etc.); hoy aplica la decisión de Tomi.
- La escarcha es una textura generada por código (greybox); el arte final puede reemplazarla.
- Equilibrio interior con puertas cerradas ~13,7 °C (la calefacción "se cansa" cerca del objetivo); ajustable en `TrainInteriorClimate`.
