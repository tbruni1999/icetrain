# Solana y POIs grandes: especificación para Claude Code (Unity)

Este documento reúne el diseño cerrado del megaPOI Shopping Solana y de la regla de POIs grandes del juego. Es diseño, no código: sirve para planificar, construir el blockout y armar los datos en Unity.

## Cómo usar este documento

- Leerlo junto con `Docs/Documento_maestro_v0_5.md`. Las decisiones de abajo son posteriores al maestro y lo actualizan donde chocan, por ejemplo en los POI pequeños de los milestones M6 y M11.
- Según el maestro, el shopping queda fuera de la V1 (sección 21.3). No implementarlo hasta que Tomi lo pida. Cuando lo pida, seguir el orden de blockout de la Parte A (sección 12), con una prueba manual por paso.
- Lo marcado como propuesta es ajustable; el resto es decisión. Las medidas son orientativas y se ajustan en el blockout.
- Las imágenes están en `Docs/img/`.
- Ante cualquier duda de diseño, preguntar antes de inventar: no agregar mecánicas, ítems ni lore que no estén acá o en el maestro.

## Decisiones cerradas

1. La nieve está tan alta que enterró casi todo: solo se entra a estructuras grandes que asoman. Lo enterrado del todo queda fuera del juego por ahora.
2. El borde del mapa son montañas nevadas: decorado de fondo, sin contenido ni POI, que no se cruza.
3. Solana es el Mega y el punto más alto de su región: torre de estacionamiento de +52 m con un sol giratorio que llega a +60 m. Ningún otro POI pasa de +42 m.
4. Solana no tiene energía y no se puede restaurar. Nada se enciende: todo funciona a mano, con llaves, claves, barreta, manivela, volante y cabrestante.
5. Toda la cartelería está en inglés. Los nombres propios (Solana, Red Blanca) se mantienen; si Red Blanca se traduce sigue abierto.
6. El cartel SOLANA mide unos 80 m de ancho, con letras de 10 m, y mira al sur, hacia la vía. Está roto pero alegre, con un banner que dice SUMMER PARTY!.
7. El tono de todo el shopping es alegría rota, y cada local tiene su propia estética.
8. Los props estáticos expuestos llevan nieve; los objetos que usa el personaje, no.
9. No hay enemigos humanos: la fauna (lobos, zorros, cuervos) aparece como encuentros significativos, sin spawn constante.

## Contenido

- Parte A: Shopping Solana, el megaPOI.
- Parte B: POIs grandes, con catálogo, frecuencia y siluetas.
- Parte C: datos de partida en YAML para los ScriptableObjects.

# Parte A · Shopping Solana: megaPOI enterrado de cinco niveles

Shopping Solana (nombre de trabajo) es un megaPOI de cinco niveles enterrado bajo la nieve hasta el Piso 1, con un cartel gigante y una torre de estacionamiento aún más alta, ambos visibles desde el tren. Se entra por arriba, se baja hacia el botín y las salidas rápidas se abren desde adentro. Este documento fija planos, salas, llaves, peligros, estética y comportamiento del entorno para llevarlo a blockout.

## 1. Resumen y ficha del POI

Shopping Solana es un edificio de unos 128 × 70 m por nivel que quedó sepultado durante el Enfriamiento y hoy es una bóveda de hielo con mucho botín y poca luz. Fue un centro comercial climatizado y, en la fase de adaptación, se reconvirtió en Punto de Abrigo de la Red Blanca. Esa doble vida explica el loot (comercio más refugio) y la cartelería (publicidad más evacuación).

El recorrido tipo es: entrar por arriba, bajar, juntar llaves y herramientas, y abrir a mano dos salidas rápidas desde adentro. Solana no tiene energía y no se puede restaurar: todo funciona a mano. Cuanto más abajo, más oscuro, más protegido del viento y más valioso.

| Campo (Anexo D.2) | Valor propuesto |
| --- | --- |
| StableId | poi\_regionXX\_railX\_NNN (placeholder) |
| Archetype | Shopping / centro comercial (megaPOI) |
| Rail anchor | Apartadero de carga enterrado junto al muelle del Nivel -1 |
| Burial profile | Enterrado hasta el Piso 1: nieve a unos +10 m al oeste y al sur, a unos +5 m al este. Entradas válidas: boca del derrumbe, claraboya y la pasarela de la torre |
| State profile | Sin energía y sin forma de restaurarla: todo funciona a mano |
| Loot profile | Pools por subzona: médico, comida, materiales, electrónica, hogar y textil, combustible |
| Threat profile | Fauna (lobos, zorros, cuervos), frío por nivel, peligro estructural y mecánico |
| Persistent deltas | Puertas y cerraduras, loot tomado, mecanismos manuales, nivel de los tanques, túnel despejado, estado de la fauna, balizas |
| Alcance | Fuera de V1 (sección 21.3 del documento maestro). No empezar antes de aprobar el spike |

Escala de todos los planos: 1 px = 20 cm. Las medidas en metros son orientativas y se ajustan en el blockout.

## 2. Cartel, corte del edificio y entradas

Desde la vía se ven primero la torre de estacionamiento y el cartel SOLANA, gigantes sobre la nieve; después aparece el edificio enterrado, con tres entradas desde arriba y dos salidas que solo se abren desde adentro.

### El cartel SOLANA

El cartel SOLANA es lo primero que el jugador ve del POI, y se ve desde el tren. Son letras de acero de 10 m de alto y unos 80 m de ancho en total, con un emblema de sol a la izquierda, montadas sobre una estructura en el borde sur del bloque del Piso 2. Miran a la vía y asoman unos 19 m por encima de la nieve del lado sur: es el resto vertical que rompe el horizonte que pide la dirección artística (sección 19 del maestro). Su carácter es alegría rota: letras de colores de fiesta, un sol sonriente y bombillas de marquesina, todo desteñido, con piezas caídas y la nieve encima.

![Vista desde la vía, mirando al norte: el cartel SOLANA, roto pero alegre, y la torre de estacionamiento de 14 niveles coronada por un sol giratorio, el punto más alto](img/solana_vista.png)

Las alturas son orientativas. A la izquierda se ve el ala oeste colapsada, y la nieve del lado sur llega a unos +10 m.

### Rota, pero alegre

El cartel tiene que verse roto y, a la vez, transmitir el momento de alegría para el que se hizo: es el tono de todo el shopping (sección 7). Por eso conserva colores de fiesta y algo de humor, y cada daño cuenta qué pasó.

| Pieza | Cómo está rota | Qué conserva de la fiesta |
| --- | --- | --- |
| Letras | La L está inclinada, la O tiene una grieta y la última A cuelga de un solo soporte | Amarillo, turquesa, coral, rosa, verde y naranja, con formas redondeadas y amigables |
| Sol del emblema | Una grieta de lado a lado y un rayo caído | La sonrisa |
| Marquesina de bombillas | La mayoría están rotas o faltan | Unas pocas bombillas de colores siguen sanas |
| Banner | Roto, con un extremo caído | SUMMER PARTY!, el último evento del shopping antes de la nieve |
| Estructura | Oxidada, con nieve en la base y escarcha | El marco de marquesina sigue en pie |

### Qué se ve a cada distancia

| Distancia al cartel (orientativa) | Qué ve el jugador | Para qué sirve |
| --- | --- | --- |
| Más de 1 km | La silueta alta de la torre y una barra oscura de letras sobre el horizonte, a veces cortadas por la niebla | Da la dirección; coincide con la señal de estructura bajo nieve de la sonda |
| De 300 m a 1 km | Se lee SOLANA y se distingue el volumen del edificio | Confirma el destino |
| Menos de 300 m | El edificio enterrado, la torre, la duna del oeste y la zona del túnel | Permite elegir por dónde entrar |

### Estados del cartel

| Estado | Cuándo | Qué se ve |
| --- | --- | --- |
| Apagado y enterrado a medias | Al llegar | Letras de colores de fiesta desteñidos, con la base cubierta de nieve, bombillas rotas y piezas caídas (ver Rota, pero alegre); destaca contra el blanco |
| Con sol bajo | Al amanecer y al atardecer | El sol bajo enciende los colores de las letras y las bombillas sanas brillan como vidrio; nada se enciende de verdad |
| En tormenta | Con viento y nieve | Las letras se pierden entre la nieve; solo se adivina la estructura |
| De noche | Siempre | Silueta oscura contra el cielo |

Solana no tiene energía: el cartel nunca se enciende. Se reconoce por su color, su forma y el sol que gira.

### Cómo se hace

- Es un prop héroe exterior: lleva nieve y escarcha (regla de nieve de la sección 7) y puede usar texturas de 4K (sección E.4 del maestro).
- Letras, emblema y estructura son piezas separadas, para inclinarlas, romperlas o hacerlas caer una por una.
- Tiene LODs desde el pipeline y un impostor de silueta para verse de lejos aunque el interior del POI no esté cargado. Vive en la escena de exterior, no en las de los niveles.
- No hay material emisivo: las bombillas y el plástico solo reflejan el cielo y el sol.

El texto del cartel es SOLANA, el nombre de trabajo; si el nombre final cambia, se cambia en el cartel y en todo el documento. Queda como decisión abierta una variante: que desde lejos solo se lea SOL, porque ANA queda enterrada por la nieve, como guiño al slogan Everything under one sun.

### La torre de estacionamiento

La torre de estacionamiento es la estructura más alta del POI y de toda la región: el techo llega a unos +52 m, 23 m por encima del cartel; un sol giratorio la corona y llega a unos +60 m. Está al este del shopping, donde la nieve es más baja (unos +5 m), y se une al Piso 2 con una pasarela. Desde la vía se ve primero su silueta y después se lee el cartel.

| Dato | Valor |
| --- | --- |
| Huella | 40 × 26 m |
| Niveles | 14 niveles de 3,7 m, con el techo a +52 m y un sol giratorio hasta +60 m |
| Nieve | Los niveles 1 y 2 están enterrados; del nivel 3 para arriba es una estructura abierta al viento |
| Conexión | Pasarela cubierta en el nivel 4 (+11 m), hasta el hall del cine (P2-03) |
| Entrada | E5: subir por la rampa exterior, que emerge de la nieve a la altura del nivel 2 |
| Cartelería | En inglés: PARKING en el techo, LEVEL y un número en cada nivel, PARK & SMILE en cada rampa |

Adentro hay autos congelados con baterías y combustible residual, y arriba, un mirador desde donde se ven los otros hitos de la región (propuesta). Es también una ruta alternativa para entrar al Piso 2 sin pasar por el derrumbe.

En la capa de alegría rota, cada nivel tiene un animal gigante pintado (ballena, flamenco, tortuga y otros) y un color que se repite cada ocho niveles, todo desteñido y con escarcha. El nivel 7 tiene una losa colapsada que hay que rodear.

### Cómo destaca: el POI de los POI

Solana tiene que ser lo primero que se ve, se oye y se recuerda de la región, y para eso reúne recursos que ningún otro POI tiene. Los demás POI usan una silueta propia y una paleta apagada, y nada más.

