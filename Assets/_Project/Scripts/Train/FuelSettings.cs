using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Valores del combustible del tren (sección 8.1): el combustible alimenta el movimiento y la
    /// calefacción, y nunca se genera solo. Valores GENEROSOS para pruebas (Anexo G, M6); el balance
    /// final es posterior. Manejar gasta por kilómetro (desde el 2026-10-16: el tren va a velocidad real y el reloj, 60 veces más
    /// rápido; por hora de juego daba unos 14 L por km a 40 km/h, Tomi: "el gameplay es lento, rechequear el combustible"). La
    /// calefacción sigue por hora de juego: es la presión que más pesa (el frío).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Tren/Combustible", fileName = "FuelSettings")]
    public class FuelSettings : ScriptableObject
    {
        [Header("Tanque del tren")]
        [Tooltip("Cuánto entra en el tanque, en litros.")]
        [SerializeField, Range(10f, 2000f)] private float tankCapacityLiters = 200f;
        [Tooltip("Cuánto hay al empezar: reserva baja, suficiente para no morir de inmediato (sección 4).")]
        [SerializeField, Range(0f, 2000f)] private float startLiters = 60f;
        [Tooltip("Por debajo de esta fracción del tanque se enciende la luz de reserva en la cabina.")]
        [SerializeField, Range(0.01f, 0.5f)] private float lowFuelFraction = 0.15f;

        [Header("Consumo")]
        [Tooltip("Cuánto gasta el motor por kilómetro recorrido con cada posición de potencia (1, 2, 3, 4...): más rápido gasta " +
                 "más por kilómetro. Solo gasta con el reversor puesto y potencia; parado con freno no gasta.")]
        [SerializeField] private float[] driveLitersPerKm = { 0.8f, 1.0f, 1.3f, 1.8f };
        [Tooltip("Cuánto gasta la calefacción por hora de juego estando encendida, también con el tren parado.")]
        [SerializeField, Range(0f, 20f)] private float heaterLitersPerHour = 1.2f;

        [Header("Bidón (se lleva en la mano; pesa según lo que tiene)")]
        [Tooltip("Cuánto entra en un bidón, en litros.")]
        [SerializeField, Range(1f, 60f)] private float canCapacityLiters = 20f;
        [Tooltip("Peso del bidón vacío, en kg.")]
        [SerializeField, Range(0.1f, 10f)] private float canEmptyMassKg = 1.5f;
        [Tooltip("Cuánto pesa un litro de combustible, en kg (el gasoil pesa ~0,85). Un bidón lleno de 20 L pesa ~18,5 kg.")]
        [SerializeField, Range(0.5f, 1.2f)] private float fuelKgPerLiter = 0.85f;

        [Header("Trasvase (litros por segundo mientras se mantiene E)")]
        [Tooltip("Del bidón al tanque del tren por la boca de carga.")]
        [SerializeField, Range(0.1f, 20f)] private float pourLitersPerSecond = 4f;
        [Tooltip("Sifonar con el bidón y una manguera (sección 8.1, etapa 1: manual, lento).")]
        [SerializeField, Range(0.05f, 10f)] private float siphonLitersPerSecond = 1.5f;
        [Tooltip("Con una bomba manual (etapa 2: mucho más rápido que el sifón).")]
        [SerializeField, Range(0.1f, 20f)] private float handPumpLitersPerSecond = 4f;

        [Header("Combustible de la escena de prueba (cuánto hay en cada fuente)")]
        [Tooltip("Vehículo abandonado junto a la vía: se saca con sifón.")]
        [SerializeField, Range(0f, 500f)] private float abandonedVehicleLiters = 15f;
        [Tooltip("Depósito de la estación de mantenimiento: solo se saca con una bomba manual.")]
        [SerializeField, Range(0f, 2000f)] private float stationDepotLiters = 150f;

        public float CanCapacityLiters => canCapacityLiters;
        public float CanEmptyMassKg => canEmptyMassKg;
        public float FuelKgPerLiter => fuelKgPerLiter;
        public float PourLitersPerSecond => pourLitersPerSecond;
        public float SiphonLitersPerSecond => siphonLitersPerSecond;
        public float HandPumpLitersPerSecond => handPumpLitersPerSecond;
        public float AbandonedVehicleLiters => abandonedVehicleLiters;
        public float StationDepotLiters => stationDepotLiters;

        public float TankCapacityLiters => tankCapacityLiters;
        public float StartLiters => Mathf.Min(startLiters, tankCapacityLiters);
        public float LowFuelFraction => lowFuelFraction;
        public float HeaterLitersPerHour => heaterLitersPerHour;

        /// <summary>Litros por kilómetro del motor con una posición de potencia (1 = la primera); 0 sin potencia.</summary>
        public float DriveLitersPerKm(int powerNotch)
        {
            if (powerNotch <= 0 || driveLitersPerKm.Length == 0) return 0f;
            return driveLitersPerKm[Mathf.Min(powerNotch, driveLitersPerKm.Length) - 1];
        }
    }
}
