using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Audio
{
    /// <summary>
    /// El motor y las ruedas del tren (M13; sección 18.2: "combustible bajo: el motor cambia de sonido"; Anexo E.5: "capas de
    /// motor según potencia/carga, no un loop único").
    /// - Ralentí: un zumbido grave mientras el motor tiene combustible.
    /// - Carga: otra capa que sube con la potencia y la velocidad (más fuerte y más aguda).
    /// - Juntas de los rieles: un golpecito metálico cada tantos metros, más seguido cuanto más rápido.
    /// - Sin combustible: el motor se apaga (silencio), las ruedas siguen sonando si el tren rueda.
    /// Suena desde la sala de máquinas (3D): adentro se oye cerca y afuera se pierde con la distancia.
    /// </summary>
    public class TrainEngineAudio : MonoBehaviour
    {
        [SerializeField] private AudioMixSettings settings;
        [SerializeField] private TrainMover train;
        [SerializeField] private TrainControls controls;
        [SerializeField] private FuelTank tank;
        [Tooltip("De dónde sale el sonido del motor (la sala de máquinas).")]
        [SerializeField] private Transform engine;
        [Tooltip("De dónde sale el golpeteo de las ruedas (debajo del piso).")]
        [SerializeField] private Transform wheels;

        private AudioSource _idle, _load, _rails;
        private AudioClip _joint;
        private float _sinceJoint;

        public float IdleVolume => _idle != null ? _idle.volume : 0f;
        public float LoadVolume => _load != null ? _load.volume : 0f;
        public float LoadPitch => _load != null ? _load.pitch : 0f;
        /// <summary>Cuántos golpes de junta sonaron (lo usan las pruebas).</summary>
        public int JointClicks { get; private set; }

        private void Awake()
        {
            _idle = Source(engine, ProceduralClips.Hum("Motor_Ralenti", 31f, new[] { 1f, 0.7f, 0.45f, 0.2f, 0.1f }, 0.5f, 2f, 31), true);
            _load = Source(engine, ProceduralClips.Hum("Motor_Carga", 46f, new[] { 0.8f, 1f, 0.6f, 0.45f, 0.3f, 0.2f }, 1.2f, 2f, 47), true);
            _rails = Source(wheels, null, false);
            _joint = ProceduralClips.Clank("Junta_Riel", 520f, 0.12f, 61);
        }

        private static AudioSource Source(Transform at, AudioClip clip, bool loop)
        {
            var source = at.gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.spatialBlend = 1f;
            source.minDistance = 4f;
            source.maxDistance = 90f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f; // el jugador viaja con el tren: sin efecto Doppler raro
            source.volume = 0f;
            source.playOnAwake = false;
            if (loop) source.Play();
            return source;
        }

        private void Update()
        {
            float speed = Mathf.Abs(train.SpeedMetersPerSecond);
            float speedFraction = Mathf.Clamp01(speed / 11.2f); // 40 km/h
            bool running = !tank.IsEmpty;
            float power = controls.Settings.PowerNotchCount > 0 ? controls.PowerNotch / (float)controls.Settings.PowerNotchCount : 0f;
            float load = Mathf.Clamp01(power * 0.7f + speedFraction * 0.3f);
            float k = 1f - Mathf.Exp(-4f * Time.deltaTime);

            _idle.volume = Mathf.Lerp(_idle.volume, running ? settings.EngineIdleVolume : 0f, k);
            _idle.pitch = Mathf.Lerp(_idle.pitch, 0.9f + 0.25f * load, k);
            _load.volume = Mathf.Lerp(_load.volume, running ? settings.EngineLoadVolume * load : 0f, k);
            _load.pitch = Mathf.Lerp(_load.pitch, 0.75f + 0.55f * speedFraction + 0.15f * power, k);

            // Juntas de los rieles: un golpe cada RailJointMeters recorridos.
            _sinceJoint += speed * Time.deltaTime;
            if (_sinceJoint >= settings.RailJointMeters)
            {
                _sinceJoint -= settings.RailJointMeters;
                if (speed > 0.5f)
                {
                    _rails.pitch = 0.9f + 0.2f * speedFraction;
                    _rails.PlayOneShot(_joint, settings.RailClickVolume * Mathf.Clamp01(0.4f + speedFraction));
                    JointClicks++;
                }
            }
        }
    }
}
