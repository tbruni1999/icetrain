using UnityEngine;

namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Puerta corrediza (M3): E abre y cierra, la hoja se desliza animada y, cerrada, bloquea el paso.
    /// - No se cierra si hay alguien o algo suelto en el hueco (no aplasta ni patea cosas).
    /// - El hueco tiene una zona de interacción (un collider "trigger" que no bloquea), para poder
    ///   cerrar la puerta abierta apuntando al hueco aunque la hoja quede afuera de la vista.
    /// - Es estado del mundo: más adelante se guarda (M12) y la del tren separa frío y calor (M4/M13,
    ///   secciones 19 y 20.7).
    /// </summary>
    public class SlidingDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform leaf;
        [Tooltip("Cuánto se corre la hoja al abrirse, en coordenadas locales de la puerta (metros).")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 0f, -1.05f);
        [Tooltip("Centro y medio tamaño del hueco (local), para no cerrar si hay alguien o algo adentro.")]
        [SerializeField] private Vector3 doorwayCenter;
        [SerializeField] private Vector3 doorwayHalfExtents = new Vector3(0.35f, 1f, 0.5f);
        [SerializeField] private bool startOpen;
        [Tooltip("Cuánto tarda en abrirse o cerrarse, en segundos.")]
        [SerializeField, Range(0.1f, 3f)] private float slideSeconds = 0.6f;
        [Tooltip("Sacudida de la hoja cuando no se puede cerrar porque hay algo en el hueco: metros y segundos.")]
        [SerializeField] private float blockedShakeMeters = 0.03f;
        [SerializeField] private float blockedShakeSeconds = 0.3f;

        private static readonly Collider[] Overlaps = new Collider[8];

        private Vector3 _leafClosedLocal;
        private bool _open;
        private float _openness;
        private float _shakeTimeLeft;

        public bool IsOpen => _open;

        private void Awake()
        {
            _leafClosedLocal = leaf.localPosition;
            _open = startOpen;
            _openness = startOpen ? 1f : 0f;
            ApplyLeaf(0f);
        }

        public string GetPrompt(PlayerInteractor interactor) => _open ? "cerrar" : "abrir";

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            if (_open && IsDoorwayOccupied())
            {
                _shakeTimeLeft = blockedShakeSeconds;
                return;
            }
            _open = !_open;
        }

        private bool IsDoorwayOccupied()
        {
            int count = Physics.OverlapBoxNonAlloc(transform.TransformPoint(doorwayCenter), doorwayHalfExtents, Overlaps,
                transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = Overlaps[i];
                if (c is CharacterController) return true;                           // una persona
                if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) return true; // algo suelto
            }
            return false;
        }

        private void Update()
        {
            _openness = Mathf.MoveTowards(_openness, _open ? 1f : 0f, Time.deltaTime / slideSeconds);
            float shake = 0f;
            if (_shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= Time.deltaTime;
                shake = Mathf.Sin(_shakeTimeLeft * 50f) * blockedShakeMeters * Mathf.Clamp01(_shakeTimeLeft / blockedShakeSeconds);
            }
            ApplyLeaf(shake);
        }

        private void ApplyLeaf(float shake)
        {
            float eased = Mathf.SmoothStep(0f, 1f, _openness);
            Vector3 shakeOffset = openOffset.normalized * shake;
            leaf.localPosition = _leafClosedLocal + openOffset * eased + shakeOffset;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.72f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(doorwayCenter, doorwayHalfExtents * 2f);
        }
    }
}
