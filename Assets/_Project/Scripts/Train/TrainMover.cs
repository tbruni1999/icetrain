using SurvivalEnTren.Rail;
using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Mueve la locomotora sobre la vía a la velocidad que le indiquen.
    /// La posición y el giro salen de dos "bogies" (los carros de ruedas de adelante
    /// y de atrás) apoyados sobre la vía, así en las curvas el tren gira como uno real.
    /// Se mueve como Rigidbody cinemático en FixedUpdate (con interpolación) para
    /// que el motor de físicas sepa que es una plataforma en movimiento.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TrainMover : MonoBehaviour
    {
        [SerializeField] private RailSegment startSegment;
        [Tooltip("Metros desde el inicio del tramo donde arranca el centro del tren.")]
        [SerializeField] private float startDistanceMeters = 20f;
        [Tooltip("Distancia entre el bogie de adelante y el de atrás, en metros (geometría del modelo).")]
        [SerializeField] private float bogieSpacingMeters = 13f;
        [Tooltip("Medio largo del tren, del centro a cada punta, en metros (geometría del modelo).")]
        [SerializeField] private float halfLengthMeters = 9.5f;
        [Tooltip("Distancia que el tren deja libre antes de un final de vía (el tope), en metros.")]
        [SerializeField] private float endOfTrackMarginMeters = 1.5f;

        private Rigidbody _body;
        private RailPosition _railPosition;

        /// <summary>Velocidad sobre la vía en metros por segundo (negativa = marcha atrás).</summary>
        public float SpeedMetersPerSecond { get; set; }

        /// <summary>Dónde está el centro del tren sobre la vía.</summary>
        public RailPosition RailPosition => _railPosition;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;

            _railPosition = new RailPosition(startSegment, startDistanceMeters);
            SnapToRail();
        }

        private void FixedUpdate()
        {
            if (!_railPosition.IsValid) return;

            float step = SpeedMetersPerSecond * Time.fixedDeltaTime;
            if (step != 0f && WouldPassEndOfTrack(step))
            {
                // Llegó a un final de vía (un tope): se detiene en seco. Qué pasa en un choque
                // fuerte (daños, sacudón) se define más adelante.
                SpeedMetersPerSecond = 0f;
                ReachedEndOfTrack?.Invoke();
                return;
            }

            _railPosition = _railPosition.Advanced(step);
            ComputePose(_railPosition, out Vector3 position, out Quaternion rotation);
            _body.MovePosition(position);
            _body.MoveRotation(rotation);
        }

        // ¿El extremo del tren (el de adelante si avanza, el de atrás si retrocede) pasaría un final de vía?
        private bool WouldPassEndOfTrack(float step)
        {
            float sign = Mathf.Sign(step);
            RailPosition moved = _railPosition.Advanced(step, out bool centerHitEnd);
            if (centerHitEnd) return true;
            moved.Advanced(sign * (halfLengthMeters + endOfTrackMarginMeters), out bool endHitEnd);
            return endHitEnd;
        }

        /// <summary>Se dispara cuando el tren se detiene contra un final de vía.</summary>
        public event System.Action ReachedEndOfTrack;

        /// <summary>Coloca el tren de golpe en su posición de vía (al iniciar, o desde el editor).</summary>
        public void SnapToRail()
        {
            var start = _railPosition.IsValid ? _railPosition : new RailPosition(startSegment, startDistanceMeters);
            if (!start.IsValid) return;

            ComputePose(start, out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
            if (_body != null)
            {
                _body.position = position;
                _body.rotation = rotation;
            }
        }

        private void ComputePose(RailPosition center, out Vector3 position, out Quaternion rotation)
        {
            Vector3 front = center.Advanced(bogieSpacingMeters * 0.5f).WorldPosition;
            Vector3 rear = center.Advanced(-bogieSpacingMeters * 0.5f).WorldPosition;
            position = (front + rear) * 0.5f;
            Vector3 forward = front - rear;
            rotation = forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(forward, Vector3.up) : transform.rotation;
        }
    }
}
