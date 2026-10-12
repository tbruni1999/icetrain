namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Algo que el jugador puede usar mirándolo y apretando E (el pupitre de la cabina, la palanca
    /// de un cambio de vía...). Es la base mínima del M2; el sistema de interacción completo
    /// (mantener apretado, puertas, llevar objetos, avisos) es del M3 y se construye sobre esto.
    /// </summary>
    public interface IInteractable
    {
        void Interact(PlayerInteractor interactor);
    }
}
