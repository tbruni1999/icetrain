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
    /// - Plataforma es un cuerpo cinemático (el tren). Parado sobre un objeto suelto (una caja),
    ///   la plataforma es lo que sostiene a ese objeto (el tren), no el objeto.
    /// - De la plataforma solo se copia el giro horizontal: el jugador siempre queda derecho.
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
        // Hasta qué distancia debajo de un objeto suelto se busca lo que lo sostiene, en metros.
        private const float SupportSearchDistanceMeters = 3f;

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
        /// <summary>Velocidad real del jugador en el mundo en el último tic (incluye viajar con el tren), en m/s.</summary>
        public Vector3 Velocity { get; private set; }
        /// <summary>A qué velocidad camina el jugador por sus propios medios (sin contar el tren ni la caída), en m/s.</summary>
        public Vector3 WalkVelocity { get; private set; }
        /// <summary>Si es true, el jugador no camina ni salta (por ejemplo, mientras maneja el tren). Sigue viajando con lo que pisa.</summary>
        public bool MovementLocked { get; set; }
        /// <summary>Multiplica la velocidad de caminar y correr (por ejemplo, al llevar algo pesado). 1 = normal.</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>Multiplica la velocidad por cansancio (lo pone PlayerVitals). 1 = normal.</summary>
        public float FatigueMultiplier { get; set; } = 1f;
        /// <summary>Si es false, Shift no corre (sin energía).</summary>
        public bool CanRun { get; set; } = true;
        /// <summary>True mientras corre de verdad (Shift + moviéndose + apoyado).</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Se dispara al tocar el piso después de estar en el aire, con la velocidad de caída (m/s, positiva).</summary>
        public event System.Action<float> Landed;
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

        /// <summary>Hacia dónde mira el cuerpo, en grados (relativo a lo que pisa: el tren o el mundo).</summary>
        public float Yaw => _yaw;

        /// <summary>Pone hacia dónde mira el cuerpo (al cargar una partida).</summary>
        public void SetYaw(float degrees) => _yaw = Mathf.Repeat(degrees, 360f);

        /// <summary>Gira el cuerpo (lo llama PlayerLook con el mouse).</summary>
        public void AddYaw(float degrees)
        {
            _yaw = Mathf.Repeat(_yaw + degrees, 360f);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _referenceChangedThisTick = false;
            Vector3 positionAtTickStart = transform.position;

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
            Vector2 input = MovementLocked ? Vector2.zero : move.action.ReadValue<Vector2>();
            if (MovementLocked) _jumpRequested = false;
            HasMoveInput = input.sqrMagnitude > 0.01f;
            bool running = CanRun && run.action.IsPressed() && HasMoveInput;
            IsRunning = running && _body.isGrounded;
            float speed = (running ? settings.RunSpeedMetersPerSecond : settings.WalkSpeedMetersPerSecond) * SpeedMultiplier * FatigueMultiplier;
            Vector3 walk = Vector3.ClampMagnitude(transform.right * input.x + transform.forward * input.y, 1f) * speed;
            WalkVelocity = walk;

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
            bool wasGrounded = _body.isGrounded;
            float fallSpeed = -_verticalSpeed;
            _body.Move(velocity * dt);
            if (!wasGrounded && _body.isGrounded && fallSpeed > 0f) Landed?.Invoke(fallSpeed);

            // Salió caminando de un borde (sin saltar). Si el piso está a menos de un escalón (bajar de
            // una caja, de un escalón), baja pegado a él como en una escalera. Si es una caída real,
            // arranca desde quieto: el pequeño empuje hacia abajo no debe convertirse en un tirón.
            if (wasGrounded && !_body.isGrounded && _verticalSpeed < 0f && !SnapDownToGround())
                _verticalSpeed = 0f;

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

            Velocity = (transform.position - positionAtTickStart) / dt;

            // 4) Guardar la posición relativa a la referencia, para el próximo tic y para dibujar.
            //    (Si la referencia cambió en este tic, SetReference ya dejó ambas en la nueva.)
            if (!_referenceChangedThisTick)
            {
                _previousLocal = _currentLocal;
                _currentLocal = ToReferenceLocal(transform.position);
            }
        }

        /// <summary>
        /// Pone al jugador en un punto, viajando con esa plataforma (o en el mundo fijo si es null).
        /// Para despertar en la cama del tren, pruebas, etc.
        /// </summary>
        public void TeleportTo(Vector3 worldPosition, Rigidbody platform)
        {
            _body.enabled = false;
            transform.position = worldPosition;
            _body.enabled = true;
            Physics.SyncTransforms();
            _platform = platform;
            _inheritedVelocity = Vector3.zero;
            _verticalSpeed = 0f;
            _tickStartPosition = worldPosition;
            _currentLocal = _previousLocal = ToReferenceLocal(worldPosition);
        }

        // Busca piso a menos de un escalón debajo del cuerpo y, si lo hay, baja hasta él.
        private bool SnapDownToGround()
        {
            float radius = _body.radius * 0.95f;
            Vector3 center = transform.position + _body.center;
            Vector3 offset = Vector3.up * (_body.height * 0.5f - _body.radius);
            float reach = settings.StepOffsetMeters + _body.skinWidth;

            if (!Physics.CapsuleCast(center + offset, center - offset, radius, Vector3.down, out RaycastHit hit,
                    reach, ~0, QueryTriggerInteraction.Ignore))
                return false;
            if (hit.normal.y < GroundNormalMinY) return false;

            _groundHit = false;
            _groundBody = null;
            _body.Move(Vector3.down * (hit.distance + _body.skinWidth));
            return _body.isGrounded;
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

            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null) return;

            // Un cuerpo cinemático (movido por código, como el tren) es plataforma.
            if (body.isKinematic)
            {
                _groundBody = body;
                return;
            }

            // Un objeto suelto (una caja) no es plataforma: puede inclinarse o moverse cuando el
            // jugador lo empuja. Se viaja con lo que lo sostiene (el tren); sobre la nieve, con nada.
            Rigidbody support = FindSupportBelow(body);
            if (support != null) _groundBody = support;
        }

        private static readonly RaycastHit[] SupportHits = new RaycastHit[8];

        // Busca debajo de un objeto suelto el primer cuerpo cinemático que lo sostiene.
        private Rigidbody FindSupportBelow(Rigidbody looseBody)
        {
            int count = Physics.RaycastNonAlloc(looseBody.worldCenterOfMass, Vector3.down, SupportHits,
                SupportSearchDistanceMeters, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(SupportHits, 0, count, RaycastHitDistanceComparer.Instance);

            for (int i = 0; i < count; i++)
            {
                Rigidbody below = SupportHits[i].collider.attachedRigidbody;
                if (below == looseBody || SupportHits[i].collider.transform == transform) continue;
                if (below == null) return null;          // apoyado en el suelo fijo
                if (below.isKinematic) return below;      // apoyado en el tren
                // otro objeto suelto (cajas apiladas): seguir buscando más abajo
            }
            return null;
        }

        private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
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

        // Solo el giro horizontal de la plataforma: el cuerpo y la cabeza del jugador siempre quedan
        // derechos aunque lo que pisa se incline (la posición sí sigue la pose completa).
        private Quaternion ReferenceRotation(bool usePhysicsPose)
        {
            if (_platform == null) return Quaternion.identity;
            Quaternion rotation = usePhysicsPose ? _platform.rotation : _platform.transform.rotation;
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
            return forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
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
