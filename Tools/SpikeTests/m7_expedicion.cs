// Prueba del M7 paso 4: la expedición con carga. Un jugador automático (teclado y motor reales) sale con la barreta
// y el traje, va a la estación guiado por la sonda, rompe el hielo, desmonta el pallet, el armario y el colchón,
// vuelve al tren cargado (el peso lo frena), sube por la escalera y deja todo en el piso con el panel de la mochila.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m7_expedicion.cs" --timeout 60
// Resultado en la Console, líneas [ExpedicionM7]. Dura ~5 minutos reales.


var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var scanner = train.GetComponent<SurvivalEnTren.Probe.ProbeScanner>();
var probe = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Probe.PortableProbe>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var cc = motor.GetComponent<UnityEngine.CharacterController>();
var port = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.FuelPort>();
var rack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.SuitRack>();
SurvivalEnTren.Poi.FrozenDoor frozen = null; // la puerta de la estación (desde el M10 también hay hielo en la palanca de la bifurcación)
foreach (var d in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Poi.FrozenDoor>()) if (d.StableId == "door_prueba_estacion_001") frozen = d;
var stationDoor = frozen.transform.parent.GetComponentInChildren<SurvivalEnTren.Interaction.SlidingDoor>();
var stationCenter = frozen.transform.parent.parent.position; // el marcador de la señal: centro de la estación
var can1 = UnityEngine.GameObject.Find("Bidon_1").GetComponent<SurvivalEnTren.Train.FuelCan>();
var canCarry = can1.GetComponent<SurvivalEnTren.Interaction.Carryable>();
var pickups = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Equipment.GearPickup>();
SurvivalEnTren.Equipment.GearPickup crowbar = null, pump = null;
foreach (var g in pickups) { if (g.name == "Barreta") crowbar = g; if (g.name == "Bomba_Manual") pump = g; }
SurvivalEnTren.Train.FuelSource depot = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelSource>()) if (s.StableId == "fuel_estacion_deposito_001") depot = s;
var ration = UnityEngine.GameObject.Find("Racion_Estacion").GetComponent<SurvivalEnTren.Survival.FoodItem>();
var leftDoor = UnityEngine.GameObject.Find("Puerta_Izquierda").GetComponent<SurvivalEnTren.Interaction.SlidingDoor>();
var trainText = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.PortableProbe).GetField("trainText", flags).GetValue(probe);
var targetText = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.PortableProbe).GetField("targetText", flags).GetValue(probe);
var tm = typeof(SurvivalEnTren.Player.PlayerMotor);
var toLocal = tm.GetMethod("ToReferenceLocal", flags);
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

// Caminar de verdad: tecla W virtual (el motor del jugador, con su física, hace el resto). Unity sin foco: se
// deja que las teclas lleguen igual y se restaura al terminar.
var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = inputSettings.editorInputBehaviorInPlayMode;
var oldBackground = inputSettings.backgroundBehavior;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
bool wDown = false;
void HoldW(bool on)
{
    if (on == wDown) return;
    wDown = on;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, on ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W) : new UnityEngine.InputSystem.LowLevel.KeyboardState());
}

object Target() => tInteractor.GetField("_target", flags).GetValue(who);
bool UsableWhileEngaged() => (bool)tInteractor.GetField("_usableWhileEngaged", flags).GetValue(who);
string P(SurvivalEnTren.Interaction.IInteractable i) => i.GetPrompt(who) ?? "(sin indicación)";
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
float Flat(UnityEngine.Vector3 a, UnityEngine.Vector3 b) { a.y = 0f; b.y = 0f; return UnityEngine.Vector3.Distance(a, b); }

