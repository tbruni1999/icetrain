using UnityEngine;

namespace SurvivalEnTren.World
{
    /// <summary>
    /// Valores del clima visible (producción P1, sección 19 y Anexo E.2): la nieve que cae, el viento y la escarcha de las
    /// ventanas del tren. Todo es PROPUESTA: se ajusta acá, sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Atmósfera", fileName = "Atmosfera")]
    public class AtmosphereSettings : ScriptableObject
    {
        [Header("Nieve que cae")]
        [Tooltip("Copos nuevos por segundo, con la nevada al máximo.")]
        [SerializeField, Range(0f, 3000f)] private float flakesPerSecond = 1100f;
        [Tooltip("Radio del área donde cae la nieve alrededor del jugador, en metros.")]
        [SerializeField, Range(5f, 60f)] private float areaRadius = 14f;
        [Tooltip("Altura sobre el jugador desde donde caen los copos, en metros.")]
        [SerializeField, Range(3f, 30f)] private float spawnHeight = 10f;
        [Tooltip("Velocidad con la que caen, en metros por segundo.")]
        [SerializeField, Range(0.2f, 6f)] private float fallSpeed = 1.4f;
        [Tooltip("Tamaño de los copos, en metros (entre el mínimo y el máximo).")]
        [SerializeField, Range(0.005f, 0.2f)] private float flakeSizeMin = 0.05f;
        [SerializeField, Range(0.005f, 0.2f)] private float flakeSizeMax = 0.11f;
        [Tooltip("Cuánto varía la nevada con el tiempo (0 = siempre igual; 1 = de nada a todo). Cambia despacio, en horas de juego.")]
        [SerializeField, Range(0f, 1f)] private float intensityVariation = 0.5f;
        [Tooltip("Cada cuántas horas de juego cambia la nevada, más o menos.")]
        [SerializeField, Range(0.5f, 24f)] private float variationHours = 3f;

        [Header("Viento")]
        [Tooltip("Hacia dónde sopla el viento, en grados (0 = hacia +z del mundo, 90 = hacia +x).")]
        [SerializeField, Range(0f, 360f)] private float windDirectionDegrees = 70f;
        [Tooltip("Velocidad media del viento, en metros por segundo.")]
        [SerializeField, Range(0f, 20f)] private float windSpeed = 3.5f;
        [Tooltip("Ráfagas: cuánto sube y baja el viento (0 = parejo; 1 = de calma al doble).")]
        [SerializeField, Range(0f, 1f)] private float gustStrength = 0.5f;
        [Tooltip("Ráfagas por minuto, más o menos.")]
        [SerializeField, Range(0.5f, 30f)] private float gustsPerMinute = 6f;
        [Tooltip("Nieve que el viento levanta a ras del suelo: partículas por segundo.")]
        [SerializeField, Range(0f, 600f)] private float driftPerSecond = 90f;

        [Header("Escarcha en las ventanas del tren")]
        [Tooltip("Con el interior a esta temperatura o menos, las ventanas quedan escarchadas del todo (°C).")]
        [SerializeField, Range(-30f, 20f)] private float frostFullBelowC = -2f;
        [Tooltip("Con el interior a esta temperatura o más, solo queda un poco en los bordes (°C).")]
        [SerializeField, Range(-30f, 30f)] private float frostClearAboveC = 12f;
        [Tooltip("Escarcha que queda siempre en los bordes, aunque adentro esté templado (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float frostMinimum = 0.25f;
        [Tooltip("Cuántos minutos de juego tarda la escarcha en alcanzar lo que corresponde a la temperatura.")]
        [SerializeField, Range(1f, 240f)] private float frostResponseMinutes = 40f;

        public float FlakesPerSecond => flakesPerSecond;
        public float AreaRadius => areaRadius;
        public float SpawnHeight => spawnHeight;
        public float FallSpeed => fallSpeed;
        public float FlakeSizeMin => flakeSizeMin;
        public float FlakeSizeMax => Mathf.Max(flakeSizeMin, flakeSizeMax);
        public float IntensityVariation => intensityVariation;
        public float VariationHours => variationHours;
        public float WindDirectionDegrees => windDirectionDegrees;
        public float WindSpeed => windSpeed;
        public float GustStrength => gustStrength;
        public float GustsPerMinute => gustsPerMinute;
        public float DriftPerSecond => driftPerSecond;
        public float FrostFullBelowC => frostFullBelowC;
        public float FrostClearAboveC => Mathf.Max(frostFullBelowC + 0.1f, frostClearAboveC);
        public float FrostMinimum => frostMinimum;
        public float FrostResponseMinutes => frostResponseMinutes;

        /// <summary>Dirección hacia donde sopla el viento, horizontal y de largo 1, en coordenadas del mundo.</summary>
        public Vector3 WindDirection => Quaternion.Euler(0f, windDirectionDegrees, 0f) * Vector3.forward;
    }
}
