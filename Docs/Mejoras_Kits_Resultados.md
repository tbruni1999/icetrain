# Mejoras nuevas: banco con tiempo, kits y muebles que se colocan

Decisiones de Tomi del 2026-10-16:
- "La idea es que puedas sobrevivir sin tener las mejoras."
- "No me gusta eso de que aparezcan las cosas para mejorar en azul. Le quitan inmersión."
- "Lo mejor va a ser que se mejoren en el workbench y tengan un timer."
- "Me gustaría instalarla yo, pero a su vez me gustaría que algunas piezas, por ejemplo los estantes, las pueda instalar como yo quiera … que aparezca como lo que va a ocupar y yo ponerla como quiera … y que exista una herramienta que te deje moverla."

## Qué cambió

### 1. No hay más fantasmas azules
- Las obras del tren (la cama y la forja) y el arreglo de la bifurcación ya no muestran piezas translúcidas.
- Lo que todavía no se instaló, directamente no se ve.
- Mirando la cama sin nada en la mano, solo dice "dormir". Ya no dice qué le falta.

### 2. El banco de trabajo arma las mejoras, con un tiempo
- La lista del banco ahora tiene, además de las herramientas, **el kit de la próxima fase de cada obra**:
  - cama improvisada: base de madera, después relleno, después manta;
  - forja: hogar, después fuelle, después yunque;
  - bifurcación oeste: palanca, después durmientes y riel, después aguja de cambio;
  - y el **estante reforzado**, si ya leíste su plano en la estación.
- **Los materiales se gastan al empezar.** El banco sigue solo con el reloj del juego (30 minutos del juego por kit, 40 el estante; con el reloj normal es más o menos medio minuto real). Podés irte, manejar o dormir mientras tanto.
- **Arriba del panel se ve qué está armando y cuánto falta**, con un botón CANCELAR (o la tecla Retroceso). Cancelar devuelve los materiales a la mochila.
- **Mirando el banco**, la indicación también dice qué está armando y cuántos minutos faltan.
- Se arma una cosa por vez. Se puede adelantar: mientras la base de la cama todavía está en el piso sin instalar, el banco ya ofrece el relleno.
- **El banco usa lo que llevás en la mochila, lo que está guardado en los estantes del tren y los bultos que dejaste en el piso del tren.** Gasta primero lo de la mochila. Hacía falta: los 6 tablones y 2 chapas del estante no entran juntos en la mochila (6 x 5 casillas), y el estante precario es chico. Así se juntan materiales en varios viajes, como antes con la obra a medias: volvés, dejás todo en el tren y el banco lo usa.
- Las herramientas (sierra, bidón, martillo, llave) se siguen fabricando en el momento, con la barra de progreso.

### 3. El kit
- Cuando termina, aparece **un cajón de madera con flejes de chapa** en el piso, al lado del banco (hacia la puerta del taller).
- Si ese lugar está ocupado, espera. La indicación dice "listo, falta lugar al lado del banco".
- **Pesa lo que pesan sus materiales**: la base de la cama 8 kg, el estante 19 kg. Te frena al caminar, como cualquier cosa en la mano.
- Se lleva con las dos manos (E para tomarlo), **a la altura de la cintura** (así se ve por encima dónde va) y **no se lanza**: el clic es para instalar.
- Si lo soltás, queda donde cayó. Se guarda con la partida.

### 4. Instalar en su lugar: la cama, la forja y la bifurcación
- Con el kit en la mano, mirá la obra.
- La indicación dice "mantener para instalar: base de madera · cama improvisada".
- Mantené E 2,5 segundos. La fase aparece entera, ya hecha, y suena el golpe de obra.
- Si el kit es de una fase que todavía no toca, dice "primero va: …".
- **En la bifurcación** el hielo tiene que estar roto antes de instalar.
- Las fases van en orden. Al terminar la bifurcación, la palanca funciona como antes.

### 5. Estantes donde quieras
- Con el kit del estante en la mano **se ve el estante como va a quedar**:
  - **claro y transparente** si entra;
  - **rojo** si choca con algo o no está adentro del tren.
