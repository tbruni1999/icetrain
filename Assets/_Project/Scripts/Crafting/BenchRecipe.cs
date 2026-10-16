using System;
using SurvivalEnTren.Building;
using SurvivalEnTren.Equipment;

namespace SurvivalEnTren.Crafting
{
    /// <summary>
    /// Una línea del banco de trabajo: un plano (una herramienta, un bidón, un mueble) o el kit de la próxima fase de una obra
    /// (la cama, la forja, la bifurcación). El panel las muestra todas igual.
    /// - Las herramientas y el bidón se fabrican en el momento, con el jugador en el banco (como desde el M9).
    /// - Los muebles y los kits de obra llevan un tiempo del reloj del juego y el banco sigue solo (decisión de Tomi del
    ///   2026-10-16: "que se mejoren en el workbench y tengan un timer").
    /// </summary>
    public sealed class BenchRecipe
    {
        public readonly Blueprint Blueprint;
        public readonly BuildSite Site;
        public readonly int Phase;

        public BenchRecipe(Blueprint blueprint)
        {
            Blueprint = blueprint;
        }

        public BenchRecipe(BuildSite site, int phase)
        {
            Site = site;
            Phase = phase;
        }

        public bool IsSitePhase => Site != null;
        public string PhaseName => IsSitePhase ? Site.Project.Phase(Phase).name : "";

        public string DisplayName => IsSitePhase ? $"{Site.DisplayName}: {PhaseName}" : Blueprint.DisplayName;

        public string Description => IsSitePhase
            ? $"Kit para la {Site.DisplayName}. Llevalo en la mano hasta la obra y mantené E para instalarlo."
            : Blueprint.Description;

        public CraftIngredient[] Ingredients
        {
            get
            {
                if (!IsSitePhase) return Blueprint.Ingredients;
                BuildRequirement[] reqs = Site.Project.Phase(Phase).requirements ?? Array.Empty<BuildRequirement>();
                var list = new CraftIngredient[reqs.Length];
                for (int i = 0; i < reqs.Length; i++) list[i] = new CraftIngredient { item = reqs[i].item, count = reqs[i].count };
                return list;
            }
        }

        public GearId[] RequiredTools => IsSitePhase ? Array.Empty<GearId>() : Blueprint.RequiredTools;

        /// <summary>Lleva un tiempo del reloj del juego y el banco sigue solo (sale un kit).</summary>
        public bool Timed => IsSitePhase || Blueprint.OutputKind == CraftOutputKind.Placeable;

        /// <summary>Minutos del juego que tarda (solo si es con tiempo).</summary>
        public float Minutes => IsSitePhase ? Site.Project.PhaseMinutes(Phase) : Blueprint.WorkMinutes;

        public string OutputText => IsSitePhase ? "un kit en el piso, al lado del banco" : Blueprint.OutputText;

        /// <summary>Cómo se guarda qué se está armando ("plano:ID" o "obra:ID:fase").</summary>
        public string Key => IsSitePhase ? $"obra:{Site.StableId}:{Phase}" : "plano:" + Blueprint.Id;

        /// <summary>Lo que dice el kit que sale ("base de madera · cama improvisada", "estante reforzado").</summary>
        public string KitName => IsSitePhase ? $"{PhaseName} · {Site.DisplayName}" : Blueprint.DisplayName;
    }
}
