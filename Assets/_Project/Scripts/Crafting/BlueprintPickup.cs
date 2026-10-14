using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Crafting
{
    /// <summary>
    /// Un plano encontrado en el mundo (sección 10.3: "planos y diagramas físicos"): una hoja sobre un escritorio. E la
    /// lee: el jugador aprende el plano y la hoja desaparece (así no se aprende dos veces ni se duplica).
    /// </summary>
    public class BlueprintPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private Blueprint blueprint;

        public Blueprint Blueprint => blueprint;

        public string GetPrompt(PlayerInteractor interactor)
        {
            var book = interactor.GetComponent<BlueprintBook>();
            if (book == null || blueprint == null || book.Knows(blueprint)) return null;
            return $"leer el plano: {blueprint.DisplayName}";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var book = interactor.GetComponent<BlueprintBook>();
            if (book == null || blueprint == null) return;
            if (book.Learn(blueprint)) Destroy(gameObject);
        }
    }
}
