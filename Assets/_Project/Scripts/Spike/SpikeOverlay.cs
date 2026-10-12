using SurvivalEnTren.Player;
using SurvivalEnTren.Train;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Spike
{
    /// <summary>
    /// Panel de lectura del spike M1 (esquina superior izquierda): velocidad del tren,
    /// tiempo por cuadro, mediciones de estabilidad y teclas de prueba. Es una herramienta
    /// de medición, no el HUD del juego; vive en la carpeta Spike para poder borrarla entera.
    /// </summary>
    public class SpikeOverlay : MonoBehaviour
    {
        [SerializeField] private TrainTestDriver driver;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private SpikeRideMetrics metrics;
        [SerializeField] private SpikePropMetrics propMetrics;
        [SerializeField] private InputActionReference resetMeasure;

        private float _smoothedFrameMs;
        private GUIStyle _style;

        private void OnEnable()
        {
            resetMeasure.action.performed += OnResetMeasure;
            resetMeasure.action.Enable();
        }

        private void OnDisable()
        {
            resetMeasure.action.performed -= OnResetMeasure;
        }

        private void OnResetMeasure(InputAction.CallbackContext _)
        {
            propMetrics.ResetAnchors();
        }

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

            string reference = motor.Platform != null ? "en el tren" : motor.IsGrounded ? "en el suelo" : "en el aire";
            string stability = metrics.OnPlatform
                ? $"Temblor: {metrics.JitterMillimeters:0.0} mm (máx. 2 s)   Deriva quieto: {metrics.DriftCentimeters:0.0} cm en {metrics.StillSeconds:0} s"
                : "Temblor / deriva: se miden estando en el tren";

            string text =
                "Estrategia A: el tren se mueve por el mundo\n" +
                $"Velocidad: {driver.CurrentSpeedKmh:0} km/h   (objetivo {driver.TargetSpeedKmh:0})\n" +
                $"Cuadro: {_smoothedFrameMs:0.0} ms  (~{1000f / Mathf.Max(0.01f, _smoothedFrameMs):0} FPS)\n" +
                $"Jugador: {reference}\n" +
                stability + "\n" +
                $"Cajas: {propMetrics.SettledCount}/{propMetrics.Count} asentadas   corrimiento máx. {propMetrics.MaxDisplacementCentimeters:0.00} cm   " +
                $"temblor {propMetrics.JitterMillimeters:0.0} mm   fuera del tren {propMetrics.OutsideCount}\n" +
                "Tren:  ↑ más rápido   ↓ más lento   B frenar\n" +
                "Jugador:  WASD caminar   Shift correr   mouse mirar   Espacio saltar\n" +
                "Prueba:  G soltar caja   R reiniciar medición de cajas   (Esc libera el mouse)";
            GUI.Box(new Rect(12, 12, 720, 196), text, _style);
        }
    }
}
