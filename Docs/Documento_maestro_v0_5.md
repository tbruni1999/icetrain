Versión 0.5 · 12 de octubre de 2026

Actualización v0.5: hoja de ruta unificada entre la sección 24 y el Anexo G, con los milestones de inventario, desmontaje y mini-región que faltaban, y correcciones menores.

| **PRINCIPIO** | Este archivo sustituye como fuente de verdad de diseño a las versiones 0.1 a 0.4. Conserva el concepto original, pero incorpora las decisiones posteriores de la conversación. Si una idea antigua entra en conflicto con una DECISIÓN de este documento, prevalece este documento. |
|---------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

**OBJETIVO DEL DOCUMENTO**

Servir como especificación de producto y guía de implementación para construir una primera versión jugable real. No es un guion cerrado ni una biblia narrativa definitiva: separa explícitamente lo decidido, lo propuesto, los riesgos técnicos y las preguntas abiertas.

**ESTADO ACTUAL**

El concepto ya es suficientemente sólido para pasar de ideación a prototipo. La prioridad deja de ser sumar sistemas y pasa a validar el loop: vivir en el tren, detectar una oportunidad, salir, recuperar algo útil, volver, transformar físicamente el refugio y abrir la posibilidad de ir más lejos.

**FANTASÍA CENTRAL**

“Transformar gradualmente un tren miserable en un hogar autosuficiente mientras atravesamos un mundo enterrado bajo la nieve.”

# 0. Cómo leer y usar este documento

Las etiquetas siguientes indican el nivel de compromiso de cada idea. Claude debe respetarlas al implementar.

| **Etiqueta**            | **Significado**                                                              |
|-------------------------|------------------------------------------------------------------------------|
| DECISIÓN                | Parte actual del diseño. No cambiar sin una instrucción posterior explícita. |
| PRINCIPIO               | Regla de diseño que sirve para resolver casos nuevos y evitar incoherencias. |
| PROPUESTA               | Dirección preferida pero aún ajustable después de pruebas.                   |
| RIESGO                  | Problema técnico o de diseño que requiere validación temprana.               |
| ALCANCE V1              | Debe existir en la primera vertical slice o prototipo jugable.               |
| INSTRUCCIÓN PARA CLAUDE | Regla de trabajo/arquitectura del proyecto.                                  |

| **INSTRUCCIÓN PARA CLAUDE** | Claude no debe convertir propuestas en requisitos por su cuenta, ni ampliar el alcance con sistemas no pedidos. Primero se valida el loop central. |
|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|

## Mapa del documento

- 1\. High concept y pilares

- 2\. No-objetivos y límites

- 3\. Mundo, catástrofe y Red Blanca

- 4\. Personaje y situación inicial

- 5\. Loop central y ritmo

- 6\. Tren, conducción y red ferroviaria

- 7\. Supervivencia: vida, hambre, energía y temperatura

- 8\. Combustible, calor y agua

- 9\. Recursos, desmontaje y logística

- 10\. Workbench, herramientas y blueprints

- 11\. Construcción y restauración física

- 12\. Vagones y especialización

- 13\. Sondas, orientación y balizas

- 14\. Mundo procedural híbrido

- 15\. POIs iniciales

- 16\. Amenazas, fauna y animales recuperables

- 17\. Rifle y combate limitado

- 18\. UI/UX y lenguaje visual

- 19\. Dirección artística y audio

- 20\. Primeros 30–45 minutos

- 21\. V1 / vertical slice

- 22\. Arquitectura técnica propuesta

- 23\. Optimización y rendimiento

- 24\. Orden de implementación

- 25\. Pruebas de aceptación

- 26\. Reglas de trabajo para Claude

- 27\. Decisiones abiertas y backlog

- 28\. Glosario de diseño

# 1. High concept y pilares

Survival para un jugador en primera persona, ambientado en una Tierra cubierta por nieve después de un enfriamiento global prolongado. El jugador habita y conduce un tren de mantenimiento superviviente de una antigua red ferroviaria de emergencia. El tren funciona simultáneamente como vehículo, refugio, base, inventario físico, sistema de progresión y registro visual de la partida.

| **DECISIÓN** | El tren puede detenerse. Su movimiento y su calefacción dependen del combustible. Se abandona la regla antigua de “tren siempre en movimiento”. |
|--------------|-------------------------------------------------------------------------------------------------------------------------------------------------|

## Pilares de experiencia

| **Pilar**                 | **Implicación de diseño**                                                                                                                |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| Soledad                   | El inicio no depende de NPC, comerciantes o conversaciones. El mundo se cuenta mediante rastros, infraestructura y espacios abandonados. |
| Tren como hogar           | Toda mejora importante debe sentirse dentro del tren: luz, calor, orden, mobiliario, capacidades y nuevos vagones.                       |
| Exploración con propósito | Se sale porque falta algo, porque la sonda detectó algo o porque existe una meta logística concreta.                                     |
| Progresión física         | Las cosas no “suben de nivel” de manera abstracta: aparecen, se reparan, se instalan y cambian físicamente.                              |
| Supervivencia legible     | Pocos sistemas básicos, fáciles de comprender y con feedback visual/sonoro fuerte.                                                       |
| Tecnología recuperada     | Workbench, blueprints, herramientas y piezas reutilizadas amplían capacidades sin convertir el juego en un árbol de perks.               |
| Atmósfera antes que AAA   | La inmersión nace de iluminación, materiales, nieve, sonido, escala, clima y contraste interior/exterior.                                |
| Mundo con vacío           | La ausencia de cosas también es contenido. Los POIs deben sentirse importantes porque existen largos tramos de silencio y nieve.         |

## Promesa al jugador

Cada expedición debería dejar al menos una consecuencia legible: más combustible, una parte del tren mejorada, una nueva herramienta, un blueprint, un objeto físico recuperado, una fase de restauración avanzada o acceso potencial a una nueva ruta.

# 2. No-objetivos y límites

| **DECISIÓN** | El juego NO debe convertirse en un shooter. El combate es excepcional y peligroso. |
|--------------|------------------------------------------------------------------------------------|

- No buscar un arsenal de armas. La progresión bélica gira alrededor de un rifle rudimentario único.

- No convertir la construcción en un menú de “+15% estadísticas”. Las mejoras grandes deben ser físicas.

- No llenar el HUD de texto, números y tutoriales permanentes.

- No generar edificios enteros proceduralmente pieza por pieza si eso destruye su identidad.

- No colocar un POI cada pocos metros. El vacío y la espera son parte del tono.

- No convertir animales recuperables en una granja masiva o en un juego de administración ganadera.

- No usar blueprints para bloquear conocimientos triviales que una persona razonable podría improvisar.

- No perseguir gráficos AAA. El objetivo es “realismo atmosférico indie” con rendimiento alto.

# 3. Mundo, catástrofe y Red Blanca

## 3.1 La catástrofe: “El Enfriamiento”

Base narrativa provisional: antes del colapso, la humanidad enfrentaba un calentamiento climático severo. Se desplegó un sistema de geoingeniería atmosférica destinado a reflejar una fracción de la radiación solar. La intervención funcionó, pero el sistema se mantuvo activo o fuera de control demasiado tiempo. El descenso térmico empezó a retroalimentarse: más nieve y hielo implicaban más radiación reflejada y, por tanto, menos calentamiento de la superficie.

| **DECISIÓN** | El Sol sigue existiendo y hay ciclo de día/noche. El mundo no está oscuro de forma permanente. El contraste de un Sol visible sobre un paisaje mortalmente frío es deseable. |
|--------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

La explicación exacta, la escala científica y quién fue responsable pueden refinarse después. El objetivo narrativo no es impartir una clase de climatología, sino sostener un mundo donde el enfriamiento ocurrió durante años y la civilización tuvo tiempo de intentar adaptarse.

## 3.2 Cronología conceptual

| **Fase**          | **Qué ocurre**                                                                                       | **Consecuencia jugable/visual**                                                                          |
|-------------------|------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------|
| 1\. La solución   | Se activa la geoingeniería para frenar el calentamiento.                                             | Infraestructura técnica y documentación que originalmente hablaban de “control climático”.               |
| 2\. El problema   | Los inviernos se alargan y las nevadas dejan de ser estacionales.                                    | Primeras adaptaciones urbanas y ferroviarias.                                                            |
| 3\. La adaptación | Carreteras y aeropuertos se vuelven poco fiables. El ferrocarril pasa a ser infraestructura crítica. | Nace/refuerza la Red Blanca: corredores elevados, talleres, depósitos, calefacción de cambios, refugios. |
| 4\. El colapso    | No se logra sostener toda la infraestructura. Se abandonan rutas, ciudades y regiones.               | La red queda fragmentada; algunos tramos sobreviven, otros están congelados, rotos o enterrados.         |
| 5\. Presente      | El jugador utiliza restos de esa infraestructura para seguir moviéndose.                             | El mundo se descubre a través de rutas, estaciones y proyectos que quedaron incompletos.                 |

## 3.3 La Red Blanca

“Red Blanca” es el nombre de trabajo para la red ferroviaria reforzada durante la fase de adaptación. No todas las vías son nuevas: muchos corredores existentes fueron elevados, reforzados o equipados para inviernos extremos.

- Terraplenes y viaductos para quedar por encima de acumulaciones normales.

- Barreras contra nieve y zonas expuestas al viento donde la acumulación es menor.

- Cambios con sistemas de calefacción o actuadores eléctricos.

- Estaciones técnicas, depósitos, talleres y refugios a intervalos estratégicos.

- Rutas de evacuación, mantenimiento, combustible y suministro.

| **DECISIÓN** | Esto explica por qué pueden existir edificios semienterrados mientras ciertos tramos ferroviarios siguen expuestos. Las vías más importantes fueron adaptadas y elevadas; otras estarán enterradas o dañadas. |
|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

## 3.4 Narrativa ambiental

La historia debe descubrirse por capas. El jugador no recibe un monólogo inicial explicando el planeta. Primero entiende su problema inmediato; luego descubre la Red Blanca; mucho más tarde puede inferir que algunas instalaciones climáticas quizá sigan activas.

