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
        Electrico
    }

    /// <summary>
    /// Qué se necesita para desmontar un tipo de objeto y qué sale (sección 9.2: "la acción consume tiempo y
    /// produce materiales coherentes"). Es un dato por tipo de objeto (pallet, armario...): la herramienta, los
    /// golpes y los materiales se ajustan en el archivo, sin tocar código (Anexo B.3).
    /// Desde H2 (herramientas y forja; DECISIÓN de Tomi del 2026-10-13) se desarma a golpes, no manteniendo E.
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

        public string DisplayName => displayName;
        public GearId RequiredTool => requiredTool;
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
