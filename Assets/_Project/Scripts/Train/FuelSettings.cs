using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Valores del combustible del tren (sección 8.1): el combustible alimenta el movimiento y la
    /// calefacción, y nunca se genera solo. Valores GENEROSOS para pruebas (Anexo G, M6); el balance
    /// final es posterior. Los consumos son litros por hora de juego (1 s real = 1 min de juego).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Tren/Combustible", fileName = "FuelSettings")]
    public class FuelSettings : ScriptableObject
    {
        [Header("Tanque del tren")]
        [Tooltip("Cuánto entra en el tanque, en litros.")]
        [SerializeField, Range(10f, 2000f)] private float tankCapacityLiters = 200f;
        [Tooltip("Cuánto hay al empezar: reserva baja, suficiente para no morir de inmediato (sección 4).")]
        [SerializeField, Range(0f, 2000f)] private float startLiters = 50f;
        [Tooltip("Por debajo de esta fracción del tanque se enciende la luz de reserva en la cabina.")]
        [SerializeField, Range(0.01f, 0.5f)] private float lowFuelFraction = 0.15f;

        [Header("Consumo (litros por hora de juego)")]
        [Tooltip("Cuánto gasta el motor con cada posición de potencia (1, 2, 3, 4...). " +
                 "Solo gasta con el reversor puesto y potencia; parado con freno no gasta.")]
        [SerializeField] private float[] driveLitersPerHour = { 2f, 4f, 6f, 8f };
        [Tooltip("Cuánto gasta la calefacción estando encendida, también con el tren parado.")]
        [SerializeField, Range(0f, 20f)] private float heaterLitersPerHour = 1.5f;

        public float TankCapacityLiters => tankCapacityLiters;
        public float StartLiters => Mathf.Min(startLiters, tankCapacityLiters);
        public float LowFuelFraction => lowFuelFraction;
        public float HeaterLitersPerHour => heaterLitersPerHour;

        /// <summary>Litros por hora del motor con una posición de potencia (1 = la primera); 0 sin potencia.</summary>
        public float DriveLitersPerHour(int powerNotch)
        {
            if (powerNotch <= 0 || driveLitersPerHour.Length == 0) return 0f;
            return driveLitersPerHour[Mathf.Min(powerNotch, driveLitersPerHour.Length) - 1];
        }
    }
}
