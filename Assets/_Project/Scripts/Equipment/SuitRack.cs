using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Perchero con el traje (secciones 7 y 20.3: "el traje ... modifica la pérdida térmica"). E se lo
    /// pone o lo deja; mientras lo lleva puesto, el traje desaparece del perchero. Con el traje el cuerpo
    /// pierde menos calor (PlayerGear).
    /// </summary>
    public class SuitRack : MonoBehaviour, IInteractable
    {
        [Tooltip("El traje colgado: se ve mientras NO se lo lleva puesto.")]
        [SerializeField] private GameObject hangingSuit;

        public string GetPrompt(PlayerInteractor interactor)
        {
            var equipment = interactor.GetComponent<PlayerGear>();
            if (equipment == null) return null;
            return equipment.SuitWorn ? "dejar el traje" : "ponerse el traje";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var equipment = interactor.GetComponent<PlayerGear>();
            if (equipment == null) return;
            equipment.SetSuit(!equipment.SuitWorn);
            hangingSuit.SetActive(!equipment.SuitWorn);
        }
    }
}
