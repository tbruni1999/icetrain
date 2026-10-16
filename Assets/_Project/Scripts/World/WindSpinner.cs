using UnityEngine;

namespace SurvivalEnTren.World
{
    /// <summary>
    /// Algo que el viento hace girar sobre un eje (el sol de la torre de Solana, documento "Solana y POIs grandes", sección 2:
    /// "es lo único que se mueve sobre el horizonte"). Sin energía: gira solo, con un vaivén lento como de rachas.
    /// </summary>
    public class WindSpinner : MonoBehaviour
    {
        [Tooltip("Grados por segundo con viento normal.")]
        [SerializeField, Range(0f, 180f)] private float degreesPerSecond = 25f;
        [Tooltip("Cuánto varía con las rachas (0 = siempre igual, 1 = de parado al doble).")]
        [SerializeField, Range(0f, 1f)] private float gustiness = 0.5f;
        [SerializeField] private Vector3 localAxis = Vector3.up;

        private float _seed;

        private void Awake() => _seed = Random.value * 100f;

        private void Update()
        {
            float gust = 1f + gustiness * (Mathf.PerlinNoise(_seed, Time.time * 0.15f) * 2f - 1f);
            transform.Rotate(localAxis, degreesPerSecond * gust * Time.deltaTime, Space.Self);
        }
    }
}
