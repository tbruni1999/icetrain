// Prueba del M8 paso 4: almacenamiento. El estante precario (cuadrícula de 4 x 3) y el estante reforzado (una obra de
// cuatro fases cuya cuadrícula crece de 4 x 2 a 6 x 4): abrir, pasar piezas (lo largo no entra en lo chico), que se vea
// lo guardado, que lo guardado se conserve al crecer el estante y que no se pierdan ni dupliquen piezas.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m8_almacenamiento.cs" --timeout 60
// Resultado en la Console, líneas [Almacen].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
SurvivalEnTren.Items.StorageContainer starter = null, reinforced = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.StorageContainer>())
{
    if (s.StableId == "storage_tren_estante_precario_001") starter = s;
    else if (s.StableId == "storage_tren_estante_reforzado_001") reinforced = s;
}
SurvivalEnTren.Building.BuildSite site = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (s.StableId == "obra_tren_estante_001") site = s;
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var items = new[] { wood, metal, cloth };
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = inputSettings.editorInputBehaviorInPlayMode;
var oldBackground = inputSettings.backgroundBehavior;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key key)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
    yield return wait; yield return wait; yield return wait;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    yield return wait; yield return wait; yield return wait;
}
void KeyE(bool down) => UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, down ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E) : new UnityEngine.InputSystem.LowLevel.KeyboardState());

