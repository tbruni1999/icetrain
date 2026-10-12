using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Spike
{
    /// <summary>
    /// Panel de lectura del spike M1 (esquina superior izquierda): velocidad del tren,
    /// tiempo por cuadro y teclas de prueba. Es una herramienta de medición, no el HUD
    /// del juego; vive en la carpeta Spike para poder borrarla entera al terminar.
    /// </summary>
    public class SpikeOverlay : MonoBehaviour
    {
        [SerializeField] private TrainTestDriver driver;

        private float _smoothedFrameMs;
        private GUIStyle _style;

        private void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            _smoothedFrameMs = _smoothedFrameMs <= 0f ? ms : Mathf.Lerp(_smoothedFrameMs, ms, 0.05f);
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 15,
                    padding = new RectOffset(10, 10, 8, 8)
                };
                _style.normal.textColor = Color.white;
            }

            string text =
                $"Velocidad: {driver.CurrentSpeedKmh:0} km/h   (objetivo {driver.TargetSpeedKmh:0})\n" +
                $"Cuadro: {_smoothedFrameMs:0.0} ms  (~{1000f / Mathf.Max(0.01f, _smoothedFrameMs):0} FPS)\n" +
                "Tren:  ↑ más rápido   ↓ más lento   B frenar\n" +
                "Jugador:  WASD caminar   Shift correr   mouse mirar   Espacio saltar\n" +
                "Esc libera el mouse; clic en Game para volver";
            GUI.Box(new Rect(12, 12, 540, 110), text, _style);
        }
    }
}
