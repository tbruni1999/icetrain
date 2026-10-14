using System.Collections.Generic;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.Crafting
{
    /// <summary>Por qué todavía no se puede fabricar algo (o None si se puede).</summary>
    public enum CraftBlock
    {
        None,
        /// <summary>Faltan materiales en la mochila.</summary>
        MissingMaterials,
        /// <summary>Falta una herramienta que la receta pide (no se gasta, pero hay que tenerla).</summary>
        MissingTool,
        /// <summary>Es una herramienta y ya se tiene: no tiene sentido fabricar otra.</summary>
        AlreadyHave,
        /// <summary>Lo que sale necesita un lugar libre sobre la mesa y está ocupado.</summary>
        OutputBlocked
    }

    /// <summary>
    /// El banco de trabajo del taller (secciones 4, 10.1 y 20.6): el mueble donde se fabrican cosas con los planos que se
    /// conocen. Acá vive la lógica de "¿se puede fabricar?" y "fabricar" (gastar materiales y entregar el resultado); el
    /// panel (WorkbenchPanel) solo la muestra y maneja el tiempo y los controles.
    /// - Usa los materiales de la mochila (lo que está guardado en un estante hay que sacarlo antes).
    /// - Las herramientas que pide una receta se tienen que tener, pero no se gastan.
    /// - Lo que sale es una herramienta que queda al cinto (PlayerGear) o un bidón vacío sobre la mesa.
    /// - El ID estable es para el guardado futuro (qué banco es y en qué nivel está, Anexo B.4).
    /// </summary>
    public class Workbench : MonoBehaviour, IInteractable
    {
        [Tooltip("ID estable del banco (para el guardado futuro).")]
        [SerializeField] private string stableId = "taller_banco_001";
        [SerializeField] private string displayName = "banco de trabajo";
        [Tooltip("Los planos que se pueden fabricar en este banco (se muestran los que el jugador conoce).")]
        [SerializeField] private Blueprint[] catalog = new Blueprint[0];
        [Tooltip("Dónde aparece lo que se fabrica y ocupa lugar (el bidón), sobre la mesa.")]
        [SerializeField] private Transform outputPoint;
        [Tooltip("El bidón vacío que sale de la receta del bidón.")]
        [SerializeField] private GameObject jerrycanPrefab;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public Blueprint[] Catalog => catalog;
        public Transform OutputPoint => outputPoint;

        /// <summary>Los planos del banco que el jugador conoce ahora, en el orden del banco.</summary>
        public List<Blueprint> KnownRecipes(BlueprintBook book)
        {
            var list = new List<Blueprint>();
            foreach (Blueprint blueprint in catalog)
                if (blueprint != null && blueprint.Kind == BlueprintKind.Craft && book != null && book.Knows(blueprint)) list.Add(blueprint);
            return list;
        }

        // ---------------------------------------------------------------- Se puede fabricar?

        /// <summary>Qué impide fabricar ahora (None si se puede). El orden importa: lo primero que se le dice al jugador.</summary>
        public CraftBlock Check(Blueprint blueprint, Backpack backpack, PlayerGear gear)
        {
            if (blueprint.OutputKind == CraftOutputKind.Gear && gear.Has(blueprint.OutputGear)) return CraftBlock.AlreadyHave;
            foreach (GearId tool in blueprint.RequiredTools)
                if (!gear.Has(tool)) return CraftBlock.MissingTool;
            foreach (CraftIngredient ingredient in blueprint.Ingredients)
                if (backpack.CountOf(ingredient.item) < ingredient.count) return CraftBlock.MissingMaterials;
            if (blueprint.OutputKind == CraftOutputKind.Jerrycan && OutputSpotBusy()) return CraftBlock.OutputBlocked;
            return CraftBlock.None;
        }

        /// <summary>Lo que falta, en palabras ("chapa ×1", "la sierra manual").</summary>
        public static string MissingText(Blueprint blueprint, Backpack backpack, PlayerGear gear)
        {
            var parts = new List<string>();
            foreach (GearId tool in blueprint.RequiredTools)
                if (!gear.Has(tool)) parts.Add("la " + Blueprint.ToolName(tool));
            foreach (CraftIngredient ingredient in blueprint.Ingredients)
            {
                int missing = ingredient.count - backpack.CountOf(ingredient.item);
                if (missing > 0) parts.Add($"{ingredient.item.PieceName} ×{missing}");
            }
            return string.Join(", ", parts);
        }

        // El lugar de la mesa donde sale el bidón: libre si no hay nada sólido encima (no cuenta el propio banco ni el jugador).
        private bool OutputSpotBusy()
        {
            if (outputPoint == null) return false;
            Vector3 center = outputPoint.position + Vector3.up * 0.27f;
            Collider[] hits = Physics.OverlapBox(center, new Vector3(0.2f, 0.25f, 0.2f), outputPoint.rotation, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                if (hit.GetComponentInParent<CharacterController>() != null) continue;
                if (hit.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- Fabricar

        /// <summary>
        /// Gasta los materiales y entrega lo fabricado. Devuelve false (sin gastar nada) si ya no se puede.
        /// </summary>
        public bool Complete(Blueprint blueprint, Backpack backpack, PlayerGear gear)
        {
            if (Check(blueprint, backpack, gear) != CraftBlock.None) return false;
            foreach (CraftIngredient ingredient in blueprint.Ingredients) backpack.Remove(ingredient.item, ingredient.count);

            switch (blueprint.OutputKind)
            {
                case CraftOutputKind.Gear:
                    gear.Give(blueprint.OutputGear);
                    break;
                case CraftOutputKind.Jerrycan:
                    if (jerrycanPrefab != null && outputPoint != null)
                    {
                        // El nombre marca que es fabricado: no está en la escena y al cargar una partida se vuelve a crear.
                        GameObject made = Instantiate(jerrycanPrefab, outputPoint.position + Vector3.up * 0.02f, outputPoint.rotation);
                        made.name = "Bidon_Fabricado";
                    }
                    break;
            }
            return true;
        }

        // ---------------------------------------------------------------- Interacción

        public string GetPrompt(PlayerInteractor interactor) =>
            interactor.GetComponent<WorkbenchPanel>() != null ? $"usar el {displayName}" : null;

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var panel = interactor.GetComponent<WorkbenchPanel>();
            if (panel != null) panel.Open(this);
        }
    }
}
