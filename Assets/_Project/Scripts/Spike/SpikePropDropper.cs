using SurvivalEnTren.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Spike
{
    /// <summary>
    /// Tecla de prueba del spike M1 (G): suelta una caja delante de la cámara.
    /// No es el sistema de interacción (eso es del M3): solo sirve para probar
    /// cajas apoyadas en el tren en movimiento. Si el jugador viaja en el tren,
    /// la caja nace con la velocidad del tren en ese punto, como si la soltara de la mano.
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
            var body = box.GetComponent<Rigidbody>();
            if (motor.Platform != null)
                body.linearVelocity = motor.Platform.GetPointVelocity(position);
            metrics.Add(body);
        }
    }
}
