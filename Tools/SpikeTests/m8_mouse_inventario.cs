// Prueba del inventario con el MOUSE (Input System, con un mouse virtual): agarrar una pieza, arrastrarla y soltarla donde
// da verde, SIN Enter; R mientras se arrastra; rojo no entra (vuelve a su lugar); afuera del panel va al piso; dos clics
// (clic agarra, clic deja); clic derecho cancela; del mueble a la mochila y al revés; el clic no lanza lo que se lleva.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m8_mouse_inventario.cs" --timeout 60
// Resultado en la Console, líneas [MouseInv].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var box = UnityEngine.GameObject.Find("Caja_Piso_Grande").GetComponent<SurvivalEnTren.Interaction.Carryable>();
var trainBody = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>().GetComponent<UnityEngine.Rigidbody>();
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
var frame = new UnityEngine.WaitForEndOfFrame();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = inputSettings.editorInputBehaviorInPlayMode;
var oldBackground = inputSettings.backgroundBehavior;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("SpikeTestMouse");
mouse.MakeCurrent();
void Mouse(UnityEngine.Vector2 pos, bool left = false, bool right = false)
{
    var st = new UnityEngine.InputSystem.LowLevel.MouseState { position = pos };
    st = st.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, left).WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, right);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, st);
}
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
System.Collections.IEnumerator TapKey(UnityEngine.InputSystem.Key key)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
    yield return Frames(4);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    yield return Frames(4);
}
UnityEngine.Vector2 Cell(int grid, int cx, int cy) => panel.CellCenterScreen(grid, cx, cy);
SurvivalEnTren.Items.GridItem Piece(SurvivalEnTren.Items.ItemGrid g, SurvivalEnTren.Items.ItemDefinition item)
{
    foreach (var p in g.Items) if (p.Item == item) return p;
    return null;
}
string Where(SurvivalEnTren.Items.GridItem p) => p == null ? "(no está)" : $"({p.X},{p.Y}) {p.Size.x}x{p.Size.y}{(p.Rotated ? " girada" : "")}";

