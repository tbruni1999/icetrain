using System;
using UnityEngine;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Estado de un cambio de vía (Anexo C.3 y sección 6.3: "los bloqueos ferroviarios son estado de mundo persistente:
    /// congelado, roto, sin energía, reparado, automatizado"). En esta versión: sano, congelado, roto o reparado.
    /// </summary>
    public enum RailSwitchCondition
    {
        /// <summary>Funciona desde el principio (el cambio manual del M2).</summary>
        Operational,
        /// <summary>Hielo sobre el mecanismo: hay que romperlo con una barreta.</summary>
        Frozen,
        /// <summary>Sin hielo, pero la palanca y la vía están dañadas: hace falta un proyecto de reparación.</summary>
        Broken,
        /// <summary>Se reparó: funciona como un cambio sano.</summary>
        Repaired
    }

    /// <summary>
    /// Cambio de vía (Anexo C.3: nodos de cambio en la red). El tramo de entrada termina en este
    /// cambio, y según su posición el tren sigue por la vía directa o por el desvío. Es estado del
    /// mundo (ver RailSwitchCondition): congelado o roto no se puede mover.
    /// - Al ir hacia atrás desde cualquiera de las dos salidas se vuelve al tramo de entrada, sin
    ///   importar la posición (como un cambio "a resorte"). Se puede endurecer más adelante.
    /// - No se puede mover mientras un tren lo está pisando.
    /// - El ID estable es para el guardado futuro (estado del cambio, Anexo B.4).
    /// </summary>
    public class RailSwitch : MonoBehaviour
    {
        [Tooltip("ID estable del cambio (para el guardado futuro). Vacío en los cambios que no se guardan.")]
        [SerializeField] private string stableId;
        [SerializeField] private RailSegment entry;
        [SerializeField] private RailSegment straightExit;
        [SerializeField] private RailSegment divergingExit;
        [SerializeField] private bool diverging;
        [Tooltip("En qué estado está al empezar (sano, congelado o roto).")]
        [SerializeField] private RailSwitchCondition condition = RailSwitchCondition.Operational;
        [Tooltip("Se considera que un tren pisa el cambio si su centro está a menos de esta distancia, en metros " +
                 "(medio tren + margen).")]
        [SerializeField] private float occupiedRadiusMeters = 13f;

        public string StableId => stableId;
        public bool IsDiverging => diverging;
        public RailSwitchCondition Condition => condition;
        /// <summary>Se puede mover con la palanca (sano o ya reparado).</summary>
        public bool CanMove => condition == RailSwitchCondition.Operational || condition == RailSwitchCondition.Repaired;
        public RailSegment ActiveExit => diverging ? divergingExit : straightExit;
        /// <summary>El tramo que sale por el desvío (la ruta que se abre al repararlo).</summary>
        public RailSegment DivergingExit => divergingExit;
        /// <summary>Punto del cambio en el mundo (el final del tramo de entrada).</summary>
        public Vector3 Point => entry.GetPosition(entry.Length);

        /// <summary>Se dispara cuando el cambio se mueve.</summary>
        public event Action Changed;
        /// <summary>Se dispara cuando cambia el estado (se rompe el hielo, se repara).</summary>
        public event Action ConditionChanged;

        public void Configure(RailSegment entrySegment, RailSegment straight, RailSegment divergingBranch)
        {
            entry = entrySegment;
            straightExit = straight;
            divergingExit = divergingBranch;
            entry.SetExitSwitch(this);
            straight.SetPrevious(entry);
            divergingBranch.SetPrevious(entry);
        }

        /// <summary>Cambia el estado del cambio. Si deja de poder moverse mientras está en desvío, vuelve a la vía directa.</summary>
        public void SetCondition(RailSwitchCondition value)
        {
            if (value == condition) return;
            condition = value;
            if (!CanMove && diverging)
            {
                diverging = false;
                Changed?.Invoke();
            }
            ConditionChanged?.Invoke();
        }

        /// <summary>Pone el estado y la posición del cambio de golpe (al cargar una partida).</summary>
        public void RestoreState(RailSwitchCondition value, bool divergingValue)
        {
            condition = value;
            diverging = divergingValue && CanMove;
            ConditionChanged?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Intenta mover el cambio. Devuelve false si está congelado o roto, o si un tren lo está pisando.</summary>
        public bool TrySetDiverging(bool value, Func<Vector3, bool> isOccupiedNear)
        {
            if (value == diverging) return true;
            if (!CanMove) return false;
            if (isOccupiedNear != null && isOccupiedNear(Point)) return false;
            diverging = value;
            Changed?.Invoke();
            return true;
        }

        public float OccupiedRadiusMeters => occupiedRadiusMeters;
    }
}
