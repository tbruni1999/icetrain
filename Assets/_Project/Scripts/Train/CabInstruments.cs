using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Instrumentos físicos de la cabina (sección 18.1: la información del tren va en el tren,
    /// no en la pantalla): las tres palancas se inclinan según su posición y la aguja del
    /// velocímetro marca la velocidad. Solo dibuja; el estado vive en TrainControls.
    /// </summary>
    public class CabInstruments : MonoBehaviour
    {
        [SerializeField] private TrainControls controls;
        [SerializeField] private TrainMover train;
        [SerializeField] private Transform reverserPivot;
        [SerializeField] private Transform powerPivot;
        [SerializeField] private Transform brakePivot;
        [SerializeField] private Transform needlePivot;

        [Tooltip("Velocidad del final de la escala del velocímetro, en km/h.")]
        [SerializeField] private float gaugeMaxKmh = 60f;
        [Tooltip("Inclinación máxima de las palancas, en grados.")]
        [SerializeField] private float leverMaxDegrees = 35f;
        [Tooltip("Ángulo de la aguja en 0 y en el máximo de la escala, en grados (0 = apunta arriba).")]
        [SerializeField] private float needleSweepDegrees = 120f;
        [Tooltip("Qué tan rápido se mueven las palancas hasta su posición (más alto = más brusco).")]
        [SerializeField] private float leverSpeed = 14f;

        [Tooltip("Cuánto se sacude la palanca del reversor si se la quiere mover con el tren en marcha, en grados.")]
        [SerializeField] private float blockedShakeDegrees = 7f;
        [Tooltip("Cuánto dura esa sacudida, en segundos.")]
        [SerializeField] private float blockedShakeSeconds = 0.35f;

        private float _reverser, _power, _brake;
        private float _shakeTimeLeft;

        private void OnEnable()
        {
            controls.ReverserBlocked += OnReverserBlocked;
        }

        private void OnDisable()
        {
            controls.ReverserBlocked -= OnReverserBlocked;
        }

        // El reversor no se puede mover en marcha: la palanca se sacude y vuelve, como si estuviera trabada.
        private void OnReverserBlocked() => _shakeTimeLeft = blockedShakeSeconds;

        private void LateUpdate()
        {
            TrainDriveSettings settings = controls.Settings;
            float k = 1f - Mathf.Exp(-leverSpeed * Time.deltaTime);

            // Reversor: atrás / neutro / adelante. Potencia: hacia adelante. Freno: hacia el maquinista.
            _reverser = Mathf.Lerp(_reverser, (int)controls.Reverser * leverMaxDegrees, k);
            _power = Mathf.Lerp(_power, leverMaxDegrees * controls.PowerNotch / Mathf.Max(1, settings.PowerNotchCount), k);
            _brake = Mathf.Lerp(_brake, -leverMaxDegrees * controls.BrakeNotch / Mathf.Max(1, settings.BrakeNotchCount), k);
            float shake = 0f;
            if (_shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= Time.deltaTime;
                shake = Mathf.Sin(_shakeTimeLeft * 45f) * blockedShakeDegrees * Mathf.Clamp01(_shakeTimeLeft / blockedShakeSeconds);
            }
            reverserPivot.localRotation = Quaternion.Euler(_reverser + shake, 0f, 0f);
            powerPivot.localRotation = Quaternion.Euler(_power, 0f, 0f);
            brakePivot.localRotation = Quaternion.Euler(_brake, 0f, 0f);

            // Aguja: de abajo a la izquierda (0) a abajo a la derecha (máximo), como un reloj.
            float kmh = Mathf.Abs(train.SpeedMetersPerSecond) * 3.6f;
            float t = Mathf.Clamp01(kmh / gaugeMaxKmh);
            needlePivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(needleSweepDegrees, -needleSweepDegrees, t));
        }
    }
}