// Arrastra con el botón izquierdo apretado de una posición a otra y suelta.
System.Collections.IEnumerator Drag(UnityEngine.Vector2 from, UnityEngine.Vector2 to, bool rotateInTheMiddle = false)
{
    Mouse(from); yield return Frames(3);
    Mouse(from, left: true); yield return Frames(3);
    var mid = (from + to) * 0.5f;
    Mouse(mid, left: true); yield return Frames(3);
    if (rotateInTheMiddle) yield return TapKey(UnityEngine.InputSystem.Key.R);
    Mouse(to, left: true); yield return Frames(5);
    Mouse(to, left: false); yield return Frames(5);
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;
    var grid = backpack.Grid;
    while (grid.Items.Count > 0) grid.Remove(grid.Items[0]);
    backpack.Add(wood, 1); backpack.Add(metal, 1); backpack.Add(cloth, 1);
    var w = Piece(grid, wood); var m = Piece(grid, metal); var c = Piece(grid, cloth);
    log.Append($"Inicio: tablón {Where(w)}, chapa {Where(m)}, rollo {Where(c)}.");
    panel.Open();
    yield return Frames(4);

    // 1) Agarrar el tablón (casilla (0,1)), arrastrarlo y soltarlo donde da verde: la columna 5 (a la derecha), sin Enter.
    yield return Drag(Cell(0, 0, 1), Cell(0, 5, 2));
    log.Append($" ARRASTRAR (sin Enter): tablón a la derecha → {Where(w)}.");

    // 2) Rojo: la chapa 2x2 soltada sobre el tablón no entra: vuelve a su lugar.
    var mBefore = Where(m);
    yield return Drag(Cell(0, 1, 0), Cell(0, 5, 1));
    log.Append($" ROJO: chapa soltada sobre el tablón: antes {mBefore}, ahora {Where(m)} (no entró, vuelve a su lugar).");

    // 3) R mientras se arrastra: el tablón (4 de largo) se acuesta y entra en la fila de abajo.
    yield return Drag(Cell(0, 5, 1), Cell(0, 2, 4), rotateInTheMiddle: true);
    log.Append($" R EN EL ARRASTRE: tablón soltado en la fila de abajo → {Where(w)}.");

    // 4) Dos clics: un clic agarra el rollo, otro lo deja en un hueco libre.
    Mouse(Cell(0, 3, 0)); yield return Frames(3);
    Mouse(Cell(0, 3, 0), left: true); yield return Frames(2);
    Mouse(Cell(0, 3, 0), left: false); yield return Frames(3);
    bool carrying = panel.IsCarryingByClick;
    Mouse(Cell(0, 5, 2)); yield return Frames(3);
    Mouse(Cell(0, 5, 2), left: true); yield return Frames(3);
    Mouse(Cell(0, 5, 2), left: false); yield return Frames(4);
    log.Append($" DOS CLICS: el primer clic lo agarra (en la mano={carrying}); el segundo lo deja → rollo {Where(c)}; en la mano ahora={panel.IsDragging}.");

    // 5) Clic derecho cancela: agarro la chapa con un clic, clic derecho, no se movió.
    var mBefore2 = Where(m);
    Mouse(Cell(0, 1, 0)); yield return Frames(3);
    Mouse(Cell(0, 1, 0), left: true); yield return Frames(2);
    Mouse(Cell(0, 1, 0), left: false); yield return Frames(3);
    bool carrying2 = panel.IsCarryingByClick;
    Mouse(Cell(0, 4, 2), right: true); yield return Frames(3);
    Mouse(Cell(0, 4, 2), right: false); yield return Frames(3);
    log.Append($" CLIC DERECHO: chapa agarrada={carrying2}, cancelada → sigue en {Where(m)} (antes {mBefore2}), en la mano={panel.IsDragging}.");

    // 6) Soltar afuera del panel: la chapa va al piso como bulto.
    int bundlesBefore = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>().Length;
    int metalPack = backpack.CountOf(metal);
    yield return Drag(Cell(0, 1, 0), new UnityEngine.Vector2(60f, 60f)); // esquina de la pantalla, afuera del panel
    for (int i = 0; i < 10; i++) yield return wait;
    int bundlesAfter = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>().Length;
    log.Append($" AFUERA DEL PANEL: chapas en la mochila {metalPack} → {backpack.CountOf(metal)}, bultos en el mundo {bundlesBefore} → {bundlesAfter}.");

    // 7) De la mochila a un estante y de vuelta, arrastrando.
    panel.Close();
    var shelf = new SurvivalEnTren.Items.ItemGrid(4, 3);
    panel.Open(shelf, "ESTANTE PRECARIO");
    yield return Frames(4);
    var clothInPack = Piece(grid, cloth);
    // el rollo (1x2) a la casilla (1,0) del estante: con la mitad de su alto de margen, soltado en la casilla (1,1)
    yield return Drag(Cell(0, clothInPack.X, clothInPack.Y), Cell(1, 1, 1));
    log.Append($" A UN ESTANTE: rollo en la mochila {backpack.CountOf(cloth)}, en el estante {shelf.CountOf(cloth)} {(shelf.Items.Count > 0 ? Where(shelf.Items[0]) : "")}.");
    yield return Drag(Cell(1, shelf.Items[0].X, shelf.Items[0].Y), Cell(0, 5, 2));
    log.Append($" DE VUELTA: rollo en la mochila {backpack.CountOf(cloth)}, en el estante {shelf.CountOf(cloth)}.");
    // lo largo en un estante chico: el tablón acostado entra, parado no
    var woodInPack = Piece(grid, wood);
    yield return Drag(Cell(0, woodInPack.X, woodInPack.Y), Cell(1, 2, 1));
    log.Append($" LO LARGO: tablón al estante de 4x3 → en el estante {shelf.CountOf(wood)} {(shelf.CountOf(wood) > 0 ? Where(Piece(shelf, wood)) : "(no entró)")} (el tablón ya venía acostado de la prueba anterior).");
    yield return Drag(Cell(0, woodInPack.X, woodInPack.Y), Cell(1, 2, 1), rotateInTheMiddle: true);
    log.Append($" Otra vez, con R en el arrastre: en el estante {shelf.CountOf(wood)} {(shelf.CountOf(wood) > 0 ? Where(Piece(shelf, wood)) : "(no entró)")}.");
    panel.Close();

    // 8) Con una caja en la mano el clic izquierdo del panel no la lanza.
    motor.TeleportTo(box.transform.position + trainBody.rotation * new UnityEngine.Vector3(0f, 0.1f, 1.0f), trainBody);
    var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
    var d = box.transform.position - (motor.transform.position + UnityEngine.Vector3.up * 1.6f);
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, 30f);
    for (int i = 0; i < 30; i++) yield return wait;
    box.Interact(who);
    for (int i = 0; i < 40; i++) yield return wait;
    bool heldBefore = carrier.Holding == box;
    panel.Open();
    yield return Frames(4);
    Mouse(new UnityEngine.Vector2(60f, 60f), left: true); yield return Frames(3);
    Mouse(new UnityEngine.Vector2(60f, 60f), left: false); yield return Frames(3);
    bool heldAfter = carrier.Holding == box;
    panel.Close();
    carrier.Drop(false);
    log.Append($" CLIC CON UNA CAJA EN LA MANO: la tenía={heldBefore}; con el panel abierto un clic no la lanza (la sigue teniendo={heldAfter}).");

    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[MouseInv] " + log);
}
motor.StartCoroutine(Run());
return "prueba mouse inventario iniciada";
