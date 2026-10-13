# M8 — Construcción por fases + inventario por cuadrícula: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"). Pruebas automáticas en `Tools/SpikeTests/m7_*.cs` y `m8_*.cs`. Valores en `Assets/_Project/Data/Items/` (materiales con su tamaño en casillas, peso e icono; `BackpackSettings`; recetas de desmontaje) y `Assets/_Project/Data/Build/` (las obras: fases, materiales y valores). Sin paquetes nuevos.

## Qué hay

### Inventario por cuadrícula (DECISIÓN de Tomi, pedida en este milestone)
- **Cada pieza ocupa un rectángulo de casillas** (`ItemDefinition`): tablón 1×4, chapa 2×2, rollo de tela 1×2, componente 1×1. Cada una con su **imagen** provisoria (dibujada por código, en `Data/Items/Iconos`; el arte final se pone en el mismo archivo del material), su nombre de pieza y su peso.
- **La mochila es una cuadrícula de 6×5** (30 casillas). El límite duro es el **espacio**; el **peso** sigue frenando el caminar, sumado a lo que se lleva en la mano (×0,65 con 17 kg, mínimo ×0,45).
- Las piezas **se giran** con R (si no son cuadradas), entran solas en el primer hueco (primero sin girar, después giradas) y **no entran si no hay lugar**: lo largo no entra en lo chico.
- **Panel con Tab:** la mochila como cuadrícula con las piezas dibujadas sobre sus casillas. Al abrir un mueble aparece su cuadrícula al lado (un estante de 4×3 se ve más chico que la mochila).
  - **Mouse:** arrastrar una pieza (dentro de la mochila, del estante a la mochila y al revés), R para girar mientras se arrastra, sombra **verde** si entra y **roja** si no, y soltar afuera del panel la deja en el piso. El pie del panel dice qué pieza se mira, cuántas casillas ocupa y su peso.
  - **Teclado:** flechas mueven el cursor entre casillas (pasa de una cuadrícula a la otra), R gira, Enter pasa la pieza al otro lado (o la suelta en el piso si solo está la mochila), Supr pasa o suelta todas las del mismo material.
  - Mientras está abierto no se camina ni se usa E y el cursor queda libre; el juego no se pausa.

### Obras por fases (`BuildSite`, sección 11)
- Una obra es un archivo de datos (`BuildProject`): sus fases, los materiales de cada una y el valor del efecto en cada nivel. Con los materiales en la mochila, **mantener E** los aporta de a uno (2 por segundo): salen de la mochila y la obra los guarda. Lo aportado queda **representado físicamente**: piezas sólidas, y las que faltan de la fase actual como **fantasmas translúcidos**; las fases siguientes no se ven hasta llegar a ellas. El avance se conserva entre expediciones.
- Al completar una fase se corta el aporte (se nota el hito) y se vuelve a mantener E para la siguiente. Cada avance vale algo aunque la fase no esté completa (sección 11.2).
- Sin nada que aportar, E hace lo de siempre del mueble (dormir, abrir el estante) y la indicación dice qué pide la fase ("dormir · base de madera pide tablón ×4").

### Cama mejorable (sección 20.8)
- El "lugar miserable" del principio (una tela sucia en el piso) y la obra **cama improvisada** en tres fases: **base de madera** (tablón ×4), **relleno** (rollo de tela ×3) y **manta** (rollo de tela ×2, con almohada).
- La recuperación de energía por hora de sueño sube de **6 a 7,5, a 9 y a 11**, y sube de a poco con lo aportado dentro de cada fase.

### Almacenamiento y estante reforzado
- **Estante precario del taller** (el que ya estaba): cuadrícula de **4×3** (12 casillas). Los tablones largos solo entran acostados, uno por fila.
- **Estante reforzado** (pared izquierda del taller): obra de **cuatro fases** (sección 11.1, "Ejemplo: estante"): soportes (chapa ×2), estructura (tablón ×2), primer estante (tablón ×2) y segundo estante (tablón ×2). No guarda hasta el primer estante (cuadrícula **4×2**) y llega a **6×4** (24 casillas) con el segundo; lo guardado se conserva en su lugar al crecer.
- **Lo guardado se ve en el mueble:** un cubo del color de cada material sobre las tablas, más grande según la cantidad (sección 11.3).
- Los muebles y las obras tienen ID estable para el guardado futuro.

## Mediciones (Editor, 60 FPS)

