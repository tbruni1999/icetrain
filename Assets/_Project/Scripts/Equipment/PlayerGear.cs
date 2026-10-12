using System;
using SurvivalEnTren.Survival;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>Herramientas que el jugador lleva "al cinto" (no ocupan la mano).</summary>
    public enum GearId
    {
        Crowbar,
        HandPump
    }

    /// <summary>
    /// Equipo del jugador (secciones 4, 5.3 y 20.3: "preparar equipo: herramienta, traje, sonda portátil,
    /// bidones"). Es lo mínimo para M6: qué herramientas tiene (barreta, bomba manual) y si lleva puesto el
    /// traje. No es el inventario con peso (eso es M7): las herramientas se guardan solas al tomarlas.
    /// - El traje abriga: el cuerpo pierde menos calor afuera (SurvivalSettings.suitCoolingFactor).
    /// </summary>
    public class PlayerGear : MonoBehaviour
    {
        [SerializeField] private PlayerVitals vitals;

        private bool _crowbar;
        private bool _handPump;

        public bool SuitWorn { get; private set; }

        /// <summary>Se dispara cuando cambia el equipo (para el panel de depuración, sonidos, etc.).</summary>
        public event Action Changed;

        public bool Has(GearId id)
        {
            switch (id)
            {
                case GearId.Crowbar: return _crowbar;
                case GearId.HandPump: return _handPump;
                default: return false;
            }
        }

        public void Give(GearId id)
        {
            switch (id)
            {
                case GearId.Crowbar: _crowbar = true; break;
                case GearId.HandPump: _handPump = true; break;
            }
            Changed?.Invoke();
        }

        public void SetSuit(bool worn)
        {
            SuitWorn = worn;
            vitals.CoolingMultiplier = worn ? vitals.Settings.SuitCoolingFactor : 1f;
            Changed?.Invoke();
        }
    }
}
