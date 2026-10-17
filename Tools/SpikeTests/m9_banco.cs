// Prueba del M9 pasos 2 y 3: el banco de trabajo. E abre el panel con los planos conocidos; sin materiales no fabrica y dice
// qué falta; con los materiales hay una barra de progreso y recién al terminar se gastan y sale lo fabricado (la sierra
// queda al cinto y aparece colgada; el bidón sale vacío sobre la mesa y pide la sierra); Tab cancela sin perder nada y no abre
// la mochila; con la mesa ocupada no fabrica otro bidón; el mouse elige planos y aprieta FABRICAR.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s), con la ventana de Unity con foco,
//   unity command eval_file --file "Tools/SpikeTests/m9_banco.cs" --timeout 60
// Resultado en la Console, líneas [Banco].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.WorkbenchPanel>();
var inventory = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
var bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
var display = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.GearDisplay>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
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
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("SpikeTestMouse");
mouse.MakeCurrent();
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key key)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
    yield return wait; yield return wait; yield return wait;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    yield return wait; yield return wait; yield return wait;
}
void Mouse(UnityEngine.Vector2 pos, bool left = false)
{
    var st = new UnityEngine.InputSystem.LowLevel.MouseState { position = pos };
    st = st.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, left);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, st);
}
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
System.Collections.IEnumerator Click(UnityEngine.Vector2 pos)
{
    Mouse(pos); yield return Frames(3);
    Mouse(pos, left: true); yield return Frames(3);
    Mouse(pos, left: false); yield return Frames(3);
}