| Prueba | Resultado |
|---|---|
| `m7_mochila.cs`: cuadrícula | 4 tablones 1×4 + 2 de 3 chapas 2×2 (la tercera no entra: no queda hueco de 2×2) + 3 rollos 1×2 que entran **girados** en la fila de abajo: 30 de 30 casillas, 17,4 kg, caminar ×0,65 |
| idem, lógica de la cuadrícula (4×3) | tablón 1×4 parado no entra, acostado sí; en una de 3×3 no entra de ninguna forma; no se pisan piezas; mover a un lugar libre sí, a uno ocupado no; la copia es independiente; agrandar sí, achicar no |
| idem, 5 vueltas de sacar y volver a meter | mismas cantidades y mismo peso |
| idem, mochila 11 kg + caja 10 kg en la mano | ×0,58 (se suman) |
| `m7_panel_y_bultos.cs` (Tab, flechas, R, Enter, Supr con teclado virtual real) | abre y cierra (bloquea el caminar, cursor libre, interactor apagado; al cerrar vuelve todo); el cursor pasa de la mochila al estante; R no gira una chapa (cuadrada) ni un tablón tapado; Enter suelta una pieza; Supr, todas las del material; Enter con un estante abierto pasa la pieza |
| idem, mover con la misma función que usa el mouse | a un lugar libre sí, sobre otra pieza no, girar en el lugar solo si entra |
| idem, recoger bultos | todo / parte ("guardar madera ×1 de 5") / nada ("no entra, falta lugar"); conservación exacta; el bulto acompaña al tren a 21 km/h (0,00 m/s relativos) |
| `m7_desmontaje.cs`, `m7_expedicion.cs` | igual que en M7, ahora con cuadrícula: la expedición carga 30 casillas (4 tablones + 2 chapas + 3 rollos), la chapa que sobra queda en el piso; sube al tren cargado y deja todo con el panel; cuentas exactas |
| `m8_obras_y_cama.cs` | 6 niveles de sueño de 8 h: **+48** (lugar miserable), **+54** (a mitad de la fase 1), **+60** (fase 1), **+64** (a mitad de la 2), **+72** (fase 2), **+88** (cama terminada); piezas sólidas/fantasma/ocultas correctas en cada paso; aportar con E real mantenido; el avance se conserva; cuentas exactas |
| `m8_almacenamiento.cs` | estante precario: al apuntarlo "abrir estante precario (0/12 casillas)"; Supr sobre un tablón pasa 3 de 4 (4×3: acostados, uno por fila); la chapa no entra con el estante lleno; sacar del estante a la mochila; se ve 1 cubo por material. Estante reforzado: sin cuadrícula hasta el primer estante (4×2, llena con 2 chapas); abre por la obra cuando no hay nada que aportar; el segundo estante lo agranda a 6×4 y las chapas siguen en el mismo lugar |
| `m8_ciclo_completo.cs`: ir a la estación y al pallet, desmontar todo, volver, construir las dos obras, guardar el resto, dormir | **3 viajes** (la mochila es chica); cama y estante terminados (6×4), nada queda por desmontar ni afuera; guarda 3 chapas y 2 componentes; dormir en la cama terminada **+88**; cuentas exactas (tablones 10 = 10 aportados, rollos 5 = 5, chapas 5 = 2 aportadas + 3 guardadas) |

La prueba de la fase 6 ("mejorar la cama y comprobar mejora de energía") da +48 → +88.

Regresión con todo lo nuevo en la escena: `m2_cambio_y_parada.cs` (parada a 0,15 m del cartel), `m3_puertas.cs`, `m3_llevar.cs`, `m4_temperatura.cs`, `m4_comer_dormir_morir.cs` (la cama nueva despierta en el mismo punto: 0,00 m), `m4_feedback_y_caida.cs`, `m5_sonda_portatil.cs`, las cuatro de M6, las de M7 (adaptadas a la cuadrícula), `parado_en_caja.cs` (0,00 cm) y `recentrado.cs` (6 re-centrados, temblor del jugador 0,03 mm, temblor de cajas 1,19 mm en una corrida limpia) iguales que antes.

Una nota sobre las pruebas: cuando Unity se traba un instante (por ejemplo, al compilar el script de otra prueba que corre al mismo tiempo) el "temblor de cajas" de `recentrado.cs` puede dar valores enormes (se vieron 32 a 376 mm en corridas con esos tirones); no es del juego: con el editor tranquilo da 1,1 a 1,2 mm, y la medición por caja (sin tirones) da 0,4 a 1,2 mm.

## Pendientes y riesgos

- **El arrastre con el mouse no se pudo probar automáticamente** (el panel usa la interfaz inmediata de Unity, que toma el mouse del sistema operativo, no del teclado virtual). Se probó la misma función que usa al soltar (`TryMovePiece`) y todo el teclado. Probar a mano: arrastrar, girar con R mientras se arrastra, la sombra verde/roja y soltar afuera.
- **Los materiales de los estantes no se aportan directo a las obras**: hay que sacarlos a la mochila primero (una acción). Se puede cambiar más adelante (el taller del M9 puede decidirlo).
- **El mundo de prueba trae justo lo necesario**: tablones 10 (cama 4 + estante 6), chapas 5 (estante 2), rollos 5 (cama 5): no hay margen en tela ni madera.
- Los objetos enteros (cajas, bidones) siguen yendo en la mano, no en la cuadrícula. La barreta y la bomba van "al cinto" sin ocupar lugar.
- Iconos provisorios dibujados por código; sin sonidos de construir ni de mover piezas (M13). El contenido de los estantes, las obras y la mochila no se guardan (M12): tienen ID estable para eso.
- Una cosa que salió de las pruebas: apenas aparecen materiales en la mochila, la primera pulsación de E sobre una obra puede perderse si es en el mismo cuadro (la indicación se actualiza al cuadro siguiente); a mano no se nota.
