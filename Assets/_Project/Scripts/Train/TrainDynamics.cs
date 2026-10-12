using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Física simple de conducción: a partir de los controles de la cabina calcula la velocidad
    /// del tren en cada tic de física y se la pasa a TrainMover.
    /// - Con potencia, acelera en el sentido del reversor hasta la velocidad de esa posición
    ///   (suavizando al llegar, para no frenar en seco contra el tope).
    /// - El freno y el rozamiento siempre se oponen al movimiento y nunca lo invierten.
    /// - Si se baja la potencia por debajo de la velocidad actual, el tren sigue "por inercia" y
    ///   pierde velocidad solo por rozamiento (hay que frenar para bajar rápido).
    /// - Los topes de fin de vía son un resorte amortiguado: frenan al tren en unos centímetros y lo
    ///   hacen rebotar un poco (pedido de Tomi; antes frenaba en seco y todo salía volando).
    /// </summary>
    [RequireComponent(typeof(TrainMover))]
    public class TrainDynamics : MonoBehaviour
    {
        // Margen (en m/s) antes de la velocidad de la posición en el que la aceleración se va apagando.
        private const float ApproachWindowMetersPerSecond = 0.4f;
        private static readonly int[] BufferDirections = { 1, -1 };

        [SerializeField] private TrainControls controls;
        [Tooltip("Tanque de combustible (M6). Vacío, el motor no da potencia. Sin tanque asignado, siempre hay.")]
        [SerializeField] private FuelTank fuel;

        private TrainMover _train;

        private void Awake()
        {
            _train = GetComponent<TrainMover>();
        }

        private void FixedUpdate()
        {
            TrainDriveSettings settings = controls.Settings;
            float dt = Time.fixedDeltaTime;
            float speed = _train.SpeedMetersPerSecond;

            // Tracción en el sentido del reversor.
            int direction = (int)controls.Reverser;
            bool hasFuel = fuel == null || !fuel.IsEmpty; // sin combustible no hay tracción: el tren sigue por inercia
            if (direction != 0 && controls.PowerNotch > 0 && hasFuel)
            {
                float top = settings.TopSpeedKmh(controls.PowerNotch) / 3.6f;
                float alongDirection = speed * direction;
                if (alongDirection < top)
                {
                    // La tracción también vence al rozamiento, así el tren se estabiliza justo en el tope.
                    float fade = Mathf.Clamp01((top - alongDirection) / ApproachWindowMetersPerSecond);
                    float push = settings.TractionAccelerationMetersPerSecondSquared * fade + settings.RollingResistanceMetersPerSecondSquared;
                    speed += direction * Mathf.Min(push * dt, top - alongDirection + settings.RollingResistanceMetersPerSecondSquared * dt);
                }
            }

            // Freno + rozamiento: se oponen al movimiento, sin pasarse de cero.
            float slowdown = (settings.BrakeDeceleration(controls.BrakeNotch) + settings.RollingResistanceMetersPerSecondSquared) * dt;
            speed = Mathf.Abs(speed) <= slowdown ? 0f : speed - Mathf.Sign(speed) * slowdown;

            // Topes de fin de vía: si la punta del tren entra en el tope, el tope empuja como un resorte
            // amortiguado. Frena al tren en unos centímetros y lo devuelve un poco hacia atrás.
            foreach (int end in BufferDirections)
            {
                float compression = -_train.GapToTrackEnd(end);
                if (compression <= 0f) continue;
                float damping = settings.BufferDampingPerSecond * Mathf.Clamp01(compression / settings.BufferDampingRampMeters);
                float push = -end * settings.BufferStiffnessPerSecondSquared * compression - damping * speed;
                speed += push * dt;
            }

            _train.SpeedMetersPerSecond = speed;
        }
    }
}
