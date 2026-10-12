using SurvivalEnTren.Interaction;
using SurvivalEnTren.Train;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Probe
{
    /// <summary>
    /// Sonda portátil (sección 13.2, DECISIÓN: no hay minimapa; esta sonda reemplaza gran parte de esa
    /// función). Se lleva en la mano: Q la saca y la guarda.
    /// - Aguja blanca: dirección al tren. Aguja ámbar: dirección aproximada al objetivo fijado en la
    ///   consola del tren. Las dos relativas a hacia dónde mira el jugador (arriba = adelante).
    /// - Distancias aproximadas y una barra de calidad de señal (baja con la distancia y la interferencia).
    /// - Interferencias (ProbeInterference): las agujas tiemblan y la calidad baja.
    /// - Con algo grande en las manos (una caja) se guarda sola; con un bidón (una mano) se puede seguir usando.
    /// </summary>
    public class PortableProbe : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ProbeSettings settings;
        [SerializeField] private ProbeScanner scanner;
        [SerializeField] private Transform head;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private InputActionReference toggle;

        [Header("Aparato (hijos de la cámara)")]
        [SerializeField] private Transform device;
        [SerializeField] private Transform trainNeedle;
        [SerializeField] private Transform targetNeedle;
        [SerializeField] private TextMesh trainText;
        [SerializeField] private TextMesh targetText;
        [SerializeField] private Renderer[] qualityBars;
        [Tooltip("Posición del aparato en la mano (local a la cámara) y cuánto baja al guardarlo, en metros.")]
        [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.15f, -0.12f, 0.34f);
        [SerializeField] private float stowDrop = 0.25f;

        private bool _out;
        private float _raise;
        private float _trainAngle, _targetAngle;
        private MaterialPropertyBlock _block;
        private ProbeInterference[] _zones;

        public bool IsOut => _out && _raise > 0.99f;
        public float Quality { get; private set; }
        public float Interference { get; private set; }
        /// <summary>Dirección que marca cada aguja respecto de hacia dónde mira el jugador, en grados.</summary>
        public float TrainNeedleDegrees => _trainAngle;
        public float TargetNeedleDegrees => _targetAngle;

        private void Start()
        {
            _zones = FindObjectsByType<ProbeInterference>();
            device.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            toggle.action.performed += OnToggle;
            toggle.action.Enable();
        }

        private void OnDisable() => toggle.action.performed -= OnToggle;

        private void OnToggle(InputAction.CallbackContext _) => SetOut(!_out);

        public void SetOut(bool value) => _out = value;

        private void LateUpdate()
        {
            bool handsBusy = carrier != null && carrier.Holding != null && !carrier.Holding.OneHanded; // un bidón deja una mano libre
            bool wantOut = _out && !handsBusy;
            _raise = Mathf.MoveTowards(_raise, wantOut ? 1f : 0f, Time.deltaTime * 4f);
            device.gameObject.SetActive(_raise > 0f);
            if (_raise <= 0f) return;
            device.localPosition = heldLocalPosition + Vector3.down * (stowDrop * (1f - Mathf.SmoothStep(0f, 1f, _raise)));

            Vector3 here = head.position;
            Interference = 0f;
            foreach (ProbeInterference zone in _zones) Interference = Mathf.Max(Interference, zone.At(here));
            float jitter = settings.InterferenceNeedleDegrees * Interference;
            float k = 1f - Mathf.Exp(-settings.NeedleSpeed * Time.deltaTime);

            // Aguja al tren.
            Vector3 trainPosition = scanner.Train.transform.position;
            float trainDistance = Flat(trainPosition - here).magnitude;
            bool trainInRange = trainDistance <= settings.PortableRangeMeters;
            float trainBearing = Bearing(trainPosition) + Noise(0.7f) * jitter;
            _trainAngle = Mathf.LerpAngle(_trainAngle, trainBearing, k);
            trainNeedle.gameObject.SetActive(trainInRange);
            trainNeedle.localRotation = Quaternion.Euler(0f, 0f, -_trainAngle);
            trainText.text = trainInRange ? $"TREN  ~{Approx(trainDistance)}" : "TREN  SIN LECTURA";

            // Aguja al objetivo fijado en la consola (aproximada, como en la consola).
            ProbeSignal target = scanner.Target;
            float targetDistance = target != null ? Flat(target.transform.position - here).magnitude : 0f;
            bool targetInRange = target != null && targetDistance <= settings.PortableRangeMeters;
            if (targetInRange)
            {
                float farness = settings.Farness(targetDistance, settings.PortableRangeMeters);
                float bearing = Bearing(target.transform.position) + target.StableNoise(4) * settings.BearingError(farness) * 0.5f
                                + Noise(1.3f) * jitter;
                _targetAngle = Mathf.LerpAngle(_targetAngle, bearing, k);
                targetNeedle.localRotation = Quaternion.Euler(0f, 0f, -_targetAngle);
                targetText.text = $"OBJ.  ~{Approx(targetDistance)}";
            }
            else targetText.text = target == null ? "OBJ.  SIN OBJETIVO" : "OBJ.  SIN LECTURA";
            targetNeedle.gameObject.SetActive(targetInRange);

            // Calidad: la mejor de las dos lecturas, menos la interferencia.
            float best = Mathf.Max(trainInRange ? 1f - trainDistance / settings.PortableRangeMeters : 0f,
                targetInRange ? 1f - targetDistance / settings.PortableRangeMeters : 0f);
            Quality = Mathf.Clamp01(best * (1f - Interference));
            _block ??= new MaterialPropertyBlock();
            for (int i = 0; i < qualityBars.Length; i++)
            {
                bool lit = Quality > (i + 0.5f) / qualityBars.Length;
                _block.SetColor(BaseColor, lit ? new Color(0.45f, 1f, 0.55f) : new Color(0.08f, 0.18f, 0.1f));
                qualityBars[i].SetPropertyBlock(_block);
            }
        }

        // Ángulo hacia un punto respecto de hacia dónde mira el jugador (horizontal), en grados.
        private float Bearing(Vector3 point) =>
            Vector3.SignedAngle(Flat(head.forward), Flat(point - head.position), Vector3.up);

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static float Noise(float seed) => (Mathf.PerlinNoise(Time.time * 3f, seed * 10f) - 0.5f) * 2f;

        private string Approx(float meters)
        {
            float step = settings.Rounding(settings.Farness(meters, settings.PortableRangeMeters));
            float rounded = Mathf.Max(step, Mathf.Round(meters / step) * step);
            return rounded >= 1000f ? $"{rounded / 1000f:0.0} KM" : $"{rounded:0} M";
        }
    }
}
