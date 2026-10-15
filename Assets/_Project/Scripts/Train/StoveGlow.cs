using SurvivalEnTren.Survival;
using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// El resplandor de la estufa del tren (producción P1; guía de arte: "la estufa es el único punto naranja"): una luz chica
    /// y naranja que tiembla un poco, como un fuego. Se apaga despacio si la calefacción se apaga o se queda sin combustible,
    /// y vuelve cuando arranca. Solo dibuja.
    /// </summary>
    public class StoveGlow : MonoBehaviour
    {
        [SerializeField] private Light stoveLight;
        [Tooltip("Opcional: el clima del tren (si la calefacción está apagada o sin combustible, la estufa se apaga).")]
        [SerializeField] private TrainInteriorClimate climate;
        [Tooltip("Cuánto tiembla la luz (0 = fija).")]
        [SerializeField, Range(0f, 0.5f)] private float flicker = 0.12f;
        [Tooltip("Segundos que tarda en apagarse o prenderse.")]
        [SerializeField, Range(0.1f, 10f)] private float fadeSeconds = 2.5f;

        private float _baseIntensity;
        private float _level = 1f;
        private float _seed;

        private void Awake()
        {
            if (climate == null) climate = GetComponentInParent<TrainInteriorClimate>();
            _baseIntensity = stoveLight.intensity;
            _seed = Random.value * 50f;
        }

        private void Update()
        {
            bool burning = climate == null || (climate.HeaterOn && !climate.FuelStarved);
            _level = Mathf.MoveTowards(_level, burning ? 1f : 0f, Time.deltaTime / fadeSeconds);
            float wobble = 1f + flicker * (Mathf.PerlinNoise(_seed, Time.time * 2.3f) * 2f - 1f);
            stoveLight.intensity = _baseIntensity * _level * wobble;
            stoveLight.enabled = _level > 0.01f;
        }
    }
}
