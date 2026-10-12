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

            _railPosition = _railPosition.Advanced(SpeedMetersPerSecond * Time.fixedDeltaTime);
            ComputePose(_railPosition, out Vector3 position, out Quaternion rotation);
            _body.MovePosition(position);
            _body.MoveRotation(rotation);
        }

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
