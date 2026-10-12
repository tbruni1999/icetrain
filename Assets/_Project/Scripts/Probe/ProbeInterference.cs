using UnityEngine;

namespace SurvivalEnTren.Probe
{
    /// <summary>
    /// Zona que interfiere la sonda portátil (sección 13.2: "interferencias por tormenta, profundidad o
    /// estructuras"): cerca del centro las agujas tiemblan y la calidad de señal baja. Los POIs de M6
    /// (interiores profundos, estructuras metálicas) la usan; las tormentas, más adelante.
    /// </summary>
    public class ProbeInterference : MonoBehaviour
    {
        [Tooltip("Radio de la zona, en metros. La interferencia es máxima en el centro y nula en el borde.")]
        [SerializeField, Range(1f, 500f)] private float radiusMeters = 20f;
        [Tooltip("Interferencia máxima (0-1) en el centro.")]
        [SerializeField, Range(0f, 1f)] private float strength = 0.6f;

        /// <summary>Interferencia (0-1) en un punto del mundo.</summary>
        public float At(Vector3 point)
        {
            float d = Vector3.Distance(point, transform.position);
            return d >= radiusMeters ? 0f : strength * (1f - d / radiusMeters);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radiusMeters);
        }
    }
}
