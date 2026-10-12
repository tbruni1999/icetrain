using SurvivalEnTren.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Interacción mínima del M2: mira hacia donde apunta la cámara y, al apretar E, usa lo primero
    /// que encuentra si es usable (IInteractable). Si el jugador está "ocupado" con algo (por ejemplo,
    /// manejando desde el pupitre), E lo suelta aunque esté mirando a otro lado.
    /// Dibuja un punto chico en el centro de la pantalla para apuntar; se agranda sobre algo usable.
    /// El sistema completo (mantener apretado, avisos, puertas, llevar objetos) es del M3.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Transform head;
        [SerializeField] private InputActionReference interact;
        [Tooltip("Distancia máxima a la que se puede usar algo, en metros.")]
        [SerializeField] private float reachMeters = 2.2f;

        private IInteractable _target;
        private IInteractable _engaged;

        public PlayerMotor Motor => motor;
        public bool HasTarget => _target != null;

        private void OnEnable()
        {
            interact.action.performed += OnInteract;
            interact.action.Enable();
        }

        private void OnDisable()
        {
            interact.action.performed -= OnInteract;
        }

        private void Update()
        {
            _target = FindTarget();
        }

        /// <summary>Lo llama un objeto que "ocupa" al jugador (ej. el pupitre): E lo va a soltar.</summary>
        public void Engage(IInteractable interactable) => _engaged = interactable;

        public void Release(IInteractable interactable)
        {
            if (_engaged == interactable) _engaged = null;
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (_engaged != null)
            {
                _engaged.Interact(this);
                return;
            }
            _target?.Interact(this);
        }

        private IInteractable FindTarget()
        {
            int count = Physics.RaycastNonAlloc(head.position, head.forward, Hits, reachMeters, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(Hits, 0, count, HitDistanceComparer.Instance);
            for (int i = 0; i < count; i++)
            {
                if (Hits[i].collider.transform.IsChildOf(transform)) continue; // el propio cuerpo
                return Hits[i].collider.GetComponentInParent<IInteractable>();  // lo primero que tapa la vista
            }
            return null;
        }

        private void OnGUI()
        {
            float size = _target != null && _engaged == null ? 8f : 4f;
            var rect = new Rect((Screen.width - size) * 0.5f, (Screen.height - size) * 0.5f, size, size);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private sealed class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