- Carteles de evacuación y mantenimiento.

- Mapas parciales de la red.

- Manuales técnicos y registros de estaciones.

- Terminales, notas y diagramas de infraestructura.

- POIs donde las últimas adaptaciones humanas quedaron a medio terminar.

| **PROPUESTA** | Posible arco mayor: descubrir si parte de la infraestructura climática sigue operativa y si existe alguna forma de alterar el estado del mundo. No es requisito de V1. |
|---------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

# 4. Personaje y situación inicial

| **DECISIÓN** | Evitar el cliché de amnesia. El personaje ya lleva un tiempo sobreviviendo y conoce nociones básicas de su equipo y del tren. |
|--------------|-------------------------------------------------------------------------------------------------------------------------------|

Origen provisional: el personaje estuvo relacionado con una evacuación o convoy ferroviario durante el colapso. El convoy se fragmentó con el tiempo por daños, tormentas, falta de combustible y abandonos. Terminó utilizando una locomotora/vagón de mantenimiento de la Red Blanca como refugio.

## Lo que esto justifica

- Sabe conducir de forma básica el tren.

- Conoce blueprints elementales e improvisaciones de supervivencia.

- Posee una sonda básica y herramientas mínimas.

- Comprende que la red ferroviaria puede abrir o cerrar rutas.

- No posee una explicación completa del Enfriamiento ni sabe qué ocurrió con todas las personas.

## Estado al comenzar

| **Sistema**    | **Estado inicial**                                                                      |
|----------------|-----------------------------------------------------------------------------------------|
| Refugio        | Un único espacio habitable precario, frío, oscuro y desordenado.                        |
| Cama           | Colchón/tela sudada, harapienta y poco aislada; descanso deficiente.                    |
| Almacenamiento | Un estante o cajas muy limitadas.                                                       |
| Workbench      | Mesa improvisada con capacidad de reparaciones simples.                                 |
| Combustible    | Reserva baja: suficiente para no morir de inmediato, insuficiente para sentirse seguro. |
| Comida         | Pocas raciones básicas.                                                                 |
| Agua           | Reserva mínima y sistema de derretimiento/recolección todavía rudimentario.             |
| Sonda          | Consola del tren básica + sonda portátil básica.                                        |
| Herramientas   | Barreta/herramienta manual y un recipiente/bidón.                                       |
| Arma           | Ninguna.                                                                                |

# 5. Loop central y ritmo

1.  Viajar o permanecer detenido mientras se revisa el estado del tren y las necesidades inmediatas.

2.  Detectar una oportunidad o problema: combustible, comida, recursos, blueprint, pieza especial, animal, nueva ruta.

3.  Preparar equipo: herramienta, traje, sonda portátil, bidones, comida, espacio de carga y, más adelante, balizas.

4.  Bajar y explorar un POI o un punto del terreno.

5.  Resolver obstáculos físicos, administrar temperatura/energía/peso y decidir cuándo abandonar.

6.  Recuperar materiales u objetos enteros y regresar al tren.

7.  Descargar, repostar, comer, recuperar calor y descansar.

8.  Instalar materiales, fabricar una herramienta o avanzar una restauración visible.

9.  Tomar una nueva decisión de ruta o expedición con capacidades ligeramente mejores.

| **PRINCIPIO** | La tensión ideal alterna expediciones hostiles con momentos tranquilos dentro del tren. La calma no es downtime inútil: ordenar, cocinar, reparar, construir y mirar el mundo forman parte de la experiencia. |
|---------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

# 6. Tren, conducción y red ferroviaria

## 6.1 Controles del tren

El jugador no “gira” el tren como un vehículo. La locomotora sigue la vía. La conducción debe ser entendible en menos de un minuto.

| **Control**           | **Función**                                                           |
|-----------------------|-----------------------------------------------------------------------|
| Reversor              | Define avance / neutro / retroceso.                                   |
| Potencia              | Pocas posiciones o rango simple para controlar tracción.              |
| Freno                 | Reduce velocidad y permite detenerse con precisión.                   |
| Controles secundarios | Luces, bocina, calefacción y otros sistemas físicos según progresión. |

| **DECISIÓN** | La dirección lateral se decide mediante cambios de vía. No hay steering libre. |
|--------------|--------------------------------------------------------------------------------|

## 6.2 Cambios de vía

Los cambios son objetos físicos del mundo y parte del gameplay. Al principio muchos deben operarse manualmente: frenar, bajar, accionar una palanca y volver al tren.

### Progresión posible

- Inicio: palancas manuales y cambios que exigen bajarse.

- Midgame: actuadores eléctricos fabricados/instalados en cambios importantes.

- Late game: control remoto de cambios compatibles desde la consola del tren.

## 6.3 Bloqueos ferroviarios como gating natural

La red debe contener “trancas” físicas que funcionen como pequeños rompecabezas o proyectos de reparación. No son puertas de nivel abstractas: son infraestructura dañada.

| **Bloqueo**                  | **Respuesta del jugador**                                    |
|------------------------------|--------------------------------------------------------------|
| Cambio congelado             | Quitar hielo, aplicar calor o usar una herramienta adecuada. |
| Palanca rota                 | Sustituir piezas y metal.                                    |
| Cambio eléctrico sin energía | Conectar batería/generador o reparar cableado.               |
| Nieve acumulada              | Limpiar manualmente o usar equipo posterior.                 |
| Riel dañado                  | Proyecto de reparación con metal, durmientes y componentes.  |
| Túnel/puente comprometido    | Proyecto de mayor escala o ruta temporalmente inaccesible.   |

| **DECISIÓN** | Abrir rutas nuevas debe requerir reservas reales. Algunas reparaciones son proyectos logísticos: el jugador tiene que haber acumulado una cantidad significativa de recursos y una pieza especial. |
|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

### Ejemplo de proyecto de ruta

| **Recurso**       | **Ejemplo de requisito (placeholder de balance)** |
|-------------------|---------------------------------------------------|
| Metal             | Cantidad alta                                     |
| Madera/durmientes | Cantidad media                                    |
| Componentes       | Varios                                            |
| Combustible       | Reserva operativa para la obra/viaje              |
| Pieza especial    | 1 componente ferroviario específico               |

Los números definitivos se balancean después. Lo importante es la estructura de decisión: usar recursos en mejorar la casa ahora o guardarlos para abrir una región nueva.

# 7. Supervivencia: vida, hambre, energía y temperatura

| **DECISIÓN** | El HUD básico debe girar alrededor de cuatro estados: vida, hambre, energía y temperatura. Mantener el sistema legible y evitar demasiadas barras. |
|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------|

## Vida

Representa lesiones: caídas, hipotermia avanzada, accidentes, ataques de fauna, quemaduras u otros daños. No debería regenerarse mágicamente de forma rápida. Estar alimentado, caliente y bien descansado puede favorecer una recuperación lenta.

## Hambre

Baja con el tiempo de forma relativamente lenta. El hambre temprana afecta energía y recuperación antes de convertirse en daño directo. Evitar el comportamiento caricaturesco de “comí hace diez minutos y estoy muriendo”.

## Energía

Representa descanso y estado general, no la stamina inmediata de correr. Baja con el tiempo y más rápido por frío, trabajo físico, hambre y expediciones intensas. Dormir es el medio principal de recuperación.

| **PROPUESTA** | Puede existir una stamina corta separada que se recupere sola. No debe confundirse con la barra de energía/sueño. |
|---------------|-------------------------------------------------------------------------------------------------------------------|

## Temperatura

El exterior es el gran reloj de las expediciones. El traje, el viento, el clima, el aislamiento y el refugio modifican la pérdida térmica. El feedback debe ser audiovisual: respiración, condensación, escarcha, sonido y cambios de interfaz antes de que aparezcan penalizaciones graves.

## La cama como ejemplo de supervivencia física

| **Estado**               | **Visual**                                       | **Efecto conceptual**                            |
|--------------------------|--------------------------------------------------|--------------------------------------------------|
| Lugar miserable          | Tela/colchón sucio en el piso.                   | Recuperación de energía deficiente.              |
| Cama improvisada         | Base de madera + relleno + manta.                | Recuperación claramente mejor.                   |
| Cama real                | Estructura, buen colchón, almohada y abrigo.     | Recuperación completa o casi completa.           |
| Dormitorio acondicionado | Aislamiento, calor, luz cálida y almacenamiento. | Mismo descanso en menos tiempo o bonus moderado. |

| **PRINCIPIO** | La comodidad del tren tiene impacto funcional. Mejorar el hogar mejora la capacidad del jugador para sobrevivir, no solo la estética. |
|---------------|---------------------------------------------------------------------------------------------------------------------------------------|

# 8. Combustible, calor y agua

## 8.1 Combustible

| **DECISIÓN** | El combustible alimenta movimiento y calefacción principal. Nunca debe existir generación infinita dentro del tren: seguir buscándolo mantiene viva la exploración. |
|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------|

La recolección debe evolucionar desde trabajo manual hacia infraestructura más eficiente. La progresión mecánica debe sentirse tan clara como la progresión visual del tren.

| **Etapa**                    | **Mecánica**                                                                    | **Lectura visual**                                           |
|------------------------------|---------------------------------------------------------------------------------|--------------------------------------------------------------|
| 1\. Manual                   | Bidón + sifón/manguera; extracción de vehículos, tanques pequeños o maquinaria. | Bidones sueltos, manguera sencilla.                          |
| 2\. Bomba manual             | Bomba de palanca para tambores y depósitos.                                     | Herramienta física que el jugador opera.                     |
| 3\. Bomba eléctrica portátil | Workbench + batería; trasvase rápido.                                           | Equipo reutilizable que ocupa espacio/carga.                 |
| 4\. Sistema del tren         | Manguera y bomba instaladas en un vagón/locomotora.                             | Tanques, tuberías, medidores y carrete de manguera visibles. |
| 5\. Infraestructura externa  | Reactivar estaciones de repostaje o instalar extractores.                       | El mundo conserva infraestructura creada por el jugador.     |

## 8.2 Indicios de combustible en el mundo

