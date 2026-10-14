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

REGRESION_PLACEHOLDER

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
- **No hay menú todavía** (F5/F9). El menú de pausa con Guardar/Cargar llega en el M13 (UI mínima).
- **Una sola partida** (un archivo). Varias ranuras, guardado automático al dormir o en la nube quedan fuera (el Anexo G deja la nube fuera).
- **El castigo por morir sigue igual** (DECISIÓN de Tomi: despertar en la cama del tren). No se borra la partida al morir; el "permadeath" sigue abierto (sección 27).
