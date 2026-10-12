using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Gasta el combustible que usa el motor (sección 8.1): con el reversor puesto y potencia, los
    /// litros por hora de juego de esa posición de potencia (FuelSettings). La calefacción gasta lo
    /// suyo en TrainInteriorClimate. Parado con freno, el motor no gasta.
    /// Al saltar horas (dormir, desmayarse) el tren está parado: ahí solo gasta la calefacción.
    /// </summary>
    public class TrainFuelBurner : MonoBehaviour
    {
        [SerializeField] private FuelTank tank;
        [SerializeField] private TrainControls controls;
        [SerializeField] private GameClock clock;

        /// <summary>Litros por hora que gasta el motor en este momento.</summary>
        public float DriveLitersPerHour =>
            controls.Reverser == ReverserPosition.Neutral ? 0f : tank.Settings.DriveLitersPerHour(controls.PowerNotch);

        private void Update()
        {
            float rate = DriveLitersPerHour;
            if (rate > 0f) tank.Draw(rate * clock.DeltaHours);
        }
    }
}
