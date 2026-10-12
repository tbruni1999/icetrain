using SurvivalEnTren.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Conducción de prueba del M1: teclas para subir/bajar la velocidad objetivo y frenar.
    /// Existe solo para probar el movimiento del jugador dentro del tren; la conducción
    /// real (reversor, potencia, freno) es del M2 y reemplaza a este componente.
    /// </summary>
    public class TrainTestDriver : MonoBehaviour
    {
        private const float KmhToMetersPerSecond = 1f / 3.6f;

        [SerializeField] private TrainMover train;
        [SerializeField] private TrainTestDriveSettings settings;
        [SerializeField] private InputActionReference faster;
        [SerializeField] private InputActionReference slower;
        [SerializeField] private InputActionReference brake;

        private float _targetSpeedKmh;

        public float TargetSpeedKmh => _targetSpeedKmh;
        public float CurrentSpeedKmh => train != null ? train.SpeedMetersPerSecond / KmhToMetersPerSecond : 0f;

        private void Start()
        {
            _targetSpeedKmh = Mathf.Min(settings.StartSpeedKmh, settings.MaxSpeedKmh);
        }

        private void OnEnable()
        {
            Subscribe(faster, OnFaster);
            Subscribe(slower, OnSlower);
            Subscribe(brake, OnBrake);
        }

        private void OnDisable()
        {
            Unsubscribe(faster, OnFaster);
            Unsubscribe(slower, OnSlower);
            Unsubscribe(brake, OnBrake);
        }

        private void FixedUpdate()
        {
            float current = train.SpeedMetersPerSecond;
            float target = _targetSpeedKmh * KmhToMetersPerSecond;
            float rate = target > current
                ? settings.AccelerationMetersPerSecondSquared
                : settings.BrakingMetersPerSecondSquared;
            train.SpeedMetersPerSecond = Mathf.MoveTowards(current, target, rate * Time.fixedDeltaTime);
        }

        private void OnFaster(InputAction.CallbackContext _)
        {
            SetTarget(_targetSpeedKmh + settings.SpeedStepKmh);
        }

        private void OnSlower(InputAction.CallbackContext _)
        {
            SetTarget(_targetSpeedKmh - settings.SpeedStepKmh);
        }

        private void OnBrake(InputAction.CallbackContext _)
        {
            SetTarget(0f);
        }

        private void SetTarget(float kmh)
        {
            _targetSpeedKmh = Mathf.Clamp(kmh, 0f, settings.MaxSpeedKmh);
            GameLog.Info("Tren", $"Velocidad objetivo: {_targetSpeedKmh:0} km/h.", this);
        }

        private static void Subscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed += handler;
            reference.action.Enable();
        }

        private static void Unsubscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed -= handler;
        }
    }
}
