# M9 — Taller: planos y fabricar: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"; la escena Spike también se reconstruye, pero no tiene panel de banco ni libro de planos: el banco ahí es solo un mueble). Pruebas automáticas en `Tools/SpikeTests/m9_*.cs`. Valores (planos, materiales, tiempos) en `Assets/_Project/Data/Crafting/` (un archivo por plano) y en `Assets/_Project/Data/Items/`.

## Qué hay

### Planos y conocimiento (secciones 10.3 y 22.2)
- **Un plano es un archivo de datos** (`Blueprint`): su nombre, para qué sirve, si se sabe desde el arranque, qué materiales gasta, qué herramientas pide, cuánto tarda y qué sale. Para cambiar un costo o un tiempo se cambia el archivo, no el código.
- **Libro de planos del jugador** (`BlueprintBook`): los planos iniciales se saben desde el arranque; los que se encuentran se suman una sola vez (el libro solo guarda el ID de cada uno: listo para el guardado del M12). Al aprender uno aparece arriba un aviso ámbar "PLANO NUEVO · nombre" durante 5 s.
- **Hoja de plano en el mundo** (`BlueprintPickup`): una hoja sobre el escritorio de la **segunda sala de la estación**. La indicación dice "leer el plano: estante reforzado"; E lo lee, el jugador lo aprende y la hoja desaparece (no se puede leer dos veces).
- **El estante reforzado ahora pide su plano:** hasta leer la hoja, la pared izquierda del taller está **vacía** (no se ve ningún marco ni se puede apuntar). Al leerlo aparece el marco fantasma de siempre y se puede construir como en el M8.

### Banco de trabajo (secciones 4, 10.1 y 20.6)
- El banco del taller (pared izquierda) ahora se usa: apuntarlo dice **"usar el banco de trabajo"** y E abre su panel. Tab lo cierra.
- **Panel** (a la izquierda la lista de planos que conocés; a la derecha lo que pide el elegido):
  - **Materiales:** una pieza dibujada por cada unidad que pide; **sólida** si la tenés en la mochila y **translúcida** si falta (mismo lenguaje que los marcos fantasma de las obras). Debajo, "chapa 1/2".
  - **Herramientas (no se gastan):** hay que tenerlas, pero no se consumen. Se ven sólidas si las tenés.
  - **Sale:** qué se fabrica y cuánto tarda.
  - **FABRICAR (Enter)** con el mouse o con Enter: aparece una **barra de progreso** (en segundos reales). Los materiales se gastan **recién al terminar**; si cerrás con Tab a la mitad, se cancela sin perder nada.
  - Sin lo necesario, el botón se apaga y dice qué falta ("faltan chapa ×1", "falta la sierra manual", "ya la tenés", "sacá lo que hay sobre la mesa").
  - Mouse: clic en un plano y clic en FABRICAR. Teclado: ↑ ↓ para elegir, Enter para fabricar, Tab para cerrar.
- Mientras está abierto no se camina ni se usa E, el cursor queda libre y el juego sigue corriendo (igual que el inventario). **Usa los materiales de la mochila**; lo que está en un estante hay que sacarlo antes.

### Qué se fabrica (valores en los archivos de plano; son PROPUESTA y se ajustan ahí)
| Plano | Cómo se consigue | Cuesta | Pide tener | Tarda | Sale |
|---|---|---|---|---|---|
| **Sierra manual** | inicial | tablón ×1 + chapa ×1 | nada | 6 s | queda **al cinto** (no ocupa lugar) y aparece **colgada en la pared** del taller |
| **Bidón** | inicial | chapa ×2 | **la sierra** (no se gasta) | 8 s | un **bidón vacío** sobre la mesa del banco (el mismo objeto que los bidones del tren: se toma con E, se llena con el sifón, se carga por la boca de combustible) |

- **DECISIÓN de Tomi:** la sierra **no acelera el desmontaje**; es una herramienta de crafteo que otras recetas piden. El desmontaje a golpes (que los objetos se "farmeen" pegándoles con la herramienta, no con E) es un milestone propio más adelante.
- Si ya hay algo sobre el lugar donde sale el bidón, no se puede fabricar otro (aviso "sacá lo que hay sobre la mesa") para que no se amontonen ni se traben.
- **Un pallet más** afuera (a 26 m del tren, a la derecha), para que alcance la madera de la sierra y del estante.

