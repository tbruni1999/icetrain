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

        private float _pitch;

        private void OnEnable()
        {
            look.action.Enable();
            LockCursor();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && enabled) LockCursor();
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            PlayerMotorSettings settings = motor.Settings;
            Vector2 delta = look.action.ReadValue<Vector2>() * settings.MouseSensitivityDegreesPerPixel;
            motor.AddYaw(delta.x);
            _pitch = Mathf.Clamp(_pitch - delta.y, -settings.MaxPitchDegrees, settings.MaxPitchDegrees);
        }

        private void LateUpdate()
        {
            Quaternion body = motor.RenderBodyRotation;
            Vector3 eye = motor.RenderFeetPosition + body * Vector3.up * motor.Settings.EyeHeightMeters;
            head.SetPositionAndRotation(eye, body * Quaternion.Euler(_pitch, 0f, 0f));
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
