# Parte 1 — Arreglos (interfaz, vista, tren más grande, combustible y velocidad)

Lo pidió Tomi el 2026-10-16, después de jugar la slice:
- "El UI de todo se ve extra chico."
- "El gameplay es lento, tenemos que rechequear el tema del combustible."
- "No me gusta lo chico que se ve el tren: se ve muy chico, muy angosto." Después: "Probá lo del campo de visión y también lo de más ancho y alto."

El 2026-10-17 sumó:
- "Bajá textura de roca."
- "Que vaya más rápido el tren."

Esta es la **Parte 1** del plan aprobado ("primero arreglos, después grindeo"). La Parte 1b (la nieve del piso sin repetición) y el grindeo (`Docs/Plan_Grindeo_7Days.md`) vienen después.

## 1. La interfaz ya no se ve chiquita
**Qué pasaba:** toda la interfaz (barras, menú, mochila, banco, indicaciones) se dibujaba en píxeles fijos. La vista Game de Tomi dibuja a 3840 x 2160 (4K) en un monitor de 1920 x 1080, así que todo se veía a un tercio del tamaño pensado.

**Qué cambió:**
- La interfaz se diseña para una pantalla de 1280 x 720 y se **agranda sola según el alto de la pantalla**. En 4K, todo se dibuja 3 veces más grande, así que se ve igual que en un monitor común.
- Nueva opción **"Tamaño de la interfaz"** en Opciones (menú principal y pausa): de 70 % a 160 %.
- Pasaron a este sistema las 14 pantallas del juego: barras de estado, barra de herramientas, indicaciones, mochila, estantes, banco, libro de planos, colocar muebles, menú principal, pausa, opciones, guardado, fundidos y el final.
- Código: `Scripts/UI/UiScale.cs`.

## 2. Campo de visión
- Nueva opción **"Campo de visión"** en Opciones: de 60° a 100°. Arranca en 70°, igual que antes.
- Mueve las dos cámaras a la vez: la del mundo y la de las manos (así la herramienta no se deforma).
- Con más campo de visión, el tren se siente más amplio. Lo dejo a gusto de cada jugador.

## 3. El tren, más ancho y más alto
| | Antes | Ahora |
|---|---|---|
| Ancho por fuera | 3,4 m | **4,4 m** |
| Alto por dentro (piso a techo) | 2,4 m | **2,9 m** |
| Puertas | 2,1 m de alto | 2,54 m |
| Largo | 19 m | 19 m (igual) |

- **El arte del vagón se estira solo** a lo ancho y a lo alto. Las ruedas, los bogies, la cabina y los muebles no se deforman: se corren a su lugar. El código está en `TestScenesBuilder.ArtWiden.cs`, y las copias estiradas de las mallas quedan en `Art/Tren/MR04/Generado/*_Ancho.asset`. El FBX original no se toca.
- **Lo jugable** (paredes, piso, techo, puertas, escalones y la boca de combustible) se arma con las medidas nuevas.
- **La consola de la sonda y el banco** se corrieron con la pared. El banco pasó de x -1,2 a x -1,7.

Antes y después, desde el mismo lugar:

![Antes](Capturas_Parte1/1a_antes_pasillo.png)
![Después](Capturas_Parte1/1b_despues_pasillo.png)
![Pasillo](Capturas_Parte1/2_pasillo_ancho.png)
![Afuera](Capturas_Parte1/3_tren_afuera.png)
![Frente](Capturas_Parte1/4_frente_afuera.png)

## 4. Combustible y ritmo
**Qué pasaba:** el motor gastaba por **hora de juego**, pero el reloj del juego corre 60 veces más rápido que el real y el tren va a velocidad real. Un kilómetro a 40 km/h costaba unos **14 litros**: con el tanque inicial no llegabas a ningún lado y todo se sentía lento.

**Qué cambió** (valores en `Data/Train/FuelSettings.asset`, PROPUESTA para ajustar jugando):
- **El motor gasta por kilómetro recorrido:** 0,8 / 1,0 / 1,3 / 1,8 litros por km, según la posición de potencia (1 a 4). Parado no gasta, y tampoco con el reversor en neutro.
- **La calefacción sigue por hora de juego**, y es la presión principal (el frío): baja de 1,5 a **1,2 L por hora** (1 minuto real = 1 hora de juego).
- **El tanque arranca con 60 L** (antes 50).
- Ejemplo con el tanque inicial de 60 L, contando también la calefacción mientras se viaja:

  | Velocidad | Un km tarda | Gasta por km (motor + calefacción) | Alcanza para |
  |---|---|---|---|
  | 80 km/h | 45 s reales | 1,8 + 0,9 = 2,7 L | ~22 km |
  | 40 km/h | 90 s reales | 1,0 + 1,8 = 2,8 L | ~21 km |
  | 20 km/h | 3 min reales | 0,8 + 3,6 = 4,4 L | ~14 km |

  Ir despacio no ahorra, porque el frío sigue corriendo. Parado y con la calefacción, el tanque dura unos 50 minutos reales.

