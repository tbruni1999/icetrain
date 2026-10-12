# Survival en tren

Survival en primera persona en un mundo cubierto de nieve, donde el jugador vive y conduce un tren de mantenimiento. Proyecto en Unity 6 + URP + C#.

## Fuente de verdad
- El diseño completo está en `Docs/Documento_maestro_v0_5.md` (copia en Word: `Docs/Survival_en_tren_Documento_maestro_para_Claude_v0_5.docx`).
- Antes de implementar algo, leer las secciones que lo gobiernan y respetar las etiquetas: DECISIÓN no se cambia sin instrucción explícita; PROPUESTA es ajustable; ALCANCE V1 debe existir en la slice.
- Si hay duda entre agregar contenido y mejorar el loop actual, priorizar el loop.

## Cómo trabajar
- Hacer solo el milestone que se pide (hoja de ruta: sección 24 y Anexo G). Los sistemas futuros son contexto, no permiso para construirlos.
- Secuencia por tarea (Anexo F.1): revisar el repositorio, repetir el objetivo y lo que queda fuera, proponer archivos a tocar, implementar de forma incremental, compilar, probar, resumir cambios y riesgos.
- No actualizar Unity ni cambiar URP sin autorización. No agregar paquetes sin justificarlo. No crear managers globales ni singletons por conveniencia. No inventar lore, armas ni stats.
- Valores de balance en datos configurables, no escondidos en scripts. Sin PlayerPrefs para el guardado del mundo.
- No afirmar que algo está optimizado sin medirlo. No hacer commit salvo que se pida.
- Riesgo principal: el movimiento del jugador dentro del tren en marcha (Anexo C). No avanzar a sistemas grandes hasta aprobar ese spike.

## Sobre quien dirige el proyecto
Tomi es director de arte en 3D y diseño de producto digital, sin experiencia en programación. Explicar cada resultado en palabras simples, evitar jerga sin definirla, y para cada entrega decir paso a paso qué mirar o probar dentro de Unity.

## Milestone actual
M0 — Bootstrap. Ver `PRIMER_PEDIDO.md`.