| Recurso | Qué se percibe | Desde dónde (orientativo) | Detalle de diseño |
| --- | --- | --- | --- |
| Silueta | La torre de +52 m coronada por un sol giratorio (+60 m) y el cartel de +29 m | 3 km o más con buen tiempo | Es la cima de la región. Las montañas del fondo bajan detrás de Solana, para que se recorte contra el cielo |
| Color | Letras, bandas de la torre y banderines saturados: amarillo, turquesa, coral y rosa | 2 km | Único lugar del mundo con color saturado a gran escala; los demás POI usan paletas apagadas |
| Movimiento | El sol gira con el viento y los banderines flamean | 2 km | Es lo único que se mueve sobre el horizonte |
| Destello | La claraboya refleja el sol a mediodía y guiña como un espejo | 4 km | Dura unos segundos y depende del ángulo del sol y de dónde esté el tren |
| Contraluz | Al atardecer, la torre y el sol giratorio se recortan negros contra el cielo naranja | 5 km | Sin energía, el cielo hace el trabajo de la luz: es el momento más reconocible de Solana |
| Sonido | El sol giratorio chirría y el viento silba entre las letras huecas | 1 km con viento a favor | No necesita energía; con tormenta o niebla se oye antes de verse |
| Camino de farolas | Las puntas de una hilera de farolas apagadas, con banderines, asoman de la nieve y llevan desde la vía hasta el shopping | 500 m | Las farolas miden 18 m y asoman 8 m: una línea que guía sin marcador |
| Vía en curva | La vía se curva al sur de Solana y lo pone de frente durante los últimos 2 km | 2 km | Así el cartel se lee de frente y no de canto |
| Sonda | La señal más fuerte de la región | El doble de distancia que otro POI | Propuesta: la sonda marca Solana antes que a cualquier otro POI |

![Aproximación a Solana en tres encuadres: a 3 km, a 1 km y a 300 m, con la torre, el sol giratorio, el cartel, el destello y el camino de farolas](img/solana_aproximacion.png)

Los tres encuadres suponen que la vía se curva y apunta hacia el shopping en el tramo final. A 3 km se ve la torre, un punto de color y el destello; a 1 km se lee el cartel y se oye el chirrido del sol con el viento; a 300 m se ve el edificio entero, con su duna y el camino de farolas.

Estos recursos son solo de Solana y cuestan poco de producir: el sol giratorio es una pieza animada, el destello es un efecto de lente según el ángulo del sol, el chirrido es un emisor 3D de largo alcance y las farolas son props repetidos. Ninguno pide densidad poligonal (sección E.4 del maestro).

### Regla de enterramiento para todos los POI

La nieve es tan alta que enterró casi todo: lo que no la supera quedó bajo la nieve y no existe para el jugador. Por eso los POI son estructuras grandes que todavía asoman: edificios, complejos o instalaciones.

- En el shopping la nieve llega a unos +10 m al oeste y al sur, y a unos +5 m al este. La profundidad de cada región se define en el perfil de enterramiento de sus POI (Anexo D.2 del maestro).
- Lo que mide menos que la nieve de su zona quedó enterrado por completo: no se ve, no se puede entrar y no es un POI.
- Un POI es una estructura que supera la nieve de su zona, o que asoma al menos en partes: un edificio grande, una torre, una chimenea de fábrica.
- Cuanto más enterrado está un POI, más se entra por arriba y más se baja: el shopping asoma por la azotea, el cartel y la torre, y su interior queda bajo la nieve.
- Lo que asoma es lo que el jugador ve desde la vía, a 1 km o más con buen tiempo (propuesta), y la sonda confirma lo que hay debajo.
- Lo enterrado del todo queda fuera del juego por ahora: solo se entra a estructuras grandes. El resto de los POI grandes, con su frecuencia y sus siluetas, está en la Parte B de este documento.

Esta regla vale para todo el juego y habría que pasarla al documento maestro, que ya habla de edificios semienterrados y de un perfil de enterramiento por POI. La V1 menciona POI pequeños (milestones M6 y M11): con esta regla serían estructuras que asoman sobre la nieve, no objetos chicos.

### Corte y entradas

Las cinco entradas y salidas dependen de hasta dónde llega la nieve. Desde abajo no hay entrada: la puerta principal está bajo la nieve.

![Implantación: shopping, torre de estacionamiento de +52 m y vía, con el cartel mirando al sur y las cinco entradas y salidas](img/solana_implantacion.png)

La implantación muestra la vía al sur, el cartel mirando hacia ella, la torre al este y las cinco entradas y salidas.

![Corte del edificio y la torre de +52 m con su sol giratorio: oeste a este y sur a norte, con la línea de nieve, el cartel SOLANA y las cinco entradas](img/solana_corte.png)

Escala vertical exagerada; las alturas están en metros y son orientativas.

| Entrada o salida | Nivel | Cómo se usa | Estado inicial | Qué cambia después |
| --- | --- | --- | --- | --- |
| E1 Boca del derrumbe (entrada) | Azotea y Piso 2 | Subir la duna contra el parapeto oeste y bajar por el cono de nieve | Abierta, con nieve inestable | Una tormenta fuerte puede taparla parcialmente |
| E2 Claraboya del atrio (entrada) | Azotea y atrio | Bajar por la pasarela técnica y un panel roto hasta el balcón del Piso 2 | Abierta, con riesgo de caída | Ruta alta, rápida y arriesgada |
| E3 Túnel de la brigada (salida) | Planta baja, vestíbulo | Abrir la compuerta con la llave de brigada y su volante, y despejar el túnel con pala | Sellado | Atajo permanente al exterior, hacia la vía |
| E4 Muelle de carga (salida de carga) | Nivel -1 | Derretir el hielo de las guías con calor y subir la persiana con la manivela; detrás hay un frente de hielo | Sellada | Despejar el apartadero es un proyecto de infraestructura posterior (propuesta) |
| E5 Torre de estacionamiento (entrada) | Torre, niveles 2 a 4, y Piso 2 | Subir por la rampa exterior desde la nieve del este, llegar al nivel 4 y cruzar la pasarela hasta el hall del cine | Abierta, con hielo en la rampa y vidrio roto en la pasarela | Ruta larga pero sin derrumbe; el techo de la torre sirve de mirador |

## 3. Planos por nivel

Las cinco plantas comparten la huella de 128 × 70 m y los núcleos de circulación alineados, para recorrer el edificio de abajo hacia arriba sin perder la orientación. Norte arriba. Las letras K y T son llaves, herramientas e ítems; L es una cerradura; la estrella es un hallazgo excepcional; F son lobos y zorros; C son cuervos. Los números se explican en la sección 4.

### Nivel -1 · Subsuelo

![Plano del Nivel -1, Subsuelo](img/solana_plano_s1.png)

La logística queda al sur y el generador muerto, entre el depósito y los tanques. El estacionamiento ocupa el resto y la rampa vehicular está sellada.

### Nivel 0 · Planta baja

![Plano del Nivel 0, Planta baja](img/solana_plano_pb.png)

El atrio ordena la circulación en anillo. La farmacia y el supermercado quedan al norte y al oeste, la ferretería al este, y el túnel sellado al sur del vestíbulo.

### Nivel 1 · Piso 1

![Plano del Nivel 1, Piso 1](img/solana_plano_p1.png)

La Administración, abajo a la izquierda, concentra las llaves del edificio. Ropa y hogar quedan al oeste, electrónica al este y el equipo de montaña al sur.

### Nivel 2 · Piso 2

![Plano del Nivel 2, Piso 2](img/solana_plano_p2.png)

El ala oeste colapsada es la entrada estándar. El cine ocupa el este y la terraza sur es exterior.

### Nivel 3 · Azotea

![Plano del Nivel 3, Azotea, con el cartel SOLANA en el borde sur](img/solana_plano_azotea.png)

La duna sube hasta la boca del derrumbe y la claraboya está sobre el atrio. El cartel SOLANA ocupa el borde sur del bloque del Piso 2 y mira a la vía; al sur, la terraza está a +11 m.

## 4. Llaves, códigos y accesos

Una sola cadena abre el edificio, y todo funciona a mano porque Solana no tiene energía: se entra por arriba, la Administración guarda casi todas las llaves y las salidas se abren con mecanismos manuales. Todas las cerraduras son mecánicas: llave, clave, barreta, manivela o volante.

![Cadena de acceso sin energía: entrada por el derrumbe, llaves de la Administración y salidas que se abren a mano](img/solana_cadena_acceso.png)

Forzar una puerta con la barreta salta la llave a cambio de ruido, y la fauna lo oye (sección 6).

### Recorrido tipo

1. Entrar por la boca del derrumbe (E1) y cruzar el gimnasio.
2. Abrir el casillero 31 (K1) y bajar por la escalera de emergencia, cuyo cortafuego está trabado por hielo (L1, barreta).
3. Abrir la Administración (L2). Tomar K2, K3 y K4, y abrir la caja fuerte (L14) para sacar la llave de brigada (K5).
4. Pasar por la ferretería (T1, T2, T3) y por la cocina del Café Solana (K9, garrafa de gas).
5. Con K4, abrir el depósito del subsuelo (L8) y sacar la manivela de emergencia (K6) de la jaula de mantenimiento.
6. Derretir el hielo de las guías de la persiana del muelle con la garrafa y subirla con la manivela (L7): salida de carga E4.
7. Con K5, abrir la compuerta del túnel con su volante (L10) y despejarlo con la pala: salida E3.

### Llaves, herramientas e ítems

| Código | Qué es | Dónde está | Cómo se consigue | Qué abre o habilita |
| --- | --- | --- | --- | --- |
| K1 | Llave ADM | Casillero 31, vestuario del gimnasio (P2-06) | Abrir el casillero con la barreta; el listado de casilleros dice MANAGER: 31 | Administración (L2) |
| K2 | Llave técnica | Tablero de llaves, gancho TECH (P1-08) | Tomarla | Sala del generador (L5), calderas y tanques (L6), sala de máquinas (L15) |
| K3 | Llave de farmacia | Tablero de llaves, gancho PHARM | Tomarla | Farmacia (L3) |
| K4 | Llavero de servicio | Cajón del escritorio de gerencia (P1-08) | Abrir el cajón | Depósito (L8), pozo del montacargas (L9), bodega de ferretería (L11) |
| K5 | Llave de brigada (naranja) | Caja fuerte (L14), P1-08 | Clave 0731 | Compuerta del túnel (L10), con su volante |
| K6 | Manivela de emergencia | Jaula de mantenimiento del depósito (S1-05) | Abrir el depósito con K4 | Persiana del muelle (L7), junto con el calor de K9 |
| K7 | Llave del cine | Boletería (P2-03) | Abrir el cajón de la boletería | Cabina de proyección (L13) |
| K8 | Llave del armario de controlados | Colgada del cartel de turno, farmacia (PB-03) | Mirar el cartel | Armario (L4) |
| K9 | Garrafa de gas | Cocina del Café Solana (PB-05) | Tomarla; pesa | Derretir el hielo de las guías de la persiana (L7); calor para un refugio |
| K10 | Módulo receptor de radio | Electro Solana (P1-07) | Hallazgo excepcional | No abre nada: se instala en la sonda del tren |
| T1 | Pala | Ferretería (PB-09) | Tomarla | Despejar el túnel de la brigada y la boca del derrumbe |
| T2 | Cizalla | Ferretería | Tomarla | Persiana de Montaña Sur Outdoor (L12) y candado de la caseta de la antena (L16) |
| T3 | Kit mecánico | Ferretería | Tomarlo | Desmontar por piezas el generador, el autoelevador y los autos |

### La clave de la caja fuerte

La clave es 0731. La pizarra de turnos de la Administración dice SAFE: 07 + MANAGER'S LOCKER, y el listado del gimnasio dice MANAGER: 31. Si el jugador no la deduce, la bitácora del guardia la anota a medias (sección 10).

### Cerraduras

El ruido es la moneda de cambio (propuesta): bajo no despierta nada, medio inquieta a la fauna cercana, alto se oye en todo el nivel y muy alto se oye en todo el edificio.

