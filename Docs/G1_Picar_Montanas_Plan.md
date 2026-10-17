# G1 — Picar el mundo, con las montañas y las zonas (plan)

Es el paso que sigue en el orden que eligió Tomi el 2026-10-16: primero la Parte 1b (nieve sin repetición, hecha), después G1 y después G2. El plan general y la lista completa están en `Docs/Plan_Grindeo_7Days.md` y `Docs/Grindeo_Lista_Completa.md`.

## Qué es G1 en una línea
Bajar del tren y **picar** lo que hay (rocas, árboles muertos, autos, chatarra, postes). Cada golpe da un poco de material directo a la mochila. Las rocas están al pie de una **cordillera** que rodea la región.

## Qué queda afuera de G1
- **Para G2:** crafteo desde la mochila con cola, herramientas de hierro y de forja, desgaste, panel de crafteo nuevo.
- **Para G3:** fundir en la forja.
- **Para G4:** partes del tren por niveles.
- **Para G5:** rutas bloqueadas.
- **Para después:** saqueo de contenedores, sonidos reales, arte final (Claude hace versiones simples).

## Los pasos
1. **Materiales nuevos y pilas en la mochila.**
   - Materiales: piedra, mineral de hierro, carbón, cable y piezas mecánicas, con su peso y tamaño de pila en datos (lista, sección 2.1).
   - Chatarra: queda la de H3.
   - Pilas: la cuadrícula se queda, pero un material ocupa una casilla con, por ejemplo, hasta 25 de madera (decisión de Tomi). Las herramientas y los objetos grandes siguen ocupando su forma.
2. **Lo "picable"** (lo que hoy se desarma a golpes, generalizado):
   - Cada cosa tiene vida en golpes, de qué está hecha y qué herramienta le sirve. Con otra herramienta pega poco o rebota.
   - **Cada golpe suma un poco a la mochila**, con un número que sube ("+1 piedra"). Si no entra, cae como bulto. Al romperse sale el resto.
   - Muchos golpes y poco por golpe, como en 7 Days. Los valores van en datos.
   - Lo picado no vuelve (decisión de Tomi) y se guarda en la partida.
   - Pallets, armarios, colchones, tableros y chatarra pasan a este sistema.
3. **Hacha y pico improvisados, como al empezar en 7 Days.**
   - **Piedras sueltas** en el piso, que se juntan con E.
   - En el banco del tren (el de hoy, con su tiempo): **hacha** (2 piedras + 1 madera + 1 tela) y **pico** (3 piedras + 1 madera + 1 tela).
   - Van en las casillas de la barra de herramientas, con golpe propio en primera persona y brazos.
   - En G2 pasan al crafteo desde la mochila.
4. **Las montañas.** Terreno más grande, de unos 6 km:
   - La zona de juego de hoy queda igual: el centro de 3 km con la vía, los lugares y las pruebas.
   - Alrededor, al norte y al oeste, una cordillera de 150 a 400 m de alto.
   - Roca donde la pendiente es fuerte y nieve en lo plano, con las rocas bajadas hoy (MT_RocaOscura y MT_RocaGris).
   - La montaña va en otro terreno, con sus propias 4 capas. Con más de 4 capas, URP agrega una pasada extra que no tiene la nieve sin repetición.
5. **Las zonas** (con nieve vacía entre medio, como pide el documento):
   - **Cantera** junto a la vía: rocas fáciles, algunas con veta de hierro, y un galpón.
   - **Pie de la montaña:** rocas grandes con vetas de hierro y de carbón.
   - **Bosque muerto:** árboles secos, troncos caídos y tocones.
   - **Chatarrería:** autos, camionetas y pilas de chatarra. Dan chatarra, piezas mecánicas, cable y el combustible de hoy.
   - **A lo largo de la vía:** postes y señales caídas, que dan cable y chatarra.

   Las zonas aparecen como señales en la consola de la sonda, como los lugares de hoy.
6. **Pruebas y documento:**
   - picar cada tipo con la herramienta buena y con la mala;
   - que el total de material sea el esperado, sin duplicados;
   - pilas;
   - guardar y cargar lo picado a medias;
   - un viaje completo: ir a la cantera, picar, volver y descargar;
   - el rendimiento con las montañas, medido;
   - la regresión completa.

## Decisión de Tomi que hace falta (antes del paso 4)
**La niebla tapa las montañas.** Con la niebla de hoy, a 200 m se ve solo el 4 % y a 300 m nada. Una cordillera a 1,5 km no se vería nunca. Opciones:

1. **Montañas con su propia niebla, más suave (recomendado).** Se ven como siluetas azuladas recortadas contra el cielo, le dan escala al mundo, y la niebla de cerca queda igual.
2. **Niebla según el clima:** días despejados en que se ven las montañas y días de niebla cerrada. Es casi el sistema de tormentas, que hoy está fuera.
3. **Dejar la niebla como está:** las montañas se ven recién al acercarse a su pie.

Los pasos 1, 2, 3 y 5 no dependen de esta decisión. Se avanza con ellos mientras tanto.

## Archivos que se tocan (propuesta)
- **Datos:** `Data/Items/` (materiales nuevos y recetas de picar), `Data/Equipment/Herramientas.asset` (hacha y pico) y `Data/World/Terreno.asset` (la montaña).
- **Código:**
  - `Items/ItemGrid.cs`, `Backpack.cs` e `InventoryPanel.cs` (pilas);
  - `Items/SalvageProp.cs` y `SalvageRecipe.cs` (lo picable, material por golpe);
  - `Equipment/PlayerGear.cs` y `ToolBelt.cs` (hacha y pico);
  - un componente chico para el número que sube;
  - `Save/SaveSystem.cs` (pilas).
- **Constructores de escena:** `TestScenesBuilder.Atmosphere.cs` (montaña), `TestScenesBuilder.Places.cs` (zonas), `TestScenesBuilder.Viewmodel.cs` (hacha y pico en la mano) y `Fuentes/Scripts_Manos/` (modelos simples de hacha y pico).
- **Pruebas:** `Tools/SpikeTests/g1_*.cs`.
