// Prueba del M10 paso 4: el final de la slice, de punta a punta. Desde cero: se toma la aguja de cambio del taller ferroviario (M11; antes en la segunda sala de la
// estación, se maneja el tren hasta la bifurcación dañada (la consola la muestra BLOQUEADA), se frena antes del cambio, se baja,
// se rompe el hielo con la barreta, se desarman objetos del taller y del refugio (armarios, tablero, pallet; antes estaban al lado del cambio) y se aporta el proyecto por
// fases, se acciona la palanca ya reparada, se vuelve al tren y se entra por primera vez a la ruta desconocida.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m10_slice.cs" --timeout 60
// Resultado en la Console, líneas [Slice]. Dura unos 5 minutos reales.

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var scanner = train.GetComponent<SurvivalEnTren.Probe.ProbeScanner>();
var console = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Probe.ProbeConsole>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var ride = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeRideMetrics>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
SurvivalEnTren.Rail.RailSwitch sw = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Rail.RailSwitch>()) if (s.StableId == "rail_switch_oeste_001") sw = s;
SurvivalEnTren.Rail.SwitchLever lever = null;
foreach (var l in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Rail.SwitchLever>()) if (l.Switch == sw) lever = l;
SurvivalEnTren.Poi.FrozenDoor ice = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Poi.FrozenDoor>()) if (d.StableId == "ice_switch_oeste_001") ice = d;
SurvivalEnTren.Building.BuildSite site = null;
foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (b.StableId == "obra_cambio_oeste_001") site = b;
SurvivalEnTren.Probe.ProbeSignal fork = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Probe.ProbeSignal>()) if (s.StableId == "net_rail_oeste_001") fork = s;
var items = "Assets/_Project/Data/Items/";
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Metal.asset");
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Madera.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Componentes.asset");
var needle = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "AgujaCambio.asset");
var line1 = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.ProbeConsole).GetField("line1", flags).GetValue(console);
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