| Código | Dónde | Tipo | Requisito | Alternativa y ruido |
| --- | --- | --- | --- | --- |
| L1 | Cortafuego de la escalera de emergencia (Piso 2 a Piso 1) | Mecánica | Barreta (hielo) | Ninguna; ruido medio |
| L2 | Administración (P1-08) | Mecánica | K1 | Forzar con la barreta: tarda, ruido alto |
| L3 | Farmacia (PB-03) | Mecánica | K3 | Romper el vidrio: ruido alto |
| L4 | Armario de controlados (PB-03) | Mecánica | K8 | Forzar con la barreta: ruido medio |
| L5 | Sala del generador (S1-06) | Mecánica | K2 | Con la barreta no se puede; amoladora (tardía): ruido muy alto |
| L6 | Calderas y tanques (S1-07) | Mecánica | K2 | Forzar con la barreta: ruido medio |
| L7 | Persiana del muelle (S1-04) | Manual | K6 y el calor de K9 para derretir el hielo de las guías | Ninguna; subirla hace ruido alto |
| L8 | Depósito general (S1-05) | Mecánica | K4 | Forzar con la barreta: ruido alto |
| L9 | Pozo del montacargas | Mecánica | K4 (llave triangular) | Forzar las puertas con la barreta: ruido alto |
| L10 | Compuerta del túnel (PB-01) | Manual | K5 y el volante; después, la pala (T1) | Ninguna; ruido medio |
| L11 | Bodega de la ferretería (PB-09) | Mecánica | K4 | Forzar la reja con la barreta: ruido alto |
| L12 | Persiana de Montaña Sur Outdoor (P1-09) | Mecánica | Cizalla (T2) | Barreta: ruido alto |
| L13 | Cabina de proyección (P2-05) | Mecánica | K7 | Forzar con la barreta: ruido medio |
| L14 | Caja fuerte (P1-08) | Mecánica con clave | Clave 0731 | Amoladora (tardía): ruido muy alto |
| L15 | Sala de máquinas (A-01) | Mecánica | K2 | Forzar con la barreta: ruido medio |
| L16 | Caseta de la antena (A-02) | Mecánica | Cizalla (T2) | Barreta: ruido medio |

Las persianas y rejas de los locales comunes (las marcadas como persiana con barreta en la sección 5) no llevan código: se fuerzan con la barreta y hacen ruido medio.

## 5. Salas piso por piso

Cada nivel tiene un oficio distinto: el subsuelo es logística y combustible, la planta baja es comercio básico, el Piso 1 es gerencia y equipo, el Piso 2 es ocio y derrumbe, y la azotea es entrada y antenas. Cada sala tiene un pool de loot coherente con lo que fue, y solo unas pocas guardan un hallazgo excepcional. Las llaves se explican en la sección 4; las plantillas de local, en la sección 7.

### Nivel -1 · Subsuelo (de -4,5 a 0 m)

| Sala | Función y loot | Acceso | Peligro | Assets clave |
| --- | --- | --- | --- | --- |
| S1-01 Estacionamiento | Autos congelados con loot menor: baterías, combustible residual (sifón), mantas, herramientas de baúl, comida enlatada | Libre desde las escaleras | Oscuridad, hielo negro, losa agrietada al sureste | Autos (3 modelos, 2 estados), columnas numeradas, charcos de aceite congelado |
| S1-02 Cisterna y bombas | Agua para el tren (sección 8.3 del maestro), tuberías de cobre (metal), bomba manual rota (pieza) | Puerta de servicio libre | Hielo fino sobre el agua: caer da hipotermia rápida | Tanque, válvulas, tuberías, pasarela metálica |
| S1-03 Rampa vehicular | Sin loot: sellada por hielo y nieve compactada. Filtra luz azul por una grieta | No practicable | Desprendimiento de hielo si se golpea la pared | Portón deformado, barrera, cartel de altura máxima |
| S1-04 Muelle de carga | Carga varada: comida, ropa, insumos. Autoelevador y camión de reparto desmontables (metal, motor, batería). Salida de carga pesada al apartadero | Persiana sellada: manivela de emergencia (K6) y calor para derretir el hielo (K9) | Palets inestables, persiana pesada | Persiana en 3 estados, autoelevador, palets, camión de reparto |
| S1-05 Depósito general | Madera de palets, estantes metálicos, stock textil, cinta y film, herramientas de mantenimiento; en la jaula, la manivela de emergencia (K6) | Llavero de servicio (K4) | Racks que caen, oscuridad total | Racks altos, cajas de temporada, jaula de mantenimiento |
| S1-06 Sala del generador | Generador diésel muerto: no arranca. Se desmonta por piezas mecánicas, cables y un motor (pieza especial) | Llave técnica (puerta cortafuego: la barreta no alcanza) | Piezas pesadas y aceite congelado; ruido al desmontar | Generador (héroe, desmontable), tablero, tanque diario, ducto de escape, cartel OUT OF ORDER |
| S1-07 Calderas y tanques | Combustible: 3 tanques bajo el piso con indicios de derrame. Plano de bomba de trasvase con anclaje al suelo (hallazgo excepcional) | Llave técnica | Vapores y derrames inflamables, piso resbaladizo | Calderas, tanques, medidores, manguera, mesa de planos |
| Núcleos | Montacargas y escalera de emergencia | Escalera libre; pozo del montacargas con el llavero de servicio (K4) | Puertas cortafuego pesadas que se cierran solas | Cabina, botonera, puertas cortafuego |

### Nivel 0 · Planta baja (de 0 a +6 m)

| Sala | Función y loot | Acceso | Peligro | Assets clave |
| --- | --- | --- | --- | --- |
| PB-01 Vestíbulo | Entrada principal sepultada: puertas de vidrio con hielo y, detrás, la compuerta térmica de la brigada. Carritos, cajero, pórtico antirrobo | No practicable desde afuera. Desde adentro: llave de brigada (K5), volante de la compuerta y pala para despejar el túnel | Entra nieve si se rompe el vidrio | Puertas dobles, compuerta térmica, pórtico, directorio |
| PB-02 Atrio | Cruce de todos los niveles. Fuente helada, jardineras muertas, cono de nieve bajo la claraboya. Loot menor: monedas, metal | Libre | Carámbanos desde el vacío, hielo en el piso, exposición a la vista desde los pisos altos | Fuente (héroe), jardineras, bancos, barandas, montículo de nieve, pozo de luz |
| PB-03 Farmacia | Médico (alto): analgésicos, antibióticos, vendas, antiséptico, suero, mantas térmicas. Armario de controlados con el botiquín de brigada (hallazgo excepcional) | Llave de farmacia, o romper el vidrio (ruido alto). Armario: llave colgada del cartel de turno | Vidrios, zorros en la trastienda, ruido | Mostrador, estanterías, heladera, armario, cartel de turno |
| PB-04 Celulares y accesorios | Componentes menores: baterías, cables, cargadores | Persiana (barreta) | Bajo | Vitrinas, exhibidores, mostrador |
| PB-05 Café Solana | Comida y utensilios, café, garrafa de gas (K9, calor), cocina trasera | Libre | Gas residual | Barra, máquina de café, cocina industrial, garrafa |
| PB-06 Baños | Poco loot; cañerías de cobre. Buen escondite | Libre | Hielo, caños rotos | Sanitarios, espejos, cascada de hielo |
| PB-07 Perfumería | Loot menor: alcohol, vidrio, telas pequeñas | Persiana (barreta) | Bajo | Vitrinas, góndolas bajas |
| PB-08 Supermercado | Comida (alta, pesada), agua, recipientes, bolsas, carros. Cámara frigorífica: carne congelada (hallazgo excepcional) y guarida de lobos | Entrada libre. Cámara: puerta manual con cartel de advertencia | Lobos, góndolas altas, espacio abierto sin cobertura | Góndolas, cajas, cámara frigorífica, freezers, carros |
| PB-09 Ferretería | Materiales y herramientas: metal, cadenas, sierra, kit mecánico, kit eléctrico, pala, cizalla; amoladora (tardía). Bodega: cabrestante y carro de carga (hallazgo excepcional) | Libre. Bodega: llavero de servicio (K4) | Estanterías altas, solventes inflamables | Estanterías industriales, mostrador, bodega, cabrestante |
| PB-10 a PB-12 Locales (calzado, kiosco y cajero, bazar) | Telas, cuero, alimentos envasados, vidrio. Cajero con billetes inútiles (detalle narrativo) | Persianas (barreta) | Bajo | Plantillas de local (sección 7) |
| Núcleos | Escalera mecánica congelada, montacargas, escalera de emergencia. Túnel de la brigada, sellado, al sur del vestíbulo | Escaleras libres | Resbalón en la escalera mecánica | Escalera mecánica (módulo), puertas cortafuego |

### Nivel 1 (de +6 a +11 m)

| Sala | Función y loot | Acceso | Peligro | Assets clave |
| --- | --- | --- | --- | --- |
| P1-01 Juguetería | Peluches (relleno y aislante), pilas, cajas. Mostrador de premios del arcade (secreto) | Persiana (barreta) | Bajo | Góndolas bajas, peluches, vitrina |
| P1-02 Librería y papelería | Papel (aislante y combustible), manuales, mapa turístico de la región (mapa parcial), cuadernos | Libre | Bajo | Estantes, mostrador, exhibidor de mapas |
| P1-03 Música y regalos | Parlantes, baterías, discos, cableado | Persiana (barreta) | Bajo | Bateas, parlantes, vitrina |
| P1-04 Baños | Igual que PB-06 | Libre | Hielo, caños rotos | Variante de PB-06 |
| P1-05 Óptica | Metal fino, vidrio, estuches | Persiana (barreta) | Cristales rotos | Vitrinas, espejos |
| P1-06 Solana Hogar y Moda | Telas (alto): ropa, mantas, cortinas, colchones. Colchón de calidad para la cama real (hallazgo excepcional, objeto entero y pesado) | Libre | Probadores oscuros, filtración de agua congelada | Ropa colgada, góndolas, probadores, maniquíes, colchones apilados |
| P1-07 Electro Solana | Componentes eléctricos (alto): baterías, lámparas, radios, kit eléctrico. Módulo receptor de radio (hallazgo excepcional) | Reja de seguridad (barreta) | Cristales, cableado expuesto | Pared de televisores, exhibidores, mostrador, cajas |
| P1-08 Administración y Seguridad | Llavero del edificio: tablero de llaves, caja fuerte, monitores apagados con la bitácora del guardia, pizarra de turnos, plano del edificio | Llave ADM, o forzar con la barreta (ruido) | Puerta pesada; poco más | Escritorios, tablero de llaves, caja fuerte, monitores CRT, pizarra, casilleros |
| P1-09 Montaña Sur Outdoor | Equipo de alto valor: parka, botas, mochila, estufa portátil. Bolsa de dormir de expedición (hallazgo excepcional) | Persiana con candado: cizalla, o barreta con ruido | Bajo | Carpa de exhibición, pared de escalada, ropa técnica |
| P1-10 Enfermería del Punto de Abrigo | Médico (medio): vendas, catres, registros de pacientes. Segundo punto médico, menos completo que la farmacia | Libre | Bajo | Camillas, biombos, estufa improvisada, lonas de brigada |
| P1-11 Pasarela del atrio | Circulación con vista a la planta baja | Libre | Caída por baranda rota, carámbanos | Baranda de vidrio y acero, bancos, jardineras |
| Núcleos | Escalera mecánica, montacargas, escalera de emergencia | Libre; pozo del montacargas con el llavero de servicio (K4) | Hielo en los escalones | Mismos módulos que PB |

### Nivel 2 (de +11 a +17 m)

