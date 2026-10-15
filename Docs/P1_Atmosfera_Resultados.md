# Producción P1 — Atmósfera y clima: resultados

Después de la V1 (el documento maestro termina en el M13), Tomi pidió "seguir produciendo el juego porque lo veo super verde" y eligió empezar por **atmósfera y clima**. Después pidió también **revisar la iluminación y las sombras** ("las sombras en general se ven recontra mal"). Base: sección 19, Anexo E.2 y E.3, y la guía de arte (`Fuentes/Guia_arte_MR04.md`). Todos los valores son PROPUESTA y están en datos.

## Qué hay

### Post-proceso (la guía de arte)
- Tonemapping (ACES), bloom bajo, viñeta leve y una corrección leve hacia frío, en `Data/World/Atmosfera_Postproceso.asset`.
- El feedback del cuerpo (M4: escarcha en los bordes con frío, oscuro con sueño) ahora **se suma** a esta base en vez de pisarla.
- **Sombras de contacto (SSAO)**: un efecto de URP que oscurece los rincones y las uniones.

### Cielo de invierno
- Un cielo propio (`Shaders/CieloNiebla.shader`): en el horizonte toma **el color de la niebla**, así la nieve lejana se funde con el cielo sin línea; arriba, gris azulado. El sol, a tamaño real con su halo. El día y la noche (M13) lo siguen manejando.

### Nieve que cae y viento (`Data/World/Atmosfera.asset`)
- Copos alrededor del jugador y rachas finas a ras del suelo; el viento sopla con ráfagas y la nevada sube y baja despacio con las horas.
- **Adentro del tren no nieva** (una caja invisible del tamaño del tren apaga los copos, aunque el tren se mueva), y en los galpones los techos los frenan.
- Con el tren andando, la nieve se suelta donde va a estar el jugador: se ve pasar por las ventanas.

### Escarcha en las ventanas del tren
- Dos capas por vidrio (la escarcha del borde y la que cubre todo, de la librería), del lado de adentro. **Con la calefacción andando queda un poco en los bordes; si el tren se enfría, crece hasta cubrir los vidrios**, y al volver la calefacción se derrite despacio.

### Iluminación y sombras (pedido de Tomi)
- **El serrucho de luz bajo el techo**: el sol se colaba por la unión fina entre el techo y las paredes del modelo. Ahora las cajas grises del tren (invisibles) hacen sombra: son gruesas y cerradas, y la luz entra y sale solo por las ventanas y las puertas.
- **Sombras del sol**: estaban con una sola "cascada" para 50 m (cada punto de sombra cubría ~2,4 cm) y sin sombras suaves: bordes en escalera. Ahora: 4 cascadas (mucho detalle cerca), sombras suaves, menos desfase.
- **Luces de adentro**: las dos del techo hacen sombra (la luz cálida sale a la nieve solo por las ventanas); una luz chica sobre el pupitre (la cabina quedaba muy oscura); y **el resplandor naranja de la estufa**, que tiembla como un fuego y se apaga si la calefacción se queda sin combustible.

### Suelo con relieve y restos en el horizonte (`Data/World/Terreno.asset`)
- El plano blanco pasó a un **terreno de Unity** con dunas suaves alargadas por el viento, con la textura de nieve de la librería (compacta junto a la vía, pisada cerca de los lugares).
- **Plano cerca de la vía (12 m) y de los lugares (14 m)**: lo que se camina no cambia. Y un tope de pendiente: si las dunas quedan más empinadas que 15°, se achatan (quedaron en 13,3°).
- **12 restos altos medio enterrados** lejos de la vía (torres de alta tensión, chimeneas, postes de una línea caída, un tanque de agua), que se recortan en la niebla.
- Para el terreno se activó el **módulo de terreno de Unity** (viene con Unity, no se descarga nada). Justificación (Anexo A.4, "paquetes Unity oficiales cuando resuelven una necesidad real"): da el relieve con detalle según la distancia, su colisión y la mezcla de texturas, y Tomi lo puede esculpir y pintar a mano en el editor.

## Mediciones

REGRESION_PRODUCCION_PLACEHOLDER

## Cómo probarlo vos en Unity (paso a paso)

1. Abrí `Assets/_Project/Scenes/Pruebas/M2_Conduccion` y dale Play.
2. **Adentro**: mirá la unión del techo con las paredes (ya no hay luz colada), la estufa naranja y las manchas de sol de las ventanas.
3. **Afuera**: bajá del tren; cae nieve, hay rachas a ras del suelo, el suelo tiene dunas suaves y a lo lejos se recortan torres y chimeneas en la niebla.
4. **Escarcha**: con **F1** (panel de pruebas) y **F2** (acelerar el tiempo), apagá la calefacción dejando el tanque vacío o abrí las puertas un rato: los vidrios se escarchan; al volver el calor, se despejan.
5. **De noche** (F2 hasta las 18:00): el cielo y la niebla se oscurecen juntos y las ventanas del tren iluminan la nieve.
6. Para ajustar: `Data/World/Atmosfera.asset` (nieve, viento, escarcha), `Data/World/Atmosfera_Postproceso.asset` (post-proceso), `Data/World/Terreno.asset` (dunas, zonas planas, restos; reconstruir la escena después) y `Data/World/Cielo.mat` (colores del cielo).

## Pendientes y riesgos
- **Tormentas** como sistema (con efecto en el juego), huellas en la nieve y montañas de fondo (Megascans: licencia sin verificar) quedan fuera.
- El terreno se genera al armar la escena: si Tomi lo esculpe a mano, reconstruir la escena lo pisa (habría que guardar el terreno aparte).
- La textura de nieve se repite a lo lejos; se puede mezclar una segunda escala más adelante.
