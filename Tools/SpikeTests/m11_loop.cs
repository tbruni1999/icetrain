// Prueba del M11 paso 2: el loop completo de la V1, repetido en viajes hasta abrir la ruta (Anexo G, M11). Desde cero y con el
// tren manejado de verdad (palancas por código, física real):
//   viaje 1, estación: chapas (armario + bulto) y componentes (tablero);
//   viaje 2, taller ferroviario: la aguja de cambio;
//   viaje 3, bifurcación y refugio: romper el hielo, aportar lo que se trae, ir al refugio por la madera, volver, terminar la
//   reparación, accionar la palanca y entrar con el tren a la ruta desconocida.
// Las caminatas entre el tren y cada lugar se hacen de un salto (las caminatas reales con la sonda ya las prueban m5/m6/m7/m10);
// lo que se mide es el loop: qué se junta, qué se gasta, combustible, cuerpo y el tiempo por cuadro de cada viaje (sección 25.2:
// "repetir el loop varias veces sin degradación de rendimiento").
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m11_loop.cs" --timeout 60
// Resultado en la Console, líneas [Loop]. Dura unos 6 minutos reales.

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
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
var items = "Assets/_Project/Data/Items/";
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Metal.asset");
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Madera.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Componentes.asset");
var needle = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "AgujaCambio.asset");
var seg1a = UnityEngine.GameObject.Find("Tramo_1a_Recta").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var seg1b = UnityEngine.GameObject.Find("Tramo_1b_Recta").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var seg3 = UnityEngine.GameObject.Find("Tramo_3_Recta").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var station = UnityEngine.GameObject.Find("Estacion_Mantenimiento").transform;
var workshop = UnityEngine.GameObject.Find("Taller_Ferroviario").transform;
var shelter = UnityEngine.GameObject.Find("Refugio").transform;
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

void Power(int notch) { while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1); while (controls.PowerNotch < notch) controls.MovePowerBrake(+1); while (controls.PowerNotch > notch) controls.MovePowerBrake(-1); }
void Brake(int notch) { while (controls.PowerNotch > 0) controls.MovePowerBrake(-1); while (controls.BrakeNotch < notch) controls.MovePowerBrake(-1); while (controls.BrakeNotch > notch) controls.MovePowerBrake(+1); }
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
object Target() => tInteractor.GetField("_target", flags).GetValue(who);
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
float Flat(UnityEngine.Vector3 a, UnityEngine.Vector3 b) { a.y = 0f; b.y = 0f; return UnityEngine.Vector3.Distance(a, b); }
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
void StandNear(UnityEngine.Vector3 target, float dx = 1.2f)
{
    motor.TeleportTo(new UnityEngine.Vector3(target.x + dx, 0.1f, target.z), null);
    LookAtWorld(target);
}
string Pack() => $"chapas {backpack.CountOf(metal)}, componentes {backpack.CountOf(parts)}, madera {backpack.CountOf(wood)}, agujas {backpack.CountOf(needle)} ({backpack.UsedCells}/30 casillas)";

// Metros por la vía hacia adelante hasta un punto (siguiendo los cambios como están).
float AheadTo(SurvivalEnTren.Rail.RailSegment target, float targetDist)
{
    var seg = train.RailPosition.Segment;
    float d = train.RailPosition.Distance;
    if (seg == target && targetDist >= d) return targetDist - d;
    float sum = seg.Length - d;
    seg = seg.Next;
    int guard = 0;
    while (seg != null && guard++ < 20)
    {
        if (seg == target) return sum + targetDist;
        sum += seg.Length;
        seg = seg.Next;
    }
    return float.PositiveInfinity;
}

// Maneja hasta un punto de la vía: potencia 4 y freno 4 justo a tiempo (con la física del tren).
float maxJitter = 0f;
string route = "";
System.Collections.IEnumerator DriveTo(SurvivalEnTren.Rail.RailSegment target, float targetDist, float[] result)
{
    float t = 0f;
    string last = train.RailPosition.Segment.name;
    route = last;
    controls.TryMoveReverser(+1); controls.TryMoveReverser(+1);
    Power(4);
    var settings = controls.Settings;
    float decel = settings.BrakeDeceleration(4) + settings.RollingResistanceMetersPerSecondSquared;
    while (t < 400f)
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        maxJitter = UnityEngine.Mathf.Max(maxJitter, ride.JitterMillimeters);
        string now = train.RailPosition.Segment.name;
        if (now != last) { last = now; route += " > " + now; }
        float v = train.SpeedMetersPerSecond;
        if (AheadTo(target, targetDist) <= v * v / (2f * decel) + 2f) break;
    }
    Brake(4);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) { yield return wait; t += UnityEngine.Time.fixedDeltaTime; }
    result[0] = t;
    float left = AheadTo(target, targetDist);
    result[1] = float.IsInfinity(left) ? -1f : left;
}

