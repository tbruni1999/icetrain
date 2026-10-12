using UnityEngine;

namespace SurvivalEnTren.Spike
{
    /// <summary>
    /// Cámara de observación del spike M1: sigue al tren desde afuera.
    /// Solo existe hasta que el jugador en primera persona (paso 2) la reemplace.
    /// </summary>
    public class SpikeFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Posición de la cámara relativa al tren (x = costado, y = altura, z = adelante/atrás), en metros.")]
        [SerializeField] private Vector3 offset = new Vector3(14f, 7f, -22f);
        [Tooltip("Punto al que mira, relativo al tren, en metros.")]
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 2f, 0f);

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = target.TransformPoint(offset);
            transform.LookAt(target.TransformPoint(lookAtOffset), Vector3.up);
        }
    }
}
