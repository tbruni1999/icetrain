using System.Collections.Generic;
using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Probe
{
    /// <summary>
    /// Pantalla de la sonda principal en la cabina (secciones 13.1, 18.1 y 19: "CRT o pantallas
    /// pequeñas"). Arriba de la pantalla es el frente del tren. Un barrido gira; las señales aparecen
    /// como puntos que se encienden al pasar el barrido y se apagan de a poco. Debajo, la lectura de
    /// la señal elegida (tipo, distancia, dirección, posición por la vía). Una bifurcación (lectura de la red,
    /// sección 14.3) se dibuja como un rombo blanco y dice si está BLOQUEADA o ABIERTA. E pasa a la siguiente señal
    /// y la fija como objetivo de la sonda portátil (punto ámbar).
    /// Solo dibuja: los cálculos están en ProbeScanner.
    /// </summary>
    public class ProbeConsole : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color Trace = new Color(0.45f, 1f, 0.55f);
        private static readonly Color TargetColor = new Color(1f, 0.72f, 0.2f);
        private static readonly Color NetworkColor = new Color(0.92f, 0.95f, 1f);

        [SerializeField] private ProbeScanner scanner;
        [Tooltip("Centro del círculo del radar (local a la pantalla) y su radio, en metros.")]
        [SerializeField] private Transform radarCenter;
        [SerializeField] private float radarRadius = 0.13f;
        [SerializeField] private Transform sweep;
        [SerializeField] private Transform[] blips;
        [SerializeField] private TextMesh header;
        [SerializeField] private TextMesh line1;
        [SerializeField] private TextMesh line2;
        [SerializeField] private TextMesh line3;
        [Tooltip("Cuánto tarda en apagarse un punto después de que pasa el barrido, en segundos.")]
        [SerializeField] private float blipFadeSeconds = 2.5f;
        [Tooltip("La mezcla de sonido (M13): un \"ping\" cuando el barrido pasa por una señal.")]
        [SerializeField] private SurvivalEnTren.Audio.AudioMixSettings audioMix;

        private float _sweepAngle;
        private readonly Dictionary<ProbeSignal, float> _lastLit = new Dictionary<ProbeSignal, float>();
        private MaterialPropertyBlock _block;
        private AudioSource _ping;
        private AudioClip _pingClip;

        /// <summary>Cuántos "ping" dio la pantalla (lo usan las pruebas).</summary>
        public int Pings { get; private set; }

        public string GetPrompt(PlayerInteractor interactor) =>
            scanner.Readings.Count == 0 ? null : scanner.Target == null ? "fijar objetivo" : "siguiente señal";

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor) => scanner.SelectNext();

        private void Update()
        {
            _block ??= new MaterialPropertyBlock();
            float previous = _sweepAngle;
            _sweepAngle = Mathf.Repeat(_sweepAngle + 360f * Time.deltaTime / scanner.Settings.SweepSeconds, 360f);
            sweep.localRotation = Quaternion.Euler(0f, 0f, -_sweepAngle); // gira como un reloj

            var readings = scanner.Readings;
            for (int i = 0; i < blips.Length; i++)
            {
                if (i >= readings.Count)
                {
                    blips[i].gameObject.SetActive(false);
                    continue;
                }
                ProbeReading r = readings[i];
                float bearing = Mathf.Repeat(r.BearingDegrees, 360f);
                if (Passed(previous, _sweepAngle, bearing))
                {
                    _lastLit[r.Signal] = Time.time;
                    Ping(r.Signal == scanner.Target);
                }
                _lastLit.TryGetValue(r.Signal, out float lit);
                float glow = Mathf.Clamp01(1f - (Time.time - lit) / blipFadeSeconds);

                float radius = radarRadius * Mathf.Clamp01(r.Distance / scanner.Settings.MainRangeMeters);
                float a = bearing * Mathf.Deg2Rad;
                blips[i].gameObject.SetActive(true);
                blips[i].localPosition = radarCenter.localPosition + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * radius;
                // La lectura de la red (una bifurcación) es un rombo blanco: se distingue de las señales de lugares (puntos verdes).
                bool network = r.Signal.IsNetworkMarker;
                blips[i].localRotation = Quaternion.Euler(0f, 0f, network ? 45f : 0f);
                Color color = r.Signal == scanner.Target ? TargetColor : network ? NetworkColor : Trace;
                _block.SetColor(BaseColor, color * Mathf.Lerp(0.25f, 1f, glow));
                blips[i].GetComponent<Renderer>().SetPropertyBlock(_block);
            }

            WriteText(readings);
        }


        // Un "ping" de la pantalla cuando el barrido pasa por una señal: más agudo si es el objetivo fijado.
        private void Ping(bool isTarget)
        {
            if (audioMix == null) return;
            if (_ping == null)
            {
                _ping = gameObject.AddComponent<AudioSource>();
                _ping.spatialBlend = 1f;
                _ping.minDistance = 1.5f;
                _ping.maxDistance = 15f;
                _ping.playOnAwake = false;
                _pingClip = SurvivalEnTren.Audio.ProceduralClips.Beep("Consola_Ping", 880f, 0.09f);
            }
            _ping.pitch = isTarget ? 1.5f : 1f;
            _ping.PlayOneShot(_pingClip, audioMix.ProbeVolume);
            Pings++;
        }
        // ¿El barrido pasó por este ángulo en este cuadro?
        private static bool Passed(float from, float to, float angle)
        {
            if (to >= from) return angle >= from && angle < to;
            return angle >= from || angle < to;
        }

        private void WriteText(IReadOnlyList<ProbeReading> readings)
        {
            header.text = $"SONDA   {readings.Count} SEÑAL{(readings.Count == 1 ? "" : "ES")}";
            if (scanner.Target == null || !scanner.TryGetReading(scanner.Target, out ProbeReading r))
            {
                line1.text = readings.Count == 0 ? "SIN SEÑALES AL ALCANCE" : "E: FIJAR OBJETIVO";
                line2.text = "";
                line3.text = "";
                return;
            }
            if (r.Signal.IsNetworkMarker)
            {
                // Lectura de la red (sección 14.3): estado de la bifurcación y que la ruta es desconocida.
                line1.text = $"OBJETIVO: {ProbeSignal.DisplayName(r.Signal.Type)} {r.Signal.StatusText}";
                line2.text = $"~{Km(r.Distance)}   {Direction(r.BearingDegrees)}";
                line3.text = r.AlongKnown
                    ? $"RUTA DESCONOCIDA  {Km(Mathf.Abs(r.AlongMeters))} {(r.AlongMeters >= 0 ? "ADELANTE" : "ATRÁS")}"
                    : "RUTA DESCONOCIDA  FUERA DE RUTA";
                return;
            }
            line1.text = "OBJETIVO: " + ProbeSignal.DisplayName(r.Signal.Type);
            line2.text = $"~{Km(r.Distance)}   {Direction(r.BearingDegrees)}";
            line3.text = r.AlongKnown
                ? $"POR LA VÍA {Km(Mathf.Abs(r.AlongMeters))} {(r.AlongMeters >= 0 ? "ADELANTE" : "ATRÁS")}, {Km(r.LateralMeters)} A LA {(r.Side > 0 ? "DER." : "IZQ.")}"
                : "FUERA DE LA RUTA ACTUAL";
        }

        private static string Km(float meters) =>
            meters >= 1000f ? $"{meters / 1000f:0.0} KM" : $"{meters:0} M";

        private static string Direction(float bearing)
        {
            float b = Mathf.DeltaAngle(0f, bearing);
            if (Mathf.Abs(b) <= 15f) return "ADELANTE";
            if (Mathf.Abs(b) >= 165f) return "ATRÁS";
            string side = b > 0f ? "DER." : "IZQ.";
            return Mathf.Abs(b) < 75f ? $"ADELANTE {side}" : Mathf.Abs(b) <= 105f ? side : $"ATRÁS {side}";
        }
    }
}