| Sala | Función y loot | Acceso | Peligro | Assets clave |
| --- | --- | --- | --- | --- |
| P2-01 Arcade Solana Fun | Madera, componentes de gabinetes, fichas. Una máquina de premios mecánica que todavía funciona (secreto) | Libre | Bajo | Seis gabinetes, mostrador de premios, neón apagado, alfombra |
| P2-02 Patio de comidas | Comida deteriorada, utensilios, cocinas con gas; mesas como fuente de madera y metal | Libre | Ruta de paso de los lobos, nieve acumulada sobre el tragaluz | Mesas y sillas, cuatro fachadas de cocina, tragaluz |
| P2-03 Cine: hall y boletería | Llave del cine (en la boletería), pochoclera, carteleras | Libre | Bajo | Boletería, pochoclera (héroe), pósters, barra de dulces |
| P2-04 Cine, Sala 1 (dormitorio del refugio) | Catres, mantas (telas), diarios y dibujos, mochilas abandonadas | Libre | Oscuridad total, butacas que traban el paso | Butacas, catres, lonas, pantalla |
| P2-05 Cine, Sala 2 (proyección) | Cabina con el proyector apagado y un rollo de película del noticiero (secreto), componentes | Llave del cine (K7) | Oscuridad, escaleras de platea | Proyector (héroe), pantalla, butacas inclinadas |
| P2-06 Gimnasio Solana Fit (colapsado) | Pesas (metal), colchonetas (telas), casilleros con objetos personales. Casillero 31: llave ADM | Boca del derrumbe: es la entrada estándar al edificio | Nieve inestable, piso con huecos, vigas caídas | Máquinas, pesas, casilleros, piscina congelada, vigas, cono de nieve |
| P2-07 Terraza sur | Mesas congeladas y vista panorámica de la vía | Puerta del patio de comidas | Viento, cuervos, hielo | Toldos colapsados, mesas, barandas, nieve |
| P2-08 Balcón del vacío | Borde del atrio con barandas | Libre | Caída, carámbanos | Barandas, bancos |
| Núcleos | Escalera de emergencia; caja del montacargas, rota | Escalera con cortafuego trabado por hielo (barreta) | Hielo, caída por la caja rota | Puerta cortafuego, caja de ascensor |

### Nivel 3 · Azotea (+17 m)

| Sala | Función y loot | Acceso | Peligro | Assets clave |
| --- | --- | --- | --- | --- |
| A-01 Sala de máquinas | Piezas mecánicas, motores, ductos. Actuador de compuerta, compatible con los cambios de la Red Blanca (pieza especial) | Llave técnica | Ventiladores trabados, viento | Unidades de aire, ductos, tableros, escalera de gato |
| A-02 Antena y radio | Antena y caseta de equipos con el cuaderno de frecuencias (secreto) | Escalera de gato | Altura, viento | Torre, caseta, cableado |
| A-03 Claraboya del atrio | Vidrio y acero con paneles rotos. Entrada alternativa directa al atrio | Libre | Caída de unos 17 m si se pisa el vidrio; nieve que cae | Marcos, paneles rotos, pasarela técnica, toldo |
| A-04 Boca del derrumbe | Entrada estándar: cono de nieve que baja al Piso 1, vigas rotas | Subir la duna contra el parapeto oeste | Nieve inestable, vigas | Vigas, cortes de losa, duna, nieve |
| A-05 Tanque de agua elevado | Agua congelada y tuberías de cobre | Escalera de gato | Altura | Tanque, escalera, tuberías |
| Letras SOLANA | Hito principal del POI: unos 80 m de ancho, letras de 10 m, mirando a la vía. Sin loot | Exterior | Viento | Letras de acero (10 m), emblema de sol, estructura, nieve acumulada |
| Caseta de salida | Final de la escalera de emergencia | Escalera | Puerta con hielo | Puerta, caseta |

### Torre de estacionamiento (de 0 a +52 m)

La torre es un edificio aparte, unido al Piso 2 por una pasarela. Tiene 14 niveles de 3,7 m, una rampa helicoidal exterior y el techo a +52 m.

| Sala | Función y loot | Acceso | Peligro | Assets clave |
| --- | --- | --- | --- | --- |
| PK-01 Niveles 1 y 2 | Autos congelados con baterías y combustible residual; oscuridad | Desde el nivel 3 por escalera o rampa; la nieve tapa las entradas de abajo | Oscuridad, hielo negro, nieve que entró por las rampas | Autos, pilares numerados, charcos congelados, nieve apilada |
| PK-02 Niveles 3 a 14 | Autos, mantas y comida enlatada en los baúles; cada nivel tiene un color y un animal pintado | Rampa helicoidal o escaleras de los núcleos | Viento, hielo en las rampas; losa colapsada en el nivel 7, que hay que rodear | Autos (variantes), parapetos pintados, números y animales gigantes, pilares |
| PK-03 Pasarela (nivel 4, +11 m) | Cubierta, con un tramo de vidrio roto, hasta el hall del cine (P2-03) | Libre | Viento, vidrio roto, hielo | Pasarela modular, vidrio, barandas |
| PK-04 Rampa helicoidal | Une todos los niveles; la parte baja está enterrada y la alta, expuesta | Se sube desde la nieve del este (E5) | Resbalones, nieve acumulada | Rampa, parapetos, señales de altura en inglés |
| PK-05 Techo (+52 m) | Mirador con vista de los otros hitos de la región (propuesta); cabina de peaje con radio rota | Rampa o escaleras | Viento extremo, cuervos, hielo | Letras PARKING, cabina, autos, barandas, mástil del sol giratorio |
| Núcleos | Dos escaleras (noreste y suroeste) y un ascensor trabado | Escaleras libres; el ascensor no funciona | Puertas cortafuego con hielo | Puertas, cajas de escalera |

## 6. Fauna y peligros

El edificio no tiene enemigos humanos: el peligro es la fauna que se refugió, el frío de cada nivel y una estructura que cede. Dos reglas del documento maestro rigen todo: un encuentro con fauna debe ser significativo y no un spawn constante (sección 16.2), y cada peligro avisa antes de dañar (sección 18.2).

### Fauna

Cada grupo tiene un nivel de alerta interno: dormido, inquieto, buscando, persiguiendo. El estado de cada guarida se guarda como cambio persistente.

| Quién | Dónde vive | Qué lo despierta | Cómo reacciona | Cómo se maneja |
| --- | --- | --- | --- | --- |
| Manada de lobos (3) | Cámara frigorífica del supermercado (PB-08). Cuando está alerta recorre PB y Nivel -1 | Ruido fuerte (persiana, vidrio roto, barreta contra metal, estantes que caen), olor a comida o carne, correr cerca | Duerme; si se inquieta, se acerca al origen del ruido; si ve al jugador, ataca; después vuelve a la guarida | Evitar, cerrar puertas, distraer con ruido lejano, usar luz y fuego (propuesta). Sin arma en V1 |
| Zorros (1 o 2) | Trastienda de la farmacia y baños de PB | Verse sorprendidos de cerca | Huyen. Sus huellas en la nieve indican refugios y rutas | Inofensivos; sirven de pista |
| Cuervos (bandada) | Azotea, terraza sur y techo de la torre | Acercarse o hacer ruido arriba | Levantan vuelo y graznan: delatan al jugador a la fauna cercana (propuesta) | Moverse despacio, evitar correr en la azotea |

Queda como decisión abierta sumar un oso hibernando en el depósito de alimentos. No está incluido aquí.

### Peligros del entorno

| Peligro | Dónde | Aviso | Consecuencia | Contramedida |
| --- | --- | --- | --- | --- |
| Frío por nivel | Extremo en azotea, terraza y techo de la torre (viento); alto en el Piso 2; medio en el Piso 1; bajo en PB y Nivel -1 (sin viento) | Respiración visible, escarcha en el HUD, cambio de sonido | Baja la temperatura del jugador, después su energía y luego su vida | Refugios con fuego (cocina con garrafa, enfermería), ropa de Hogar y Moda y de Outdoor |
| Hielo negro y escalones helados | Estacionamiento, escalera mecánica, pasarelas | Brillo del piso, crujido | Resbalón, caída, daño y ruido | Caminar despacio, no cargar peso excesivo |
| Carámbanos | Atrio y pasarelas | Goteo y crujido previos | Daño por caída de hielo | No quedarse bajo ellos; un golpe de barreta los derriba |
| Nieve inestable | Boca del derrumbe y claraboya | Sonido sordo, grietas en la superficie | Deslizamiento que entierra parcialmente y retrasa | Salir en diagonal; correr aumenta el riesgo |
| Piso debilitado | Gimnasio del Piso 2 | Bordes rotos, vigas colgando | Caída al Piso 1, daño y ruido | Reconocer el piso firme por el color y el sonido |
| Hielo fino sobre agua | Cisterna del Nivel -1 y piscina del Piso 2 | Hielo más oscuro, crujido | Caída al agua helada: hipotermia acelerada | Rodearla; usar tablones (madera) para cruzar |
| Vapores e incendio | Calderas y tanques; solventes de la ferretería | Olor, distorsión del aire | Fuego si hay llama o chispa: la garrafa no se usa cerca | Sin llama; ventilar |
| Pozo del montacargas | Pozo vertical del montacargas | Corriente de aire y eco | Caída de varios pisos | Bajar por la escalera de servicio sin cargar peso; el cabrestante sube la carga |
| Estantes, racks y falsos techos | Ferretería, depósito, cielorrasos del atrio | Chirrido al moverlos | Caída, daño y ruido que alerta a la fauna | Moverse sin tirar de las estructuras |
| Cortafuegos | Pasillos de PB y Nivel -1 | Chirrido de los resortes | Se cierran solas detrás del jugador y cortan una retirada | Trabarlas con una cuña o con la barreta |
| Oscuridad | Nivel -1, interior de PB, cine | Sin luz no se ve | Pérdida de orientación y de tiempo | Linterna (decisión abierta: si empieza con una) y fuego; no hay luces que encender |

## 7. Estética y assets

Shopping Solana mezcla tres capas visuales: el centro comercial original, la adaptación de la Red Blanca y el hielo que lo cubre todo. La referencia es realismo atmosférico indie con materiales PBR, sin perseguir fotorealismo AAA (sección 19 del maestro), y sin futurismo brillante.

### Alegría rota

El tono de todo el shopping es alegría rota: un lugar hecho para celebrar, congelado en plena fiesta y herido por el frío. El cartel de la azotea es su ejemplo más claro, y el mismo criterio vale para cada sala.

- Cada espacio conserva un resto de fiesta (un color, una guirnalda, un globo, una música) y un daño que lo contradice.
- La alegría se ve en el color y la forma, y el daño en la textura y la estructura: formas redondeadas y colores saturados, con escarcha, grietas y piezas caídas.
- El humor y la ternura pesan más que el miedo: carteles de oferta, banners, peluches, dibujos. Sin cuerpos ni gore (propuesta).
- Los colores de fiesta de Solana chocan con el azul del hielo y con el naranja de la Red Blanca, y ese choque es el sello visual del POI.

### Un local, una estética

Cada local tiene su propia identidad dentro de la capa de Solana: una paleta de dos o tres colores, una tipografía, una pieza de alegría y un daño que cuenta qué pasó. Las zonas de la Red Blanca (Cine Sala 1 y enfermería) tienen la suya: lonas y dibujos de chicos.

