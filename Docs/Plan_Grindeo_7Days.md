# Plan: grindeo a lo 7 Days to Die para el tren

Lo pidió Tomi el 2026-10-16, después de jugar la slice:
- "No me convence cómo se craftea, tampoco cómo se consiguen los materiales."
- "Grindeo a lo 7 Days, de picar."
- "Más elementos de grindeo para tu tren."
- "Le falta propósito hoy al game."

Este documento es la **Parte 2** del plan aprobado. La Parte 1 (interfaz, campo de visión, tren más grande, combustible y ritmo) va antes, y la **Parte 1b** (la nieve del piso sin repetición) también.

**La lista completa** (materiales, zonas con montañas, herramientas, la forja paso a paso, recetas, partes del tren, escalera de objetivos, sistemas, arte y sonido) está en `Docs/Grindeo_Lista_Completa.md`.

**Decisiones de Tomi del 2026-10-16:**
- **Picar el mundo implica montañas.** La cordillera alrededor de la región y las rocas al pie entran en G1.
- **La forja tiene cola para fundir** y después **se le pega en el yunque** para las piezas.

**La base no cambia:** el tren, el manejo, la supervivencia, las sondas, los lugares, el guardado y la forja. Cambia **de dónde salen los materiales**, **cómo se fabrica** y **para qué sirve todo eso**: el tren pasa a ser tu base, que se mejora a martillazos.

## La idea en una línea
Salir con el tren, **picar** lo que hay (árboles muertos, rocas, autos, chatarra, edificios), **fundir** en la forja, **fabricar** desde la mochila y **subir de nivel cada parte del tren** a martillazos, para aguantar más frío, ir más lejos y abrir rutas bloqueadas que llevan a lugares con mejores materiales.

## Qué respeta del documento maestro
- **Las mejoras grandes son físicas** (sección 2: "no un menú de +15%"). Cada nivel de una parte del tren se ve, adentro y afuera.
- **Los recursos tienen un origen que se entiende** (9.3). La madera sale de árboles y muebles, el hierro de rocas con mineral y de chatarra, la tela de colchones y ropa.
- **Hay vacío entre lugares** (sección 2). Los recursos van en zonas (bosque muerto, cantera, chatarrería, cementerio de vagones, pueblo), con nieve vacía entre medio. No es un mundo lleno como el de 7 Days.
- **No es un shooter.** El antagonista sigue siendo el frío y la logística.

## Los pasos

### G1. Picar el mundo (con las montañas)
- **Las montañas**: terreno más grande (unos 6 km) con una cordillera alrededor de la región, roca donde la pendiente es fuerte, y la cantera y el pie de la montaña como zonas de piedra, mineral de hierro y carbón.
- Un componente nuevo, "picable", para todo lo que se rompe a golpes. Lo que hoy se desarma a golpes (pallets, armarios, colchones, tableros, chatarra) pasa a este sistema.
- Cada cosa tiene "vida" en golpes, de qué está hecha y qué herramienta le sirve. **Cada golpe da un poco de material directo a la mochila** y al romperla sale el resto. Se ve un número y se escucha el material: madera, metal o piedra.
- Herramienta equivocada: pega poco o rebota, igual que ahora.
- Cosas nuevas para picar (con medidas y arte simple, a reemplazar por el chat de arte):

  | Fuente | Da | Herramienta |
  |---|---|---|
  | Árboles muertos y troncos caídos | madera | hacha |
  | Rocas | piedra, y a veces mineral de hierro o carbón | pico |
  | Autos y camionetas | metal, piezas mecánicas, cables y combustible | llave o barreta |
  | Postes y señales de la vía | metal y cables | barreta o llave |
  | Muebles y paredes de los lugares | tablas, chapa, tela | barreta o martillo |

- **Grindeo a lo 7 Days**: muchos golpes por cosa y poco por golpe. Los valores van en datos para ajustar jugando.
- Prueba: picar cada tipo, con la herramienta buena y la mala, y que el total sea el esperado, sin duplicados.

### G2. Herramientas por niveles y crafteo desde la mochila
- **Herramientas en tres niveles**: improvisada (piedra y palo), de hierro (con planchuela) y de forja. Cada nivel pica más rápido y saca más.
  - Hacha, pico, barreta, martillo y llave.
  - La sierra sigue como herramienta de banco.
- **Crafteo desde la mochila**, con **una cola de fabricación que avanza mientras jugás**, como en 7 Days. Las recetas simples están desde el principio, sin planos: tablas, herramientas improvisadas, vendas, antorchas...
- **El banco del tren** fabrica lo avanzado: herramientas de hierro, partes del tren y muebles. Usa la misma cola y lo fabricado aparece en el banco o en la mochila. Se acaban los kits para cargar.
- **Los planos** quedan solo para lo especial: rifle, piezas raras, mejoras grandes del motor. El documento pide no bloquear con planos lo trivial.
- Prueba: fabricar con la cola mientras se camina, cancelar y que devuelva los materiales, y guardar y cargar con la cola a medias.

