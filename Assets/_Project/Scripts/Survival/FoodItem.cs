using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Comida que se come en el lugar con E (una ración básica; sección 4: "pocas raciones básicas").
    /// Al comerla sube la saciedad y el objeto desaparece. Cocinar y mejorar la comida es posterior.
    /// </summary>
    public class FoodItem : MonoBehaviour, IInteractable
    {
        [Tooltip("Cuánto sube la saciedad (0-100) al comerla.")]
        [SerializeField, Range(1f, 100f)] private float satiety = 35f;

        public string GetPrompt(PlayerInteractor interactor) =>
            interactor.GetComponent<PlayerVitals>() != null ? "comer" : null;

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var vitals = interactor.GetComponent<PlayerVitals>();
            if (vitals == null) return;
            vitals.Eat(satiety);
            Destroy(gameObject);
        }
    }
}