| Local | Estética propia | Pieza de alegría | Cómo está roto |
| --- | --- | --- | --- |
| PB-03 Farmacia | Verde cruz y blanco clínico, sans limpia | Cruz de neón y caramelos en el mostrador | Cruz apagada con tubos rotos; vitrina helada |
| PB-05 Café Solana | Turquesa y crema retro, letra cursiva | Tazas de colores y pizarra con el menú del día | Menú a medio borrar; hielo en la barra |
| PB-08 Supermercado | Rojo y amarillo de oferta, precios gigantes | Globos de promoción y carteles de oferta | Globos deshinchados colgando; góndolas caídas |
| PB-09 Ferretería | Naranja industrial y negro, estilo taller | Carteles con humor y herramientas pintadas de colores | Estantes torcidos, letras caídas |
| P1-06 Solana Hogar y Moda | Lila, dorado y crema de tienda departamental clásica | Vidrieras con escenas de verano armadas con maniquíes | Maniquíes en ropa de playa cubiertos de escarcha |
| P1-07 Electro Solana | Azul eléctrico y gris, pantallas | Pared de televisores con una demo de colores | Pantallas quebradas, demo congelada |
| P1-09 Montaña Sur Outdoor | Verde bosque y ocre, madera | Carpa armada y mapa de senderos | Carpa colapsada bajo la nieve que vendía |
| P1-01 Juguetería | Colores primarios saturados, letra redonda | Peluches y móvil de globos | Peluches con escarcha, globos congelados |
| P1-02 Librería y papelería | Marrón madera y verde oscuro, tipografía clásica | Cartel de lecturas de verano | Libros hinchados por la humedad congelada |
| P2-01 Arcade Solana Fun | Neón magenta y cian sobre negro | Luces de neón y premios | Neón roto; una máquina de premios mecánica que todavía funciona (secreto) |
| P2-02 Patio de comidas | Cuatro fachadas con colores de cadenas distintas | Sombrillas de colores y carteles de combos | Sombrillas colapsadas, mesas con hielo |
| P2-03 Cine | Terciopelo rojo y dorado, marquesina de bombillas | Cartelera y pochoclera | Bombillas rotas, terciopelo rígido por el frío |
| P2-06 Gimnasio Solana Fit | Azul y naranja deportivos | Carteles motivacionales: Your summer starts today | Derrumbe, vigas y nieve |
| Locales chicos (PB-04, PB-07, PB-10 a PB-12, P1-03, P1-05) | Cada uno toma una paleta propia: violeta y plata, rosa y dorado, rojo y blanco, amarillo y rojo, multicolor kitsch, violeta y negro, azul sobrio | Un objeto de fiesta por local | Vidrio roto y escarcha |
| Cine Sala 1 y enfermería (Punto de Abrigo) | Lonas azules y blancas, papel y cartón | Dibujos de chicos y guirnaldas de papel | Humedad; no suman color nuevo |

### Las tres capas

| Capa | Qué es | Lenguaje visual | Dónde predomina |
| --- | --- | --- | --- |
| 1. Solana original | Centro comercial climatizado de fines de los 90, cálido y comercial | Terrazo, acero inoxidable, vidrio, cielorraso de placas, tubos fluorescentes, cajas de luz. Colores: amarillo sol, turquesa, terracota, crema | Locales, atrio, patio de comidas |
| 2. Punto de Abrigo | Reconversión de la Red Blanca durante la adaptación | Lonas azules y blancas, catres, cableado improvisado, estufas, carteles stencil naranja y blanco, tanques de agua | Cine Sala 1, enfermería, vestíbulo, administración, túnel |
| 3. Hielo y derrumbe | Lo que hizo el Enfriamiento después | Escarcha, estalactitas, hielo en el piso, nieve infiltrada, vigas rotas, vidrio empañado | Crece con la altura y hacia los bordes del edificio |

### Paleta

La base es de grises azulados y blancos fríos. Los colores de fiesta de Solana (amarillo sol, turquesa, coral, rosa) aparecen desteñidos pero reconocibles, y cada local suma los suyos. La luz cálida (ámbar) solo viene del fuego que enciende el jugador: garrafas, estufas y la cocina, para mantener el contraste frío y cálido que pide la sección E.3. La Red Blanca usa naranja y blanco; el peligro, un rojo apagado.

### Materiales PBR clave

| Material | Estados | Nota |
| --- | --- | --- |
| Terrazo del piso | Limpio, con escarcha, con hielo, con nieve | Mezcla por máscara de altura y normales (sección E.2) |
| Vidrio | Limpio, empañado, helado, roto | El vidrio exterior muestra nieve compactada detrás |
| Acero inoxidable | Empañado, con escarcha | Barandas, mostradores, cocinas |
| Pintura de paredes | Descascarada, con moho de humedad congelada | Pocas variantes, con decals |
| Plástico de carteles | Amarillento, quebradizo | Cajas de luz y letras |
| Lona de brigada | Limpia, sucia, rígida por el frío | Azul y blanco, reutilizable en todo el edificio |
| Madera de puntales | Nueva, con escarcha | Refuerzos de la brigada |

### Kit modular

Texturas de 2K como referencia y materiales compartidos; el 4K se reserva para los props héroe (sección E.4).

| Pieza | Variantes | Uso |
| --- | --- | --- |
| Frente de local con vitrina y persiana | 3 anchos (5, 8 y 12 m); persiana cerrada, forzada o abierta | Todos los locales |
| Piso de terrazo | Limpio, escarcha, hielo, nieve | Todo el edificio |
| Baranda de vidrio y acero | Sana, rota, helada | Pasarelas, atrio, balcón |
| Cielorraso de placas | Sano, caído, con goteo | Locales y circulaciones |
| Columna | Cuadrada y redonda; con revestimiento o sin él | Estructura |
| Escalera mecánica | Congelada; con hielo y nieve | PB a Piso 2 |
| Puertas dobles de vidrio | Cerradas con hielo, rotas | Vestíbulo, terraza |
| Puerta cortafuego | Abierta, cerrada, trabada por hielo | Núcleos y pasillos |
| Luminaria | Apagada, rota | Todo el edificio |
| Caja de luz (cartel) | 3 tamaños; nueva, descolorida, escarchada, caída | Cartelería comercial |
| Góndola y estantería | Baja, alta, caída | Supermercado, ferretería, depósito |
| Puntal y lona de la brigada | Con escarcha | Túnel, derrumbe, dormitorio |

### Props héroe

| Prop | Dónde | Función |
| --- | --- | --- |
| Fuente helada con escultura de hielo | Atrio | Hito central; monedas de deseos (secreto) |
| Generador diésel | S1-06 | Muerto: se desmonta por piezas |
| Autoelevador | S1-04 | Se desmonta por metal, motor y batería |
| Pared de televisores | P1-07 | Pantallas apagadas que reflejan la linterna |
| Tablero de llaves | P1-08 | Muestra qué llaves existen y cuáles faltan |
| Cabrestante manual | PB-09, bodega | Mejora logística (sección 9.4) |
| Proyector | P2-05 | Rollo de película: secreto del noticiero |
| Máquina de premios mecánica | P2-01 | Secreto jugable sin energía |
| Letras SOLANA | Azotea | Hito principal del POI, visible desde el tren (sección 2) |

### Plantillas de local

Los locales sin nombre propio se arman con 6 plantillas, que varían por seed (Anexo D) para no modelar cada local a mano.

| Plantilla | Loot | Props | Slots |
| --- | --- | --- | --- |
| Ropa y calzado | Telas, cuero | Percheros, exhibidores, zapateros | PB-10, P1-06 (variante grande) |
| Juguetería y regalos | Relleno, pilas | Góndolas bajas, peluches, vitrina | P1-01, PB-12 |
| Librería y papelería | Papel, manuales, mapas | Estantes, mostrador | P1-02 |
| Electro menor y celulares | Baterías, cables | Vitrinas, exhibidores | PB-04, P1-03 |
| Perfumería y óptica | Alcohol, vidrio, metal fino | Vitrinas, espejos | PB-07, P1-05 |
| Kiosco y bazar | Comida envasada, vidrio | Mostrador, heladera | PB-11 |

### Regla de nieve en props

Los objetos estáticos expuestos (azotea, terraza, derrumbe, duna) llevan nieve. Los objetos que el personaje usa, como la barreta, las linternas o una lata de comida, no la llevan, ni siquiera donde la nieve entró. Adentro, los estáticos solo llevan escarcha, salvo bajo la claraboya y en el derrumbe.

## 8. Cartelería

La cartelería cuenta el edificio con dos voces: la publicidad de Solana, que ya nadie lee, y las indicaciones de la Red Blanca, que alguien pintó con apuro. Una tercera voz, manuscrita, aporta las pistas y los avisos humanos. Todos los carteles están en inglés; los nombres propios (Solana, Red Blanca) se mantienen.

| Voz | Estilo | Colores |
| --- | --- | --- |
| Solana | Sans redondeada comercial, caja de luz | Amarillo sol, turquesa, coral, rosa, crema |
| Red Blanca | Stencil industrial, flechas | Naranja y blanco |
| Manuscrita | Marcador sobre cartón, pared o vidrio | Negro o rojo apagado |

Cada cartel existe en cuatro estados (nuevo, descolorido, escarchado, caído) y se resuelve con atlas de texturas y decals compartidos, sin un material por cartel (sección E.4). Cada local tiene además su propia cartelería, con su tipografía y sus colores (sección 7); solo el cartel principal, la evacuación y las señales de seguridad se comparten.

| Cartel | Texto de ejemplo | Dónde | Función |
| --- | --- | --- | --- |
| Letras SOLANA | SOLANA | Azotea | Cartel principal: se ve desde el tren antes que el edificio; la sonda marca el edificio bajo nieve y el cartel lo confirma a simple vista |
| Pilón exterior | SOLANA SHOPPING · Everything under one sun | Junto a la duna oeste, medio enterrado | Presenta el lugar y su ironía |
| Directorio | DIRECTORY · YOU ARE HERE, con el mapa de los niveles | Vestíbulo y junto a las escaleras de cada nivel | Mapa parcial dentro del mundo: el hielo tapa zonas |
| Evacuación de la Red Blanca | SHELTER POINT · SIDING LEVEL -1, con flecha naranja | Paredes de PB y escaleras | Señala la ruta al muelle y a la salida de carga |
| Cortafuego | FIRE DOOR · KEEP CLOSED | Puertas cortafuego | Puertas pesadas que se cierran solas |
| Salida de emergencia | EXIT, con flecha | Núcleos | Orientación |
| Promociones | SUMMER SALE −50% | Vidrieras de locales | Ironía y color contra el hielo |
| Cartelera del cine | Títulos ficticios: Eternal Summer 2, Golden Coast, Operation Thaw | P2-03 | Ironía y ambiente |
| Racionamiento | RATIONS: 1 PER PERSON PER DAY · WATER BY SHIFT | Patio de comidas y enfermería | Cuenta la vida en el refugio |
| Pizarra de turnos | Turnos de brigada y una línea: SAFE: 07 + MANAGER'S LOCKER | P1-08 | Primera mitad de la clave de la caja fuerte |
| Listado de casilleros | MANAGER: 31 | Vestuario del gimnasio | Segunda mitad de la clave |
| Farmacia de turno | PHARMACY ON DUTY, con una llave colgada | PB-03 | Esconde la llave del armario de controlados |
| Aviso manuscrito | DO NOT OPEN. NOISES INSIDE. | Puerta de la cámara frigorífica | Anticipa a los lobos |
| Brigada | SOUTH TUNNEL · SUPPORTS CHECKED | Vestíbulo | Anuncia la salida rápida y su estado |
| Estacionamiento | LEVEL -1 · SECTOR B · DOCK | Columnas del subsuelo | Orientación en la oscuridad |
| Generador | GENERATOR OUT OF ORDER, escrito a mano | S1-06 | Confirma que no hay energía |
| Etiquetas del tablero de llaves | ADMIN, TECH, PHARM, CINEMA, TERRACE | P1-08 | Muestra qué llaves existen y cuáles faltan |
| Marcas de conteo | Rayas en la pared, sin números | Café Solana y Cine Sala 1 | Pasó tiempo, sin fijar cuánto |
| Arcade | INSERT COIN · HI-SCORE | P2-01 | Pista del secreto |

