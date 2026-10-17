using System;
using SurvivalEnTren.Survival;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>Herramientas que el jugador lleva "al cinto" (no ocupan la mano).</summary>
    public enum GearId
    {
        Crowbar,
        HandPump,
        Saw,
        /// <summary>El martillo de la forja (H4): se fabrica en el banco y se usa en el yunque.</summary>
        Hammer,
        /// <summary>La llave de montaje (2026-10-16): mueve los muebles que el jugador colocó (decisión de Tomi).</summary>
        Wrench,
        /// <summary>El hacha (G1, 2026-10-17): corta árboles muertos y troncos. Primero improvisada (piedra y palo).</summary>
        Axe,
        /// <summary>El pico (G1, 2026-10-17): pica rocas, mineral y carbón. Primero improvisado (piedra y palo).</summary>
        Pickaxe
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
        private bool _saw;
        private bool _hammer;
        private bool _wrench;

        public bool SuitWorn { get; private set; }

        /// <summary>Se dispara cuando cambia el equipo (para el panel de depuración, sonidos, etc.).</summary>
        public event Action Changed;

        public bool Has(GearId id)
        {
            switch (id)
            {
                case GearId.Crowbar: return _crowbar;
                case GearId.HandPump: return _handPump;
                case GearId.Saw: return _saw;
                case GearId.Hammer: return _hammer;
                case GearId.Wrench: return _wrench;
                default: return false;
            }
        }

        public void Give(GearId id)
        {
            switch (id)
            {
                case GearId.Crowbar: _crowbar = true; break;
                case GearId.HandPump: _handPump = true; break;
                case GearId.Saw: _saw = true; break;
                case GearId.Hammer: _hammer = true; break;
                case GearId.Wrench: _wrench = true; break;
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
