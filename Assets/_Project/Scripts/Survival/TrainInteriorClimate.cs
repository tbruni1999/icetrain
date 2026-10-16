using SurvivalEnTren.Interaction;
using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Temperatura del aire dentro del tren (secciones 7, 19 y 20.7): la calefacción la empuja hacia
    /// su temperatura objetivo; se pierde calor hacia afuera por las paredes y mucho más por cada
    /// puerta abierta. Así, al cerrar la puerta, vuelve el calor.
    /// - El volumen interior es una caja en coordenadas del tren (se mueve con el tren).
    /// - Avanza con el reloj del juego, también en las horas que se saltan (dormir).
    /// - La calefacción gasta combustible del tanque (sección 8.1, M6) mientras está encendida, también con
    ///   el tren parado y mientras se duerme. Sin combustible no calienta: el interior se enfría hasta
    ///   la temperatura de afuera. Sin tanque asignado (escenas viejas), siempre funciona.
    /// </summary>
    public class TrainInteriorClimate : MonoBehaviour
    {
        private const float SkipStepHours = 0.05f;

        [SerializeField] private SurvivalSettings settings;
        [SerializeField] private GameClock clock;
        [Tooltip("Tanque del que gasta la calefacción (M6). Vacío = sin calefacción.")]
        [SerializeField] private FuelTank fuel;
        [Tooltip("Centro y tamaño del espacio interior, en coordenadas del tren (metros).")]
        [SerializeField] private Vector3 interiorCenter;
        [SerializeField] private Vector3 interiorSize = Vector3.one;

        [Header("Calefacción y pérdidas (valores generosos)")]
        [Tooltip("Temperatura a la que la calefacción lleva el interior, en °C (refugio precario: templado, no cálido).")]
        [SerializeField, Range(-10f, 30f)] private float heaterTargetC = 15f;
        [Tooltip("Cuántos grados por hora puede subir la calefacción el aire interior.")]
        [SerializeField, Range(0f, 200f)] private float heaterPowerCPerHour = 30f;
        [Tooltip("Pérdida por las paredes: fracción de la diferencia con el exterior que se pierde por hora.")]
        [SerializeField, Range(0f, 5f)] private float wallLossPerHour = 0.5f;
        [Tooltip("Pérdida extra por cada puerta abierta: fracción de la diferencia con el exterior por hora.")]
        [SerializeField, Range(0f, 20f)] private float openDoorLossPerHour = 3f;

        private SlidingDoor[] _doors;

        public float InteriorTemperatureC { get; private set; }
        /// <summary>La calefacción está encendida (interruptor). Con el tanque vacío no calienta aunque esté encendida.</summary>
        public bool HeaterOn { get; set; } = true;
        /// <summary>Calor de otras fuentes (la forja, H4), en °C por hora: se suma a la calefacción y no gasta su combustible.</summary>
        public float ExtraHeatCPerHour { get; set; }
        /// <summary>La calefacción está encendida pero sin combustible (no calienta).</summary>
        public bool FuelStarved { get; private set; }
        /// <summary>Cuántas puertas del tren están abiertas ahora (se cuenta en el momento: el sonido lo usa aunque el tiempo esté quieto).</summary>
        public int OpenDoors
        {
            get
            {
                int open = 0;
                if (_doors != null) foreach (SlidingDoor door in _doors) if (door != null && door.IsOpen) open++;
                return open;
            }
        }

        /// <summary>Pone la temperatura de adentro (al cargar una partida).</summary>
        public void RestoreTemperature(float celsius) => InteriorTemperatureC = celsius;

        private void Awake()
        {
            _doors = GetComponentsInChildren<SlidingDoor>();
            InteriorTemperatureC = heaterTargetC;
        }

        private void OnEnable() => clock.Skipped += OnSkipped;
        private void OnDisable() => clock.Skipped -= OnSkipped;

        private void Update() => Simulate(clock.DeltaHours);

        private void OnSkipped(float hours)
        {
            for (float done = 0f; done < hours; done += SkipStepHours)
                Simulate(Mathf.Min(SkipStepHours, hours - done));
        }

        private void Simulate(float hours)
        {
            if (hours <= 0f) return;
            int openDoors = OpenDoors;

            float outside = settings.OutsideTemperatureC;
            float t = InteriorTemperatureC;
            float heating = HeaterOn ? heaterPowerCPerHour * Mathf.Clamp01((heaterTargetC - t) / 2f) : 0f;

            // La calefacción encendida gasta combustible; si el tanque no alcanza, calienta solo la parte que pudo.
            FuelStarved = false;
            if (HeaterOn && fuel != null)
            {
                float needed = fuel.Settings.HeaterLitersPerHour * hours;
                if (needed > 0f)
                {
                    float drawn = fuel.Draw(needed);
                    FuelStarved = drawn < needed * 0.999f;
                    heating *= drawn / needed;
                }
            }
            float loss = (wallLossPerHour + openDoorLossPerHour * openDoors) * (t - outside);
            InteriorTemperatureC = t + (heating + ExtraHeatCPerHour - loss) * hours;
        }

        /// <summary>¿Este punto del mundo está dentro del espacio interior del tren?</summary>
        public bool Contains(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint) - interiorCenter;
            Vector3 half = interiorSize * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.6f, 0.3f, 0.4f);
            Gizmos.DrawWireCube(interiorCenter, interiorSize);
        }
    }
}