Toda la cartelería del shopping está en inglés, incluida la de la torre (PARKING, LEVEL 7, PARK & SMILE) y el banner SUMMER PARTY!. Los nombres en español de los locales (Ferretería, Juguetería y otros) son nombres de trabajo: lo que se ve en el juego es el texto del cartel de la tabla siguiente.

### Carteles de los locales

| Local | Texto del cartel | Estilo |
| --- | --- | --- |
| PB-03 Farmacia | PHARMACY, con una cruz verde de neón | Verde cruz y blanco clínico |
| PB-05 Café Solana | CAFÉ SOLANA · Coffee & sunshine | Turquesa y crema, letra cursiva |
| PB-08 Supermercado | SOLANA MARKET · Weekly deals | Rojo y amarillo, precios gigantes |
| PB-09 Ferretería | HARDWARE · If we don't have it, you don't need it | Naranja industrial y negro |
| P1-01 Juguetería | TOY BOX | Colores primarios, letra redonda |
| P1-02 Librería y papelería | BOOKS & PAPER · Summer reads | Marrón madera y verde oscuro |
| P1-06 Solana Hogar y Moda | SOLANA HOME & FASHION | Lila y dorado, serif clásica |
| P1-07 Electro Solana | ELECTRO SOLANA · Bright ideas on sale | Azul eléctrico y gris |
| P1-09 Montaña Sur Outdoor | SOUTH MOUNTAIN OUTDOOR · Get out there | Verde bosque y ocre |
| P2-01 Arcade | SOLANA FUN · INSERT COIN | Neón magenta y cian |
| P2-02 Patio de comidas | FOOD COURT, con cuatro cadenas: SUNBURGER, NOODLE STOP, TACO STAND, PIZZA CORNER | Cuatro colores distintos |
| P2-03 Cine | SOLANA CINEMAS · NOW SHOWING | Terciopelo rojo y dorado |
| P2-06 Gimnasio | SOLANA FIT · Your summer starts today | Azul y naranja deportivos |
| Locales chicos | PHONE ZONE, PERFUME & CO, SHOE STOP, KIOSK, GIFT BAZAAR, MUSIC & GIFTS, OPTICAL | Cada uno con su paleta |
| Cine Sala 1 y enfermería | SHELTER POINT, INFIRMARY | Stencil naranja y blanco de la Red Blanca |

## 9. Atmósfera de un edificio enterrado

Estar enterrado invierte la lógica del exterior: cuanto más arriba, más luz, más viento y más frío; cuanto más abajo, más silencio, más oscuridad y una calma helada. El jugador baja hacia el botín y también hacia la calma, y por eso el peligro de abajo es el de lo que no se ve.

| Nivel | Luz | Sonido | Frío y aire | Sensación |
| --- | --- | --- | --- | --- |
| Azotea | Sol y cielo, luz dura y larga | Viento fuerte, graznidos, metal que cruje | Extremo, sin protección | Alivio y exposición |
| Piso 2 | Luz azul por el derrumbe y la claraboya; el cine, a oscuras | Viento amortiguado, goteo, eco del patio | Alto | Orientación y vértigo |
| Piso 1 | Luz filtrada por el vacío del atrio; locales a oscuras | Eco, crujidos de la estructura | Medio | Penumbra |
| Planta baja | El pozo de luz del atrio; el resto casi sin luz | Silencio denso, goteo, pasos sobre hielo | Bajo, aire quieto | Calma tensa |
| Nivel -1 | Casi nada: linterna y grietas azules | Zumbidos, crujidos graves, hielo que se asienta | Bajo y estable, aire pesado | Claustrofobia |

### Cómo se nota que está enterrado

- Vidrieras exteriores con nieve compactada detrás del vidrio, que filtra una luz azul tenue.
- Marcos deformados, puertas trabadas y grietas en los cielorrasos que crecen con las tormentas.
- Puntales de madera y lonas de la brigada sosteniendo los techos más dañados.
- Conos de nieve bajo cada grieta y escarcha en los bordes del edificio.
- Respiración visible y eco corto: la nieve apaga el sonido y no hay viento adentro.

### El pozo de luz

El atrio atraviesa tres niveles y la claraboya deja entrar un único haz de sol que cruza el edificio y cambia con la hora. Es el reloj interior del shopping y el punto más fotogénico del POI. Los pisos de abajo no tienen ciclo de día y noche: ahí manda la linterna.

### Cómo se logra dentro del presupuesto

- Iluminación horneada y sondas de luz por nivel, con un solo estado: sin energía (sección E.3).
- Luces dinámicas con sombra solo para la linterna y el fuego que enciende el jugador.
- El haz del atrio es un volumen animado según la hora, no iluminación global en tiempo real.
- Instantáneas de audio por nivel (azotea, pisos altos, subsuelo) con transición al bajar, y reverberación según el tamaño del espacio (sección E.5).
- Crujidos y desprendimientos como eventos esporádicos con tiempo de espera, para que sorprendan sin repetirse.

## 10. Secretos

Los secretos premian la curiosidad con información, atajos y detalles de mundo, no con poder: ninguno es necesario para terminar el POI. Todos tienen una pista dentro del mundo, sin marcadores.

| Secreto | Dónde | Pista dentro del mundo | Recompensa |
| --- | --- | --- | --- |
| Máquina de premios mecánica | P2-01 | Funciona sin electricidad; tres fichas están dispersas (juguetería, mostrador de premios, cajero) | Con las tres fichas suelta un ticket que se canjea en la juguetería por un peluche para el tren (cosmético) |
| Rollo de película del noticiero | P2-05, cabina | Un rollo puesto en el proyector apagado; se llega con la llave del cine | Mirando los fotogramas a contraluz en el pozo de luz se ve un mapa parcial de la Red Blanca y una mención a estaciones de control del clima |
| Ducto de ventilación | A-01 a P1-08 | Escarcha en línea sobre una pared, corriente de aire, reja con tornillos flojos | Ruta alternativa que evita la boca del derrumbe y la puerta de administración |
| Cámara de la brigada | Junto al túnel sur | Un nicho tapado con lona que aparece al despejar el túnel | Suministros sellados de la brigada y notas de evacuación |
| Cuaderno de frecuencias | A-02, caseta de la antena | Caseta con candado (cizalla) | Anota la frecuencia de una emisión automática; con el módulo receptor (K10) instalado en la sonda del tren, la sonda capta ese POI lejano |
| Monedas de la fuente | PB-02 | Reflejos metálicos bajo el hielo; se rompe con la barreta | Metal menor y una nota de deseos |
| Piscina congelada | P2-06 | Bajo el hielo se ve un bolso | Mochila con componentes y un diario; hielo fino |
| Bitácora del guardia | P1-08, cajón junto a los monitores apagados | Cuaderno con la última guardia | Narrativa del último día del refugio; anota la clave de la caja fuerte a medias (07\_\_) |
| Dibujos y notas | P2-04, Cine Sala 1 | Dibujos de chicos pegados a la pantalla | Sin recompensa de juego: cuentan la vida en el refugio |

## 11. Comportamiento del entorno

El edificio no tiene energía y no se puede restaurar: lo que cambia es la hora del día, las tormentas y lo que hace el jugador, y todo queda guardado. Las rutas se abren a mano y cada mecanismo hace ruido.

### Sin energía: cómo funciona cada sistema

| Sistema | Cómo funciona |
| --- | --- |
| Luz | Solo luz natural: pozo de luz del atrio, derrumbe, claraboya y grietas. El resto, linterna o fuego del jugador |
| Puertas y persianas | Todas mecánicas: llave, clave, barreta, manivela o volante |
| Montacargas | La cabina está trabada; el pozo es un atajo vertical con escalera de servicio, y con el cabrestante se suben cargas pesadas |
| Muelle y túnel | Se abren a mano: manivela y calor para la persiana; llave y volante para la compuerta |
| Calor | Solo el que enciende el jugador: garrafas, estufas y la cocina del Café |
| Sonido | Sin máquinas: viento, crujidos, goteo y el chirrido del sol giratorio |
| Generador | Muerto: no arranca; se desmonta por piezas |

### Ciclo de derretido y congelado

Cerca de la boca del derrumbe, la claraboya y la terraza, el sol derrite la nieve durante el día y de noche todo se congela de nuevo. Eso cambia la ruta de entrada según la hora.

| Momento | Efecto cerca de las aberturas |
| --- | --- |
| Mañana | Hielo duro, carámbanos nuevos, buen agarre en la nieve |
| Mediodía | Goteo y charcos, nieve más inestable en el derrumbe |
| Noche | Todo congelado, sin luz y con más frío; el hielo del piso resbala más |

### Tormentas y estructura

- Una tormenta aumenta la carga de nieve: más crujidos, nieve que cae por la claraboya y por la boca del derrumbe.
- Una tormenta fuerte puede tapar parcialmente la boca del derrumbe: hay que despejarla o entrar por la claraboya (propuesta).
- Con viento fuerte la fauna oye menos, así que es un buen momento para moverse (propuesta).
- Se puede esperar una tormenta dentro, en PB o en el Piso 1, usando los refugios cálidos.

### Física y ruido

Los objetos sueltos reaccionan: carritos que ruedan por la rampa, latas que ruedan, persianas que caen con estruendo, vidrio que se rompe y estantes que se desploman si se tira de ellos. Todo lo que hace ruido es ruido para la fauna, y ese vínculo es la base de las decisiones de sigilo.

### Persistencia

| Cambio | Se guarda como |
| --- | --- |
| Puertas y cerraduras | Cerrada, forzada o abierta |
| Loot tomado | Por contenedor, sin reaparecer |
| Mecanismos manuales | Persiana del muelle subida o no; compuerta del túnel abierta o no; cabrestante instalado en el pozo o no |
| Tanques | Nivel restante |
| Túnel y muelle | Cerrado o despejado |
| Fauna | Dormida, alerta o ausente (guarida abandonada) |
| Boca del derrumbe | Despejada o no; la nieve no se regenera sola (sección E.2: no se simulan años de acumulación) |
| Fuegos | Garrafas usadas y fuegos encendidos o apagados |
| Balizas y secretos | Colocadas o descubiertos |

## 12. Anexo para Claude Code: blockout y datos

El shopping queda fuera de la V1 (sección 21.3 del maestro), así que este anexo deja el plan listo para cuando llegue su turno, después del spike y del milestone M13. Todo lo de abajo es propuesta hasta que se apruebe.

### Niveles y cotas

| Nivel | Cota | Altura libre | Escena aditiva sugerida | Nota |
| --- | --- | --- | --- | --- |
| Azotea | +17 m | Parapeto de 1,2 m | shopping\_n3 | Claraboya hasta unos +19 m; cartel SOLANA hasta unos +29 m |
| Piso 2 | de +11 a +17 m | 5,5 m (cine 7 m) | shopping\_n2 | Ala oeste colapsada |
| Piso 1 | de +6 a +11 m | 4,5 m | shopping\_n1 | Pasarela sobre el atrio |
| Planta baja | de 0 a +6 m | 5,5 m | shopping\_n0 | El atrio la une con los pisos 1 y 2 |
| Subsuelo | de -4,5 a 0 m | 4,0 m | shopping\_s1 | Muelle y apartadero al sur |

Huella de 128 × 70 m, con el atrio de 40 × 26 m en el centro. La torre de estacionamiento es un volumen aparte, de 40 × 26 m y +52 m de alto, al este y unida al Piso 2 por una pasarela. La nieve exterior llega a unos +10 m al oeste y al sur, y a unos +5 m al este.

