# Solana — lectura del diseño y blockout (pasos 1 a 4)

Tomi pasó `Solana_y_POIs_grandes.md` (con sus imágenes, ahora en `Docs/` y `Docs/img/`) y un pedido: leerlo y devolver qué se entendió, dónde choca, qué falta decidir y cómo encaja. Después pidió: "vos seguí y crea lo que sea creable". Lo que se podía crear sin cerrar decisiones abiertas es el **blockout geométrico** (pasos 1 a 4 de la sección 12: medidas y salas cerradas). Las llaves, los mecanismos, el ruido y la fauna (pasos 5 y 6) esperan decisiones.

## 1. Lo que entendí
Solana es un shopping de cinco niveles (subsuelo a azotea, 128 × 70 m) enterrado hasta el Piso 1, sin energía y sin forma de restaurarla. Desde la vía se ve primero la torre de estacionamiento (+52 m, con un sol giratorio hasta +60 m) y el cartel SOLANA de 80 m, roto pero alegre, mirando al sur. Se entra por arriba (derrumbe, claraboya o la pasarela de la torre), se baja hacia lo más oscuro y valioso, y las salidas rápidas (túnel y muelle) se abren a mano desde adentro con llaves, una clave, manivela y volante. La regla general del mundo: la nieve enterró todo lo chico; solo existen POIs que son estructuras grandes que asoman, con muchos km entre ellos, y el borde del mapa son montañas.

## 2. Dónde choca
- **Con lo que está construido**:
  - Los lugares de la V1 (estación, taller ferroviario y refugio) son casillas chicas sobre el piso. Con la regla nueva estarían enterrados.
  - El mundo de prueba es un óvalo de 1,2 km con los lugares a 0,2–0,5 km. La región del documento tiene 45 km, con POIs cada 4 o 5 km.
  - No hay montañas de borde: en P1 quedaron fuera por la licencia de Megascans sin verificar.
  - La barra de herramientas tiene 4 casillas fijas, y el documento suma pala, cizalla, kit mecánico, manivela, volante y amoladora.
  - Lo que sí coincide: la velocidad de crucero (40 km/h es la potencia 4 del tren) y la distancia a pie desde el tren (150–500 m).
- **Con el documento maestro**:
  - El shopping y los animales quedaban fuera de la V1 (21.3). La V1 ya está cerrada, así que esto entra como producción.
  - El maestro ya hablaba de edificios semienterrados y de un perfil de enterramiento por POI: la regla nueva lo concreta.
- **Sistemas que no existen todavía**:
  - llaves y cerraduras, y la clave de la caja fuerte (con algo para escribirla);
  - el ruido como recurso y la fauna con niveles de alerta;
  - la linterna y la oscuridad de verdad;
  - la carga por niveles (escenas aditivas) y la luz horneada;
  - mecanismos manuales como la manivela, el volante y el cabrestante.
- **Detalle técnico**: con la nieve a +10 m, el terreno de nieve de P1 cortaría los pisos enterrados. El POI va a necesitar huecos en el terreno (Unity los tiene) o su propia nieve.

## 3. Decisiones abiertas antes de seguir (después del blockout)
- ¿El jugador empieza con **linterna**? (Solana se vuelve oscuro de verdad abajo.)
- ¿Cuánto **ruido** hacen la manivela, el volante y el cabrestante? (Afecta a los lobos.) ¿Hay **bengalas**?
- ¿El túnel y el muelle son **atajos permanentes** o proyectos caros?
- ~~¿RED BLANCA se traduce?~~ DECIDIDO (Tomi, 2026-10-16): en inglés, WHITE NETWORK. Linterna: sí, en el pecho con F. La V1 pasa a POIs medianos.
- ¿La V1 cambia sus lugares chicos por **Medianos** (cocheras, complejo de emergencias, torre mirador)?
- ¿La **altura de la nieve** es igual en todas las regiones? ¿Los 45 km y los 40 km/h de región sirven?
- ¿Hay **osos**? ¿Otros Mega (aeropuerto, estadio, parque con noria)?

## 4. Cómo encaja
Es producción, después del M13 (la V1 está cerrada). Va en el orden de la sección 12, un paso por vez con su prueba: **1 a 4 hechos ahora** (este documento); 5 (llaves, cerraduras, mecanismos manuales, guardado) y 6 (fauna) cuando se cierren las decisiones de arriba; 7 (luz y audio por nivel, kit modular, vestido) al final. No toca nada de la V1: es una escena aparte.

