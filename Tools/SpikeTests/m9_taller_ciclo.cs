// Prueba del M9 paso 4: el ciclo del taller de punta a punta. Se sale con la barreta a juntar madera y chapas, se lee el plano
// de la estación, se vuelve al tren, se fabrica la sierra y después el bidón en el banco (con el teclado de verdad), y ese bidón
// fabricado se usa de verdad: se toma con E, se llena con el sifón del vehículo y se carga al tanque del tren.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s), con la ventana de Unity con foco,
//   unity command eval_file --file "Tools/SpikeTests/m9_taller_ciclo.cs" --timeout 60
// Resultado en la Console, líneas [TallerCiclo].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var book = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>();
var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.WorkbenchPanel>();
var port = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.FuelPort>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var sheet = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintPickup>();
var shelfPlan = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_EstanteReforzado.asset");
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
SurvivalEnTren.Train.FuelSource vehicle = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelSource>()) if (s.StableId == "fuel_prueba_vehiculo_001") vehicle = s;
SurvivalEnTren.Building.BuildSite shelfSite = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (s.StableId == "obra_tren_estante_001") shelfSite = s;
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
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }

object Target() => typeof(SurvivalEnTren.Interaction.PlayerInteractor).GetField("_target", flags).GetValue(who);
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
void LookAtWorld(UnityEngine.Vector3 point)
{
    var eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var d = point - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
void FaceInTrain(UnityEngine.Vector3 standLocal, UnityEngine.Vector3 lookLocal)
{
    motor.TeleportTo(trainBody.position + trainBody.rotation * standLocal, trainBody);
    LookAtWorld(trainBody.position + trainBody.rotation * lookLocal);
}
UnityEngine.Vector3 CenterOf(UnityEngine.Component c)
{
    var rs = c.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs[0].bounds;
    foreach (var r in rs) b.Encapsulate(r.bounds);
    return b.center;
}
void StandNear(UnityEngine.Vector3 target)
{
    var stand = target + new UnityEngine.Vector3(1.2f, 0f, 0f);
    motor.TeleportTo(new UnityEngine.Vector3(stand.x, 0.1f, stand.z), null);
    LookAtWorld(target);
}
bool IsOutside(UnityEngine.Component c) => UnityEngine.Vector3.Distance(c.transform.position, trainBody.position) > 25f;
SurvivalEnTren.Interaction.Carryable canCarry = null;
void PutCanInHand()
{
    var eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var fwd = UnityEngine.Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f) * UnityEngine.Quaternion.Euler(look.Pitch, 0f, 0f) * UnityEngine.Vector3.forward;
    canCarry.Body.position = eye + fwd * 1.1f;
    canCarry.Body.linearVelocity = UnityEngine.Vector3.zero;
}

