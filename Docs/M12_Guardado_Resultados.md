# M12 — Guardado: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity`. Código en `Assets/_Project/Scripts/Save/`. Prueba en `Tools/SpikeTests/m12_guardado.cs`. Este milestone lo planifiqué yo sin pedir aprobación (Tomi pidió seguir sin preguntar).

## Qué hay

- **F5 guarda y F9 carga.** Arriba aparece un aviso breve: "PARTIDA GUARDADA", "PARTIDA CARGADA" o "no hay partida guardada".
- **Dónde se guarda:** en un archivo de texto (JSON) en la carpeta de datos del juego, `.../AppData/LocalLow/DefaultCompany/icetrain/saves/partida.json`. Antes de pisar la partida anterior se hace una copia (`partida.json.bak`); si algo falla a mitad de guardar, la anterior queda. Sin PlayerPrefs, como pide el Anexo B.4.
- **Versionado:** el archivo lleva un número de versión. Si en el futuro cambia el formato, una partida vieja se puede convertir o rechazar con un aviso en vez de romperse.
- **Cómo funciona cargar:** se vuelve a abrir la región tal como arranca y se le aplica encima solo lo que cambió (lo que el Anexo B.4 llama "seed del mundo y deltas de estado"; en la V1 la región está hecha a mano y la escena hace de seed). Así:
  - lo que se desarmó **antes** de guardar no reaparece;
  - lo que se desarmó **después** de guardar vuelve a estar;
  - nada se duplica, aunque se cargue muchas veces.

### Qué se guarda (Anexo B.4)

| Grupo | Qué |
|---|---|
| Jugador | dónde estaba (si estaba adentro del tren, relativo al tren; si no, en coordenadas del mundo, descontando los re-centrados del origen flotante), hacia dónde miraba, vida, comida, energía, calor, barreta, bomba, sierra, traje, la mochila pieza por pieza (lugar y giro) y los planos aprendidos |
| Tren | lugar en la vía, velocidad, reversor, potencia, freno, combustible y temperatura de adentro |
| Construcción | cada obra (cama, estante reforzado, bifurcación): fase y lo aportado a la fase actual; lo guardado en cada mueble, pieza por pieza |
| Mundo | objetos desarmados y a medio desarmar, todos los bultos (los del lugar y los que se soltaron), objetos que se llevan en la mano (cajas, bidones con su combustible, también los **fabricados**), hielos rotos y a medio romper, puertas abiertas o cerradas, raciones comidas, combustible que queda en el vehículo y en el depósito |
| Red ferroviaria | cada cambio de vía: estado (congelado / roto / reparado) y si está en desvío |
| Sonda | la señal fijada como objetivo |

- **No se guarda a propósito:** una fabricación en curso del banco (se cancela) y lo que el jugador tenía en la mano (se suelta y se guarda donde cae).
- **No se puede guardar mientras se duerme.**

## Mediciones (Editor)

| Prueba (`m12_guardado.cs`) | Resultado |
|---|---|
| A) Se arma un estado y se guarda | barreta tomada del perchero, traje puesto, plano leído, pallet de la estación desarmado y armario a medias, hielo de la estación roto y su puerta abierta, cama con 2 tablones de 4, sierra y bidón fabricados (el bidón con 7 L), un tablón tirado afuera, hielo de la bifurcación roto y su obra empezada, una ración comida, el tren movido 14 m, el jugador afuera junto a la estación. **60 datos** en la huella del estado |
| B) Después de guardar se cambia todo | se termina de desarmar, se come otra ración, se aporta a la cama, pasan 3 h, se mueve el tren: la huella cambia (**12 diferencias**) |
| C) Cargar | recarga y aplica en **0,8 s**: la huella es **IGUAL** a la guardada (los 60 datos) |
| D) Otro estado con el jugador **adentro del tren** | mochila con una chapa y una tela, el estante precario con un tablón, un componente tirado en el piso del tren: guardar y cargar → **IGUAL** |
| E) Cargar otra vez la misma partida | **IGUAL**: nada se duplica. Cuentas del mundo idénticas en todas las cargas |
| Cargar sin partida | no carga y avisa "no hay partida guardada" |

La "huella" compara, entre otras cosas: el equipo y qué herramientas siguen en el mundo, los planos y las hojas, la mochila y cada mueble pieza por pieza, cada obra con lo aportado, cada cambio de vía, qué objetos quedan por desarmar y cuánto avanzó cada uno, los hielos, las puertas abiertas, la comida, los bultos por material, **las cuentas de cada material en todo el mundo**, los objetos y bidones (con sus litros), el combustible del tanque y de las fuentes, el tren, el jugador, el cuerpo, la hora y la sonda.