Pueden aparecer manchas oscuras, restos aceitosos, respiraderos o señales de depósitos bajo la nieve. Investigarlos revela tuberías, cisternas, tanques enterrados o instalaciones de repostaje.

| **DECISIÓN** | Los puntos de combustible pueden generarse proceduralmente. Al principio se explotan manualmente; más adelante algunos admiten una bomba automática/semiautomática instalada por el jugador. |
|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

## 8.3 Agua

El calor residual del tren derrite nieve. El jugador puede capturar esa agua con un sistema físicamente mejorable: canaleta improvisada → recipientes → tanque → filtro → bomba/distribución.

- Uso inicial: beber/sobrevivir.

- Usos posteriores: cocina, higiene, cultivo, animales y otros sistemas.

| **PROPUESTA** | La hidratación exacta como barra separada sigue abierta. El agua sí existe como recurso y sistema físico. |
|---------------|-----------------------------------------------------------------------------------------------------------|

# 9. Recursos, desmontaje y logística

## 9.1 Familias base

| **Recurso**            | **Fuentes típicas**                                    | **Usos**                                                                     |
|------------------------|--------------------------------------------------------|------------------------------------------------------------------------------|
| Madera                 | Pallets, muebles, puertas, estructuras.                | Mobiliario, almacenamiento, reparaciones interiores, proyectos ferroviarios. |
| Metal                  | Armarios, maquinaria, vehículos, restos de tren.       | Estructura, soportes, depósitos, mecanismos.                                 |
| Telas / aislantes      | Ropa, colchones, mantas, paneles.                      | Cama, aislamiento, sellado y confort.                                        |
| Componentes eléctricos | Tableros, lámparas, aparatos, instalaciones.           | Sonda, iluminación, bombas, sistemas técnicos.                               |
| Piezas mecánicas       | Motores, bombas, maquinaria.                           | Herramientas, tren, infraestructura y workbench.                             |
| Combustible            | Vehículos, depósitos, estaciones, puntos subterráneos. | Movimiento y calefacción.                                                    |
| Piezas especiales      | POIs específicos.                                      | Gating de mejoras importantes y rutas.                                       |

## 9.2 Desmontaje físico simple

El jugador apunta a un objeto aprovechable y, si tiene la herramienta, puede desmontarlo. La acción consume tiempo y produce materiales coherentes. No se busca un simulador de carpintería: la interacción debe ser corta, legible y con animación/sonido convincentes.

### Herramientas orientativas

- Barreta: separar tablas, abrir cajones, desmontaje básico.

- Sierra manual: madera y reducción de objetos grandes.

- Llave / kit mecánico: maquinaria y piezas metálicas.

- Cortadora/amoladora portátil: metal pesado, más adelante.

- Kit eléctrico: equipos delicados y cableado.

## 9.3 Objeto entero vs materiales

Algunos objetos pueden desarmarse o transportarse enteros. Una silla puede convertirse en madera y herrajes o viajar al tren como mobiliario. Una estufa puede dar metal/piezas o instalarse como equipo recuperado.

| **PRINCIPIO** | Los recursos deben representar algo físico que el jugador entiende de dónde salió y dónde terminó. |
|---------------|----------------------------------------------------------------------------------------------------|

## 9.4 Logística

El peso y el volumen convierten el saqueo en decisión. Materiales voluminosos y objetos enteros pueden requerir carga manual, carros, cabrestantes o mejoras futuras. El vagón de almacén tiene valor estratégico porque permite acumular reservas para proyectos de infraestructura.

# 10. Workbench, herramientas y blueprints

## 10.1 Función del workbench

Mesa física dentro del tren para fabricar y reparar herramientas, dispositivos reutilizables, piezas y módulos pequeños. No sustituye la construcción física del tren.

| **DECISIÓN** | Grandes mejoras: fabricar/preparar piezas en el workbench y luego instalarlas físicamente donde corresponden. |
|--------------|---------------------------------------------------------------------------------------------------------------|

## 10.2 Evolución visible

| **Estado**       | **Capacidad**                                    | **Cambios visuales**                                      |
|------------------|--------------------------------------------------|-----------------------------------------------------------|
| Mesa improvisada | Reparaciones y herramientas manuales.            | Superficie precaria, pocas herramientas.                  |
| Banco de trabajo | Piezas mecánicas y mejores herramientas.         | Tornillo de banco, soportes, iluminación.                 |
| Taller equipado  | Dispositivos eléctricos y restauración avanzada. | Taladro, sierra, equipos y cableado.                      |
| Taller avanzado  | Tecnología recuperada y sistemas especiales.     | Máquinas y herramientas complejas instaladas físicamente. |

No mostrar “Workbench nivel 4” como concepto principal. El jugador debe entender la mejora al mirar el espacio.

## 10.3 Blueprints

| **DECISIÓN** | El jugador comienza con conocimientos suficientes para no estancarse. Los planos encontrados amplían capacidades; no bloquean necesidades triviales. |
|--------------|------------------------------------------------------------------------------------------------------------------------------------------------------|

### Categorías

- Blueprints iniciales: almacenamiento simple, reparaciones, cama improvisada, recipiente, herramientas básicas.

- Blueprints encontrados: bombas, filtros, actuadores ferroviarios, sistemas eléctricos, herramientas mejores.

- Blueprints especiales: invernadero, sistemas térmicos avanzados, tecnología rara, rifle.

### Cómo se descubren

- Planos y diagramas físicos.

- Manuales técnicos.

- Terminales o registros.

- Máquinas completas que pueden analizarse en el workbench.

- Planos incompletos por fragmentos, solo para tecnologías importantes y sin abusar.

# 11. Construcción y restauración física

| **DECISIÓN** | Toda mejora importante del tren debe tener presencia física, construirse por etapas visibles y mostrar un cambio funcional o visual en cada avance. |
|--------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|

## 11.1 Construcción parcial

Si el jugador aporta una fracción de los materiales, esa fracción debe quedar representada. Una mejora puede permanecer varias expediciones a medio construir.

### Ejemplo: estante

- Estado 0: pared vacía.

- Estado 1: marcas/soportes iniciales.

- Estado 2: estructura parcial.

- Estado 3: tablas montadas.

- Estado 4: almacenamiento funcional.

### Ejemplo: vagón recuperado

10. Asegurar enganche y estructura.

11. Retirar nieve/escombros.

12. Reparar piso, paredes y ventanas.

13. Restaurar electricidad/calefacción/servicios.

14. Asignar función y equiparlo.

15. Personalizar con objetos recuperados.

## 11.2 Beneficios parciales

Cuando sea razonable, una fase incompleta ya aporta valor: una ventana parcheada pierde menos calor; una canaleta recoge poca agua; una cama improvisada recupera más energía aunque todavía no sea “buena”.

## 11.3 Tren como registro visual de la partida

Herramientas colgadas, materiales almacenados, objetos rescatados, parches, iluminación y mobiliario deben hacer que dos trenes puedan terminar viéndose diferentes según las prioridades del jugador.

# 12. Vagones y especialización

| **DECISIÓN** | Los vagones nuevos no deben ser solo “más espacio” ni venir siempre etiquetados con una función. El jugador los encuentra, engancha, restaura y decide en qué convertirlos. |
|--------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

## 12.1 Funciones posibles

| **Función**       | **Aporta**                                                                             |
|-------------------|----------------------------------------------------------------------------------------|
| Almacén / stock   | Estanterías, materiales, objetos grandes, bidones y reservas para proyectos.           |
| Taller            | Workbench expandido, máquinas y reparaciones avanzadas.                                |
| Dormitorio        | Mejor descanso, recuperación y aislamiento.                                            |
| Cocina/comedor    | Cocinar, mejorar valor de alimentos y aprovechar ingredientes.                         |
| Agua              | Tanques, filtros, bombas y distribución.                                               |
| Combustible       | Depósitos auxiliares, bombas y autonomía.                                              |
| Energía           | Generadores, baterías y distribución eléctrica.                                        |
| Invernadero       | Producción limitada de alimentos y vínculo con agua/fertilizante.                      |
| Médico            | Tratamiento, almacenamiento sanitario y recuperación.                                  |
| Exploración       | Trajes, baterías, herramientas, rifle, sonda y preparación de salidas.                 |
| Sonda/observación | Mayor alcance e interpretación de señales ferroviarias/POIs.                           |
| Comodidad         | Salón, lectura, música y objetos personales; bonus de descanso sin barra de felicidad. |

## 12.2 Chasis con afinidades

La libertad no implica que todos los vagones sean idénticos. Un cisterna es naturalmente apropiado para líquidos; uno refrigerado para comida; uno de pasajeros para dormitorio. El jugador puede elegir, pero el tipo encontrado condiciona eficiencia y coste.

## 12.3 Coste sistémico

Cada vagón puede aumentar peso, consumo de combustible, demanda de calefacción y mantenimiento. Agregar espacio debe ser una decisión, no un crecimiento gratuito.

# 13. Sondas, orientación y balizas

## 13.1 Sonda principal del tren

Instrumento estratégico. Barre el terreno alrededor de la red y detecta oportunidades con incertidumbre. Informa dirección, distancia aproximada, tiempo/posición respecto a la vía y tipo de señal sin revelar el loot exacto.

| **Señal**                      | **Interpretación posible**                  |
|--------------------------------|---------------------------------------------|
| Calor residual                 | Generador, refugio o instalación activa.    |
| Estructura bajo nieve          | Edificio/estación enterrada.                |
| Masa metálica                  | Depósito, tren o infraestructura.           |
| Emisión automática             | Baliza, terminal o equipo de mantenimiento. |
| Mancha/indicio de hidrocarburo | Depósito o tubería de combustible.          |

## 13.2 Sonda portátil

| **DECISIÓN** | No hay minimapa tradicional como herramienta principal. La sonda portátil reemplaza gran parte de esa función durante expediciones. |
|--------------|-------------------------------------------------------------------------------------------------------------------------------------|

Se lleva físicamente en la mano. Su función primaria es evitar que el jugador se pierda sin regalarle un GPS perfecto.

