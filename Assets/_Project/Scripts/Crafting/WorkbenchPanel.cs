using System.Collections.Generic;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using SurvivalEnTren.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Crafting
{
    /// <summary>
    /// El panel del banco de trabajo (sección 18.1, tercera capa: UI de sistema "solo al abrir"). E sobre el banco lo abre.
    /// A la izquierda, la lista de planos que el jugador conoce; a la derecha, lo que pide el elegido: cada pieza que hace
    /// falta se ve SÓLIDA si la tenés en la mochila y TRANSLÚCIDA si falta (lo mismo que las obras del tren), las
    /// herramientas que pide (no se gastan) y qué sale. Enter (o el botón FABRICAR con el mouse) arranca la fabricación:
    /// una barra de progreso y, al terminar, se gastan los materiales y sale el resultado.
    /// - Mientras está abierto no se camina, no se usa E y el cursor queda libre; el juego sigue corriendo (no es pausa).
    /// - Cerrar el panel (Tab) mientras se fabrica cancela sin perder nada: los materiales se gastan recién al terminar.
    /// - Sobrio: blanco/neutro y ámbar (sección 18.4).
    /// </summary>
    public class WorkbenchPanel : MonoBehaviour
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);
        // Tamaño del panel en su propio espacio; se achica si la pantalla es chica.
        private const float W = 820f;
        private const float H = 480f;
        private const float Tile = 48f;

        [SerializeField] private Backpack backpack;
        [SerializeField] private PlayerGear gear;
        [SerializeField] private BlueprintBook book;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerCarrier carrier;
        [Tooltip("Tab abre la mochila: mientras el banco está abierto se apaga para que Tab solo lo cierre.")]
        [SerializeField] private InputActionReference toggleBackpack;
        [Header("Mapa \"Workbench\" (solo activo con el panel abierto)")]
        [SerializeField] private InputActionReference up;
        [SerializeField] private InputActionReference down;
        [SerializeField] private InputActionReference confirm;
        [SerializeField] private InputActionReference close;

        private bool _open;
        private bool _wasMovementLocked;
        private Workbench _bench;
        private List<BenchRecipe> _recipes = new List<BenchRecipe>();
        private int _selected;
        private bool _crafting;
        private float _progress;
        private string _message = "";
        private float _messageUntil;
        private bool _toggleDisabled;
        private int _toggleEnableFrame;
        private GUIStyle _title, _name, _small, _tip;

        public bool IsOpen => _open;
        public bool IsCrafting => _crafting;
        public int RecipeCount => _recipes.Count;
        public int SelectedIndex => _selected;
        public BenchRecipe Selected => _selected >= 0 && _selected < _recipes.Count ? _recipes[_selected] : null;
        /// <summary>Qué fracción de la fabricación en curso va hecha (0 a 1).</summary>
        public float Progress01 => _crafting && Selected != null ? Mathf.Clamp01(_progress / Selected.Blueprint.CraftSeconds) : 0f;
        /// <summary>El banco abierto (null si está cerrado).</summary>
        public Workbench Bench => _open ? _bench : null;
        public string LastMessage => _message;

        private void OnDisable()
        {
            if (_open) Close();
            EnableToggleNow();
        }

        // ---------------------------------------------------------------- Abrir y cerrar

        public void Open(Workbench bench)
        {
            if (_open) Close();
            _open = true;
            _bench = bench;
            _recipes = bench.Recipes(book);
            _selected = 0;
            _crafting = false;
            _progress = 0f;
            _message = "";
            _wasMovementLocked = motor.MovementLocked;
            motor.MovementLocked = true;
            interactor.enabled = false;
            if (carrier != null) carrier.ThrowLocked = true;
            look.CursorFree = true;
            if (toggleBackpack != null) { toggleBackpack.action.Disable(); _toggleDisabled = true; }
            Bind(up, OnUp); Bind(down, OnDown); Bind(confirm, OnConfirm); Bind(close, OnClose);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _crafting = false;
            _progress = 0f;
            Unbind(up, OnUp); Unbind(down, OnDown); Unbind(confirm, OnConfirm); Unbind(close, OnClose);
            motor.MovementLocked = _wasMovementLocked;
            interactor.enabled = true;
            if (carrier != null) carrier.ThrowLocked = false;
            look.CursorFree = false;
            // Tab cerró el banco: la mochila se vuelve a prender un par de cuadros después, para que esa misma pulsación no la abra.
            _toggleEnableFrame = Time.frameCount + 2;
        }

        private void EnableToggleNow()
        {
            if (!_toggleDisabled) return;
            _toggleDisabled = false;
            if (toggleBackpack != null) toggleBackpack.action.Enable();
        }

        // ---------------------------------------------------------------- Teclado

        private void OnUp(InputAction.CallbackContext _) => MoveSelection(-1);
        private void OnDown(InputAction.CallbackContext _) => MoveSelection(1);
        private void OnConfirm(InputAction.CallbackContext _) => StartCraft();
        private void OnClose(InputAction.CallbackContext _) => Close();

        public void MoveSelection(int delta)
        {
            if (_crafting || _recipes.Count == 0) return;
            _selected = Mathf.Clamp(_selected + delta, 0, _recipes.Count - 1);
        }

        public void Select(int index)
        {
            if (_crafting || index < 0 || index >= _recipes.Count) return;
            _selected = index;
        }

        // ---------------------------------------------------------------- Fabricar

        /// <summary>Qué impide fabricar el plano elegido ahora (None si se puede).</summary>
        public CraftBlock CurrentBlock => Selected == null ? CraftBlock.MissingMaterials : _bench.Check(Selected, backpack, gear);

        /// <summary>Arranca la fabricación del plano elegido; si no se puede, dice por qué. Devuelve si arrancó.</summary>
        public bool StartCraft()
        {
            if (!_open || _crafting || Selected == null) return false;
            CraftBlock block = CurrentBlock;
            if (block != CraftBlock.None)
            {
                Say(BlockText(Selected, block));
                return false;
            }
            // Mejoras (kits y muebles): el banco las arma solo, con su tiempo; el panel se puede cerrar.
            if (Selected.Timed)
            {
                BenchRecipe recipe = Selected;
                if (!_bench.StartJob(recipe, backpack, gear)) return false;
                Say($"el banco está armando: {recipe.KitName}   ·   {MinutesText(recipe.Minutes)} del juego (podés irte)");
                Refresh();
                return true;
            }
            _crafting = true;
            _progress = 0f;
            return true;
        }

        /// <summary>Cancela lo que el banco está armando (los materiales vuelven a la mochila). Devuelve si se canceló.</summary>
        public bool CancelJob()
        {
            if (!_open || !_bench.IsBusy) return false;
            string name = _bench.Job.KitName;
            if (!_bench.CancelJob(backpack))
            {
                Say("no entran los materiales en la mochila: hacé lugar para cancelar");
                return false;
            }
            Say($"cancelado: {name}   ·   los materiales volvieron a la mochila");
            Refresh();
            return true;
        }

        // Vuelve a pedir la lista (cambia al empezar un kit: aparece el de la fase siguiente) sin perder la línea elegida.
        private void Refresh()
        {
            string key = Selected?.Key;
            _recipes = _bench.Recipes(book);
            int index = key == null ? -1 : _recipes.FindIndex(r => r.Key == key);
            _selected = index >= 0 ? index : Mathf.Clamp(_selected, 0, Mathf.Max(0, _recipes.Count - 1));
        }

        private static string MinutesText(float minutes) =>
            minutes >= 60f ? $"{Mathf.FloorToInt(minutes / 60f)} h {Mathf.RoundToInt(minutes % 60f):00} min" : $"{Mathf.CeilToInt(minutes)} min";

        private void Update()
        {
            if (_toggleDisabled && !_open && Time.frameCount >= _toggleEnableFrame) EnableToggleNow();
            if (!_open) return;

            if (_crafting)
            {
                _progress += Time.unscaledDeltaTime;
                if (_progress >= Selected.Blueprint.CraftSeconds) Finish();
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame) CancelJob();
            HandleMouse(PanelMouse());
        }

        private void Finish()
        {
            Blueprint blueprint = Selected.Blueprint;
            _crafting = false;
            _progress = 0f;
            if (_bench.Complete(blueprint, backpack, gear)) Say($"fabricado: {blueprint.DisplayName}   ·   {blueprint.OutputText}");
            else Say($"no se pudo fabricar: {BlockText(Selected, _bench.Check(blueprint, backpack, gear))}");
        }

        private string BlockText(BenchRecipe recipe, CraftBlock block)
        {
            switch (block)
            {
                case CraftBlock.MissingMaterials: return "faltan " + _bench.MissingText(recipe, backpack, gear);
                case CraftBlock.MissingTool: return "falta " + _bench.MissingText(recipe, backpack, gear);
                case CraftBlock.AlreadyHave: return "ya la tenés";
                case CraftBlock.OutputBlocked: return "sacá lo que hay sobre la mesa";
                case CraftBlock.Busy: return "el banco está armando otra cosa";
                default: return "";
            }
        }

        private void Say(string text)
        {
            _message = text;
            _messageUntil = Time.unscaledTime + 4f;
        }

        // ---------------------------------------------------------------- Diseño (en el espacio propio del panel, 820 x 480)

        private static float Scale => Mathf.Min(1f, (Screen.width - 30f) / W, (Screen.height - 30f) / H);
        private static Vector2 Origin => new Vector2((Screen.width - W * Scale) * 0.5f, (Screen.height - H * Scale) * 0.5f);

        private static Rect RowRect(int i) => new Rect(20f, 64f + i * 46f, 260f, 42f);
        private static Rect ButtonRect => new Rect(316f, 392f, 240f, 46f);
        // Lo que el banco está armando (arriba a la derecha) y su botón para cancelar.
        private static Rect JobRect => new Rect(316f, 16f, 380f, 30f);
        private static Rect CancelRect => new Rect(704f, 16f, 96f, 30f);

        // El mouse en el espacio del panel (origen arriba a la izquierda del panel).
        private static Vector2 PanelMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return new Vector2(-1000f, -1000f);
            Vector2 p = mouse.position.ReadValue();
            Vector2 gui = new Vector2(p.x, Screen.height - p.y);
            return (gui - Origin) / Scale;
        }

        // Del espacio del panel a coordenadas del mouse (origen abajo a la izquierda): lo usan las pruebas.
        private static Vector2 ToMouseScreen(Vector2 panelPoint)
        {
            Vector2 gui = Origin + panelPoint * Scale;
            return new Vector2(gui.x, Screen.height - gui.y);
        }

        public Vector2 RowCenterScreen(int index) => ToMouseScreen(RowRect(index).center);
        public Vector2 CraftButtonCenterScreen() => ToMouseScreen(ButtonRect.center);
        public Vector2 CancelButtonCenterScreen() => ToMouseScreen(CancelRect.center);

        private void HandleMouse(Vector2 pos)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            for (int i = 0; i < _recipes.Count; i++)
                if (RowRect(i).Contains(pos)) { Select(i); return; }
            if (ButtonRect.Contains(pos)) StartCraft();
            if (_bench.IsBusy && CancelRect.Contains(pos)) CancelJob();
        }

        // ---------------------------------------------------------------- Dibujo

        // Dibuja con GUI.* (sin el maquetado automático de GUILayout): menos memoria nueva por cuadro (M13).
        private void Awake() => useGUILayout = false;

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();
            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(Origin.x, Origin.y, 0f), Quaternion.identity, new Vector3(Scale, Scale, 1f));
            Vector2 mouse = PanelMouse();

            GUI.color = new Color(0.08f, 0.09f, 0.1f, 0.97f);
            GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);
            ItemTile.Outline(new Rect(0f, 0f, W, H), new Color(1f, 1f, 1f, 0.18f), 1.5f);

            _title.normal.textColor = Color.white;
            GUI.Label(new Rect(20f, 14f, 400f, 30f), _bench.DisplayName.ToUpperInvariant(), _title);
            if (_bench.IsBusy) DrawJob(mouse);
            else
            {
                _small.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
                GUI.Label(new Rect(W - 330f, 20f, 310f, 20f), "usa lo que llevás y lo que hay en el tren", _small);
            }

            DrawRecipeList(mouse);
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            GUI.DrawTexture(new Rect(296f, 64f, 1f, 370f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (Selected != null) DrawDetails(mouse);
            else
            {
                _tip.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
                GUI.Label(new Rect(316f, 70f, 480f, 60f), "No conocés ningún plano para fabricar acá todavía.", _tip);
            }
            DrawFooter();
            GUI.matrix = saved;
        }

        // Lo que el banco está armando: una barra con lo que falta y el botón para cancelar.
        private void DrawJob(Vector2 mouse)
        {
            Rect r = JobRect;
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(Amber.r, Amber.g, Amber.b, 0.45f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * _bench.JobProgress01, r.height), Texture2D.whiteTexture);
            ItemTile.Outline(r, Amber, 1.5f);
            GUI.color = Color.white;
            _small.normal.textColor = Color.white;
            string left = _bench.WaitingForSpace ? "listo: falta lugar al lado del banco" : MinutesText(_bench.JobMinutesLeft);
            GUI.Label(new Rect(r.x + 8f, r.y + 7f, r.width - 16f, 18f), $"armando: {_bench.Job.KitName}   ·   {left}", _small);

            Rect c = CancelRect;
            GUI.color = new Color(1f, 1f, 1f, c.Contains(mouse) ? 0.16f : 0.06f);
            GUI.DrawTexture(c, Texture2D.whiteTexture);
            ItemTile.Outline(c, new Color(1f, 1f, 1f, 0.4f), 1.5f);
            GUI.color = Color.white;
            var centered = new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter };
            centered.normal.textColor = Color.white;
            GUI.Label(c, "CANCELAR", centered);
        }

        private void DrawRecipeList(Vector2 mouse)
        {
            for (int i = 0; i < _recipes.Count; i++)
            {
                Rect r = RowRect(i);
                BenchRecipe recipe = _recipes[i];
                bool selected = i == _selected;
                GUI.color = new Color(1f, 1f, 1f, selected ? 0.12f : (r.Contains(mouse) && !_crafting ? 0.08f : 0.04f));
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                if (selected) ItemTile.Outline(r, Amber, 2f);
                GUI.color = Color.white;

                _name.normal.textColor = Color.white;
                GUI.Label(new Rect(r.x + 10f, r.y + 2f, r.width - 20f, 22f), recipe.DisplayName, _name);
                CraftBlock block = _bench.Check(recipe, backpack, gear);
                _small.normal.textColor = block == CraftBlock.None ? Amber : new Color(1f, 1f, 1f, 0.5f);
                string status = block == CraftBlock.None ? "podés fabricarla"
                    : block == CraftBlock.MissingMaterials ? "faltan materiales"
                    : block == CraftBlock.MissingTool ? "falta una herramienta"
                    : block == CraftBlock.AlreadyHave ? "ya la tenés"
                    : block == CraftBlock.Busy ? "el banco está ocupado"
                    : "la mesa está ocupada";
                if (recipe.Timed) status += "   ·   " + MinutesText(recipe.Minutes);
                GUI.Label(new Rect(r.x + 10f, r.y + 23f, r.width - 20f, 18f), status, _small);
            }
        }

        private void DrawDetails(Vector2 mouse)
        {
            BenchRecipe blueprint = Selected;
            _name.normal.textColor = Color.white;
            GUI.Label(new Rect(316f, 62f, 480f, 26f), blueprint.DisplayName, _name);
            _small.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(316f, 90f, 480f, 44f), blueprint.Description, _small);

            // Materiales: una pieza por unidad que pide la receta; sólida si ya la tenés, translúcida si falta.
            Label("MATERIALES", 316f, 140f);
            float x = 316f;
            foreach (CraftIngredient ingredient in blueprint.Ingredients)
            {
                int have = _bench.Available(ingredient.item, backpack);
                for (int i = 0; i < ingredient.count; i++)
                    ItemTile.Draw(new Rect(x + i * (Tile + 6f), 160f, Tile, Tile), ingredient.item, have > i ? 1f : 0.28f, _small);
                float groupWidth = ingredient.count * (Tile + 6f);
                _small.normal.textColor = have >= ingredient.count ? new Color(1f, 1f, 1f, 0.75f) : Amber;
                GUI.Label(new Rect(x, 212f, Mathf.Max(groupWidth, 110f), 18f), $"{ingredient.item.PieceName} {Mathf.Min(have, ingredient.count)}/{ingredient.count}", _small);
                x += Mathf.Max(groupWidth, 110f) + 12f;
            }

            // Herramientas: hay que tenerlas, no se gastan.
            Label("HERRAMIENTAS (no se gastan)", 316f, 244f);
            if (blueprint.RequiredTools.Length == 0)
            {
                _small.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
                GUI.Label(new Rect(316f, 266f, 300f, 20f), "ninguna", _small);
            }
            float tx = 316f;
            foreach (GearId tool in blueprint.RequiredTools)
            {
                bool owned = gear.Has(tool);
                var r = new Rect(tx, 264f, 150f, 34f);
                GUI.color = new Color(1f, 1f, 1f, owned ? 0.14f : 0.05f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                ItemTile.Outline(r, new Color(1f, 1f, 1f, owned ? 0.85f : 0.25f), 1.5f);
                _small.normal.textColor = new Color(1f, 1f, 1f, owned ? 1f : 0.4f);
                GUI.Label(new Rect(r.x + 8f, r.y + 8f, r.width - 16f, 20f), Blueprint.ToolName(tool), _small);
                tx += 160f;
            }

            Label("SALE", 316f, 316f);
            _tip.normal.textColor = Color.white;
            string time = blueprint.Timed ? $"tarda {MinutesText(blueprint.Minutes)} del juego (el banco sigue solo)" : $"tarda {blueprint.Blueprint.CraftSeconds:0.#} s";
            GUI.Label(new Rect(316f, 334f, 480f, 24f), blueprint.OutputText, _tip);
            _small.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(316f, 358f, 480f, 20f), time, _small);

            // El botón (o la barra mientras se fabrica).
            if (_crafting) DrawProgressBar(blueprint);
            else DrawCraftButton(blueprint, mouse);
        }

        private void DrawCraftButton(BenchRecipe blueprint, Vector2 mouse)
        {
            CraftBlock block = _bench.Check(blueprint, backpack, gear);
            bool can = block == CraftBlock.None;
            Rect r = ButtonRect;
            GUI.color = can ? new Color(Amber.r, Amber.g, Amber.b, r.Contains(mouse) ? 0.38f : 0.2f) : new Color(1f, 1f, 1f, 0.05f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            ItemTile.Outline(r, can ? Amber : new Color(1f, 1f, 1f, 0.2f), 2f);
            var centered = new GUIStyle(_name) { alignment = TextAnchor.MiddleCenter };
            centered.normal.textColor = can ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            GUI.Label(r, blueprint.Timed ? "ARMAR   (Enter)" : "FABRICAR   (Enter)", centered);

            if (!can)
            {
                _small.normal.textColor = Amber;
                GUI.Label(new Rect(r.xMax + 14f, r.y + 2f, 240f, 44f), BlockText(blueprint, block), _small);
            }
        }

        private void DrawProgressBar(BenchRecipe recipe)
        {
            var r = new Rect(316f, 392f, 484f, 46f);
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(Amber.r, Amber.g, Amber.b, 0.55f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Progress01, r.height), Texture2D.whiteTexture);
            ItemTile.Outline(r, Amber, 2f);
            var centered = new GUIStyle(_name) { alignment = TextAnchor.MiddleCenter };
            centered.normal.textColor = Color.white;
            float left = Mathf.Max(0f, recipe.Blueprint.CraftSeconds - _progress);
            GUI.Label(r, $"fabricando…  {left:0.#} s      (Tab cancela)", centered);
        }

        private void DrawFooter()
        {
            if (Time.unscaledTime < _messageUntil)
            {
                _tip.normal.textColor = Amber;
                GUI.Label(new Rect(20f, 440f, W - 40f, 24f), _message, _tip);
            }
            _small.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(20f, 456f, W - 40f, 20f), _bench.IsBusy ? "mouse: clic en un plano y en FABRICAR   ·   teclado: ↑ ↓ elegir, Enter fabricar, Retroceso cancela lo del banco   ·   Tab cerrar" : "mouse: clic en un plano y en FABRICAR   ·   teclado: ↑ ↓ elegir, Enter fabricar   ·   Tab cerrar", _small);
        }

        private void Label(string text, float x, float y)
        {
            _small.normal.textColor = new Color(1f, 1f, 1f, 0.45f);
            GUI.Label(new Rect(x, y, 400f, 18f), text, _small);
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperLeft };
            _name = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.UpperLeft };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.UpperLeft, wordWrap = true };
            _tip = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.UpperLeft };
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
