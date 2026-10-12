using SurvivalEnTren.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Player
{
    /// <summary>
    /// Mueve al jugador: caminar con WASD (correr con Shift), gravedad, salto, y viajar
    /// parado sobre plataformas que se mueven (el tren).
    ///
    /// Cómo acompaña al tren (estrategia A del spike, Anexo C):
    /// - Todo se calcula en FixedUpdate, el mismo "tic" de física en el que se mueve el tren,
    ///   y antes que el tren (orden de ejecución -100), así ve la pose del tren ya aplicada.
    /// - Mientras pisa una plataforma, se guarda la posición del jugador relativa a ella;
    ///   en cada tic primero se lo lleva con la plataforma y después se aplica su caminata.
    ///   No se usa parenting (el documento pide no confiar en eso sin pruebas).
    /// - En el aire no lo lleva: conserva la velocidad que traía (inercia).
    /// - Para dibujar, la cámara se suaviza entre los dos últimos tics, en coordenadas de la
    ///   plataforma (PlayerLook), igual que Unity suaviza el dibujo del tren.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour, IOriginShiftListener
    {
        // Qué tan "piso" tiene que ser una superficie para pararse (1 = horizontal).
        private const float GroundNormalMinY = 0.6f;

        [SerializeField] private PlayerMotorSettings settings;
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference jump;
        [SerializeField] private InputActionReference run;

        private CharacterController _body;
        private float _verticalSpeed;
        private bool _jumpRequested;

        // Hacia dónde mira el cuerpo, en grados, relativo a la referencia actual.
        private float _yaw;

        // Referencia actual: la plataforma que se pisa, o null para el mundo fijo.
        private Rigidbody _platform;
        // Posición de los pies relativa a la referencia, en los dos últimos tics.
        private Vector3 _previousLocal;
        private Vector3 _currentLocal;
        private Vector3 _tickStartPosition;
        private bool _referenceChangedThisTick;

        // Velocidad que trae al estar en el aire (inercia), en el mundo, en m/s.
        private Vector3 _inheritedVelocity;
        private Vector3 _lastCarryVelocity;

        // Lo que tocó como piso durante el último Move.
        private bool _groundHit;
        private Rigidbody _groundBody;

        /// <summary>La plataforma sobre la que viaja, o null si está en el mundo fijo o en el aire.</summary>
        public Rigidbody Platform => _platform;
        public bool IsGrounded => _body != null && _body.isGrounded;
        public bool HasMoveInput { get; private set; }
        public PlayerMotorSettings Settings => settings;

        private void Awake()
        {
            _body = GetComponent<CharacterController>();
            _body.height = settings.BodyHeightMeters;
            _body.radius = settings.BodyRadiusMeters;
            _body.center = Vector3.up * (settings.BodyHeightMeters * 0.5f);
            _body.stepOffset = settings.StepOffsetMeters;
            _body.slopeLimit = settings.SlopeLimitDegrees;

            _yaw = transform.eulerAngles.y;
            _previousLocal = _currentLocal = transform.position;
        }

        private void OnEnable()
        {
            move.action.Enable();
            run.action.Enable();
            jump.action.performed += OnJump;
            jump.action.Enable();
        }

        private void OnDisable()
        {
            jump.action.performed -= OnJump;
        }

        /// <summary>Gira el cuerpo (lo llama PlayerLook con el mouse).</summary>
        public void AddYaw(float degrees)
        {
            _yaw = Mathf.Repeat(_yaw + degrees, 360f);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _referenceChangedThisTick = false;

            // 1) Viajar con la plataforma: aplicar lo que se movió desde el tic anterior.
            if (_platform != null)
            {
                Vector3 before = transform.position;
                Vector3 carried = _platform.position + _platform.rotation * _currentLocal;
                transform.position = carried;
                Physics.SyncTransforms();
                _lastCarryVelocity = (carried - before) / dt;
            }
            // Dónde terminó de dibujarse el tic anterior: punto de partida del suavizado.
            _tickStartPosition = transform.position;
            transform.rotation = ReferenceRotation(usePhysicsPose: true) * Quaternion.Euler(0f, _yaw, 0f);

            // 2) Caminar, correr, saltar y caer.
            Vector2 input = move.action.ReadValue<Vector2>();
            HasMoveInput = input.sqrMagnitude > 0.01f;
            float speed = run.action.IsPressed() ? settings.RunSpeedMetersPerSecond : settings.WalkSpeedMetersPerSecond;
            Vector3 walk = Vector3.ClampMagnitude(transform.right * input.x + transform.forward * input.y, 1f) * speed;

            if (_body.isGrounded)
            {
                // Un pequeño empuje hacia abajo mantiene los pies pegados al piso al bajar escalones.
                if (_verticalSpeed < 0f) _verticalSpeed = -2f;
                if (_jumpRequested)
                    _verticalSpeed = Mathf.Sqrt(2f * settings.GravityMetersPerSecondSquared * settings.JumpHeightMeters);
            }
            _jumpRequested = false;
            _verticalSpeed -= settings.GravityMetersPerSecondSquared * dt;

            Vector3 velocity = walk + Vector3.up * _verticalSpeed;
            if (_platform == null) velocity += _inheritedVelocity;

            _groundHit = false;
            _groundBody = null;
            _body.Move(velocity * dt);

            // 3) Decidir la referencia para el próximo tic.
            if (_body.isGrounded && _groundHit)
            {
                _inheritedVelocity = Vector3.zero;
                SetReference(_groundBody);
            }
            else if (!_body.isGrounded && _platform != null)
            {
                // Despegó de la plataforma (salto o borde): sigue con la velocidad que traía.
                _inheritedVelocity = Vector3.ProjectOnPlane(_lastCarryVelocity, Vector3.up);
                SetReference(null);
            }

            // 4) Guardar la posición relativa a la referencia, para el próximo tic y para dibujar.
            //    (Si la referencia cambió en este tic, SetReference ya dejó ambas en la nueva.)
            if (!_referenceChangedThisTick)
            {
                _previousLocal = _currentLocal;
                _currentLocal = ToReferenceLocal(transform.position);
            }
        }

        /// <summary>El mundo se re-centró: corregir las posiciones guardadas en coordenadas del mundo.</summary>
        public void OnOriginShifted(Vector3 delta)
        {
            _tickStartPosition += delta;
            if (_platform != null) return; // relativas a la plataforma: no cambian
            _previousLocal += delta;
            _currentLocal += delta;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < GroundNormalMinY) return;
            _groundHit = true;

            // Cualquier cuerpo cinemático (movido por código, como el tren) cuenta como plataforma.
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body != null && body.isKinematic) _groundBody = body;
        }

        private void SetReference(Rigidbody platform)
        {
            if (platform == _platform) return;

            Quaternion worldFacing = ReferenceRotation(usePhysicsPose: true) * Quaternion.Euler(0f, _yaw, 0f);
            _platform = platform;
            Quaternion localFacing = Quaternion.Inverse(ReferenceRotation(usePhysicsPose: true)) * worldFacing;
            _yaw = localFacing.eulerAngles.y;

            // Pasar las dos posiciones del suavizado a la nueva referencia, así el cuadro del
            // cambio (subir o bajar del tren) no da un saltito.
            _previousLocal = ToReferenceLocal(_tickStartPosition);
            _currentLocal = ToReferenceLocal(transform.position);
            _referenceChangedThisTick = true;
        }

        private Vector3 ToReferenceLocal(Vector3 world)
        {
            if (_platform == null) return world;
            return Quaternion.Inverse(_platform.rotation) * (world - _platform.position);
        }

        private Quaternion ReferenceRotation(bool usePhysicsPose)
        {
            if (_platform == null) return Quaternion.identity;
            return usePhysicsPose ? _platform.rotation : _platform.transform.rotation;
        }

        /// <summary>
        /// Posición de los pies para dibujar este cuadro: suavizada entre los dos últimos tics,
        /// relativa a la plataforma tal como se está dibujando (así no tiembla contra el tren).
        /// </summary>
        public Vector3 RenderFeetPosition
        {
            get
            {
                float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
                Vector3 local = Vector3.Lerp(_previousLocal, _currentLocal, alpha);
                return _platform != null ? _platform.transform.TransformPoint(local) : local;
            }
        }

        /// <summary>Hacia dónde mira el cuerpo, para dibujar este cuadro.</summary>
        public Quaternion RenderBodyRotation => ReferenceRotation(usePhysicsPose: false) * Quaternion.Euler(0f, _yaw, 0f);

        private void OnJump(InputAction.CallbackContext _)
        {
            _jumpRequested = true;
        }
    }
}