- Dirección y distancia al tren.

- Dirección aproximada al objetivo fijado desde la sonda principal.

- Intensidad/calidad de señal.

- Interferencias por tormenta, profundidad o estructuras.

- Mejoras futuras: mayor precisión, lectura de calor/metal, memoria de ruta y balizas.

## 13.3 Balizas

| **DECISIÓN** | Las balizas son crafteables y limitadas por recursos. Colocar una debe ser una decisión para evitar llenar el mundo con cientos de marcadores. |
|--------------|------------------------------------------------------------------------------------------------------------------------------------------------|

Se usan para marcar entradas, salidas, cruces, depósitos o rutas en interiores grandes. La sonda portátil detecta las balizas y las diferencia del tren/objetivo. Si son recuperables o consumibles definitivamente queda como detalle de balance.

# 14. Mundo procedural híbrido

| **DECISIÓN** | Usar proceduralidad para distribución, estado y variación; no para generar edificios incoherentes pieza por pieza. |
|--------------|--------------------------------------------------------------------------------------------------------------------|

## 14.1 Qué puede variar proceduralmente

- Ubicación del POI respecto a la red.

- Nivel de enterramiento por nieve.

- Accesos disponibles/bloqueados.

- Estado de energía/calefacción.

- Pools de loot coherentes con el edificio.

- Amenazas y clima local.

- Entradas alternativas y algunas habitaciones cerradas.

- Puntos de combustible y recursos del terreno.

## 14.2 Qué debe diseñarse a mano

- Identidad, layout base y narrativa espacial de los POIs importantes.

- Set pieces y megaPOIs como shopping.

- Rutas principales de onboarding/V1.

- Momentos narrativos relevantes.

## 14.3 Red ferroviaria

La red puede combinar regiones/chunks, bifurcaciones y segmentos con reglas procedurales. La sonda del tren funciona también como lectura simplificada de la red: vías conocidas, bifurcaciones, rutas desconocidas y bloqueos.

| **PRINCIPIO** | Debe existir mucho espacio vacío entre señales. La soledad pierde fuerza si cada treinta segundos aparece otro edificio. |
|---------------|--------------------------------------------------------------------------------------------------------------------------|

# 15. POIs iniciales

Pool conceptual de diez categorías para una versión amplia. V1 solo necesita un subconjunto.

| **POI**                     | **Recursos/función predominante**                              | **Riesgo/gancho**                                                  |
|-----------------------------|----------------------------------------------------------------|--------------------------------------------------------------------|
| Hospital semienterrado      | Medicinas, telas, baterías, electrónica.                       | Accesos verticales, zonas profundas, amenaza opcional.             |
| Comisaría                   | Linternas, radios, equipamiento resistente, lockers.           | No usar como excusa para un arsenal; valor en herramientas/equipo. |
| Escuela                     | Madera, muebles, telas, comedor/gimnasio.                      | Gran volumen de objetos recuperables.                              |
| Restaurante                 | Comida, utensilios, gas, cocina industrial.                    | Conservación/temperatura, espacios compactos.                      |
| Shopping / centro comercial | Múltiples subzonas: farmacia, ferretería, electrónica, comida. | MegaPOI de alto valor y alto peligro.                              |
| Taller mecánico             | Metal, aceite, piezas, herramientas.                           | Ideal para blueprints y componentes.                               |
| Estación ferroviaria        | Repuestos, mapas, combustible, piezas Red Blanca.              | Conecta narrativa y red.                                           |
| Supermercado                | Comida, agua, recipientes, almacenamiento.                     | Loot abundante pero pesado.                                        |
| Hotel                       | Colchones, telas, muebles, confort.                            | Objetos enteros y habitaciones repetidas con variación.            |
| Planta industrial           | Combustible, bombas, motores, piezas especiales.               | Infraestructura compleja, riesgo alto.                             |

## Regla de loot

La aleatoriedad no debe romper la lógica espacial. Un quirófano no es un cofre random. Cada POI tiene pools coherentes y unos pocos hallazgos excepcionales que generan sorpresa.

# 16. Amenazas, fauna y animales recuperables

## 16.1 Amenazas principales

El clima y la logística siguen siendo los antagonistas principales: frío, tormentas, distancia, peso, falta de combustible, puertas congeladas, maquinaria y rutas dañadas.

## 16.2 Fauna salvaje

Puede existir fauna adaptada al frío: lobos, zorros y, si la geografía/ficción lo justifica, osos u otros depredadores. No todos los animales deben ser agresivos. Un encuentro debe ser significativo, no un spawn constante.

POIs de alto valor pueden contener animales refugiados en interiores relativamente cálidos, lo que justifica que un shopping u hospital sean más peligrosos sin introducir enemigos humanos.

## 16.3 Animales recuperables

Animales de granja pueden encajar si sobrevivieron en “bolsillos” humanos: granjas industriales semienterradas, instalaciones con calor residual, depósitos de comida o refugios de emergencia. Encontrarlos debe ser un acontecimiento.

| **Animal** | **Valor**                               | **Coste**                                        |
|------------|-----------------------------------------|--------------------------------------------------|
| Cabras     | Leche; tamaño y resistencia razonables. | Agua, alimento, espacio y calor.                 |
| Gallinas   | Huevos y bajo volumen.                  | Protección térmica y alimento.                   |
| Ovejas     | Lana/aislante y productos secundarios.  | Más espacio, agua y alimentación.                |
| Vacas      | Solo opción muy tardía o descartable.   | Demasiado volumen y demanda para el tono actual. |

## 16.4 Vagón ganadero

Un vagón de carga puede restaurarse por fases para alojar pocos animales: estructura → aislamiento → bebederos/comederos → calefacción → almacenamiento de alimento. No debe convertirse en producción masiva.

## 16.5 Conexión sistémica

Animales → estiércol/fertilizante → invernadero → comida mejor → supervivencia. El objetivo es crear un pequeño ecosistema móvil, no una granja industrial.

# 17. Rifle y combate limitado

| **DECISIÓN** | El juego no es un shooter. Existirá más adelante la posibilidad de obtener un único rifle rudimentario, de una sola bala y recarga muy lenta. |
|--------------|-----------------------------------------------------------------------------------------------------------------------------------------------|

## Desbloqueo

El rifle puede surgir de un blueprint especial encontrado y construirse/restaurarse en el workbench/taller. No debe estar disponible en el onboarding.

## Progresión

- Mejorar el mismo rifle, no reemplazarlo por un arsenal.

- Mira posterior.

- Mejoras de mecanismo, fiabilidad o precisión.

- Posibles componentes recuperados en POIs específicos.

## Filosofía de uso

El rifle es un seguro frente a fauna peligrosa. Fallar un disparo debe seguir siendo grave. La munición y el ritmo de recarga deben impedir que el jugador resuelva el mundo disparando.

# 18. UI/UX y lenguaje visual

| **DECISIÓN** | La interfaz debe ser entendible con muy poco texto. El mundo y los dispositivos físicos comunican primero; el texto explica excepciones. |
|--------------|------------------------------------------------------------------------------------------------------------------------------------------|

## 18.1 Tres capas de información

| **Capa**                     | **Qué contiene**                                                            | **Cuándo aparece**              |
|------------------------------|-----------------------------------------------------------------------------|---------------------------------|
| Información física/diegética | Combustible, agua, energía eléctrica, sonda, calefacción, luces, medidores. | En objetos/paneles del tren.    |
| HUD mínimo                   | Vida, hambre, energía y temperatura.                                        | Durante juego normal, discreto. |
| UI de sistema                | Inventario, workbench, blueprints y gestión específica.                     | Solo al abrir/interactuar.      |

## 18.2 Feedback por estado

| **Estado**              | **Feedback antes del texto**                                                                  |
|-------------------------|-----------------------------------------------------------------------------------------------|
| Combustible bajo        | Motor cambia de sonido, calefacción baja, aguja física cerca de vacío, luces pueden fluctuar. |
| Frío                    | Respiración, escarcha, condensación, audio, tinte/feedback sutil.                             |
| Energía baja            | Bostezos/respiración, recuperación física más lenta.                                          |
| Hambre                  | Feedback corporal y penalización gradual, no alarmas constantes.                              |
| Construcción incompleta | La propia geometría muestra progreso; no hace falta un “43%” permanente.                      |

## 18.3 Workbench visual

Mostrar el objeto como plano/ensamble con piezas existentes sólidas y faltantes translúcidas o claramente diferenciadas. Los nombres y cantidades aparecen al inspeccionar, no como una pared de texto.

## 18.4 Paleta funcional de UI

- Blanco / neutro: información normal.

- Ámbar: atención o advertencia.

- Rojo: peligro inmediato.

- Azul frío: temperatura/condiciones térmicas.

Evitar una interfaz multicolor tipo RPG. La estética debe ser sobria, industrial y técnica.

# 19. Dirección artística y audio

## Objetivo visual

Realismo atmosférico indie: referencia conceptual cercana al nivel de inmersión de survivals 3D como The Forest, sin perseguir fotorealismo AAA. La sensación debe venir de iluminación, materiales PBR, clima, escala y animaciones físicas creíbles.

## Exterior

- Blancos/grises/azules, niebla y visibilidad variable.

- Nieve con acumulaciones, viento y zonas erosionadas.

- Ruinas parcialmente enterradas y restos verticales que rompan el horizonte.

- Sol y ciclo día/noche visibles; días despejados pueden ser hermosos y hostiles a la vez.

## Interior del tren

- Contraste cálido: luces amarillas, madera recuperada, metal usado, condensación en ventanas.

- El espacio se vuelve más ordenado y habitable con el progreso.

- La diferencia acústica al cerrar la puerta debe ser fuerte: el viento cae y vuelve el motor/calefacción.

## Lenguaje tecnológico

Industrial ferroviario + supervivencia improvisada + tecnología analógica recuperada. Interruptores, agujas, etiquetas, CRT o pantallas pequeñas, metal rayado y cableado. Evitar futurismo brillante y evitar copiar la identidad visual de Fallout.

# 20. Primeros 30–45 minutos

