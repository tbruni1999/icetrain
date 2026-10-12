using SurvivalEnTren.Rail;
using UnityEngine;

namespace SurvivalEnTren.Probe
{
    /// <summary>Tipos de señal de la sección 13.1.</summary>
    public enum ProbeSignalType
    {
        CalorResidual,
        EstructuraBajoNieve,
        MasaMetalica,
        EmisionAutomatica,
        Hidrocarburo
    }

    /// <summary>
    /// Algo que la sonda del tren puede detectar (sección 13.1): un POI, un depósito, una baliza…
    /// Dice qué tipo de señal emite, no qué hay adentro (sección 25.1).
    /// Tiene un ancla en la vía (Anexo D.2: "segmento, distancia y lado respecto a vía") para decir
    /// a cuántos metros por la vía está, y un ID estable para el guardado (M12).
    /// </summary>
    public class ProbeSignal : MonoBehaviour
    {
        [Tooltip("ID estable (Anexo D.2), por ejemplo poi_region01_railA_001.")]
        [SerializeField] private string stableId;
        [SerializeField] private ProbeSignalType type;
        [Tooltip("Intensidad (0-1): multiplica el alcance de la sonda. Débil = se detecta más cerca.")]
        [SerializeField, Range(0.05f, 1f)] private float strength = 1f;

        [Header("Ancla en la vía")]
        [SerializeField] private RailSegment anchorSegment;
        [SerializeField] private float anchorDistanceMeters;
        [Tooltip("De qué lado de la vía está, mirando en el sentido del tramo: 1 derecha, -1 izquierda.")]
        [SerializeField] private int anchorSide = 1;
        [Tooltip("A qué distancia de la vía está, en metros.")]
        [SerializeField] private float anchorLateralMeters;

        public string StableId => stableId;
        public ProbeSignalType Type => type;
        public float Strength => strength;
        public RailPosition Anchor => new RailPosition(anchorSegment, anchorDistanceMeters);
        public int AnchorSide => anchorSide;
        public float AnchorLateralMeters => anchorLateralMeters;

        /// <summary>Nombre que muestra la pantalla de la sonda.</summary>
        public static string DisplayName(ProbeSignalType type)
        {
            switch (type)
            {
                case ProbeSignalType.CalorResidual: return "CALOR RESIDUAL";
                case ProbeSignalType.EstructuraBajoNieve: return "ESTRUCTURA BAJO NIEVE";
                case ProbeSignalType.MasaMetalica: return "MASA METÁLICA";
                case ProbeSignalType.EmisionAutomatica: return "EMISIÓN AUTOMÁTICA";
                default: return "INDICIO DE HIDROCARBURO";
            }
        }

        /// <summary>Un número fijo por señal (de su ID), para que su error de lectura no salte cada cuadro.</summary>
        public float StableNoise(int salt)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in stableId ?? name) h = h * 31 + c;
                h = h * 31 + salt * 7919;
                return ((h & 0x7fffffff) % 10000) / 10000f * 2f - 1f; // -1..1
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.72f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 6f);
            if (anchorSegment != null) Gizmos.DrawLine(transform.position, Anchor.WorldPosition);
        }
    }
}
