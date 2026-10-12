using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SurvivalEnTren.Core
{
    /// <summary>
    /// Prueba del M0: al darle Play escribe en la Console qué versión de Unity corre,
    /// si URP está activo y si el Input System responde (apretando Espacio).
    /// No es parte del juego; es solo una comprobación del arranque.
    /// </summary>
    public class BootstrapCheck : MonoBehaviour
    {
        private const string Category = "Bootstrap";

#if ENABLE_INPUT_SYSTEM
        private InputAction _testAction;
#endif

        private void Start()
        {
            GameLog.Info(Category, $"Escena de test iniciada. Unity {Application.unityVersion}.", this);

            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null)
                GameLog.Info(Category, $"Render pipeline activo: {pipeline.name} ({pipeline.GetType().Name}).", this);
            else
                GameLog.Warning(Category, "URP NO está activo (se usa el render antiguo). Ejecutar el menú 'Survival en tren > M0 > Configurar proyecto'.", this);

#if ENABLE_INPUT_SYSTEM
            GameLog.Info(Category, "Input System activo. Apretá la barra espaciadora para probarlo.", this);
#else
            GameLog.Warning(Category, "Input System NO está activo. Ejecutar el menú 'Survival en tren > M0 > Configurar proyecto'.", this);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            _testAction = new InputAction("TestEspacio", InputActionType.Button, "<Keyboard>/space");
            _testAction.performed += OnTestPerformed;
            _testAction.Enable();
        }

        private void OnDisable()
        {
            if (_testAction == null) return;
            _testAction.performed -= OnTestPerformed;
            _testAction.Disable();
            _testAction.Dispose();
            _testAction = null;
        }

        private void OnTestPerformed(InputAction.CallbackContext context)
        {
            GameLog.Info(Category, "Barra espaciadora detectada: el Input System funciona.", this);
        }
#endif
    }
}
