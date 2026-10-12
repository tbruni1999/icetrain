using System;
using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Tanque de combustible del tren (sección 8.1). Solo guarda cuántos litros hay: quien gasta
    /// (el motor, la calefacción) y quien carga (la boca de carga) le piden o le dan litros.
    /// No genera combustible por su cuenta.
    /// </summary>
    public class FuelTank : MonoBehaviour
    {
        private const float EmptyEpsilonLiters = 0.001f;

        [SerializeField] private FuelSettings settings;

        private float _liters;
        private bool _started;

        public FuelSettings Settings => settings;
        public float Capacity => settings.TankCapacityLiters;
        public float Liters { get { EnsureStarted(); return _liters; } }
        public float Fraction => Liters / Mathf.Max(1f, Capacity);
        public bool IsEmpty => Liters <= EmptyEpsilonLiters;
        public bool IsLow => Fraction <= settings.LowFuelFraction;
        public bool IsFull => Liters >= Capacity - EmptyEpsilonLiters;

        /// <summary>Se dispara cuando cambia la cantidad (para instrumentos, sonidos, etc.).</summary>
        public event Action Changed;

        private void Awake() => EnsureStarted();

        // Quien lo pide antes de Awake (otro Awake) igual ve el valor inicial.
        private void EnsureStarted()
        {
            if (_started) return;
            _started = true;
            _liters = settings.StartLiters;
        }

        /// <summary>Carga litros; devuelve cuántos entraron de verdad (el tanque tiene un tope).</summary>
        public float Add(float liters)
        {
            EnsureStarted();
            float added = Mathf.Clamp(liters, 0f, Capacity - _liters);
            if (added <= 0f) return 0f;
            _liters += added;
            Changed?.Invoke();
            return added;
        }

        /// <summary>Saca litros; devuelve cuántos se sacaron de verdad (no se puede gastar lo que no hay).</summary>
        public float Draw(float liters)
        {
            EnsureStarted();
            float drawn = Mathf.Clamp(liters, 0f, _liters);
            if (drawn <= 0f) return 0f;
            _liters -= drawn;
            if (_liters < EmptyEpsilonLiters) _liters = 0f;
            Changed?.Invoke();
            return drawn;
        }
    }
}