## 5. El tren va más rápido
- **Antes de subirle la velocidad, la medí.** El M1 había validado al jugador adentro del tren hasta 60 km/h. Ahora una prueba nueva (`velocidad_alta.cs`) mide a 60, 80 y 90 km/h:

  | Velocidad | Temblor del jugador | Corrimiento parado | Temblor de las cajas | Cajas corridas | Jugador sigue en el tren |
  |---|---|---|---|---|---|
  | 60 km/h | 0,07 mm | 0,02 cm | 1,20 mm | 0,10 cm | sí |
  | 80 km/h | 0,06 mm | 0,11 cm | 1,64 mm | 0,13 cm | sí |
  | 90 km/h | 0,06 mm | 0,11 cm | 1,77 mm | 0,14 cm | sí |

  Todo queda muy por debajo de lo que se nota (un milímetro de temblor no se ve).
- **Marchas nuevas** (valores en `Data/Train/TrainDriveSettings.asset`, PROPUESTA):

  | | Antes | Ahora |
  |---|---|---|
  | Velocidad tope de cada potencia (1 a 4) | 15 / 30 / 45 / 60 km/h | **20 / 40 / 60 / 80 km/h** |
  | Aceleración | 0,6 m/s² | 0,8 m/s² |
  | Frenos (1 a 4) | 0,4 / 0,8 / 1,3 / 1,9 m/s² | 0,5 / 1,0 / 1,6 / 2,4 m/s² |

  Con el freno a fondo, desde 80 km/h el tren para en unos 100 m.
- El velocímetro de la cabina (el del arte) ya llegaba a 130 km/h. El de la versión sin arte ahora llega a 90.

## 6. Texturas de roca
- Bajé dos texturas de roca escaneadas de **Poly Haven**, con licencia CC0 (libres, sin pedir permiso ni nombrar). Están en `Art/Libreria/rocas`:
  - **MT_RocaOscura** (`dark_rock`, de Amal Kumar; una baldosa cubre 2,4 m): para las paredes de montaña y las rocas grandes.
  - **MT_RocaGris** (`rock_01`, de Rob Tuytel; 1,5 m): para las piedras sueltas, la cantera y el pie de la montaña.
- Cada una trae los mapas de color, relieve y metal/suavidad, como el resto de la librería. Están anotadas en `catalogo.json` y `LICENCIAS.txt`.
- Se usan en G1 (las montañas y las rocas para picar). Todavía no aparecen en el mundo.

## 7. La nieve del piso sin repetición (Parte 1b)
Tomi: "la nieve se ve con repetición". Cada textura de nieve mide 2 m y se repetía igual sobre un terreno de 3 km, así que se veía una grilla de marcas hasta el horizonte.

**Qué cambió:**
- **Un sombreador propio para el terreno**, "Survival en tren/Terreno Nieve", sin paquetes nuevos. Es una copia del de URP con tres cosas agregadas:
  1. **Corrimientos que cambian por el terreno.** Cada capa se lee con un corrimiento que cambia de a poco cada unas 7 baldosas y se mezcla con otro. Así deja de verse la misma baldosa una al lado de la otra.
  2. **De lejos, la misma textura más grande.** A partir de unos 20 m se mezcla con una versión de 13 m (la de cerca mide 2 m), así de lejos tampoco se ve la grilla.
  3. **Manchas de brillo muy suaves**, de unos 70 m, como zonas más sopladas o más apretadas por el viento.

  Todo se calcula sobre el terreno, no sobre el mundo, así que no salta cuando el juego recentra el origen (el arreglo del M1 para viajar lejos).
- **Capas nuevas del suelo:**
  - nieve compactada por el viento, en manchas por el campo;
  - una cuarta capa, **nieve sucia** (`MT_NieveSucia`), pegada a la vía.
- **Dónde se ajusta:**
  - Los corrimientos, lo de lejos y las manchas de brillo están en el material `Data/World/Terreno/Terreno_Lit` (se ve en el Inspector). Al reconstruir la escena no se pisan.
  - Las manchas del viento y la nieve sucia están en `Data/World/Terreno.asset`. Ahí sí hay que reconstruir la escena para verlas.
