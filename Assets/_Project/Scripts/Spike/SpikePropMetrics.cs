using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEnTren.Spike
{
    /// <summary>
    /// Mide qué tan estables quedan las cajas apoyadas dentro del tren (Anexo C.2, "Props"):
    /// - Corrimiento: cuánto se movió cada caja respecto del tren desde que se asentó, en cm.
    /// - Temblor: salto de un cuadro a otro respecto del tren (segunda diferencia), en mm,
    ///   máximo de los últimos 2 segundos.
    /// - Caídas: cajas que terminaron fuera del tren.
    /// Una caja se considera "asentada" cuando pasa 1 segundo casi quieta respecto del tren.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class SpikePropMetrics : MonoBehaviour
    {
        private const float SettleSpeed = 0.05f;
        private const float SettleSeconds = 1f;
        private const float JitterWindowSeconds = 2f;

        [SerializeField] private Rigidbody train;
        [Tooltip("Cajas que se miden. Las que suelta el jugador se agregan solas.")]
        [SerializeField] private List<Rigidbody> props = new List<Rigidbody>();

        private class Track
        {
            public Rigidbody Body;
            public bool Settled;
            public float StillTime;
            public Vector3 Anchor;
            public Vector3 Last, BeforeLast;
            public int Samples;
            public float MaxDisplacement;
        }

        private readonly List<Track> _tracks = new List<Track>();
        private float _windowMax;
        private float _windowTimer;

        public int Count => _tracks.Count;
        public int SettledCount { get; private set; }
        public int OutsideCount { get; private set; }
        public float MaxDisplacementCentimeters { get; private set; }
        public float JitterMillimeters { get; private set; }

        private void Start()
        {
            foreach (var body in props) Add(body);
        }

        public void Add(Rigidbody body)
        {
            if (body != null) _tracks.Add(new Track { Body = body });
        }

        /// <summary>Vuelve a tomar como punto de partida la posición actual de todas las cajas.</summary>
        public void ResetAnchors()
        {
            foreach (var t in _tracks)
            {
                t.Settled = false;
                t.StillTime = 0f;
                t.MaxDisplacement = 0f;
            }
        }

        private void LateUpdate()
        {
            Transform frame = train.transform;
            int settled = 0;
            int outside = 0;
            float maxDisplacement = 0f;

            foreach (var t in _tracks)
            {
                if (t.Body == null) continue;
                Vector3 local = frame.InverseTransformPoint(t.Body.transform.position);

                if (!IsInsideTrain(local))
                {
                    outside++;
                    continue;
                }

                // Temblor visual respecto del tren.
                if (t.Samples >= 2)
                    _windowMax = Mathf.Max(_windowMax, (local - 2f * t.Last + t.BeforeLast).magnitude * 1000f);
                t.BeforeLast = t.Last;
                t.Last = local;
                t.Samples++;

                // Asentarse: casi quieta respecto del tren durante un rato.
                Vector3 relative = t.Body.linearVelocity - train.GetPointVelocity(t.Body.worldCenterOfMass);
                if (!t.Settled)
                {
                    t.StillTime = relative.magnitude < SettleSpeed ? t.StillTime + Time.deltaTime : 0f;
                    if (t.StillTime >= SettleSeconds)
                    {
                        t.Settled = true;
                        t.Anchor = local;
                    }
                    continue;
                }

                settled++;
                t.MaxDisplacement = Mathf.Max(t.MaxDisplacement, (local - t.Anchor).magnitude * 100f);
                maxDisplacement = Mathf.Max(maxDisplacement, t.MaxDisplacement);
            }

            SettledCount = settled;
            OutsideCount = outside;
            MaxDisplacementCentimeters = maxDisplacement;

            _windowTimer += Time.unscaledDeltaTime;
            if (_windowTimer >= JitterWindowSeconds)
            {
                JitterMillimeters = _windowMax;
                _windowMax = 0f;
                _windowTimer = 0f;
            }
        }

        // Caja dentro del volumen del tren (coordenadas locales del tren, en metros).
        private static bool IsInsideTrain(Vector3 local)
        {
            return Mathf.Abs(local.x) < 1.6f && Mathf.Abs(local.z) < 9.6f && local.y > 0.7f && local.y < 3.6f;
        }
    }
}