## El blockout (escena `Scenes/Pruebas/Solana_Blockout`)
Se arma con **Survival en tren > Solana > Construir blockout** (`Scripts/Editor/TestScenesBuilder.Solana.cs`). Volúmenes simples a escala real, sin arte:
- **Cáscara (paso 1)**: 128 × 70 m; subsuelo (−4,5 a 0), planta baja (0 a +6), Piso 1 (+6 a +11), Piso 2 (+11 a +17, solo el bloque norte: al sur queda la terraza a +11) y azotea (+17). El vacío del atrio (40 × 26 m) atraviesa del Piso 1 a la claraboya, con la fuente helada abajo.
- **Núcleos alineados (paso 2)**: la escalera de emergencia (en el mismo lugar en todos los niveles, de 4 tramos de rampa de menos de 17°, que el jugador sube caminando) hasta la caseta de salida en la azotea; el pozo del montacargas (un pozo de verdad: la cabina está trabada abajo); las dos escaleras mecánicas congeladas del atrio (PB → P1 → P2).
- **Salas (pasos 3 y 4)**: las **44 salas de los planos**, con su piso del color de su tipo de botín (como en los planos), un cartel con su número y nombre, y sus **puertas** donde los planos marcan las cerraduras (por ahora huecos abiertos: las llaves son el paso 5). Las salidas selladas se ven: la compuerta de la brigada y el túnel (E3), la persiana del muelle (E4).
- **Azotea**: la **duna oeste** sube de la nieve (+10) al parapeto y el **cono de nieve** baja por la boca del derrumbe hasta el gimnasio colapsado (E1), con vigas rotas; la **claraboya** con paneles de vidrio, la pasarela técnica y el panel roto que baja al balcón del Piso 2 (E2); la sala de máquinas, la caseta de la antena y el tanque de agua.
- **Torre de estacionamiento**: 40 × 26 m, 14 niveles de 3,7 m con rampas entre niveles, los dos primeros enterrados y el resto abiertos con el parapeto de un color por nivel (se repite cada ocho, con el nombre de su animal), la losa colapsada del nivel 7, la entrada E5 desde la nieve del este al nivel 3, la **pasarela cubierta** del nivel 4 al hall del cine, el techo-mirador a +51,8 m con PARKING y el **sol giratorio** (gira solo, con rachas; `WindSpinner`).
- **El cartel SOLANA**: letras de bloques de 10 m (S amarilla, O turquesa, L coral inclinada, A rosa, N verde y la última A naranja colgando), el sol del emblema sonriente con un rayo caído y el banner SUMMER PARTY! roto, sobre la estructura de marquesina oxidada; mira al sur.
- **Afuera**: la nieve a +10 al oeste, sur y norte y a +5 al este (con la bajada suave entre las dos), la vía al sur con un tren de referencia de 19 m, el camino de **farolas apagadas con banderines** (18 m, asoman 8) y el pilón SOLANA SHOPPING · Everything under one sun.
- Los carteles de texto usan un material que respeta lo que tienen adelante (`Shaders/TextoConProfundidad.shader` + `DepthTextMaterial`): con el de Unity se veían a través de la nieve y las paredes.

### Mediciones (prueba `Tools/SpikeTests/s1_solana_recorridos.cs`, en `regresion.sh`)
- **Salas con piso firme: 40 de 40** (los 4 pozos del montacargas no cuentan: son pozos).
- **Recorridos caminando como el jugador** (las rutas del documento; ninguna se trabó, ninguna cayó, salto máximo entre dos pasos de 9 a 24 cm):
  - E1 → Administración (paso 4): entrar por la duna y el derrumbe, cruzar el gimnasio y el balcón, bajar por la escalera de emergencia al Piso 1 y llegar a la Administración: **19/19 puntos, 71,5 s**.
  - Del subsuelo a la azotea por la escalera (paso 2): **23/23, 50,9 s**.
  - Las escaleras mecánicas PB → P1 → P2: **7/7, 13,7 s**.
  - La claraboya (E2): pasarela técnica y panel roto hasta el balcón: **8/8, 23,2 s**.
  - La torre (E5): de la nieve del este al nivel 3, la rampa al 4, la pasarela y el hall del cine: **9/9, 24,8 s**.
- Ajuste en el camino: al salir de la caseta a la azotea el jugador chocaba con la cabeza en la viga del cartel. La estructura subió a +19,2 m (las letras quedaron de +19,6 a +29,6).

### Capturas (`Docs/Capturas_Solana`)
1. Desde la vía a unos 300 m: la torre con sus colores y el cartel recortados sobre el horizonte.
2. Desde la vía, de cerca: el cartel roto pero alegre, la torre, las farolas con banderines.
3. El atrio desde la planta baja: la claraboya con sus paneles, el panel roto de la E2, las escaleras mecánicas, la fuente y el cono de nieve.
4. La boca del derrumbe vista desde el gimnasio: el cono de nieve y las vigas.
5. La pasarela cubierta hacia la torre.

## Cómo probarlo vos en Unity
1. Abrí `Assets/_Project/Scenes/Pruebas/Solana_Blockout` y dale Play: arrancás al pie de la duna oeste.
2. Subí la duna (de frente, al este), entrá por la boca del derrumbe y bajá por el cono al gimnasio.
3. Salí al balcón del atrio y mirá para abajo; buscá la escalera de emergencia (al sur, cerca del este) y bajá piso por piso hasta el subsuelo. Cada sala tiene su cartel.
4. Probá las escaleras mecánicas del atrio y la claraboya desde la azotea.
5. Para la torre: está al este; subí por la nieve del este hasta el nivel 3 (cartel E5), subí las rampas y cruzá la pasarela del nivel 4 al cine. Arriba de todo está el sol que gira.
6. Si algo no se ve como lo imaginaste (proporciones, alturas, lo que se lee desde la vía), es el momento de cambiarlo: es todo volúmenes.

## Lo que sigue (cuando se decida)
- Paso 5: llaves (K1 a K10), cerraduras (L1 a L16, con forzar con barreta y su ruido), la clave de la caja fuerte, manivela, volante y cabrestante, y el guardado de todo eso.
- Paso 6: la fauna (lobos con su guarida y alerta, zorros, cuervos) y el sistema de ruido.
- Paso 7: luz y audio por nivel, kit modular y el vestido de cada local con su estética.
- Para la región: la regla de POIs grandes, la distancia entre POIs y las montañas del borde.