- **Cómo se armó:** el sombreador sale de un script (`Fuentes/Sombreadores/generar.pl`) que toma el terreno de URP y le agrega lo de `NieveSinRepeticion.hlsl`. Si algún día se actualiza URP, se corre de nuevo.
- **Lo que se ve cerca no cambia** (es el grano de la textura). Cambia de media distancia para allá.

![Nieve antes y después](Capturas_Parte1/6_nieve_antes_despues.png)

**Rendimiento** (medido en el Editor con la vista Game en 4K, alternando las dos versiones en la misma sesión; tiempo por cuadro, sin tope de cuadros):

| Dónde | Terreno original de URP | Nuevo (versión final) |
|---|---|---|
| Afuera, mirando el campo nevado | 5,48 ms | **4,54 ms** (casi 1 ms menos) |
| Adentro del tren, mirando el pasillo | 7,51 ms | **7,59 ms** (igual) |

Para llegar ahí hicieron falta tres vueltas:
1. La primera versión costaba 2,7 ms más afuera, porque leía cada capa tres veces.
2. Después dejó de leer las capas que no se usan en cada punto: casi siempre se ven una o dos de las cuatro, y el de URP lee todas. Si no hay mezcla, lee una sola vez, y la textura grande de lejos se usa solo para el color. Con eso afuera quedó más barata que la de URP, pero adentro del tren seguía costando 0,6 a 0,8 ms más. Desde ahí se dibuja mucho terreno tapado por las paredes, y el relieve y la máscara se seguían leyendo dos veces.
3. Ahora el relieve y la máscara se leen una sola vez, con el corrimiento que más pesa en ese punto. Además, desde 250 m vuelve la imagen simplificada de lejos de Unity: con la niebla de hoy, a 200 m ya se ve solo el 4 % del terreno y a 300 m nada, así que el cambio no se ve.

Si un día hay días despejados con menos niebla, el sombreador ya tiene preparado un modo de una sola lectura para lo lejano ("Lejos: desde donde se lee una sola vez").

**Lo que queda para después** (de la lista del grindeo, sección 3.3): textura girada, nieve con ondas de viento (sastrugi), hielo y roca como capas del suelo, y montoncitos de nieve como objetos cerca del jugador. La roca va con las montañas (G1).

## Pruebas
- **La regresión completa** (47 pruebas) se corrió con el tren ancho y el combustible por km. Las diferencias venían de **pruebas con medidas viejas escritas a mano**, no del juego:
  - `m3_puertas`: la pared en 1,6 m. Ahora la prueba mide la pared desde la puerta, así no se rompe si el tren vuelve a cambiar.
  - `m3_llevar`, `m7_panel_y_bultos`, `m7_expedicion`, `m10_bifurcacion` y `m10_slice`: "adentro del tren" medía 1,6 o 1,7 m.
  - `m9_banco`: el banco se corrió con la pared.
  - `m6_combustible`: esperaba el gasto por hora. Ahora mide litros por km.
  - `m6_interactor_e`: esperaba el sifón viejo. Ahora lo lee de los datos.
  - La medición de cajas del M1 (`SpikePropMetrics`) contaba como "afuera" las cajas cerca de las paredes nuevas.
