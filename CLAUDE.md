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
M1 — Tren estable (spike del Anexo C). Plan aprobado por Tomi el 2026-10-12:
1. Vía en óvalo + locomotora greybox moviéndose — HECHO.
2. Jugador en primera persona (caminar, mirar, saltar) en el suelo — HECHO.
3. Jugador dentro del tren en movimiento (estrategia A: el tren se mueve por el mundo; sin parenting).
4. Props (cajas) estables en mesa/estante/piso + tecla de prueba para soltar cajas.
5. Bajar y subir del tren (detenido y a baja velocidad).
6. Estrategias B (mundo se mueve) y C (floating origin), medir las tres y que Tomi elija.
- Fuera de M1: controles reales del tren y cambios de vía (M2), sistema de interacción (M3), supervivencia, combustible, HUD, guardado, arte final.
- Decisiones: velocidad de prueba máx. 60 km/h (probar a 20/40/60). Locomotora 3 m x 19 m con zonas reservadas para la V1 (cabina, descanso, taller, entrada/carga, sala de máquinas, boca de combustible exterior), porque en la V1 es el único espacio habitable.
- Escena: `Scenes/Spike/Spike_TrenEstable.unity`, generada por el menú "Survival en tren > M1 > Construir escena Spike". Se regenera entera: cambiar el script `M1SpikeSceneBuilder.cs`, no la escena a mano.
- Commit + push a GitHub al final de cada paso (pedido de Tomi).

### Estado del M0: TERMINADO (2026-10-12)
- Editor fijado: 6000.6.5f1 (Unity 6.6). Paquetes: URP 17.6.0, Input System 1.20.0, y com.unity.pipeline 0.8.0-exp.1 (experimental; solo sirve para que Claude Code se conecte al Editor, se puede quitar sin afectar el juego).
- Verificado en la PC de Tomi: compila sin errores ni avisos, URP activo, Active Input Handling = Input System, `Bootstrap_Test` muestra las líneas `[Bootstrap]` en Play y responde a Espacio.
- Proyecto local: `E:\IceTrain\icetrain` (no usar la copia vieja en OneDrive).
- Conexión con Unity: plugin oficial de Unity para Claude Code + Unity CLI (`unity status`, `unity command ...`). Probar teclado/mouse en Play requiere que Tomi enfoque la vista Game: las teclas simuladas no llegan si Unity no tiene el foco.
- Siguiente: proponer el plan del M1 y esperar aprobación de Tomi antes de implementar.
