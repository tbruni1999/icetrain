# M6 — Primer POI + combustible: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"). Pruebas automáticas en `Tools/SpikeTests/m6_*.cs`. Valores en `Assets/_Project/Data/Train/FuelSettings` y `Data/Survival/SurvivalSettings` (generosos, para probar: Anexo G). Sin paquetes nuevos.

## Qué hay

### Combustible del tren (sección 8.1)
- **Tanque** (`FuelTank`): 200 L; el tren arranca con 50 L (reserva baja: sección 4). Nada genera combustible: solo se saca de fuentes con cantidad fija.
- **Gasto** (litros por hora de juego; 1 s real = 1 min de juego):
  - el **motor** gasta 2 / 4 / 6 / 8 L/h según la posición de potencia (1 a 4), solo con el reversor puesto; parado o en neutro no gasta;
  - la **calefacción** gasta 1,5 L/h encendida, también con el tren parado y mientras se duerme (`TrainInteriorClimate`).
- **Sin combustible:** el motor no da potencia (el tren sigue por inercia y se frena con el freno) y la calefacción no calienta (el interior se enfría hasta la temperatura de afuera).
- **Medidor en la cabina** (`FuelGauge`): aguja de vacío (E, izquierda) a lleno (F, derecha), a la derecha del velocímetro, y una **luz roja de reserva**: fija con menos del 15%, parpadeando con el tanque vacío. El panel F1 muestra los litros.
- **Boca de carga** en el costado derecho del chasis, atrás (`FuelPort`): con un bidón con combustible en la mano, **mantener E** lo vuelca en el tanque a 2 L/s.

### Equipo y bidón
- **Bidón** (`FuelCan`): 20 L, se lleva en la mano; pesa 1,5 kg vacío + 0,85 kg por litro (lleno: 18,5 kg, camina al 63%). Es de **una mano**: se puede seguir usando la sonda portátil con el bidón (con una caja, no). Dos bidones vacíos en el piso del tren, junto a la puerta de atrás.
- **Perchero** junto a la puerta de atrás: el **traje** (E se lo pone o lo deja; abriga: el cuerpo pierde la mitad de calor afuera) y la **barreta** (E la toma; es la herramienta del inicio, sección 4).
- **Sifón** en el **vehículo abandonado** (a unos 40 m del tren, a la derecha): con el bidón en la mano, mantener E en su tanque (la tapa amarilla). Lento: 0,4 L/s; tiene 15 L.
- **Mantener E de a poco** (`IChanneledInteractable`): sifonar, bombear y cargar avanzan mientras E sigue apretado y se cortan al soltarlo sin perder lo ya movido. La indicación muestra litros. Con las manos ocupadas por el bidón, E usa el objeto que se mira (si no, E suelta lo que se lleva, como antes).

### Estación de mantenimiento (secciones 15 y 20.3 a 20.7)
Hecha a mano, en la señal "estructura bajo nieve" (a unos 180 m del arranque, hacia el otro lado de la vía), con la puerta mirando a la vía:
- **Puerta congelada** (`FrozenDoor`): un bloque de hielo azul traba la puerta. Sin barreta: "el hielo la traba: hace falta una barreta". Con la barreta, mantener E 4 s lo adelgaza y lo rompe en pedazos; el avance se conserva si se suelta. Después es una puerta corrediza común.
- **Sala principal:** depósito de combustible (150 L, se saca con la **bomba manual**), mesa con la **bomba manual vieja** (se toma con E) y una **ración**, tablones, un rollo de tela y una chapa sueltos (se llevan en la mano; sus usos son de M8).
- **Segunda sala** al oeste, vacía por ahora (la tentación opcional de 20.6; el blueprint llega en M9).
- **Adentro:** sin viento pero frío: **-10 °C** (afuera -25 °C; `SurvivalSettings.unheatedShelterOffsetC`), y la sonda pierde algo de señal (zona de interferencia de 10 m).
- Medio enterrada: nieve acumulada contra el lado norte y el oeste y sobre la mitad del techo. Arte provisional (cajas).

## Mediciones (Editor, 60 FPS)

