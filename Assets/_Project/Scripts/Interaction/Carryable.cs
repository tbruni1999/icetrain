using UnityEngine;

namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Objeto que se puede llevar entero en la mano (sección 9.3): una caja, y más adelante una silla,
    /// un bidón... Su peso es la masa del Rigidbody. Mirándolo: "E tomar" (o "demasiado pesado").
    /// Mientras se lleva: "E soltar", clic izquierdo para lanzar. Quien lo sostiene es PlayerCarrier.
    /// Si el objeto tiene un IItemDescription (ej. el bidón), su texto se agrega a la indicación.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : MonoBehaviour, IInteractable
    {
        [Tooltip("Se lleva con una sola mano (un bidón): deja la otra libre para la sonda portátil. " +
                 "Los objetos grandes (cajas) ocupan las dos manos.")]
        [SerializeField] private bool oneHanded;

        private Rigidbody _body;
        private IItemDescription _description;
        private bool _descriptionLooked;

        public bool OneHanded => oneHanded;
        public Rigidbody Body => _body != null ? _body : _body = GetComponent<Rigidbody>();

        public string GetPrompt(PlayerInteractor interactor)
        {
            var carrier = interactor.GetComponent<PlayerCarrier>();
            if (carrier == null) return null;
            if (carrier.Holding == this) return "soltar   (clic: lanzar)" + Detail();
            if (carrier.Holding != null) return null;
            return (carrier.CanLift(this) ? "tomar" : "demasiado pesado") + Detail();
        }

        private string Detail()
        {
            if (!_descriptionLooked)
            {
                _description = GetComponent<IItemDescription>();
                _descriptionLooked = true;
            }
            return _description == null ? "" : "   (" + _description.Describe() + ")";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var carrier = interactor.GetComponent<PlayerCarrier>();
            if (carrier == null) return;
            if (carrier.Holding == this) carrier.Drop(throwIt: false);
            else if (carrier.Holding == null && carrier.CanLift(this)) carrier.Pick(this);
        }
    }
}
