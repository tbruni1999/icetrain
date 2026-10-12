using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Valores de supervivencia (sección 7) y del reloj del juego. Son valores GENEROSOS para pruebas
    /// (Anexo G, M4); el balance final es posterior. Todos los ritmos están en horas de juego.
    /// Los estados van de 0 a 100: vida, saciedad (100 = sin hambre), energía y calor (100 = abrigado).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Supervivencia/Supervivencia", fileName = "SurvivalSettings")]
    public class SurvivalSettings : ScriptableObject
    {
        [Header("Reloj del juego (PROPUESTA)")]
        [Tooltip("Minutos de juego que pasan por cada segundo real. 1 = un día dura 24 minutos reales.")]
        [SerializeField, Range(0.1f, 10f)] private float gameMinutesPerRealSecond = 1f;

        [Header("Hambre (sección 7: baja lento, primero afecta energía y recuperación)")]
        [Tooltip("Horas de juego para pasar de saciado (100) a vacío (0) estando despierto.")]
        [SerializeField, Range(4f, 200f)] private float hungerHoursFullToEmpty = 36f;
        [Tooltip("Durmiendo el hambre avanza más lento (fracción del ritmo despierto).")]
        [SerializeField, Range(0f, 1f)] private float hungerSleepFactor = 0.6f;
        [Tooltip("Por debajo de esta saciedad hay \"hambre temprana\": la energía baja más rápido y no hay recuperación de vida.")]
        [SerializeField, Range(0f, 100f)] private float hungryThreshold = 25f;

        [Header("Energía (sección 7: descanso, no stamina; dormir es la recuperación principal)")]
        [Tooltip("Horas de juego despierto para pasar de 100 a 0 en condiciones normales.")]
        [SerializeField, Range(4f, 100f)] private float energyHoursAwakeFullToEmpty = 20f;
        [Tooltip("Multiplica el gasto de energía con frío (calor por debajo de 50).")]
        [SerializeField, Range(1f, 4f)] private float energyColdFactor = 1.5f;
        [Tooltip("Multiplica el gasto de energía con hambre temprana.")]
        [SerializeField, Range(1f, 4f)] private float energyHungryFactor = 1.5f;
        [Tooltip("Multiplica el gasto de energía mientras se corre (trabajo físico).")]
        [SerializeField, Range(1f, 6f)] private float energyRunningFactor = 2f;
        [Tooltip("Con energía en 0: fracción de la velocidad de caminar (no se puede correr).")]
        [SerializeField, Range(0.3f, 1f)] private float exhaustedWalkFactor = 0.7f;

        [Header("Calor del cuerpo (sección 7: el exterior es el gran reloj de las expediciones)")]
        [Tooltip("Temperatura del aire a partir de la cual el cuerpo deja de perder calor, en °C.")]
        [SerializeField, Range(-10f, 25f)] private float comfortTemperatureC = 12f;
        [Tooltip("Calor que se pierde por hora por cada grado por debajo del confort. Con 0,25: a -25 °C se pasa de 100 a 0 en ~11 h de juego.")]
        [SerializeField, Range(0.01f, 2f)] private float coolingPerHourPerDegree = 0.25f;
        [Tooltip("Calor que se recupera por hora en un lugar templado (aire a la temperatura de confort o más).")]
        [SerializeField, Range(1f, 200f)] private float warmingPerHour = 40f;
        [Tooltip("Temperatura del exterior en las escenas de prueba, en °C (más adelante la da el clima).")]
        [SerializeField, Range(-60f, 10f)] private float outsideTemperatureC = -25f;

        [Header("Vida (sección 7: lesiones; no se regenera mágicamente)")]
        [Tooltip("Daño por hora con el calor del cuerpo en 0 (hipotermia avanzada).")]
        [SerializeField, Range(0f, 200f)] private float hypothermiaDamagePerHour = 10f;
        [Tooltip("Daño por hora con la saciedad en 0.")]
        [SerializeField, Range(0f, 100f)] private float starvationDamagePerHour = 4f;
        [Tooltip("Recuperación de vida por hora, solo si se está alimentado, abrigado y descansado (umbrales abajo).")]
        [SerializeField, Range(0f, 50f)] private float healthRegenPerHour = 3f;
        [SerializeField, Range(0f, 100f)] private float regenMinSatiety = 40f;
        [SerializeField, Range(0f, 100f)] private float regenMinWarmth = 50f;
        [SerializeField, Range(0f, 100f)] private float regenMinEnergy = 30f;
        [Tooltip("Velocidad de caída que no hace daño, en m/s (8 ≈ caer de unos 2 m).")]
        [SerializeField, Range(1f, 30f)] private float fallSafeSpeedMetersPerSecond = 8f;
        [Tooltip("Daño por cada m/s de caída por encima de la velocidad segura (12: caer de 5 m quita ~50).")]
        [SerializeField, Range(0f, 50f)] private float fallDamagePerMetersPerSecond = 12f;

        [Header("Refugios sin calefacción (estaciones, POIs)")]
        [Tooltip("Cuántos grados más templado que el exterior está el aire adentro de un lugar resguardado sin calefacción " +
                 "(sin viento, pero frío). Con 15 y -25 °C afuera: -10 °C adentro.")]
        [SerializeField, Range(0f, 40f)] private float unheatedShelterOffsetC = 15f;

        [Header("Traje (sección 7: el traje modifica la pérdida térmica)")]
        [Tooltip("Con el traje puesto, el cuerpo pierde esta fracción del calor que perdería sin él (0,5 = la mitad).")]
        [SerializeField, Range(0.05f, 1f)] private float suitCoolingFactor = 0.5f;

        [Header("Al morir (DECISIÓN de Tomi: castigo de recursos, no de progreso)")]
        [Tooltip("Horas de juego que pasan hasta despertar en la cama del tren.")]
        [SerializeField, Range(0f, 48f)] private float knockoutHours = 6f;
        [Tooltip("Estados al despertar: vida, saciedad, energía y calor.")]
        [SerializeField] private Vector4 wakeUpVitals = new Vector4(30f, 30f, 20f, 70f);

        public float GameMinutesPerRealSecond => gameMinutesPerRealSecond;
        public float HungerHoursFullToEmpty => hungerHoursFullToEmpty;
        public float HungerSleepFactor => hungerSleepFactor;
        public float HungryThreshold => hungryThreshold;
        public float EnergyHoursAwakeFullToEmpty => energyHoursAwakeFullToEmpty;
        public float EnergyColdFactor => energyColdFactor;
        public float EnergyHungryFactor => energyHungryFactor;
        public float EnergyRunningFactor => energyRunningFactor;
        public float ExhaustedWalkFactor => exhaustedWalkFactor;
        public float ComfortTemperatureC => comfortTemperatureC;
        public float CoolingPerHourPerDegree => coolingPerHourPerDegree;
        public float WarmingPerHour => warmingPerHour;
        public float OutsideTemperatureC => outsideTemperatureC;
        public float HypothermiaDamagePerHour => hypothermiaDamagePerHour;
        public float StarvationDamagePerHour => starvationDamagePerHour;
        public float HealthRegenPerHour => healthRegenPerHour;
        public float RegenMinSatiety => regenMinSatiety;
        public float RegenMinWarmth => regenMinWarmth;
        public float RegenMinEnergy => regenMinEnergy;
        public float FallSafeSpeedMetersPerSecond => fallSafeSpeedMetersPerSecond;
        public float FallDamagePerMetersPerSecond => fallDamagePerMetersPerSecond;
        public float UnheatedShelterOffsetC => unheatedShelterOffsetC;
        public float SuitCoolingFactor => suitCoolingFactor;
        public float KnockoutHours => knockoutHours;
        public Vector4 WakeUpVitals => wakeUpVitals;
    }
}
