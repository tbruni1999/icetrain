// Prueba del M8 paso 4: almacenamiento. El estante precario (cuadrícula de 4 x 3) y el estante reforzado (desde el 2026-10-16 un
// mueble de 6 x 4 que el banco arma con su tiempo y el jugador coloca donde quiere): abrir, pasar piezas (lo largo no entra en
// lo chico), que se vea lo guardado, que el banco use lo de la mochila y lo del estante del taller, y que no se pierdan ni
// dupliquen piezas.
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
var shelfPlan = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_EstanteReforzado.asset");
// El estante reforzado pide su plano: se aprende antes (el jugador lo lee de la hoja de la estación).
UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>().Learn(shelfPlan);
var bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var placer = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Building.PlacementController>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
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
    if (reinforced != null && reinforced.Grid != null) n += reinforced.Grid.CountOf(items[k]);
    return n;
}
int CountCubes(SurvivalEnTren.Items.StorageContainer s) { int c = 0; foreach (var t in s.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name.StartsWith("Guardado_") && t.GetComponent<UnityEngine.MeshRenderer>() != null && t.gameObject.activeSelf) c++; return c; }
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

    // 2) Pasar los tablones largos (1x4) con Supr (todos los del material): en 4x3 solo caben acostados; desde G1 (pilas) los 4
    //    van en una sola pila acostada.
    yield return Tap(UnityEngine.InputSystem.Key.Delete);
    for (int i = 0; i < 10; i++) yield return wait;
    int planksIn = starter.Grid.CountOf(wood);
    bool lying = true; foreach (var g in starter.Grid.Items) if (g.Item == wood && !g.Rotated) lying = false;
    log.Append($" SUPR sobre un tablón: pasaron {planksIn} de 4 (el estante es de 4x3: entran acostados, los 4 en una pila); acostados={lying}; en la mochila quedan {backpack.CountOf(wood)}; estante {starter.Grid.UsedCells}/{starter.Grid.CellCount} casillas.");

    // 3) La pila de chapas (2x2): con las pilas entra en lo que queda del estante (antes el estante quedaba lleno con los tablones).
    SurvivalEnTren.Items.GridItem metalPiece = null;
    foreach (var g in backpack.Grid.Items) if (g.Item == metal && metalPiece == null) metalPiece = g;
    panel.Close();
    panel.Open(starter.Grid, "ESTANTE PRECARIO");
    for (int i = 0; i < metalPiece.X; i++) yield return Tap(UnityEngine.InputSystem.Key.RightArrow);
    for (int i = 0; i < metalPiece.Y; i++) yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" ENTER sobre la pila de chapas: chapas en el estante {starter.Grid.CountOf(metal)} (entran las 2 en una pila), en la mochila {backpack.CountOf(metal)}; estante {starter.Grid.UsedCells}/{starter.Grid.CellCount} casillas.");

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

    // 6) El estante reforzado: el banco lo arma con lo de la mochila y lo del estante del taller (no entra todo en la mochila).
    backpack.Remove(cloth, backpack.CountOf(cloth)); // la tela no se usa acá: fuera de las cuentas
    for (int g = 0; g < 8 && Total(0) < 6; g++) Give(0, 1);
    for (int g = 0; g < 4 && Total(1) < 2; g++) Give(1, 1);
    int wood0 = Total(0), metal0 = Total(1);
    string fromWhere = $"mochila {backpack.CountOf(wood)} tablones y {backpack.CountOf(metal)} chapas, estante del taller {starter.Grid.CountOf(wood)} tablones y {starter.Grid.CountOf(metal)} chapas";
    var shelfRecipe = new SurvivalEnTren.Crafting.BenchRecipe(shelfPlan);
    var block = bench.Check(shelfRecipe, backpack, gear);
    bool started = bench.StartJob(shelfRecipe, backpack, gear);
    log.Append($" ESTANTE REFORZADO EN EL BANCO: con {fromWhere}: se puede={block}, empezó={started}, tarda {shelfRecipe.Minutes:0} min; usó tablones {wood0 - Total(0)} y chapas {metal0 - Total(1)}.");
    bench.FinishJobNow();
    for (int i = 0; i < 5; i++) yield return wait;
    SurvivalEnTren.Building.UpgradeKit kit = null;
    foreach (var k in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>()) kit = k;
    carrier.Pick(kit.GetComponent<SurvivalEnTren.Interaction.Carryable>());

    // 7) Colocarlo contra la pared izquierda (donde antes estaba la obra) y usarlo.
    Face(new UnityEngine.Vector3(0.2f, 1.05f, 2.7f), new UnityEngine.Vector3(-1.7f, 1.9f, 2.7f));
    var held = carrier.Holding; var hp = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.2f, 2.3f, 2.7f); held.transform.position = hp; held.Body.position = hp;
    for (int i = 0; i < 20; i++) yield return wait;
    bool valid = placer.Valid;
    bool placed = placer.Confirm();
    for (int i = 0; i < 10; i++) yield return wait;
    var placeable = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Building.Placeable>();
    reinforced = placeable != null ? placeable.GetComponent<SurvivalEnTren.Items.StorageContainer>() : null;
    log.Append($" COLOCADO contra la pared: entraba={valid}, colocado={placed}; cuadrícula {reinforced?.Grid?.Width}x{reinforced?.Grid?.Height}; indicación \"{P(reinforced)}\".");
    for (int g = 0; g < 4 && backpack.CountOf(metal) < 2; g++) Give(1, 1);
    reinforced.Interact(who);
    for (int i = 0; i < 10; i++) yield return wait;
    bool opened = panel.IsOpen && panel.HasOtherGrid;
    yield return Tap(UnityEngine.InputSystem.Key.Delete); // pasa las chapas
    for (int i = 0; i < 10; i++) yield return wait;
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    log.Append($" ABRIR con E: abrió={opened}; chapas pasadas al estante reforzado {reinforced.Grid.CountOf(metal)} ({reinforced.Grid.UsedCells}/{reinforced.Grid.CellCount} casillas, quedan {reinforced.Grid.CellCount - reinforced.Grid.UsedCells}); cubos que se ven {CountCubes(reinforced)}.");

    panel.Close();
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Almacen] " + log);
}
motor.StartCoroutine(Run());
return "prueba almacenamiento iniciada";
