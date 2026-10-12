# M3 — Interacción: resultados

Escena `Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity` (menú "Survival en tren > M2 > Construir escena de conducción"). Pruebas automáticas en `Tools/SpikeTests/`.

## Qué hay

- **Base de interacción** (`IInteractable`, `PlayerInteractor`, `InteractionSettings`): mirar algo y ver una indicación mínima "E verbo" (sección 18: poco texto). Tocar E o **mantener E** con un anillo de puntos que se llena (ámbar). Si se suelta o se deja de mirar, se cancela. Si el jugador está ocupado (manejando o llevando algo), E lo suelta aunque mire a otro lado.
- **Pupitre**: "E tomar/soltar los controles" (toque). **Palanca del cambio**: "E mover el cambio", mantener 1 s (ajustable por palanca).
- **Puertas corredizas** (`SlidingDoor`) en los dos costados de la locomotora: E abre/cierra, hoja animada que cerrada bloquea el paso. No cierran si hay una persona o algo suelto en el hueco (se sacuden). Una zona de uso en el hueco permite cerrarlas desde adentro. En la escena del spike arrancan abiertas.
- **Llevar objetos** (`Carryable`, `PlayerCarrier`, `CarrySettings`): E tomar, E soltar, clic izquierdo lanzar. El objeto se sostiene empujándolo con velocidad (choca con paredes, no las atraviesa) y acompaña al tren en marcha. Soltar o lanzar sale con la velocidad del jugador. Más peso = caminar más lento; más de 25 kg = "demasiado pesado". Se suelta solo si queda trabado lejos de la mano 0,4 s.
- **Locomotora más ancha**: 3,4 m por afuera (era 3 m; pedido de Tomi).

## Mediciones (Editor, 60 FPS)

| Prueba | Resultado |
|---|---|
| `m3_puertas.cs`: caminar contra la puerta cerrada | se detiene en la pared (x = 1,35) |
| idem, abierta | sale (x = 5,48) |
| idem, cerrar con el jugador en el hueco | no cierra |
| idem, mirar el hueco desde adentro | "E cerrar", y cierra |
| `m3_llevar.cs`: tomar caja de 10 kg | caminar ×0,80 |
| idem, sostenerla 20 s con el tren en marcha | sigue en la mano; temblor visto desde la cámara 0,2 mm; se aleja de la mano hasta 13 cm |
| idem, soltarla en marcha | cae adentro del tren (velocidad máx. respecto del tren 4,7 m/s = la caída) |
| idem, lanzar caja de 3 kg | sale a 4,1 m/s respecto del tren |
| idem, 30 kg | "demasiado pesado" |
| idem, contra la pared | queda apoyada, no la atraviesa |

Regresión: `m2_cambio_y_parada.cs` (cambio, vía muerta con resorte, marcha atrás, parada a 0,15 m del cartel), `parado_en_caja.cs` (0,00 cm) y `recentrado.cs` (6 re-centrados sin saltos) siguen bien. Temblor máximo de cajas apoyadas en `recentrado.cs`: 3,8 mm (antes 1,1 mm); sigue siendo imperceptible, anotado por si crece.

## Pendientes y riesgos

- La indicación "demasiado pesado" aparece con "E" adelante aunque E no haga nada. Afinar el estilo cuando haya arte de UI.
- Al soltar los controles el jugador queda parado donde apretó E (no se sienta).
- Los sonidos de puertas, palancas y objetos son de M13; el aislamiento térmico de la puerta, de M4.
- El guardado del estado de puertas y objetos movidos es de M12 (el documento pide IDs estables).
- Las pruebas que usan teclado virtual (`bajar_subir.cs`, `pasar_sobre_cajas.cs`) necesitan la ventana de Unity con foco.
