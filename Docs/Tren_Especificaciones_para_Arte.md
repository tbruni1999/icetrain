# Ficha del tren para modelado 3D

Para pasarle a quien modele el tren (otro Claude u otra persona). Todas las medidas salen del tren que **ya está jugándose en Unity** (el greybox: cajas grises que marcan el lugar de cada cosa). No son medidas de catálogo ni de un tren real: son las que el juego necesita para que el jugador camine, mire y use las cosas bien. Fecha: 2026-10-13 (hasta el M8; el M9 agrega el banco de trabajo con panel).

> **Palabra clave.** En el proyecto se le dice "locomotora de mantenimiento" o "el tren". Es **un solo vehículo habitable** (en la V1 es el único espacio donde se vive), no un tranvía de varios coches. Lo que importa para el modelo es la forma de caja con interior, puertas, ventanas y sus zonas.

---

## 1. Cómo se miden las cosas

- Unidades: **metros** (1 unidad de Unity = 1 m).
- Ejes del modelo (los de Unity): **+Z = adelante** (la cabina de conducción está adelante), **+X = derecha** mirando hacia adelante, **+Y = arriba**.
- Origen del modelo: **centro del tren (a mitad del largo y del ancho), a la altura de la parte de arriba del riel.** Todo lo de este documento está medido desde ahí: "altura 1,0" quiere decir 1 m por encima del riel.
- El riel está 0,6 m más alto que la nieve del suelo (balasto y durmientes), o sea el piso del tren queda a 1,6 m de la nieve.

## 2. Medidas generales

| Qué | Medida |
|---|---|
| Largo total | **19,0 m** (de z = −9,5 a z = +9,5) |
| Ancho exterior | **3,4 m** (de x = −1,7 a x = +1,7). Tomi lo pidió más ancho: con 3 m se sentía angosto |
| Alto total (desde el riel hasta lo más alto del techo) | **3,6 m** |
| Piso: parte de arriba | **1,0 m** (el piso mide 0,2 m de grosor: de 0,8 a 1,0) |
| Techo: parte de abajo | **3,4 m** (el techo mide 0,2 m de grosor: de 3,4 a 3,6) |
| **Alto interior** (piso a techo) | **2,4 m** |
| Paredes | **0,1 m** de grosor |
| **Ancho interior** | **3,2 m** (de x = −1,6 a x = +1,6) |
| Chasis (la caja de abajo) | de x −1,4 a +1,4, de altura 0,25 a 0,8, de z −9,3 a +9,3 |
| Bogies (carros con ruedas) | dos, centrados en z = **+6,5** y z = **−6,5**; cada uno 2,0 m de ancho × 2,4 m de largo, de altura 0,05 a 0,6 |
| Distancia entre bogies | 13,0 m |

**Vía** (por si se ve el tren sobre la vía): trocha estándar **1,435 m** entre rieles, riel de 0,07 m de ancho, durmientes de 0,25 m cada 1,2 m, balasto de 4 m de ancho.

## 3. Aberturas (puertas, ventanas, escalones)

Todo lo que sigue es igual en los dos costados salvo que se diga otra cosa.

| Qué | Dónde | Medida |
|---|---|---|
| **Puerta corrediza** (una por lado, derecha e izquierda) | centrada en z = **−6,0** | 1,0 m de ancho (z −6,5 a −5,5) × 2,1 m de alto (de 1,0 a 3,1). La hoja va por **afuera** de la pared y se corre hacia **atrás** (−Z) 1,05 m |
| Escalones bajo cada puerta | afuera, hacia el costado | **4 escalones**, cada uno sube 0,32 m y entra 0,28 m, del ancho de la puerta (1,0 m). El último escalón queda 0,32 m sobre la nieve |
| Ventana 1 | z −4,5 a −3,6 | 0,9 m de ancho |
| Ventana 2 | z −1,5 a +1,0 | 2,5 m de ancho |
| Ventana 3 | z +3,0 a +5,5 | 2,5 m de ancho |
| Ventana de cabina | z +7,2 a +9,0 | 1,8 m de ancho |
| Alto de todas las ventanas | de 1,9 a 2,8 m (o sea de 0,9 a 1,8 m sobre el piso) | |
| Parabrisas (pared de adelante) | de x −1,4 a +1,4 | de 1,9 a 2,9 m de altura |
| Pared de atrás | sin ventanas ni puerta | |
| **Boca de combustible** (tapa exterior) | costado **derecho**, sobre el **chasis** (x = 1,4), z = **−8,6**, centrada a 0,55 m de altura | tapa de 0,6 m de largo × 0,34 m de alto, sobresale 0,1 m del chasis; el jugador se para a su lado con un bidón |