void Power(int notch) { while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1); while (controls.PowerNotch < notch) controls.MovePowerBrake(+1); while (controls.PowerNotch > notch) controls.MovePowerBrake(-1); }
void Brake(int notch) { while (controls.PowerNotch > 0) controls.MovePowerBrake(-1); while (controls.BrakeNotch < notch) controls.MovePowerBrake(-1); while (controls.BrakeNotch > notch) controls.MovePowerBrake(+1); }
string Seg() => train.RailPosition.Segment.name;
float Dist() => train.RailPosition.Distance;
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
object Target() => tInteractor.GetField("_target", flags).GetValue(who);
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
void LookAtWorld(UnityEngine.Vector3 point)
{
    var eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var d = point - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
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
System.Collections.IEnumerator Salvage(string id)
{
    SurvivalEnTren.Items.SalvageProp prop = null;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (p.StableId == id) prop = p;
    StandNear(CenterOf(prop));
    yield return Frames(40);
    while (prop != null && prop.Channel(who, 0.1f)) yield return wait;
    yield return Frames(15);
    // Lo que no entró en la mochila queda en bultos junto al objeto: se recoge.
    foreach (var bundle in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
    {
        if (bundle.Item == needle || UnityEngine.Vector3.Distance(bundle.transform.position, motor.transform.position) > 6f) continue;
        int guard = 0;
        while (bundle != null && bundle.Count > 0 && backpack.MaxAddable(bundle.Item) > 0 && guard++ < 10) { int c = bundle.Count; bundle.Interact(who); yield return wait; if (bundle != null && bundle.Count == c) break; }
    }
}
System.Collections.IEnumerator Contribute()
{
    int total = 0, g = 0;
    while (g++ < 8) { int n = 0; while (site.Channel(who, 0.55f) && n++ < 60) yield return null; yield return Frames(3); if (site.IsComplete || n == 0) break; total++; }
}
string Backpack() => $"madera {backpack.CountOf(wood)}, chapas {backpack.CountOf(metal)}, componentes {backpack.CountOf(parts)}, agujas {backpack.CountOf(needle)} ({backpack.UsedCells}/30 casillas)";

System.Collections.IEnumerator Run()
{
    float real0 = UnityEngine.Time.realtimeSinceStartup;
    double hours0 = clock.TotalHours;
    float tank0 = tank.Liters;
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    for (int i = 0; i < 60; i++) yield return wait;
    int g0 = 0; while (scanner.Target != fork && g0++ < 8) scanner.SelectNext();
    yield return Frames(30);
    log.Append($"AL EMPEZAR: cambio {sw.Condition}, la consola dice \"{line1.text}\"; obra {site.Level}/{site.Project.PhaseCount} (bloqueada={site.Locked}); mochila: {Backpack()}.");

    // 1) La pieza especial: en el taller ferroviario (M11; a pie con la sonda portátil en el juego real; acá se va directo).
    SurvivalEnTren.Items.MaterialBundle needleBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == needle) needleBundle = b;
    var bp = needleBundle.transform.position;
    motor.TeleportTo(new UnityEngine.Vector3(bp.x + 0.9f, 0.1f, bp.z), null);
    LookAtWorld(bp + new UnityEngine.Vector3(0f, 0.15f, 0f));
    yield return Frames(40);
    needleBundle.Interact(who);
    yield return Frames(10);
    log.Append($" TALLER: tomó la aguja de cambio de la mesa del taller ferroviario (en la mochila {backpack.CountOf(needle)}).");

    // 2) Volver al tren y manejar hasta la bifurcación (recta oeste, hacia -z).
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    yield return Frames(200);
    controls.TryMoveReverser(+1);
    Power(4);
    float maxJitter = 0f, t = 0f;
    string path = Seg(), last = Seg();
    while (t < 300f && !(Seg() == "Tramo_3_Recta" && Dist() > 90f))
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        maxJitter = UnityEngine.Mathf.Max(maxJitter, ride.JitterMillimeters);
        if (Seg() != last) { last = Seg(); path += " > " + last; }
    }
    // Desde 40 km/h el freno 4 detiene el tren en unos 50 m: se frena a 100 m del cambio.
    while (t < 330f && Dist() < 200f) { yield return wait; t += UnityEngine.Time.fixedDeltaTime; maxJitter = UnityEngine.Mathf.Max(maxJitter, ride.JitterMillimeters); }
    Brake(4);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;
    float toSwitch = (train.transform.position - sw.Point).magnitude;
    int g1 = 0; while (scanner.Target != fork && g1++ < 8) scanner.SelectNext();
    yield return Frames(30);
    log.Append($" MANEJANDO: recorrido {path}; temblor máx. del jugador {maxJitter:0.00} mm; el tren quedó a {toSwitch:0} m del cambio (el cambio pide >13 m libres); la consola dice \"{line1.text}\".");

    // 3) Bajar, romper el hielo.
    var stand = UnityEngine.GameObject.Find("Cambio_2_Palanca").transform;
    motor.TeleportTo(new UnityEngine.Vector3(stand.position.x + 2.0f, 0.1f, stand.position.z + 1.0f), null);
    yield return Frames(20);
    LookAtWorld(stand.position + new UnityEngine.Vector3(0f, 0.6f, 0f));
    yield return Frames(30);
    object tIce = Target();
    string icePrompt = (tIce as SurvivalEnTren.Interaction.IInteractable)?.GetPrompt(who) ?? "(sin indicación)";
    float held = 0f; int g2 = 0;
    while (ice != null && !ice.IsBroken && g2++ < 1000) { held += UnityEngine.Time.fixedDeltaTime; if (!ice.Channel(who, UnityEngine.Time.fixedDeltaTime)) break; yield return wait; }
    yield return Frames(30);
    log.Append($" HIELO: apuntando el objetivo era {Name(tIce)} \"{icePrompt}\"; se rompió en {held:0.0} s; el cambio pasó a {sw.Condition}.");

    // 4) Juntar y aportar por tandas (la mochila es de 6x5 casillas).
    yield return Salvage("salv_taller_armario_001");
    yield return Salvage("salv_taller_tablero_001");
    log.Append($" JUNTÓ armario y tablero: {Backpack()}.");
    StandNear(stand.position + new UnityEngine.Vector3(0f, 0.3f, 0f));
    yield return Frames(20);
    yield return Contribute();
    log.Append($" FASE 1: nivel {site.Level}, {Backpack()}.");
    yield return Salvage("salv_taller_armario_002");
    yield return Contribute();
    log.Append($" Más chapas → nivel {site.Level}, fracción {site.Fraction:0.00}, {Backpack()}.");
    yield return Salvage("salv_refugio_pallet_001");
    yield return Contribute();
    log.Append($" FASES 2 y 3: nivel {site.Level}/{site.Project.PhaseCount}, obra completa={site.IsComplete}, cambio {sw.Condition}, {Backpack()}.");

    // 5) Accionar la palanca (apuntada de verdad, a 1 m de la base).
    motor.TeleportTo(new UnityEngine.Vector3(stand.position.x + 1.0f, 0.1f, stand.position.z + 1.2f), null);
    LookAtWorld(stand.position + new UnityEngine.Vector3(0f, 0.15f, 0f));
    yield return Frames(30);
    object tLever = Target();
    string leverPrompt = (tLever as SurvivalEnTren.Interaction.IInteractable)?.GetPrompt(who) ?? "(sin indicación)";
    lever.Interact(who);
    yield return Frames(10);
    log.Append($" PALANCA: apuntando el objetivo es {Name(tLever)} \"{leverPrompt}\"; desvío={sw.IsDiverging}, el tren iría a {sw.ActiveExit.name}; la consola dice \"{line1.text}\".");

    // 6) Volver al tren y entrar por primera vez a la ruta desconocida.
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    yield return Frames(200);
    Power(2);
    path = Seg(); last = Seg(); t = 0f; maxJitter = 0f;
    while (t < 90f && !(Seg() == "Tramo_RutaDesconocida" && Dist() > 150f))
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        maxJitter = UnityEngine.Mathf.Max(maxJitter, ride.JitterMillimeters);
        if (Seg() != last) { last = Seg(); path += " > " + last; }
    }
    Brake(4);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;
    var inside = trainBody.transform.InverseTransformPoint(motor.transform.position);
    var here = train.transform.position;
    log.Append($" RUTA DESCONOCIDA: recorrido {path}; el tren está en {Seg()} a {Dist():0} m (ruta de {train.RailPosition.Segment.Length:0} m), en ({here.x:0}, {here.z:0}); temblor del jugador {maxJitter:0.00} mm; el jugador sigue adentro={System.Math.Abs(inside.x) < 1.7f && System.Math.Abs(inside.z) < 9.5f}.");

    double hours = clock.TotalHours - hours0;
    log.Append($" RESUMEN: {UnityEngine.Time.realtimeSinceStartup - real0:0} s reales ({hours:0.0} h de juego); tanque {tank0:0.0} → {tank.Liters:0.0} L; vida {vitals.Health:0} comida {vitals.Satiety:0} energía {vitals.Energy:0} calor {vitals.Warmth:0}.");
    UnityEngine.Debug.Log("[Slice] " + log);
}
controls.StartCoroutine(Run());
return "prueba slice iniciada";
