# Survival en tren

Proyecto Unity 6 + URP + C#. El diseño completo está en `Docs/Documento_maestro_v0_5.md`.

## Versión fijada
- **Unity Editor: 6000.3.1f1** (Unity 6.3 LTS). Está escrita en `ProjectSettings/ProjectVersion.txt`.
- No se actualiza durante la vertical slice salvo necesidad demostrada.
- Paquetes: Universal RP 17.3.0 y Input System 1.14.2 (ver `Packages/manifest.json`). No hay otros.

## Primera vez que se abre
1. En Unity Hub: Add > seleccionar esta carpeta, con el Editor 6000.3.1f1 (instalarlo desde Hub si falta).
2. Esperar a que Unity importe (la primera vez tarda unos minutos).
3. Menú **Survival en tren > M0 > Configurar proyecto (URP + Input System)**. Si pide reiniciar, aceptar.
4. Abrir `Assets/_Project/Scenes/Bootstrap/Bootstrap_Test.unity`, apretar Play.
5. En la pestaña Console deben verse mensajes `[Bootstrap]`; al apretar Espacio aparece otro.