System.Collections.IEnumerator Salvage(string id, SurvivalEnTren.Items.ItemDefinition only)
{
    SurvivalEnTren.Items.SalvageProp prop = null;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (p.StableId == id) prop = p;
    if (prop == null) yield break;
    StandNear(CenterOf(prop));
    yield return Frames(40);
    yield return HitDown(prop);
    yield return Frames(15);
    yield return PickBundlesNear(motor.transform.position, 6f, only); // solo lo que se vino a buscar: la mochila es chica
}
System.Collections.IEnumerator PickBundlesNear(UnityEngine.Vector3 center, float radius, SurvivalEnTren.Items.ItemDefinition only)
{
    foreach (var bundle in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
    {
        if (UnityEngine.Vector3.Distance(bundle.transform.position, center) > radius) continue;
        if (only != null && bundle.Item != only) continue;
        int guard = 0;
        while (bundle != null && bundle.Count > 0 && backpack.MaxAddable(bundle.Item) > 0 && guard++ < 10) { int c = bundle.Count; bundle.Interact(who); yield return wait; if (bundle != null && bundle.Count == c) break; }
    }
}
// Desde el 2026-10-16 las fases se arman en el banco del tren como kits (con su tiempo de verdad: se espera) y se instalan en la
// obra con el kit en la mano. Arma e instala todas las fases que alcancen con lo que hay en la mochila (y los estantes del tren).
var kitBench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
var kitCarrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var kitGear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
float benchWaitSeconds = 0f;
System.Collections.IEnumerator Contribute()
{
    var back = motor.transform.position;
    float yaw = motor.transform.eulerAngles.y;
    for (int round = 0; round < 3 && !site.IsComplete; round++)
    {
        // En el tren, lo que se trae se guarda en el estante del taller (lo que entre): el banco usa la mochila y los estantes, y
        // así el próximo viaje hay lugar en la mochila.
        foreach (var shelf in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.StorageContainer>())
        {
            if (shelf.StableId != "storage_tren_estante_precario_001" || shelf.Grid == null) continue;
            foreach (var piece in new System.Collections.Generic.List<SurvivalEnTren.Items.GridItem>(backpack.Grid.Items))
                if (shelf.Grid.FindFreeSpot(piece.Item, out int sx, out int sy, out bool sr)) { backpack.Grid.Remove(piece); shelf.Grid.TryPlace(piece.Item, sx, sy, sr); }
        }
        // Lo que no entró en el estante se deja en el piso del tren (el banco también lo usa).
        var dropSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.BackpackSettings>("Assets/_Project/Data/Items/BackpackSettings.asset");
        var floorRb = kitBench.GetComponentInParent<UnityEngine.Rigidbody>();
        int floorDrops = 0;
        foreach (var piece in new System.Collections.Generic.List<SurvivalEnTren.Items.GridItem>(backpack.Grid.Items))
        {
            backpack.Grid.Remove(piece);
            SurvivalEnTren.Items.MaterialBundle.Spawn(dropSettings, piece.Item, 1, floorRb.position + floorRb.rotation * new UnityEngine.Vector3(0.9f, 1.4f, 0.4f + 0.25f * (floorDrops++ % 5)), floorRb.linearVelocity);
        }
        var recipe = new SurvivalEnTren.Crafting.BenchRecipe(site, site.Level);
        if (kitBench.Check(recipe, backpack, kitGear) != SurvivalEnTren.Crafting.CraftBlock.None) break;
        var trainRb = kitBench.GetComponentInParent<UnityEngine.Rigidbody>();
        motor.TeleportTo(trainRb.position + trainRb.rotation * new UnityEngine.Vector3(-0.3f, 1.05f, 1.2f), trainRb);
        for (int i = 0; i < 10; i++) yield return new UnityEngine.WaitForFixedUpdate();
        kitBench.StartJob(recipe, backpack, kitGear);
        float waited = 0f;
        while (kitBench.IsBusy && waited < 200f) { yield return new UnityEngine.WaitForFixedUpdate(); waited += UnityEngine.Time.fixedDeltaTime; }
        benchWaitSeconds += waited;
        SurvivalEnTren.Building.UpgradeKit kit = null;
        foreach (var k in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>()) kit = k;
        if (kit == null) break;
        kitCarrier.Pick(kit.GetComponent<SurvivalEnTren.Interaction.Carryable>());
        motor.TeleportTo(back, null);
        motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, yaw));
        var p = back + UnityEngine.Vector3.up * 1.4f + motor.transform.forward * 0.8f;
        kit.transform.position = p; kit.GetComponent<UnityEngine.Rigidbody>().position = p;
        for (int i = 0; i < 10; i++) yield return new UnityEngine.WaitForFixedUpdate();
        Step("instalar fase " + site.Level + " kit " + (kit != null ? kit.SaveKey : "-") + " en la mano " + (kitCarrier.Holding != null));
        site.Interact(who); // con el kit en la mano: lo mismo que mantener E sobre la obra
        for (int i = 0; i < 5; i++) yield return new UnityEngine.WaitForFixedUpdate();
    }
}
void BoardTrain() => motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);

