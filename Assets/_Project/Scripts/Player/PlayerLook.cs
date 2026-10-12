using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Player
{
    /// <summary>
    /// Mirar con el mouse: el giro horizontal rota todo el cuerpo y el vertical
    /// solo la cabeza (la cámara). Al darle Play el cursor queda oculto y fijo;
    /// Escape lo libera dentro del Editor.
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerMotorSettings settings;
        [SerializeField] private Transform head;
        [SerializeField] private InputActionReference look;

        private float _pitch;

        private void Start()
        {
            head.localPosition = Vector3.up * settings.EyeHeightMeters;
        }

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

            Vector2 delta = look.action.ReadValue<Vector2>() * settings.MouseSensitivityDegreesPerPixel;
            transform.Rotate(0f, delta.x, 0f, Space.Self);
            _pitch = Mathf.Clamp(_pitch - delta.y, -settings.MaxPitchDegrees, settings.MaxPitchDegrees);
            head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
