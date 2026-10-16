// Prueba de las mejoras nuevas (decisiones de Tomi del 2026-10-16): el banco arma los kits con un tiempo del reloj del juego
// (sigue solo), el kit sale al lado del banco, se lleva en la mano y se instala manteniendo E en la obra (sin fantasmas);
// el estante sale como kit y se coloca donde uno quiere (vista previa clara o roja, la rueda gira, el clic instala); la llave
// de montaje lo levanta con lo guardado adentro y lo deja en otro lado; todo se guarda.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/h6_mejoras.cs" --timeout 60
// Resultado en la Console, línea [Mejoras].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var book = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>();
var belt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
var placer = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Building.PlacementController>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.WorkbenchPanel>();
var bed = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.Bed>();
var save = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Save.SaveSystem>();
SurvivalEnTren.Building.BuildSite bedSite = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (s.StableId == "obra_tren_cama_001") bedSite = s;
string D(string n) => $"Assets/_Project/Data/Items/{n}.asset";
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(D("Madera"));
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(D("Metal"));
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(D("Tela"));
var shelfPlan = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_EstanteReforzado.asset");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;

var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = inputSettings.editorInputBehaviorInPlayMode;
var oldBackground = inputSettings.backgroundBehavior;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("PruebaTeclado");
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("PruebaMouse");
void Key(UnityEngine.InputSystem.Key key, bool down) => UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, down ? new UnityEngine.InputSystem.LowLevel.KeyboardState(key) : new UnityEngine.InputSystem.LowLevel.KeyboardState());
void MouseState(bool left, float scroll) => UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = (ushort)(left ? 1 : 0), scroll = new UnityEngine.Vector2(0f, scroll) });
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
System.Collections.IEnumerator Click() { MouseState(true, 0f); yield return Frames(3); MouseState(false, 0f); yield return Frames(4); }
// Un clic simulado a veces no llega a tiempo (el editor cargado): se reintenta hasta 3 veces y se cuenta cuántos hicieron falta.
int retries = 0;
System.Collections.IEnumerator ClickUntil(System.Func<bool> done, int settle) { for (int k = 0; k < 3; k++) { yield return Click(); for (int i = 0; i < settle; i++) yield return wait; if (done()) yield break; retries++; } }
System.Collections.IEnumerator Scroll(float amount) { MouseState(false, amount); yield return Frames(1); MouseState(false, 0f); yield return Frames(3); }
System.Collections.IEnumerator HoldE(int frames) { Key(UnityEngine.InputSystem.Key.E, true); yield return Frames(frames); Key(UnityEngine.InputSystem.Key.E, false); yield return Frames(5); }

