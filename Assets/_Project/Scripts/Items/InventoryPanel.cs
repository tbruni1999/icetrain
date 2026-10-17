using SurvivalEnTren.Interaction;
using SurvivalEnTren.Player;
using SurvivalEnTren.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// El panel del inventario (sección 18.1, tercera capa: "UI de sistema: inventario ... solo al abrir").
    /// DECISIÓN de Tomi: cuadrícula estilo Tarkov. Tab abre la mochila; usar un mueble con inventario (un estante) abre
    /// además la cuadrícula del mueble al lado. Cada pieza se ve con su imagen (provisoria) ocupando sus casillas, así se
    /// ve cuánto ocupa cada una y que lo largo no entra en lo chico.
    /// - Mouse: arrastrar una pieza a otro lugar (de la mochila al mueble y al revés), R para girarla mientras se arrastra,
    ///   y soltarla afuera del panel la deja en el piso (solo desde la mochila). Verde: entra; rojo: no entra.
    /// - Teclado: flechas mueven el cursor entre casillas (de una cuadrícula a la otra), R gira la pieza, Enter la pasa al
    ///   otro lado (o la suelta en el piso si solo está la mochila), Supr pasa/suelta todas las piezas del mismo material.
    /// - Mientras está abierto no se camina, no se usa E y el cursor queda libre; el juego sigue corriendo (no es pausa).
    /// - Sobrio: blanco/neutro y ámbar (sección 18.4).
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);
        private static readonly Color Good = new Color(0.45f, 0.9f, 0.5f, 0.45f);
        private static readonly Color Bad = new Color(0.9f, 0.3f, 0.25f, 0.5f);
        private const float Cell = 44f;
        private const float Pad = 16f;

        [SerializeField] private Backpack backpack;
        [SerializeField] private BackpackSettings settings;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private Transform head;
        [SerializeField] private InputActionReference toggle;
        [Header("Mapa \"Backpack\" (solo activo con el panel abierto)")]
        [SerializeField] private InputActionReference up;
        [SerializeField] private InputActionReference down;
        [SerializeField] private InputActionReference left;
        [SerializeField] private InputActionReference right;
        [SerializeField] private InputActionReference rotate;
        [SerializeField] private InputActionReference act;
        [SerializeField] private InputActionReference actAll;

        private sealed class GridView
        {
            public ItemGrid Grid;
            public string Title;
            public bool IsPlayerPack;
            public Rect Origin; // esquina de la casilla (0,0) y tamaño total, en pantalla
        }

        private bool _open;
        private bool _wasMovementLocked;
        private GridView _pack, _other;
        private int _cursorView, _cursorX, _cursorY;
        private string _message = "";
        private float _messageUntil;
        private GridItem _dragItem;
        private GridView _dragFrom;
        private bool _dragRotated;
        private bool _carryMode;
        private float _pressTime;
        private Vector2 _pressPos;
        private Rect _area;
        private GUIStyle _label, _small, _tip;
        private Texture2D _plate;

        public bool IsOpen => _open;
        public bool HasOtherGrid => _other != null;
        public int CursorX => _cursorX;
        public int CursorY => _cursorY;
        public int CursorGrid => _cursorView;

        /// <summary>La pieza bajo el cursor del teclado, o null.</summary>
        public GridItem SelectedItem => View(_cursorView)?.Grid.ItemAt(_cursorX, _cursorY);

        private void OnEnable()
        {
            toggle.action.performed += OnToggle;
            toggle.action.Enable();
        }

        private void OnDisable()
        {
            toggle.action.performed -= OnToggle;
            if (_open) Close();
        }

        private void OnToggle(InputAction.CallbackContext _)
        {
            if (_open) Close();
            else Open();
        }

        /// <summary>Abre la mochila; con "other", también la cuadrícula de un mueble al lado.</summary>
        public void Open(ItemGrid other = null, string otherTitle = null)
        {
            if (_open) Close();
            _open = true;
            _wasMovementLocked = motor.MovementLocked;
            motor.MovementLocked = true;
            interactor.enabled = false; // sin indicación ni E mientras se mira el inventario
            if (carrier != null) carrier.ThrowLocked = true; // el clic izquierdo es del panel, no lanza lo que se lleva
            look.CursorFree = true;
            _pack = new GridView { Grid = backpack.Grid, Title = "MOCHILA", IsPlayerPack = true };
            _other = other != null ? new GridView { Grid = other, Title = otherTitle ?? "MUEBLE" } : null;
            _cursorView = 0;
            _cursorX = _cursorY = 0;
            _dragItem = null;
            Bind(up, OnUp); Bind(down, OnDown); Bind(left, OnLeft); Bind(right, OnRight);
            Bind(rotate, OnRotate); Bind(act, OnAct); Bind(actAll, OnActAll);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _dragItem = null;
            Unbind(up, OnUp); Unbind(down, OnDown); Unbind(left, OnLeft); Unbind(right, OnRight);
            Unbind(rotate, OnRotate); Unbind(act, OnAct); Unbind(actAll, OnActAll);
            motor.MovementLocked = _wasMovementLocked;
            interactor.enabled = true;
            if (carrier != null) carrier.ThrowLocked = false;
            look.CursorFree = false;
        }

        private GridView View(int index) => index == 0 ? _pack : _other;

        // ---------------------------------------------------------------- Teclado

        private void OnUp(InputAction.CallbackContext _) => MoveCursor(0, -1);
        private void OnDown(InputAction.CallbackContext _) => MoveCursor(0, 1);
        private void OnLeft(InputAction.CallbackContext _) => MoveCursor(-1, 0);
        private void OnRight(InputAction.CallbackContext _) => MoveCursor(1, 0);

        /// <summary>Mueve el cursor una casilla; al salir por el costado pasa a la otra cuadrícula (si hay).</summary>
        public void MoveCursor(int dx, int dy)
        {
            GridView current = View(_cursorView);
            int x = _cursorX + dx, y = Mathf.Clamp(_cursorY + dy, 0, current.Grid.Height - 1);
            if (x < 0 && _cursorView == 1)
            {
                _cursorView = 0;
                _cursorX = _pack.Grid.Width - 1;
                _cursorY = Mathf.Min(y, _pack.Grid.Height - 1);
            }
            else if (x >= current.Grid.Width && _cursorView == 0 && _other != null)
            {
                _cursorView = 1;
                _cursorX = 0;
                _cursorY = Mathf.Min(y, _other.Grid.Height - 1);
            }
            else
            {
                _cursorX = Mathf.Clamp(x, 0, current.Grid.Width - 1);
                _cursorY = y;
            }
        }

        private void OnRotate(InputAction.CallbackContext _)
        {
            if (_dragItem != null)
            {
                if (_dragItem.Item.CanRotate) _dragRotated = !_dragRotated;
                return;
            }
            GridItem piece = SelectedItem;
            if (piece == null) return;
            if (!piece.Item.CanRotate) Say($"el {piece.Item.PieceName} no se puede girar");
            else if (!View(_cursorView).Grid.TryRotate(piece)) Say("no entra girado: no hay lugar");
        }

        private void OnAct(InputAction.CallbackContext _) => ActOnSelected(all: false);
        private void OnActAll(InputAction.CallbackContext _) => ActOnSelected(all: true);

        private void ActOnSelected(bool all)
        {
            GridView view = View(_cursorView);
            GridItem piece = SelectedItem;
            if (piece == null) return;
            ItemDefinition item = piece.Item;

            if (_other == null)
            {
                // Solo la mochila: soltar la pieza (o todas las del mismo material) en el piso.
                if (all) DropAllOfMaterial(view, item);
                else DropToWorld(piece, view);
            }
            else
            {
                // Con un mueble abierto: pasar al otro lado (lo que no entra se queda donde estaba).
                GridView target = view == _pack ? _other : _pack;
                int moved = 0;
                foreach (GridItem candidate in new System.Collections.Generic.List<GridItem>(view.Grid.Items))
                {
                    if (candidate.Item != item || (!all && candidate != piece)) continue;
                    if (!TransferAuto(candidate, view, target)) break;
                    moved++;
                }
                if (moved == 0) Say($"no entra en {target.Title.ToLowerInvariant()}: falta lugar");
            }
            ClampCursor();
        }

        private void ClampCursor()
        {
            GridView v = View(_cursorView) ?? _pack;
            _cursorX = Mathf.Clamp(_cursorX, 0, v.Grid.Width - 1);
            _cursorY = Mathf.Clamp(_cursorY, 0, v.Grid.Height - 1);
        }

        // ---------------------------------------------------------------- Mover piezas (lo usan el mouse, el teclado y las pruebas)

        /// <summary>Pasa una pieza de una cuadrícula a otra en la primera posición libre. Devuelve si pudo.</summary>
        private bool TransferAuto(GridItem piece, GridView from, GridView to)
        {
            if (!to.Grid.FindFreeSpot(piece.Item, out int x, out int y, out bool rotated)) return false;
            if (!from.Grid.Remove(piece)) return false;
            to.Grid.TryPlace(piece.Item, x, y, rotated);
            return true;
        }

        /// <summary>
        /// Mueve una pieza a una posición exacta (y la gira si se pide), en la misma cuadrícula o en la otra del panel.
        /// Devuelve si pudo; si no entra, no cambia nada.
        /// </summary>
        public bool TryMovePiece(GridItem piece, ItemGrid from, ItemGrid to, int x, int y, bool rotated)
        {
            if (from == to) return from.TryMove(piece, x, y, rotated);
            if (!to.CanPlace(piece.Item, x, y, rotated)) return false;
            if (!from.Remove(piece)) return false;
            to.TryPlace(piece.Item, x, y, rotated);
            return true;
        }

        private void DropToWorld(GridItem piece, GridView from)
        {
            if (!from.IsPlayerPack) { Say("del mueble no se tira al piso"); return; }
            ItemDefinition item = piece.Item;
            if (!from.Grid.Remove(piece)) return;
            SpawnBundle(item, 1);
        }

        /// <summary>Suelta una pieza de la mochila al piso (un bulto de 1). Devuelve si pudo.</summary>
        public bool DropPiece(GridItem piece)
        {
            if (!System.Linq.Enumerable.Contains(backpack.Grid.Items, piece)) return false;
            DropToWorld(piece, _pack ?? new GridView { Grid = backpack.Grid, IsPlayerPack = true });
            return true;
        }

        private int DropAllOfMaterial(GridView from, ItemDefinition item)
        {
            if (!from.IsPlayerPack) { Say("del mueble no se tira al piso"); return 0; }
            int units = 0;
            foreach (GridItem candidate in new System.Collections.Generic.List<GridItem>(from.Grid.Items))
            {
                if (candidate.Item != item) continue;
                from.Grid.Remove(candidate);
                units++;
            }
            if (units > 0) SpawnBundle(item, units);
            return units;
        }

        /// <summary>Suelta todas las piezas de un material de la mochila en un solo bulto. Devuelve cuántas.</summary>
        public int DropAll(ItemDefinition item) =>
            DropAllOfMaterial(_pack ?? new GridView { Grid = backpack.Grid, IsPlayerPack = true }, item);

        private void SpawnBundle(ItemDefinition item, int units)
        {
            // Delante de los ojos, sin meterlo adentro de una pared: se acorta hasta lo que deja libre la vista.
            Vector3 forward = Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f) * Vector3.forward;
            float distance = settings.DropDistanceMeters;
            RaycastHit[] hits = Physics.RaycastAll(head.position, forward, distance + 0.3f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            foreach (RaycastHit hit in hits)
                if (!hit.collider.transform.IsChildOf(transform)) nearest = Mathf.Min(nearest, hit.distance); // el propio cuerpo no cuenta
            if (nearest < float.MaxValue) distance = Mathf.Max(0.2f, nearest - 0.3f);
            Vector3 position = head.position + forward * distance + Vector3.down * 0.5f;
            MaterialBundle.Spawn(settings, item, units, position, motor.Velocity);
        }

        private void Say(string text)
        {
            _message = text;
            _messageUntil = Time.unscaledTime + 2.5f;
        }

        // ---------------------------------------------------------------- Pantalla

        // Dibuja con GUI.* (sin el maquetado automático de GUILayout): menos memoria nueva por cuadro (M13).
        private void Awake() => useGUILayout = false;

        private void OnGUI()
        {
            Matrix4x4 uiOld = UiScale.Begin(); // la interfaz en puntos, agrandada según la pantalla
            DrawGui();
            GUI.matrix = uiOld;
        }

        private void DrawGui()
        {
            if (!_open) return;
            EnsureStyles();
            Layout(out Rect area);
            Vector2 mouse = MouseGui();

            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            GUI.DrawTexture(area, _plate);
            GUI.color = Color.white;

            DrawView(_pack);
            if (_other != null) DrawView(_other);

            DrawDrag(mouse);
            DrawFooter(area, mouse);
        }

        private void Layout(out Rect area)
        {
            float w1 = _pack.Grid.Width * Cell;
            float h = Mathf.Max(_pack.Grid.Height, _other != null ? _other.Grid.Height : 0) * Cell;
            float width = Pad + w1 + Pad + (_other != null ? 40f + _other.Grid.Width * Cell + Pad : 0f);
            width = Mathf.Max(width, 380f);
            float height = 66f + h + 94f;
            area = new Rect((UiScale.Width - width) * 0.5f, (UiScale.Height - height) * 0.5f, width, height);
            _pack.Origin = new Rect(area.x + Pad, area.y + 66f, w1, _pack.Grid.Height * Cell);
            if (_other != null)
                _other.Origin = new Rect(_pack.Origin.xMax + 40f, area.y + 66f, _other.Grid.Width * Cell, _other.Grid.Height * Cell);
        }

        private void DrawView(GridView view)
        {
            Grid(view);
            ItemGrid grid = view.Grid;

            // Título y datos.
            _label.normal.textColor = Color.white;
            GUI.Label(new Rect(view.Origin.x, view.Origin.y - 46f, 320f, 28f), view.Title, _label);
            string data = view.IsPlayerPack
                ? $"{grid.UsedCells}/{grid.CellCount} casillas · {grid.MassKg:0.0} kg"
                : $"{grid.UsedCells}/{grid.CellCount} casillas";
            _small.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(view.Origin.x, view.Origin.y - 22f, 320f, 20f), data, _small);

            // Piezas.
            foreach (GridItem piece in grid.Items)
            {
                if (piece == _dragItem) continue; // la que se arrastra se dibuja con el mouse
                Rect r = PieceRect(view, piece.X, piece.Y, piece.Size);
                DrawPiece(r, piece.Item, piece.Rotated, 1f);
            }

            // Cursor del teclado.
            if (View(_cursorView) == view)
            {
                GridItem under = grid.ItemAt(_cursorX, _cursorY);
                Rect r = under != null ? PieceRect(view, under.X, under.Y, under.Size)
                                       : new Rect(view.Origin.x + _cursorX * Cell, view.Origin.y + _cursorY * Cell, Cell, Cell);
                Outline(r, Amber, 2f);
            }
        }

        private void Grid(GridView view)
        {
            for (int y = 0; y < view.Grid.Height; y++)
                for (int x = 0; x < view.Grid.Width; x++)
                {
                    var r = new Rect(view.Origin.x + x * Cell, view.Origin.y + y * Cell, Cell, Cell);
                    GUI.color = new Color(1f, 1f, 1f, 0.07f);
                    GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), Texture2D.whiteTexture);
                }
            GUI.color = Color.white;
        }

        private static Rect PieceRect(GridView view, int x, int y, Vector2Int size) =>
            new Rect(view.Origin.x + x * Cell + 1f, view.Origin.y + y * Cell + 1f, size.x * Cell - 2f, size.y * Cell - 2f);

        // Una pieza: fondo del color del material, su imagen (girada si hace falta) y, si no hay imagen, el nombre.
        private void DrawPiece(Rect r, ItemDefinition item, bool rotated, float alpha)
        {
            GUI.color = new Color(item.Color.r * 0.35f, item.Color.g * 0.35f, item.Color.b * 0.35f, 0.85f * alpha);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            Outline(r, new Color(item.Color.r, item.Color.g, item.Color.b, 0.9f * alpha), 1.5f);

            Sprite icon = item.Icon;
            if (icon != null)
            {
                var inner = new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                if (!rotated)
                {
                    GUI.DrawTexture(inner, icon.texture, ScaleMode.StretchToFill, true);
                }
                else
                {
                    // El dibujo es de ancho x alto sin girar: se dibuja con esas medidas y se gira 90° dentro del rectángulo.
                    Matrix4x4 saved = GUI.matrix;
                    Vector2 center = inner.center;
                    GUI.matrix = saved * Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, 90f), Vector3.one) * Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);
                    var turned = new Rect(center.x - inner.height * 0.5f, center.y - inner.width * 0.5f, inner.height, inner.width);
                    GUI.DrawTexture(turned, icon.texture, ScaleMode.StretchToFill, true);
                    GUI.matrix = saved;
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(r, item.PieceName, _small);
            }
            GUI.color = Color.white;
        }

        private static void Outline(Rect r, Color c, float t)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // ---------------------------------------------------------------- Mouse (por el Input System, no por la interfaz inmediata)

        // El mouse en coordenadas de pantalla de dibujo (origen arriba a la izquierda).
        private static Vector2 MouseGui()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return new Vector2(-1000f, -1000f);
            Vector2 p = mouse.position.ReadValue();
            return UiScale.ScreenToGui(p);
        }

        /// <summary>Dónde está el centro de una casilla en coordenadas del mouse (origen abajo a la izquierda). Lo usan las pruebas.</summary>
        public Vector2 CellCenterScreen(int gridIndex, int cx, int cy)
        {
            Layout(out _area);
            GridView view = View(gridIndex);
            return UiScale.GuiToScreen(new Vector2(view.Origin.x + (cx + 0.5f) * Cell, view.Origin.y + (cy + 0.5f) * Cell));
        }

        /// <summary>Hay una pieza agarrada con el mouse (arrastrándola o "en la mano" después de un clic).</summary>
        public bool IsDragging => _dragItem != null;
        public bool IsCarryingByClick => _dragItem != null && _carryMode;

        private void Update()
        {
            if (!_open) return;
            Layout(out _area);
            HandleMouse(MouseGui());
        }

        // Cómo se maneja, sin Enter: se agarra una pieza con el clic izquierdo, se arrastra y se suelta donde da verde. También
        // se puede hacer con dos clics (un clic la agarra, otro la deja). El clic derecho la devuelve a su lugar.
        private void HandleMouse(Vector2 pos)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (_dragItem != null && mouse.rightButton.wasPressedThisFrame)
            {
                EndDrag(); // cancelar: vuelve a su lugar
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (_dragItem == null) TryStartDrag(pos);
                else if (_carryMode) TryDropDragged(pos, keepOnFail: true);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _dragItem != null && !_carryMode)
            {
                bool clickWithoutMoving = Time.unscaledTime - _pressTime < 0.25f && (pos - _pressPos).sqrMagnitude < 25f;
                if (clickWithoutMoving) _carryMode = true; // un clic suelto: la pieza queda "en la mano" hasta el próximo clic
                else TryDropDragged(pos, keepOnFail: false);
            }
        }

        // La casilla bajo el mouse en una cuadrícula; false si el mouse no está sobre ella.
        private static bool CellUnder(GridView view, Vector2 mouse, out int cx, out int cy)
        {
            cx = Mathf.FloorToInt((mouse.x - view.Origin.x) / Cell);
            cy = Mathf.FloorToInt((mouse.y - view.Origin.y) / Cell);
            return view.Origin.Contains(mouse);
        }

        private void TryStartDrag(Vector2 pos)
        {
            foreach (GridView view in new[] { _pack, _other })
            {
                if (view == null || !CellUnder(view, pos, out int cx, out int cy)) continue;
                GridItem piece = view.Grid.ItemAt(cx, cy);
                _cursorView = view == _pack ? 0 : 1;
                _cursorX = cx;
                _cursorY = cy;
                if (piece == null) return;
                _dragItem = piece;
                _dragFrom = view;
                _dragRotated = piece.Rotated;
                _carryMode = false;
                _pressTime = Time.unscaledTime;
                _pressPos = pos;
                return;
            }
        }

        private void EndDrag()
        {
            _dragItem = null;
            _carryMode = false;
        }

        // Dónde quedaría la esquina de la pieza arrastrada si se suelta en el mouse: centrada en la casilla de abajo.
        private bool DragTarget(Vector2 mouse, out GridView view, out int x, out int y, out Vector2Int size)
        {
            size = _dragRotated ? new Vector2Int(_dragItem.Item.SizeCells.y, _dragItem.Item.SizeCells.x) : _dragItem.Item.SizeCells;
            foreach (GridView candidate in new[] { _pack, _other })
            {
                if (candidate == null || !candidate.Origin.Contains(mouse)) continue;
                int cx = Mathf.FloorToInt((mouse.x - candidate.Origin.x) / Cell);
                int cy = Mathf.FloorToInt((mouse.y - candidate.Origin.y) / Cell);
                view = candidate;
                x = Mathf.Clamp(cx - size.x / 2, 0, Mathf.Max(0, candidate.Grid.Width - size.x));
                y = Mathf.Clamp(cy - size.y / 2, 0, Mathf.Max(0, candidate.Grid.Height - size.y));
                return true;
            }
            view = null; x = y = 0;
            return false;
        }

        // Suelta la pieza agarrada donde está el mouse: si entra, queda ahí; si no, vuelve a su lugar (o sigue en la mano si
        // se agarró con un clic). Afuera del panel, desde la mochila, queda en el piso.
        private bool TryDropDragged(Vector2 pos, bool keepOnFail)
        {
            GridItem piece = _dragItem;
            GridView from = _dragFrom;
            bool rotated = _dragRotated;
            if (DragTarget(pos, out GridView target, out int x, out int y, out Vector2Int _))
            {
                if (TryMovePiece(piece, from.Grid, target.Grid, x, y, rotated)) { EndDrag(); return true; }
                Say("no entra ahí");
                if (!keepOnFail) EndDrag();
                return false;
            }
            if (!_area.Contains(pos))
            {
                if (from.IsPlayerPack) { EndDrag(); DropToWorld(piece, from); return true; }
                Say("del mueble no se tira al piso");
            }
            if (!keepOnFail) EndDrag();
            return false;
        }

        private void DrawDrag(Vector2 mouse)
        {
            if (_dragItem == null) return;
            Vector2Int size = _dragRotated ? new Vector2Int(_dragItem.Item.SizeCells.y, _dragItem.Item.SizeCells.x) : _dragItem.Item.SizeCells;

            // Sombra verde o roja de dónde quedaría.
            if (DragTarget(mouse, out GridView target, out int x, out int y, out Vector2Int _))
            {
                bool fits = target.Grid == _dragFrom.Grid
                    ? target.Grid.CanPlace(_dragItem.Item, x, y, _dragRotated, _dragItem)
                    : target.Grid.CanPlace(_dragItem.Item, x, y, _dragRotated);
                GUI.color = fits ? Good : Bad;
                GUI.DrawTexture(new Rect(target.Origin.x + x * Cell, target.Origin.y + y * Cell, size.x * Cell, size.y * Cell), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            var r = new Rect(mouse.x - size.x * Cell * 0.5f, mouse.y - size.y * Cell * 0.5f, size.x * Cell, size.y * Cell);
            DrawPiece(r, _dragItem.Item, _dragRotated, 0.9f);
        }

        private void DrawFooter(Rect area, Vector2 mouse)
        {
            // Qué pieza se está mirando (mouse o cursor) y los controles.
            GridItem hover = null;
            foreach (GridView view in new[] { _pack, _other })
                if (view != null && CellUnder(view, mouse, out int cx, out int cy)) hover = view.Grid.ItemAt(cx, cy);
            hover ??= _dragItem ?? SelectedItem;

            float y = area.yMax - 74f;
            _tip.normal.textColor = Color.white;
            if (Time.unscaledTime < _messageUntil)
            {
                _tip.normal.textColor = Amber;
                GUI.Label(new Rect(area.x + Pad, y, area.width - 2f * Pad, 24f), _message, _tip);
            }
            else if (hover != null)
            {
                ItemDefinition item = hover.Item;
                GUI.Label(new Rect(area.x + Pad, y, area.width - 2f * Pad, 24f),
                    $"{item.PieceName} de {item.DisplayName}   ·   {item.SizeCells.x}×{item.SizeCells.y} casillas   ·   {item.WeightKg:0.0} kg", _tip);
            }
            _small.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
            string keys = _other != null
                ? "mouse: agarrá una pieza y soltala donde da verde (R gira; clic derecho cancela)  ·  teclado: flechas, R, Enter (pasar), Supr (todo)  ·  Tab cerrar"
                : "mouse: agarrá una pieza y soltala donde da verde (R gira; afuera del panel = al piso; clic derecho cancela)  ·  teclado: flechas, R, Enter (soltar), Supr (todo)  ·  Tab cerrar";
            GUI.Label(new Rect(area.x + Pad, area.yMax - 40f, area.width - 2f * Pad, 36f), keys, _small);
        }


        private void EnsureStyles()
        {
            if (_label != null) return;
            _plate = new Texture2D(1, 1);
            _plate.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.1f));
            _plate.Apply();
            _label = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.UpperLeft };
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
