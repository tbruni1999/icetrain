using SurvivalEnTren.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// El menú principal (producción P3): Nueva partida, Continuar (si hay una partida guardada), Opciones y Salir, sobre una
    /// vista del tren en la nieve. Mouse o flechas + Enter. Sobrio: blanco/neutro y ámbar (sección 18.4).
    /// - Nueva partida: carga la escena de juego con el despertar de la sección 20.1 (WakeUpIntro).
    /// - Continuar: carga la partida guardada (como F9).
    /// - Con el argumento "-medicion" en la línea de comandos (la build), entra directo a la medición de rendimiento.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);
        private const float W = 440f;
        private const float H = 470f;

        [Tooltip("La escena del juego.")]
        [SerializeField] private string gameScene = "M2_Conduccion";
        [Tooltip("Opcional: una cámara que gira despacio alrededor de este punto (la vista de fondo).")]
        [SerializeField] private Transform orbitCenter;
        [SerializeField] private float orbitDegreesPerSecond = 2.5f;

        private static readonly string[] Items = { "Nueva partida", "Continuar", "Opciones", "Salir" };
        private int _selected;
        private bool _hasSave;
        private GameOptions _options;
        private OptionsPanel _panel;
        private GUIStyle _title, _subtitle, _item, _value;

        public bool OptionsOpen => _panel != null && _panel.IsOpen;
        public bool HasSave => _hasSave;

        private void Awake()
        {
            useGUILayout = false; // dibuja con GUI.* (sin GUILayout), como el resto de la interfaz (M13)
            _options = GameOptions.Load();
            _options.ApplyGlobal();
            _hasSave = SaveSystem.SaveExists();
            _panel = new OptionsPanel(OptionRect);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (Benchmark.RequestedByCommandLine()) { Benchmark.Requested = true; SceneManager.LoadScene(gameScene); }
        }

        /// <summary>Elige una opción (la usan el teclado, el mouse y las pruebas).</summary>
        public void Choose(int index)
        {
            switch (index)
            {
                case 0:
                    WakeUpIntro.PlayOnNextLoad = true;
                    SceneManager.LoadScene(gameScene);
                    break;
                case 1:
                    if (_hasSave) SaveSystem.LoadFromMenu(gameScene);
                    break;
                case 2:
                    _panel.Open(_options, o => o.ApplyGlobal());
                    break;
                case 3:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        // ---------------------------------------------------------------- Diseño: el panel a la izquierda, la vista a la derecha

        private static float Scale => Mathf.Min(1f, (Screen.width - 30f) / W, (Screen.height - 30f) / H);
        private static Vector2 Origin => new Vector2(Mathf.Max(15f, Screen.width * 0.08f), (Screen.height - H * Scale) * 0.5f);
        private static Rect OptionRect(int i) => new Rect(30f, 150f + i * 56f, W - 60f, 46f);

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
            if (orbitCenter != null && Camera.main != null)
                Camera.main.transform.RotateAround(orbitCenter.position, Vector3.up, orbitDegreesPerSecond * Time.deltaTime);

            Vector2 mouseInMenu = MenuMouse();
            if (_panel.IsOpen)
            {
                Keyboard k = Keyboard.current;
                if (k != null && k.escapeKey.wasPressedThisFrame) { _panel.Close(); return; }
                _panel.HandleInput(mouseInMenu);
                return;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame) _selected = Next(-1);
                if (keyboard.downArrowKey.wasPressedThisFrame) _selected = Next(+1);
                if (keyboard.enterKey.wasPressedThisFrame) { Choose(_selected); return; }
            }
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                for (int i = 0; i < Items.Length; i++)
                    if (OptionRect(i).Contains(mouseInMenu) && (i != 1 || _hasSave)) { _selected = i; Choose(i); return; }
        }

        private int Next(int dir)
        {
            int i = _selected;
            do i = (i + dir + Items.Length) % Items.Length; while (i == 1 && !_hasSave);
            return i;
        }

        private void OnGUI()
        {
            _title ??= new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _subtitle ??= new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
            _item ??= new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter };
            _value ??= new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter };

            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(Origin.x, Origin.y, 0f), Quaternion.identity, new Vector3(Scale, Scale, 1f));
            GUI.color = new Color(0.06f, 0.07f, 0.08f, 0.82f);
            GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);
            GUI.color = Color.white;
            _title.normal.textColor = Color.white;
            GUI.Label(new Rect(30f, 30f, W - 60f, 50f), "SURVIVAL EN TREN", _title);
            _subtitle.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(32f, 80f, W - 60f, 26f), _panel.IsOpen ? "Opciones" : "La Red Blanca, después del Enfriamiento", _subtitle);

            Vector2 mouse = MenuMouse();
            if (_panel.IsOpen) _panel.Draw(mouse, _item, _value);
            else
                for (int i = 0; i < Items.Length; i++)
                {
                    Rect r = OptionRect(i);
                    bool enabled = i != 1 || _hasSave;
                    bool selected = enabled && (i == _selected || r.Contains(mouse));
                    GUI.color = new Color(1f, 1f, 1f, selected ? 0.12f : 0.04f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    _item.normal.textColor = !enabled ? new Color(1f, 1f, 1f, 0.3f) : selected ? Amber : Color.white;
                    GUI.Label(r, Items[i], _item);
                }
            GUI.matrix = saved;
            GUI.color = Color.white;
        }
    }
}
