# M13 — Pulido de la slice: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity`. Pruebas en `Tools/SpikeTests/m13_*.cs`. Este milestone lo planifiqué yo sin pedir aprobación (Tomi pidió seguir sin preguntar). El Anexo G pide: "audio interior/exterior, iluminación, UI mínima, performance pass, final entrando a vía nueva". Todos los valores son PROPUESTA y están en datos: `Assets/_Project/Data/Audio/AudioMezcla.asset` y `Assets/_Project/Data/World/DiaNoche.asset`.

## Qué hay

### Sonido (secciones 18.2, 19 y Anexo E.5)
No había archivos de audio y no se agregaron paquetes. Los sonidos se **generan por código** al arrancar (ruido filtrado, tonos y golpes) y son **provisorios**: cuando haya sonidos de verdad se cambia el clip y nada más.
- **Ambiente según dónde estás:**
  - **Afuera:** viento fuerte y con todos sus agudos.
  - **En un lugar sin viento** (estación, taller, refugio): más bajo y apagado.
  - **En el tren con las puertas cerradas:** casi no se oye el viento, muy apagado, y se oye la calefacción.
  - **Con una puerta del tren abierta:** el viento entra.

  El cambio es inmediato pero sin saltos: es la "diferencia acústica al cerrar la puerta" de la sección 19.
- **Motor por capas:** un ralentí grave mientras hay combustible y una capa de carga que sube con la potencia y la velocidad (más fuerte y más aguda). **Sin combustible el motor se apaga** (sección 18.2: "el motor cambia de sonido").
- **Ruedas:** un golpecito metálico en cada junta de riel (cada 12,5 m), más seguido cuanto más rápido.
- **Cosas del mundo:** palancas del pupitre, cambios de vía, puertas corredizas, hielo que se rompe, lo que se desarma, aportar a una obra, el banco de trabajo y un plano nuevo.
- **La sonda:** la pantalla de la cabina hace "ping" cuando el barrido pasa por una señal (más agudo si es el objetivo). La sonda portátil **pita más seguido y más agudo cuanto más cerca está el objetivo** (cada 2 s lejos, cada 0,3 s encima).

### Día y noche (sección 19 y Anexo E.3)
- El sol sigue la hora del reloj del juego: días cortos de invierno (**amanece 7:30, anochece 18:00**).
- **De noche:** luz de luna tenue y azul que viene siempre de arriba (nunca desde abajo del suelo), cielo oscuro y niebla azul oscura. La nieve se sigue viendo y **las ventanas del tren brillan cálidas**: es el contraste frío afuera / cálido adentro de la sección 19.
- Es barato: una luz, la niebla, el color del ambiente y el brillo del cielo.
- Arreglo de paso: el vaho de la respiración brillaba de noche como una lámpara. Ahora recibe la luz de la escena y es una mancha suave, no un cuadrado.

### UI mínima (sección 18)
- **Menú de pausa con Escape:** Seguir, Guardar partida, Cargar partida, Controles (la lista de teclas) y Salir. **El juego se detiene de verdad** mientras está abierto. Se maneja con el mouse o con flechas y Enter.
- **El panel de pruebas (F1) arranca oculto** en la escena de juego; F1 lo muestra como siempre.

### Final de la slice (secciones 21.2 y 25.3)
La primera vez que el tren entra a la ruta desconocida, la pantalla se funde a blanco nieve, aparece **"RUTA DESCONOCIDA"** y debajo **"continuará"**, y vuelve. El juego no se detiene: se puede seguir manejando.

### Performance pass (sección 23 y Anexo E.1)
Ver mediciones abajo. Cambio hecho: los dibujos de pantalla del juego (indicación, barras, panel de pruebas, mochila, banco, pausa, final, avisos) ya no usan el "maquetado automático" de Unity, que no necesitaban y generaba memoria nueva en cada cuadro.

## Mediciones (Editor)

RENDIMIENTO_PLACEHOLDER

