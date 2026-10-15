using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Ruedas que giran según la velocidad del tren (arte del vagón MR-04): cada eje gira sobre el eje x del tren, una vuelta
    /// por cada 2·π·radio metros recorridos, hacia adelante o hacia atrás. Solo dibuja.
    /// </summary>
    public class TrainWheels : MonoBehaviour
    {
        [SerializeField] private TrainMover train;
        [SerializeField] private Transform[] axles;
        [Tooltip("Radio de las ruedas, en metros.")]
        [SerializeField] private float wheelRadius = 0.4f;

        private Quaternion[] _rest;
        private float _degrees;

        private void Awake()
        {
            // Cómo está girado cada eje respecto del tren (el arte viene girado 180°): se gira encima de eso.
            _rest = new Quaternion[axles.Length];
            for (int i = 0; i < axles.Length; i++)
                _rest[i] = Quaternion.Inverse(transform.rotation) * axles[i].rotation;
        }

        private void LateUpdate()
        {
            _degrees = Mathf.Repeat(_degrees + train.SpeedMetersPerSecond * Time.deltaTime / wheelRadius * Mathf.Rad2Deg, 360f);
            Quaternion spin = transform.rotation * Quaternion.AngleAxis(_degrees, Vector3.right);
            for (int i = 0; i < axles.Length; i++)
                axles[i].rotation = spin * _rest[i];
        }
    }
}
