using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// De dónde se saca combustible con el bidón (sección 8.1): un vehículo abandonado (sifón con una
    /// manguera: lento) o el depósito de una estación (con una bomba manual: rápido). Tiene una cantidad
    /// fija de litros que no se repone: el combustible nunca es infinito. Con el bidón en la mano, mantener
    /// E llena el bidón de a poco; si se suelta E se corta sin perder lo ya sacado.
    /// El ID estable es para el guardado futuro (loot ya retirado, Anexo D.2).
    /// </summary>
    public class FuelSource : MonoBehaviour, IChanneledInteractable, IUsableWhileCarrying
    {
        public enum Method
        {
            Siphon,
            HandPump
        }

        [SerializeField] private FuelSettings settings;
        [Tooltip("ID estable de la fuente (para el guardado futuro).")]
        [SerializeField] private string stableId;
        [SerializeField] private Method method;
        [Tooltip("Cómo se llama en la indicación (\"vehículo\", \"depósito\").")]
        [SerializeField] private string displayName = "vehículo";
        [Tooltip("Litros que quedan.")]
        [SerializeField, Range(0f, 2000f)] private float liters = 15f;

        public string StableId => stableId;
        public float Liters => liters;

        /// <summary>Pone cuánto combustible le queda (al cargar una partida).</summary>
        public void RestoreLiters(float value) => liters = Mathf.Max(0f, value);

        private bool HasTool(PlayerInteractor interactor)
        {
            if (method == Method.Siphon) return true;
            var equipment = interactor.GetComponent<PlayerGear>();
            return equipment != null && equipment.Has(GearId.HandPump);
        }

        private static FuelCan HeldCan(PlayerInteractor interactor)
        {
            var carrier = interactor.GetComponent<PlayerCarrier>();
            return carrier != null && carrier.Holding != null ? carrier.Holding.GetComponent<FuelCan>() : null;
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            FuelCan can = HeldCan(interactor);
            if (can == null)
            {
                // Con las manos libres, una pista: hace falta un bidón. Ocupado con otra cosa, no se muestra nada
                // (E tiene que seguir soltando lo que se lleva).
                return interactor.IsEngaged || liters <= 0f ? null : $"{displayName}: hace falta un bidón";
            }
            if (liters <= 0.01f) return $"{displayName} vacío";
            if (!HasTool(interactor)) return $"{displayName}: hace falta una bomba";
            if (can.FreeLiters <= 0.01f) return "bidón lleno";
            string verb = method == Method.Siphon ? "sifonar" : "bombear";
            return $"mantener para {verb}   (quedan ~{liters:0} L, bidón {can.Liters:0} / {can.Capacity:0} L)";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor) { } // todo pasa en Channel

        public bool Channel(PlayerInteractor interactor, float deltaTime)
        {
            FuelCan can = HeldCan(interactor);
            if (can == null || liters <= 0f || !HasTool(interactor) || can.FreeLiters <= 0.01f) return false;
            float rate = method == Method.Siphon ? settings.SiphonLitersPerSecond : settings.HandPumpLitersPerSecond;
            float moved = can.Add(Mathf.Min(rate * deltaTime, liters));
            liters = Mathf.Max(0f, liters - moved);
            return moved > 0f && liters > 0f && can.FreeLiters > 0.01f;
        }
    }
}
