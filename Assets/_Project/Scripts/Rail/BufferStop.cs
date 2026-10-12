using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Tope de fin de vía, parte visual: la placa de choque se hunde en el bloque cuando la punta de
    /// un tren la empuja, y vuelve cuando el tren rebota. La física del choque (resorte amortiguado)
    /// está en TrainDynamics; acá solo se dibuja. La cara de la placa en reposo coincide con el final
    /// de la vía (el punto donde el resorte empieza a empujar).
    /// </summary>
    public class BufferStop : MonoBehaviour
    {
        [SerializeField] private TrainMover train;
        [SerializeField] private Transform plate;
        [Tooltip("Cuánto se puede hundir la placa, en metros.")]
        [SerializeField] private float maxTravelMeters = 0.8f;

        private Vector3 _plateRestLocal;

        private void Awake()
        {
            _plateRestLocal = plate.localPosition;
        }

        private void LateUpdate()
        {
            // Las dos puntas del tren (dibujadas), medidas hacia adentro del tope desde la cara en reposo.
            Transform t = train.transform;
            Vector3 face = transform.position;
            float compression = 0f;
            foreach (float sign in new[] { 1f, -1f })
            {
                Vector3 end = t.position + t.forward * (sign * train.HalfLengthMeters);
                compression = Mathf.Max(compression, Vector3.Dot(end - face, transform.forward));
            }
            compression = Mathf.Clamp(compression, 0f, maxTravelMeters);
            plate.localPosition = _plateRestLocal + Vector3.forward * compression;
        }
    }
}