void LookAtWorld(UnityEngine.Vector3 point)
{
    UnityEngine.Vector3 eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    UnityEngine.Vector3 dir = point - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(dir.y, new UnityEngine.Vector2(dir.x, dir.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
void PutCanInHand()
{
    UnityEngine.Vector3 eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var fwd = UnityEngine.Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f) * UnityEngine.Quaternion.Euler(look.Pitch, 0f, 0f) * UnityEngine.Vector3.forward;
    canCarry.Body.position = eye + fwd * 1.1f;
    canCarry.Body.linearVelocity = UnityEngine.Vector3.zero;
}

bool wasHolding = false;
void CheckDrop(string where)
{
    bool holding = carrier.Holding == canCarry;
    if (wasHolding && !holding) log.Append($" [SE SOLTÓ EL BIDÓN en {where}: jugador {motor.transform.position}, bidón {canCarry.transform.position}]");
    wasHolding = holding;
}
// Un paso de caminar: se gira yawDelta grados (como con el mouse) y se mantiene W apretada.
void Step(float yawDelta)
{
    float dt = UnityEngine.Time.fixedDeltaTime;
    motor.AddYaw(UnityEngine.Mathf.Clamp(yawDelta, -150f * dt, 150f * dt));
    HoldW(true);
}
float YawTo(UnityEngine.Vector3 p)
{
    var to = p - motor.transform.position; to.y = 0f;
    return UnityEngine.Vector3.SignedAngle(motor.transform.forward, to, UnityEngine.Vector3.up);
}

// Camina siguiendo una aguja hasta que el texto de la sonda diga que llegó.
System.Collections.IEnumerator WalkNeedle(System.Func<float> needle, UnityEngine.TextMesh text, string arrived, float[] r)
{
    float t = 0f, path = 0f, stuck = 0f;
    var last = motor.transform.position;
    while (t < 200f && text.text != arrived && stuck < 3f)
    {
        yield return wait;
        t += UnityEngine.Time.fixedDeltaTime;
        CheckDrop("el camino con la aguja");
        Step(needle());
        float moved = UnityEngine.Vector3.ProjectOnPlane(motor.transform.position - last, UnityEngine.Vector3.up).magnitude;
        last = motor.transform.position;
        path += moved;
        stuck = moved < 0.002f ? stuck + UnityEngine.Time.fixedDeltaTime : 0f;
    }
    HoldW(false);
    yield return wait;
    r[0] = t; r[1] = path;
}

// Camina hasta un punto del mundo (el último tramo, a la vista). Para al llegar a "stop" metros.
System.Collections.IEnumerator WalkTo(UnityEngine.Vector3 point, float stop, float[] r)
{
    float t = 0f, stuck = 0f;
    var last = motor.transform.position;
    while (t < 60f && Flat(motor.transform.position, point) > stop && stuck < 2f)
    {
        yield return wait;
        t += UnityEngine.Time.fixedDeltaTime;
        CheckDrop("camino a " + point);
        Step(YawTo(point));
        float moved = UnityEngine.Vector3.ProjectOnPlane(motor.transform.position - last, UnityEngine.Vector3.up).magnitude;
        last = motor.transform.position;
        stuck = moved < 0.002f ? stuck + UnityEngine.Time.fixedDeltaTime : 0f;
    }
    HoldW(false);
    yield return wait;
    r[0] = t;
}

// ---- Desde acá, lo propio de la expedición del M7 (las ayudas de arriba vienen de la expedición del M6).
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Componentes.asset");
var allItems = new[] { wood, metal, cloth, parts };
int[] Units()
{
    var u = new int[4];
    for (int k = 0; k < 4; k++) u[k] = backpack.CountOf(allItems[k]);
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        for (int k = 0; k < 4; k++) if (b.Item == allItems[k]) u[k] += b.Count;
    return u;
}
string Show(int[] u) => $"madera {u[0]}, metal {u[1]}, tela {u[2]}, componentes {u[3]}";
UnityEngine.Vector3 CenterOf(UnityEngine.Component c)
{
    var rs = c.GetComponentsInChildren<UnityEngine.Renderer>();
    var bounds = rs[0].bounds;
    foreach (var rr in rs) bounds.Encapsulate(rr.bounds);
    return bounds.center;
}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key key)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
    yield return wait; yield return wait; yield return wait;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    yield return wait; yield return wait; yield return wait;
}
// Se para a ~1,4 m del objeto (del lado de "from") y lo mira al centro; desmonta con Channel() como el interactor.
System.Collections.IEnumerator Salvage(SurvivalEnTren.Items.SalvageProp prop, UnityEngine.Vector3 from, int[] gained)
{
    var recipe = prop.Recipe;
    var center = CenterOf(prop);
    var dir = from - center; dir.y = 0f; dir.Normalize();
    var stand = center + dir * 1.4f;
    motor.TeleportTo(new UnityEngine.Vector3(stand.x, 0.1f, stand.z), null);
    LookAtWorld(center);
    for (int i = 0; i < 30; i++) yield return wait;
    object t = Target();
    float t0 = 0f;
    while (prop != null && prop.Channel(who, UnityEngine.Time.fixedDeltaTime)) { yield return wait; t0 += UnityEngine.Time.fixedDeltaTime; }
    for (int i = 0; i < 10; i++) yield return wait;
    bool gone = prop == null;
    if (gone) foreach (var y in recipe.Yields) for (int k = 0; k < 4; k++) if (allItems[k] == y.item) gained[k] += y.count;
    log.Append($" [{recipe.DisplayName}: objetivo {Name(t)}, {t0:0.0} s, ya no existe={gone}]");
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 1f;
    for (int i = 0; i < 30; i++) yield return wait;
    double hours0 = clock.TotalHours;
    var r = new float[2];
    var gained = new int[4];
    vitals.Died += () => log.Append($" [SE DESMAYÓ: vida {vitals.Health:0} calor {vitals.Warmth:0}, a las {clock.TotalHours - hours0:0.00} h]");
    var before = Units();
    log.Append($"Inicio: mochila {backpack.MassKg:0.0} kg; material suelto en bultos: {Show(before)}.");

    // 1) Prepararse: barreta y traje; salir por la puerta izquierda.
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-0.5f, 1.05f, -6.7f), trainBody);
    LookAtWorld(crowbar.transform.position);
    for (int i = 0; i < 30; i++) yield return wait;
    crowbar.Interact(who);
    rack.Interact(who);
    leftDoor.Interact(who);
    for (int i = 0; i < 60; i++) yield return wait;
    var outside = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-4.2f, 0f, -6f);
    motor.TeleportTo(new UnityEngine.Vector3(outside.x, 0.1f, outside.z), null);
    LookAtWorld(stationCenter);

    // 2) Ir a la estación guiado por la sonda, romper el hielo y entrar.
    probe.SetOut(true);
    int guard = 0;
    while (scanner.Target == null || scanner.Target.StableId != "poi_prueba_rail1a_001") { scanner.SelectNext(); if (++guard > 6) break; }
    for (int i = 0; i < 60; i++) yield return wait;
    yield return WalkNeedle(() => probe.TargetNeedleDegrees, targetText, "OBJ.  ~10 M", r);
    var doorFront = stationCenter + new UnityEngine.Vector3(5.2f, 0f, 0f);
    yield return WalkTo(doorFront, 0.1f, r);
    LookAtWorld(stationCenter + new UnityEngine.Vector3(4.4f, 1.1f, 0f));
    for (int i = 0; i < 40; i++) yield return wait;
    while (frozen != null && frozen.Channel(who, UnityEngine.Time.fixedDeltaTime)) yield return wait;
    for (int i = 0; i < 10; i++) yield return wait;
    stationDoor.Interact(who);
    for (int i = 0; i < 60; i++) yield return wait;
    yield return WalkTo(stationCenter + new UnityEngine.Vector3(4.6f, 0f, 0f), 0.15f, r);
    yield return WalkTo(stationCenter + new UnityEngine.Vector3(1.6f, 0f, 0f), 0.4f, r);
    log.Append($" ENTRÓ a la estación: jugador a {Flat(stationCenter, motor.transform.position):0.0} m del centro, en {motor.transform.position}, puerta abierta={stationDoor.IsOpen}, aire {vitals.AmbientTemperatureC:0} °C.");

    // 3) Desmontar el pallet, el armario y el colchón (en ese orden).
    var props = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>();
    SurvivalEnTren.Items.SalvageProp pallet = null, locker = null, mattress = null;
    foreach (var p in props)
    {
        if (p.StableId == "salv_estacion_pallet_001") pallet = p;
        else if (p.StableId == "salv_estacion_armario_001") locker = p;
        else if (p.StableId == "salv_estacion_colchon_001") mattress = p;
    }
    var east = stationCenter + new UnityEngine.Vector3(4f, 0f, 0f);
    yield return Salvage(pallet, east, gained);
    yield return Salvage(locker, east, gained);
    yield return Salvage(mattress, east, gained);
    float loaded = backpack.MassKg;
    for (int i = 0; i < 20; i++) yield return wait;
    log.Append($" CARGADO: mochila {loaded:0.0} kg, {backpack.UsedCells}/{backpack.CellCount} casillas ({Show(new[] { backpack.CountOf(wood), backpack.CountOf(metal), backpack.CountOf(cloth), backpack.CountOf(parts) })}); caminar x{motor.SpeedMultiplier:0.00}.");

    // 4) Volver al tren siguiendo la aguja blanca, cargado, y subir por la escalera.
    yield return WalkTo(stationCenter + new UnityEngine.Vector3(4.6f, 0f, 0f), 0.15f, r);
    yield return WalkTo(doorFront, 0.4f, r);
    yield return WalkTo(stationCenter + new UnityEngine.Vector3(8.5f, 0f, 0f), 0.4f, r); // seguir derecho unos metros antes de girar hacia el tren (el bidón queda a la cadera: si se gira pegado a la pared, se traba)
    float straightBack = Flat(trainBody.position, motor.transform.position);
    yield return WalkNeedle(() => probe.TrainNeedleDegrees, trainText, "TREN  ~10 M", r);
    log.Append($" VUELTA CARGADA: {r[0]:0} s, caminó {r[1]:0} m para {straightBack:0} m en línea recta (velocidad x{motor.SpeedMultiplier:0.00}).");
    var leftDoorOutside = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-4.2f, 0f, -6f);
    yield return WalkTo(new UnityEngine.Vector3(leftDoorOutside.x, 0.1f, leftDoorOutside.z), 0.3f, r);
    var inside = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-0.4f, 1.05f, -6f);
    float climb = 0f;
    while (Flat(motor.transform.position, inside) > 0.5f && climb < 20f) { yield return wait; climb += UnityEngine.Time.fixedDeltaTime; Step(YawTo(inside)); }
    HoldW(false);
    var localNow = trainBody.transform.InverseTransformPoint(motor.transform.position);
    log.Append($" SUBIÓ AL TREN cargado: adentro={localNow.y > 0.9f && System.Math.Abs(localNow.x) < 1.6f} (local {localNow.x:0.00}, {localNow.y:0.00}, {localNow.z:0.00}) en {climb:0.0} s.");

    // 5) Dejar todo en el piso del tren con el panel (Tab y Supr, con el teclado real): queda en bultos adentro.
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    bool opened = panel.IsOpen;
    foreach (var item in allItems) panel.DropAll(item); // soltar todo, material por material (lo mismo que hace Supr)

    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    for (int i = 0; i < 90; i++) yield return wait;
    int insideCount = 0, total = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
    {
        var l = trainBody.transform.InverseTransformPoint(b.transform.position);
        if (b.Item != null && b.transform.parent == null)
        {
            total++;
            if (System.Math.Abs(l.x) < 1.7f && l.z > -7.5f && l.z < 9.5f && l.y > 0.9f) insideCount++;
        }
    }
    log.Append($" DEJAR EN EL TREN: panel abierto={opened}, mochila vacía={backpack.Grid.Items.Count == 0}, caminar x{motor.SpeedMultiplier:0.00}; bultos sueltos en el mundo (sin contar los de la estación) {total}, de los cuales adentro del tren {insideCount}.");

    // 6) Cuentas: lo que había + lo desmontado = lo que hay (mochila + bultos); nada se duplicó ni se perdió.
    var after = Units();
    var expected = new int[4];
    for (int k = 0; k < 4; k++) expected[k] = before[k] + gained[k];
    bool same = true;
    for (int k = 0; k < 4; k++) same &= after[k] == expected[k];
    double elapsed = clock.TotalHours - hours0;
    log.Append($" CUENTAS: hay {Show(after)}; esperado {Show(expected)}; sin duplicar ni perder={same}. RESUMEN: {elapsed:0.00} h de juego ({elapsed * 60:0} s reales); vida {vitals.Health:0} comida {vitals.Satiety:0} energía {vitals.Energy:0} calor {vitals.Warmth:0}.");

    HoldW(false);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    UnityEngine.Debug.Log("[ExpedicionM7] " + log);
}
motor.StartCoroutine(Run());
return "prueba expedición M7 iniciada";