Este onboarding debe estar diseñado a mano. Su objetivo es enseñar el ADN del juego sin tutoriales largos.

## 20.1 00:00 — Despertar

Pantalla negra, viento y vibración metálica. El jugador despierta sobre un colchón/tela deteriorados. Luz amarilla inestable, respiración visible y un vagón precario. Puede recorrer el espacio y leer visualmente sus problemas: poco combustible, pocas provisiones, almacenamiento mínimo.

## 20.2 05:00 — Primera señal

Se activa la sonda del tren y aparece una estructura ferroviaria de señal débil. El viaje enseña reversor/potencia/freno y muestra el mundo por primera vez.

## 20.3 10:00 — Estación de mantenimiento

Primer POI: pequeña estación/taller de la Red Blanca, parcialmente enterrada. El tren se detiene cerca. El jugador toma traje, sonda portátil, herramienta y bidón.

## 20.4 Primer obstáculo

Puerta congelada. El marco de hielo comunica el problema sin texto. La barreta permite desprenderlo. Esta interacción enseña que el mundo contiene obstáculos físicos solucionables.

## 20.5 Primer saqueo

Dentro hay combustible, madera, tela/aislante, metal, una ración y una bomba manual vieja. El jugador aprende el trasvase manual y siente cómo la temperatura exterior/interior condiciona el tiempo.

## 20.6 Tentación opcional

Una segunda sala tiene una señal o sonido. Si el jugador arriesga tiempo, puede encontrar un blueprint no esencial (por ejemplo, estante reforzado). Enseña que no es necesario limpiar un POI en una sola visita.

## 20.7 Regreso

La vuelta debe vender el contraste: viento y hostilidad afuera; al cerrar la puerta del tren, cae el ruido, aparece el calor y el jugador entiende emocionalmente por qué vale la pena mejorar el refugio.

## 20.8 Primera mejora

Se reposta el tren y se usan madera/tela para convertir el lugar miserable de descanso en una cama improvisada. La construcción se ve por etapas. Al dormir, la recuperación de energía es claramente mejor.

## 20.9 Primer gran objetivo

Al continuar, el jugador encuentra una bifurcación congelada y dañada. La nueva vía se dirige a una región desconocida, pero el cambio requiere recursos y una pieza ferroviaria especial. La sonda muestra esa ruta como desconocida/bloqueada.

| **PRINCIPIO** | La bifurcación transforma el juego de “sobrevivir hoy” a “prepararme para ir más lejos”. |
|---------------|------------------------------------------------------------------------------------------|

# 21. V1 / vertical slice

La V1 debe ser una pequeña experiencia completa que demuestre el loop y termine con un deseo concreto de seguir jugando. No necesita proceduralidad completa, megaPOIs, animales, rifle ni vagones adicionales.

## 21.1 Pregunta de validación

“¿Es divertido vivir en este tren, salir por una necesidad, volver con algo útil, transformarlo físicamente y preparar el acceso a una nueva región?”

## 21.2 Contenido obligatorio

| **Sistema**         | **V1**                                                                                 |
|---------------------|----------------------------------------------------------------------------------------|
| Tren                | Locomotora/refugio precario; puede arrancar, frenar y detenerse.                       |
| Movimiento relativo | Caminar dentro, bajar y subir sin jitter/deslizamientos.                               |
| Sonda principal     | Detectar al menos 1–3 señales preparadas a mano.                                       |
| Sonda portátil      | Dirección al tren + objetivo básico.                                                   |
| Supervivencia       | Vida, hambre, energía, temperatura; valores generosos para pruebas.                    |
| Combustible         | Consumo + repostaje manual.                                                            |
| Recursos            | Madera, metal, aislante/tela, componentes simples.                                     |
| Desmontaje          | Un conjunto pequeño de props aprovechables.                                            |
| Workbench           | Mesa básica con pocos recipes/blueprints.                                              |
| Construcción        | Cama por fases y al menos otra mejora simple.                                          |
| POIs                | Estación de mantenimiento + 2–4 lugares pequeños para reunir el proyecto de vía.       |
| Cambio ferroviario  | Bifurcación bloqueada que funciona como objetivo de largo plazo de la slice.           |
| Guardado            | Estado del jugador, tren, recursos, construcción y reparación de vía.                  |
| Final de slice      | Reparar el cambio, accionar la palanca y entrar por primera vez a la ruta desconocida. |

## 21.3 Lo que NO entra en V1

- Mundo procedural completo.

- Shopping/megaPOI terminado.

- Animales de granja y vagón ganadero.

- Rifle.

- Invernadero.

- Múltiples vagones funcionales anexados.

- Narrativa final del Enfriamiento.

- Automatización avanzada de combustible o cambios.

# 22. Arquitectura técnica propuesta

| **PROPUESTA** | Stack recomendado actual: Unity 6 + URP + C# + Git + Claude Code. Tratarlo como propuesta técnica validable mediante un spike inicial. |
|---------------|----------------------------------------------------------------------------------------------------------------------------------------|

## 22.1 Principios de arquitectura

- Separar sistemas para poder reequilibrar uno sin reescribir los demás.

- Datos de balance en configuraciones/ScriptableObjects o equivalente, no hardcodeados en scripts.

- Comunicación por interfaces/eventos cuando tenga sentido; evitar referencias circulares.

- Estados persistentes con IDs estables para impedir duplicaciones al cargar.

- No crear managers globales monolíticos que conozcan todo el juego.

## 22.2 Módulos sugeridos (nombres ilustrativos)

| **Módulo**            | **Responsabilidad**                                                 |
|-----------------------|---------------------------------------------------------------------|
| Train / Locomotion    | Potencia, freno, reversor, combustible mecánico, velocidad.         |
| Rail Graph            | Segmentos, bifurcaciones, rutas y switches.                         |
| Player Motor          | Movimiento FPS y referencias relativas al tren.                     |
| Interaction           | Raycast/target, mantener tecla, animaciones e interfaces de uso.    |
| Survival              | Vida, hambre, energía, temperatura y efectos derivados.             |
| Fuel / Heat / Water   | Reservas, consumo, calefacción y recolección de agua.               |
| Inventory / Carry     | Slots/peso/objetos grandes y contenedores.                          |
| Salvage               | Objetos desmontables, yields, herramientas requeridas.              |
| Workbench / Recipes   | Fabricación, reparación y objetos reutilizables.                    |
| Blueprint Registry    | Qué diseños conoce el jugador y cómo se desbloquean.                |
| Build Sites           | Construcciones por fases y aporte parcial de materiales.            |
| Probe                 | Sonda principal y portátil, señales e interferencias.               |
| POI / World Streaming | Chunks, carga de regiones y variaciones de POIs.                    |
| Save                  | Persistencia de mundo, construcción, recursos, switches y upgrades. |
| UI/Feedback           | HUD mínimo y adapters a instrumentos diegéticos.                    |

## 22.3 Riesgo técnico principal: movimiento dentro del tren

| **RIESGO** | No avanzar a sistemas grandes hasta que caminar, saltar, interactuar con props, bajar y volver a subir a un tren en movimiento sea estable. |
|------------|---------------------------------------------------------------------------------------------------------------------------------------------|

Claude debe hacer un spike específico y comparar estrategias de referencia/movimiento. La solución elegida debe tolerar físicas, objetos apoyados, animaciones, cambios de velocidad y carga/descarga de chunks. No asumir que “parentar al jugador al tren” resolverá todo sin pruebas.

## 22.4 Mundo y streaming

Propuesta: regiones/chunks alrededor de la red ferroviaria cargados de manera aditiva/asíncrona. POIs artesanales se instancian según seed y reglas. La nieve, niebla y distancia limitada ayudan a ocultar transiciones sin que parezcan trucos técnicos.

# 23. Optimización y rendimiento

| **PRINCIPIO** | La optimización es un requisito de diseño, no un paso final. Aun así, evitar microoptimizar antes de medir. |
|---------------|-------------------------------------------------------------------------------------------------------------|

- URP y presupuesto visual controlado.

- LODs agresivos en exteriores.

- GPU instancing / batching en props repetidos cuando corresponda.

- Pooling para objetos y efectos recurrentes.

- Occlusion culling especialmente en interiores grandes.

- No ejecutar Update por objeto si un sistema central/event-driven puede resolverlo.

- Texturas y materiales reutilizables/modulares.

- Clima y niebla como decisión artística y herramienta para limitar distancia de dibujado.

- Profiler desde builds tempranas; medir CPU, GPU, memoria y GC.

- El procedural debe generar datos y composición, no un costo continuo innecesario en runtime.

## Objetivo de calidad

“Súper optimizado” significa que el juego se mantiene estable en hardware objetivo sin sacrificar atmósfera. La inmersión se prioriza mediante audio, iluminación, materiales y comportamiento, no densidad geométrica extrema.

# 24. Orden de implementación

| **INSTRUCCIÓN PARA CLAUDE** | Construir por hitos verificables. Cada fase debe compilar, ejecutarse y tener una prueba manual antes de añadir otra. |
|-----------------------------|-----------------------------------------------------------------------------------------------------------------------|

| **Fase**                        | **Construir**                                                           | **Prueba de aceptación**                                                          |
|---------------------------------|-------------------------------------------------------------------------|-----------------------------------------------------------------------------------|
| 0\. Spike técnico               | Proyecto limpio, FPS controller, vagón/tren móvil, referencias físicas. | Caminar, saltar, dejar un objeto, bajar y subir repetidas veces sin jitter grave. |
| 1\. Conducción                  | Reversor, potencia, freno, vía simple, parada.                          | Conducir y detener el tren en un punto sin errores.                               |
| 2\. Interacción + supervivencia | Interactuar, vida/hambre/energía/temperatura.                           | Estados cambian y feedback es legible sin bloquear controles.                     |
| 3\. Sondas                      | Consola principal + portátil básica.                                    | Detectar POI y volver al tren sin minimapa.                                       |
| 4\. Primer POI + combustible    | Estación, puerta congelada, bidón/bomba manual, repostaje.              | Completar primera expedición.                                                     |
| 5\. Inventario + salvage        | Materiales, peso, props desmontables.                                   | Recuperar recursos lógicos sin duplicaciones.                                     |
| 6\. Workbench + cama            | Blueprints base, recipe, BuildSite por fases.                           | Mejorar la cama y comprobar mejora de energía.                                    |
| 7\. Rail gating                 | Switch congelado/roto + proyecto de recursos.                           | Aportar materiales parcialmente y conservar progreso.                             |
| 8\. Mini-región                 | 2–4 POIs adicionales y recursos para la vía.                            | Loop completo repetible hasta abrir la ruta.                                      |
| 9\. Guardado                    | Persistencia de estado.                                                 | Guardar/cargar sin reaparecer loot o perder construcciones.                       |
| 10\. Polish/perf                | Audio, VFX, UI mínima, perfilado.                                       | Vertical slice estable y presentable.                                             |

