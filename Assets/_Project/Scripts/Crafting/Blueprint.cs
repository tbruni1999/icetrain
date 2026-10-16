using System;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.Crafting
{
    /// <summary>Una cantidad de un material que consume una receta.</summary>
    [Serializable]
    public struct CraftIngredient
    {
        public ItemDefinition item;
        [Min(1)] public int count;
    }

    public enum BlueprintKind
    {
        /// <summary>Se fabrica en el banco de trabajo.</summary>
        Craft,
        /// <summary>Habilita una obra del tren (ej. el estante reforzado): se construye por fases, no en el banco.</summary>
        Project
    }

    public enum CraftOutputKind
    {
        /// <summary>Una herramienta que queda al cinto del jugador (PlayerGear).</summary>
        Gear,
        /// <summary>Un bidón vacío que aparece sobre el banco.</summary>
        Jerrycan
    }

    /// <summary>
    /// Un plano (sección 10.3): el conocimiento técnico que habilita fabricar o construir algo no trivial. Es un dato: lo
    /// que cuesta, qué herramientas pide, cuánto tarda y qué sale se ajusta en el archivo, sin tocar código (Anexo B.3).
    /// - Los planos iniciales se saben desde el arranque ("el jugador comienza con conocimientos suficientes para no
    ///   estancarse"); los encontrados amplían capacidades y no bloquean necesidades triviales.
    /// - Las herramientas que pide una receta (la sierra) no se gastan: hay que tenerlas.
    /// - El ID estable es para el guardado futuro (qué planos conoce el jugador, Anexo B.4).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Taller/Plano", fileName = "Plano")]
    public class Blueprint : ScriptableObject
    {
        [Tooltip("ID estable del plano (no se cambia una vez usado: lo usará el guardado).")]
        [SerializeField] private string id = "plano_nuevo";
        [Tooltip("Cómo se llama en el banco y en los avisos.")]
        [SerializeField] private string displayName = "plano";
        [Tooltip("Una línea que explica para qué sirve.")]
        [SerializeField] private string description = "";
        [Tooltip("Si el jugador ya lo sabe al empezar (planos iniciales) o hay que encontrarlo.")]
        [SerializeField] private bool knownFromStart = true;
        [SerializeField] private BlueprintKind kind = BlueprintKind.Craft;

        [Header("Receta (solo para planos de fabricar)")]
        [Tooltip("Materiales que se gastan al fabricar.")]
        [SerializeField] private CraftIngredient[] ingredients = Array.Empty<CraftIngredient>();
        [Tooltip("Herramientas que hay que tener (no se gastan).")]
        [SerializeField] private GearId[] requiredTools = Array.Empty<GearId>();
        [Tooltip("Cuántos segundos lleva fabricarlo.")]
        [SerializeField, Range(0.5f, 60f)] private float craftSeconds = 6f;
        [SerializeField] private CraftOutputKind outputKind = CraftOutputKind.Gear;
        [Tooltip("Si sale una herramienta: cuál.")]
        [SerializeField] private GearId outputGear = GearId.Saw;
        [Tooltip("Qué sale, en palabras (\"queda al cinto\", \"un bidón vacío sobre la mesa\").")]
        [SerializeField] private string outputText = "";

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public bool KnownFromStart => knownFromStart;
        public BlueprintKind Kind => kind;
        public CraftIngredient[] Ingredients => ingredients;
        public GearId[] RequiredTools => requiredTools;
        public float CraftSeconds => craftSeconds;
        public CraftOutputKind OutputKind => outputKind;
        public GearId OutputGear => outputGear;
        public string OutputText => outputText;

        /// <summary>Nombre de una herramienta para mostrar.</summary>
        public static string ToolName(GearId gear)
        {
            switch (gear)
            {
                case GearId.Crowbar: return "barreta";
                case GearId.HandPump: return "bomba manual";
                case GearId.Saw: return "sierra manual";
                case GearId.Hammer: return "martillo";
                default: return gear.ToString();
            }
        }
    }
}
