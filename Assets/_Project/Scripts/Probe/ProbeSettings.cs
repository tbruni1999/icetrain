using UnityEngine;

namespace SurvivalEnTren.Probe
{
    /// <summary>
    /// Valores de las sondas (sección 13; Anexo B.3). La sonda informa "lo suficiente para orientar
    /// sin revelar todo" (sección 25.1): dirección y distancia aproximadas, más precisas de cerca.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Sonda/Sondas", fileName = "ProbeSettings")]
    public class ProbeSettings : ScriptableObject
    {
        [Header("Sonda principal del tren")]
        [Tooltip("Alcance de la consola para una señal de intensidad 1, en metros (una señal débil se detecta más cerca).")]
        [SerializeField, Range(100f, 10000f)] private float mainRangeMeters = 2500f;
        [Tooltip("Error de dirección en el borde del alcance y muy cerca, en grados.")]
        [SerializeField, Range(0f, 60f)] private float bearingErrorFarDegrees = 20f;
        [SerializeField, Range(0f, 30f)] private float bearingErrorNearDegrees = 3f;
        [Tooltip("Error de distancia en el borde del alcance y muy cerca, como fracción (0,15 = ±15%).")]
        [SerializeField, Range(0f, 0.5f)] private float distanceErrorFar = 0.15f;
        [SerializeField, Range(0f, 0.5f)] private float distanceErrorNear = 0.03f;
        [Tooltip("A qué múltiplo se redondea la distancia mostrada lejos y cerca, en metros.")]
        [SerializeField, Range(1f, 1000f)] private float roundingFarMeters = 100f;
        [SerializeField, Range(1f, 100f)] private float roundingNearMeters = 10f;
        [Tooltip("Cuánto tarda el barrido de la pantalla en dar una vuelta, en segundos.")]
        [SerializeField, Range(0.5f, 20f)] private float sweepSeconds = 4f;
        [Tooltip("Hasta qué distancia, por la vía, se busca el ancla de una señal, en metros.")]
        [SerializeField, Range(100f, 20000f)] private float railSearchMeters = 5000f;

        [Header("Sonda portátil")]
        [Tooltip("Alcance de la portátil hacia el tren y el objetivo, en metros (más lejos, sin lectura).")]
        [SerializeField, Range(100f, 10000f)] private float portableRangeMeters = 3000f;
        [Tooltip("Qué tan rápido se acomodan las agujas (más alto = más nervioso).")]
        [SerializeField, Range(0.5f, 20f)] private float needleSpeed = 4f;
        [Tooltip("Cuánto temblor agrega la interferencia máxima a las agujas, en grados.")]
        [SerializeField, Range(0f, 180f)] private float interferenceNeedleDegrees = 70f;

        public float MainRangeMeters => mainRangeMeters;
        public float SweepSeconds => sweepSeconds;
        public float RailSearchMeters => railSearchMeters;
        public float PortableRangeMeters => portableRangeMeters;
        public float NeedleSpeed => needleSpeed;
        public float InterferenceNeedleDegrees => interferenceNeedleDegrees;

        /// <summary>0 muy cerca, 1 en el borde del alcance.</summary>
        public float Farness(float distance, float range) => Mathf.Clamp01(distance / Mathf.Max(1f, range));

        public float BearingError(float farness) => Mathf.Lerp(bearingErrorNearDegrees, bearingErrorFarDegrees, farness);
        public float DistanceError(float farness) => Mathf.Lerp(distanceErrorNear, distanceErrorFar, farness);
        /// <summary>Paso de redondeo "redondo" (10, 25, 50, 100, 250...) según qué tan lejos está.</summary>
        public float Rounding(float farness)
        {
            float raw = Mathf.Lerp(roundingNearMeters, roundingFarMeters, farness);
            float step = NiceSteps[0];
            foreach (float s in NiceSteps) if (s <= raw) step = s;
            return step;
        }

        private static readonly float[] NiceSteps = { 1f, 5f, 10f, 25f, 50f, 100f, 250f, 500f, 1000f };
    }
}