### Orden de blockout propuesto

1. Cáscara: huella, cotas y vacío del atrio en volúmenes simples. Prueba: caminar el perímetro de cada nivel.
2. Núcleos alineados: escalera de emergencia, montacargas y escalera mecánica. Prueba: recorrer del subsuelo a la azotea sin saltos de posición.
3. Subsuelo y planta baja con puertas y accesos. Prueba: llegar a cada sala por su ruta prevista.
4. Pisos 1 y 2, y azotea con la boca del derrumbe y la duna. Prueba: entrar por el derrumbe y bajar a la administración.
5. Sistemas: llaves, cerraduras, mecanismos manuales (manivela, volante y cabrestante) y persistencia. Prueba: completar la cadena de la sección 4 y recargar la partida.
6. Fauna: guarida, alerta y rutas. Prueba: despertar a los lobos con ruido y escapar.
7. Iluminación y audio por nivel, y recién después el kit modular y el vestido de las salas.

### Datos que deben ser configurables

Nombres ilustrativos, en línea con el Anexo B.3: ningún valor de balance escondido en scripts.

| Dato | Qué guarda | Ejemplo |
| --- | --- | --- |
| Cerraduras | Id, tipo, requisito, alternativa, ruido al forzar | S1-06: llave técnica; no se fuerza con la barreta |
| Pools de loot | Sala, pool y hallazgos excepcionales | PB-03: médico alto; armario con botiquín de brigada |
| Fauna | Grupo, guarida, disparadores, estado de alerta | Lobos: PB-08, cámara frigorífica |
| Mecanismos manuales | Qué pide cada uno y qué abre | Persiana del muelle: K6 y calor de K9; compuerta del túnel: K5 y volante; pozo: K4 y cabrestante |
| Cambios persistentes | Lo que se guarda | Tabla de la sección 11 |

### Presupuesto y streaming

Cada nivel es una celda de carga y de culling: se activa el del jugador y los contiguos. El atrio conecta tres niveles y es el espacio más caro, así que se divide en dos sectores con oclusión. Los interiores grandes son buenos candidatos a culling (sección 23). Nada se declara optimizado sin medirlo.

### Decisiones abiertas

- ¿El jugador empieza con linterna? El estado inicial solo menciona una barreta y un recipiente.
- ¿Hay osos en el POI? Aquí no se incluyen.
- ¿Cuánto ruido hacen la manivela, el volante y el cabrestante? Afecta a los lobos.
- ¿Las bengalas existen como distracción de fauna?
- ¿El túnel de la brigada y el muelle valen como atajos permanentes, o como proyectos más costosos?
- ¿RED BLANCA se mantiene como nombre propio en los carteles, o se traduce (por ejemplo, WHITE NETWORK)?

# Parte B · POIs grandes: catálogo, frecuencia y siluetas

En un mundo enterrado bajo la nieve solo se entra a estructuras grandes. Este documento fija qué tipos de POI hay, cada cuánto aparecen, para qué sirven y qué se encuentra en ellos, con Solana como el más grande.

## 1. Regla base y borde del mapa

La nieve es tan alta que enterró casi todo, así que en el juego solo se entra a estructuras grandes, y lo enterrado del todo queda fuera por ahora. El mapa termina en montañas nevadas que son fondo y decorado.

### Qué entra y qué queda afuera

- Un POI es una estructura grande que supera la nieve de su zona o asoma al menos en partes: un edificio, un complejo o una instalación.
- Lo enterrado del todo queda fuera del juego por ahora: no se excava ni se entra.
- Lo chico, como una casilla, un auto o un puesto, aparece como detalle dentro de un POI o como hallazgo en el camino, pero no es un destino (propuesta).
- Cada POI tiene un perfil de enterramiento (Anexo D.2 del maestro): cuánto asoma y por dónde se entra, casi siempre por arriba. El Mega es siempre el punto más alto de su región: ningún otro POI pasa de +42 m.

### Las montañas: el fin del mapa

Las montañas nevadas rodean el mapa y son de fondo y decorativas: no se cruzan, no tienen contenido y marcan hasta dónde llega cada región. Desde el tren son el telón contra el que se recortan los POI grandes.

- Función doble: borde del mapa y paisaje, con picos de silueta reconocible que ayudan a orientarse.
- El borde se explica sin muros invisibles: pendientes de nieve profunda, aludes y viento que frenan y congelan al jugador antes de llegar (propuesta).
- Se resuelven con tres capas de silueta (cercana, media y lejana) con niebla entre ellas, sin colisión detallada y con LOD bajo; la niebla también limita la distancia de dibujado (sección E.4 del maestro).
- No llevan POI: no hay estructuras en las montañas.

## 2. El mapa de una región

Una región es un valle de unos 45 km de vía entre montañas nevadas, con un Mega, 4 o 5 Grandes y 5 o 6 Medianos repartidos con mucho espacio vacío en el medio.

![Mapa de una región de ejemplo: la vía de la Red Blanca entre montañas nevadas, con diez POI de tres tamaños, la separación entre ellos y la curva de aproximación a Solana](img/pois_region.png)

La región de ejemplo tiene diez POI separados por 4 o 5 km (de 6 a 8 min de viaje). La estación del km 6,5 abre una bifurcación hacia una ruta sin explorar y Solana cae al km 28,5. La vía se curva al sur de Solana para que se vea de frente durante los últimos 2 km. Las montañas cierran la región al norte y al sur, y la vía entra y sale por los extremos hacia las regiones vecinas. Los símbolos no están a escala: un POI Grande de 140 m apenas se vería en este mapa.

## 3. Cada cuánto aparecen

Los POI son pocos y están muy separados: el maestro pide mucho espacio vacío entre señales, porque la soledad pierde fuerza si cada treinta segundos aparece otro edificio (sección 14.3). Los números de abajo suponen un tren de crucero a unos 40 km/h y una región de unos 45 km de vía entre montañas (propuesta).

| Tamaño | Huella y cuánto asoma sobre la nieve | Cada cuánto | Por región | Papel en el ritmo |
| --- | --- | --- | --- | --- |
| Mega | Más de 100 m de lado; asoma 25 m o más | Uno por región, entre el 55 y el 70% del recorrido | 1 | Destino mayor: pide una expedición larga |
| Grande | De 70 a 160 m de lado; asoma 15 m o más | Uno cada unos 10 km (15 min de viaje) | 4 o 5 | Base de recursos de la región |
| Mediano | De 40 a 100 m de lado; asoma 10 m o más | Uno cada unos 9 km (13 min), entre los grandes | 5 o 6 | Paradas de reposición y de tránsito |

### Reglas de ritmo

- Dos POI cualesquiera están separados por 3 km como mínimo (unos 4,5 min de viaje).
- Nunca hay dos Grandes seguidos: entre ellos va al menos un Mediano.
- El Mega no aparece antes de la mitad de la región y se anuncia con su silueta desde lejos.
- El primer POI de una partida es un Mediano cerca del inicio: en la V1, la estación y taller de la Red Blanca (sección 20 del maestro).
- Todo POI queda a entre 150 y 500 m a pie del lugar donde se detiene el tren (propuesta).

### Garantías para no quedarse sin recursos

- Combustible: al menos un POI con combustible (planta industrial, estación, cocheras o shopping) cada 2 Grandes.
- Comida y agua: al menos un POI con comida cada 3 POI seguidos.
- Medicina: al menos un POI médico por región (hospital, complejo de emergencias o shopping).
- Piezas especiales para los proyectos de vía: al menos una por región, en una estación, una planta industrial o unas cocheras.
- Materiales: todos los POI aportan algo; talleres y planta industrial aportan más metal y piezas.

## 4. Catálogo de POIs

Los diez tipos del pool del maestro (sección 15) pasan a versiones grandes: lo que era una comisaría, una escuela o un restaurante ahora es un complejo, un campus o una torre con restaurante, porque lo chico quedó enterrado. Cada región saca de este pool los tipos que le tocan según su semilla.

### Qué es y cómo se entra

| POI | Versión del maestro | Tamaño y silueta | Cómo se entra | Para qué sirve |
| --- | --- | --- | --- | --- |
| Shopping Solana (Mega) | Shopping | 128 × 70 m, +17 m; cartel de 10 m (+29 m) y torre de estacionamiento (+52 m) coronada por un sol giratorio (+60 m) | Derrumbe, claraboya o pasarela de la torre; salidas desde adentro | Logística completa: comida, medicina, materiales, electrónica, combustible y cama; proyectos |
| Hospital regional (Grande) | Hospital semienterrado | 90 × 40 m con torre de camas de 9 niveles (+32 m), helipuerto y cruz roja en la cima | Helipuerto y pisos altos; el subsuelo técnico queda enterrado | Medicina, telas, baterías y electrónica; planos médicos |
| Estación Red Blanca (Grande) | Estación ferroviaria | 140 × 30 m con nave de techo en arco (+18 m) y torre del reloj (+30 m) | Techo de la nave, torre del reloj y accesos de servicio | Repuestos, mapas, combustible y piezas de la Red Blanca; nudo de la red y proyectos de vía |
| Planta industrial (Grande) | Planta industrial | 120 × 80 m con naves de dientes de sierra (+16 m), tres chimeneas (+40 m) y torre de enfriamiento | Claraboyas de las naves y pasarelas altas | Combustible, bombas, motores y piezas especiales |
| Centro de distribución (Grande) | Supermercado | 160 × 70 m: galpón bajo (+15 m) con grúa pórtico y pilón de cartel (+30 m) | Claraboyas del techo; los muelles de carga están enterrados | Comida, agua, recipientes y almacenamiento; carga pesada |
| Hotel torre (Grande) | Hotel | 70 × 30 m con torre de 13 pisos (+38 m) y letras HOTEL en la cima | Balcones y azotea; los tres primeros pisos están enterrados | Colchones, telas, muebles y confort |
| Campus escolar (Grande) | Escuela | 140 × 70 m: edificios bajos (+12 m), gimnasio con domo (+20 m) y cuatro torres de luz del campo deportivo (+28 m) | Domo roto del gimnasio y gradas del campo | Madera, muebles, telas y comedor; mucho volumen recuperable |
| Complejo de emergencias (Mediano) | Comisaría | 70 × 40 m con torre de entrenamiento (+26 m) y antena (+35 m) | Balcones de la torre; el garaje de bomberos está enterrado | Linternas, radios, equipo resistente y lockers; sin arsenal |
| Cocheras y talleres (Mediano) | Taller mecánico | 100 × 50 m: naves de dientes de sierra (+16 m) y torre de agua (+30 m) | Tragaluces y torre de agua | Metal, aceite, piezas y herramientas; planos y componentes |
| Torre mirador con restaurante (Mediano) | Restaurante | Base de 40 × 40 m y torre de 42 m con restaurante giratorio a +32 m | Escalera de emergencia exterior hasta el restaurante; la base está enterrada | Comida, utensilios, gas y cocina; mirador con mapa parcial |

### Qué se encuentra

Se respeta la regla de loot del maestro: pools coherentes con lo que fue cada lugar y pocos hallazgos excepcionales que sorprendan.