| Prueba | Resultado |
|---|---|
| `m13_sonido.cs` | **tren cerrado:** viento 0,06 (agudos hasta 650 Hz), calefacción 0,16. **Puerta abierta:** sonó "puerta abre" y el viento pasa a 0,30 (agudos hasta 1500 Hz). **Cerrada otra vez:** 0,06. **Afuera:** viento 0,55 con todos sus agudos, sin calefacción. **En la estación:** 0,22 (1500 Hz). **Motor:** quieto ralentí 0,22 y carga 0; con potencia 4 la carga sube a 0,38 y el tono a 1,15; **juntas: 3 golpes en 39 m** (esperado ~3). **Sin combustible:** ralentí 0 y carga 0 (motor apagado). **Cosas del mundo:** sonaron palanca, hielo, desarme, obra, banco y plano |
| `m13_dia_noche.cs` | 6:00 noche (luz de luna 0,12, niebla oscura 0,09, cielo 0,05); **7:30 amanecer** (sol en el horizonte, 0,50 de luz del día); 9:00 día (sol a 14°, luz 1,00, niebla 0,81, cielo 1,30); **12:45 mediodía (sol a 32°)**; 16:00 día; **18:00 atardecer**; 19:30, 22:00 y 2:00 noche. En todas las horas **la luz viene de arriba**; las 2 luces del interior del tren siguen prendidas |
| `m13_menu_y_final.cs` (teclado y mouse virtuales) | **Escape:** pausa abierta, tiempo detenido, cursor libre, no se camina. **↓ + Enter:** "PARTIDA GUARDADA" y el menú sigue abierto. **Controles** se ven; Escape vuelve al menú; Escape cierra (el tiempo corre, el cursor vuelve a la mira). **Mouse:** clic en "Seguir" cierra. **Final:** no estaba mostrado; apareció a los 32 s con el tren en la ruta desconocida a 120 m; duró 11 s y el juego siguió (tren a 30 km/h) |

REGRESION_PLACEHOLDER

## Cómo probarlo vos en Unity (paso a paso)

1. Abrí `Assets/_Project/Scenes/Pruebas/M2_Conduccion` y dale Play (**con sonido**).
2. **Adentro del tren** se oye la calefacción y el viento muy apagado. Abrí una puerta: entra el viento. Cerrala: se apaga de golpe.
3. **Salí del tren:** viento fuerte. Entrá a la estación: se apaga un poco.
4. **Manejá:** el motor sube con W y se oyen las juntas de los rieles, más seguidas cuanto más rápido. Si te quedás sin combustible, el motor se apaga.
5. **La sonda:** en la cabina, cada vez que el barrido pasa por una señal hay un "ping". Con la portátil (**Q**) y un objetivo fijado, pita más seguido a medida que te acercás.
6. **Noche:** **F1** muestra el panel de pruebas y **F2** acelera el tiempo. A las 18:00 anochece: mirá el tren desde afuera, con las ventanas cálidas.
7. **Escape:** el menú de pausa. Probá Guardar, Cargar y Controles.
8. **El final:** reparada la bifurcación (M10), entrá con el tren a la ruta desconocida. A los ~120 m aparece "RUTA DESCONOCIDA · continuará".

## Pendientes y riesgos

- **Los sonidos son provisorios.** Están hechos por código para tener la mezcla y los momentos en su lugar; el arte sonoro final los reemplaza (los componentes reciben cualquier clip). No hay sonidos de pasos, de combustible al cargar ni de la mochila.
- **La noche es oscura de verdad** (es la intención de la sección 19), pero no hay linterna en la V1: afuera de noche se ve poco. Si molesta, se sube la luz de luna en `DiaNoche.asset`.
- **El día dura 24 minutos reales** (1 s = 1 min de juego). Las noches son largas (13,5 h de juego), salvo que se duerma.
- **El final aparece una vez por partida** y no se guarda: si se carga una partida ya adentro de la ruta, vuelve a aparecer.
- Las mediciones son del **Editor**, que suma su propio costo; los números finales tienen que salir de una build en la PC de referencia (Anexo E.1).
