using SurvivalEnTren.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Llevar un objeto en la mano (M3): lo sostiene delante de la cámara empujándolo con velocidad
    /// (no teletransportándolo), así choca con paredes y muebles en vez de atravesarlos.
    /// - Corre en el tic de física justo después del jugador (orden -90): usa la pose ya movida del
    ///   jugador y su velocidad real, que incluye viajar con el tren. Por eso el objeto acompaña al
    ///   tren en marcha sin quedarse atrás.
    /// - Al soltarlo o lanzarlo sale con la velocidad del jugador (+ el lanzamiento).
    /// - Si queda trabado lejos de la mano (contra una pared) durante un rato, se suelta solo.
    /// - Mientras se lleva algo, el jugador camina más lento según el peso (CarrySettings).
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class PlayerCarrier : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private CarrySettings settings;
        [SerializeField] private InputActionReference throwAction;

        private Carryable _held;
        private bool _heldUsedGravity;
        private Collider[] _heldColliders;
        private Quaternion _heldLocalRotation;
        private float _separatedSeconds;

        public Carryable Holding => _held;

        private void OnEnable()
        {
            throwAction.action.performed += OnThrow;
            throwAction.action.Enable();
        }

        private void OnDisable()
        {
            throwAction.action.performed -= OnThrow;
            if (_held != null) Drop(throwIt: false);
        }

        public bool CanLift(Carryable item) => item.Body.mass <= settings.MaxCarryMassKg;

        public void Pick(Carryable item)
        {
            _held = item;
            _separatedSeconds = 0f;
            Rigidbody body = item.Body;
            _heldUsedGravity = body.useGravity;
            body.useGravity = false;
            _heldColliders = item.GetComponentsInChildren<Collider>();
            SetIgnorePlayer(true);
            // Conserva cómo estaba girado respecto del jugador, para que no "salte" al tomarlo.
            _heldLocalRotation = Quaternion.Inverse(BodyYaw()) * body.rotation;
            interactor.Engage(item);
            motor.SpeedMultiplier = settings.WalkSpeedFactor(body.mass);
        }

        public void Drop(bool throwIt)
        {
            if (_held == null) return;
            Rigidbody body = _held.Body;
            body.useGravity = _heldUsedGravity;
            Vector3 velocity = motor.Velocity;
            if (throwIt) velocity += LookDirection() * settings.ThrowSpeed(body.mass);
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;
            SetIgnorePlayer(false);
            interactor.Release(_held);
            motor.SpeedMultiplier = 1f;
            _held = null;
        }

        private void OnThrow(InputAction.CallbackContext _)
        {
            if (_held != null) Drop(throwIt: true);
        }

        private void FixedUpdate()
        {
            if (_held == null) return;
            Rigidbody body = _held.Body;
            float dt = Time.fixedDeltaTime;
            motor.SpeedMultiplier = settings.WalkSpeedFactor(body.mass); // el peso puede cambiar (un bidón que se llena)

            Vector3 eye = motor.transform.position + Vector3.up * motor.Settings.EyeHeightMeters;
            Vector3 target = eye + LookDirection() * settings.HoldDistanceMeters;
            Vector3 toTarget = target - body.position;

            // Trabado: lejos de la mano durante un rato (al tomarlo de lejos tarda un instante en llegar).
            _separatedSeconds = toTarget.magnitude > settings.DropIfSeparatedMeters ? _separatedSeconds + dt : 0f;
            if (_separatedSeconds > settings.StuckSeconds)
            {
                Drop(throwIt: false);
                return;
            }

            // Llegar a la mano en este tic, sin superar una velocidad máxima respecto del jugador.
            Vector3 relative = Vector3.ClampMagnitude(toTarget / dt - motor.Velocity, settings.MaxFollowSpeedMetersPerSecond);
            body.linearVelocity = motor.Velocity + relative;

            // Girar suavemente hacia la orientación con la que se tomó (relativa a hacia dónde mira el cuerpo).
            Quaternion delta = BodyYaw() * _heldLocalRotation * Quaternion.Inverse(body.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            body.angularVelocity = Mathf.Abs(angle) < 0.01f ? Vector3.zero : axis * (angle * Mathf.Deg2Rad / dt * 0.5f);
        }

        private Quaternion BodyYaw() => Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f);

        private Vector3 LookDirection() => BodyYaw() * Quaternion.Euler(look.Pitch, 0f, 0f) * Vector3.forward;

        private void SetIgnorePlayer(bool ignore)
        {
            var playerCollider = motor.GetComponent<CharacterController>();
            foreach (Collider c in _heldColliders)
                if (c != null) Physics.IgnoreCollision(c, playerCollider, ignore);
        }
    }
}