object Target() => tInteractor.GetField("_target", flags).GetValue(who);
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
string P(SurvivalEnTren.Interaction.IInteractable i) => i.GetPrompt(who) ?? "(sin indicación)";
var given = new int[3];
void Give(int k, int n) { given[k] += backpack.Add(items[k], n); }
int Total(int k)
{
    int n = backpack.CountOf(items[k]);
    if (starter.Grid != null) n += starter.Grid.CountOf(items[k]);
    if (reinforced.Grid != null) n += reinforced.Grid.CountOf(items[k]);
    if (site != null) { /* lo aportado a la obra ya no es de nadie más: se cuenta aparte */ }
    return n;
}
void Face(UnityEngine.Vector3 standLocal, UnityEngine.Vector3 lookLocal)
{
    motor.TeleportTo(trainBody.position + trainBody.rotation * standLocal, trainBody);
    var eye = motor.transform.position + UnityEngine.Vector3.up * 1.6f;
    var d = (trainBody.position + trainBody.rotation * lookLocal) - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;

    // 1) El estante precario del taller (pared derecha, z de -2,5 a -0,5): apuntarlo y abrirlo.
    Face(new UnityEngine.Vector3(0.1f, 1.05f, -1.5f), new UnityEngine.Vector3(1.4f, 1.6f, -1.5f));
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($"ESTANTE PRECARIO: cuadrícula {starter.Grid.Width}x{starter.Grid.Height} = {starter.Grid.CellCount} casillas; apuntando el objetivo es {Name(Target())}, indicación \"{P(starter)}\".");
    Give(0, 4); Give(1, 2); Give(2, 3);
    var before = new[] { Total(0), Total(1), Total(2) };
    starter.Interact(who);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" Abrió: panel abierto={panel.IsOpen}, con la cuadrícula del estante al lado={panel.HasOtherGrid}.");

    // 2) Pasar los tablones largos (1x4) con Supr (todos los del material): en 4x3 solo caben acostados y apenas 3.
    yield return Tap(UnityEngine.InputSystem.Key.Delete);
    for (int i = 0; i < 10; i++) yield return wait;
    int planksIn = starter.Grid.CountOf(wood);
    bool lying = true; foreach (var g in starter.Grid.Items) if (g.Item == wood && !g.Rotated) lying = false;
    log.Append($" SUPR sobre un tablón: pasaron {planksIn} de 4 (el estante es de 4x3: solo entran acostados, uno por fila); acostados={lying}; en la mochila quedan {backpack.CountOf(wood)}; estante {starter.Grid.UsedCells}/{starter.Grid.CellCount} casillas.");

    // 3) Una chapa (2x2) ya no entra (el estante está lleno): se queda en la mochila.
    SurvivalEnTren.Items.GridItem metalPiece = null;
    foreach (var g in backpack.Grid.Items) if (g.Item == metal && metalPiece == null) metalPiece = g;
    panel.Close();
    panel.Open(starter.Grid, "ESTANTE PRECARIO");
    for (int i = 0; i < metalPiece.X; i++) yield return Tap(UnityEngine.InputSystem.Key.RightArrow);
    for (int i = 0; i < metalPiece.Y; i++) yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" ENTER sobre una chapa con el estante lleno: chapas en el estante {starter.Grid.CountOf(metal)} (no entra), en la mochila {backpack.CountOf(metal)}.");

    // 4) Sacar un tablón del estante a la mochila (la mochila tiene lugar acostando...): cursor al estante y Enter.
    panel.Close();
    panel.Open(starter.Grid, "ESTANTE PRECARIO");
    for (int i = 0; i < 6; i++) yield return Tap(UnityEngine.InputSystem.Key.RightArrow); // pasa a la cuadrícula del estante
    var onShelf = panel.SelectedItem;
    int packWoodBefore = backpack.CountOf(wood);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" Del estante a la mochila (Enter sobre {onShelf?.Item.PieceName} en el estante): tablones en el estante {starter.Grid.CountOf(wood)}, en la mochila {backpack.CountOf(wood)} (antes {packWoodBefore}).");
    yield return Tap(UnityEngine.InputSystem.Key.Tab);

    // 5) Se ve lo guardado: un cubo por material distinto sobre las tablas del estante.
    int cubes = 0;
    foreach (var t in starter.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name.StartsWith("Guardado_") && t.GetComponent<UnityEngine.MeshRenderer>() != null && t.gameObject.activeSelf) cubes++;
    log.Append($" SE VE LO GUARDADO: cubos visibles en el estante {cubes} (materiales distintos adentro: {(starter.Grid.CountOf(wood) > 0 ? 1 : 0) + (starter.Grid.CountOf(metal) > 0 ? 1 : 0) + (starter.Grid.CountOf(cloth) > 0 ? 1 : 0)}).");

    // 6) El estante reforzado: obra de 4 fases; sin servir para guardar hasta el primer estante.
    Face(new UnityEngine.Vector3(0.0f, 1.05f, 2.4f), new UnityEngine.Vector3(-1.4f, 1.6f, 2.4f));
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($" ESTANTE REFORZADO: obra \"{site.Project.DisplayName}\", {site.Project.PhaseCount} fases; nivel {site.Level}; cuadrícula={(reinforced.Grid == null ? "ninguna (todavía no guarda)" : reinforced.Grid.Width + "x" + reinforced.Grid.Height)}; apuntando el objetivo es {Name(Target())}, indicación \"{P(site)}\".");
    // vaciar la mochila a un estado conocido sin perder piezas: pasar todo al estante precario o dejar en bultos
    while (backpack.Grid.Items.Count > 0) panel.DropPiece(backpack.Grid.Items[0]);
    given[0] = given[1] = given[2] = 0; // cuentas desde acá
    var bundlesBefore = new int[3];
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) for (int k = 0; k < 3; k++) if (b.Item == items[k]) bundlesBefore[k] += b.Count;
    int shelfBefore = starter.Grid.Items.Count;
    var sizes = new System.Text.StringBuilder();
    Give(1, 2); for (int w = 0; w < 15; w++) yield return wait; // chapas: soportes
    var who2 = who;
    int lv = site.Level; int held = 0; for (int attempt = 0; attempt < 3 && site.Level == lv; attempt++) { KeyE(true); held = 0;
    while (site.Level == lv && held < 200) { yield return wait; held++; }
    KeyE(false); for (int i = 0; i < 10; i++) yield return wait; }
    sizes.Append($"soportes → nivel {site.Level} (cuadrícula {(reinforced.Grid == null ? "ninguna" : reinforced.Grid.Width + "x" + reinforced.Grid.Height)}); ");
    Give(0, 2); for (int w = 0; w < 15; w++) yield return wait; // estructura
    lv = site.Level; held = 0; for (int attempt = 0; attempt < 3 && site.Level == lv; attempt++) { KeyE(true); held = 0;
    while (site.Level == lv && held < 200) { yield return wait; held++; }
    KeyE(false); for (int i = 0; i < 10; i++) yield return wait; }
    sizes.Append($"estructura → nivel {site.Level} (cuadrícula {(reinforced.Grid == null ? "ninguna" : reinforced.Grid.Width + "x" + reinforced.Grid.Height)}); ");
    Give(0, 2); for (int w = 0; w < 15; w++) yield return wait; // primer estante
    lv = site.Level; held = 0; for (int attempt = 0; attempt < 3 && site.Level == lv; attempt++) { KeyE(true); held = 0;
    while (site.Level == lv && held < 200) { yield return wait; held++; }
    KeyE(false); for (int i = 0; i < 10; i++) yield return wait; }
    sizes.Append($"primer estante → nivel {site.Level} (cuadrícula {(reinforced.Grid == null ? "ninguna" : reinforced.Grid.Width + "x" + reinforced.Grid.Height)}). ");
    log.Append($" FASES: {sizes}");

    // 7) Ya sirve: con nada que aportar, E abre el estante (la obra delega); guardar 2 chapas (2x2 cada una llenan el 4x2).
    Give(1, 2);
    log.Append($" Con el primer estante hecho y sin madera que aportar, la indicación es \"{P(site)}\".");
    site.Channel(who, 0.02f); // lo mismo que hace E: abre el estante porque no hay nada que aportar
    for (int i = 0; i < 10; i++) yield return wait;
    bool openedByDelegate = panel.IsOpen && panel.HasOtherGrid;
    panel.Close();
    panel.Open(reinforced.Grid, "ESTANTE REFORZADO");
    yield return Tap(UnityEngine.InputSystem.Key.Delete); // pasa las chapas
    for (int i = 0; i < 10; i++) yield return wait;
    int metalInShelf = reinforced.Grid.CountOf(metal);
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    log.Append($" Abrió por la obra={openedByDelegate}; pasó {metalInShelf} chapas al estante de {reinforced.Grid.Width}x{reinforced.Grid.Height} (llenaron las 8 casillas: {reinforced.Grid.UsedCells}/{reinforced.Grid.CellCount}).");

    // 8) Cuarta fase: el estante crece a 6x4 y lo guardado se conserva en su lugar.
    var chapasPositions = new System.Text.StringBuilder();
    foreach (var g in reinforced.Grid.Items) chapasPositions.Append($"({g.X},{g.Y}) ");
    Give(0, 2); for (int w = 0; w < 15; w++) yield return wait;
    lv = site.Level; held = 0; for (int attempt = 0; attempt < 3 && site.Level == lv; attempt++) { KeyE(true); held = 0;
    while (site.Level == lv && held < 200) { yield return wait; held++; }
    KeyE(false); for (int i = 0; i < 10; i++) yield return wait; }
    var after = new System.Text.StringBuilder();
    foreach (var g in reinforced.Grid.Items) after.Append($"({g.X},{g.Y}) ");
    log.Append($" SEGUNDO ESTANTE: nivel {site.Level} (completa={site.IsComplete}), cuadrícula {reinforced.Grid.Width}x{reinforced.Grid.Height}; las chapas guardadas siguen en {after} (antes {chapasPositions}); ahora hay lugar para {reinforced.Grid.CellCount - reinforced.Grid.UsedCells} casillas más; indicación \"{P(site)}\".");

    // 9) Cuentas: lo dado a la mochila = lo que queda en la mochila + estantes + la obra (4+... aportado) + bultos nuevos.
    int woodInSite = 2 + 2 + 2, metalInSite = 2;
    int bundlesWood = 0, bundlesMetal = 0, bundlesCloth = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) { if (b.Item == wood) bundlesWood += b.Count; if (b.Item == metal) bundlesMetal += b.Count; if (b.Item == cloth) bundlesCloth += b.Count; }
    log.Append($" CUENTAS (desde que se vació la mochila): madera dada {given[0]} = en la mochila {backpack.CountOf(wood)} + en la obra {woodInSite}; chapas dadas {given[1]} = en la mochila {backpack.CountOf(metal)} + en el estante reforzado {reinforced.Grid.CountOf(metal)} + en la obra {metalInSite}.");

    panel.Close();
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Almacen] " + log);
}
motor.StartCoroutine(Run());
return "prueba almacenamiento iniciada";