La sección 24 fija el orden y las pruebas de aceptación; el Anexo G fija la entrega mínima de cada paso. Equivalencia: fase 0 = M0 y M1; fase 1 = M2; fase 2 = M3 y M4; fase 3 = M5; fase 4 = M6; fase 5 = M7; fase 6 = M8 y M9; fase 7 = M10; fase 8 = M11; fase 9 = M12; fase 10 = M13.

# 25. Pruebas de aceptación

## 25.1 Experiencia

- Al volver del exterior, el interior debe sentirse claramente más seguro y cálido.

- Después de una expedición, el jugador puede señalar físicamente qué cambió gracias a ella.

- La sonda informa lo suficiente para orientar sin revelar todo.

- El jugador puede abandonar un objetivo y regresar sin que el juego lo castigue por “no limpiar” el lugar.

- El HUD no requiere leer párrafos para entender necesidades básicas.

- Abrir una vía nueva se siente como logro logístico, no como pulsar un botón.

## 25.2 Técnica

- No hay vibración/jitter perceptible al caminar dentro del tren a velocidades objetivo.

- Props apoyados no atraviesan ni se desplazan absurdamente por el vagón.

- Carga/descarga de POIs no produce pausas severas visibles.

- Guardar/cargar conserva recursos, construcciones parciales, combustible y estado de switches.

- No se duplican ítems ni blueprints al recargar.

- El juego puede repetir el loop de V1 varias veces sin degradación de rendimiento significativa.

## 25.3 Criterio de éxito de la vertical slice

Al accionar el cambio reparado y ver al tren entrar en una vía desconocida, el jugador debería querer saber qué hay después.

# 26. Reglas de trabajo para Claude

| **INSTRUCCIÓN PARA CLAUDE** | Este documento es la fuente de verdad. Antes de implementar una feature, identificar qué secciones la gobiernan y qué partes están marcadas como decisión/propuesta. |
|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------|

16. Revisar el estado actual del repositorio antes de crear scripts o duplicar sistemas existentes.

17. Trabajar una feature por vez. Evitar “de paso agregué inventario, clima y crafting”.

18. Proponer estructura y archivos antes de una refactorización grande.

19. Mantener commits pequeños, reversibles y descriptivos.

20. Centralizar parámetros de balance y no esconder números críticos en múltiples scripts.

21. Escribir una prueba manual breve para cada entrega.

22. No ampliar el lore ni inventar sistemas fuera del documento sin marcarlos primero como propuesta.

23. Si una decisión de diseño causa un problema técnico real, explicar el conflicto y ofrecer 2–3 soluciones con tradeoffs antes de modificarla.

24. Priorizar legibilidad y mantenibilidad sobre soluciones “mágicas” generadas por IA.

25. Evitar dependencias innecesarias y paquetes externos si la función es pequeña y puede resolverse de forma estable con el motor.

26. Perfilar antes de afirmar que algo está optimizado.

27. No sacrificar el feedback visual/físico de una mecánica importante solo para simplificar código; buscar primero una implementación escalable.

## Formato recomendado para cada tarea pedida a Claude

| **Bloque**           | **Contenido esperado**                       |
|----------------------|----------------------------------------------|
| Objetivo             | Qué experiencia o sistema se quiere lograr.  |
| Restricciones        | Decisiones relevantes de este documento.     |
| Archivos afectados   | Qué existe y qué se propone tocar.           |
| Implementación       | Pasos concretos y arquitectura.              |
| Parámetros editables | Valores de balance expuestos.                |
| Prueba manual        | Cómo comprobar que funciona dentro de Unity. |
| Riesgos              | Qué puede romperse o qué queda pendiente.    |

# 27. Decisiones abiertas y backlog

Estas preguntas no deben bloquear la V1 salvo que una implementación concreta las necesite.

| **Tema**                             | **Estado / pregunta**                                                                                 |
|--------------------------------------|-------------------------------------------------------------------------------------------------------|
| Nombre definitivo                    | Abierto.                                                                                              |
| Ubicación geográfica exacta          | Abierta; afecta fauna y arquitectura.                                                                 |
| Causa técnica final del Enfriamiento | Geoingeniería + retroalimentación de hielo es base provisional; detalle científico/narrativo abierto. |
| Responsable del desastre             | Abierto; evitar exposición temprana.                                                                  |
| Objetivo narrativo final             | Posible búsqueda de infraestructura capaz de alterar el clima; no cerrado.                            |
| Hidratación                          | El agua es recurso; decidir si merece barra independiente.                                            |
| Muerte/guardado                      | Castigo final por fallo y posible permadeath siguen abiertos.                                         |
| Balizas                              | Decidir si son recuperables, si usan batería y cuánto cuesta colocarlas.                              |
| Fauna exacta                         | Definir según región y tono; encuentros escasos.                                                      |
| Reproducción animal                  | Si existe, debe ser lenta y contenida.                                                                |
| Anexado de vagones                   | Diseñar transición física/enganche sin glitches; no V1.                                               |
| Procedural de red                    | Definir algoritmo solo después de validar región hecha a mano.                                        |
| Rifle                                | Balance, munición y mejoras específicas después del core loop.                                        |
| Multijugador                         | Fuera del alcance actual.                                                                             |
| Plataformas objetivo                 | Definir antes de optimización final y UI de input.                                                    |

# 28. Glosario de diseño

| **Término**         | **Definición**                                                                        |
|---------------------|---------------------------------------------------------------------------------------|
| El Enfriamiento     | Nombre de trabajo para el colapso climático que congeló el mundo.                     |
| Red Blanca          | Red ferroviaria reforzada/adaptada durante los años de enfriamiento.                  |
| POI                 | Point of Interest: lugar explorable con identidad y recursos lógicos.                 |
| Sonda principal     | Consola del tren para detección estratégica de señales.                               |
| Sonda portátil      | Dispositivo manual de orientación durante expediciones.                               |
| Baliza              | Marcador físico crafteable detectado por la sonda portátil.                           |
| BuildSite           | Zona física donde se aportan materiales y una mejora avanza por fases.                |
| Blueprint           | Conocimiento técnico que habilita fabricar/restaurar algo no trivial.                 |
| Salvage             | Recuperación/desmontaje de objetos del mundo para obtener materiales o reutilizarlos. |
| Vertical slice / V1 | Primera experiencia pequeña pero completa que valida el juego.                        |
| MegaPOI             | Lugar grande de múltiples subzonas, como el shopping.                                 |
| Gating ferroviario  | Bloqueos/reparaciones que regulan acceso a nuevas rutas mediante lógica del mundo.    |

# Cierre: qué estamos intentando demostrar

La primera meta no es demostrar que podemos construir un mundo infinito. Es demostrar que el jugador desarrolla apego por el tren y que cada salida tiene un motivo concreto y una recompensa visible.

La V1 funciona si el jugador recuerda cómo era su refugio al despertar, ve que lo cambió con sus propias expediciones, acumula recursos porque entiende para qué los necesita y siente curiosidad real cuando finalmente abre una vía nueva.

| **PRINCIPIO** | Regla final: cuando exista duda entre agregar más contenido y hacer que el loop actual se sienta mejor, priorizar el loop. |
|---------------|----------------------------------------------------------------------------------------------------------------------------|

# ANEXO A. Baseline técnico ejecutable

| **DECISIÓN TÉCNICA** | Para la V1 se trabajará sobre Unity 6.x + URP + C# + Git + Claude Code. La versión exacta del Editor se fija al crear el proyecto y no se actualiza durante la vertical slice salvo necesidad demostrada. |
|----------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

## A.1 Stack base

| **Área**             | **Elección para V1**                  | **Regla de uso**                                                                                          |
|----------------------|---------------------------------------|-----------------------------------------------------------------------------------------------------------|
| Motor                | Unity 6.x estable                     | Fijar la versión del Editor en el repositorio. No actualizar a mitad de un milestone.                     |
| Render               | Universal Render Pipeline (URP)       | Priorizar claridad, atmósfera y rendimiento. No usar HDRP para la V1.                                     |
| Lenguaje             | C#                                    | Código legible, modular y con parámetros de balance fuera de scripts cuando corresponda.                  |
| Input                | Unity Input System                    | Mapas de acciones para jugador, tren, UI e interacción. Evitar leer teclas crudas repartidas por scripts. |
| IA de desarrollo     | Claude Code + plugin oficial de Unity | Claude inspecciona el proyecto, propone plan, edita, ejecuta validaciones y documenta pruebas.            |
| Control de versiones | Git + Git LFS para binarios grandes   | Commits pequeños; no guardar Library/Temp/Logs. LFS para arte/audio binario pesado.                       |
| Audio V1             | Unity AudioMixer + AudioSources       | Evitar middleware externo al principio. Interior/exterior mediante grupos, snapshots y filtros simples.   |
| Plataforma inicial   | PC / Windows                          | Primero una plataforma. La portabilidad se evalúa cuando la V1 sea estable.                               |

## A.2 Unity + Claude Code

**Estado verificado a octubre de 2026.** Unity dispone de un plugin oficial de primera parte para Claude Code, compatible con Unity 6.0 o posterior, que instala skills de ingeniería y permite utilizar Unity CLI dentro del flujo del agente. Debe preferirse ese plugin frente a integraciones comunitarias para las tareas que cubra.

