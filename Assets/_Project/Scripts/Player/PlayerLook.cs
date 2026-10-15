using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Player
{
    /// <summary>
    /// Mirar con el mouse y ubicar la cámara. El giro horizontal gira el cuerpo
    /// (PlayerMotor) y el vertical solo la cabeza. En LateUpdate la cámara se coloca
    /// en la posición suavizada que da PlayerMotor, así acompaña al tren sin temblar.
    /// Al darle Play el cursor queda oculto y fijo; Escape lo libera dentro del Editor.
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Transform head;
        [SerializeField] private InputActionReference look;

        // Subida mínima en un cuadro para considerarla un escalón (y no una rampa), y máxima
        // para no confundir un teletransporte con un escalón. En metros.
        private const float StepRiseThresholdMeters = 0.04f;
        private const float MaxStepRiseMeters = 0.7f;

        private float _pitch;
        private bool _cursorFree;
        private float _stepSmoothing;

        /// <summary>Cuánto mira hacia abajo (positivo) o arriba (negativo), en grados.</summary>
        public float Pitch => _pitch;

        /// <summary>Cuántas veces la sensibilidad de base (las opciones del jugador, P3).</summary>
        public float SensitivityScale { get; set; } = 1f;

        /// <summary>Pone la inclinación de la mirada (al cargar una partida).</summary>
        public void SetPitch(float degrees) => _pitch = degrees;

        /// <summary>
        /// Libera el cursor para usar un panel con el mouse (la mochila): mientras tanto no se mira con el mouse.
        /// Al volver a false, el cursor se oculta y se fija de nuevo.
        /// </summary>
        public bool CursorFree
        {
            get => _cursorFree;
            set
            {
                _cursorFree = value;
                if (value) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                else LockCursor();
            }
        }
        private float _lastFeetHeight;
        private bool _hasLastFeet;
        private bool _wasGrounded;

        private void OnEnable()
        {
            look.action.Enable();
            LockCursor();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && enabled && !_cursorFree) LockCursor();
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            PlayerMotorSettings settings = motor.Settings;
            Vector2 delta = look.action.ReadValue<Vector2>() * settings.MouseSensitivityDegreesPerPixel * SensitivityScale;
            motor.AddYaw(delta.x);
            _pitch = Mathf.Clamp(_pitch - delta.y, -settings.MaxPitchDegrees, settings.MaxPitchDegrees);
        }

        private void LateUpdate()
        {
            Quaternion body = motor.RenderBodyRotation;
            Vector3 feet = motor.RenderFeetPosition;

            // Al subir o bajar un escalón (o una caja) el cuerpo cambia de altura de golpe; la cámara
            // lo alcanza en unas décimas. Solo caminando apoyado: saltos y caídas no se suavizan.
            float step = feet.y - _lastFeetHeight;
            bool grounded = motor.IsGrounded;
            if (_hasLastFeet && grounded && _wasGrounded &&
                Mathf.Abs(step) > StepRiseThresholdMeters && Mathf.Abs(step) < MaxStepRiseMeters)
                _stepSmoothing -= step;
            _lastFeetHeight = feet.y;
            _wasGrounded = grounded;
            _hasLastFeet = true;
            _stepSmoothing *= Mathf.Exp(-motor.Settings.StepSmoothingSpeed * Time.deltaTime);

            Vector3 eye = feet + Vector3.up * (motor.Settings.EyeHeightMeters + _stepSmoothing);
            head.SetPositionAndRotation(eye, body * Quaternion.Euler(_pitch, 0f, 0f));
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
