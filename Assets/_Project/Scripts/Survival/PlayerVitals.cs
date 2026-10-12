using System;
using SurvivalEnTren.Player;
using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Los cuatro estados del jugador (sección 7, DECISIÓN): vida, hambre (como saciedad: 100 = sin
    /// hambre), energía y temperatura (como calor del cuerpo: 100 = abrigado). Van de 0 a 100.
    /// - Hambre: baja lento; con hambre temprana la energía baja más rápido y no hay recuperación;
    ///   recién vacía hace daño.
    /// - Energía: baja con el tiempo, más con frío, hambre y corriendo; dormir la recupera. En 0 se
    ///   camina más lento y no se puede correr (nunca se bloquean los controles).
    /// - Calor: baja si el aire está por debajo del confort (afuera), sube en un lugar templado.
    ///   En 0 hay daño (hipotermia).
    /// - Vida: daño por hipotermia, hambre sostenida o golpes; recuperación lenta solo si se está
    ///   alimentado, abrigado y descansado.
    /// Todo avanza con el reloj del juego, y también simula las horas que se saltan (dormir).
    /// </summary>
    public class PlayerVitals : MonoBehaviour
    {
        private const float MaxValue = 100f;
        private const float SkipStepHours = 0.1f;

        [SerializeField] private SurvivalSettings settings;
        [SerializeField] private GameClock clock;
        [SerializeField] private PlayerMotor motor;

        private bool _dead;

        public float Health { get; private set; } = MaxValue;
        public float Satiety { get; private set; } = 80f;
        public float Energy { get; private set; } = 75f;
        public float Warmth { get; private set; } = MaxValue;

        /// <summary>Temperatura del aire alrededor del jugador, en °C (la pone ThermalSensor).</summary>
        public float AmbientTemperatureC { get; set; }
        /// <summary>Recuperación de energía por hora mientras duerme (la pone la cama).</summary>
        public float SleepRecoveryPerHour { get; set; }
        public bool IsSleeping { get; set; }
        public bool IsHungry => Satiety < settings.HungryThreshold;
        public SurvivalSettings Settings => settings;
        public GameClock Clock => clock;

        /// <summary>Se dispara cuando la vida llega a 0.</summary>
        public event Action Died;

        private void Awake()
        {
            AmbientTemperatureC = settings.OutsideTemperatureC;
        }

        private void OnEnable()
        {
            clock.Skipped += OnClockSkipped;
        }

        private void OnDisable()
        {
            clock.Skipped -= OnClockSkipped;
        }

        private void Update()
        {
            Simulate(clock.DeltaHours, running: motor.IsRunning);
            ApplyToMovement();
        }

        // Al saltar horas (dormir, desmayarse) se simulan en pasos chicos, para que los umbrales funcionen.
        private void OnClockSkipped(float hours)
        {
            for (float done = 0f; done < hours; done += SkipStepHours)
                Simulate(Mathf.Min(SkipStepHours, hours - done), running: false);
        }

        public void Simulate(float hours, bool running)
        {
            if (hours <= 0f || _dead) return;

            // Hambre.
            float hungerRate = MaxValue / settings.HungerHoursFullToEmpty * (IsSleeping ? settings.HungerSleepFactor : 1f);
            Satiety = Mathf.Clamp(Satiety - hungerRate * hours, 0f, MaxValue);

            // Calor del cuerpo.
            float comfort = settings.ComfortTemperatureC;
            float warmthChange = AmbientTemperatureC >= comfort
                ? settings.WarmingPerHour
                : -settings.CoolingPerHourPerDegree * (comfort - AmbientTemperatureC);
            Warmth = Mathf.Clamp(Warmth + warmthChange * hours, 0f, MaxValue);

            // Energía.
            if (IsSleeping)
            {
                Energy = Mathf.Clamp(Energy + SleepRecoveryPerHour * hours, 0f, MaxValue);
            }
            else
            {
                float drain = MaxValue / settings.EnergyHoursAwakeFullToEmpty;
                if (Warmth < 50f) drain *= settings.EnergyColdFactor;
                if (IsHungry) drain *= settings.EnergyHungryFactor;
                if (running) drain *= settings.EnergyRunningFactor;
                Energy = Mathf.Clamp(Energy - drain * hours, 0f, MaxValue);
            }

            // Vida: daño y recuperación lenta.
            float damage = 0f;
            if (Warmth <= 0f) damage += settings.HypothermiaDamagePerHour;
            if (Satiety <= 0f) damage += settings.StarvationDamagePerHour;
            bool canRecover = Satiety >= settings.RegenMinSatiety && Warmth >= settings.RegenMinWarmth && Energy >= settings.RegenMinEnergy;
            float regen = damage <= 0f && canRecover ? settings.HealthRegenPerHour : 0f;
            ChangeHealth((regen - damage) * hours);
        }

        /// <summary>Daño directo (golpes, caídas).</summary>
        public void ApplyDamage(float amount) => ChangeHealth(-Mathf.Max(0f, amount));

        public void Eat(float satiety) => Satiety = Mathf.Clamp(Satiety + satiety, 0f, MaxValue);

        /// <summary>Vuelve a la vida con estos valores (al despertar después de desmayarse).</summary>
        public void Revive(float health, float satiety, float energy, float warmth)
        {
            Health = Mathf.Clamp(health, 1f, MaxValue);
            Satiety = Mathf.Clamp(satiety, 0f, MaxValue);
            Energy = Mathf.Clamp(energy, 0f, MaxValue);
            Warmth = Mathf.Clamp(warmth, 0f, MaxValue);
            _dead = false;
        }

        private void ChangeHealth(float delta)
        {
            if (_dead) return;
            Health = Mathf.Clamp(Health + delta, 0f, MaxValue);
            if (Health <= 0f)
            {
                _dead = true;
                Died?.Invoke();
            }
        }

        // Sin energía: más lento y sin correr. Nunca se bloquea el control (sección 24).
        private void ApplyToMovement()
        {
            bool exhausted = Energy <= 0f;
            motor.FatigueMultiplier = exhausted ? settings.ExhaustedWalkFactor : 1f;
            motor.CanRun = !exhausted;
        }
    }
}
