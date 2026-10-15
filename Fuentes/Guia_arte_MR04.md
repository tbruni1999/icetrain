# Survival en tren (nombre de trabajo)

Juego de supervivencia en primera persona dentro de un tren que avanza en clima extremo. Unity. Equipo de una persona: Tomi, director de arte.

## Cómo trabajar con Tomi

- Tomi no programa. Explicá en simple qué hiciste y cómo probarlo en el editor; no le pidas que lea o edite código.
- Antes de borrar archivos, reorganizar carpetas o cambiar muchos assets a la vez, contale el plan y esperá su OK.
- Preferí automatizar: si una tarea se repite (configurar 60 texturas, crear materiales, armar prefabs), hacé un script de editor en `Assets/Editor/` con un menú `Herramientas/...` en vez de pedirle pasos manuales.
- Commit de git antes de cambios grandes, con mensajes en español.
- Al empezar, averiguá qué render pipeline usa el proyecto (URP, HDRP o Built-in) y adaptá materiales y efectos a ese pipeline.

## Assets y rutas

| Qué | Dónde |
| --- | --- |
| Vagón MR-04, versión invierno | `Assets/Modelos/MR04/RedBlanca_MR04_v03_invierno.glb` (y `.fbx` como alternativa) |
| Librería de texturas (63 materiales) | `Assets/Texturas/MR04_Libreria/<categoria>/` con `catalogo.json` |
| Materiales de Unity | `Assets/Materiales/MR04/` (uno por material de la librería, mismo nombre) |

El GLB se importa con el paquete glTFast (`com.unity.cloud.gltfast`).

## Ejes y escala

1 unidad = 1 metro. La cabina mira hacia +Z en Unity (si queda al revés, rotar la raíz 180° en Y). La puerta derecha queda del lado derecho mirando hacia la cabina. El vagón mide unos 19,6 m.

## Vagón MR-04: piezas

Objetos fijos principales: `Carroceria_LOD0`, `Chasis_LOD0`, `Interior_LOD0`, `Cabina_LOD0`, `Muebles_LOD0`, `Nieve_LOD0` (techo), y los agregados de invierno: `Quitanieves_LOD0`, `Faldones_LOD0`, `Rejillas_Faroles_LOD0`, `Escape_Elevado_LOD0`, `Nieve_Costra_LOD0`, `Carambanos_LOD0`.

Piezas móviles (pivote ya ubicado en el eje correcto):

| Pieza | Pivote | Eje en Unity | Recorrido |
| --- | --- | --- | --- |
| `Puerta_Derecha`, `Puerta_Izquierda` | Bisagra, abajo | Y | 0° a 100°, abre hacia afuera (probar signo) |
| `Palanca_Freno`, `Palanca_Potencia`, `Palanca_Reversor` | Base | X | −30° a +30° |
| `Aguja_Velocimetro` (0–120 km/h), `Aguja_Combustible` (E–F) | Centro del dial | Z | Medir en escena el ángulo de 0 y del máximo |
| `Eje_Ruedas_1` a `Eje_Ruedas_4` | Centro del eje | X | Giro continuo según velocidad |
| `Tapa_Combustible` | Borde superior | Z | 0° a 90° |

- Empties `Luz_Techo_Delantera` y `Luz_Techo_Trasera` marcan dónde van las luces del techo.
- Static (luz horneada): todo menos las piezas móviles. Los meshes ya traen un segundo canal de UV para lightmaps (no generar otro).
- Colisiones: Mesh Collider en `Interior_LOD0` y `Carroceria_LOD0`; Box Colliders en muebles; nieve y carámbanos sin colisión.
- Si aparece un LOD Group con un solo nivel que hace desaparecer el vagón a distancia, quitarlo o poner Culled en 0 %.

## Materiales

Convención: un material por entrada de la librería, nombre `MT_<Nombre>` (ej. `MT_ChapaCorrugada`). Los props futuros llegan con esos mismos nombres y deben reutilizar estos materiales.

Cada material de la librería trae:

| Archivo | Uso en Unity | Import |
| --- | --- | --- |
| `_BaseColor` | Base Map | sRGB on |
| `_Normal` | Normal Map | Texture Type: Normal map (OpenGL, sin invertir verde) |
| `_MetalSmooth` | URP: Metallic Map (Smoothness Source = Metallic Alpha) y Occlusion Map. HDRP: Mask Map | sRGB off |
| `_MetalRough` | No se usa en Unity (es para Blender) | — |

`catalogo.json` tiene el tamaño real de cada textura (`escala_m`) y si tiene alfa.

Especiales:
- `MT_Rejilla`: Alpha Clipping.
- `MT_Vidrio`, `MT_VidrioSucio`, `MT_Escarcha` y los `decal_*`: transparentes; los decals van en Decal Projectors (en URP agregar la feature Decal al renderer).
- `MT_Hielo`: transparente, Base Color #BDD9EB con alfa ~0,6, smoothness 0,9.
- `MT_Emisivo` (estufa): Emission #FFD97A.

## Dirección de arte

- Estilo: low-poly semirrealista, PBR, desgaste "usado pero cuidado". Saturación baja; rojo, ocre y naranja solo como señales de lectura (interactuables, peligro).
- Luz: afuera gris azulado frío y niebla suave; adentro cálido y apagado; la estufa es el único punto naranja. Luces del techo puntuales cálidas (~3000 K, alcance ~4 m). Post: tonemapping, bloom bajo, SSAO, corrección leve hacia frío, viñeta leve.
- El tren se mueve: la nieve del exterior es de tren en marcha (pegada abajo y al frente), no montoncitos prolijos.
- Regla de props: lo que agarra o usa el jugador (barreta, latas, herramientas) nunca lleva nieve; lo estático a la intemperie sí. En los props, la nieve viene como objeto aparte (`Nieve_<Prop>_LODn`) para poder apagarla.

## Pendientes (en orden)

1. Importar el vagón y verificar ejes, escala, materiales y piezas móviles.
2. Crear automáticamente los materiales de la librería (script de editor).
3. Iluminación, niebla, probes y luz horneada del vagón.
4. Interacción: puertas, palancas, agujas, ruedas.
5. Clima: shader de nieve por normal para terreno y props (Shader Graph), decals, escarcha en vidrios ligada a la temperatura de la estufa, partículas (nieve, polvo, aire caliente, aliento).
