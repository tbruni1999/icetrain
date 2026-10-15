using SurvivalEnTren.Survival;
using SurvivalEnTren.World;
using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Escarcha en las ventanas del tren según la temperatura de adentro (producción P1; sección 19, "condensación en
    /// ventanas"; la guía de arte: "escarcha en vidrios ligada a la temperatura de la estufa"). Dos capas por vidrio, con las
    /// texturas de la librería: la escarcha del borde (aparece primero) y la que cubre todo el vidrio (con mucho frío).
    /// Con la calefacción andando queda un poco en los bordes; si el tren se enfría, crece despacio (minutos de juego).
    /// Solo dibuja: cambia la transparencia de cada capa.
    /// </summary>
    public class WindowFrost : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] private AtmosphereSettings settings;
        [SerializeField] private TrainInteriorClimate climate;
        [SerializeField] private GameClock clock;
        [Tooltip("La escarcha del borde de cada vidrio (una textura que va de 0 a 1 en el vidrio).")]
        [SerializeField] private Renderer[] edgeLayers;
        [Tooltip("La escarcha que cubre todo el vidrio (una textura repetida a tamaño real).")]
        [SerializeField] private Renderer[] fullLayers;
        [Tooltip("Tamaño real de la textura de escarcha que se repite, en metros (catalogo.json: escala_m).")]
        [SerializeField] private float fullTextureMeters = 0.5f;

        private MaterialPropertyBlock _block;
        private double _lastHours;
        private bool _started;

        /// <summary>Cuánta escarcha hay ahora (0 = nada, 1 = vidrios cubiertos).</summary>
        public float Amount { get; private set; }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            // La escarcha pareja se repite a tamaño real en cada vidrio (cada uno mide distinto).
            foreach (Renderer r in fullLayers)
            {
                Vector3 size = r.transform.lossyScale;
                r.GetPropertyBlock(_block);
                _block.SetVector(BaseMapSt, new Vector4(size.x / fullTextureMeters, size.y / fullTextureMeters, 0f, 0f));
                r.SetPropertyBlock(_block);
            }
        }

        private float Target()
        {
            float t = Mathf.InverseLerp(settings.FrostFullBelowC, settings.FrostClearAboveC, climate.InteriorTemperatureC);
            return Mathf.Lerp(1f, settings.FrostMinimum, t);
        }

        private void LateUpdate()
        {
            double hours = clock != null ? clock.TotalHours : Time.time / 60.0;
            if (!_started)
            {
                Amount = Target(); // al arrancar (o al cargar una partida), ya en su lugar
                _started = true;
            }
            else
            {
                float minutes = Mathf.Max(0f, (float)(hours - _lastHours) * 60f);
                Amount = Mathf.Lerp(Amount, Target(), 1f - Mathf.Exp(-minutes / settings.FrostResponseMinutes));
            }
            _lastHours = hours;

            // El borde se completa primero; el vidrio entero se escarcha con más frío.
            float edge = Mathf.Clamp01(Amount / 0.4f);
            float full = Mathf.Clamp01((Amount - 0.35f) / 0.65f) * 0.85f;
            Apply(edgeLayers, edge);
            Apply(fullLayers, full);
        }

        private void Apply(Renderer[] layers, float alpha)
        {
            foreach (Renderer r in layers)
            {
                r.enabled = alpha > 0.01f;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, new Color(1f, 1f, 1f, alpha));
                r.SetPropertyBlock(_block);
            }
        }
    }
}
