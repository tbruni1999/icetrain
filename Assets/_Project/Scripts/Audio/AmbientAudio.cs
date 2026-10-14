using SurvivalEnTren.Survival;
using UnityEngine;

namespace SurvivalEnTren.Audio
{
    /// <summary>
    /// El ambiente que oye el jugador (sección 19 y Anexo E.5: "snapshots interior/exterior para que cerrar la puerta cambie
    /// inmediatamente viento, motor y sensación térmica"). Va en la cabeza del jugador:
    /// - Afuera: viento fuerte y con todos sus agudos.
    /// - En un refugio sin viento: más bajo y apagado.
    /// - En el tren: con las puertas cerradas casi no se oye (y muy apagado) y se oye la calefacción; con una puerta abierta
    ///   el viento entra.
    /// Los cambios son rápidos pero sin saltos (FadeSpeed). Valores en AudioMixSettings.
    /// </summary>
    public class AmbientAudio : MonoBehaviour
    {
        [SerializeField] private AudioMixSettings settings;
        [SerializeField] private TrainInteriorClimate climate;
        [Tooltip("Los lugares sin viento (estación, taller, refugio).")]
        [SerializeField] private ThermalZone[] shelters = new ThermalZone[0];

        private AudioSource _wind;
        private AudioSource _heater;
        private AudioLowPassFilter _windFilter;

        /// <summary>Dónde está el oído ahora: 0 afuera, 1 refugio, 2 tren con puertas abiertas, 3 tren cerrado (lo usan las pruebas).</summary>
        public int Place { get; private set; }
        public float WindVolume => _wind != null ? _wind.volume : 0f;
        public float WindCutoffHz => _windFilter != null ? _windFilter.cutoffFrequency : 0f;
        public float HeaterVolume => _heater != null ? _heater.volume : 0f;

        private void Awake()
        {
            var windObject = new GameObject("Sonido_Viento");
            windObject.transform.SetParent(transform, false);
            _wind = Loop(windObject, ProceduralClips.Wind());
            _windFilter = windObject.AddComponent<AudioLowPassFilter>();
            _windFilter.cutoffFrequency = 22000f;

            var heaterObject = new GameObject("Sonido_Calefaccion");
            heaterObject.transform.SetParent(transform, false);
            _heater = Loop(heaterObject, ProceduralClips.Hum("Calefaccion", 55f, new[] { 1f, 0.5f, 0.25f, 0.1f }, 0.6f, 2f, 21));
        }

        private static AudioSource Loop(GameObject owner, AudioClip clip)
        {
            var source = owner.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 0f; // ambiente: no viene de un punto
            source.volume = 0f;
            source.playOnAwake = false;
            source.Play();
            return source;
        }

        private void Update()
        {
            Vector3 ear = transform.position;
            bool inTrain = climate != null && climate.Contains(ear);
            bool doorsOpen = inTrain && climate.OpenDoors > 0;
            bool sheltered = false;
            if (!inTrain) foreach (ThermalZone zone in shelters) if (zone != null && zone.Contains(ear)) { sheltered = true; break; }
            Place = inTrain ? (doorsOpen ? 2 : 3) : sheltered ? 1 : 0;

            float windTarget, cutoffTarget;
            switch (Place)
            {
                case 3: windTarget = settings.WindTrainClosed; cutoffTarget = settings.TrainCutoffHz; break;
                case 2: windTarget = settings.WindTrainOpen; cutoffTarget = settings.ShelterCutoffHz; break;
                case 1: windTarget = settings.WindShelter; cutoffTarget = settings.ShelterCutoffHz; break;
                default: windTarget = settings.WindOutside; cutoffTarget = 22000f; break;
            }
            bool heating = inTrain && climate.HeaterOn && !climate.FuelStarved;
            float k = 1f - Mathf.Exp(-settings.FadeSpeed * Time.unscaledDeltaTime);
            _wind.volume = Mathf.Lerp(_wind.volume, windTarget, k);
            _windFilter.cutoffFrequency = Mathf.Lerp(_windFilter.cutoffFrequency, cutoffTarget, k);
            _heater.volume = Mathf.Lerp(_heater.volume, heating ? settings.HeaterVolume : 0f, k);
        }
    }
}
