# M7 — Inventario con peso + desmontaje: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"). Pruebas automáticas en `Tools/SpikeTests/m7_*.cs`. Valores en `Assets/_Project/Data/Items/` (los cuatro materiales, `BackpackSettings` y una receta de desmontaje por tipo de objeto: se ajustan sin tocar código). Sin paquetes nuevos.

## Qué hay

- **Cuatro materiales** (sección 9.1 y la V1): madera 2 kg, metal 3,5 kg, tela 0,8 kg y componentes 0,3 kg por unidad (`ItemDefinition`).
- **Mochila** (`Backpack`, sección 22.2 "Inventory / Carry"): límite de **30 kg** (PROPUESTA: solo peso, sin volumen ni casilleros; el documento deja abierto "slots/peso"). Nunca inventa ni pierde unidades: lo que no entra, se queda donde estaba.
- **El peso frena** (sección 9.4): lo que va en la mochila se suma a lo que se lleva en la mano, con la misma fórmula (2% por kg, hasta el 45%). Los objetos enteros (cajas, bidones) siguen yendo en la mano.
- **Panel con Tab** (sección 18.1, "UI de sistema, solo al abrir"): lista, kg, barra de peso (blanca; ámbar desde el 80%, roja desde el 95%) y soltar. Se maneja con el mouse (botones "soltar 1" / "todo") o con el teclado (↑↓ elegir, Enter soltar 1, Supr soltar todo, Tab cerrar). Abierto: no se camina, no se usa E y el cursor queda libre; el juego **no** se pausa (afuera hace frío).
- **Bultos** (`MaterialBundle`): lo que se suelta, lo que sobra al desmontar y los materiales sueltos de la estación son bultos con física del color del material y tamaño según la cantidad. E los guarda en la mochila: todo, **parte** (lo que da el peso) o nada ("no entra, mochila llena"). Acompañan al tren en marcha.
- **Desmontaje** (`SalvageProp` + `SalvageRecipe`, sección 9.2): con la barreta, mantener E desarma el objeto (se achica) y al terminar se rompe en pedazos y deja los materiales. La indicación dice qué sale: "mantener para desarmar pallet → madera ×4, 8,0 kg" y avisa si no entra todo. El avance se conserva si se suelta E. Lo que no entra por el peso queda en el piso, en bultos. Cada objeto tiene ID estable (para el guardado) y una vez desmontado ya no existe.
  - **pallet** → madera ×4 (3 s) · **armario metálico** → metal ×3 (6 s) · **colchón viejo** → tela ×3 (4 s) · **tablero eléctrico** → componentes ×2 (5 s).
- **Dónde está todo:** en la estación hay un pallet, un armario, un colchón y un tablero, más bultos sueltos (madera ×2, metal ×2, tela ×2) en lugar de los tablones, la tela y la chapa de antes. Un pallet más está tirado cerca del vehículo abandonado, para probar sin ir a la estación. La segunda sala sigue vacía.

## Mediciones (Editor, 60 FPS)

| Prueba | Resultado |
|---|---|
| `m7_mochila.cs`: guardar madera 4, metal 3, tela 3, componentes 2 | pesa 21,50 kg (exacto); caminar ×0,57 |
| idem, límite de 30 kg | 29,5 kg: otra madera no entra; pedir 100 entra solo lo que cabe |
| idem, sacar más de lo que hay / cantidades 0 o negativas | solo sale lo que hay; nada raro |
| idem, 5 vueltas de sacar todo y volver a meter | mismas cantidades y mismo peso |
| idem, mochila 10 kg + caja 10 kg en la mano | ×0,60 (se suman); al soltar la caja ×0,80 |
| idem, mochila llena | ×0,45 (el mínimo); no se bloquea el movimiento |
| `m7_panel_y_bultos.cs` (Tab, ↓, Enter, Supr con teclado virtual real) | abre y cierra; con el panel abierto no se camina, el cursor queda libre y el interactor apagado; al cerrar todo vuelve |
| idem, bulto de 1 metal | pesa 3,5 kg; queda apoyado en el piso del tren |
| idem, recoger | todo (8 kg de madera), **parte** ("madera ×2 de 5": entran 2 y el bulto queda con 3 de 6 kg) y nada (mochila llena) |
| idem, conservación | madera, metal y tela: lo que hay = lo que se dio, exacto |
| idem, soltar contra una pared / con el tren a 21 km/h | el bulto queda adentro del tren / va a 0,00 m/s respecto del tren |
| `m7_desmontaje.cs`: sin barreta | "pallet: hace falta una barreta", no avanza |
| idem, los cuatro tipos | cada uno tarda lo de su receta (3 / 4 / 5 / 6 s) y deja lo esperado (8 / 2,4 / 0,6 / 10,5 kg); ya no existen |
| idem, armario con 9 kg libres | entran 2 de metal (7 kg) y 1 queda en el piso |
| idem, pallet de afuera con la tecla E real mantenida | 3,0 s, madera ×4 en la mochila |
| idem, cuentas | lo que había + lo desmontado + lo puesto a mano = lo que hay (mochila + bultos): exacto |
| `m7_expedicion.cs`: expedición con carga (jugador automático con teclado y motor reales) | llega a la estación con la sonda, rompe el hielo, desmonta pallet + armario + colchón (20,9 kg: madera ×4, metal ×3, tela ×3); caminar ×0,58; vuelta cargada 81 s (164 m para 175 m en línea recta); **sube al tren por la escalera cargado** (1,8 s); deja todo con Tab + Supr: 3 bultos, los 3 adentro del tren; cuentas exactas; ~3 h de juego (~3 minutos reales) |