- Instalar/validar Unity y el plugin antes de pedir a Claude que genere gameplay.

- Claude debe revisar ProjectVersion.txt, Packages/manifest.json, estado de Git y estructura de Assets antes de modificar el proyecto.

- No permitir que el agente cambie versión de Unity, render pipeline o paquetes centrales sin una tarea explícita.

- Usar el agente para inspección, código, pruebas y operaciones repetibles; las decisiones de diseño siguen gobernadas por este documento.

## A.3 Render path y GPU culling

| **RIESGO / SPIKE** | No activar funciones de GPU por moda. En URP, GPU Resident Drawer / GPU Occlusion dependen de configuración y hardware; GPU Occlusion requiere Forward+ y soporte de compute shaders. Comparar en el hardware objetivo y conservarlo solo si mejora los tiempos medidos. |
|--------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

- La V1 puede comenzar con un renderer URP simple y pasar a Forward+ solo si el spike lo justifica.

- Interiores grandes como shopping/hospital son buenos candidatos a culling; exteriores abiertos pueden beneficiarse menos.

- El criterio es frame time y estabilidad, no una casilla marcada en Project Settings.

## A.4 Paquetes y dependencias

| **Categoría**                     | **Política**                                                                                                      |
|-----------------------------------|-------------------------------------------------------------------------------------------------------------------|
| Paquetes Unity oficiales          | Permitidos cuando resuelven una necesidad real: Input System, Test Framework y herramientas oficiales necesarias. |
| Asset Store                       | No introducir paquetes grandes en V1 sin revisar licencia, coste de mantenimiento, rendimiento y necesidad.       |
| Middleware de audio               | Postergar. Unity AudioMixer es suficiente para validar el loop.                                                   |
| Frameworks de inventario/crafting | Evitar al inicio. Los sistemas son específicos y la dependencia puede imponer una arquitectura ajena al diseño.   |
| Código generado por IA            | Se trata como código de producción: revisión, nombres claros, pruebas, profiler y commits reversibles.            |

# ANEXO B. Estructura de proyecto y repositorio

| **PRINCIPIO** | La estructura debe ayudar a Claude y al desarrollador a saber dónde vive cada responsabilidad. No crear carpetas ni “Managers” genéricos para todo. |
|---------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|

## B.1 Estructura sugerida

Assets/\_Project/Art

Assets/\_Project/Audio

Assets/\_Project/Data

Assets/\_Project/Materials

Assets/\_Project/Prefabs

Assets/\_Project/Scenes/Bootstrap

Assets/\_Project/Scenes/V1

Assets/\_Project/Scenes/POI

Assets/\_Project/Scripts/Core

Assets/\_Project/Scripts/Train

Assets/\_Project/Scripts/Rail

Assets/\_Project/Scripts/Player

Assets/\_Project/Scripts/Interaction

Assets/\_Project/Scripts/Survival

Assets/\_Project/Scripts/Inventory

Assets/\_Project/Scripts/Workbench

Assets/\_Project/Scripts/Building

Assets/\_Project/Scripts/Probe

Assets/\_Project/Scripts/World

Assets/\_Project/Scripts/Save

Assets/\_Project/Scripts/UI

Assets/\_Project/Tests

**No crear todas las carpetas “por anticipado”.** La lista define el destino esperado cuando el módulo exista; el proyecto debe permanecer pequeño durante los primeros milestones.

## B.2 Assembly Definitions

Usar pocos asmdefs, no uno por carpeta. Solo introducirlos cuando el proyecto tenga suficiente tamaño como para mejorar tiempos de compilación y límites de dependencia. Propuesta futura: Core, Gameplay, World, UI y Tests. La V1 puede funcionar con una estructura más simple.

## B.3 Datos de balance

- ScriptableObjects/configuración para velocidades, consumo, frío, hambre, costes, capacidades y tuning de interacción.

- Prefabs contienen referencias visuales y componentes; no deben ser la única fuente de números de balance dispersos.

- Cada configuración relevante tiene nombre, unidad y rango comprensible. Ejemplo: fuelConsumptionLitersPerMinute, no “rate = 0.017”.

- Separar datos de diseño de estado runtime. Un ScriptableObject no debe convertirse accidentalmente en el save game.

## B.4 Save game

| **DECISIÓN TÉCNICA** | No usar PlayerPrefs como sistema de guardado del mundo. Guardar un modelo versionado con IDs persistentes, seed del mundo y deltas de estado. |
|----------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|

| **Guardar**     | **Ejemplos**                                                                     |
|-----------------|----------------------------------------------------------------------------------|
| Jugador         | posición segura, vida, hambre, energía, temperatura, inventario/equipo.          |
| Tren            | posición ferroviaria, combustible, agua, vagones, contenido físico, upgrades.    |
| Construcción    | build sites, materiales ya aportados, fases completadas.                         |
| Mundo           | seed, POIs visitados, loot retirado, puertas/interruptores, animales relevantes. |
| Red ferroviaria | cambios, reparaciones, bloqueos resueltos, rutas descubiertas.                   |
| Conocimiento    | blueprints y descubrimientos.                                                    |

# ANEXO C. Spike técnico crítico: tren + jugador + mundo

| **BLOQUEADOR** | No construir el resto del survival hasta aprobar este spike. El proyecto depende de que el jugador, objetos y cámara se comporten correctamente en y alrededor de un tren que se mueve sobre una red. |
|----------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

## C.1 Estrategias a comparar

| **Estrategia**                     | **Ventaja**                                                       | **Riesgo**                                                                       |
|------------------------------------|-------------------------------------------------------------------|----------------------------------------------------------------------------------|
| Tren se mueve por el mundo         | Modelo mental simple; el mundo permanece estable.                 | Precisión a grandes distancias; físicas y jugador sobre plataforma móvil.        |
| Tren casi fijo / mundo se desplaza | Muy estable dentro del tren.                                      | Complica POIs, físicas exteriores, persistencia y lectura de posición.           |
| Híbrido + floating origin          | Mantiene coordenadas razonables y conserva movimiento real local. | Más arquitectura; recenter debe ser transparente para físicas, streaming y save. |

Claude debe construir prototipos mínimos y medir/observar antes de elegir. No debe declarar ganadora una estrategia solo por intuición.

## C.2 Matriz de aceptación del spike

| **Prueba**        | **Debe ocurrir**                                                                                                  |
|-------------------|-------------------------------------------------------------------------------------------------------------------|
| Caminar           | Caminar adelante/atrás/lateral dentro del vagón a velocidad constante sin deriva perceptible.                     |
| Aceleración/freno | El jugador no sale despedido ni desliza absurdamente cuando cambia la velocidad dentro de parámetros normales.    |
| Salto             | Saltar y aterrizar dentro del vagón conserva referencia coherente.                                                |
| Props             | Dejar una caja/herramienta en una mesa o piso no produce jitter grave ni atraviesa geometría.                     |
| Bajar/subir       | Transición interior→exterior→interior repetible sin teletransporte visible ni offsets.                            |
| POI cercano       | El tren puede detenerse junto a un POI y el jugador interactúa con ambos espacios sin cambio de “mundo” evidente. |
| Streaming         | Cargar/descargar una zona próxima no rompe posición, físicas ni cámara.                                           |
| Guardado          | Guardar y volver a cargar mantiene una posición ferroviaria coherente.                                            |

## C.3 Red ferroviaria

- Representar la red como segmentos conectados y nodos de switch, no como una spline infinita sin topología.

- El tren conoce el segmento actual, distancia sobre el segmento y sentido. Esto simplifica save, rutas y sonda.

- Un cambio de vía determina el segmento de salida disponible. El jugador no tiene steering libre.

- Los bloqueos ferroviarios son estado de mundo persistente: congelado, roto, sin energía, reparado, automatizado.

- La representación visual del riel y la representación lógica de navegación deben estar relacionadas pero no ser el mismo objeto monolítico.

# ANEXO D. Generación procedural híbrida y persistencia

| **DECISIÓN** | La proceduralidad compone un mundo con piezas diseñadas; no inventa contenido arbitrario. La misma seed debe poder reconstruir la base del mundo y el save conserva únicamente los cambios relevantes. |
|--------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

## D.1 Pipeline conceptual de generación

28. Elegir/generar una seed de partida.

29. Construir o seleccionar una red ferroviaria regional con bifurcaciones válidas.

30. Asignar regiones/biomas climáticos y reglas de densidad.

31. Seleccionar POIs artesanales compatibles con cada tramo y reservar distancias de silencio.

32. Aplicar variaciones: enterramiento, accesos, energía, amenaza, loot pool y señal de sonda.

33. Generar puntos secundarios: combustible, restos, pequeñas estructuras y obstáculos ferroviarios.

34. Al cargar una región, reconstruir base desde seed y aplicar deltas persistentes del save.

## D.2 Datos mínimos de un POI

| **Campo**         | **Ejemplo**                                                            |
|-------------------|------------------------------------------------------------------------|
| StableId          | poi_region03_railA_004                                                 |
| Archetype         | Hospital / estación / taller / escuela / shopping                      |
| Seed local        | Derivada de seed global + ID para variaciones deterministas.           |
| Rail anchor       | Segmento, distancia y lado respecto a vía.                             |
| Burial profile    | Nivel de enterramiento y entradas válidas.                             |
| State profile     | Sin energía / calor residual / parcialmente activo.                    |
| Loot profile      | Pools coherentes por subzona.                                          |
| Threat profile    | Ninguna / clima / fauna / peligro mecánico.                            |
| Persistent deltas | Puertas, loot tomado, máquinas, balizas, reparación, animal rescatado. |

## D.3 V1

La vertical slice NO necesita este generador completo. Usar una región hecha a mano o una seed fija que simule la estructura final. El código de V1 sí debe evitar asumir que solo existirá un POI o una ruta.

# ANEXO E. Estrategia visual y presupuesto de rendimiento

| **PRINCIPIO** | La inmersión se compra primero con contraste térmico, audio, clima, materiales, escala y animación. No con densidad poligonal indiscriminada. |
|---------------|-----------------------------------------------------------------------------------------------------------------------------------------------|

