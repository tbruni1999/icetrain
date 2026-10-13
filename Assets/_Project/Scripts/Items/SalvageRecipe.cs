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

    /// <summary>
    /// Qué se necesita para desmontar un tipo de objeto y qué sale (sección 9.2: "la acción consume tiempo y
    /// produce materiales coherentes"). Es un dato por tipo de objeto (pallet, armario...): la herramienta, el
    /// tiempo y los materiales se ajustan en el archivo, sin tocar código (Anexo B.3).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Objetos/Receta de desmontaje", fileName = "Desmontaje")]
    public class SalvageRecipe : ScriptableObject
    {
        [Tooltip("Cómo se llama el objeto en la indicación (\"pallet\").")]
        [SerializeField] private string displayName = "objeto";
        [Tooltip("Herramienta necesaria (sección 9.2): la barreta separa tablas y abre cajones.")]
        [SerializeField] private GearId requiredTool = GearId.Crowbar;
        [Tooltip("Cuántos segundos hay que mantener E para desmontarlo.")]
        [SerializeField, Range(0.5f, 30f)] private float salvageSeconds = 4f;
        [Tooltip("Qué materiales salen, y cuántos.")]
        [SerializeField] private SalvageYield[] yields = Array.Empty<SalvageYield>();
        [Tooltip("Cuántos pedazos salen volando al terminar.")]
        [SerializeField, Range(0, 30)] private int shardCount = 8;

        public string DisplayName => displayName;
        public GearId RequiredTool => requiredTool;
        public float SalvageSeconds => salvageSeconds;
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
