using System;
using UnityEngine;

namespace SurvivalEnTren.Train
{
    public enum ReverserPosition
    {
        Back = -1,
        Neutral = 0,
        Forward = 1
    }

    /// <summary>
    /// Estado de los controles de la cabina (sección 6.1): reversor, potencia y freno, con sus reglas.
    /// - El reversor (atrás / neutro / adelante) solo se mueve con el tren casi detenido.
    /// - Potencia y freno se manejan como una sola palanca combinada: subir primero suelta el freno
    ///   y después da potencia; bajar primero quita potencia y después frena. Así nunca hay
    ///   potencia y freno a la vez, y se entiende en segundos.
    /// - El tren arranca estacionado: reversor en neutro, sin potencia y con todo el freno puesto.
    /// La física que usa este estado está en TrainDynamics.
    /// </summary>
    public class TrainControls : MonoBehaviour
    {
        [SerializeField] private TrainDriveSettings settings;
        [SerializeField] private TrainMover train;

        public ReverserPosition Reverser { get; private set; } = ReverserPosition.Neutral;
        public int PowerNotch { get; private set; }
        public int BrakeNotch { get; private set; }
        public TrainDriveSettings Settings => settings;

        /// <summary>Se dispara cuando cambia cualquier control (para palancas, sonidos, etc.).</summary>
        public event Action Changed;

        /// <summary>
        /// Se dispara cuando se intenta mover el reversor con el tren en marcha (la palanca está
        /// trabada). Sirve para que la cabina lo muestre físicamente en vez de con texto.
        /// </summary>
        public event Action ReverserBlocked;

        private void Awake()
        {
            BrakeNotch = settings.BrakeNotchCount;
        }

        /// <summary>
        /// Mueve el reversor un paso (+1 hacia adelante, -1 hacia atrás).
        /// Devuelve false si no se pudo: el tren se está moviendo o ya está en el extremo.
        /// </summary>
        public bool TryMoveReverser(int step)
        {
            float speedKmh = Mathf.Abs(train.SpeedMetersPerSecond) * 3.6f;
            if (speedKmh > settings.ReverserMaxSpeedKmh)
            {
                ReverserBlocked?.Invoke();
                return false;
            }

            int next = Mathf.Clamp((int)Reverser + Math.Sign(step), -1, 1);
            if (next == (int)Reverser) return false;
            Reverser = (ReverserPosition)next;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Palanca combinada: +1 suelta freno o suma potencia; -1 quita potencia o suma freno.</summary>
        public void MovePowerBrake(int step)
        {
            if (step > 0)
            {
                if (BrakeNotch > 0) BrakeNotch--;
                else if (PowerNotch < settings.PowerNotchCount) PowerNotch++;
                else return;
            }
            else if (step < 0)
            {
                if (PowerNotch > 0) PowerNotch--;
                else if (BrakeNotch < settings.BrakeNotchCount) BrakeNotch++;
                else return;
            }
            else return;

            Changed?.Invoke();
        }
    }
}
