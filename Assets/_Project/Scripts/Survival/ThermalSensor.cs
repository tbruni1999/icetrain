using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Le dice al jugador qué temperatura de aire lo rodea: la del interior de un tren si está
    /// adentro, o la del exterior. Los espacios con clima propio se asignan en la escena (más
    /// adelante se suman refugios y POIs con calor residual, sección 13.1).
    /// </summary>
    [DefaultExecutionOrder(-50)] // antes que PlayerVitals, que usa la temperatura medida
    public class ThermalSensor : MonoBehaviour
    {
        [SerializeField] private PlayerVitals vitals;
        [Tooltip("Desde dónde se mide (los pies del jugador) y a qué altura (el pecho), en metros.")]
        [SerializeField] private Transform probe;
        [SerializeField] private float probeHeightMeters = 1f;
        [SerializeField] private TrainInteriorClimate[] climates;

        public bool IsSheltered { get; private set; }

        private void Update()
        {
            Vector3 point = probe.position + Vector3.up * probeHeightMeters;
            foreach (TrainInteriorClimate climate in climates)
            {
                if (climate != null && climate.Contains(point))
                {
                    IsSheltered = true;
                    vitals.AmbientTemperatureC = climate.InteriorTemperatureC;
                    return;
                }
            }
            IsSheltered = false;
            vitals.AmbientTemperatureC = vitals.Settings.OutsideTemperatureC;
        }
    }
}