// Tiempo por cuadro de cada viaje.
int trip = 0;
var frameSum = new double[5];
var frameCount = new int[5];
var frameMax = new float[5];
bool sampling = true;
System.Collections.IEnumerator Sampler()
{
    var endOfFrame = new UnityEngine.WaitForEndOfFrame();
    while (sampling)
    {
        yield return endOfFrame;
        float ms = UnityEngine.Time.unscaledDeltaTime * 1000f;
        frameSum[trip] += ms; frameCount[trip]++;
        if (ms > frameMax[trip]) frameMax[trip] = ms;
    }
}
string FrameText(int k) => frameCount[k] == 0 ? "-" : $"{frameSum[k] / frameCount[k]:0.0} ms promedio (máx. {frameMax[k]:0})";

// H2 (herramientas y forja): desarmar es a golpes de barreta, uno cada ~0,75 s como el jugador, y lo que sale cae al piso
// como bultos: se juntan con E (solo los que cayeron de este objeto).
System.Collections.IEnumerator HitDown(SurvivalEnTren.Items.SalvageProp target)
{
    var hitBelt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
    var where = target.transform.position;
    var already = new System.Collections.Generic.HashSet<SurvivalEnTren.Items.MaterialBundle>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>());
    int hits = 0;
    while (target != null && !target.IsDone && hits++ < 60)
    {
        target.OnToolHit(new SurvivalEnTren.Equipment.ToolHit(target.Recipe.RequiredTool, where + UnityEngine.Vector3.up * 0.5f, UnityEngine.Vector3.up, null, hitBelt));
        for (int i = 0; i < 38; i++) yield return new UnityEngine.WaitForFixedUpdate();
    }
    for (int i = 0; i < 40; i++) yield return new UnityEngine.WaitForFixedUpdate(); // que caigan los bultos
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        if (!already.Contains(b) && (b.transform.position - where).sqrMagnitude < 9f) b.Interact(who);
    for (int i = 0; i < 5; i++) yield return new UnityEngine.WaitForFixedUpdate();
}

System.IO.File.WriteAllText("Temp/prueba_paso.txt", "");
void Step(string s) { System.IO.File.AppendAllText("Temp/prueba_paso.txt", s + "\n"); }

