using SurvivalEnTren.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// El pupitre de la cabina. Mirándolo y apretando E se toman los controles: el jugador deja de
    /// caminar (puede seguir mirando) y las teclas manejan el tren:
    /// W / S = palanca de potencia y freno, R / F = reversor adelante / atrás. E de nuevo suelta los
    /// controles. Usa el mapa de acciones "Train" (Anexo A.1: un mapa por contexto).
    /// </summary>
    public class TrainCab : MonoBehaviour, IInteractable
    {
        [SerializeField] private TrainControls controls;
        [SerializeField] private InputActionReference powerUp;
        [SerializeField] private InputActionReference powerDown;
        [SerializeField] private InputActionReference reverserForward;
        [SerializeField] private InputActionReference reverserBack;

        private PlayerInteractor _driver;

        public bool IsDriving => _driver != null;

        public string GetPrompt(PlayerInteractor interactor) =>
            _driver == null ? "tomar los controles" : interactor == _driver ? "soltar los controles" : null;

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            if (_driver == null) TakeControls(interactor);
            else if (interactor == _driver) LeaveControls();
        }

        private void OnDisable()
        {
            if (_driver != null) LeaveControls();
        }

        private void TakeControls(PlayerInteractor interactor)
        {
            _driver = interactor;
            _driver.Engage(this);
            _driver.Motor.MovementLocked = true;
            Bind(powerUp, OnPowerUp);
            Bind(powerDown, OnPowerDown);
            Bind(reverserForward, OnReverserForward);
            Bind(reverserBack, OnReverserBack);
        }

        private void LeaveControls()
        {
            Unbind(powerUp, OnPowerUp);
            Unbind(powerDown, OnPowerDown);
            Unbind(reverserForward, OnReverserForward);
            Unbind(reverserBack, OnReverserBack);
            _driver.Motor.MovementLocked = false;
            _driver.Release(this);
            _driver = null;
        }

        private void OnPowerUp(InputAction.CallbackContext _) => controls.MovePowerBrake(+1);
        private void OnPowerDown(InputAction.CallbackContext _) => controls.MovePowerBrake(-1);
        private void OnReverserForward(InputAction.CallbackContext _) => controls.TryMoveReverser(+1);
        private void OnReverserBack(InputAction.CallbackContext _) => controls.TryMoveReverser(-1);

        private static void Bind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            reference.action.performed += handler;
            reference.action.Enable();
        }

        private static void Unbind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            reference.action.performed -= handler;
            reference.action.Disable();
        }
    }
}
