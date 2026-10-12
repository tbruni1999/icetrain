using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Bidón de combustible (sección 8.1, etapa 1: "bidón + sifón/manguera"). Es un objeto que se lleva en
    /// la mano (Carryable): su peso es el del bidón más el combustible que tiene (FuelSettings), así que
    /// lleno se camina más lento. Se llena sifonando o bombeando y se vacía en la boca de carga del tren.
    /// </summary>
    [RequireComponent(typeof(Carryable))]
    public class FuelCan : MonoBehaviour, IItemDescription
    {
        [SerializeField] private FuelSettings settings;
        [Tooltip("Litros que tiene al empezar.")]
        [SerializeField, Range(0f, 60f)] private float startLiters;

        private float _liters;
        private bool _started;
        private Carryable _carryable;

        public float Capacity => settings.CanCapacityLiters;
        public float Liters { get { EnsureStarted(); return _liters; } }
        public float FreeLiters => Capacity - Liters;

        private void Awake() => EnsureStarted();

        private void EnsureStarted()
        {
            if (_started) return;
            _started = true;
            _carryable = GetComponent<Carryable>();
            _liters = Mathf.Min(startLiters, Capacity);
            UpdateMass();
        }

        /// <summary>Llena el bidón; devuelve cuántos litros entraron (tiene un tope).</summary>
        public float Add(float liters)
        {
            EnsureStarted();
            float added = Mathf.Clamp(liters, 0f, Capacity - _liters);
            _liters += added;
            UpdateMass();
            return added;
        }

        /// <summary>Vacía el bidón; devuelve cuántos litros salieron.</summary>
        public float Draw(float liters)
        {
            EnsureStarted();
            float drawn = Mathf.Clamp(liters, 0f, _liters);
            _liters -= drawn;
            UpdateMass();
            return drawn;
        }

        public string Describe() => $"bidón {Liters:0} / {Capacity:0} L";

        private void UpdateMass() => _carryable.Body.mass = settings.CanEmptyMassKg + _liters * settings.FuelKgPerLiter;
    }
}