System.Collections.IEnumerator Run()
{
    float real0 = UnityEngine.Time.realtimeSinceStartup;
    double hours0 = clock.TotalHours;
    float fuel0 = tank.Liters;
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar); // del perchero junto a la puerta de atrás
    gear.SetSuit(true);
    yield return Frames(60);
    var r = new float[2];
    log.Append($"AL EMPEZAR: cambio {sw.Condition}, obra {site.Level}/{site.Project.PhaseCount}, tanque {tank.Liters:0.0} L, mochila vacía={backpack.UsedCells == 0}.");

    // ---- Viaje 1: la estación (chapas y componentes).
    trip = 1;
    float f1 = tank.Liters;
    yield return DriveTo(seg1a, 100f, r);
    log.Append($" VIAJE 1 (estación): manejó {r[0]:0} s ({route}), quedó a {r[1]:0} m del punto; el tren a {Flat(train.transform.position, station.position):0} m de la estación.");
    yield return Salvage("salv_estacion_armario_001", metal);
    yield return Salvage("salv_estacion_tablero_001", parts);
    yield return PickBundlesNear(station.position, 6f, metal);
    log.Append($" Juntó: {Pack()}.");
    BoardTrain();
    yield return Frames(120);
    log.Append($" Combustible del viaje {f1 - tank.Liters:0.0} L; tiempo por cuadro {FrameText(1)}.");

    // ---- Viaje 2: el taller ferroviario (la aguja de cambio).
    trip = 2;
    float f2 = tank.Liters;
    yield return DriveTo(seg1b, 250f, r);
    log.Append($" VIAJE 2 (taller): manejó {r[0]:0} s ({route}), quedó a {r[1]:0} m del punto; el tren a {Flat(train.transform.position, workshop.position):0} m del taller.");
    SurvivalEnTren.Items.MaterialBundle needleBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == needle) needleBundle = b;
    var np = needleBundle.transform.position;
    motor.TeleportTo(new UnityEngine.Vector3(np.x, 0.1f, np.z + 1.0f), null);
    LookAtWorld(np);
    yield return Frames(40);
    needleBundle.Interact(who);
    yield return Frames(10);
    log.Append($" Juntó: {Pack()}.");
    BoardTrain();
    yield return Frames(120);
    log.Append($" Combustible del viaje {f2 - tank.Liters:0.0} L; tiempo por cuadro {FrameText(2)}.");

    // ---- Viaje 3: la bifurcación (y el refugio por la madera).
    trip = 3;
    float f3 = tank.Liters;
    yield return DriveTo(seg3, 235f, r);
    log.Append($" VIAJE 3 (bifurcación y refugio): manejó {r[0]:0} s ({route}), quedó a {r[1]:0} m del punto; el tren a {Flat(train.transform.position, sw.Point):0} m del cambio y a {Flat(train.transform.position, shelter.position):0} m del refugio.");
    var stand = UnityEngine.GameObject.Find("Cambio_2_Palanca").transform;
    StandNear(stand.position + new UnityEngine.Vector3(0f, 0.6f, 0f), 2.0f);
    yield return Frames(30);
    int g = 0;
    while (ice != null && !ice.IsBroken && g++ < 1000) { if (!ice.Channel(who, UnityEngine.Time.fixedDeltaTime)) break; yield return wait; }
    yield return Frames(30);
    yield return Contribute();
    log.Append($" Hielo roto ({sw.Condition}); armó e instaló lo que alcanzaba: nivel {site.Level}; queda {Pack()}.");
    yield return Salvage("salv_refugio_pallet_001", wood);
    log.Append($" En el refugio: {Pack()}.");
    StandNear(stand.position + new UnityEngine.Vector3(0f, 0.6f, 0f), 2.0f);
    yield return Frames(20);
    yield return Contribute();
    log.Append($" Volvió y aportó: nivel {site.Level}/{site.Project.PhaseCount}, completa={site.IsComplete}, cambio {sw.Condition}; queda {Pack()}; el banco tardó {benchWaitSeconds:0} s reales en armar los kits.");
    motor.TeleportTo(new UnityEngine.Vector3(stand.position.x + 1.0f, 0.1f, stand.position.z + 1.2f), null);
    LookAtWorld(stand.position + new UnityEngine.Vector3(0f, 0.15f, 0f));
    yield return Frames(30);
    object tl = Target();
    lever.Interact(who);
    yield return Frames(10);
    log.Append($" Palanca (objetivo {Name(tl)}): desvío={sw.IsDiverging}.");
    BoardTrain();
    yield return Frames(200);
    Power(2);
    string path = train.RailPosition.Segment.name, last = path;
    float t = 0f;
    while (t < 90f && !(last == "Tramo_RutaDesconocida" && train.RailPosition.Distance > 150f))
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        maxJitter = UnityEngine.Mathf.Max(maxJitter, ride.JitterMillimeters);
        string now = train.RailPosition.Segment.name;
        if (now != last) { last = now; path += " > " + now; }
    }
    Brake(4);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;
    log.Append($" RUTA: {path}; el tren está {train.RailPosition.Distance:0} m adentro de la ruta desconocida. Combustible del viaje {f3 - tank.Liters:0.0} L; tiempo por cuadro {FrameText(3)}.");

    sampling = false;
    log.Append($" RESUMEN: {UnityEngine.Time.realtimeSinceStartup - real0:0} s reales ({clock.TotalHours - hours0:0.0} h de juego); combustible {fuel0:0.0} → {tank.Liters:0.0} L; temblor del jugador manejando máx. {maxJitter:0.00} mm; cuerpo: vida {vitals.Health:0} comida {vitals.Satiety:0} energía {vitals.Energy:0} calor {vitals.Warmth:0}; tiempo por cuadro: viaje 1 {FrameText(1)}, viaje 2 {FrameText(2)}, viaje 3 {FrameText(3)}.");
    UnityEngine.Debug.Log("[Loop] " + log);
}
motor.StartCoroutine(Sampler());
controls.StartCoroutine(Run());
return "prueba loop iniciada";