### Regresión completa final (35 pruebas, con el M11, el M12 y el M13 en la escena)

Se corrieron todas las pruebas del M1 al M13, una por una, cada una con la escena recién puesta en Play (`Tools/SpikeTests/regresion.sh`, unas 2 h). **Todas dan lo mismo que en su milestone:**

- **Tren y jugador (M1 a M3):** parada a +0,15 m del cartel; temblor del jugador pasando el cambio 0,03 mm; tope a 10 km/h: 23 cm, 2,6 g, rebote 1,8 km/h; puertas y llevar cosas igual (temblor en la mano 0,2 mm). Parado sobre una caja a 60 km/h: corrimiento 0,00 cm. Re-centrado del origen: 6 veces, costo máx. 0,65 ms, temblor del jugador 0,03 mm, deriva 0,02 cm, temblor de las cajas 1,1 mm.
- **Supervivencia (M4):** adentro 13,7 °C; con las dos puertas abiertas baja a −20 °C y al cerrarlas vuelve a 13,7 °C en 2 h; caída de 6 m: vida 100 → 38.
- **Sonda (M5):** la portátil lleva al objetivo caminando un 96 % de la línea recta; la interferencia junto a la masa metálica, igual.
- **Combustible (M6):** gasto exacto (1,5 / 5,5 / 9,5 L/h); la expedición, idéntica: 4,88 h de juego, bombea 20 L y carga 19,7 L.
- **Inventario, obras y taller (M7 a M9):** cuentas exactas, sin duplicar ni perder; ciclo completo de obras en 4 viajes; el taller fabrica la sierra y el bidón.
- **Bifurcación (M10):** de cero a la ruta desconocida en 227 s reales (3,8 h de juego).
- **Mini-región (M11):** el loop en tres viajes, 263 s reales (4,4 h de juego), combustible 48,8 → 17,2 L, 16,7 ms por cuadro en los tres viajes.
- **Guardado (M12):** guardar y cargar da IGUAL en las tres comparaciones; cargar dos veces no duplica nada.
- **Pulido (M13):** sonido, día y noche, menú de pausa y final, igual que en sus pruebas.

Ajustes de las pruebas (no del juego):
- Las dos expediciones (`m6_expedicion` y `m7_expedicion`) buscaban "el hielo" de la escena. Desde el M10 hay dos (la puerta de la estación y la palanca de la bifurcación) y a veces agarraban el de la palanca: la del M7 se cortaba al arrancar. Ahora eligen el de la estación por su ID; corridas solas, dan lo mismo que en el M10.
- En la corrida larga, el jugador automático de `m6_expedicion` soltó una vez el bidón en el camino de vuelta (ya había pasado una vez en el M7). Corrida otra vez, salió completa. Es del caminante automático, que gira de golpe con el bidón a la cadera, no del juego. Si vuelve a pasar, la prueba ahora anota contra qué se trabó.
- Los valores del arranque son un poco más bajos (tanque 48,8 L en vez de 50) porque el lanzador ahora espera a que Unity conteste antes de empezar, y en esa espera corren unos minutos de juego.

## Cómo probarlo vos en Unity (paso a paso)

1. Abrí `Assets/_Project/Scenes/Pruebas/M2_Conduccion` y dale Play.
2. Hacé algo que se note: desarmá el pallet de afuera, aportá a la cama, mové el tren unos metros, comé una ración.
3. Apretá **F5**: arriba dice "PARTIDA GUARDADA".
4. Cambiá cosas: desarmá otra cosa, tirá algo de la mochila, mové el tren.
5. Apretá **F9**: la escena se recarga en menos de un segundo y todo vuelve a como estaba al apretar F5. Lo desarmado antes sigue desarmado, lo de después volvió, la mochila está igual.
6. Apretá **F9** de nuevo: tiene que quedar igual (nada se duplica).
7. Si salís de Play y volvés a entrar, **F9** carga la partida guardada (queda en disco).

## Pendientes y riesgos

- **Los IDs son la base:** cada cosa que se guarda tiene un ID estable o se reconoce por su lugar en la escena. Si se renombran objetos en el constructor de escenas, una partida vieja puede no encontrarlos (se ignoran, sin romper). Por eso el archivo lleva versión.
- **Menú:** desde el M13, el menú de pausa (**Escape**) también tiene Guardar y Cargar; F5/F9 siguen funcionando.
- **Una sola partida** (un archivo). Varias ranuras, guardado automático al dormir o en la nube quedan fuera (el Anexo G deja la nube fuera).
- **El castigo por morir sigue igual** (DECISIÓN de Tomi: despertar en la cama del tren). No se borra la partida al morir; el "permadeath" sigue abierto (sección 27).
