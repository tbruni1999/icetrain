using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Valores de la conducción del tren (sección 6.1): qué velocidad da cada posición de potencia,
    /// cuánto frena cada posición de freno, el rozamiento y cuándo se puede mover el reversor.
    /// Las mejoras futuras del tren pueden subir las velocidades (idea de Tomi: arrancar lento).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Tren/Conducción", fileName = "TrainDriveSettings")]
    public class TrainDriveSettings : ScriptableObject
    {
        [Tooltip("Velocidad máxima que alcanza cada posición de potencia (1, 2, 3, 4...), en km/h. " +
                 "La cantidad de posiciones sale del largo de esta lista.")]
        [SerializeField] private float[] powerNotchTopSpeedsKmh = { 10f, 20f, 30f, 40f };

        [Tooltip("Cuánto acelera el tren con potencia, en metros por segundo cada segundo.")]
        [SerializeField, Range(0.05f, 2f)] private float tractionAccelerationMetersPerSecondSquared = 0.35f;

        [Tooltip("Cuánto frena cada posición de freno (1, 2, 3, 4...), en metros por segundo cada segundo. " +
                 "La cantidad de posiciones sale del largo de esta lista.")]
        [SerializeField] private float[] brakeNotchDecelerationsMetersPerSecondSquared = { 0.25f, 0.5f, 0.8f, 1.2f };

        [Tooltip("Cuánta velocidad pierde el tren solo por rozamiento (sin potencia ni freno), en m/s cada segundo.")]
        [SerializeField, Range(0f, 0.5f)] private float rollingResistanceMetersPerSecondSquared = 0.03f;

        [Tooltip("El reversor solo se puede mover con el tren casi detenido: por debajo de esta velocidad, en km/h.")]
        [SerializeField, Range(0.1f, 5f)] private float reverserMaxSpeedKmh = 0.5f;

        [Header("Topes de fin de vía (resorte amortiguado)")]
        [Tooltip("Rigidez del resorte del tope: cuánto empuja por cada metro comprimido, en m/s² por metro. " +
                 "Más blando = el tope se hunde más y el golpe es más suave.")]
        [SerializeField, Range(10f, 1000f)] private float bufferStiffnessPerSecondSquared = 30f;

        [Tooltip("Amortiguación del tope (cuánta energía se come), en 1/s. Más alto = rebota menos.")]
        [SerializeField, Range(0f, 60f)] private float bufferDampingPerSecond = 10f;

        [Tooltip("En cuántos metros de compresión entra la amortiguación del tope. Evita el golpe seco del " +
                 "primer instante del choque.")]
        [SerializeField, Range(0.01f, 0.5f)] private float bufferDampingRampMeters = 0.12f;

        public float BufferStiffnessPerSecondSquared => bufferStiffnessPerSecondSquared;
        public float BufferDampingPerSecond => bufferDampingPerSecond;
        public float BufferDampingRampMeters => bufferDampingRampMeters;

        public int PowerNotchCount => powerNotchTopSpeedsKmh.Length;
        public int BrakeNotchCount => brakeNotchDecelerationsMetersPerSecondSquared.Length;
        public float TractionAccelerationMetersPerSecondSquared => tractionAccelerationMetersPerSecondSquared;
        public float RollingResistanceMetersPerSecondSquared => rollingResistanceMetersPerSecondSquared;
        public float ReverserMaxSpeedKmh => reverserMaxSpeedKmh;

        /// <summary>Velocidad máxima de una posición de potencia (1 = la primera), en km/h; 0 si no hay potencia.</summary>
        public float TopSpeedKmh(int powerNotch)
        {
            if (powerNotch <= 0) return 0f;
            return powerNotchTopSpeedsKmh[Mathf.Min(powerNotch, PowerNotchCount) - 1];
        }

        /// <summary>Desaceleración de una posición de freno (1 = la primera), en m/s²; 0 si el freno está suelto.</summary>
        public float BrakeDeceleration(int brakeNotch)
        {
            if (brakeNotch <= 0) return 0f;
            return brakeNotchDecelerationsMetersPerSecondSquared[Mathf.Min(brakeNotch, BrakeNotchCount) - 1];
        }
    }
}
