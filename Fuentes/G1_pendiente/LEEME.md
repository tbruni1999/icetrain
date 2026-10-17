# G1: lo preparado y todavía no aplicado (pausa del 2026-10-17, pedido de Tomi)

Todo esto se escribió y se revisó fuera de Unity, pero **todavía no se aplicó ni se probó en el juego**. Para seguir, desde la carpeta del proyecto y con Unity fuera de Play:

1. Los modelos:
   - `modelos/Herramientas/*.fbx` → `Assets/_Project/Art/Herramientas/`;
   - cada `modelos/Props/<Nombre>/<Nombre>.fbx` → `Assets/_Project/Art/Props/<Nombre>/`.

   Después, correr el menú "Survival en tren > Arte > Crear materiales y conectar modelos". Los modelos salen de `Fuentes/Scripts_Manos/hacha_pico.py` y `Fuentes/Scripts_Props/picables.py`; las fotos de revisión son `hoja_picables.png`, `hacha_lado.png` y `pico.png`.
2. Las teclas 6 y 7:
   ```
   perl Fuentes/G1_pendiente/input_patch.pl < Assets/_Project/Data/Input/IceTrainInput.inputactions > tmp.json
   ```
   Si está bien, reemplazar `IceTrainInput.inputactions` por `tmp.json`.
3. El paso 3 (hacha y pico: casillas 6 y 7, modelos en la mano, golpe, guardado, recetas del banco y piedras sueltas junto a la vía):
   ```
   perl Fuentes/G1_pendiente/paso3.pl
   ```
4. El paso 5 (la cantera y el bosque muerto, con las recetas de picar):
   ```
   perl Fuentes/G1_pendiente/paso5.pl
   ```
5. Compilar, reconstruir las escenas (M2, M1 y menú) y correr `Tools/SpikeTests/g1_picar.cs` y `g1_pilas.cs`.
6. Correr la regresión completa. Varias pruebas viejas van a cambiar a propósito, porque ahora lo desarmado va a la mochila golpe a golpe y hay más objetos para picar en la escena. Esas se adaptan.

Después de eso, el paso 4: las montañas, con su propia niebla, más suave (DECISIÓN de Tomi). El plan está en `Docs/G1_Picar_Montanas_Plan.md` y en CLAUDE.md.
