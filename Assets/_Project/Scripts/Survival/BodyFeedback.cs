using SurvivalEnTren.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Feedback del cuerpo antes que los números (secciones 7 y 18.2). Nunca toca los controles.
    /// - Frío: borde escarchado (viñeta blanco-azulada), imagen más fría y desaturada; vaho al
    ///   respirar si el aire está frío; temblor leve de la cámara con el cuerpo muy frío.
    /// - Poca energía: bordes oscuros y "pestañeos" pesados de vez en cuando.
    /// - Vida baja: pulso rojizo muy sutil en los bordes.
    /// - Hambre: sin efecto visual (sección 18.2: "feedback corporal, no alarmas"; el audio es M13).
    /// Usa un Volume de postproceso de URP (viñeta y ajustes de color) de la escena.
    /// - Se suma a la atmósfera (P1): la viñeta, el color y la temperatura de base salen del perfil "baseline" (el del
    ///   post-proceso del ambiente) y el cuerpo agrega lo suyo encima, en vez de pisarlo.
    /// Corre después de que PlayerLook ubica la cámara (orden 100), para sumarle el temblor.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class BodyFeedback : MonoBehaviour
    {
        private static readonly Color FrostColor = new Color(0.42f, 0.58f, 0.82f); // azul grisáceo: se ve también sobre la nieve
        private static readonly Color HurtColor = new Color(0.55f, 0.05f, 0.05f);

        [SerializeField] private PlayerVitals vitals;
        [SerializeField] private Transform head;
        [SerializeField] private Volume volume;
        [SerializeField] private Material breathMaterial;
        [Tooltip("Opcional: el perfil del post-proceso del ambiente. Su viñeta, saturación, exposición y temperatura son la base.")]
        [SerializeField] private VolumeProfile baseline;

        [Header("Frío")]
        [Tooltip("Por debajo de este calor del cuerpo empieza la escarcha en los bordes.")]
        [SerializeField, Range(0f, 100f)] private float frostBelowWarmth = 60f;
        [Tooltip("Aire por debajo de esta temperatura: se ve el vaho al respirar, en °C.")]
        [SerializeField, Range(-20f, 20f)] private float breathBelowC = 5f;
        [Tooltip("Por debajo de este calor del cuerpo la cámara tiembla.")]
        [SerializeField, Range(0f, 100f)] private float shiverBelowWarmth = 25f;
        [Tooltip("Temblor máximo de la cámara, en grados.")]
        [SerializeField, Range(0f, 3f)] private float maxShiverDegrees = 0.6f;

        [Header("Energía y vida")]
        [Tooltip("Por debajo de esta energía la vista se pone pesada.")]
        [SerializeField, Range(0f, 100f)] private float tiredBelowEnergy = 25f;
        [Tooltip("Por debajo de esta vida aparece el pulso rojizo.")]
        [SerializeField, Range(0f, 100f)] private float hurtBelowHealth = 30f;

        private Texture2D _frostTexture;
        private Vignette _vignette;
        private ColorAdjustments _color;
        private WhiteBalance _white;
        private ParticleSystem _breath;
        private float _breathTimer;
        private float _blinkTimer;
        private float _blink;
        private float _baseVignette, _baseSaturation, _baseExposure, _baseTemperature;

        /// <summary>Intensidad actual de la escarcha (0-1), para pruebas.</summary>
        public float Frost { get; private set; }
        public float Tiredness { get; private set; }
        public float Hurt { get; private set; }
        public float Shiver { get; private set; }
        public bool BreathVisible { get; private set; }

        private void Awake()
        {
            useGUILayout = false; // dibuja con GUI.* (sin GUILayout): menos memoria nueva por cuadro (M13)
            VolumeProfile profile = volume.profile; // copia propia en tiempo de juego (no toca el asset)
            profile.TryGet(out _vignette);
            profile.TryGet(out _color);
            profile.TryGet(out _white);
            if (baseline != null)
            {
                // El perfil del cuerpo pisa todos los valores de sus componentes (viñeta, color, balance de blancos): se le copian
                // los del ambiente, y los que el cuerpo mueve cada cuadro se guardan como base para sumarles el efecto.
                foreach (VolumeComponent baseComponent in baseline.components)
                {
                    if (!profile.TryGet(baseComponent.GetType(), out VolumeComponent mine)) continue;
                    for (int i = 0; i < baseComponent.parameters.Count; i++)
                    {
                        if (!baseComponent.parameters[i].overrideState) continue;
                        mine.parameters[i].SetValue(baseComponent.parameters[i]);
                        mine.parameters[i].overrideState = true;
                    }
                }
                if (baseline.TryGet(out Vignette v) && v.intensity.overrideState) _baseVignette = v.intensity.value;
                if (baseline.TryGet(out ColorAdjustments c))
                {
                    if (c.saturation.overrideState) _baseSaturation = c.saturation.value;
                    if (c.postExposure.overrideState) _baseExposure = c.postExposure.value;
                }
                if (baseline.TryGet(out WhiteBalance w) && w.temperature.overrideState) _baseTemperature = w.temperature.value;
            }
            _breath = CreateBreath();
            _frostTexture = CreateFrostTexture(256);
        }

        private void OnDestroy()
        {
            if (_frostTexture != null) Destroy(_frostTexture);
        }

        // Escarcha: cristales de hielo que crecen desde los bordes de la pantalla.
        private void OnGUI()
        {
            Matrix4x4 uiOld = UiScale.Begin(); // la interfaz en puntos, agrandada según la pantalla
            DrawGui();
            GUI.matrix = uiOld;
        }

        private void DrawGui()
        {
            if (Frost <= 0.01f) return;
            GUI.depth = 10; // por debajo del HUD
            GUI.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Frost));
            GUI.DrawTexture(new Rect(0f, 0f, UiScale.Width, UiScale.Height), _frostTexture, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        // Textura generada por código (sin arte): opaca cerca de los bordes, transparente al centro,
        // con ruido de varias escalas para que parezcan cristales.
        private static Texture2D CreateFrostTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (size - 1f), v = y / (size - 1f);
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 2f; // 0 en el borde, 1 al centro
                    float noise = Mathf.PerlinNoise(u * 9f, v * 9f) * 0.55f + Mathf.PerlinNoise(u * 31f + 7f, v * 31f + 3f) * 0.3f
                                + Mathf.PerlinNoise(u * 83f + 1f, v * 83f + 9f) * 0.15f;
                    float reach = 0.28f + noise * 0.35f;                     // hasta dónde llega la escarcha
                    float a = Mathf.Clamp01((reach - edge) / reach);
                    a = Mathf.Pow(a, 1.6f) * (0.65f + 0.35f * noise);
                    byte alpha = (byte)(Mathf.Clamp01(a) * 235f);
                    pixels[y * size + x] = new Color32(225, 238, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            Frost = Mathf.Clamp01((frostBelowWarmth - vitals.Warmth) / frostBelowWarmth);
            Tiredness = Mathf.Clamp01((tiredBelowEnergy - vitals.Energy) / tiredBelowEnergy);
            Hurt = Mathf.Clamp01((hurtBelowHealth - vitals.Health) / hurtBelowHealth);
            Shiver = Mathf.Clamp01((shiverBelowWarmth - vitals.Warmth) / shiverBelowWarmth);

            // "Pestañeos" con sueño: cada tanto la vista se oscurece un momento.
            _blinkTimer -= dt;
            if (Tiredness > 0.3f && _blinkTimer <= 0f)
            {
                _blink = 1f;
                _blinkTimer = Mathf.Lerp(9f, 4f, Tiredness) + Random.Range(0f, 2f);
            }
            _blink = Mathf.MoveTowards(_blink, 0f, dt * 2.5f);

            // Viñeta: escarcha (fría), cansancio (oscura) o herida (rojiza), lo que pese más.
            float hurtPulse = Hurt * (0.6f + 0.4f * Mathf.Sin(Time.time * 3f));
            Color color = FrostColor;
            float intensity = Frost * 0.55f;
            if (Tiredness * 0.4f > intensity) { color = Color.black; intensity = Tiredness * 0.4f; }
            if (hurtPulse * 0.4f > intensity) { color = HurtColor; intensity = hurtPulse * 0.4f; }
            if (_baseVignette > intensity) { color = Color.black; intensity = _baseVignette; } // la viñeta leve del ambiente
            if (_vignette != null)
            {
                _vignette.color.Override(color);
                _vignette.intensity.Override(intensity);
                _vignette.smoothness.Override(0.8f);
            }
            if (_color != null)
            {
                _color.saturation.Override(_baseSaturation - 35f * Frost - 25f * Tiredness);
                _color.postExposure.Override(_baseExposure - 1.5f * _blink * Tiredness);
            }
            if (_white != null) _white.temperature.Override(_baseTemperature - 25f * Frost);

            // Temblor de frío: un poco de ruido en la rotación de la cámara.
            if (Shiver > 0f)
            {
                float t = Time.time * 22f;
                float a = maxShiverDegrees * Shiver;
                head.localRotation *= Quaternion.Euler((Mathf.PerlinNoise(t, 0f) - 0.5f) * a, (Mathf.PerlinNoise(0f, t) - 0.5f) * a, 0f);
            }

            // Vaho al respirar si el aire está frío.
            BreathVisible = vitals.AmbientTemperatureC < breathBelowC;
            _breathTimer -= dt;
            if (BreathVisible && _breathTimer <= 0f)
            {
                _breathTimer = Random.Range(2.8f, 3.6f);
                _breath.transform.SetPositionAndRotation(head.position + head.forward * 0.18f - head.up * 0.06f, head.rotation);
                _breath.Emit(Random.Range(10, 16));
            }
        }

        private ParticleSystem CreateBreath()
        {
            var go = new GameObject("Vaho");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = new Color(1f, 1f, 1f, 0.22f);
            main.gravityModifier = -0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.01f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.25f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = gradient;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = breathMaterial;
            return ps;
        }
    }
}