### Ficha de medidas para el modelado 3D (pedido de Tomi durante este milestone)
`Docs/Tren_Especificaciones_para_Arte.md`: todas las medidas del tren (sacadas del código, no de memoria), ejes y origen del modelo, puertas, ventanas, zonas, muebles, piezas que se mueven y cómo se entrega y se conecta con el juego. Pensada para pasársela a quien modele (otro Claude u otra persona).

## Mediciones (Editor, 60 FPS)

| Prueba | Resultado |
|---|---|
| `m9_planos.cs` | al empezar se saben la sierra y el bidón, **no** el estante reforzado; sin el plano la obra del estante está **bloqueada** (0 de 8 piezas visibles, zona de uso apagada, sin objetivo ni indicación aunque haya chapas en la mochila); la hoja de la estación se apunta ("leer el plano: estante reforzado"), E la lee, se aprende, la hoja desaparece y aprenderlo otra vez devuelve falso; con el plano la obra aparece (2 de 8 piezas fantasma visibles, "mantener para construir soportes (chapa 0/2)"); hay 2 pallets afuera |
| `m9_banco.cs` (teclado y mouse virtuales reales) | apuntando al banco: objetivo `Workbench`, "usar el banco de trabajo"; E abre el panel con 2 planos (elegido: sierra manual), bloquea el caminar y la E, cursor libre; sin materiales: "faltan tablón ×1, chapa ×1"; con la chapa sola: "faltan tablón ×1"; **sierra:** arranca, a ~1 s la barra va en 0,19 y los materiales siguen en la mochila, termina a los **5,9 s** (plano: 6 s), gasta tablón y chapa, la sierra queda al cinto y aparece colgada; una segunda sierra: "ya la tenés"; **bidón:** ↓ lo elige, "faltan chapa ×2"; **Tab a los ~2 s cancela**: nada se gasta ni se crea, el panel cierra, el caminar y la E vuelven, **Tab no abre la mochila** y después sí la abre y la cierra normalmente; al reabrir elige el primero; bidón completo en **7,9 s** (plano: 8 s), gasta 2 chapas, sale **vacío**, se lleva a una mano, queda **quieto sobre la mesa** (x −1,20, y 1,90, z 0,10 del tren); con la mesa ocupada: "sacá lo que hay sobre la mesa", y sacado el bidón se puede de nuevo; mouse: clic en cada plano lo elige, clic en FABRICAR sin materiales da el aviso; Tab cierra y deja todo como estaba |
| `m9_taller_ciclo.cs` (jugador automático, ciclo entero) | de cero: sin sierra, sin plano, obra bloqueada → sale con la barreta y junta (3 tablones, 3 chapas en 2 lugares; primero lo escaso) → lee la hoja de la estación → vuelve y en el banco con E fabrica la sierra y el bidón (gasta **1 tablón y 3 chapas**, justo) → toma el bidón con E ("tomar (bidón 0 / 20 L)") → lo llena con el sifón del vehículo (15,0 → 13,0 L; el bidón tiene **2,0 L**) → lo carga por la boca del tren (**50,0 → 52,0 L**, el bidón queda en 0,0 L). Cuentas exactas |
| **Regresión completa (24 pruebas, todas corridas con el M9 en la escena)** | Dan lo mismo que en el M8: `m2_cambio_y_parada` (parada a **0,15 m** del cartel, tope, marcha atrás), `m3_puertas`, `m3_llevar` (10 kg a 25 km/h: temblor 0,1 mm), `m4_temperatura`, `m4_comer_dormir_morir` (despierta a 0,00 m de la cama), `m4_feedback_y_caida`, `m5_sonda_portatil` (96 % en línea recta), `m6_combustible`, `m6_equipo_y_bidon`, `m6_interactor_e`, `m6_expedicion` (idéntica a la del M8: 4,88 h, bombea 20 L, carga 19,8 L), `m7_mochila`, `m7_panel_y_bultos`, `m7_desmontaje`, `m7_expedicion` (cuentas exactas), `m8_obras_y_cama` (+48 → +88), `m8_almacenamiento`, `m8_ciclo_completo` (cama y estante terminados, +88; **4 viajes** en vez de 3 porque ahora hay más madera en el mundo por el pallet nuevo), `m8_mouse_inventario`, `parado_en_caja` (corrimiento 0,00 cm el jugador y 0,02 cm la caja, inclinación 0,0°) y `recentrado` (6 re-centrados, costo máx. 0,53 ms, temblor del jugador 0,03 mm, deriva 0,02 cm en 77 s). Las del M8 que usan el estante reforzado ahora **leen el plano antes** (`Learn`), porque la obra está bloqueada hasta tenerlo |
| `recentrado.cs`: "temblorCajas" | Imprime **32–35 mm** (3 corridas seguidas, también en la regresión del M8; en el M7 daba 1,14 mm). **No lo causó el M9.** La explicación que dejé en el M8 ("tirones del editor") **era incorrecta**: es repetible. Lo medí aparte caja por caja, mirando lo que se dibuja al final del cuadro, durante 77 s a 60 km/h incluyendo el arranque: **máximo 1,2 mm** (ninguna caja pasa de eso, igual que en el M1). Entonces las cajas no tiemblan; lo que da 32–35 mm es el número del componente de medición (`SpikePropMetrics`, que lee en otro momento del cuadro). Causa todavía sin encontrar: queda anotado como pendiente de herramienta de medición, no de juego |

