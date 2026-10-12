using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Una herramienta tirada o colgada en el mundo (la barreta del tren, la bomba manual de la
    /// estación). E la toma: pasa al equipo del jugador (PlayerGear) y el objeto desaparece del lugar
    /// (el que la tomó ya no la deja ahí: nada se duplica). El inventario con peso es M7.
    /// </summary>
    public class GearPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private GearId gear;
        [Tooltip("Cómo se llama en la indicación (\"tomar barreta\").")]
        [SerializeField] private string displayName = "herramienta";

        public string GetPrompt(PlayerInteractor interactor)
        {
            var equipment = interactor.GetComponent<PlayerGear>();
            return equipment == null || equipment.Has(gear) ? null : "tomar " + displayName;
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var equipment = interactor.GetComponent<PlayerGear>();
            if (equipment == null || equipment.Has(gear)) return;
            equipment.Give(gear);
            Destroy(gameObject);
        }
    }
}