## 4. Zonas del interior (de adelante hacia atrás)

Cada zona es un lugar pensado para un sistema del juego. **No hace falta respetar el diseño de los muebles de mi greybox, sí respetar el lugar y el tamaño aproximado**, porque el jugador camina entre ellos.

| Zona | Largo (z) | Qué hay | Medidas |
|---|---|---|---|
| **Cabina** | +6,8 a +9,5 | Pupitre de conducción en la pared de adelante | mueble de x −1,0 a +1,0, z 8,6 a 9,3, **alto 1,0 m sobre el piso** (parte de arriba a 2,0). Arriba tiene: 3 palancas (x −0,75 reversor, −0,45 potencia, −0,15 freno; z 8,85; varilla de 0,32 m + manija), **velocímetro** de aguja (x +0,3, cara de 0,3 m de diámetro), **medidor de combustible** (x +0,72, cara de 0,22 m) |
| | | Asiento | x 0,6 a 1,1, z 7,6 a 8,1, alto 0,5 m |
| | | **Consola del radar/sonda** (pantalla CRT) contra la pared **izquierda** | mueble x −1,6 a −1,0, z 6,8 a 7,6, **alto 1,2 m** sobre el piso; pantalla de 0,54 × 0,44 m con carcasa |
| **Descanso** | +3,5 a +5,5 | **Cama** contra la pared **derecha** | colchón de 0,88 m de ancho × 1,96 m de largo y 0,08 m de grosor, apoyado en el piso (con las mejoras se le suma un bastidor de tablas y patas y el plano de dormir sube a ~0,3 m); centro en x +1,15, z +4,5 |
| | | Calefacción contra la pared izquierda | x −1,6 a −1,0, z 3,8 a 4,4, alto 0,9 m |
| **Taller** | −2,5 a +3,4 | **Banco de trabajo** contra la pared **izquierda** | x −1,6 a −0,8 (0,8 m de profundidad), z −1,0 a +1,0 (2,0 m de largo), **alto 0,9 m**. Encima: lugar para fabricar (el bidón sale en el medio, z ≈ +0,1). La sierra fabricada se ve colgada en la pared izquierda, a 1,3 m del piso y z ≈ −2,4 (tramo macizo entre las ventanas; sobre el banco hay ventana) |
| | | **Estante precario** contra la pared **derecha** | profundidad 0,4 m, z −2,5 a −0,5, alto 1,8 m, **3 tablas** a 0,1 / 0,7 / 1,3 m sobre el piso |
| | | **Estante reforzado** (se construye por fases) contra la pared izquierda | z +1,4 a +3,4, profundidad ~0,35 m, alto ~1,6 m, 2 tablas (0,5 y 1,1 m); los postes y soportes se ven antes de terminarse |
| **Entrada / carga** | alrededor de −6,0 | Puertas laterales, escalones, lugar para dejar y recibir cosas | pasillo central libre |
| **Sala de máquinas** | −9,5 a −7,5 | Separada del resto por un tabique en z = −7,5 (0,1 m de grosor, de piso a techo); adentro, un bloque motor/tanque | bloque x −1,2 a +1,2, z −9,3 a −7,7, 2,0 m de alto. **El jugador no entra** (el tabique no tiene puerta todavía) |
| Luces interiores | z +4,0 y z −2,5 | dos luces en el techo | a 0,3 m del techo |

Pasillo libre: entre el banco (a la izquierda) y el estante precario (a la derecha) quedan unos **2,0 m** de paso. No cerrarlo más: el jugador ocupa 0,6 m de ancho, y a veces lleva una caja en las manos.

## 5. Piezas que se mueven o se usan (importante para el modelo)

Estas piezas hay que modelarlas **separadas del resto** para que Unity las pueda mover o apuntar. Cada una con su **pivote** (el punto sobre el que gira o se desliza) donde se indica:

| Pieza | Cómo se mueve | Pivote |
|---|---|---|
| Hoja de cada puerta (2) | se desliza 1,05 m hacia atrás (−Z) por fuera de la pared | centro de la puerta, a nivel del piso |
| Palancas del pupitre (3) | giran adelante/atrás | en la base de la palanca |
| Aguja del velocímetro y del medidor de combustible | giran sobre el eje del frente de la cara | centro de la esfera |
| Pantalla del radar | solo el vidrio (la imagen la dibuja el juego) | — |
| Tapa de la boca de combustible | se abre al usarla (por ahora solo se apunta) | borde de la bisagra |
| Cama: colchón, base y relleno | se van agregando por fases | origen de cada pieza donde apoya |
| Estante reforzado: soportes / estructura / tablas | aparecen una fase a la vez | origen de cada pieza donde apoya |
| Sierra colgada | aparece cuando se fabrica | en la pared |
| Bidón (objeto suelto, ~0,3 × 0,46 × 0,16 m) | se lleva en la mano, rueda y se cae | centro de la base |

