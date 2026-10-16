using SurvivalEnTren.Crafting;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using SurvivalEnTren.Player;
using SurvivalEnTren.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// Menú de pausa (M13, "UI mínima"; sección 18.1: UI de sistema "solo al abrir"). Escape lo abre y lo cierra:
    /// Seguir, Guardar partida, Cargar partida, Controles (la lista de teclas) y Salir. Mientras está abierto el juego se
    /// detiene (es pausa de verdad, a diferencia de la mochila y el banco). Mouse o flechas + Enter.
    /// Sobrio: blanco/neutro y ámbar (sección 18.4).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);
        private const float W = 420f;
        private const float H = 480f;

        [SerializeField] private InputActionReference toggle;
        [SerializeField] private SaveSystem saver;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private InventoryPanel inventory;
        [SerializeField] private WorkbenchPanel workbench;

        private static readonly string[] Options = { "Seguir", "Guardar partida   (F5)", "Cargar partida   (F9)", "Controles", "Opciones", "Salir al menú", "Salir del juego" };
        private const string MenuScene = "Menu_Principal";
        private bool _open;
        private bool _help;
        private int _selected;
        private float _timeScaleBefore = 1f;
        private bool _wasMovementLocked;
        private GUIStyle _title, _item, _small, _value;
        private OptionsPanel _panel;
        private GameOptions _options;

        public bool IsOpen => _open;
        public bool ShowingHelp => _help;
        public bool ShowingOptions => _panel != null && _panel.IsOpen;

        private void OnEnable()
        {
            if (toggle != null) { toggle.action.performed += OnToggle; toggle.action.Enable(); }
        }

        private void OnDisable()
        {
            if (toggle != null) toggle.action.performed -= OnToggle;
            if (_open) Close();
        }

        private void OnToggle(InputAction.CallbackContext _)
        {
            if (!_open) Open();
            else if (_help) _help = false;
            else if (_panel.IsOpen) _panel.Close();
            else Close();
        }

        public void Open()
        {
            if (_open) return;
            if (inventory != null && inventory.IsOpen) inventory.Close();
            if (workbench != null && workbench.IsOpen) workbench.Close();
            _open = true;
            _help = false;
            _selected = 0;
            _timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _wasMovementLocked = motor.MovementLocked;
            motor.MovementLocked = true;
            interactor.enabled = false;
            if (carrier != null) carrier.ThrowLocked = true;
            look.CursorFree = true;
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _help = false;
            _panel.Close();
            Time.timeScale = _timeScaleBefore;
            motor.MovementLocked = _wasMovementLocked;
            interactor.enabled = true;
            if (carrier != null) carrier.ThrowLocked = false;
            look.CursorFree = false;
        }

        /// <summary>Elige una opción del menú (la usan el teclado, el mouse y las pruebas).</summary>
        public void Choose(int index)
        {
            switch (index)
            {
                case 0: Close(); break;
                case 1: saver.Save(); break;
                case 2:
                    Close(); // el tiempo vuelve a correr antes de recargar la escena
                    saver.Load();
                    break;
                case 3: _help = true; break;
                case 4: _panel.Open(_options, o => o.ApplyToGame(look)); break;
                case 5:
                    if (!Application.CanStreamedLevelBeLoaded(MenuScene)) break; // sin el menú en la lista de escenas (las pruebas)
                    Close();
                    UnityEngine.SceneManagement.SceneManager.LoadScene(MenuScene);
                    break;
                case 6:
                    Close();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        // ---------------------------------------------------------------- Diseño (espacio propio del menú)

        private static float Scale => Mathf.Min(1f, (Screen.width - 30f) / W, (Screen.height - 30f) / H);
        private static Vector2 Origin => new Vector2((Screen.width - W * Scale) * 0.5f, (Screen.height - H * Scale) * 0.5f);
        private static Rect OptionRect(int i) => new Rect(40f, 80f + i * 52f, W - 80f, 44f);

        private static Vector2 MenuMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return new Vector2(-1000f, -1000f);
            Vector2 p = mouse.position.ReadValue();
            return (new Vector2(p.x, Screen.height - p.y) - Origin) / Scale;
        }

        /// <summary>Dónde está el centro de una opción en coordenadas del mouse (lo usan las pruebas).</summary>
        public Vector2 OptionCenterScreen(int i)
        {
            Vector2 gui = Origin + OptionRect(i).center * Scale;
            return new Vector2(gui.x, Screen.height - gui.y);
        }

        private void Update()
        {
            if (!_open) return;
            if (_panel.IsOpen) { _panel.HandleInput(MenuMouse()); return; }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && !_help)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame) _selected = (_selected + Options.Length - 1) % Options.Length;
                if (keyboard.downArrowKey.wasPressedThisFrame) _selected = (_selected + 1) % Options.Length;
                if (keyboard.enterKey.wasPressedThisFrame) { Choose(_selected); return; }
            }
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (_help) { _help = false; return; }
                Vector2 pos = MenuMouse();
                for (int i = 0; i < Options.Length; i++)
                    if (OptionRect(i).Contains(pos)) { _selected = i; Choose(i); return; }
            }
        }

        // Dibuja con GUI.* (sin el maquetado automático de GUILayout): menos memoria nueva por cuadro (M13).
        private void Awake()
        {
            useGUILayout = false;
            _panel = new OptionsPanel(OptionRect);
        }

        // Las opciones del jugador (P3): sensibilidad, volumen y calidad, desde el archivo de opciones.
        private void Start()
        {
            _options = GameOptions.Load();
            _options.ApplyToGame(look);
        }

        private void OnGUI()
        {
            if (!_open) return;
            _title ??= new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            _item ??= new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.UpperLeft, wordWrap = true };
            GUI.depth = -10;

            // Oscurece la pantalla detrás del menú.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(Origin.x, Origin.y, 0f), Quaternion.identity, new Vector3(Scale, Scale, 1f));
            GUI.color = new Color(0.08f, 0.09f, 0.1f, 0.97f);
            GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);
            GUI.color = Color.white;
            _title.normal.textColor = Color.white;
            GUI.Label(new Rect(0f, 20f, W, 40f), _help ? "CONTROLES" : _panel.IsOpen ? "OPCIONES" : "PAUSA", _title);

            if (_panel.IsOpen)
            {
                _value ??= new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
                _panel.Draw(MenuMouse(), _item, _value);
            }
            else if (_help)
            {
                _small.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
                GUI.Label(new Rect(30f, 70f, W - 60f, H - 90f),
                    "WASD caminar · Shift correr · Espacio saltar · mouse mirar\n" +
                    "E usar (mantener E: acciones largas) · clic lanzar lo que llevás\n" +
                    "Tab mochila · Q sonda portátil · F linterna\n" +
                    "1 a 5 o la rueda: barreta, sierra, sonda, martillo, llave · clic golpear\n" +
                    "Kit de un mueble en la mano: clic instala, rueda gira · llave: moverlo\n" +
                    "En el pupitre (E): W/S potencia y freno, R/F reversor, E soltar\n" +
                    "F5 guardar · F9 cargar · Escape pausa\n" +
                    "F1 panel de pruebas · F2 acelerar el tiempo (pruebas)\n\n" +
                    "Escape o clic para volver", _small);
            }
            else
            {
                Vector2 mouse = MenuMouse();
                for (int i = 0; i < Options.Length; i++)
                {
                    Rect r = OptionRect(i);
                    bool selected = i == _selected || r.Contains(mouse);
                    GUI.color = new Color(1f, 1f, 1f, selected ? 0.12f : 0.04f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    _item.normal.textColor = selected ? Amber : Color.white;
                    GUI.Label(r, Options[i], _item);
                }
            }
            GUI.matrix = saved;
            GUI.color = Color.white;
        }
    }
}
