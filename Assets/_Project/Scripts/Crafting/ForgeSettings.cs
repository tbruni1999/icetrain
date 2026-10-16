using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.Crafting
{
    /// <summary>
    /// Los números de la forja (herramientas y forja, paso H4; idea de Tomi del 2026-10-15, no está en el documento maestro: todo
    /// es PROPUESTA y se ajusta acá). Los tiempos están en horas de juego (1 s real = 1 min de juego).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Forja", fileName = "Forja")]
    public class ForgeSettings : ScriptableObject
    {
        [Header("Qué entra y qué sale")]
        [Tooltip("El metal en bruto que se mete al fuego (la chapa).")]
        [SerializeField] private ItemDefinition rawItem;
        [Tooltip("El metal trabajado que sale del yunque (la planchuela).")]
        [SerializeField] private ItemDefinition forgedItem;
        [Tooltip("La leña (el tablón).")]
        [SerializeField] private ItemDefinition woodItem;

        [Header("Fuego")]
        [Tooltip("Cuánto fuego da un tablón, en horas de juego (Tomi: la leña rinde menos que el combustible).")]
        [SerializeField, Range(0.05f, 3f)] private float woodHoursPerPlank = 0.4f;
        [Tooltip("Cuánta leña entra en el fogón, en horas de fuego.")]
        [SerializeField, Range(0.5f, 8f)] private float maxWoodHours = 2f;
        [Tooltip("Litros por hora que gasta del tanque del tren con la válvula del combustible abierta.")]
        [SerializeField, Range(0.1f, 10f)] private float dieselLitersPerHour = 2f;
        [Tooltip("Qué tan rápido se calienta la forja encendida (1 = de frío a caliente en una hora) y se enfría apagada.")]
        [SerializeField, Range(0.5f, 20f)] private float heatUpPerHour = 4f;
        [SerializeField, Range(0.5f, 20f)] private float coolDownPerHour = 2f;
        [Tooltip("Cuánto calienta el vagón con la forja a pleno, en °C por hora (se suma a la calefacción; sirve si falta combustible).")]
        [SerializeField, Range(0f, 60f)] private float cabinHeatCPerHour = 16f;

        [Header("La pieza")]
        [Tooltip("Qué tan rápido se calienta la pieza en el fuego y se enfría en el yunque (1 = en una hora).")]
        [SerializeField, Range(0.5f, 30f)] private float pieceHeatPerHour = 6f;
        [SerializeField, Range(0.5f, 30f)] private float pieceCoolPerHour = 4f;
        [Tooltip("Desde qué calor se puede martillar (al rojo), de 0 a 1.")]
        [SerializeField, Range(0.1f, 1f)] private float workHeat = 0.6f;
        [Tooltip("Martillazos para convertir una chapa en una planchuela.")]
        [SerializeField, Range(1, 40)] private int hitsToForge = 8;
        [Tooltip("Cuánto calor pierde la pieza con cada martillazo.")]
        [SerializeField, Range(0f, 0.3f)] private float heatLossPerHit = 0.03f;

        public ItemDefinition RawItem => rawItem;
        public ItemDefinition ForgedItem => forgedItem;
        public ItemDefinition WoodItem => woodItem;
        public float WoodHoursPerPlank => woodHoursPerPlank;
        public float MaxWoodHours => maxWoodHours;
        public float DieselLitersPerHour => dieselLitersPerHour;
        public float HeatUpPerHour => heatUpPerHour;
        public float CoolDownPerHour => coolDownPerHour;
        public float CabinHeatCPerHour => cabinHeatCPerHour;
        public float PieceHeatPerHour => pieceHeatPerHour;
        public float PieceCoolPerHour => pieceCoolPerHour;
        public float WorkHeat => workHeat;
        public int HitsToForge => hitsToForge;
        public float HeatLossPerHit => heatLossPerHit;
    }
}
