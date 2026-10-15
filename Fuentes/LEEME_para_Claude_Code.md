# Survival en tren — entrega de props y librería MR-04

Este paquete reúne todo lo hecho en el chat de props. Claude Code: leé este archivo antes de importar.

## Qué hay y dónde va en el proyecto de Unity

| Contenido | Dónde ponerlo | Notas |
|---|---|---|
| Librería de materiales (27 zips `libreria_mr04/*.zip`) | `Assets/Art/Texturas/MR04/` | Descomprimir **todos** en la misma carpeta; se unen solos (cada zip trae una categoría o parte). 63 materiales en 2K: 35 escaneados CC0 + 28 procedurales. `catalogo.json` tiene el tamaño real de cada textura (`escala_m`). |
| Props (`Lata_Conserva`, `Piolet`, `Bidon_Combustible_Rojo`, `Bidon_Combustible_Verde`, `Bomba_Manual`, `Bomba_Lanza`) | `Assets/Art/Props/<Nombre>/` | Importar el **FBX** (los LOD se arman solos por los sufijos `_LOD0/1/2`). El GLB y el .blend NO van en Assets. |
| `.blend` de cada prop | `Fuentes/Props/` (fuera de Assets) | Fuente editable. Si quedan dentro de Assets, Unity intenta importarlos con Blender. |
| `props/*.py` | `Fuentes/Scripts_Props/` | Recetas que generan cada prop con Blender (skill props-3d-invierno). Sirven para regenerarlos con más calidad más adelante. |
| `libreria/*.py` | `Fuentes/Scripts_Libreria/` | Pipeline que bajó y convirtió las texturas escaneadas (Poly Haven / ambientCG). |

## Ajustes de importación de texturas
- `*_Normal.png` → Texture Type: **Normal map**.
- `*_MetalSmooth.png` → **sRGB apagado**. R = metálico, G = oclusión, A = suavidad (formato de Unity Standard/URP Lit: "Smoothness source = Metallic Alpha").
- `*_MetalRough.jpg` → solo para glTF/otros motores; en Unity no hace falta.
- `*_BaseColor` → sRGB encendido. La oclusión ya viene horneada al 25 %.
- Los materiales son repetibles (tileables) a tamaño real; los props usan proyección de caja, así que no hay que tocar el tiling por objeto.

## Escala, ejes y pivotes
- 1 unidad = 1 metro, medidas reales.
- Objetos de mano (lata, piolet, bidones, bomba manual): pivote en el punto de agarre. Para apoyarlos en el piso hay que bajarlos hasta la base.
- Herramientas largas (piolet): mango a lo largo del eje Y.
- `Bomba_Manual_Palanca` es un objeto aparte con pivote en su eje: es la parte que se anima.
- `Bomba_Lanza`: pivote donde la lanza entra al suelo; apoyarla en el terreno deja lanza y estacas enterradas.
- Regla de nieve: lo que pasa por las manos del jugador no lleva nieve.

## Props entregados
| Prop | Tris LOD0/1/2 | Uso en el diseño |
|---|---|---|
| Lata_Conserva | 1454 / 710 / 290 | Ración (primer saqueo). Materiales propios de etiqueta incluidos en su carpeta Texturas. |
| Piolet | 528 / 376 / 104 | Herramienta para hielo (no está en el documento maestro todavía). |
| Bidon_Combustible_Rojo / _Verde | 604 / 484 / 104 | Combustible, trasvase manual. Misma malla, dos colores. |
| Bomba_Manual (+ Palanca) | 620 | Bomba manual vieja de la estación de mantenimiento. |
| Bomba_Lanza | 1774 / 1206 / 353 | Mejora: bomba eléctrica que se clava al suelo (etapa 3, workbench + batería). |
