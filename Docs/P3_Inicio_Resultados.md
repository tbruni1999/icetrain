# Producción P3 — Inicio del juego: resultados

Parte del plan de la noche. Que el juego arranque como un juego: un menú principal, opciones, el despertar de la sección 20.1 y una versión exportada (build) de Windows con su medición de rendimiento.

## Qué hay
- **Menú principal** (`Scenes/Menu_Principal`, primera escena del juego): el vagón al atardecer en la nieve, con la nevada cayendo y las ventanas encendidas; la cámara gira despacio. **Nueva partida**, **Continuar** (si hay una partida guardada), **Opciones** y **Salir**. Mouse o flechas + Enter. Se regenera con **Survival en tren > Juego > Construir menú principal**.
- **Opciones** (en el menú principal y en la pausa): volumen, sensibilidad del mouse, calidad (alta / media / baja: sombras, cuánta nieve cae y el detalle del terreno lejano) y pantalla completa. Se guardan en `opciones.json`, al lado de las partidas (nada de PlayerPrefs).
- **El despertar** (sección 20.1): con "Nueva partida", la pantalla arranca negra con el viento y el tren; el jugador está acostado junto a la cama mirando el techo, la vista se abre y se levanta despacio (7 s). Sin textos. No aparece al continuar una partida ni en las pruebas.
- **Menú de pausa** (Escape): suma **Opciones** y **Salir al menú**.
- **Continuar** carga la partida guardada (como F9).
- **Build de Windows**: **Survival en tren > Juego > Exportar build de Windows** → `Builds/Windows/SurvivalEnTren.exe` (fuera de Git). Con `SurvivalEnTren.exe -medicion` el juego entra solo, recorre las situaciones de la prueba de rendimiento y escribe `medicion.txt` en su carpeta de datos.

## Mediciones

| Prueba (`p3_inicio.cs`, del menú a la partida y de vuelta) | Resultado |
|---|---|
| Opciones | se abren; volumen 0,55, sensibilidad 1,7 y calidad media se guardan y se vuelven a leer iguales |
| Nueva partida | carga la escena de juego; al empezar el jugador está quieto, mirando el techo (−75°), **a 0,00 m del lugar de despertar de la cama**; el despertar dura 7,0 s; después se camina, mirando al frente (8°), con la sensibilidad de las opciones (1,7) |
| Guardar y salir al menú | guardó; volvió al menú con el tiempo corriendo; "Continuar" disponible |
| Continuar | vuelve a la escena de juego **con la partida aplicada** (13,1 h, la hora guardada) y sin el despertar |

### La build de Windows (medida en esta PC, no en la de referencia)

`SurvivalEnTren.exe -medicion` a 1920 × 1080, calidad alta, en un **AMD Ryzen 7 5800X3D (16 hilos), NVIDIA GeForce RTX 3070 (8 GB), 32 GB de RAM**. La build pesa 662 MB y se exportó en 2,4 minutos.

| Situación | Cuadro promedio | FPS promedio | Peor cuadro | 1 % más lento |
|---|---|---|---|---|
| Tren quieto, adentro | 3,0 ms | 333 | 8,6 ms | 3,5 ms |
| Manejando a 38 km/h, adentro | 3,5 ms | 286 | 4,3 ms | 3,9 ms |
| Afuera, junto a la estación | 2,8 ms | 361 | 5,1 ms | 3,3 ms |
| Adentro del taller | 2,3 ms | 426 | 3,3 ms | 2,8 ms |
| En la nieve, mirando el horizonte | 2,1 ms | 467 | 4,2 ms | 2,5 ms |

La meta del Anexo E.1 es 16,7 ms por cuadro (60 FPS a 1080p): el juego usa entre un 13 y un 21 % de ese tiempo, con el arte, la atmósfera, el terreno y las sombras nuevas. Esta PC es potente; la de referencia (todavía sin definir) puede dar números más altos.

REGRESION_PRODUCCION_PLACEHOLDER

## Cómo probarlo vos en Unity
1. Abrí `Assets/_Project/Scenes/Menu_Principal` y dale Play.
2. **Opciones**: cambiá la sensibilidad y el volumen (clic en la barra o flechas izquierda/derecha); "Volver".
3. **Nueva partida**: mirá el despertar. Después jugá un rato, apretá Escape → **Guardar partida** → **Salir al menú**.
4. **Continuar**: volvés a donde guardaste.
5. Para probar la build: abrí `Builds/Windows/SurvivalEnTren.exe` (doble clic).

## Pendientes
- El nombre del juego en el menú es el de trabajo ("Survival en tren": la sección 27 deja el nombre abierto).
- Una sola partida guardada (varias ranuras quedan para más adelante).
- La medición de la build es en esta PC: la PC de referencia todavía no está definida (Anexo E.1).
