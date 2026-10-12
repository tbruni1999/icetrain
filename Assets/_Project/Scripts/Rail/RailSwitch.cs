using System;
using UnityEngine;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Cambio de vía (Anexo C.3: nodos de cambio en la red). El tramo de entrada termina en este
    /// cambio, y según su posición el tren sigue por la vía directa o por el desvío. Es estado del
    /// mundo: en el futuro puede estar congelado, roto o sin energía (sección 6.3).
    /// - Al ir hacia atrás desde cualquiera de las dos salidas se vuelve al tramo de entrada, sin
    ///   importar la posición (como un cambio "a resorte"). Se puede endurecer más adelante.
    /// - No se puede mover mientras un tren lo está pisando.
    /// </summary>
    public class RailSwitch : MonoBehaviour
    {
        [SerializeField] private RailSegment entry;
        [SerializeField] private RailSegment straightExit;
        [SerializeField] private RailSegment divergingExit;
        [SerializeField] private bool diverging;
        [Tooltip("Se considera que un tren pisa el cambio si su centro está a menos de esta distancia, en metros " +
                 "(medio tren + margen).")]
        [SerializeField] private float occupiedRadiusMeters = 13f;

        public bool IsDiverging => diverging;
        public RailSegment ActiveExit => diverging ? divergingExit : straightExit;
        /// <summary>Punto del cambio en el mundo (el final del tramo de entrada).</summary>
        public Vector3 Point => entry.GetPosition(entry.Length);

        /// <summary>Se dispara cuando el cambio se mueve.</summary>
        public event Action Changed;

        public void Configure(RailSegment entrySegment, RailSegment straight, RailSegment divergingBranch)
        {
            entry = entrySegment;
            straightExit = straight;
            divergingExit = divergingBranch;
            entry.SetExitSwitch(this);
            straight.SetPrevious(entry);
            divergingBranch.SetPrevious(entry);
        }

        /// <summary>Intenta mover el cambio. Devuelve false si un tren lo está pisando.</summary>
        public bool TrySetDiverging(bool value, Func<Vector3, bool> isOccupiedNear)
        {
            if (value == diverging) return true;
            if (isOccupiedNear != null && isOccupiedNear(Point)) return false;
            diverging = value;
            Changed?.Invoke();
            return true;
        }

        public float OccupiedRadiusMeters => occupiedRadiusMeters;
    }
}
