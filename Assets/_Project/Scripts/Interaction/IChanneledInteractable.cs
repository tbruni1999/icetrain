namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Algo que se usa manteniendo E mientras dura la acción y se va haciendo de a poco: sifonar
    /// combustible, cargar el tanque, bombear. Distinto del "mantener E" común (que se llena una vez y
    /// listo): acá cada cuadro que E sigue apretado se avanza un poco, y si se suelta se corta sin perder
    /// lo ya hecho. El texto de la indicación puede mostrar el avance (GetPrompt se pide cada cuadro).
    /// </summary>
    public interface IChanneledInteractable : IInteractable
    {
        /// <summary>
        /// Avanza la acción durante deltaTime segundos. Devuelve false cuando ya no hay nada más que
        /// hacer (se terminó o no se puede seguir): hay que soltar E y volver a apretar para reintentar.
        /// </summary>
        bool Channel(PlayerInteractor interactor, float deltaTime);
    }

    /// <summary>
    /// Marca los objetos que se pueden usar con las manos ocupadas llevando algo (un bidón). Por defecto,
    /// con algo en la mano E lo suelta; si se mira un objeto con esta marca y tiene indicación, E lo usa a
    /// él. Su GetPrompt debe devolver null salvo que lo que se lleva sirva para usarlo.
    /// </summary>
    public interface IUsableWhileCarrying
    {
    }

    /// <summary>Texto corto que un objeto agrega a su indicación ("bidón 12 / 20 L").</summary>
    public interface IItemDescription
    {
        string Describe();
    }
}
