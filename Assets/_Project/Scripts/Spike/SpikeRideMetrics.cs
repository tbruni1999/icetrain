using SurvivalEnTren.Player;
using UnityEngine;

namespace SurvivalEnTren.Spike
{
    /// <summary>
    /// Mide qué tan estable se ve el interior del tren desde la cámara (Anexo C.2):
    /// - Temblor: cuánto "salta" la cámara de un cuadro a otro respecto del tren,
    ///   sin contar el movimiento suave (segunda diferencia de la posición), en milímetros.
    ///   Se informa el máximo de los últimos 2 segundos.
    /// - Deriva: cuánto se corre la cámara respecto del tren estando quieto, en centímetros.
    /// Corre después de que PlayerLook ubica la cámara (orden de ejecución 200).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class SpikeRideMetrics : MonoBehaviour
    {
        private const float JitterWindowSeconds = 2f;

        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Transform head;

        private Rigidbody _platform;
        private Vector3 _last;
        private Vector3 _beforeLast;
        private int _samples;

        private float _windowMax;
        private float _windowTimer;

        private bool _anchored;
        private Vector3 _anchor;
        private float _stillSeconds;

        public bool OnPlatform => _platform != null;
        public float JitterMillimeters { get; private set; }
        public float DriftCentimeters { get; private set; }
        public float StillSeconds => _stillSeconds;

        private void LateUpdate()
        {
            Rigidbody platform = motor.Platform;
            if (platform != _platform)
            {
                _platform = platform;
                _samples = 0;
                _anchored = false;
                _windowMax = 0f;
                JitterMillimeters = 0f;
                DriftCentimeters = 0f;
            }
            if (_platform == null) return;

            Vector3 local = _platform.transform.InverseTransformPoint(head.position);

            // Temblor: la segunda diferencia es ~0 si el movimiento es suave.
            if (_samples >= 2)
            {
                float jitter = (local - 2f * _last + _beforeLast).magnitude * 1000f;
                _windowMax = Mathf.Max(_windowMax, jitter);
            }
            _beforeLast = _last;
            _last = local;
            _samples++;

            _windowTimer += Time.unscaledDeltaTime;
            if (_windowTimer >= JitterWindowSeconds)
            {
                JitterMillimeters = _windowMax;
                _windowMax = 0f;
                _windowTimer = 0f;
            }

            // Deriva: solo cuenta mientras no se toca el teclado y se está apoyado.
            if (motor.HasMoveInput || !motor.IsGrounded)
            {
                _anchored = false;
                _stillSeconds = 0f;
                return;
            }
            if (!_anchored)
            {
                _anchored = true;
                _anchor = local;
                _stillSeconds = 0f;
            }
            _stillSeconds += Time.deltaTime;
            DriftCentimeters = (local - _anchor).magnitude * 100f;
        }
    }
}