object Target() => tInteractor.GetField("_target", flags).GetValue(who);
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
void Face(UnityEngine.Vector3 standLocal, UnityEngine.Vector3 lookLocal)
{
    motor.TeleportTo(trainBody.position + trainBody.rotation * standLocal, trainBody);
    var eye = motor.transform.position + UnityEngine.Vector3.up * 1.6f;
    var d = (trainBody.position + trainBody.rotation * lookLocal) - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
bool DisplayOn() => (bool)((UnityEngine.GameObject)typeof(SurvivalEnTren.Equipment.GearDisplay).GetField("visual", flags).GetValue(display)).activeSelf;
int Cans() => UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelCan>().Length;
void Give(SurvivalEnTren.Items.ItemDefinition item, int n) { backpack.Add(item, n); }

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;

    // 1) Apuntar al banco (pared izquierda del taller) y abrirlo con E.
    // (al banco de verdad, donde esté: desde el 2026-10-16 el tren es más ancho y el banco quedó contra la pared nueva)
    var benchLocal = trainBody.transform.InverseTransformPoint(bench.transform.position);
    Face(new UnityEngine.Vector3(benchLocal.x + 1.3f, 1.05f, benchLocal.z), new UnityEngine.Vector3(benchLocal.x + 0.3f, 1.9f, benchLocal.z));
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($"EL BANCO: apuntándolo el objetivo es {Name(Target())}, indicación \"{bench.GetPrompt(who)}\"; sierra colgada visible={DisplayOn()}; planos en el banco {bench.Catalog.Length}.");
    yield return Tap(UnityEngine.InputSystem.Key.E);
    yield return Frames(4);
    string firstName = panel.Selected != null ? panel.Selected.DisplayName : "ninguno";
    log.Append($" Abrió con E: panel abierto={panel.IsOpen}, planos que se ven {panel.RecipeCount}, elegido \"{firstName}\"; no se camina={motor.MovementLocked}, E apagada={!who.enabled}, cursor libre={look.CursorFree}.");

    // 2) Sin materiales: no fabrica y dice qué falta.
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" SIN MATERIALES: bloqueo={panel.CurrentBlock}, fabricando={panel.IsCrafting}, mensaje \"{panel.LastMessage}\".");
    Give(metal, 1);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" CON LA CHAPA SOLA: fabricando={panel.IsCrafting}, mensaje \"{panel.LastMessage}\".");

    // 3) La sierra: tablón + chapa, 6 s. Los materiales se gastan al terminar, no al empezar.
    Give(wood, 1);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    float t0 = UnityEngine.Time.realtimeSinceStartup;
    bool started = panel.IsCrafting;
    for (int i = 0; i < 50; i++) yield return wait; // ~1 s
    float partial = panel.Progress01;
    int woodMid = backpack.CountOf(wood), metalMid = backpack.CountOf(metal);
    int guard = 0;
    while (panel.IsCrafting && guard++ < 1200) yield return wait;
    float took = UnityEngine.Time.realtimeSinceStartup - t0;
    log.Append($" SIERRA: arrancó={started}; al ~1 s la barra va en {partial:0.00} y los materiales siguen en la mochila (tablón {woodMid}, chapa {metalMid}); terminó a los {took:0.0} s (plano: 6 s); ahora tablón {backpack.CountOf(wood)}, chapa {backpack.CountOf(metal)}; sierra al cinto={gear.Has(SurvivalEnTren.Equipment.GearId.Saw)}; colgada en el taller={DisplayOn()}; mensaje \"{panel.LastMessage}\".");
    log.Append($" Otra sierra: bloqueo={panel.CurrentBlock} (ya la tenés).");
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" Enter otra vez: fabricando={panel.IsCrafting}, mensaje \"{panel.LastMessage}\".");

    // 4) El bidón: ↓ lo elige; pide 2 chapas y tener la sierra.
    yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    log.Append($" BIDÓN: elegido \"{panel.Selected.DisplayName}\" (índice {panel.SelectedIndex}); bloqueo={panel.CurrentBlock};");
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" mensaje \"{panel.LastMessage}\".");

    // 5) Tab cancela a la mitad sin perder nada, y esa misma pulsación no abre la mochila.
    Give(metal, 2);
    int cansBefore = Cans();
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    bool startedCan = panel.IsCrafting;
    for (int i = 0; i < 100; i++) yield return wait; // ~2 s
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    yield return Frames(10);
    log.Append($" CANCELAR (Tab a los ~2 s): fabricaba={startedCan}; ahora panel abierto={panel.IsOpen}, fabricando={panel.IsCrafting}, chapas {backpack.CountOf(metal)} (tenía 2), bidones {Cans()} (antes {cansBefore}); la mochila no se abrió={!inventory.IsOpen}; se camina={!motor.MovementLocked}, E prendida={who.enabled}, cursor libre={look.CursorFree}.");
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    bool backpackWorks = inventory.IsOpen;
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    log.Append($" Después, Tab abre la mochila otra vez={backpackWorks} y la cierra={!inventory.IsOpen}.");

    // 6) El bidón de verdad: 8 s, sale vacío sobre la mesa.
    for (int i = 0; i < 20; i++) yield return wait;
    yield return Tap(UnityEngine.InputSystem.Key.E);
    yield return Frames(4);
    log.Append($" Reabrió: abierto={panel.IsOpen}, elegido \"{panel.Selected?.DisplayName}\" (vuelve al primero).");
    yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    var knownCans = new System.Collections.Generic.HashSet<SurvivalEnTren.Train.FuelCan>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelCan>());
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    t0 = UnityEngine.Time.realtimeSinceStartup;
    guard = 0;
    while (panel.IsCrafting && guard++ < 1500) yield return wait;
    took = UnityEngine.Time.realtimeSinceStartup - t0;
    for (int i = 0; i < 75; i++) yield return wait; // que se asiente
    SurvivalEnTren.Train.FuelCan fresh = null;
    foreach (var c in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelCan>()) if (!knownCans.Contains(c)) fresh = c;
    string where = "no apareció";
    bool onBench = false;
    if (fresh != null)
    {
        UnityEngine.Vector3 local = trainBody.transform.InverseTransformPoint(fresh.transform.position);
        onBench = local.x < -0.8f && local.x > -2.1f && local.y > 1.85f && local.y < 2.3f && UnityEngine.Mathf.Abs(local.z - 0.1f) < 0.4f;
        where = $"en el tren x {local.x:0.00}, y {local.y:0.00}, z {local.z:0.00}";
    }
    log.Append($" BIDÓN: terminó a los {took:0.0} s (plano: 8 s); chapas {backpack.CountOf(metal)}; bidones {Cans()} (antes {cansBefore}); sale vacío={(fresh != null && fresh.Liters == 0f)}; se lleva a una mano={(fresh != null && fresh.GetComponent<SurvivalEnTren.Interaction.Carryable>() != null)}; posición {where} → sobre la mesa y quieto={onBench}; sierra todavía al cinto={gear.Has(SurvivalEnTren.Equipment.GearId.Saw)}; mensaje \"{panel.LastMessage}\".");

    // 7) La mesa ocupada: no fabrica otro encima.
    Give(metal, 2);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" MESA OCUPADA: bloqueo={panel.CurrentBlock}, fabricando={panel.IsCrafting}, mensaje \"{panel.LastMessage}\".");
    if (fresh != null) UnityEngine.Object.Destroy(fresh.gameObject);
    yield return Frames(5);
    log.Append($" Sacada la cosa de la mesa: bloqueo={panel.CurrentBlock}.");

    // 8) Mouse: elegir un plano con clic y apretar FABRICAR (sin materiales, para ver el aviso).
    backpack.Remove(metal, backpack.CountOf(metal));
    yield return Click(panel.RowCenterScreen(0));
    int afterRow0 = panel.SelectedIndex;
    yield return Click(panel.RowCenterScreen(1));
    int afterRow1 = panel.SelectedIndex;
    yield return Click(panel.CraftButtonCenterScreen());
    log.Append($" MOUSE: clic en el plano 1 → elegido {afterRow0}; clic en el plano 2 → elegido {afterRow1}; clic en FABRICAR sin materiales: fabricando={panel.IsCrafting}, mensaje \"{panel.LastMessage}\".");

    // 9) Cerrar con Tab deja todo como estaba.
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    yield return Frames(6);
    log.Append($" CERRAR: abierto={panel.IsOpen}, se camina={!motor.MovementLocked}, E prendida={who.enabled}, cursor libre={look.CursorFree}, mochila abierta={inventory.IsOpen}.");

    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Banco] " + log);
}
motor.StartCoroutine(Run());
return "prueba banco iniciada";
