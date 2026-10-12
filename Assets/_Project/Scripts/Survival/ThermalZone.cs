using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Un lugar resguardado del viento pero sin calefacción (una estación abandonada, un refugio): el aire
    /// adentro está más templado que afuera, pero sigue frío (SurvivalSettings.unheatedShelterOffsetC).
    /// Es una caja en coordenadas locales de este objeto. ThermalSensor la consulta para decirle al
    /// jugador qué temperatura de aire lo rodea. Los espacios con calefacción propia (el tren) usan
    /// TrainInteriorClimate.
    /// </summary>
    public class ThermalZone : MonoBehaviour
    {
        [Tooltip("Centro y tamaño de la caja, en coordenadas locales (metros).")]
        [SerializeField] private Vector3 center;
        [SerializeField] private Vector3 size = Vector3.one;

        /// <summary>¿Este punto del mundo está dentro de la zona?</summary>
        public bool Contains(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint) - center;
            Vector3 half = size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.4f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
