using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Gasta el combustible que usa el motor (sección 8.1): con el reversor puesto y potencia, los litros por kilómetro de esa
    /// posición de potencia (FuelSettings) por cada metro que se mueve el tren. La calefacción gasta lo suyo en
    /// TrainInteriorClimate. Parado, el motor no gasta (desde el 2026-10-16 se cuenta por distancia: el tren va a velocidad real y
    /// el reloj del juego, 60 veces más rápido).
    /// </summary>
    public class TrainFuelBurner : MonoBehaviour
    {
        [SerializeField] private FuelTank tank;
        [SerializeField] private TrainControls controls;
        [SerializeField] private GameClock clock;
        [Tooltip("El tren: de su velocidad sale cuánto se recorrió.")]
        [SerializeField] private TrainMover mover;

        /// <summary>Litros por kilómetro que gasta el motor con la potencia de ahora.</summary>
        public float DriveLitersPerKm =>
            controls.Reverser == ReverserPosition.Neutral ? 0f : tank.Settings.DriveLitersPerKm(controls.PowerNotch);

        private void Awake()
        {
            if (mover == null) mover = GetComponentInParent<TrainMover>();
            if (mover == null) mover = FindAnyObjectByType<TrainMover>();
        }

        private void Update()
        {
            float rate = DriveLitersPerKm;
            if (rate <= 0f || mover == null) return;
            float km = Mathf.Abs(mover.SpeedMetersPerSecond) * Time.deltaTime / 1000f;
            if (km > 0f) tank.Draw(rate * km);
        }
    }
}