La expedición con carga cumple la prueba de la fase 5 de la sección 24: recuperar recursos lógicos (cada material sale de un objeto que se entiende) sin duplicaciones.

Regresión con todo lo nuevo en la escena: `m2_cambio_y_parada.cs` (parada a 0,15 m del cartel), `m3_puertas.cs`, `m3_llevar.cs` (peso de la mano ×0,80 con 10 kg, igual que antes con la mochila vacía), `m4_temperatura.cs`, `m4_comer_dormir_morir.cs`, `m4_feedback_y_caida.cs`, `m5_sonda_portatil.cs`, las cuatro de M6 (`m6_combustible.cs`, `m6_equipo_y_bidon.cs`, `m6_interactor_e.cs` y la expedición de combustible, que da +20 L en el tanque), `parado_en_caja.cs` (0,00 cm) y `recentrado.cs` (6 re-centrados, 0,48 ms, temblor de cajas 1,14 mm) iguales que antes.

## Dos arreglos que salieron de las pruebas

- **El bidón se lleva a la cadera** (a la derecha y abajo, junto al cuerpo; `CarrySettings.oneHandedHoldOffset`), no al frente. En la expedición del M6 a veces se trababa en el marco de la puerta de la estación porque, colgado al frente, era más ancho que el jugador. Ahora no tapa la mira ni ensancha al jugador. Si se gira pegado a una pared, el bidón puede quedar del otro lado y soltarse (es la regla de siempre: trabado más de 0,4 s, se suelta solo).
- **Los pedazos de hielo y de lo desmontado no frenan a quien los rompió** (ignoran su colisión): el jugador se trababa pisando escombros en la puerta de la estación. Siguen cayendo y chocando con todo lo demás, y desaparecen a los 8 s.

## Pendientes y riesgos

- **El clic del mouse en los botones del panel no se pudo probar automáticamente** (el panel usa la interfaz inmediata de Unity, que toma los clics del sistema operativo y no del teclado virtual). El teclado sí está probado de punta a punta. Probar a mano los botones "soltar 1" y "todo".
- **No hay dónde guardar cosas en el tren todavía** (almacenamiento y estante: M8): lo que no querés llevar lo soltás en el piso, donde queda como bulto con física. Muchos bultos en el piso del tren podrían cargar la física: no se midió con decenas; el M8 los va a guardar.
- **Los materiales todavía no sirven para nada** (cama, mejoras: M8; recetas: M9). La barreta desarma todo; sierra, llave y kit eléctrico llegan con el taller (M9).
- **Con una caja o un bidón en la mano no se guardan bultos** (E suelta lo que se lleva, como siempre): primero soltá lo que llevás.
- El objeto desmontado, los bultos sueltos y lo que hay en la mochila no se guardan (M12); los objetos tienen ID estable para eso.
- Arte provisional (cajas). Sin sonidos de desarmado (M13).
- La prueba automática con el jugador caminando a veces no logra entrar por la puerta de la estación en una corrida (un detalle del jugador automático al alinearse con el vano, no del juego: pasa en la mayoría y al repetir entra).