## Cómo probarlo vos en Unity (paso a paso)

1. Abrí la escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion` y dale Play.
2. **Plano:** al empezar el taller (pared izquierda, atrás del banco) no tiene nada en la pared del estante: eso es lo esperado. Para tener el plano hay que ir a la estación (como en el M6) y leer la hoja sobre el escritorio de la **segunda sala**: apuntala, **E**, aparece arriba "PLANO NUEVO · estante reforzado". Ahora la pared del taller muestra el marco fantasma.
3. **Materiales:** con la barreta (rincón de equipo, junto a la puerta de atrás) y **mantener E** desarmá objetos: el pallet da madera (tablones), el armario y los bultos sueltos dan chapas. Para fabricar todo hacen falta **1 tablón y 3 chapas**.
4. **Banco:** volvé al tren, apuntá al banco (pared izquierda del taller, la mesa larga) y apretá **E**. Probá:
   - Mirá cómo las piezas que tenés se ven **sólidas** y las que faltan **translúcidas**.
   - Sin materiales apretá **Enter**: tiene que decir qué falta.
   - Con tablón y chapa: **Enter** en "sierra manual": barra de progreso de 6 s. Cuando termine, la sierra aparece **colgada en la pared izquierda** y el equipo del cartel de arriba a la izquierda dice "sierra sí".
   - **↓** para elegir "bidón" (o clic con el mouse), **Enter**: 8 s y aparece un **bidón rojo vacío** sobre la mesa, en el medio.
   - Probá **Tab a la mitad**: cancela sin gastar nada, y Tab no te abre la mochila.
   - Probá fabricar otro bidón con el primero todavía sobre la mesa: "sacá lo que hay sobre la mesa".
5. **Usar el bidón fabricado:** agarralo (**E**), llevalo al vehículo abandonado, sifoná y cargalo al tanque como en el M6.

## Pendientes y riesgos

- **Los materiales de los estantes no se usan directo en el banco** (como en las obras): hay que sacarlos a la mochila. Se puede cambiar si molesta.
- El **tiempo de fabricación pasa en tiempo real** (6 y 8 s) y la vida sigue corriendo; no es pausa.
- La sierra "colgada" es solo un adorno: aparece cuando la fabricás y no se puede tocar. La barreta sigue sin mostrarse colgada.
- **Solo dos planos fabricables** (los de la V1 de este milestone). La evolución del banco, el rifle, las bombas, los filtros y los actuadores llegan con los planos que se encuentren más adelante; la pieza especial de vía y el mando de cambios son del M10.
- Panel con un máximo práctico de **5 planos en la lista** (cabe sin desplazar); si hay más, hará falta una lista con desplazamiento.
- Los planos aprendidos, la sierra y lo fabricado no se guardan hasta el M12 (los planos y las obras tienen ID estable para eso). Sin sonidos de fabricación (M13). Iconos y modelos provisorios (sierra y bidón son cajas).
- **El panel de depuración de arriba a la izquierda se ve a través del panel del banco** (F1 lo oculta): es el panel de pruebas, no el juego.
- Las pruebas nuevas hay que correrlas con la ventana de Unity en primer plano (usan teclado y mouse virtuales).
- **Medición de cajas del `recentrado.cs` da 32–35 mm desde el M8** aunque las cajas no tiemblan (1,2 mm medido cuadro a cuadro): hay que revisar cómo mide `SpikePropMetrics` antes de volver a fiarse de ese número. No afecta al juego.
