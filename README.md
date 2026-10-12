# Survival en tren

Proyecto Unity 6 + URP + C#. El diseño completo está en `Docs/Documento_maestro_v0_5.md`.

## Versión fijada
- **Unity Editor: 6000.6.5f1** (Unity 6.6, la que Tomi instaló desde el Hub). Está escrita en `ProjectSettings/ProjectVersion.txt`.
- No se actualiza durante la vertical slice salvo necesidad demostrada.
- Paquetes: Universal RP 17.6.0 (deducida del número del Editor, sin verificar) y Input System 1.20.0 (la 1.14.2 falla en Unity 6.6; ver `Packages/manifest.json`). No hay otros.

## Primera vez que se abre
1. En Unity Hub: Add > seleccionar esta carpeta, con el Editor 6000.6.5f1.
2. Esperar a que Unity importe (la primera vez tarda unos minutos).
3. Menú **Survival en tren > M0 > Configurar proyecto (URP + Input System)**. Si pide reiniciar, aceptar.
4. Abrir `Assets/_Project/Scenes/Bootstrap/Bootstrap_Test.unity`, apretar Play.
5. En la pestaña Console deben verse mensajes `[Bootstrap]`; al apretar Espacio aparece otro.
