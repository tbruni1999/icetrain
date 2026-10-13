using SurvivalEnTren.Equipment;
using SurvivalEnTren.Items;
using SurvivalEnTren.Player;
using SurvivalEnTren.Survival;
using SurvivalEnTren.Train;
using SurvivalEnTren.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Spike
{
    /// <summary>
    /// Panel de lectura para pruebas (esquina superior izquierda): velocidad del tren, controles,
    /// tiempo por cuadro, mediciones de estabilidad y teclas. Es una herramienta de medición, no el
    /// HUD del juego (en el juego la velocidad se lee en el velocímetro de la cabina). F1 lo oculta.
    /// Vive en la carpeta Spike para poder borrarla entera.
    /// </summary>
    public class SpikeOverlay : MonoBehaviour
    {
        [SerializeField] private TrainMover train;
        [Tooltip("Opcional: conducción de prueba del M1 (teclas de flecha).")]
        [SerializeField] private TrainTestDriver driver;
        [Tooltip("Opcional: controles reales de la cabina (M2).")]
        [SerializeField] private TrainControls controls;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private SpikeRideMetrics metrics;
        [SerializeField] private SpikePropMetrics propMetrics;
        [SerializeField] private InputActionReference resetMeasure;
        [SerializeField] private InputActionReference toggleOverlay;
        [SerializeField] private FloatingOrigin floatingOrigin;
        [Tooltip("Opcional: supervivencia (M4).")]
        [SerializeField] private PlayerVitals vitals;
        [SerializeField] private GameClock clock;
        [SerializeField] private TrainInteriorClimate climate;
        [Tooltip("Opcional: combustible del tren (M6).")]
        [SerializeField] private FuelTank fuel;
        [Tooltip("Opcional: equipo del jugador (M6).")]
        [SerializeField] private PlayerGear gear;
        [Tooltip("Opcional: mochila del jugador (M7).")]
        [SerializeField] private Backpack backpack;
        [SerializeField] private InputActionReference timeFast;
        [Tooltip("Cuánto se acelera el reloj con F2, para probar.")]
        [SerializeField] private float timeFastMultiplier = 30f;

        private float _smoothedFrameMs;
        private GUIStyle _style;
        private bool _visible = true;

        private void OnEnable()
        {
            resetMeasure.action.performed += OnResetMeasure;
            resetMeasure.action.Enable();
            toggleOverlay.action.performed += OnToggle;
            toggleOverlay.action.Enable();
            if (timeFast != null)
            {
                timeFast.action.performed += OnTimeFast;
                timeFast.action.Enable();
            }
        }

        private void OnDisable()
        {
            resetMeasure.action.performed -= OnResetMeasure;
            toggleOverlay.action.performed -= OnToggle;
            if (timeFast != null) timeFast.action.performed -= OnTimeFast;
        }

        private void OnTimeFast(InputAction.CallbackContext _)
        {
            if (clock != null) clock.SpeedMultiplier = clock.SpeedMultiplier > 1f ? 1f : timeFastMultiplier;
        }

        private void OnResetMeasure(InputAction.CallbackContext _) => propMetrics.ResetAnchors();
        private void OnToggle(InputAction.CallbackContext _) => _visible = !_visible;

        private void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            _smoothedFrameMs = _smoothedFrameMs <= 0f ? ms : Mathf.Lerp(_smoothedFrameMs, ms, 0.05f);
        }

        private void OnGUI()
        {
            if (!_visible) return;
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

            string speed = $"Velocidad: {train.SpeedMetersPerSecond * 3.6f:0} km/h";
            if (driver != null) speed += $"   (objetivo {driver.TargetSpeedKmh:0})";
            if (controls != null) speed += $"   reversor {ReverserName(controls.Reverser)}   potencia {controls.PowerNotch}   freno {controls.BrakeNotch}";

            string trainKeys = driver != null
                ? "Tren:  ↑ más rápido   ↓ más lento   B frenar"
                : "Tren:  mirar el pupitre de la cabina + E para tomar los controles;  W / S potencia-freno,  R / F reversor,  E soltar";

            string text =
                "Estrategia C: el tren se mueve por el mundo y el mundo se re-centra\n" +
                $"Re-centrados: {floatingOrigin.ShiftCount} (último {floatingOrigin.LastShiftMilliseconds:0.00} ms)   " +
                $"origen absoluto: {floatingOrigin.AbsoluteOriginX / 1000.0:0.000} km, {floatingOrigin.AbsoluteOriginZ / 1000.0:0.000} km\n" +
                speed + "\n" +
                $"Cuadro: {_smoothedFrameMs:0.0} ms  (~{1000f / Mathf.Max(0.01f, _smoothedFrameMs):0} FPS)\n" +
                $"Jugador: {reference}\n" +
                stability + "\n" +
                $"Cajas: {propMetrics.SettledCount}/{propMetrics.Count} asentadas   corrimiento máx. {propMetrics.MaxDisplacementCentimeters:0.00} cm   " +
                $"temblor {propMetrics.JitterMillimeters:0.0} mm   fuera del tren {propMetrics.OutsideCount}\n" +
                SurvivalLine() +
                trainKeys + "\n" +
                "Jugador:  WASD caminar   Shift correr   mouse mirar   Espacio saltar   E usar   Q sonda\n" +
                "Prueba:  G soltar caja   T reiniciar medición de cajas   F1 ocultar este panel   (Esc libera el mouse)";
            GUI.Box(new Rect(12, 12, 860, (vitals != null ? (fuel != null ? 298 : 276) : 236) + (backpack != null ? 22 : 0)), text, _style);
        }

        private string SurvivalLine()
        {
            if (vitals == null || clock == null) return "";
            int hour = Mathf.FloorToInt(clock.HourOfDay);
            int minute = Mathf.FloorToInt((clock.HourOfDay - hour) * 60f);
            string interior = climate != null ? $"   interior del tren {climate.InteriorTemperatureC:0} °C (puertas abiertas {climate.OpenDoors})" : "";
            return $"Reloj {hour:00}:{minute:00} (x{clock.SpeedMultiplier:0}, F2 acelera)   aire alrededor {vitals.AmbientTemperatureC:0} °C{interior}\n" +
                   $"vida {vitals.Health:0}  comida {vitals.Satiety:0}  energía {vitals.Energy:0}  calor {vitals.Warmth:0}\n" +
                   FuelLine() + BackpackLine();
        }

        private string BackpackLine()
        {
            if (backpack == null) return "";
            string contents = "";
            foreach (ItemStack stack in backpack.Stacks) contents += $"  {stack.Item.DisplayName} ×{stack.Count}";
            return $"mochila {backpack.MassKg:0.0} / {backpack.CapacityKg:0} kg{(contents == "" ? "  (vacía)" : ":" + contents)}\n";
        }

        private string FuelLine()
        {
            if (fuel == null) return "";
            string heater = climate == null ? "" : climate.FuelStarved ? "   calefacción SIN combustible" : "   calefacción encendida";
            string equipment = gear == null ? "" :
                $"   equipo: barreta {(gear.Has(GearId.Crowbar) ? "sí" : "no")}, bomba {(gear.Has(GearId.HandPump) ? "sí" : "no")}, traje {(gear.SuitWorn ? "puesto" : "no")}";
            return $"combustible {fuel.Liters:0.0} / {fuel.Capacity:0} L{(fuel.IsEmpty ? " (VACÍO)" : fuel.IsLow ? " (reserva)" : "")}{heater}{equipment}\n";
        }

        private static string ReverserName(ReverserPosition position)
        {
            switch (position)
            {
                case ReverserPosition.Forward: return "adelante";
                case ReverserPosition.Back: return "atrás";
                default: return "neutro";
            }
        }
    }
}
