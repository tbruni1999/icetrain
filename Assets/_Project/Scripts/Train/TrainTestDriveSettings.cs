using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Valores de la conducción de prueba del M1 (teclas para acelerar y frenar).
    /// No es la conducción real: reversor, potencia y freno llegan en M2.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Tren/Conducción de prueba", fileName = "TrainTestDriveSettings")]
    public class TrainTestDriveSettings : ScriptableObject
    {
        [Tooltip("Velocidad máxima que permiten las teclas de prueba, en km/h.")]
        [SerializeField, Range(0f, 120f)] private float maxSpeedKmh = 60f;

        [Tooltip("Velocidad objetivo al darle Play, en km/h.")]
        [SerializeField, Range(0f, 120f)] private float startSpeedKmh = 0f;

        [Tooltip("Cuánto sube o baja la velocidad objetivo con cada toque de tecla, en km/h.")]
        [SerializeField, Range(1f, 60f)] private float speedStepKmh = 20f;

        [Tooltip("Qué tan rápido gana velocidad, en metros por segundo cada segundo.")]
        [SerializeField, Range(0.05f, 5f)] private float accelerationMetersPerSecondSquared = 0.5f;

        [Tooltip("Qué tan rápido pierde velocidad al bajarla o frenar, en metros por segundo cada segundo.")]
        [SerializeField, Range(0.05f, 5f)] private float brakingMetersPerSecondSquared = 1f;

        public float MaxSpeedKmh => maxSpeedKmh;
        public float StartSpeedKmh => startSpeedKmh;
        public float SpeedStepKmh => speedStepKmh;
        public float AccelerationMetersPerSecondSquared => accelerationMetersPerSecondSquared;
        public float BrakingMetersPerSecondSquared => brakingMetersPerSecondSquared;
    }
}