## E.1 Objetivo provisional de rendimiento

| **Objetivo**    | **Baseline provisional**                                                                                 |
|-----------------|----------------------------------------------------------------------------------------------------------|
| Framerate       | 60 FPS a 1080p en el PC de referencia que se defina para desarrollo.                                     |
| Frame budget    | 16,7 ms por frame. Medir CPU/GPU por separado y conservar margen para tormentas/POIs.                    |
| Stutter         | Evitar picos visibles por carga, compilación de shaders, GC o generación procedural.                     |
| GC steady-state | Buscar asignaciones cercanas a cero por frame en gameplay estable; no convertirlo en dogma sin profiler. |
| Escalabilidad   | Presets de calidad y parámetros escalables para sombras, niebla, partículas, vegetación y distancia.     |

## E.2 Nieve sin destruir rendimiento

- Base visual mediante materiales/shaders con cobertura de nieve y mezcla por normales/altura cuando corresponda.

- Nieve volumétrica/deformable global fuera de alcance de V1.

- Huellas y deformación: limitar a zona próxima al jugador, decals o buffer local si el spike visual lo justifica.

- Tormentas mediante partículas con pooling, niebla/volúmenes y audio; no millones de partículas físicas.

- Objetos enterrados usan variantes de placement/meshes y máscaras locales; no simular años de acumulación.

## E.3 Iluminación

- Sol/día-noche dinámico para exterior.

- Interiores y POIs deben apoyarse en iluminación horneada/probes cuando sea compatible con cambios de estado.

- Luces dinámicas con sombra reservadas para elementos importantes; limitar cantidad simultánea.

- Contraste exterior frío/interior cálido es parte del gameplay y debe sobrevivir a los presets de calidad.

- No perseguir GI en tiempo real de coste alto para imitar un AAA; evaluar técnicas de Unity solo si la ganancia visual justifica el presupuesto.

## E.4 Assets y streaming

- LODs desde el pipeline de arte, no como parche final.

- Materiales compartidos y atlases cuando tenga sentido; evitar una instancia de material accidental por prop.

- Texturas 2K como referencia frecuente; 4K solo para assets hero donde la cámara realmente lo justifique.

- Scenes aditivas/asíncronas para regiones/POIs; Addressables solo si el proyecto demuestra que los necesita.

- Precalentar/pool de efectos usados con frecuencia y vigilar shader variants/build size.

## E.5 Audio como optimización de inmersión

- Snapshots interior/exterior para que cerrar la puerta cambie inmediatamente viento, motor y sensación térmica.

- Capas de motor según potencia/carga, no un loop único.

- Viento contextual por exposición y tormenta.

- Señales de la sonda y maquinaria con firmas sonoras reconocibles para reducir dependencia de texto/HUD.

- Usar spatial audio y oclusión sencilla donde aporte; no sobrediseñar un sistema acústico antes de la V1.

# ANEXO F. Contrato operativo para Claude Code

| **INSTRUCCIÓN** | Claude implementa el milestone pedido, no “el juego completo”. Todo sistema futuro del documento es contexto arquitectónico, no permiso para construirlo. |
|-----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|

## F.1 Secuencia obligatoria por tarea

35. Inspeccionar repositorio: git status/diff, estructura, versión de Unity, packages y scripts relacionados.

36. Repetir el objetivo de la tarea en una frase y listar qué queda explícitamente fuera de alcance.

37. Proponer una solución mínima y los archivos que se crearán/modificarán.

38. Identificar riesgos: físicas, save, rendimiento, dependencias o ruptura de prefabs/escenas.

39. Implementar de manera incremental.

40. Compilar y corregir errores antes de continuar.

41. Ejecutar tests disponibles y una prueba manual definida para la feature.

42. Si la feature toca rendimiento, medir con Profiler antes/después.

43. Mostrar resumen de cambios y riesgos pendientes.

44. Commit solo cuando el usuario lo pida o el milestone defina explícitamente un commit.

## F.2 Prohibiciones para el agente

- No actualizar Unity o cambiar URP/HDRP sin autorización.

- No agregar un paquete externo para resolver algo pequeño sin justificarlo.

- No reescribir una escena/prefab completo si puede hacerse una modificación localizada.

- No crear singletons/managers universales por conveniencia.

- No introducir sistemas futuros “porque después los vamos a necesitar”.

- No inventar lore, enemigos, armas, currencies o stats que no estén decididos.

- No ocultar bugs de física con teletransportes, parenting forzado o correcciones visuales sin documentarlo.

- No afirmar que una feature está optimizada sin medición.

- No borrar assets ni datos persistentes sin mostrar primero el impacto.

## F.3 Definición de “terminado” para una feature

| **Criterio** | **Pregunta**                                               |
|--------------|------------------------------------------------------------|
| Funciona     | ¿Cumple exactamente el objetivo y no solo “compila”?       |
| Legible      | ¿El jugador entiende el feedback sin párrafos de tutorial? |
| Integrada    | ¿Respeta módulos, datos de balance y save cuando aplica?   |
| Testeada     | ¿Existe una prueba manual concreta y se ejecutó?           |
| Performante  | ¿Se midió si la feature es potencialmente cara?            |
| Reversible   | ¿Los cambios son acotados y fáciles de revertir?           |
| Documentada  | ¿Quedaron claras decisiones, parámetros y deuda técnica?   |

## F.4 Plantilla de pedido recomendado a Claude

**OBJETIVO:** Implementar \[feature\] correspondiente al milestone \[X\].

**CONTEXTO:** Revisar primero las secciones \[números\] del documento maestro y el estado real del repositorio.

**ALCANCE:** Construir solo \[lista\]. No implementar \[lista futura\].

**RESTRICCIONES:** Mantener arquitectura modular, valores configurables y feedback físico/visual definido.

**VALIDACIÓN:** Compilar, ejecutar \[prueba manual\], informar errores y profiler si corresponde.

**ENTREGA:** Resumir archivos tocados, decisiones, parámetros editables, pasos de prueba y riesgos pendientes.

# ANEXO G. Milestones ejecutables de la V1

| **ALCANCE V1** | Cada milestone debe dejar una build o escena demostrable. Si un milestone no es estable, no se compensa agregando contenido del siguiente. |
|----------------|--------------------------------------------------------------------------------------------------------------------------------------------|

| **Milestone**             | **Entrega mínima**                                                                                                                             | **No entra todavía**                                      |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------|
| M0 — Bootstrap            | Proyecto Unity limpio, URP, Input System, Git, escena test, logging básico.                                                                    | Supervivencia, inventario, POIs.                          |
| M1 — Tren estable         | Vía simple, locomotora, caminar dentro, bajar/subir, props estables.                                                                           | Combustible real, mundo procedural.                       |
| M2 — Conducción           | Reversor, potencia, freno, parada y primer switch manual.                                                                                      | Automatización de cambios.                                |
| M3 — Interacción          | Focus/interact/hold, puertas, objetos transportables y feedback.                                                                               | Workbench completo.                                       |
| M4 — Supervivencia        | Vida, hambre, energía, temperatura; feedback mínimo; valores generosos para pruebas.                                                           | Balance final, hidratación separada.                      |
| M5 — Sondas               | Consola principal con 1–3 señales preparadas a mano + portátil para objetivo/tren.                                                             | Mapa tradicional, balizas avanzadas.                      |
| M6 — POI + combustible    | Estación de mantenimiento hecha a mano; puerta congelada, loot lógico, bidón/bomba; consumo y repostaje manual.                                | Procedural completo.                                      |
| M7 — Inventario + salvage | Inventario con peso; desmontaje de un conjunto pequeño de props con herramienta; recursos recuperados sin duplicaciones.                       | Carga avanzada (carros, cabrestantes) y vagón de almacén. |
| M8 — Recursos + cama      | Madera/metal/tela/componentes, almacenamiento simple, construcción parcial, cama mejorable y al menos otra mejora simple (p. ej., un estante). | Vagones especializados.                                   |
| M9 — Workbench            | Mesa básica con pocos recipes/blueprints; blueprint inicial/optativo y una herramienta/pieza fabricable.                                       | Rifle, taller avanzado.                                   |
| M10 — Rail gating         | Bifurcación dañada, proyecto de reparación con reservas y pieza especial.                                                                      | Regiones múltiples complejas.                             |
| M11 — Mini-región         | 2–4 POIs pequeños para reunir recursos del proyecto de vía; loop completo repetible hasta abrir la ruta.                                       | Procedural amplio, shopping, animales.                    |
| M12 — Save                | Guardar/cargar estados de jugador, tren, loot, build site y switch.                                                                            | Cloud saves.                                              |
| M13 — Slice polish        | Audio interior/exterior, iluminación, UI mínima, performance pass, final entrando a vía nueva.                                                 | Shopping, animales, procedural amplio.                    |

# ANEXO H. Referencias técnicas verificadas (octubre de 2026)

Estas referencias verifican decisiones de tooling; no sustituyen profiling ni pruebas del proyecto.

| **Fuente**                                           | **Qué se toma de ella**                                                                                                                                                      |
|------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Unity — Official Plugin for Claude Code (9 Sep 2026) | Plugin oficial de primera parte; integra skills de Unity y Unity CLI para Claude Code.                                                                                       |
| Unity Docs — About Unity's plugin                    | El plugin para agentes requiere Unity 6.0 o posterior.                                                                                                                       |
| Unity Manual 6 — URP GPU Occlusion Culling           | GPU Occlusion en URP utiliza GPU Resident Drawer; requiere Forward+ y plataforma/API con compute shaders; su beneficio depende de la escena.                                 |
| Anthropic Docs — Claude Code                         | Claude Code opera sobre el proyecto desde terminal, puede editar archivos, ejecutar herramientas y trabajar con Git; el flujo debe mantenerse acotado por tareas y permisos. |

**Nota para Claude:** si una API, paquete o recomendación cambió desde la fecha de este documento, verificar documentación oficial antes de implementarla y proponer la actualización; no cambiar el stack silenciosamente.
