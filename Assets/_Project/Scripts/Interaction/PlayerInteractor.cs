using SurvivalEnTren.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Interacción del jugador (M3): mira hacia donde apunta la cámara, encuentra lo primero que tapa
    /// la vista y, si es usable (IInteractable), muestra una indicación mínima ("E abrir").
    /// - Tocar E usa las cosas que no piden mantener.
    /// - Mantener E usa las que sí: un anillo alrededor del punto central se llena; si se suelta E o
    ///   se deja de mirar el objeto antes de tiempo, se cancela.
    /// - Si el jugador está "ocupado" con algo (manejando desde el pupitre), E lo suelta aunque esté
    ///   mirando a otro lado.
    /// Sección 18: muy poco texto; blanco normal, ámbar para lo que está en progreso.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[8];
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);

        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Transform head;
        [SerializeField] private InputActionReference interact;
        [SerializeField] private InteractionSettings settings;

        private IInteractable _target;
        private IInteractable _engaged;
        private IInteractable _holding;
        private float _holdTime;
        private GUIStyle _promptStyle;

        public PlayerMotor Motor => motor;
        public Transform Head => head;
        public bool HasTarget => _target != null && !string.IsNullOrEmpty(_target.GetPrompt(this));
        /// <summary>Progreso del "mantener E" actual, de 0 a 1 (0 si no se está manteniendo nada).</summary>
        public float HoldProgress { get; private set; }

        private void OnEnable()
        {
            interact.action.Enable();
        }

        private void Update()
        {
            _target = FindTarget();
            InputAction action = interact.action;

            // Ocupado (manejando): E suelta, sin importar a dónde mira.
            if (_engaged != null)
            {
                ResetHold();
                if (action.WasPressedThisFrame()) _engaged.Interact(this);
                return;
            }

            IInteractable usable = _target != null && !string.IsNullOrEmpty(_target.GetPrompt(this)) ? _target : null;
            if (usable == null || !action.IsPressed())
            {
                ResetHold();
                return;
            }

            float hold = usable.GetHoldSeconds(this);
            if (hold <= 0f)
            {
                if (action.WasPressedThisFrame()) usable.Interact(this);
                ResetHold();
                return;
            }

            // Mantener: empieza solo con un toque nuevo sobre este objeto y se cancela si cambia.
            if (_holding != usable)
            {
                if (!action.WasPressedThisFrame()) return;
                _holding = usable;
                _holdTime = 0f;
            }
            _holdTime += Time.deltaTime;
            HoldProgress = Mathf.Clamp01(_holdTime / hold);
            if (_holdTime >= hold)
            {
                usable.Interact(this);
                ResetHold();
                _holding = usable; // no repetir mientras siga apretado
                _holdTime = float.NegativeInfinity;
            }
        }

        private void ResetHold()
        {
            _holding = null;
            _holdTime = 0f;
            HoldProgress = 0f;
        }

        /// <summary>Lo llama un objeto que "ocupa" al jugador (ej. el pupitre): E lo va a soltar.</summary>
        public void Engage(IInteractable interactable) => _engaged = interactable;

        public void Release(IInteractable interactable)
        {
            if (_engaged == interactable) _engaged = null;
        }

        private IInteractable FindTarget()
        {
            int count = Physics.RaycastNonAlloc(head.position, head.forward, Hits, settings.ReachMeters, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(Hits, 0, count, HitDistanceComparer.Instance);
            for (int i = 0; i < count; i++)
            {
                Collider c = Hits[i].collider;
                if (c.transform.IsChildOf(transform)) continue; // el propio cuerpo
                if (c.isTrigger)
                {
                    // Una zona de interacción (ej. el hueco de una puerta) cuenta; otros triggers se ignoran.
                    var zone = c.GetComponentInParent<IInteractable>();
                    if (zone != null) return zone;
                    continue;
                }
                return c.GetComponentInParent<IInteractable>(); // lo primero sólido que tapa la vista
            }
            return null;
        }

        private void OnGUI()
        {
            if (_promptStyle == null)
            {
                _promptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = settings.PromptFontSize
                };
            }

            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            IInteractable shown = _engaged ?? _target;
            string prompt = shown?.GetPrompt(this);
            bool usable = !string.IsNullOrEmpty(prompt);

            // Punto central: más grande sobre algo usable.
            float size = usable && _engaged == null ? 8f : 4f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), Texture2D.whiteTexture);

            // Anillo de "mantener": puntos alrededor del centro que se van encendiendo.
            if (HoldProgress > 0f)
            {
                const int dots = 24;
                float radius = settings.HoldRingRadiusPixels;
                for (int i = 0; i < dots; i++)
                {
                    float a = (i / (float)dots) * Mathf.PI * 2f;
                    bool lit = i / (float)dots < HoldProgress;
                    GUI.color = lit ? Amber : new Color(1f, 1f, 1f, 0.25f);
                    var p = center + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * radius;
                    GUI.DrawTexture(new Rect(p.x - 1.5f, p.y - 1.5f, 3f, 3f), Texture2D.whiteTexture);
                }
            }

            // Indicación mínima: tecla + verbo, debajo del punto.
            if (usable)
            {
                GUI.color = Color.white;
                float y = center.y + settings.HoldRingRadiusPixels + 8f;
                GUI.Label(new Rect(center.x - 200f, y, 400f, 40f), "E  " + prompt, _promptStyle);
            }
            GUI.color = Color.white;
        }

        private sealed class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