## 6. Medidas del jugador (para que nada quede imposible)

- Cuerpo: **1,8 m de alto, 0,6 m de ancho** (radio 0,3). Ojos a **1,65 m**.
- Sube escalones de hasta **0,4 m**. Salta 0,9 m.
- Alcance para usar cosas con E: **2,2 m** (hay que verlas, apuntando con la mira).
- Camina a 3,5 m/s.
- Por eso: pasillos de al menos **0,8 m**, nada colgando por debajo de **2,0 m** en los lugares de paso, y escalones que no pasen de 0,35 m.

## 7. Reglas para quien modele

**No se cambia sin avisarme** (porque el juego, las pruebas y el diseño dependen de estos números):

1. El **tamaño de la caja**: 19 × 3,4 × 3,6 m, piso a 1,0 m, alto interior 2,4 m.
2. **Puertas** (1,0 × 2,1 m, en z = −6,0), **escalones** (4 de 0,32 m) y **posición de los bogies** (z = ±6,5).
3. **Dónde queda cada zona** (cabina adelante, descanso al medio, taller, entrada atrás, sala de máquinas al fondo) y de qué lado de la pared va cada mueble grande.
4. El **piso plano y continuo** de punta a punta (el jugador camina y salta ahí; el tren se mueve mientras tanto).

**Libre para diseñar:**

- La forma y el estilo de afuera (paneles, remaches, óxido, parachoques, frente de la locomotora), siempre que no se salga mucho de la caja ni agrande el espacio interior.
- Los muebles y las máquinas del interior (el greybox son cajas: se pueden reemplazar con diseño de verdad) respetando lugar y medidas.
- Texturas y materiales. Ideas del juego: interior **cálido**, exterior **frío**; sobrio, que se lea el uso (sección 18.4 del documento maestro).

**Para el rendimiento:** no hay un tope de polígonos escrito todavía (el documento maestro, Anexo E, dice que la inmersión se compra con materiales, escala y luz, no con densidad de polígonos). Recomendación mientras no se mida: modelos limpios, pocos materiales por pieza, nada de geometría que no se vea.

## 8. Cómo se entrega y cómo se conecta con el juego

1. Exportar en **FBX** (o el formato que acuerden), en metros, con los ejes de la sección 1, y con las piezas de la sección 5 separadas y con nombres claros (en español, como en el proyecto: `Puerta_Derecha`, `Palanca_Potencia`, `Aguja_Velocimetro`, etc.).
2. Ponerlo en `Assets/_Project/Art/Tren/` (carpeta nueva) y pasarme los nombres de las piezas.
3. **Yo hago la conexión**: el tren de hoy (greybox) lo genera un script; mi propuesta es **dejar las cajas grises como colisión invisible** (para que la física del jugador no cambie y no se rompa lo medido) y poner el modelo de arte como lo que se ve, conectando puertas, palancas y agujas a sus piezas. Antes de cambiar nada hago una prueba y te aviso.
4. Cada vez que el modelo cambie de medidas, avisarme para volver a correr las pruebas (caminar dentro del tren en movimiento, bajar y subir por las puertas, llevar objetos).

## 9. Para comparar con la base 3D que ya tiene Tomi

Con la base (la que tiene banco, radar, palancas y puertas), mirar estos números primero; son los que más rompen cosas si no coinciden:

- [ ] Largo 19 m, ancho exterior 3,4 m, alto 3,6 m.
- [ ] Piso a 1,0 m del riel; alto interior 2,4 m.
- [ ] Puerta de 1,0 × 2,1 m, centrada a 6 m del centro hacia atrás.
- [ ] Banco de trabajo contra la izquierda: 2,0 m de largo, 0,8 de profundidad, 0,9 de alto.
- [ ] Pupitre de 2,0 × 0,7 m con las palancas a 1,0 m del piso.
- [ ] Consola del radar contra la izquierda, a 1,2 m del piso.
- [ ] Orientación: cabina en +Z, derecha en +X, origen en el centro a la altura del riel.

Si algo no coincide no es "error": decime cuál es el número de la base y lo ajustamos de un lado o del otro. Lo que **no** se puede es que cada uno corrija por su lado sin avisar.
