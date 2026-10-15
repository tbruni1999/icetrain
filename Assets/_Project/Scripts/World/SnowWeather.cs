using SurvivalEnTren.Survival;
using UnityEngine;

namespace SurvivalEnTren.World
{
    /// <summary>
    /// Nieve que cae y viento visible (producción P1; sección 19, "nieve, viento"; Anexo E.2, "partículas con pooling, no
    /// millones de partículas físicas"). Dos sistemas de partículas en el mundo alrededor del jugador:
    /// - los copos: caen desde arriba, el viento los corre de costado y las ráfagas los empujan;
    /// - la nieve a ras del suelo: rachas finas que el viento arrastra por la superficie.
    /// Los copos mueren al tocar el suelo y los techos fijos (colisión barata de Unity, solo con lo que no se mueve) y al entrar
    /// en las "zonas sin nieve" (cajas que se mueven, como el tren): adentro del tren no nieva.
    /// La nevada sube y baja despacio con las horas de juego y el viento tiene ráfagas. Los valores, en AtmosphereSettings.
    /// Cuando el origen flotante re-centra el mundo, corre las partículas con él.
    /// </summary>
    public class SnowWeather : MonoBehaviour, IOriginShiftListener
    {
        [SerializeField] private AtmosphereSettings settings;
        [Tooltip("Alrededor de quién nieva: la cabeza del jugador.")]
        [SerializeField] private Transform follow;
        [Tooltip("Opcional: el reloj del juego (la nevada varía con las horas).")]
        [SerializeField] private GameClock clock;
        [SerializeField] private Material flakeMaterial;
        [SerializeField] private Material driftMaterial;
        [Tooltip("Cajas (colliders trigger) donde no nieva aunque se muevan: el tren.")]
        [SerializeField] private Collider[] noSnowZones;

        private ParticleSystem _snow;
        private ParticleSystem _drift;
        private ParticleSystem.Particle[] _buffer;
        private float _seed;
        private Vector3 _lastFollow;
        private Vector3 _followVelocity; // cómo se mueve el jugador (a pie o en el tren), suavizado
        private bool _hasLast;

        /// <summary>Cuánto nieva ahora (0 a 1).</summary>
        public float Intensity { get; private set; }
        /// <summary>El viento ahora, en metros por segundo (horizontal).</summary>
        public Vector3 Wind { get; private set; }
        public int AliveFlakes => _snow != null ? _snow.particleCount : 0;
        public int AliveDrift => _drift != null ? _drift.particleCount : 0;

        private void Awake()
        {
            _seed = Random.value * 100f;
            _snow = CreateSnow();
            _drift = CreateDrift();
        }

        private ParticleSystem NewSystem(string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.scalingMode = ParticleSystemScalingMode.Shape;

            var collision = ps.collision; // el suelo y los techos que no se mueven
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.lifetimeLoss = 1f;
            collision.bounce = 0f;
            collision.radiusScale = 0.5f;
            collision.enableDynamicColliders = false;
            collision.sendCollisionMessages = false;

            if (noSnowZones != null && noSnowZones.Length > 0) // lo que se mueve (el tren): adentro no nieva
            {
                var trigger = ps.trigger;
                trigger.enabled = true;
                trigger.inside = ParticleSystemOverlapAction.Kill;
                trigger.enter = ParticleSystemOverlapAction.Kill;
                trigger.outside = ParticleSystemOverlapAction.Ignore;
                trigger.exit = ParticleSystemOverlapAction.Ignore;
                for (int i = 0; i < noSnowZones.Length; i++) trigger.SetCollider(i, noSnowZones[i]);
            }
            return ps;
        }

        private ParticleSystem CreateSnow()
        {
            ParticleSystem ps = NewSystem("Nieve_Copos", flakeMaterial);
            float lifetime = settings.SpawnHeight / settings.FallSpeed * 1.6f;
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(settings.FlakeSizeMin, settings.FlakeSizeMax);
            main.startColor = new Color(1f, 1f, 1f, 0.9f);
            main.maxParticles = Mathf.Clamp(Mathf.CeilToInt(settings.FlakesPerSecond * lifetime), 100, 20000);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(settings.AreaRadius * 2f, 0.2f, settings.AreaRadius * 2f);
            var noise = ps.noise; // el aleteo de cada copo
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.3f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            ps.Play();
            return ps;
        }

