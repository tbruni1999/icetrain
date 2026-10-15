// Prueba del M6 paso 4: la primera expedición completa (sección 24, fase 4: "completar primera expedición").
// Un jugador automático sale del tren con la barreta, el traje y un bidón, va a la estación guiado solo por
// la sonda portátil, rompe el hielo de la puerta, entra, saca la bomba manual, llena el bidón del depósito,
// vuelve al tren siguiendo la aguja blanca y carga el tanque por la boca de carga.
// Las acciones de mantener E se simulan llamando a Channel() cada tic (lo mismo que hace el interactor);
// el camino real de la tecla E se probó aparte (m6_interactor_e.cs).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m6_expedicion.cs" --timeout 60
// Resultado en la Console, líneas [Expedicion]. Dura ~4 minutos reales (reloj del juego a velocidad normal).

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
    if (wasHolding && !holding)
    {
        string near = "";
        foreach (var c in UnityEngine.Physics.OverlapSphere(canCarry.transform.position, 0.8f, ~0, UnityEngine.QueryTriggerInteraction.Ignore))
            if (!c.transform.IsChildOf(canCarry.transform) && c.gameObject != motor.gameObject) near += (c.transform.parent != null ? c.transform.parent.name + "/" : "") + c.name + " ";
        log.Append($" [SE SOLTÓ EL BIDÓN en {where}: jugador {motor.transform.position}, bidón {canCarry.transform.position}, toca: {near}]");
    }
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

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 1f;
    for (int i = 0; i < 30; i++) yield return wait;
    double hours0 = clock.TotalHours; float fuel0 = tank.Liters;
    var r = new float[2];
    vitals.Died += () => log.Append($" [SE DESMAYÓ: vida {vitals.Health:0} calor {vitals.Warmth:0} aire {vitals.AmbientTemperatureC:0} °C, a las {clock.TotalHours - hours0:0.00} h, en {motor.transform.position}]");
    log.Append($"Inicio: tanque {fuel0:0.0} L, vida {vitals.Health:0} comida {vitals.Satiety:0} energía {vitals.Energy:0} calor {vitals.Warmth:0}.");

    // 0) Sin barreta, el hielo no cede.
    string iceNoCrowbar = P(frozen);
    bool channelNoCrowbar = frozen.Channel(who, 0.1f);
    log.Append($" SIN BARRETA: \"{iceNoCrowbar}\", avanza={channelNoCrowbar}.");

    // 1) Prepararse en el tren: barreta, traje, bidón.
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-0.5f, 1.05f, -6.7f), trainBody);
    LookAtWorld(crowbar.transform.position);
    for (int i = 0; i < 30; i++) yield return wait;
    crowbar.Interact(who);
    rack.Interact(who);
    LookAtWorld(canCarry.transform.position + UnityEngine.Vector3.up * 0.2f);
    for (int i = 0; i < 20; i++) yield return wait;
    canCarry.Interact(who);
    for (int i = 0; i < 30; i++) yield return wait;
    log.Append($" EQUIPO: barreta={gear.Has(SurvivalEnTren.Equipment.GearId.Crowbar)}, traje puesto={gear.SuitWorn}, bidón en la mano={carrier.Holding == canCarry} ({canCarry.Body.mass:0.0} kg vacío).");

    // 2) Bajar del tren (la bajada por la escalera ya está probada en M1): se sale por la puerta izquierda.
    leftDoor.Interact(who);
    for (int i = 0; i < 60; i++) yield return wait;
    var outside = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-4.2f, 0f, -6f);
    motor.TeleportTo(new UnityEngine.Vector3(outside.x, 0.1f, outside.z), null);
    LookAtWorld(stationCenter);
    PutCanInHand();
    for (int i = 0; i < 20; i++) { PutCanInHand(); yield return wait; }

    // 3) Ir a la estación solo con la aguja ámbar.
    probe.SetOut(true);
    int guard = 0;
    while (scanner.Target == null || scanner.Target.StableId != "poi_prueba_rail1a_001") { scanner.SelectNext(); if (++guard > 6) break; }
    for (int i = 0; i < 60; i++) yield return wait;
    log.Append($" SONDA con el bidón en la mano: sacada={probe.IsOut}, \"{trainText.text}\" / \"{targetText.text}\".");
    float straight = Flat(stationCenter, motor.transform.position);
    yield return WalkNeedle(() => probe.TargetNeedleDegrees, targetText, "OBJ.  ~10 M", r);
    log.Append($" IDA: {r[0]:0} s, caminó {r[1]:0} m para {straight:0} m en línea recta; terminó a {Flat(stationCenter, motor.transform.position):0} m de la estación.");

    // 4) Llegar a la puerta (a la vista) y romper el hielo.
    var doorFront = stationCenter + new UnityEngine.Vector3(5.2f, 0f, 0f);
    yield return WalkTo(doorFront, 0.1f, r); // alineado con la puerta (el vano mide 1,1 m)
    LookAtWorld(stationCenter + new UnityEngine.Vector3(4.4f, 1.1f, 0f));
    for (int i = 0; i < 40; i++) { yield return wait; }
    object tIce = Target();
    log.Append($" [bidón en la mano={carrier.Holding == canCarry}]"); log.Append($" PUERTA: apuntando al marco el objetivo es {Name(tIce)}, indicación \"{P(frozen)}\".");
    float breakTime = 0f; string halfPrompt = "";
    while (frozen != null && frozen.Channel(who, UnityEngine.Time.fixedDeltaTime))
    {
        yield return wait; breakTime += UnityEngine.Time.fixedDeltaTime;
        if (breakTime >= 2f && halfPrompt == "") halfPrompt = P(frozen);

    }
    for (int i = 0; i < 10; i++) { yield return wait; }
    log.Append($" HIELO: se rompió en {breakTime:0.0} s, a los 2 s la indicación decía \"{halfPrompt}\"; el hielo ya no está={frozen == null}.");
    stationDoor.Interact(who);
    for (int i = 0; i < 60; i++) yield return wait;
    log.Append($" PUERTA: abierta={stationDoor.IsOpen}, bidón en la mano={carrier.Holding == canCarry}.");

    // 5) Entrar: frío sin viento y la sonda pierde señal.
    yield return WalkTo(stationCenter + new UnityEngine.Vector3(4.6f, 0f, 0f), 0.15f, r);
    yield return WalkTo(stationCenter + new UnityEngine.Vector3(1.6f, 0f, 0f), 0.4f, r);
    for (int i = 0; i < 60; i++) { yield return wait; }
    log.Append($" ADENTRO (bidón en la mano={carrier.Holding == canCarry}): llegó={Flat(stationCenter, motor.transform.position) < 2.5f} (a {Flat(stationCenter, motor.transform.position):0.0} m del centro, en {motor.transform.position}), aire {vitals.AmbientTemperatureC:0} °C (afuera -25), interferencia de la sonda {probe.Interference:0.00}, calidad {probe.Quality:0.00}.");

    // 6) Bomba manual y ración (con la mano ocupada por el bidón, E toca los objetos que se usan sin soltarlo).
    pump.Interact(who);
    float satiety0 = vitals.Satiety;
    ration.Interact(who);
    log.Append($" BOTÍN: bomba manual={gear.Has(SurvivalEnTren.Equipment.GearId.HandPump)}, comió una ración (comida {satiety0:0} → {vitals.Satiety:0}).");

    // 7) Llenar el bidón en el depósito con la bomba.
    var valve = depot.transform.position + new UnityEngine.Vector3(0f, 0f, -0.7f);
    yield return WalkTo(new UnityEngine.Vector3(valve.x, 0f, valve.z - 0.9f), 0.3f, r);
    LookAtWorld(valve);
    for (int i = 0; i < 40; i++) { yield return wait; }
    object tDepot = Target();
    string rayInfo = ""; { var cam = UnityEngine.Camera.main.transform; var hs = UnityEngine.Physics.RaycastAll(cam.position, cam.forward, 3f, ~0, UnityEngine.QueryTriggerInteraction.Collide); System.Array.Sort(hs, (a, b) => a.distance.CompareTo(b.distance)); foreach (var h in hs) rayInfo += h.collider.name + "(" + h.distance.ToString("0.00") + ") "; rayInfo += " jugador " + motor.transform.position + " mira " + cam.forward; }
    string pumpPrompt = P(depot);
    float depot0 = depot.Liters; float pumpTime = 0f;
    while (depot.Channel(who, UnityEngine.Time.fixedDeltaTime)) { yield return wait; pumpTime += UnityEngine.Time.fixedDeltaTime; }
    log.Append($" DEPÓSITO: [rayo: {rayInfo}] objetivo {Name(tDepot)}, usable con el bidón en la mano={UsableWhileEngaged()}, indicación \"{pumpPrompt}\"; bombeó {depot0 - depot.Liters:0.0} L en {pumpTime:0.0} s; bidón {can1.Liters:0.0} L ({canCarry.Body.mass:0.0} kg, caminar x{motor.SpeedMultiplier:0.00}); quedan {depot.Liters:0} L en el depósito.");

    // 8) Salir y volver al tren solo con la aguja blanca.
    yield return WalkTo(stationCenter + new UnityEngine.Vector3(4.6f, 0f, 0f), 0.15f, r);
    yield return WalkTo(doorFront, 0.4f, r);
    yield return WalkTo(stationCenter + new UnityEngine.Vector3(8.5f, 0f, 0f), 0.4f, r); // seguir derecho unos metros antes de girar hacia el tren (el bidón queda a la cadera: si se gira pegado a la pared, se traba)
    float straightBack = Flat(trainBody.position, motor.transform.position);
    yield return WalkNeedle(() => probe.TrainNeedleDegrees, trainText, "TREN  ~10 M", r);
    log.Append($" VUELTA con el bidón lleno: {r[0]:0} s, caminó {r[1]:0} m para {straightBack:0} m en línea recta, terminó a {Flat(trainBody.position, motor.transform.position):0} m del centro del tren.");

    // 9) Cargar el tanque por la boca de carga.
    // Rodear el tren por atrás hasta la boca de carga (el costado derecho).
    foreach (var corner in new[] { new UnityEngine.Vector3(-3.6f, 0f, -11.5f), new UnityEngine.Vector3(3.6f, 0f, -11.5f) })
    {
        var w = trainBody.position + trainBody.rotation * corner;
        yield return WalkTo(new UnityEngine.Vector3(w.x, 0.1f, w.z), 0.5f, r);
    }
    var portSpot = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(3.3f, 0f, -8.6f);
    yield return WalkTo(new UnityEngine.Vector3(portSpot.x, 0.1f, portSpot.z), 0.4f, r);
    LookAtWorld(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(1.5f, 0.55f, -8.6f));
    for (int i = 0; i < 40; i++) { yield return wait; }
    object tPort = Target();
    float tank0 = tank.Liters; float pourTime = 0f;
    string pourPrompt = P(port);
    while (port.Channel(who, UnityEngine.Time.fixedDeltaTime)) { yield return wait; pourTime += UnityEngine.Time.fixedDeltaTime; }
    log.Append($" CARGA: objetivo {Name(tPort)}, indicación \"{pourPrompt}\"; el tanque pasó de {tank0:0.0} a {tank.Liters:0.0} L en {pourTime:0.0} s.");

    double elapsed = clock.TotalHours - hours0;
    log.Append($" RESUMEN: {elapsed:0.00} h de juego ({elapsed * 60:0} s reales, porque 1 s real = 1 min de juego); tanque {fuel0:0.0} → {tank.Liters:0.0} L (la calefacción gastó {elapsed * 1.5:0.0} L mientras tanto); vida {vitals.Health:0} comida {vitals.Satiety:0} energía {vitals.Energy:0} calor {vitals.Warmth:0}; depósito {depot.Liters:0} L.");
    HoldW(false);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    UnityEngine.Debug.Log("[Expedicion] " + log);
}
motor.StartCoroutine(Run());
return "prueba expedición iniciada";
