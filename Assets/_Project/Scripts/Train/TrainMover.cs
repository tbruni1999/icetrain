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
        [Tooltip("Freno de seguridad: el tren se detiene en seco si un bogie quedaría a menos de esta distancia " +
                 "de un final de vía, en metros. Normalmente lo frena antes el resorte del tope (TrainDynamics).")]
        [SerializeField] private float hardStopBogieMarginMeters = 0.3f;

        private Rigidbody _body;
        private RailPosition _railPosition;

        /// <summary>Velocidad sobre la vía en metros por segundo (negativa = marcha atrás).</summary>
        public float SpeedMetersPerSecond { get; set; }

        /// <summary>Del centro a cada punta del tren, en metros.</summary>
        public float HalfLengthMeters => halfLengthMeters;

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
                // Freno de seguridad: un bogie llegaría al final de la vía. Normalmente el resorte del
                // tope ya lo frenó antes; esto solo pasa en un choque muy fuerte.
                SpeedMetersPerSecond = 0f;
                ReachedEndOfTrack?.Invoke();
                return;
            }

            _railPosition = _railPosition.Advanced(step);
            ComputePose(_railPosition, out Vector3 position, out Quaternion rotation);
            _body.MovePosition(position);
            _body.MoveRotation(rotation);
        }

        // ¿El bogie de adelante (o el de atrás si retrocede) pasaría un final de vía? Es el freno de
        // seguridad: los bogies no pueden salirse de la vía.
        private bool WouldPassEndOfTrack(float step)
        {
            float sign = Mathf.Sign(step);
            RailPosition moved = _railPosition.Advanced(step, out bool centerHitEnd);
            if (centerHitEnd) return true;
            moved.Advanced(sign * (bogieSpacingMeters * 0.5f + hardStopBogieMarginMeters), out bool bogieHitEnd);
            return bogieHitEnd;
        }

        /// <summary>
        /// Distancia entre la punta del tren (la de adelante si direction &gt; 0, la de atrás si no) y un
        /// final de vía, en metros. Negativa si la punta ya pasó el final (está comprimiendo el tope).
        /// Infinita si no hay un final cerca.
        /// </summary>
        public float GapToTrackEnd(int direction)
        {
            const float lookAheadMeters = 30f;
            float toEnd = _railPosition.DistanceToEnd(direction, halfLengthMeters + lookAheadMeters);
            return toEnd - halfLengthMeters;
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