- **La regresión completa final** (47 pruebas, 2026-10-17, con todo lo de este documento y la prueba nueva `velocidad_alta`): las 47 devolvieron resultado y no hubo ningún error. Las diferencias con la corrida anterior son las esperadas (velocidad, combustible por km, medidas del tren nuevo) y variaciones chicas de números. Se tomó como "bien" lo que coincide con lo esperado por cada prueba, igual que en las regresiones anteriores.
- **Rendimiento:** la prueba marcó 7,6 ms por cuadro. Pero es por la vista Game en 4K: en 1080p, el mismo juego da 3,3 a 3,8 ms. Con un monitor de 1080, dibujar en 4K cuesta el doble y no se ve mejor.
- Pruebas corridas de nuevo después de los arreglos y de la velocidad nueva:

  | Prueba | Qué mira | Resultado |
  |---|---|---|
  | `velocidad_alta` (nueva) | temblor y cajas a 60, 80 y 90 km/h | bien (tabla de la sección 5) |
  | `m2_cambio_y_parada` | cambio de vía, tope, marcha atrás, parar en el cartel | bien (para a +0,27 m del cartel desde 20 km/h) |
  | `m2_tope` | chocar el tope sin frenar | bien: nada sale del tren y el jugador sigue adentro (ver riesgos) |
  | `m3_puertas` | puerta cerrada, abierta, no cierra con el jugador en el hueco | bien |
  | `m3_llevar` | llevar, soltar y lanzar cajas en marcha (a 58 km/h) | bien |
  | `m6_combustible` | calefacción por hora y motor por km, medidor, tanque vacío | bien (1,00 L/km en potencia 2 y 1,80 en potencia 4, lo esperado) |
  | `m6_interactor_e` | sifón con E mantenido | bien (9 L en 6 s, lo esperado) |
  | `m6_expedicion` | expedición a la estación con el bidón y la bomba | bien |
  | `m7_panel_y_bultos` | mochila, soltar un bulto con el tren a 48 km/h | bien (queda adentro) |
  | `m7_expedicion` | desarmar en la estación y volver cargado | bien (sube al tren en 1,6 s) |
  | `m8_ciclo_completo` | cama y estante reforzado en 4 viajes | bien |
  | `m9_banco` | banco, sierra y bidón sobre la mesa | bien |
  | `m9_taller_ciclo` | plano, banco, sifón y carga | bien |
  | `m10_bifurcacion` | el tren pasa de largo con el cambio congelado | bien |
  | `m10_slice` | de cero a la ruta desconocida | bien: 254 s reales (antes 281) y frena a 36 m del cambio |
  | `m11_loop` | el loop completo en 3 viajes | bien: 276 s reales (antes 305) |
  | `m13_sonido` | ambiente, motor y juntas de rieles | bien |
  | `m13_menu_y_final` | pausa, guardar y el final | bien |
  | `m13_rendimiento` | tiempo por cuadro en 6 situaciones | igual que antes, dentro de la variación entre corridas (ver la medición de la nieve) |
  | `parado_en_caja`, `recentrado` | el jugador sobre una caja a 60 km/h; recentrar el origen | igual que antes |

  Dos pruebas más fallaban por suposiciones viejas y se corrigieron:
  - `m10_slice` frenaba siempre en el mismo punto, que alcanzaba a 60 km/h. A 80 km/h el tren quedaba encima del cambio y no se podía mover la palanca. Ahora frena según la velocidad y el freno de los datos.
  - `m9_taller_ciclo` apuntaba al banco en su lugar viejo.


## Qué probar en Unity, paso a paso
1. **Interfaz:**
   1. Abrí `Scenes/Menu_Principal` y dale Play. El menú tiene que verse grande y legible.
   2. Entrá a **Opciones** y mové **"Tamaño de la interfaz"**: todo crece o se achica.
   3. Mové **"Campo de visión"** y entrá a la partida para verlo.
2. **Tren:**
   1. En una partida, caminá por adentro del tren: tiene que sentirse más ancho y con más aire arriba.
   2. Bajá por la puerta y miralo de afuera.
3. **Velocidad y combustible:**
   1. Tomá los controles (E en la cabina). Con **W** subí la potencia hasta 4: el tren llega a 80 km/h.
   2. Mirá el medidor de combustible: baja según los kilómetros, no según el tiempo.
   3. Frená con **S** hasta el freno 4.
4. **Nieve:**
   1. Bajate del tren y mirá el campo lejos: no tiene que verse la grilla.
   2. Para jugar con los valores, abrí el material `Data/World/Terreno/Terreno_Lit` en el Inspector (sección "Nieve sin repeticion"). Los cambios se ven en vivo.
5. **Si algo no te gusta** (muy rápido, muy lento, gasta mucho o poco): son números en `Data/Train/TrainDriveSettings.asset` y `Data/Train/FuelSettings.asset`. Decime cómo lo sentís y los ajusto.

## Riesgos y pendientes
- **La primera potencia ahora es 20 km/h.** Para acomodar el tren en un lugar (por ejemplo, frente a un cambio de vía) hay que usar un poco el freno. Si molesta, la bajo a 10.
- **Choque contra el tope a 20 km/h** (prueba `m2_tope`): desacelera 5,9 g y las cajas se corren hasta 3 m, pero nada sale del tren y el jugador sigue adentro. Antes la prueba chocaba a 15 km/h (3,8 g). Es chocar a propósito sin frenar.
- **El cielo de día se ve casi de noche**: azul oscuro con estrellas al mediodía (el sol está a 31°). Ya era así en las fotos de la P1, así que no viene de estos cambios. Si no es lo que buscás, lo reviso.
- **La vista Game está en 4K.** La dejé como la tenía Tomi. Con un monitor de 1080, ponerla en 1920 x 1080 es más liviano y se ve igual.
