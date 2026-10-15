# Arte — vagón MR-04 de invierno, librería y props: integración

Entrega del chat de arte (Tomi la pasó el 2026-10-14): el vagón `RedBlanca_MR04_v03_invierno` (FBX, GLB y .blend), sus texturas, la **librería MR-04** (63 materiales en 2K, CC0 de Poly Haven y ambientCG más procedurales; ver `Assets/_Project/Art/Libreria/LICENCIAS.txt`), 6 props (lata de conserva, piolet, bidón rojo y verde, bomba manual, bomba de lanza) y dos guías del chat de arte. Este paso lo planifiqué yo sin pedir aprobación (Tomi pidió seguir sin preguntar).

## Qué hay

### Dónde quedó cada cosa
| Qué | Dónde |
|---|---|
| Vagón (FBX) y sus texturas | `Assets/_Project/Art/Tren/MR04/` (`Texturas/`, `Materiales/`) |
| Librería MR-04 (las 27 partes juntas) | `Assets/_Project/Art/Libreria/<categoría>/`, con `catalogo.json` y `LICENCIAS.txt`; materiales en `Libreria/Materiales/` |
| Props (FBX) | `Assets/_Project/Art/Props/<Nombre>/` (la lata trae sus etiquetas en `Texturas/`; el resto usa la librería) |
| Fuentes (.blend, vistas previas, scripts de Blender, las dos guías) | `Fuentes/` (fuera de Assets: Unity no los importa). La guía de arte quedó como `Fuentes/Guia_arte_MR04.md` |

No entraron al proyecto los `.glb` (son la misma malla que el FBX) ni los `_MetalRough` (la guía dice que en Unity no hacen falta): siguen en las descargas de Tomi.