### G3. La forja: cola para fundir y yunque para las piezas (decisión de Tomi)
- Cómo se arma (hogar de piedra, fuelle, chimenea y yunque de riel o de verdad) y cómo funciona: `Docs/Grindeo_Lista_Completa.md`, sección 5.3.
- La forja del tren gana **fundir**:
  - mineral de hierro con carbón da hierro;
  - chatarra da hierro, pero rinde menos.

  Tiene su propia cola y su propio combustible: leña, carbón o el combustible del tren. Mientras trabaja, calienta el vagón (como ya hace).
- **Las piezas** (planchuela, clavos, varilla, cabezas de herramienta, bisagras) se hacen **a martillazos en el yunque**, con la barra al rojo. Si se enfría, vuelve al fuego.
- Prueba: fundir con cada combustible y comprobar el rendimiento y el calor.

### G4. El tren como tu base: partes que suben de nivel a martillazos
- Cada parte del tren tiene **niveles**. Para subir uno hay que tener los materiales y **pegarle con el martillo**, como subir un bloque en 7 Days. El nivel nuevo se ve.

  | Parte | Niveles (propuesta) | Qué cambia |
  |---|---|---|
  | Paredes | chapa sola → aislada (tela y madera) → reforzada (planchuelas) → blindada | cuánto frío entra |
  | Estufa | lata → estufa de hierro → caldera | cuánto combustible gasta para el mismo calor |
  | Tanque | 200 L → 350 L → 500 L | cuánto viaje aguanta |
  | Motor | gastado → reparado → ajustado | velocidad máxima y consumo |
  | Quitanieves | ninguno → de chapa → reforzado | qué nieve acumulada atraviesa en las vías |
  | Estantes | improvisado → reforzado → grande | lugar para guardar |
  | Cama | colchón en el piso → cama → cama aislada | cuánto descansa |
  | Luces | ninguna → lámparas → reflector | ver de noche y afuera |
  | Ventanas | vidrio roto → tapado → doble | frío y escarcha |

- Lo de hoy pasa a este sistema: la cama por fases y el estante reforzado son partes con niveles, y la forja también se arma por niveles. Sin fantasmas azules ni kits.
- El martillo también **repara**: más adelante, las partes se gastan con el uso y el frío. Ver la pregunta 3.
- Prueba: subir cada parte, que el efecto se mida (frío, consumo, velocidad, lugar) y guardar y cargar.

### G5. El propósito: rutas que piden mejoras
- La red ferroviaria se abre por **logística**, como pide el documento en la sección 6.3:
  - **nieve acumulada** que solo pasa con el quitanieves de cierto nivel;
  - un **puente roto** que se repara con planchuelas y vigas;
  - **tramos largos** que piden un tanque más grande;
  - **zonas más frías** que piden paredes aisladas.
- Cada ruta lleva a **lugares con mejores materiales**: los POIs medianos que ya decidiste (estación y taller de la WHITE NETWORK, Cocheras y talleres, Torre mirador) y después Solana.
- La consola de la sonda muestra qué pide cada ruta. Así cada salida tiene un porqué: "me faltan 20 de hierro para el quitanieves".
- Prueba: el ciclo completo (picar, fundir, mejorar y abrir una ruta) de punta a punta.

### G6. Balance y regresión
- Una partida de prueba larga con mediciones: cuánto tarda cada mejora y cuánto combustible y comida se usa por día.
- Regresión completa y documento de resultados.

## Lo que queda afuera por ahora
- Zombis y enemigos: el documento dice que no es un shooter.
- Fauna, rifle y animales.
- Habilidades o puntos de experiencia como los de 7 Days. Se puede pensar después.
- Mundo procedural y vagones nuevos (los vagones serían el paso siguiente).
- El arte final de cada nivel: Claude hace versiones simples y se arma una ficha para el chat de arte.

## Preguntas para Tomi (las que siguen abiertas también están en `Docs/Grindeo_Lista_Completa.md`, sección 13)
1. **Inventario:** ¿la cuadrícula estilo Tarkov se queda, pero con **pilas** (por ejemplo, hasta 50 de madera en una casilla)? Con tanto material, ir pieza por pieza se vuelve tedioso. Propuesta: sí, con pilas.
2. ~~**La forja**~~: respondida (2026-10-16): cola para fundir y yunque para las piezas.
3. **Desgaste:** ¿las herramientas y las partes del tren se gastan y hay que repararlas? En 7 Days sí. Propuesta: las herramientas sí, y las partes del tren solo con golpes o accidentes.
4. **Orden:** ¿arranco por G1 y G2 juntos (picar más crafteo), que es lo que más cambia la sensación del juego?