UnityEngine.Vector3 Local(float x, float y, float z) => trainBody.position + trainBody.rotation * new UnityEngine.Vector3(x, y, z);
UnityEngine.Vector3 At(float x, float worldY, float z) { var p = Local(x, 0f, z); p.y = worldY; return p; }
void Stand(float x, float z)
{
    motor.TeleportTo(Local(x, 1.05f, z), trainBody);
    var held = carrier.Holding; // lo que se lleva en la mano viaja con el jugador
    if (held != null) { var p = Local(x, 2.3f, z); held.transform.position = p; held.Body.position = p; held.Body.linearVelocity = UnityEngine.Vector3.zero; }
}
void Aim(UnityEngine.Vector3 point)
{
    var eye = motor.transform.Find("Cabeza").position;
    var d = point - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
int RecipeIndex(string start) { var list = bench.Recipes(book); return list.FindIndex(r => r.DisplayName.StartsWith(start)); }
SurvivalEnTren.Building.UpgradeKit NewestKit() { SurvivalEnTren.Building.UpgradeKit k = null; foreach (var x in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>()) k = x; return k; }
string Prompt() { var t = typeof(SurvivalEnTren.Interaction.PlayerInteractor).GetField("_target", flags).GetValue(who) as SurvivalEnTren.Interaction.IInteractable; var e = typeof(SurvivalEnTren.Interaction.PlayerInteractor).GetField("_engaged", flags).GetValue(who) as SurvivalEnTren.Interaction.IInteractable; var use = (bool)typeof(SurvivalEnTren.Interaction.PlayerInteractor).GetField("_usableWhileEngaged", flags).GetValue(who); var shown = e != null && !use ? e : t; return shown?.GetPrompt(who) ?? "(nada)"; }

System.IO.File.WriteAllText("Temp/prueba_h6.txt", "");
void Step(string s) { UnityEngine.Debug.Log("[MejorasPaso] " + s); System.IO.File.WriteAllText("Temp/prueba_h6.txt", "paso " + s + " :: " + log + "\n"); }

System.Collections.IEnumerator Run()
{
    yield return Frames(30);
    float floorY = bed.transform.position.y; // el piso del tren (la tela de la cama está apoyada)

    Step("1");
    // 1) LO QUE OFRECE EL BANCO y la cama sin fantasmas.
    var phases = (System.Array)typeof(SurvivalEnTren.Building.BuildSite).GetField("phasePieces", flags).GetValue(bedSite);
    int shown = 0, total = 0;
    foreach (var phase in phases) foreach (var piece in (UnityEngine.GameObject[])phase.GetType().GetField("pieces").GetValue(phase)) { total++; if (piece.activeSelf) shown++; }
    var offer = bench.Recipes(book);
    log.Append($"BANCO: {string.Join(" | ", offer.ConvertAll(r => r.DisplayName + (r.Timed ? $" ({r.Minutes:0} min)" : "")))}. CAMA sin mejorar: se ven {shown} de {total} piezas (sin fantasmas).");

    Step("2");
    // 2) ARMAR EL KIT DE LA CAMA con el panel: los materiales se gastan al empezar, el banco sigue solo.
    backpack.Add(wood, 4);
    panel.Open(bench);
    yield return Frames(2);
    panel.Select(RecipeIndex("cama"));
    bool started = panel.StartCraft();
    int woodLeft = backpack.CountOf(wood);
    var offerAfter = bench.Recipes(book).ConvertAll(r => r.DisplayName);
    panel.Close();
    yield return Frames(5);
    Stand(-0.4f, 0.2f);
    Aim(bench.transform.position + UnityEngine.Vector3.up * 0.3f);
    yield return Frames(10);
    log.Append($" KIT DE LA CAMA: empezó={started}, tablones en la mochila 4 → {woodLeft}, banco ocupado={bench.IsBusy}, faltan {bench.JobMinutesLeft:0} min; indicación del banco \"{Prompt()}\"; el banco ahora ofrece {string.Join(", ", offerAfter.FindAll(n => n.StartsWith("cama")))}.");
    yield return Frames(60); // ~1,2 s reales = 1,2 min del juego
    float leftAfter = bench.JobMinutesLeft;
    clock.Skip(0.5f);  // dormir media hora (el reloj salta): el banco termina igual
    yield return Frames(5);
    var kit = NewestKit();
    log.Append($" Después de 1,2 s faltaban {leftAfter:0.0} min; después de saltar media hora: banco ocupado={bench.IsBusy}, kit={(kit != null ? kit.Describe() : "ninguno")}, pesa {(kit != null ? kit.GetComponent<UnityEngine.Rigidbody>().mass : 0):0.0} kg, a {(kit != null ? UnityEngine.Vector3.Distance(kit.transform.position, bench.KitPoint.position) : -1):0.00} m del lugar del kit.");

    Step("3");
    // 3) INSTALAR: tomar el kit, llevarlo a la cama, mantener E.
    carrier.Pick(kit.GetComponent<SurvivalEnTren.Interaction.Carryable>());
    yield return Frames(10);
    string carryPrompt = Prompt();
    float before = bed.RecoveryPerHour;
    Stand(0.3f, 4.5f);
    Aim(bed.transform.position + UnityEngine.Vector3.up * 0.25f);
    yield return Frames(30);
    string installPrompt = Prompt();
    yield return HoldE(150); // 3 s (pide 2,5)
    log.Append($" INSTALAR: con el kit en la mano \"{carryPrompt}\"; mirando la cama \"{installPrompt}\"; mantener E 3 s → nivel {bedSite.Level}, recuperación {before:0.0} → {bed.RecoveryPerHour:0.0} por hora, kit en la mano={carrier.Holding != null}, kits en el mundo={UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>().Length}; sin nada en la mano \"{Prompt()}\".");

    Step("4");
    // 4) EL ESTANTE: sin el plano no está; con el plano, el banco lo arma y sale como kit.
    bool offeredWithout = RecipeIndex("estante") >= 0;
    book.Learn(shelfPlan);
    backpack.Add(metal, 2); backpack.Add(wood, 4);
    SurvivalEnTren.Items.StorageContainer starter = null;
    foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.StorageContainer>()) if (s.StableId == "storage_tren_estante_precario_001") starter = s;
    starter.Grid.TryAutoPlace(wood); starter.Grid.TryAutoPlace(wood); // un estante entero no entra en la mochila: 2 tablones en el estante del taller
    panel.Open(bench);
    yield return Frames(2);
    panel.Select(RecipeIndex("estante"));
    bool shelfStarted = panel.StartCraft();
    panel.Close();
    bench.FinishJobNow();
    yield return Frames(5);
    var shelfKit = NewestKit();
    log.Append($" ESTANTE: sin plano en el banco={offeredWithout}; con el plano (4 tablones y 2 chapas en la mochila, 2 tablones en el estante del taller) empezó={shelfStarted}, quedan en el estante {starter.Grid.CountOf(wood)} y en la mochila {backpack.CountOf(wood)}, sale {(shelfKit != null ? shelfKit.Describe() : "nada")} ({(shelfKit != null ? shelfKit.GetComponent<UnityEngine.Rigidbody>().mass : 0):0} kg).");

    Step("5");
    // 5) COLOCAR: con el kit en la mano se ve el estante; contra el banco no entra; contra la pared izquierda sí.
    carrier.Pick(shelfKit.GetComponent<SurvivalEnTren.Interaction.Carryable>());
    Stand(0.2f, 0.0f);
    Aim(bench.transform.position + UnityEngine.Vector3.up * 0.5f);
    yield return Frames(15);
    bool previewOn = placer.Active;
    bool validAtBench = placer.Valid;
    string reasonAtBench = placer.Reason;
    yield return Click();
    bool placedAtBench = !placer.Active || shelfKit == null;
    Stand(0.2f, 2.7f);
    Aim(At(-1.7f, floorY + 0.8f, 2.7f));
    yield return Frames(15);
    bool validWall = placer.Valid;
    string reasonWall = placer.Reason;
    float yawBefore = placer.Preview != null ? placer.Preview.transform.eulerAngles.y : -1f;
    log.Append($" COLOCAR: vista previa al tomar el kit={previewOn}; mirando el banco entra={validAtBench} (\"{reasonAtBench}\"), el clic no la pone={!placedAtBench || placer.Active}; mirando la pared izquierda entra={validWall} (\"{reasonWall}\").");
    // La rueda gira (mirando el piso).
    Aim(At(0.2f, floorY, 3.2f));
    yield return Frames(8);
    float yaw0 = placer.RotationDegrees;
    yield return Scroll(120f);
    yield return Scroll(120f);
    log.Append($" Mirando el piso la rueda gira: {yaw0:0}° → {placer.RotationDegrees:0}°.");
    Aim(At(-1.7f, floorY + 0.8f, 2.7f));
    yield return Frames(15);
    yield return ClickUntil(() => !placer.Active, 10);
    var placed = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Building.Placeable>();
    var storage = placed != null ? placed.GetComponent<SurvivalEnTren.Items.StorageContainer>() : null;
    var local = placed != null ? train.transform.InverseTransformPoint(placed.transform.position) : UnityEngine.Vector3.zero;
    log.Append($" CLIC contra la pared: colocado={placed != null}, ID \"{placed?.StableId}\", en el tren={placed != null && placed.transform.IsChildOf(train.transform)}, lugar (x {local.x:0.00}, z {local.z:0.00}), kit consumido={UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>().Length == 0}, en la mano={carrier.Holding != null}; cuadrícula {storage?.Grid?.Width}x{storage?.Grid?.Height}, ID del estante \"{storage?.StableId}\".");

    Step("6");
    // 6) GUARDAR ALGO ADENTRO y MOVERLO con la llave.
    storage.Grid.TryAutoPlace(cloth); storage.Grid.TryAutoPlace(cloth); storage.Grid.TryAutoPlace(metal);
    Step("6a");
    gear.Give(SurvivalEnTren.Equipment.GearId.Wrench);
    int wrenchSlot = belt.SlotOf(SurvivalEnTren.Equipment.GearId.Wrench);
    belt.Select(wrenchSlot);
    for (int i = 0; i < 100 && !belt.IsRaised; i++) yield return wait;
    Stand(0.3f, 2.0f);
    Aim(placed.transform.position + UnityEngine.Vector3.up * 1.0f);
    yield return Frames(10);
    yield return ClickUntil(() => placer.IsMoving, 30);
    bool moving = placer.IsMoving;
    Step("6b " + moving + " sel=" + belt.Selected + " swings=" + belt.Swings + " hits=" + belt.HitCount + " " + belt.LastHitName);
    Aim(At(1.7f, floorY + 0.8f, 1.2f));
    yield return Frames(15);
    bool movedValid = placer.Valid;
    yield return ClickUntil(() => !placer.IsMoving, 10);
    Step("6c");
    var local2 = train.transform.InverseTransformPoint(placed.transform.position);
    log.Append($" LLAVE: casilla {wrenchSlot + 1}, en la mano={belt.ActiveTool}; un golpe al estante lo levanta={moving}; en el lugar nuevo entra={movedValid}; clic → z {local.z:0.00} → {local2.z:0.00}, levantado={placer.IsMoving}, guardado adentro {storage.Grid.UsedCells} casillas ({storage.Grid.CountOf(cloth)} tela, {storage.Grid.CountOf(metal)} chapa).");
    Step("6d");
    // Cambiar de herramienta en medio de un movimiento lo devuelve a su lugar.
    yield return ClickUntil(() => placer.IsMoving, 30); // levanta otra vez
    bool movingAgain = placer.IsMoving;
    Aim(At(0.2f, floorY, 3.2f));
    yield return Frames(10);
    belt.Select(-1);
    yield return Frames(10);
    var local3 = train.transform.InverseTransformPoint(placed.transform.position);
    log.Append($" Levantado otra vez={movingAgain}; al guardar la llave vuelve a su lugar: z {local3.z:0.00}, levantado={placer.IsMoving}, colliders prendidos={placed.GetComponentInChildren<UnityEngine.Collider>().enabled}.");

    Step("7");
    // 7) GUARDAR: el banco con un kit a medio armar y el estante colocado con lo de adentro.
    backpack.Add(cloth, 3);
    bool forgeStarted = bench.StartJob(new SurvivalEnTren.Crafting.BenchRecipe(bedSite, 1), backpack, gear);
    save.FileName = "prueba_mejoras.json";
    save.Save();
    var data = UnityEngine.JsonUtility.FromJson<SurvivalEnTren.Save.SaveGame>(System.IO.File.ReadAllText(save.SavePath));
    System.IO.File.Delete(save.SavePath);
    if (System.IO.File.Exists(save.SavePath + ".bak")) System.IO.File.Delete(save.SavePath + ".bak");
    save.FileName = "partida.json";
    var savedShelf = data.placeables.Count > 0 ? data.placeables[0] : null;
    var savedStorage = data.storages.Find(s => s.id == placed.StableId);
    log.Append($" Clics que hubo que repetir: {retries}.");
    log.Append($" GUARDADO: banco \"{data.bench.job}\" ({data.bench.minutesLeft:0} min, empezó={forgeStarted}); muebles {data.placeables.Count} (\"{savedShelf?.key}\", del jugador={savedShelf?.placedInGame}, z {savedShelf?.position.z:0.00}); lo guardado en el estante {savedStorage?.items.Count}; obra de la cama nivel {data.sites.Find(s => s.id == "obra_tren_cama_001")?.level}.");

    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    System.IO.File.AppendAllText("Temp/prueba_h6.txt", "[Mejoras] " + log + "\n");
    UnityEngine.Debug.Log($"[Mejoras] {log}");
}
motor.StartCoroutine(Run());
return "prueba mejoras iniciada";