- Arriba de la mira dice por qué no entra.
- Es la única vista previa del juego y solo aparece mientras estás colocando algo.
- **Mirando el piso**, el estante se para ahí, de frente a vos. La **rueda del mouse lo gira** (15° por paso).
- **Mirando una pared**, se apoya contra ella, con la espalda contra la pared.
- **Clic**: queda instalado.
- Es un mueble parado de 1,6 m de ancho, 1,6 de alto y 42 cm de fondo, con cuatro tablas. Guarda **6 x 4 casillas** (lo mismo que el estante reforzado terminado del M8). Lo guardado se ve en las tablas.
- Se pueden tener varios.
- **Lugares reservados**: no se puede poner un estante donde el banco deja los kits, sobre el lugar de la cama, de la forja ni del yunque (aunque todavía no se vean). Dice "ahí deja el banco lo que arma" o "ahí va la cama improvisada".
- Dónde entra hoy en el tren (medido): la pared izquierda pegada al banco (hacia la cama), la pared derecha frente al banco y la pared izquierda del lado de la entrada. El tren es angosto y casi todo lo demás ya está ocupado.
- PROPUESTA mía: solo se colocan adentro del tren (el tren es la casa).
- El estante precario del taller queda fijo: es parte del tren.

### 6. La llave de montaje (casilla 5)
- Se fabrica en el banco con **1 chapa**. Se sabe desde el principio. Es una PROPUESTA: Tomi pidió "una herramienta"; la receta se ajusta en `Data/Crafting/Plano_LlaveMontaje.asset`.
- Con la llave en la mano, **un golpe a un estante colocado lo levanta, con todo lo que tiene adentro**.
- Se coloca igual que desde el kit (piso o pared, la rueda gira, claro o rojo) y **otro clic lo deja**.
- Guardar la llave o cambiar de herramienta lo devuelve a donde estaba.
- La barra de abajo ahora tiene 5 casillas: 1 barreta, 2 sierra, 3 sonda, 4 martillo, 5 llave.

### 7. Sobrevivir sin mejoras
- Nada de esto es obligatorio para sobrevivir.
- La cama sin mejorar sigue recuperando 6 de energía por hora.
- El estante precario y la mochila alcanzan para guardar.
- La forja solo hace falta para las planchuelas.
- La bifurcación es el objetivo de la slice, no una mejora.

## Datos (todo ajustable sin tocar código)
- Tiempo del banco y tiempo de instalar por obra: `Data/Build/Obra_*.asset`. Campos "work Minutes" (30) e "install Seconds" (2,5). Cada fase puede tener su propio tiempo.
- El estante: `Data/Crafting/Plano_EstanteReforzado.asset`. Pide chapa ×2 y madera ×6, que es lo mismo que pedían las cuatro fases de la obra del M8. Tarda 40 minutos. El mueble está en `Prefabs/Items/Estante_Reforzado.prefab`.
- El kit: `Prefabs/Items/Kit_Mejora.prefab`.
- La llave: `Data/Crafting/Plano_LlaveMontaje.asset` y la casilla 5 en `Data/Equipment/Herramientas.asset`.
- Colores de la vista previa: `Materials/Greybox/Colocar_Bien.mat` y `Colocar_Mal.mat`.

## Guardado
- Lo que el banco está armando se guarda, con los minutos que le faltan.
- Los kits sueltos se guardan con su lugar.
- Los estantes colocados se guardan con su lugar, su giro y lo que tienen adentro.
- Las partidas viejas cargan igual: si tenían una obra a medio aportar, lo aportado suelto se ignora y la fase vuelve a pedir su kit.

## Lo que salió del juego
- El aporte de materiales manteniendo E en una obra.
- Las piezas fantasma (`Fantasma.mat`).
- La obra del estante reforzado en la pared izquierda del taller. Ese lugar quedó libre para poner estantes.

## Pruebas (2026-10-16)

### `h6_mejoras.cs` (nueva, en `regresion.sh`)
- **El banco**: ofrece sierra, bidón, martillo, llave, "cama improvisada: base de madera (30 min)", "forja: hogar (30 min)" y "bifurcación oeste: palanca (30 min)". El estante aparece recién al leer su plano.
- **La cama sin mejorar**: se ven 0 de sus 9 piezas (sin fantasmas).
- **Kit de la cama**:
  - los 4 tablones salen de la mochila al empezar;
  - mirando el banco dice "armando base de madera · cama improvisada (30 min)";
  - después de 1,2 s reales faltaban 28,5 min;
  - saltando media hora del reloj (como al dormir) el kit sale en el lugar exacto, con 8 kg.
