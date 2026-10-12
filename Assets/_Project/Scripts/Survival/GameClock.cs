using System;
using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Reloj del juego: cuántas horas de juego pasaron. Lo usan la supervivencia (hambre, energía,
    /// calor) y más adelante el ciclo de día y noche (sección 3.1). No es un "manager" global: quien
    /// lo necesita lo recibe por referencia.
    /// - Avanza solo con el tiempo real (SurvivalSettings.gameMinutesPerRealSecond).
    /// - Skip(horas) salta de golpe (dormir, desmayarse) y avisa con Skipped para que cada sistema
    ///   simule esas horas.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        [SerializeField] private SurvivalSettings settings;
        [Tooltip("Hora del día al empezar.")]
        [SerializeField, Range(0f, 24f)] private float startHour = 8f;

        private double _totalHours;

        /// <summary>Horas de juego desde el inicio de la partida.</summary>
        public double TotalHours => _totalHours;
        /// <summary>Hora del día, de 0 a 24.</summary>
        public float HourOfDay => (float)(_totalHours % 24.0);
        /// <summary>Horas de juego que pasaron en este cuadro.</summary>
        public float DeltaHours { get; private set; }
        /// <summary>Acelera el reloj (para pruebas). 1 = normal.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Se dispara al saltar horas de golpe, con la cantidad de horas.</summary>
        public event Action<float> Skipped;

        private void Awake()
        {
            _totalHours = startHour;
        }

        private void Update()
        {
            DeltaHours = Time.deltaTime * settings.GameMinutesPerRealSecond / 60f * SpeedMultiplier;
            _totalHours += DeltaHours;
        }

        public void Skip(float hours)
        {
            if (hours <= 0f) return;
            _totalHours += hours;
            Skipped?.Invoke(hours);
        }
    }
}
