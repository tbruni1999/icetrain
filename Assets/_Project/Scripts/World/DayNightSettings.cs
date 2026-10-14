using UnityEngine;

namespace SurvivalEnTren.World
{
    /// <summary>Los valores del día y la noche (M13). Todos son PROPUESTA y se ajustan en el archivo.</summary>
    [CreateAssetMenu(menuName = "Survival en tren/Mundo/Día y noche", fileName = "DiaNoche")]
    public class DayNightSettings : ScriptableObject
    {
        [Header("Horas (días cortos de invierno)")]
        [SerializeField, Range(0f, 12f)] private float sunriseHour = 7.5f;
        [SerializeField, Range(12f, 24f)] private float sunsetHour = 18f;
        [SerializeField, Range(5f, 80f)] private float maxSunElevation = 32f;
        [Tooltip("Cuántos grados bajo y sobre el horizonte dura el crepúsculo.")]
        [SerializeField, Range(1f, 15f)] private float twilightDegrees = 6f;
        [SerializeField, Range(10f, 80f)] private float moonElevation = 40f;

        [Header("Luz")]
        [SerializeField] private Color dayColor = new Color(0.86f, 0.9f, 1f);
        [SerializeField, Range(0f, 3f)] private float dayIntensity = 1f;
        [SerializeField] private Color nightColor = new Color(0.45f, 0.55f, 0.85f);
        [Tooltip("La luna: tenue, pero la nieve se sigue viendo.")]
        [SerializeField, Range(0f, 1f)] private float nightIntensity = 0.12f;

        [Header("Ambiente (cielo / horizonte / suelo)")]
        [SerializeField] private Color dayAmbientSky = new Color(0.62f, 0.7f, 0.82f);
        [SerializeField] private Color dayAmbientEquator = new Color(0.55f, 0.6f, 0.66f);
        [SerializeField] private Color dayAmbientGround = new Color(0.5f, 0.52f, 0.55f);
        [SerializeField] private Color nightAmbientSky = new Color(0.08f, 0.1f, 0.18f);
        [SerializeField] private Color nightAmbientEquator = new Color(0.06f, 0.07f, 0.12f);
        [SerializeField] private Color nightAmbientGround = new Color(0.05f, 0.06f, 0.09f);

        [Header("Niebla y cielo")]
        [SerializeField] private Color dayFog = new Color(0.78f, 0.82f, 0.87f);
        [SerializeField] private Color nightFog = new Color(0.07f, 0.09f, 0.15f);
        [SerializeField, Range(0f, 8f)] private float daySkyExposure = 1.3f;
        [SerializeField, Range(0f, 8f)] private float nightSkyExposure = 0.05f;

        public float SunriseHour => sunriseHour;
        public float SunsetHour => sunsetHour;
        public float MaxSunElevation => maxSunElevation;
        public float TwilightDegrees => twilightDegrees;
        public float MoonElevation => moonElevation;
        public Color DayColor => dayColor;
        public float DayIntensity => dayIntensity;
        public Color NightColor => nightColor;
        public float NightIntensity => nightIntensity;
        public Color DayAmbientSky => dayAmbientSky;
        public Color DayAmbientEquator => dayAmbientEquator;
        public Color DayAmbientGround => dayAmbientGround;
        public Color NightAmbientSky => nightAmbientSky;
        public Color NightAmbientEquator => nightAmbientEquator;
        public Color NightAmbientGround => nightAmbientGround;
        public Color DayFog => dayFog;
        public Color NightFog => nightFog;
        public float DaySkyExposure => daySkyExposure;
        public float NightSkyExposure => nightSkyExposure;
    }
}