- **Instalar**: con el kit en la mano dice "soltar (mantené E donde va)". Mirando la cama dice "mantener para instalar: base de madera · cama improvisada". Mantener E con la tecla de verdad lo instala: recuperación de 6,0 a 7,5 por hora, y el kit desaparece.
- **Estante**:
  - Con 4 tablones y 2 chapas en la mochila y 2 tablones en el estante del taller, el banco lo arma (19 kg).
  - Con el kit en la mano aparece la vista previa.
  - Mirando la mesada del banco: "va en el piso, no encima de otra cosa", y el clic no lo pone.
  - Contra la pared izquierda entra.
  - La rueda lo gira 30° en dos pasos.
  - El clic (de verdad) lo instala adentro del tren, con su ID y su cuadrícula de 6 x 4.
- **Llave**:
  - Un golpe al estante lo levanta y el clic lo deja contra la pared de enfrente, con las 3 piezas guardadas adentro.
  - Guardar la llave a mitad de un movimiento lo devuelve a su lugar.
  - Clics que hubo que repetir: 0.
- **Guardado**: guarda el kit a medio armar en el banco (con los minutos que faltan), el estante colocado (del jugador, con su lugar) y lo guardado adentro.

### Regresión de las 16 pruebas que tocan el banco o las obras
- Todas pasan. Las que usaban "aportar con E" ahora arman los kits en el banco y los instalan.
- `m8_obras_y_cama`: 3 fases con kits y E de verdad. Durmiendo 8 h, la energía sube +48, +60, +72 y +88. Cuentas exactas.
- `m8_almacenamiento`: el estante reforzado se arma con lo de la mochila y del estante del taller, se coloca y guarda 6 x 4.
- `m8_ciclo_completo`: lo que no entra en los estantes se deja en el piso del tren. Termina en **4 viajes**, con la cama terminada y el estante colocado. Cuentas exactas.
- `m9_planos`, `m9_banco`, `m9_taller_ciclo`: iguales que antes. El banco ofrece el estante solo con el plano.
- `m10_bifurcacion`, `m10_reparacion`, `m10_sonda_red`: congelada no se puede instalar. Con el hielo roto, las 3 fases con kits dejan el cambio REPARADO y la consola dice ABIERTA. Cuentas exactas.
- `m12_guardado`: el estado incluye el banco armando la palanca, el kit del relleno en el piso y el estante colocado con una tela. **IGUAL** en las tres comparaciones.
- `m13_sonido`: suenan la palanca, el hielo, el desarme, la obra, el banco y el plano.
- `h1_herramientas`: 5 casillas (se sumó LLAVE).
- `h4_forja`: la forja se arma con 3 kits y funciona igual que antes.
- `m10_slice`: de cero a la ruta desconocida en **341 s reales**. El banco tardó 90 s en armar los 3 kits.
- `m11_loop`: el recorrido completo tarda **370 s reales** (antes 278). Son los 90 s del banco: el tiempo de armar es justamente lo nuevo. Siguen 16,7 ms por cuadro en los tres viajes.

### Problemas que aparecieron y se arreglaron
- El lugar donde sale el kit rozaba la pared y el banco "esperaba lugar" para siempre. Se achicó el chequeo.
- **Un estante colocado podía tapar el lugar del kit**, o quedar encima del lugar de la cama o del yunque, que todavía no se ven. Ahora esos lugares están reservados.
- Mirando la mesada, el estante "entraba" encima del banco. Ahora va solo en el piso.
- **El mismo clic que deja un estante con la llave lo volvía a levantar.** Ahora la llave no golpea hasta soltar el botón.
- El cajón en la mano tapaba la vista previa. Ahora va a la cintura.
- Al instalar un kit, el editor de Unity a veces dibujaba la indicación de algo recién destruido (solo pasa en el editor). Ahora se ignora.
- En las pruebas: quedaban mouses virtuales de corridas cortadas, con el botón trabado, y ningún clic nuevo contaba. El lanzador (`run_test.sh`) ahora los saca y reinicia las acciones antes de cada prueba.

## Fotos (`Docs/Capturas_Mejoras`)
1. El cajón del kit al lado del banco.
2. El panel del banco: arriba lo que está armando, con los minutos que faltan y CANCELAR; a la izquierda las mejoras con su tiempo.
3. El estante en vista previa contra la pared (claro: entra).
4. El estante en rojo contra el banco: "no entra: choca con algo".
5. El estante colocado, con lo guardado a la vista.
6. La cama con la base y el relleno instalados: sin fantasmas, solo "dormir".