| Prueba | Resultado |
|---|---|
| `m6_combustible.cs`: tren parado | 1,50 L/h (calefacción) |
| idem, potencia 2 / potencia 4 | 5,50 / 9,50 L/h |
| idem, reversor neutro con potencia 3 | 1,50 L/h (el motor no gasta) |
| idem, saltar 8 h (dormir) | 12,0 L (solo calefacción) |
| idem, tanque vacío con potencia 4 | 0 km/h; calefacción sin combustible; interior 13,7 → -16,4 °C en ~3 h |
| idem, recargar 30 L | vuelve la tracción y la calefacción |
| idem, medidor | 100 L: aguja al medio, luz apagada; 20 L (10%): luz roja; lleno: aguja al tope |
| `m6_equipo_y_bidon.cs`: barreta / traje | se toma (desaparece del perchero) / calor perdido en 1 h a -25 °C: 9,3 sin traje, 4,6 con traje |
| idem, sifón del vehículo | 15 L en 37,6 s (0,4 L/s); bidón pesa 14,3 kg, camina al 72% |
| idem, cargar el tanque | 15 L en 7,6 s (2 L/s) |
| idem, depósito con bomba | 20 L en 8,0 s (el sifón tardaría 50 s); sin bomba: "hace falta una bomba" |
| idem, caja en la mano frente al vehículo | E suelta la caja (el vehículo no se usa) |
| `m6_interactor_e.cs` (tecla E virtual real) | mantener E 6 s sifona 2,4 L; soltar E corta; E tocada con el bidón en la mano mirando a otro lado lo suelta |
| `m6_expedicion.cs`: primera expedición completa (jugador automático con teclado y motor reales, solo guiado por la sonda portátil) | ver abajo |

### La primera expedición (sección 24, fase 4)
Con la barreta, el traje y un bidón vacío, siguiendo solo la aguja ámbar: salir del tren y llegar a la estación (163 m caminados en 48 s) → romper el hielo (4,0 s) → abrir la puerta → entrar con el bidón en la mano (aire -10 °C, interferencia de la sonda 0,26) → tomar la bomba manual y la ración → bombear 20 L en 8 s → volver siguiendo la aguja blanca (con el bidón lleno, a 63% de velocidad) → cargar el tanque por la boca de carga (10 s): **42,8 → 62,5 L**. En total ~4,9 h de juego (~5 minutos reales); la calefacción gastó 7,4 L mientras tanto, así que la expedición dejó **+12 L netos**; vida 100, calor 80 (con traje), energía 50, comida 89.

Regresión con todo lo nuevo en la escena: `m2_cambio_y_parada.cs` (parada a 0,15 m del cartel, temblor 0,03 mm), `m3_puertas.cs`, `m3_llevar.cs` (temblor 0,1 mm), `m4_temperatura.cs`, `m4_comer_dormir_morir.cs`, `m4_feedback_y_caida.cs`, `m5_sonda_portatil.cs`, `parado_en_caja.cs` (0,00 cm) y `recentrado.cs` (6 re-centrados, 0,52 ms, temblor del jugador 0,03 mm) iguales que antes. Única diferencia: el temblor de las cajas apoyadas en `recentrado.cs` dio 3,6 mm (otras corridas: 1,1 mm; en M3 se anotó 3,8 mm); sigue siendo imperceptible y varía entre corridas.

## Pendientes y riesgos

- **Cruzar la puerta de la estación con el bidón en la mano** funcionó en la prueba automática (pasa por el vano de 1,1 m), pero un jugador que entra en diagonal puede trabar el bidón contra la pared y que se suelte. Probar a mano.
- Las herramientas (barreta, bomba) se guardan solas al tomarlas (no hay inventario con peso: M7). Tablones, tela y chapa se llevan de a uno y por ahora no sirven para nada (M8).
- Fuentes de combustible y la puerta ya abierta no se guardan (M12); tienen ID estable para eso.
- Los valores son generosos. Con 12 L/km (mismo gasto por km a cualquier potencia) y 1,5 L/h de calefacción, el tanque inicial alcanza para ~3 km. El ajuste está en `FuelSettings`.
- La luz de la estación y la del medidor son provisorias; sin sonidos (M13).
- La prueba de la expedición usa W y la física real del jugador. Una versión previa movía al jugador "a mano" y le provocó caídas falsas (daño por caída): era un defecto de la prueba, no del juego.
