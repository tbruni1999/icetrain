using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Boca de carga exterior del tren (sección 8.1; zona reservada "boca de combustible exterior").
    /// Con un bidón con combustible en la mano, mantener E vuelca el bidón en el tanque del tren, de a
    /// poco (FuelSettings.pourLitersPerSecond). Si se suelta E se corta sin perder lo ya cargado.
    /// </summary>
    public class FuelPort : MonoBehaviour, IChanneledInteractable, IUsableWhileCarrying
    {
        [SerializeField] private FuelTank tank;

        private FuelCan HeldCan(PlayerInteractor interactor)
        {
            var carrier = interactor.GetComponent<PlayerCarrier>();
            return carrier != null && carrier.Holding != null ? carrier.Holding.GetComponent<FuelCan>() : null;
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            FuelCan can = HeldCan(interactor);
            if (can == null || can.Liters <= 0f || tank.IsFull) return null;
            return $"mantener para cargar el tanque   (bidón {can.Liters:0} L, tanque {tank.Liters:0} / {tank.Capacity:0} L)";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor) { } // todo pasa en Channel

        public bool Channel(PlayerInteractor interactor, float deltaTime)
        {
            FuelCan can = HeldCan(interactor);
            if (can == null) return false;
            float wanted = tank.Settings.PourLitersPerSecond * deltaTime;
            float moved = tank.Add(can.Draw(Mathf.Min(wanted, tank.Capacity - tank.Liters)));
            return moved > 0f && can.Liters > 0f && !tank.IsFull;
        }
    }
}