        private ParticleSystem CreateDrift()
        {
            ParticleSystem ps = NewSystem("Nieve_Rachas", driftMaterial);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startColor = new Color(1f, 1f, 1f, 0.22f);
            main.maxParticles = Mathf.Clamp(Mathf.CeilToInt(settings.DriftPerSecond * 3f), 10, 4000);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(settings.AreaRadius * 1.6f, 0.5f, settings.AreaRadius * 1.6f);
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch; // rachas: se estiran en la dirección en que van
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 2f;
            ps.Play();
            return ps;
        }

        private void LateUpdate()
        {
            if (follow == null) return;

            // Cuánto nieva: sube y baja despacio con las horas de juego.
            float hours = clock != null ? (float)clock.TotalHours : Time.time / 3600f;
            float slow = Mathf.PerlinNoise(_seed, hours / settings.VariationHours);
            Intensity = Mathf.Lerp(1f - settings.IntensityVariation, 1f, slow);

            // El viento, con ráfagas (en segundos reales: las ráfagas no se aceleran con el tiempo de juego).
            float gust = Mathf.PerlinNoise(_seed + 7.3f, Time.time * settings.GustsPerMinute / 60f) * 2f - 1f;
            Wind = settings.WindDirection * (settings.WindSpeed * Mathf.Max(0f, 1f + settings.GustStrength * gust));

            // Los copos: se sueltan arriba y un poco contra el viento, para que a la altura de los ojos rodeen al jugador.
            Vector3 eye = follow.position;
            // El jugador en movimiento (el tren a 40 km/h): los copos se largan donde va a estar cuando lleguen a sus ojos.
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 step = _hasLast ? eye - _lastFollow : Vector3.zero;
            step.y = 0f;
            if (step.magnitude > 30f) step = Vector3.zero; // un salto (teletransporte): no es velocidad
            _followVelocity = Vector3.Lerp(_followVelocity, step / dt, 1f - Mathf.Exp(-3f * dt));
            _lastFollow = eye;
            _hasLast = true;
            float fallSeconds = Mathf.Max(0f, settings.SpawnHeight - 1.6f) / settings.FallSpeed;
            _snow.transform.position = eye + Vector3.up * settings.SpawnHeight + (_followVelocity - Wind) * fallSeconds;
            var snowVelocity = _snow.velocityOverLifetime;
            snowVelocity.x = Wind.x;
            snowVelocity.y = -settings.FallSpeed;
            snowVelocity.z = Wind.z;
            var snowEmission = _snow.emission;
            snowEmission.rateOverTime = settings.FlakesPerSecond * Intensity;

            // Las rachas a ras del suelo: más con más viento.
            float ground = eye.y - 1.65f;
            _drift.transform.position = new Vector3(eye.x, ground + 0.3f, eye.z) + (_followVelocity - Wind) * 1.2f;
            var driftVelocity = _drift.velocityOverLifetime;
            driftVelocity.x = Wind.x * 1.4f;
            driftVelocity.y = 0f;
            driftVelocity.z = Wind.z * 1.4f;
            var driftEmission = _drift.emission;
            driftEmission.rateOverTime = settings.DriftPerSecond * Mathf.Clamp01(Wind.magnitude / Mathf.Max(0.1f, settings.WindSpeed)) * Intensity;
        }

        public void OnOriginShifted(Vector3 delta)
        {
            Shift(_snow, delta);
            Shift(_drift, delta);
            _lastFollow += delta; // el jugador también se corrió: no es un movimiento suyo
        }

        private void Shift(ParticleSystem ps, Vector3 delta)
        {
            if (ps == null) return;
            int max = ps.main.maxParticles;
            if (_buffer == null || _buffer.Length < max) _buffer = new ParticleSystem.Particle[max];
            int count = ps.GetParticles(_buffer);
            for (int i = 0; i < count; i++) _buffer[i].position += delta;
            ps.SetParticles(_buffer, count);
        }
    }
}
