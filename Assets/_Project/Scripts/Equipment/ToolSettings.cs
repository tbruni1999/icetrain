using System;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>Qué va en una casilla de la barra de herramientas.</summary>
    public enum HandSlotKind
    {
        /// <summary>Una herramienta del equipo (barreta, sierra…): se lleva en la mano y golpea con el clic.</summary>
        Gear,
        /// <summary>La sonda portátil (M5): se lleva en la mano pero no golpea.</summary>
        Probe
    }

    [Serializable]
    public struct ToolSlot
    {
        [Tooltip("Lo que se lee en la barra de abajo.")]
        public string label;
        public HandSlotKind kind;
        [Tooltip("La herramienta (si la casilla es de equipo). Solo se puede elegir si el jugador la tiene.")]
        public GearId gear;
        [Tooltip("Cuánto dura un golpe completo (levantar, bajar y volver), en segundos.")]
        [Range(0.2f, 2f)] public float swingSeconds;
        [Tooltip("En qué momento del golpe pega (0 = al empezar, 1 = al terminar).")]
        [Range(0.1f, 0.9f)] public float hitAt;
        [Tooltip("Hasta dónde llega el golpe desde los ojos, en metros.")]
        [Range(0.5f, 3f)] public float reachMeters;
    }

    /// <summary>
    /// Las herramientas en la mano (herramientas y forja, paso H1; pedido de Tomi del 2026-10-15). Una barra abajo con
    /// casillas fijas: con 1, 2, 3… o la rueda del mouse se elige qué se lleva en la mano; volver a apretar el mismo número
    /// deja las manos libres. Las casillas y los tiempos de cada golpe están acá, no en el código.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Herramientas en la mano", fileName = "Herramientas")]
    public class ToolSettings : ScriptableObject
    {
        [SerializeField] private ToolSlot[] slots =
        {
            new ToolSlot { label = "BARRETA", kind = HandSlotKind.Gear, gear = GearId.Crowbar, swingSeconds = 0.75f, hitAt = 0.42f, reachMeters = 2.2f },
            new ToolSlot { label = "SIERRA", kind = HandSlotKind.Gear, gear = GearId.Saw, swingSeconds = 0.6f, hitAt = 0.45f, reachMeters = 2.0f },
            new ToolSlot { label = "SONDA", kind = HandSlotKind.Probe }
        };

        [Tooltip("Qué tan rápido se saca y se guarda la herramienta (1 = un segundo).")]
        [SerializeField, Range(1f, 10f)] private float raiseSpeed = 4f;
        [Tooltip("Ángulo del golpe: cuánto se echa atrás la herramienta y cuánto baja al pegar, en grados.")]
        [SerializeField, Range(0f, 60f)] private float windupDegrees = 30f;
        [SerializeField, Range(30f, 140f)] private float strikeDegrees = 75f;
        [Tooltip("Cuánto tiempo queda frenada la herramienta contra lo que golpeó, en segundos.")]
        [SerializeField, Range(0f, 0.3f)] private float impactPauseSeconds = 0.08f;
        [Tooltip("Cuánta energía gasta cada golpe que sirve (de 0 a 100; H2: golpear cansa).")]
        [SerializeField, Range(0f, 3f)] private float energyPerHit = 0.25f;
        [Tooltip("Cuántos segundos se ve la barra bien marcada después de cambiar de herramienta (después queda tenue).")]
        [SerializeField, Range(0f, 5f)] private float barHighlightSeconds = 2f;

        public ToolSlot[] Slots => slots;
        public float RaiseSpeed => raiseSpeed;
        public float WindupDegrees => windupDegrees;
        public float StrikeDegrees => strikeDegrees;
        public float ImpactPauseSeconds => impactPauseSeconds;
        public float BarHighlightSeconds => barHighlightSeconds;
        public float EnergyPerHit => energyPerHit;
    }
}
