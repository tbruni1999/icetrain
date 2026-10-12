using SurvivalEnTren.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Spike
{
    /// <summary>
    /// Tecla de prueba del spike M1 (G): suelta una caja delante de la cámara.
    /// No es el sistema de interacción (eso es del M3): solo sirve para probar
    /// cajas apoyadas en el tren en movimiento. La caja nace con la velocidad del jugador,
    /// como si la soltara de la mano (si viaja en el tren o salta adentro, la del tren).
    /// </summary>
    public class SpikePropDropper : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Transform head;
        [SerializeField] private InputActionReference drop;
        [SerializeField] private GameObject boxPrefab;
        [SerializeField] private SpikePropMetrics metrics;
        [Tooltip("Distancia delante de los ojos donde aparece la caja, en metros.")]
        [SerializeField] private float distanceMeters = 1.1f;

        private void OnEnable()
        {
            drop.action.performed += OnDrop;
            drop.action.Enable();
        }

        private void OnDisable()
        {
            drop.action.performed -= OnDrop;
        }

        private void OnDrop(InputAction.CallbackContext _)
        {
            Vector3 position = head.position + head.forward * distanceMeters;
            var box = Instantiate(boxPrefab, position, Quaternion.LookRotation(Vector3.ProjectOnPlane(head.forward, Vector3.up)));
            // La caja sale con la velocidad del jugador, como al soltarla de la mano: si está parado
            // o saltando dentro del tren en marcha, es la velocidad del tren.
            var body = box.GetComponent<Rigidbody>();
            body.linearVelocity = motor.Velocity;
            metrics.Add(body);
        }
    }
}
