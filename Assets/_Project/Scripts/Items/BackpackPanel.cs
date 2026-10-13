using SurvivalEnTren.Interaction;
using SurvivalEnTren.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// El panel de la mochila (sección 18.1, tercera capa: "UI de sistema: inventario ... solo al abrir").
    /// Tab lo abre y lo cierra. Muestra lo que se lleva, el peso (en blanco; ámbar cerca del límite, rojo
    /// pasado el 95%) y permite soltar. Sobrio: blanco/neutro y ámbar, sin colores de RPG (sección 18.4).
    /// - Mientras está abierto no se camina, no se usa nada con E y el cursor queda libre; el juego sigue
    ///   corriendo (no es una pausa: afuera hace frío).
    /// - Se maneja con el mouse (botones) o con el teclado: ↑↓ elegir, Enter soltar 1, Supr soltar todo.
    /// - Lo soltado aparece como un bulto con física delante del jugador (MaterialBundle): no se pierde.
    /// </summary>
    public class BackpackPanel : MonoBehaviour
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);
        private static readonly Color Danger = new Color(0.9f, 0.3f, 0.25f);

        [SerializeField] private Backpack backpack;
        [SerializeField] private BackpackSettings settings;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Transform head;
        [SerializeField] private InputActionReference toggle;
        [Header("Mapa \"Backpack\" (solo activo con el panel abierto)")]
        [SerializeField] private InputActionReference up;
        [SerializeField] private InputActionReference down;
        [SerializeField] private InputActionReference dropOne;
        [SerializeField] private InputActionReference dropAll;

        private bool _open;
        private bool _wasMovementLocked;
        private int _selected;
        private GUIStyle _label, _small, _button;
        private Texture2D _plate;

        public bool IsOpen => _open;
        public int Selected => _selected;

        private void OnEnable()
        {
            toggle.action.performed += OnToggle;
            toggle.action.Enable();
        }

        private void OnDisable()
        {
            toggle.action.performed -= OnToggle;
            if (_open) SetOpen(false);
        }

        private void OnToggle(InputAction.CallbackContext _) => SetOpen(!_open);

        public void SetOpen(bool open)
        {
            if (open == _open) return;
            _open = open;
            if (open)
            {
                _wasMovementLocked = motor.MovementLocked;
                motor.MovementLocked = true;
                interactor.enabled = false; // sin indicación ni E mientras se mira la mochila
                look.CursorFree = true;
                Bind(up, OnUp);
                Bind(down, OnDown);
                Bind(dropOne, OnDropOne);
                Bind(dropAll, OnDropAll);
                _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, backpack.Stacks.Count - 1));
            }
            else
            {
                Unbind(up, OnUp);
                Unbind(down, OnDown);
                Unbind(dropOne, OnDropOne);
                Unbind(dropAll, OnDropAll);
                motor.MovementLocked = _wasMovementLocked;
                interactor.enabled = true;
                look.CursorFree = false;
            }
        }

        private void OnUp(InputAction.CallbackContext _) => _selected = Mathf.Max(0, _selected - 1);
        private void OnDown(InputAction.CallbackContext _) => _selected = Mathf.Min(Mathf.Max(0, backpack.Stacks.Count - 1), _selected + 1);
        private void OnDropOne(InputAction.CallbackContext _) => Drop(_selected, all: false);
        private void OnDropAll(InputAction.CallbackContext _) => Drop(_selected, all: true);

        /// <summary>
        /// Suelta una unidad (o todas) del material de la fila indicada: salen de la mochila y aparecen como
        /// un bulto delante del jugador. Devuelve cuántas unidades soltó.
        /// </summary>
        public int Drop(int row, bool all)
        {
            if (row < 0 || row >= backpack.Stacks.Count) return 0;
            ItemStack stack = backpack.Stacks[row];
            int units = backpack.Remove(stack.Item, all ? stack.Count : 1);
            if (units <= 0) return 0;

            // Delante de los ojos, sin meterlo adentro de una pared: se acorta hasta lo que deja libre la vista.
            Vector3 forward = Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f) * Vector3.forward;
            float distance = settings.DropDistanceMeters;
            RaycastHit[] hits = Physics.RaycastAll(head.position, forward, distance + 0.3f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            foreach (RaycastHit hit in hits)
                if (!hit.collider.transform.IsChildOf(transform)) nearest = Mathf.Min(nearest, hit.distance); // el propio cuerpo no cuenta
            if (nearest < float.MaxValue) distance = Mathf.Max(0.2f, nearest - 0.3f);
            Vector3 position = head.position + forward * distance + Vector3.down * 0.5f;
            MaterialBundle.Spawn(settings, stack.Item, units, position, motor.Velocity);
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, backpack.Stacks.Count - 1));
            return units;
        }

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();

            const float width = 400f, rowHeight = 34f;
            int rows = Mathf.Max(1, backpack.Stacks.Count);
            float height = 66f + rows * rowHeight + 46f;
            var area = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUI.color = new Color(1f, 1f, 1f, 0.82f);
            GUI.DrawTexture(area, _plate);
            GUI.color = Color.white;

            // Título y peso: "MOCHILA        21,5 / 30 kg".
            float mass = backpack.MassKg, capacity = backpack.CapacityKg;
            float fill = Mathf.Clamp01(mass / Mathf.Max(0.01f, capacity));
            _label.normal.textColor = Color.white;
            GUI.Label(new Rect(area.x + 16f, area.y + 10f, 200f, 28f), "MOCHILA", _label);
            _label.alignment = TextAnchor.UpperRight;
            GUI.Label(new Rect(area.xMax - 216f, area.y + 10f, 200f, 28f), $"{mass:0.0} / {capacity:0} kg", _label);
            _label.alignment = TextAnchor.UpperLeft;

            // Barra de peso: blanca, ámbar desde el 80%, roja desde el 95%.
            var bar = new Rect(area.x + 16f, area.y + 44f, width - 32f, 8f);
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = fill >= 0.95f ? Danger : fill >= 0.8f ? Amber : Color.white;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fill, bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Filas: marca de color, nombre y cantidad, kg, y los botones de soltar.
            float y = area.y + 66f;
            if (backpack.Stacks.Count == 0)
            {
                _small.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
                GUI.Label(new Rect(area.x + 16f, y + 4f, width - 32f, rowHeight), "vacía", _small);
            }
            for (int i = 0; i < backpack.Stacks.Count; i++)
            {
                ItemStack stack = backpack.Stacks[i];
                var row = new Rect(area.x + 8f, y + i * rowHeight, width - 16f, rowHeight - 2f);
                if (i == _selected)
                {
                    GUI.color = new Color(Amber.r, Amber.g, Amber.b, 0.25f);
                    GUI.DrawTexture(row, Texture2D.whiteTexture);
                }
                GUI.color = stack.Item.Color;
                GUI.DrawTexture(new Rect(row.x + 6f, row.y + 8f, 14f, 14f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                _label.normal.textColor = i == _selected ? Amber : Color.white;
                GUI.Label(new Rect(row.x + 30f, row.y + 4f, 170f, 26f), $"{stack.Item.DisplayName} ×{stack.Count}", _label);
                _small.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
                GUI.Label(new Rect(row.x + 190f, row.y + 8f, 70f, 22f), $"{stack.MassKg:0.0} kg", _small);
                if (GUI.Button(new Rect(row.xMax - 128f, row.y + 3f, 58f, 26f), "soltar 1", _button)) { _selected = i; Drop(i, all: false); break; }
                if (GUI.Button(new Rect(row.xMax - 66f, row.y + 3f, 58f, 26f), "todo", _button)) { _selected = i; Drop(i, all: true); break; }
            }

            _small.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(area.x + 16f, area.yMax - 28f, width - 32f, 22f), "↑↓ elegir   Enter soltar 1   Supr soltar todo   Tab cerrar", _small);
        }

        private void EnsureStyles()
        {
            if (_label != null) return;
            _plate = new Texture2D(1, 1);
            _plate.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.1f));
            _plate.Apply();
            _label = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.UpperLeft };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.UpperLeft };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        }

        private static void Bind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            reference.action.performed += handler;
            reference.action.Enable();
        }

        private static void Unbind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            reference.action.performed -= handler;
            reference.action.Disable();
        }
    }
}
