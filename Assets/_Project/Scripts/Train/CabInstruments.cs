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

        private float _reverser, _power, _brake;

        private void LateUpdate()
        {
            TrainDriveSettings settings = controls.Settings;
            float k = 1f - Mathf.Exp(-leverSpeed * Time.deltaTime);

            // Reversor: atrás / neutro / adelante. Potencia: hacia adelante. Freno: hacia el maquinista.
            _reverser = Mathf.Lerp(_reverser, (int)controls.Reverser * leverMaxDegrees, k);
            _power = Mathf.Lerp(_power, leverMaxDegrees * controls.PowerNotch / Mathf.Max(1, settings.PowerNotchCount), k);
            _brake = Mathf.Lerp(_brake, -leverMaxDegrees * controls.BrakeNotch / Mathf.Max(1, settings.BrakeNotchCount), k);
            reverserPivot.localRotation = Quaternion.Euler(_reverser, 0f, 0f);
            powerPivot.localRotation = Quaternion.Euler(_power, 0f, 0f);
            brakePivot.localRotation = Quaternion.Euler(_brake, 0f, 0f);

            // Aguja: de abajo a la izquierda (0) a abajo a la derecha (máximo), como un reloj.
            float kmh = Mathf.Abs(train.SpeedMetersPerSecond) * 3.6f;
            float t = Mathf.Clamp01(kmh / gaugeMaxKmh);
            needlePivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(needleSweepDegrees, -needleSweepDegrees, t));
        }
    }
}
