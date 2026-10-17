using System;
using SurvivalEnTren.Equipment;
using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>Una cantidad de un material que sale de desmontar algo.</summary>
    [Serializable]
    public struct SalvageYield
    {
        public ItemDefinition item;
        [Min(1)] public int count;
    }

    /// <summary>De qué está hecho lo que se golpea: cambia el ruido y lo que salta con cada golpe.</summary>
    public enum SalvageSurface
    {
        Madera,
        Metal,
        Tela,
        Electrico,
        /// <summary>Roca (G1): polvo y pedazos grises, golpe seco.</summary>
        Piedra
    }

    /// <summary>Otra herramienta que también sirve, pero pega menos (G1: "herramienta equivocada: pega poco o rebota").</summary>
    [Serializable]
    public struct ToolEfficiency
    {
        public GearId tool;
        [Tooltip("Cuánto vale un golpe con esta herramienta, comparado con la buena (0,3 = un tercio).")]
        [Range(0.05f, 1f)] public float factor;
    }

    /// <summary>
    /// Qué se necesita para desmontar un tipo de objeto y qué sale (sección 9.2: "la acción consume tiempo y
    /// produce materiales coherentes"). Es un dato por tipo de objeto (pallet, armario...): la herramienta, los
    /// golpes y los materiales se ajustan en el archivo, sin tocar código (Anexo B.3).
    /// Desde H2 (herramientas y forja; DECISIÓN de Tomi del 2026-10-13) se desarma a golpes, no manteniendo E.
    /// Desde G1 (2026-10-17, "grindeo a lo 7 Days"): cada golpe que sirve da un poco de lo que sale directo a la mochila y al
    /// romperse sale el resto (lo mismo en total: nunca se pierde ni se duplica), y otras herramientas pueden servir a medias.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Objetos/Receta de desmontaje", fileName = "Desmontaje")]
    public class SalvageRecipe : ScriptableObject
    {
        [Tooltip("Cómo se llama el objeto en la indicación (\"pallet\").")]
        [SerializeField] private string displayName = "objeto";
        [Tooltip("Herramienta necesaria (sección 9.2): la barreta separa tablas y abre cajones.")]
        [SerializeField] private GearId requiredTool = GearId.Crowbar;
        [Tooltip("Cuántos golpes con la herramienta hacen falta para romperlo (0 = sin configurar: 6).")]
        [SerializeField, Range(0, 40)] private int hitsToBreak;
        [Tooltip("De qué está hecho (el ruido y las astillas o chispas de cada golpe).")]
        [SerializeField] private SalvageSurface surface = SalvageSurface.Madera;
        [Tooltip("Qué materiales salen, y cuántos.")]
        [SerializeField] private SalvageYield[] yields = Array.Empty<SalvageYield>();
        [Tooltip("Cuántos pedazos salen volando al terminar.")]
        [SerializeField, Range(0, 30)] private int shardCount = 8;

        [Header("Picar (G1)")]
        [Tooltip("Qué se hace con él, en la indicación: \"desarmar\" (muebles, autos), \"picar\" (rocas), \"cortar\" (árboles).")]
        [SerializeField] private string verb = "desarmar";
        [Tooltip("Cada golpe que sirve da su parte de lo que sale, directo a la mochila (si no entra, cae como bulto al lado). " +
                 "Apagado = todo sale junto al romperse, como antes de G1.")]
        [SerializeField] private bool yieldPerHit = true;
        [Tooltip("Otras herramientas que también sirven, y cuánto pega cada una comparada con la buena. Las que no están, rebotan.")]
        [SerializeField] private ToolEfficiency[] otherTools = Array.Empty<ToolEfficiency>();

        public string DisplayName => displayName;
        public GearId RequiredTool => requiredTool;
        public bool YieldPerHit => yieldPerHit;
        public string Verb => string.IsNullOrEmpty(verb) ? "desarmar" : verb;
        public ToolEfficiency[] OtherTools => otherTools;

        /// <summary>Cuánto vale un golpe con esta herramienta (1 la buena, 0 = rebota).</summary>
        public float PowerOf(GearId tool)
        {
            if (tool == requiredTool) return 1f;
            foreach (ToolEfficiency t in otherTools) if (t.tool == tool) return Mathf.Clamp01(t.factor);
            return 0f;
        }
        public int HitsToBreak => hitsToBreak > 0 ? hitsToBreak : 6;
        public SalvageSurface Surface => surface;
        public SalvageYield[] Yields => yields;
        public int ShardCount => shardCount;

        /// <summary>Peso total de lo que sale, en kg.</summary>
        public float TotalMassKg
        {
            get
            {
                float total = 0f;
                foreach (SalvageYield y in yields) if (y.item != null) total += y.item.WeightKg * y.count;
                return total;
            }
        }

        /// <summary>Lo que sale, como texto corto: "madera ×4".</summary>
        public string YieldText()
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (SalvageYield y in yields) if (y.item != null) parts.Add($"{y.item.DisplayName} ×{y.count}");
            return string.Join(", ", parts);
        }
    }
}
