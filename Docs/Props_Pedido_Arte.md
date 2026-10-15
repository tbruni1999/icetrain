# Pedido de props para el chat de arte

Para pasarle al chat de arte (el que hizo el vagón MR-04 y la librería). Son los objetos que el juego ya usa y todavía son cajas grises. Las medidas salen del juego: si el modelo las respeta, se conecta sin tocar nada de lo jugable. Mismas reglas que la entrega anterior (`Fuentes/LEEME_para_Claude_Code.md` y `Fuentes/Guia_arte_MR04.md`): FBX, metros, LOD0/1/2, materiales con los nombres de la librería (`MT_...`) y la nieve como objeto aparte (`Nieve_<Prop>_LODn`) solo en lo que queda a la intemperie.

**Ejes:** +Y arriba. Para los objetos de piso, el pivote va **abajo, en el centro de la base**. "Ancho" es X, "alto" es Y, "fondo" es Z.

## Lo que se desarma con la barreta (sección 9.2)
Se ven enteros hasta que se desarman; al terminar, desaparecen y dejan el material. No hace falta que se rompan por partes (eso queda para "desmontar a golpes", más adelante).

| Prop | Medida (ancho × alto × fondo) | Notas |
|---|---|---|
| **Pallet** | 1,2 × 0,14 × 1,0 m | Tres travesaños abajo y cinco tablas arriba. Madera a la intemperie (`MT_MaderaIntemperie`). Aparece en la estación, afuera junto al tren y en el refugio |
| **Armario metálico** | 0,8 × 1,9 × 0,5 m | Una puerta al frente (−Z) con manija. Pintura gris de maquinaria y óxido (`MT_PinturaGrisMaquinaria`, `MT_AceroOxidado`) |
| **Colchón viejo** | 1,8 × 0,18 × 0,8 m | En el piso, sucio. `MT_Colchon` |
| **Tablero eléctrico** | 0,6 × 1,3 × 0,3 m | De pie contra la pared, con una tapa y una luz chica apagada al frente (−Z) |

## Herramientas y equipo
| Prop | Medida | Notas |
|---|---|---|
| **Barreta** | ~0,8 m de largo, punta curva de 0,12 m | Cuelga de dos ganchos en la pared, junto a la puerta de atrás del tren. Sin nieve (pasa por las manos). Pivote en el agarre |
| **Perchero con el traje** | Tabla de 0,75 × 1,05 m contra la pared; traje colgado de ~0,3 × 0,95 × 0,12 m | El traje se ve colgado mientras no lo tenés puesto. **Naranja**: es una señal de lectura (guía de arte). Que el traje sea un objeto aparte (`Traje_Colgado`) para poder esconderlo |

## Lugares
| Prop | Medida | Notas |
|---|---|---|
| **Depósito de combustible** (estación) | Cilindro acostado de 1,3 m de diámetro y 3,4 m de largo, sobre dos patas, con una válvula con rueda al frente | Se bombea con la bomba manual. La rueda de la válvula en **rojo** (interactuable) |
| **Palanca de cambio de vía** | Base de 0,6 × 0,3 × 0,5 m; varilla de 1 m que gira sobre la base (eje X, ±35°) con mango; poste indicador de 1,9 m con un cartel de dos caras (0,6 × 0,4 m) que gira 90° | Dos: la del cambio que funciona y la de la **bifurcación dañada** (esa también en una versión **rota**: el muñón doblado sin varilla, `Palanca_Rota`) |
| **Aguja de cambio** (la pieza especial) | Una pieza de riel afinada, ~1,5 m de largo | Se lleva en la mochila (ocupa 1 × 3 casillas) y aparece sobre la mesa del taller. Sin nieve |
| **Vehículo abandonado** (camioneta) | 2,2 × 1,75 × 6,3 m (caja atrás, cabina, capó) | Medio enterrado; con nieve en el techo y el capó como objeto aparte. Tapa del tanque en el costado derecho (+X), detrás de la cabina, a 0,8 m de altura: de ahí se saca combustible con sifón |
| **Tope de vía muerta** | Ancho de vía (1,5 m) × ~1,2 m de alto | Con franjas de seguridad (`MT_FranjasSeguridad`) |

## Lo que ya está resuelto con la librería
Mientras tanto, el juego viste estos objetos con los materiales de la librería, a tamaño real (producción P2): las paredes de hormigón de la estación, la chapa corrugada del taller, las tablas del refugio, la vía (balasto, durmientes de madera, rieles oxidados), los postes y la nieve acumulada contra los lugares. Cuando lleguen los modelos, reemplazan a las cajas sin cambiar el juego.

## Pendiente de la entrega anterior
- **La consola de la sonda:** en el juego se movió a la pared lisa entre dos ventanas (z 5,9 a 6,7) y se subió 60 cm sobre una base de acero (pedido de Tomi: quedaba baja y sobre la ventana). Conviene mover la consola así en el `.blend`.
- **La cama y el estante reforzado** en piezas por fase (la cama: tela sucia en el piso; 4 tablas con patas; 3 partes de relleno; manta y almohada. El estante: 2 soportes, 2 postes, 2 tablas abajo y 2 arriba), para mostrar el avance con el arte.