| POI | Loot coherente | Hallazgos excepcionales | Amenaza |
| --- | --- | --- | --- |
| Shopping Solana | Farmacia, ferretería, supermercado, hogar, outdoor y electrónica | Plano de bomba de trasvase, cabrestante, módulo receptor, bolsa de dormir, colchón | Lobos, derrumbes, cinco niveles a oscuras |
| Hospital regional | Medicinas, vendas, telas, baterías, equipos | Plano de esterilizador, generador de emergencia | Oscuridad, zonas profundas, fauna opcional |
| Estación Red Blanca | Repuestos, mapas, combustible, señales de la Red Blanca | Actuador de cambios, mapa completo de la región | Andenes con hielo, estructuras inestables |
| Planta industrial | Bombas, motores, tanques, tuberías, aceite | Bomba industrial, motor grande para un proyecto del tren | Vapores, maquinaria, ruido |
| Centro de distribución | Comida, agua, recipientes, estantes, palets | Cámara frigorífica con comida congelada, contenedores sellados | Peso, estantes altos, fauna |
| Hotel torre | Colchones, sábanas, muebles, vajilla, minibar | Suite con caja fuerte, colchón premium | Pasillos oscuros repetidos, vértigo |
| Campus escolar | Madera, pupitres, telas, utensilios de comedor | Biblioteca con manuales (planos), laboratorio con componentes | Domo inestable, espacios abiertos |
| Complejo de emergencias | Linternas, radios, lockers, cuerdas, hachas | Camión de bomberos con escaleras y hachas, radio de largo alcance | Hielo en las rampas, trampas de nieve |
| Cocheras y talleres | Metal, aceite, piezas, herramientas | Locomotora desmontable, plano de mejora del tren | Fosos ocultos bajo el hielo |
| Torre mirador con restaurante | Vajilla, garrafas de gas, comida envasada | Telescopio con mapa, cocina industrial | Viento extremo, hielo, altura |

La matriz siguiente cruza cada POI con los recursos que aporta: más puntos, más cantidad.

![Matriz de los recursos que aporta cada POI: comida, agua, medicina, combustible, materiales, piezas, planos y mapas](img/pois_matriz.png)

## 5. Siluetas comparadas

Todos los POI, a la misma escala contra la nieve de +10 m, asoman entre 18 y 50 m, y cada uno se reconoce por su silueta antes de leer su nombre. El más alto es siempre Solana.

![Siluetas comparadas de los diez POI contra la línea de nieve: Solana es el punto más alto, con su torre y su sol giratorio; hay un tren y una persona como referencia de escala](img/pois_siluetas.png)

Cada silueta tiene su rasgo propio: cartel y torre de estacionamiento en Solana, helipuerto y cruz roja en el hospital, nave en arco y torre del reloj en la estación, chimeneas y torre de enfriamiento en la planta, grúa pórtico y pilón en el centro de distribución, letras HOTEL en la torre del hotel, domo y torres de luz en el campus, antena y torre de entrenamiento en emergencias, torre de agua en las cocheras y disco giratorio en la torre mirador.

## 6. Solana en el catálogo

Solana es el único Mega de este catálogo: el POI más grande, más alto y más valioso, y el modelo de cómo se ve un POI enterrado. El diseño completo está en la Parte A de este documento.

- Lo que lo distingue: cinco niveles, un cartel de 10 m (+29 m) y una torre de estacionamiento (+52 m) coronada por un sol giratorio (+60 m) como siluetas propias, el punto más alto de cualquier región (ningún otro POI pasa de +42 m), tres entradas desde arriba, dos salidas que se abren desde adentro, una cadena de llaves que se abre a mano, porque Solana no tiene energía, y el tono de alegría rota.
- Lo que solo tiene Solana: color saturado a gran escala, un sol que gira, un destello en la claraboya, el chirrido del sol con el viento, que se oye a distancia y un camino de farolas que guía desde la vía. Los otros POI usan una silueta propia y una paleta apagada, y nada más.
- Lo que comparte con el resto: la regla de enterramiento, un perfil de enterramiento, entrada por arriba, pools de loot coherentes, pocos hallazgos excepcionales y una silueta que se reconoce desde la vía.
- Dónde va: uno por región, entre el 55 y el 70% del recorrido. En la región de ejemplo está al km 28,5, donde la vía se curva al sur y las montañas del fondo bajan.
- Otros Mega posibles, como un aeropuerto o un estadio, quedan como decisión abierta.

## 7. Decisiones abiertas

- ¿La velocidad de crucero de 40 km/h y los 45 km por región son razonables? De eso dependen todos los espaciados.
- ¿Hay otros Mega además de Solana, como un aeropuerto, un estadio o un parque de diversiones con noria?
- ¿La V1 usa Medianos (cocheras, complejo de emergencias, torre mirador) en lugar de los POI pequeños de los milestones M6 y M11?
- ¿La nieve tiene la misma altura en todas las regiones, o cambia por región y entonces cambia qué asoma?
- ¿Cómo se conectan las regiones entre sí a través de las montañas: pasos, túneles o bifurcaciones?

# Parte C · Datos de partida (YAML)

Nombres ilustrativos para armar ScriptableObjects. Todo valor de balance va en datos configurables, no escondido en scripts (Anexo B.3 del maestro).

```yaml
reglas:
  energia_solana: ninguna            # no se puede restaurar
  idioma_carteleria: en              # nombres propios sin traducir
  nieve_cota_m: {oeste: 10, sur: 10, este: 5}
  tope_altura_otros_poi_m: 42

solana:
  huella_m: [128, 70]
  niveles:
    - {id: S1, nombre: Subsuelo, cota_m: [-4.5, 0]}
    - {id: PB, nombre: Planta baja, cota_m: [0, 6]}
    - {id: P1, nombre: Piso 1, cota_m: [6, 11]}
    - {id: P2, nombre: Piso 2, cota_m: [11, 17]}
    - {id: A, nombre: Azotea, cota_m: 17}
  hitos:
    cartel: {ancho_m: 80, alto_letras_m: 10, cota_tope_m: 29, mira_a: sur}
    torre_estacionamiento: {huella_m: [40, 26], niveles: 14, altura_nivel_m: 3.7, techo_m: 52, cima_sol_giratorio_m: 60, pasarela_en_nivel: 4}
  entradas:
    - {id: E1, tipo: entrada, donde: "boca del derrumbe, azotea oeste hacia P2"}
    - {id: E2, tipo: entrada, donde: "claraboya del atrio"}
    - {id: E3, tipo: salida, donde: "túnel de la brigada, PB", requiere: [K5, volante, T1]}
    - {id: E4, tipo: salida_de_carga, donde: "muelle, S1", requiere: [K6, K9]}
    - {id: E5, tipo: entrada, donde: "rampa de la torre, pasarela del nivel 4 hasta P2-03"}
  llaves:
    K1:  {nombre: "Llave ADM", en: "P2-06, casillero 31", abre: [L2]}
    K2:  {nombre: "Llave técnica", en: "P1-08, tablero (TECH)", abre: [L5, L6, L15]}
    K3:  {nombre: "Llave de farmacia", en: "P1-08, tablero (PHARM)", abre: [L3]}
    K4:  {nombre: "Llavero de servicio", en: "P1-08, escritorio", abre: [L8, L9, L11]}
    K5:  {nombre: "Llave de brigada", en: "P1-08, caja fuerte (L14)", abre: [L10]}
    K6:  {nombre: "Manivela de emergencia", en: "S1-05, jaula de mantenimiento", abre: [L7]}
    K7:  {nombre: "Llave del cine", en: "P2-03, boletería", abre: [L13]}
    K8:  {nombre: "Llave del armario de controlados", en: "PB-03, cartel de turno", abre: [L4]}
    K9:  {nombre: "Garrafa de gas", en: "PB-05, cocina", usa_en: [L7]}
    K10: {nombre: "Módulo receptor de radio", en: "P1-07", usa_en: [sonda_del_tren]}
    T1:  {nombre: "Pala", en: "PB-09", usa_en: [L10, boca_del_derrumbe]}
    T2:  {nombre: "Cizalla", en: "PB-09", abre: [L12, L16]}
    T3:  {nombre: "Kit mecánico", en: "PB-09", usa_en: [desmontar_generador, desmontar_autos]}
  cerraduras:
    L1:  {donde: "cortafuego escalera P2-P1", tipo: mecanica, requiere: [barreta], ruido: medio}
    L2:  {donde: "P1-08", tipo: mecanica, requiere: [K1], forzar_con: barreta, ruido_al_forzar: alto}
    L3:  {donde: "PB-03", tipo: mecanica, requiere: [K3], forzar_con: romper_vidrio, ruido_al_forzar: alto}
    L4:  {donde: "PB-03, armario", tipo: mecanica, requiere: [K8], forzar_con: barreta, ruido_al_forzar: medio}
    L5:  {donde: "S1-06", tipo: mecanica, requiere: [K2], forzar_con: amoladora, ruido_al_forzar: muy_alto}
    L6:  {donde: "S1-07", tipo: mecanica, requiere: [K2], forzar_con: barreta, ruido_al_forzar: medio}
    L7:  {donde: "S1-04, persiana", tipo: manual, requiere: [K6, K9], ruido: alto}
    L8:  {donde: "S1-05", tipo: mecanica, requiere: [K4], forzar_con: barreta, ruido_al_forzar: alto}
    L9:  {donde: "pozo del montacargas", tipo: mecanica, requiere: [K4], forzar_con: barreta, ruido_al_forzar: alto}
    L10: {donde: "PB-01, compuerta", tipo: manual, requiere: [K5, volante, T1], ruido: medio}
    L11: {donde: "PB-09, bodega", tipo: mecanica, requiere: [K4], forzar_con: barreta, ruido_al_forzar: alto}
    L12: {donde: "P1-09", tipo: mecanica, requiere: [T2], forzar_con: barreta, ruido_al_forzar: alto}
    L13: {donde: "P2-05, cabina", tipo: mecanica, requiere: [K7], forzar_con: barreta, ruido_al_forzar: medio}
    L14: {donde: "P1-08, caja fuerte", tipo: clave, clave: "0731", forzar_con: amoladora, ruido_al_forzar: muy_alto}
    L15: {donde: "A-01", tipo: mecanica, requiere: [K2], forzar_con: barreta, ruido_al_forzar: medio}
    L16: {donde: "A-02, caseta", tipo: mecanica, requiere: [T2], forzar_con: barreta, ruido_al_forzar: medio}
  fauna:
    - {grupo: lobos, cantidad: 3, guarida: "PB-08, cámara frigorífica", estados: [dormido, inquieto, buscando, persiguiendo]}
    - {grupo: zorros, cantidad: 2, guarida: "PB-03 trastienda y PB-06", agresivo: false}
    - {grupo: cuervos, zona: "azotea, terraza sur y techo de la torre", agresivo: false}

pois_grandes:
  region: {via_km: 45, velocidad_tren_kmh: 40, separacion_minima_km: 3, a_pie_desde_la_parada_m: [150, 500]}
  tamanos:
    mega:    {lado_m: ">100", asoma_minimo_m: 25, por_region: 1}
    grande:  {lado_m: "70-160", asoma_minimo_m: 15, por_region: "4-5", cada_km: 10}
    mediano: {lado_m: "40-100", asoma_minimo_m: 10, por_region: "5-6", cada_km: 9}
  tipos:
    - {id: shopping_solana, tamano: mega, alto_m: 60}
    - {id: hospital_regional, tamano: grande, alto_m: 32}
    - {id: estacion_red_blanca, tamano: grande, alto_m: 30}
    - {id: planta_industrial, tamano: grande, alto_m: 40}
    - {id: centro_de_distribucion, tamano: grande, alto_m: 30}
    - {id: hotel_torre, tamano: grande, alto_m: 38}
    - {id: campus_escolar, tamano: grande, alto_m: 28}
    - {id: complejo_de_emergencias, tamano: mediano, alto_m: 35}
    - {id: cocheras_y_talleres, tamano: mediano, alto_m: 30}
    - {id: torre_mirador, tamano: mediano, alto_m: 42}
```
