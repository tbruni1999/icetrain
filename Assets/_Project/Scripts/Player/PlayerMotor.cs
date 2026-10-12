using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Player
{
    /// <summary>
    /// Mueve al jugador: caminar con WASD, gravedad y salto.
    /// Usa un CharacterController (cuerpo de cápsula que no rebota como un objeto
    /// de física). Seguir al tren en movimiento se agrega en el paso 3 del M1.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerMotorSettings settings;
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference jump;

        private CharacterController _body;
        private float _verticalSpeed;
        private bool _jumpRequested;

        public bool IsGrounded => _body != null && _body.isGrounded;

        private void Awake()
        {
            _body = GetComponent<CharacterController>();
            _body.height = settings.BodyHeightMeters;
            _body.radius = settings.BodyRadiusMeters;
            _body.center = Vector3.up * (settings.BodyHeightMeters * 0.5f);
            _body.stepOffset = settings.StepOffsetMeters;
            _body.slopeLimit = settings.SlopeLimitDegrees;
        }

        private void OnEnable()
        {
            move.action.Enable();
            jump.action.performed += OnJump;
            jump.action.Enable();
        }

        private void OnDisable()
        {
            jump.action.performed -= OnJump;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            Vector2 input = move.action.ReadValue<Vector2>();
            Vector3 walk = transform.right * input.x + transform.forward * input.y;
            walk = Vector3.ClampMagnitude(walk, 1f) * settings.WalkSpeedMetersPerSecond;

            if (_body.isGrounded)
            {
                // Un pequeño empuje hacia abajo mantiene los pies pegados al piso al bajar escalones.
                if (_verticalSpeed < 0f) _verticalSpeed = -2f;
                if (_jumpRequested)
                    _verticalSpeed = Mathf.Sqrt(2f * settings.GravityMetersPerSecondSquared * settings.JumpHeightMeters);
            }
            _jumpRequested = false;
            _verticalSpeed -= settings.GravityMetersPerSecondSquared * dt;

            _body.Move((walk + Vector3.up * _verticalSpeed) * dt);
        }

        private void OnJump(InputAction.CallbackContext _)
        {
            _jumpRequested = true;
        }
    }
}