void Finish()
{
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[TallerCiclo] " + log);
}
System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    float tank0 = tank.Liters;
    log.Append($"AL EMPEZAR: sierra {gear.Has(SurvivalEnTren.Equipment.GearId.Saw)}, mochila {backpack.UsedCells} casillas; plano del estante {book.Knows(shelfPlan)}; obra del estante bloqueada={shelfSite.Locked}.");

    // 1) Afuera: desmontar y juntar hasta tener 1 tablón y 3 chapas (sierra 1 + chapa, bidón 2 chapas).
    var targets = new System.Collections.Generic.List<UnityEngine.Component>();
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) targets.Add(p);
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (IsOutside(b)) targets.Add(b);
    // Primero lo que da chapas (lo más escaso), después lo que da madera; solo se visita lo que todavía hace falta.
    bool Gives(UnityEngine.Component c, SurvivalEnTren.Items.ItemDefinition item)
    {
        if (c is SurvivalEnTren.Items.SalvageProp sp) { foreach (var y in sp.Recipe.Yields) if (y.item == item) return true; return false; }
        if (c is SurvivalEnTren.Items.MaterialBundle mb) return mb.Item == item;
        return false;
    }
    targets.Sort((a, b) => (Gives(b, metal) ? 1 : 0).CompareTo(Gives(a, metal) ? 1 : 0));
    int visited = 0;
    foreach (var target in targets)
    {
        if (backpack.CountOf(wood) >= 1 && backpack.CountOf(metal) >= 3) break;
        if (target == null) continue;
        bool needMetal = backpack.CountOf(metal) < 3, needWood = backpack.CountOf(wood) < 1;
        if (!((needMetal && Gives(target, metal)) || (needWood && Gives(target, wood)))) continue;
        if (backpack.UsedCells >= 26) break;
        StandNear(CenterOf(target));
        yield return Frames(30);
        if (target is SurvivalEnTren.Items.SalvageProp prop)
        {
            while (prop != null && prop.Channel(who, 0.1f)) yield return wait;
            yield return Frames(10);
        }
        else if (target is SurvivalEnTren.Items.MaterialBundle bundle)
        {
            while (bundle != null && bundle.Count > 0 && backpack.MaxAddable(bundle.Item) > 0) { int c = bundle.Count; bundle.Interact(who); yield return wait; if (bundle != null && bundle.Count == c) break; }
        }
        visited++;
    }
    int woodGot = backpack.CountOf(wood), metalGot = backpack.CountOf(metal);
    log.Append($" AFUERA: visitó {visited} lugares y juntó tablones {woodGot}, chapas {metalGot} (necesita 1 y 3).");

    // 2) El plano de la estación: se lee con E y la obra del estante aparece.
    var sheetPos = sheet.transform.position;
    motor.TeleportTo(new UnityEngine.Vector3(sheetPos.x + 1.2f, 0.1f, sheetPos.z), null);
    LookAtWorld(sheetPos);
    yield return Frames(40);
    object tSheet = Target();
    sheet.Interact(who);
    yield return Frames(5);
    log.Append($" PLANO: apuntando la hoja el objetivo era {Name(tSheet)}; ahora el plano del estante {book.Knows(shelfPlan)}, la obra bloqueada={shelfSite.Locked}.");

    // 3) De vuelta en el tren, al banco: E, sierra, bidón.
    FaceInTrain(new UnityEngine.Vector3(-0.2f, 1.05f, 0f), new UnityEngine.Vector3(-1.1f, 1.9f, 0f));
    yield return Frames(40);
    var known = new System.Collections.Generic.HashSet<SurvivalEnTren.Train.FuelCan>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelCan>());
    int woodBefore = backpack.CountOf(wood), metalBefore = backpack.CountOf(metal);
    yield return Tap(UnityEngine.InputSystem.Key.E);
    yield return Frames(4);
    bool opened = panel.IsOpen;
    // Sierra.
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    int guard = 0;
    while (panel.IsCrafting && guard++ < 1500) yield return wait;
    bool sawNow = gear.Has(SurvivalEnTren.Equipment.GearId.Saw);
    // Bidón.
    yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    string beforeCan = panel.CurrentBlock.ToString();
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    guard = 0;
    while (panel.IsCrafting && guard++ < 1500) yield return wait;
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    yield return Frames(80);
    SurvivalEnTren.Train.FuelCan fresh = null;
    foreach (var c in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelCan>()) if (!known.Contains(c)) fresh = c;
    log.Append($" BANCO: abrió con E={opened}; fabricó la sierra={sawNow} (colgada en el taller); antes del bidón el bloqueo era {beforeCan}; materiales: tablón {woodBefore}→{backpack.CountOf(wood)}, chapa {metalBefore}→{backpack.CountOf(metal)} (gastó 1 tablón y 3 chapas: {woodBefore - backpack.CountOf(wood) == 1 && metalBefore - backpack.CountOf(metal) == 3}); bidón nuevo sobre la mesa={fresh != null}.");
    if (fresh == null) { Finish(); yield break; }

    // 4) Usar el bidón fabricado: tomarlo con E.
    canCarry = fresh.GetComponent<SurvivalEnTren.Interaction.Carryable>();
    FaceInTrain(new UnityEngine.Vector3(-0.2f, 1.05f, 0f), fresh.transform.position + UnityEngine.Vector3.up * 0.2f - trainBody.position);
    LookAtWorld(fresh.transform.position + UnityEngine.Vector3.up * 0.2f);
    yield return Frames(40);
    object tCan = Target();
    string canPrompt = (tCan as SurvivalEnTren.Interaction.IInteractable)?.GetPrompt(who) ?? "(sin indicación)";
    yield return Tap(UnityEngine.InputSystem.Key.E);
    yield return Frames(20);
    bool held = carrier.Holding == canCarry;
    log.Append($" BIDÓN: apuntándolo el objetivo es {Name(tCan)}, indicación \"{canPrompt}\"; lo tomó con E={held}.");

    // 5) Llevarlo al vehículo y sifonar (E mantenido).
    var tankPos = vehicle.transform.position;
    motor.TeleportTo(new UnityEngine.Vector3((tankPos + vehicle.transform.right * 1.6f).x, 0.1f, (tankPos + vehicle.transform.right * 1.6f).z), null);
    LookAtWorld(tankPos);
    for (int i = 0; i < 20; i++) { PutCanInHand(); yield return wait; }
    float v0 = vehicle.Liters;
    for (int i = 0; i < 250; i++) { PutCanInHand(); vehicle.Channel(who, UnityEngine.Time.fixedDeltaTime); yield return wait; }
    float v1 = vehicle.Liters;
    log.Append($" SIFÓN: el vehículo pasó de {v0:0.0} a {v1:0.0} L; el bidón fabricado tiene {fresh.Liters:0.0} L (de {fresh.Capacity:0} L).");

    // 6) Cargar el tanque del tren por la boca de carga (costado derecho, atrás).
    var portSpot = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(3.3f, 0f, -8.6f);
    motor.TeleportTo(new UnityEngine.Vector3(portSpot.x, 0.1f, portSpot.z), null);
    LookAtWorld(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(1.5f, 0.55f, -8.6f));
    for (int i = 0; i < 20; i++) { PutCanInHand(); yield return wait; }
    float liters = fresh.Liters;
    guard = 0;
    while (guard++ < 1500) { PutCanInHand(); if (!port.Channel(who, UnityEngine.Time.fixedDeltaTime)) break; yield return wait; }
    log.Append($" CARGA: el tanque pasó de {tank0:0.0} a {tank.Liters:0.0} L (+{tank.Liters - tank0:0.0} de los {liters:0.0} L del bidón); el bidón quedó con {fresh.Liters:0.0} L.");
    Finish();
}
motor.StartCoroutine(Run());
return "prueba taller ciclo iniciada";