### La herramienta de materiales
Menú **Survival en tren > Arte > Crear materiales y conectar modelos** (`Scripts/Editor/ArtImport.cs`):
- Las texturas que entran en `Assets/_Project/Art` se configuran solas por el final del nombre: `_Normal` como mapa de normales, `_MetalSmooth` sin corrección de color (R metálico, G oclusión, A suavidad), `_BaseColor` como color. Sirve para las entregas que vengan.
- Crea un material URP Lit por juego de texturas (mismo nombre, `MT_...`): **85 materiales** (63 de la librería, los del vagón y los de la etiqueta de la lata). Con los especiales de la guía: rejilla recortada; vidrio, escarcha, hielo y calcos transparentes; hielo celeste con alfa 0,6; estufa que brilla en ámbar (#FFD97A).
- Conecta cada material de cada FBX al suyo por nombre: primero los de su carpeta, después los de la librería (el vagón usa algunos nombres con guion bajo, como `MT_Nieve_Compacta`, que se buscan también sin él).
- Los FBX se importan sin cámaras, luces, animación ni colliders.

### El vagón en el juego
- **Lo que se ve del tren es el vagón MR-04**; las cajas grises quedan **invisibles pero siguen siendo la colisión**. Así no cambia nada de lo medido en el M1 (caminar dentro del tren en marcha, subir y bajar, las cajas): el riesgo principal del Anexo C queda igual.
- El FBX trae la cabina hacia atrás (la guía lo avisa): se gira 180°. Medido así, **todo coincide con el tren del juego al centímetro**: palancas, diales, cama, estante, banco, estufa, consola, puertas, boca de combustible, bogies.
- **Palancas:** las del arte se mueven con las del juego (reversor, potencia, freno), ±30° como dice la guía.
- **Agujas:** el velocímetro del arte va de 0 a 120 km/h; medí la escala en la textura de los diales (de 135° en 0 a −107° en 120) y la aguja la sigue. El de combustible va de E a F (±135°). La luz roja de reserva quedó abajo del dial.
- **Ruedas:** los cuatro ejes giran según la velocidad (componente nuevo `TrainWheels`).
- **Puertas:** las del arte son **con bisagra** (las del juego eran corredizas). Ahora la puerta abre 100° hacia afuera girando sobre la bisagra; todo lo demás es igual (E abre y cierra, no cierra si hay alguien en el hueco, el frío entra con la puerta abierta). Si Tomi las prefiere corredizas, es un valor de la puerta (`openEuler` / `openOffset`).
- **La pantalla de la sonda** (la dibuja el juego) quedó dentro del monitor del arte.
- **La consola de la sonda, movida (pedido de Tomi):** en el modelo estaba en el borde de la ventana de la cabina y con la pantalla a 0,9 m del piso, poco visible. Ahora se separa de la malla de la cabina, se corre 90 cm hacia atrás a la pared lisa entre dos ventanas (z 5,9 a 6,7) y se sube 60 cm sobre una base de acero: **la pantalla queda a 1,5 m del piso**, a la altura de la vista (los ojos del jugador están a 1,65 m). La malla separada se guarda en `Art/Tren/MR04/Generado/` y se rehace cada vez que se arma la escena. **Para el chat de arte:** conviene mover la consola así en el .blend, para que la próxima entrega ya venga en su lugar.
- **La sierra fabricada** que aparece colgada es la del arte (entre el banco y el estante).
- **Lo que el juego arma por fases** (la cama y el estante reforzado) sigue siendo del juego, ahora con los materiales de la librería (madera, colchón, manta, acero). El arte los trae terminados, en una pieza por fase; el juego muestra el avance pieza por pieza (M8: "2 de 4 tablones"). Ver pendientes.
- Nada de luz horneada ni de objetos Static (la guía los pide, pero el tren se mueve). Las dos luces del techo ya estaban donde el arte puso sus marcas.

### Los props en el juego
- **Bidones:** los dos del tren son el bidón rojo; el que se fabrica en el banco, el verde. La colisión es la de siempre (el arte es unos 2 cm más ancho por lado).
- **Raciones:** cada ración es una **lata de conserva** (la del tren, la de la estación y las del refugio), con su colisión a medida.
- **Bomba manual** de la estación: la del arte, acostada sobre la mesa (mide casi un metro).
- **Piolet** y **bomba de lanza:** importados, sin usar. El piolet no está en el documento maestro (el hielo se rompe con la barreta) y la bomba de lanza es una mejora de la etapa 3.

## Mediciones (Editor)

### Que el arte no cambie el juego (`Tools/SpikeTests/regresion_arte.sh`, 18 pruebas)

Se corrieron las pruebas de todo lo que toca el arte: la física dentro del tren, las puertas, llevar cosas, los bidones, las raciones, el estante precario, el banco, el guardado, el sonido y el rendimiento. **Todas dan lo mismo que antes del arte:**

| Qué | Resultado |
|---|---|
| Jugador y cajas en el tren (M1) | parado sobre una caja a 60 km/h: corrimiento 0,00 cm; re-centrado del origen: temblor del jugador 0,03 mm, deriva 0,02 cm, cajas 1,1 mm |
| Conducción (M2) | parada, cambio y tope igual (tope a 10 km/h: 23 cm, 2,6 g) |
| Puertas con bisagra (M3) | cerrada no deja pasar (el jugador queda en x = 1,36); abierta sale a la nieve; con alguien en el hueco no cierra; desde adentro se cierra apuntando al hueco |
| Llevar cosas (M3) | a 25 km/h la caja sigue en la mano (temblor 0,1 mm) |
| Temperatura (M4) | con las dos puertas abiertas baja a −20 °C y al cerrarlas vuelve a 13,7 °C |
| Comer (M4) | la lata se come igual que la ración de antes |
| Bidones (M6) | expedición idéntica: bombea 20 L y carga 19,8 L en 4,88 h; sifón y boca de carga igual |
| Expedición con carga (M7) | sube al tren por la puerta con bisagra en 1,6 s; cuentas exactas |
| Obras y estantes (M8) | la cama y el estante reforzado avanzan pieza por pieza como antes; lo guardado en el estante precario se ve (sus lugares bajaron 3 cm, a la altura de las tablas del arte) |
| Banco (M9) | la sierra aparece colgada al fabricarla; el bidón fabricado (verde) sale sobre la mesa |
| Guardado (M12) | guardar y cargar: IGUAL (también con las puertas y las latas) |
| Sonido (M13) | la puerta abierta deja entrar el viento igual |

Ajuste: las puertas y la sierra del arte se llamaban igual que las del juego ("Puerta_Derecha", "Sierra"); al buscarlas por nombre podía aparecer la del arte. Ahora se llaman `Arte_Puerta_Derecha`, `Arte_Puerta_Izquierda` y `Arte_Sierra`, y las pruebas que buscan por nombre se repitieron.

### Rendimiento (`m13_rendimiento.cs`, Editor, sin límite de cuadros)

| Situación | Antes (greybox) | Con el arte |
|---|---|---|
| Tren quieto, adentro | 1,6 ms · 97 mil triángulos · 37 cambios de material | **1,6 ms** · 218 mil triángulos · 42 cambios de material |
| Manejando a 38 km/h, adentro | 1,6 ms · 81 mil · 35 | **1,6 ms** · 201 mil · 41 |
| Afuera, junto a la estación | 1,6 ms · 119 mil · 38 | **1,6 ms** · 151 mil · 38 |
| Adentro del taller (otro lugar) | 1,5 ms · 20 mil · 24 | 1,5 ms · 20 mil · 23 |

- El tiempo por cuadro **no cambió**: el juego sigue usando alrededor de un 10 % del tiempo disponible (la meta es 16,7 ms por cuadro, 60 FPS).
- Los triángulos dentro del tren más que se duplican (el vagón tiene detalle: remaches, rejillas, nieve, carámbanos), pero todavía son pocos para una PC actual. Los números finales tienen que salir de una build en la PC de referencia (Anexo E.1).

![Cabina manejando](Capturas_Arte/1_cabina_manejando.png)
![Interior](Capturas_Arte/2_interior.png)
![Fondo del tren: bidones, perchero y puertas](Capturas_Arte/3_fondo_bidones.png)
![Afuera: la puerta derecha abierta con bisagra](Capturas_Arte/4_exterior_puerta.png)
![La consola de la sonda en la pared, a la altura de la vista](Capturas_Arte/5_radar_en_la_pared.png)
![La consola de frente](Capturas_Arte/6_radar_de_frente.png)

## Cómo probarlo vos en Unity (paso a paso)

1. Abrí `Assets/_Project/Scenes/Pruebas/M2_Conduccion` y dale Play.
2. **Adentro:** caminá por el vagón. El banco, la estufa, el estante, la consola y el pupitre son los del arte; la cama y el estante reforzado se siguen armando por fases.
3. **Cabina:** tomá los controles (E sobre el pupitre). Con W/S y R/F se mueven las palancas del arte; al arrancar, la aguja del velocímetro sube (40 km/h queda sobre el "40"). La aguja de combustible marca ¼ (50 de 200 L).
4. **La sonda:** la pantalla de radar aparece dentro del monitor verde, a la izquierda de la cabina.
5. **Puertas:** E en la puerta de atrás: abre hacia afuera con bisagra. Bajá por la escalera y mirá el tren desde la nieve: con el tren en marcha, las ruedas giran.
6. **Props:** los bidones rojos al fondo del tren, las latas en el estante, la bomba manual sobre la mesa de la estación; fabricá un bidón en el banco: sale el verde.
7. Para volver a armar los materiales si llega una entrega nueva: menú **Survival en tren > Arte > Crear materiales y conectar modelos**, y después reconstruir la escena (**Survival en tren > M2 > Construir escena de conducción**).

## Pendientes y preguntas

- **Para el chat de arte** (lo que serviría en la próxima entrega):
  1. **La cama y el estante reforzado en piezas por fase**, para mostrar el avance con el arte: la cama arranca como una tela sucia en el piso; fase 1, 4 tablas con patas; fase 2, 3 partes de relleno; fase 3, manta y almohada. El estante: 2 soportes de metal, 2 postes, 2 tablas abajo y 2 arriba. Mientras tanto, el juego usa sus piezas con los materiales de la librería.
  2. **La barreta** (la herramienta del inicio): todavía es gris.
  3. La tapa de la boca de combustible tiene su pivote, pero todavía no se abre al cargar.
- **Para Tomi:**
  1. **Puertas con bisagra:** quedaron como en el modelo. Si las querés corredizas, se cambia un valor.
  2. **El piolet:** ¿reemplaza a la barreta para romper hielo, o es otra herramienta? El documento no lo tiene; no le di uso.
  3. **Tamaño en GitHub:** el arte suma unos 640 MB (va por Git LFS). Si la cuenta de GitHub es gratuita, revisar el cupo de LFS antes de sumar más librerías.
- Las cajas de prueba del M1 (las de cartón) siguen grises: son para las pruebas de física, no del juego.
