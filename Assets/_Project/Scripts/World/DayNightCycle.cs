using SurvivalEnTren.Survival;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalEnTren.World
{
    /// <summary>
    /// El día y la noche afuera (M13; secciones 19 y E.3: "sol/día-noche dinámico para exterior"; "el contraste exterior
    /// frío / interior cálido es parte del gameplay"). Sigue la hora del reloj del juego:
    /// - De día el sol sube y baja (días cortos de invierno); la luz es blanca-azulada y el cielo, claro.
    /// - De noche la luz es la de la luna: tenue y azul, desde arriba (nunca desde abajo del suelo), el cielo se oscurece y
    ///   la niebla se vuelve azul oscuro. Adentro del tren las luces cálidas pasan a ser lo más brillante: el contraste.
    /// - Barato: una luz, la niebla, el color ambiente y la exposición del cielo (una copia del material: el archivo no se
    ///   toca). Valores en DayNightSettings.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private DayNightSettings settings;
        [SerializeField] private GameClock clock;
        [SerializeField] private Light sun;

        private Material _sky;
        private static readonly int Exposure = Shader.PropertyToID("_Exposure");

        /// <summary>0 = noche cerrada, 1 = pleno día (lo usan las pruebas y otros sistemas).</summary>
        public float Daylight { get; private set; }
        /// <summary>Altura del sol sobre el horizonte, en grados (negativa de noche).</summary>
        public float SunElevation { get; private set; }

        private void Start()
        {
            if (RenderSettings.skybox != null)
            {
                _sky = new Material(RenderSettings.skybox);
                RenderSettings.skybox = _sky;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Apply();
        }

        private void Update() => Apply();

        private void Apply()
        {
            if (clock == null || sun == null || settings == null) return;
            float hour = clock.HourOfDay;
            float noon = (settings.SunriseHour + settings.SunsetHour) * 0.5f;
            float halfDay = (settings.SunsetHour - settings.SunriseHour) * 0.5f;
            // Altura del sol: 0 al amanecer y al atardecer, máxima al mediodía, bajo el horizonte de noche.
            float c0 = Mathf.Cos(2f * Mathf.PI * halfDay / 24f);
            float c = Mathf.Cos(2f * Mathf.PI * (hour - noon) / 24f);
            SunElevation = settings.MaxSunElevation * (c - c0) / (1f - c0);
            Daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-settings.TwilightDegrees, settings.TwilightDegrees, SunElevation));

            float azimuth = 180f + 360f * (hour - noon) / 24f;
            Quaternion sunRotation = Quaternion.Euler(Mathf.Max(SunElevation, 2f), azimuth, 0f);
            Quaternion moonRotation = Quaternion.Euler(settings.MoonElevation, azimuth + 180f, 0f);
            // La luz usa la dirección del sol de día y la de la luna de noche; cambia de una a otra en el crepúsculo, cuando
            // las dos son muy tenues (no se nota el salto).
            sun.transform.rotation = Daylight > 0.5f ? sunRotation : moonRotation;
            float twilightDip = 1f - 0.85f * (1f - Mathf.Abs(Daylight * 2f - 1f)); // más oscuro justo en el cambio
            sun.intensity = Mathf.Lerp(settings.NightIntensity, settings.DayIntensity, Daylight) * twilightDip;
            sun.color = Color.Lerp(settings.NightColor, settings.DayColor, Daylight);

            RenderSettings.ambientSkyColor = Color.Lerp(settings.NightAmbientSky, settings.DayAmbientSky, Daylight);
            RenderSettings.ambientEquatorColor = Color.Lerp(settings.NightAmbientEquator, settings.DayAmbientEquator, Daylight);
            RenderSettings.ambientGroundColor = Color.Lerp(settings.NightAmbientGround, settings.DayAmbientGround, Daylight);
            RenderSettings.fogColor = Color.Lerp(settings.NightFog, settings.DayFog, Daylight);
            if (_sky != null && _sky.HasProperty(Exposure)) _sky.SetFloat(Exposure, Mathf.Lerp(settings.NightSkyExposure, settings.DaySkyExposure, Daylight));
        }
    }
}
