using System;
using System.Collections.Generic;
using SurvivalEnTren.Rail;
using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Probe
{
    /// <summary>Lo que la sonda "cree" de una señal: aproximado, no exacto (sección 13.1).</summary>
    public struct ProbeReading
    {
        public ProbeSignal Signal;
        /// <summary>Dirección aproximada respecto del frente del tren, en grados (0 = adelante, + = derecha).</summary>
        public float BearingDegrees;
        /// <summary>Distancia en línea recta aproximada y redondeada, en metros.</summary>
        public float Distance;
        /// <summary>¿Se llega por la vía con los cambios como están?</summary>
        public bool AlongKnown;
        /// <summary>Distancia por la vía, redondeada: + adelante, - atrás.</summary>
        public float AlongMeters;
        /// <summary>Separación de la vía, redondeada, y de qué lado (1 derecha, -1 izquierda, mirando el sentido del tramo).</summary>
        public float LateralMeters;
        public int Side;
    }

    /// <summary>
    /// Sonda principal del tren (sección 13.1): barre alrededor del tren y detecta las señales a su
    /// alcance, con incertidumbre (dirección y distancia aproximadas, más precisas de cerca; el error
    /// de cada señal es estable para que la lectura no salte). Dice el tipo de señal, no el contenido.
    /// Guarda el objetivo fijado, que la sonda portátil usa (sección 13.2).
    /// </summary>
    public class ProbeScanner : MonoBehaviour
    {
        [SerializeField] private ProbeSettings settings;
        [SerializeField] private TrainMover train;

        private readonly List<ProbeSignal> _signals = new List<ProbeSignal>();
        private readonly List<ProbeReading> _readings = new List<ProbeReading>();

        public IReadOnlyList<ProbeReading> Readings => _readings;
        public ProbeSignal Target { get; private set; }
        public ProbeSettings Settings => settings;
        public TrainMover Train => train;

        public event Action TargetChanged;

        private void Start()
        {
            // Señales del mundo; más adelante las regiones las agregan al cargarse (Register).
            foreach (ProbeSignal s in FindObjectsByType<ProbeSignal>()) Register(s);
        }

        public void Register(ProbeSignal signal)
        {
            if (signal != null && !_signals.Contains(signal)) _signals.Add(signal);
        }

        private void Update()
        {
            _readings.Clear();
            Vector3 origin = train.transform.position;
            foreach (ProbeSignal signal in _signals)
            {
                if (signal == null) continue;
                float range = settings.MainRangeMeters * signal.Strength;
                Vector3 offset = signal.transform.position - origin;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance > range) continue;
                _readings.Add(Read(signal, offset, distance, range));
            }
            if (Target != null && !IsDetected(Target)) SetTarget(null); // salió del alcance
        }

        private ProbeReading Read(ProbeSignal signal, Vector3 offset, float distance, float range)
        {
            float farness = settings.Farness(distance, range);
            Vector3 forward = Vector3.ProjectOnPlane(train.transform.forward, Vector3.up);
            float bearing = Vector3.SignedAngle(forward, offset, Vector3.up) + signal.StableNoise(1) * settings.BearingError(farness);
            float rounding = settings.Rounding(farness);
            float approx = distance * (1f + signal.StableNoise(2) * settings.DistanceError(farness));

            var reading = new ProbeReading
            {
                Signal = signal,
                BearingDegrees = Mathf.DeltaAngle(0f, bearing),
                Distance = Round(approx, rounding),
                Side = signal.AnchorSide,
                LateralMeters = Round(signal.AnchorLateralMeters * (1f + signal.StableNoise(3) * settings.DistanceError(farness)), rounding)
            };
            RailPosition anchor = signal.Anchor;
            if (anchor.IsValid && RailNavigation.TryDistanceAlong(train.RailPosition, anchor.Segment, anchor.Distance,
                    settings.RailSearchMeters, out float along))
            {
                reading.AlongKnown = true;
                reading.AlongMeters = Round(along, rounding);
            }
            return reading;
        }

        private static float Round(float value, float step) => Mathf.Round(value / step) * step;

        public bool IsDetected(ProbeSignal signal)
        {
            foreach (ProbeReading r in _readings) if (r.Signal == signal) return true;
            return false;
        }

        public bool TryGetReading(ProbeSignal signal, out ProbeReading reading)
        {
            foreach (ProbeReading r in _readings)
            {
                if (r.Signal != signal) continue;
                reading = r;
                return true;
            }
            reading = default;
            return false;
        }

        /// <summary>Pasa a la siguiente señal detectada (ordenadas por distancia) y la fija como objetivo.</summary>
        public void SelectNext()
        {
            if (_readings.Count == 0) { SetTarget(null); return; }
            var sorted = new List<ProbeReading>(_readings);
            sorted.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            int index = sorted.FindIndex(r => r.Signal == Target);
            SetTarget(sorted[(index + 1) % sorted.Count].Signal);
        }

        private void SetTarget(ProbeSignal signal)
        {
            if (Target == signal) return;
            Target = signal;
            TargetChanged?.Invoke();
        }
    }
}
