namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Algo que el jugador puede usar mirándolo y apretando (o manteniendo) E: el pupitre de la
    /// cabina, la palanca de un cambio, una puerta, un objeto que se puede llevar...
    /// Cada objeto dice qué verbo mostrar y si hay que mantener E y cuánto (sección 22.2:
    /// "raycast/target, mantener tecla, animaciones e interfaces de uso").
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Verbo corto para la indicación en pantalla ("abrir", "tomar"...). Vacío o null = ahora no se
        /// puede usar (no se muestra nada y E no hace nada).
        /// </summary>
        string GetPrompt(PlayerInteractor interactor);

        /// <summary>Segundos que hay que mantener E para usarlo; 0 = alcanza con tocar E.</summary>
        float GetHoldSeconds(PlayerInteractor interactor);

        void Interact(PlayerInteractor interactor);
    }
}
