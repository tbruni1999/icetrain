// Prueba del M10 paso 1: la bifurcación dañada. El cambio nuevo de la recta oeste arranca CONGELADO (el hielo tapa el
// mecanismo): no se puede mover; con la barreta se rompe el hielo y pasa a ROTO (sigue sin poder moverse); el tren, manejado
// de verdad, pasa de largo por la vía directa; la ruta desconocida existe, sale tangente a la vía y termina en un tope.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m10_bifurcacion.cs" --timeout 60
// Resultado en la Console, líneas [Bifurcacion].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var ride = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeRideMetrics>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
SurvivalEnTren.Rail.RailSwitch sw = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Rail.RailSwitch>()) if (s.StableId == "rail_switch_oeste_001") sw = s;
SurvivalEnTren.Poi.FrozenDoor ice = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Poi.FrozenDoor>()) if (d.StableId == "ice_switch_oeste_001") ice = d;
var link = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Rail.DamagedSwitch>();
var seg3 = UnityEngine.GameObject.Find("Tramo_3_Recta").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var seg3b = UnityEngine.GameObject.Find("Tramo_3b_Recta").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var route = UnityEngine.GameObject.Find("Tramo_RutaDesconocida").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

void Power(int notch) { while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1); while (controls.PowerNotch < notch) controls.MovePowerBrake(+1); while (controls.PowerNotch > notch) controls.MovePowerBrake(-1); }
void Brake(int notch) { while (controls.PowerNotch > 0) controls.MovePowerBrake(-1); while (controls.BrakeNotch < notch) controls.MovePowerBrake(-1); while (controls.BrakeNotch > notch) controls.MovePowerBrake(+1); }
string Seg() => train.RailPosition.Segment.name;

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;

    // 1) Cómo es la bifurcación.
    var routeEnd = route.GetPosition(route.Length);
    var entryDir = seg3.GetDirection(seg3.Length);
    var routeDir = route.GetDirection(0f);
    var stop = UnityEngine.GameObject.Find("Tope_RutaDesconocida");
    bool tangent = UnityEngine.Vector3.Dot(entryDir, routeDir) > 0.999f;
    log.Append($"BIFURCACIÓN: cambio \"{sw.StableId}\" en ({sw.Point.x:0}, {sw.Point.z:0}); estado al empezar {sw.Condition}, se puede mover={sw.CanMove}; la vía directa sigue a {sw.ActiveExit.name}; el desvío va a {sw.DivergingExit.name} ({route.Length:0} m, sale tangente a la vía={tangent}, termina en ({routeEnd.x:0}, {routeEnd.z:0}) con tope={stop != null}).");

    // 2) Congelado: la palanca no tiene qué mover (no hay hecho el cambio).
    bool moved = sw.TrySetDiverging(true, null);
    log.Append($" CONGELADO: intentar mover el cambio → {(moved ? "SE MOVIÓ (mal)" : "no se mueve (bien)")}, desvío={sw.IsDiverging}.");

    // 3) El hielo: sin barreta no cede; con ella, mantener E lo rompe.
    string promptNo = ice.GetPrompt(who);
    bool noCrowbar = ice.Channel(who, 0.5f);
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    string promptYes = ice.GetPrompt(who);
    float held = 0f;
    int guard = 0;
    bool halfSeen = false;
    string promptHalf = "";
    while (ice != null && !ice.IsBroken && guard++ < 1000)
    {
        bool keep = ice.Channel(who, UnityEngine.Time.fixedDeltaTime);
        held += UnityEngine.Time.fixedDeltaTime;
        if (!halfSeen && ice.Progress >= 0.5f) { halfSeen = true; promptHalf = ice.GetPrompt(who); }
        if (!keep) break;
        yield return wait;
    }
    yield return wait; yield return wait;
    log.Append($" HIELO: sin barreta \"{promptNo}\", avanza={noCrowbar}; con barreta \"{promptYes}\"; a la mitad \"{promptHalf}\"; se rompió en {held:0.0} s; el hielo ya no está={ice == null}.");
    log.Append($" Estado ahora {sw.Condition}, se puede mover={sw.CanMove}.");
    moved = sw.TrySetDiverging(true, null);
    log.Append($" ROTO: intentar mover el cambio → {(moved ? "SE MOVIÓ (mal)" : "no se mueve (bien)")}, desvío={sw.IsDiverging}; el tren sigue por {seg3.Next.name}.");

    // 4) Manejar de verdad: el tren pasa de largo por la vía directa (jugador adentro todo el tiempo).
    var tRail = typeof(SurvivalEnTren.Train.TrainMover).GetField("_railPosition", flags);
    tRail.SetValue(train, new SurvivalEnTren.Rail.RailPosition(seg3, 235f));
    train.SnapToRail();
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    for (int i = 0; i < 250; i++) yield return wait; // más que la ventana de 2 s de la medición: que no cuente el salto del teletransporte
    controls.TryMoveReverser(+1);
    Power(3);
    string path = Seg();
    string last = Seg();
    bool enteredRoute = false;
    float maxJitter = 0f, t = 0f;
    while (t < 60f && !(Seg() == "Tramo_3b_Recta" && train.RailPosition.Distance > 60f))
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        maxJitter = UnityEngine.Mathf.Max(maxJitter, ride.JitterMillimeters);
        if (Seg() != last) { last = Seg(); path += " > " + last; }
        if (Seg() == "Tramo_RutaDesconocida") enteredRoute = true;
    }
    Brake(4);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;
    var inside = trainBody.transform.InverseTransformPoint(motor.transform.position);
    log.Append($" MANEJANDO por la bifurcación a 30 km/h: recorrido {path}; entró a la ruta desconocida={enteredRoute}; temblor del jugador {maxJitter:0.00} mm; el jugador sigue adentro={System.Math.Abs(inside.x) < 1.7f && System.Math.Abs(inside.z) < 9.5f}.");

    // 5) Marcha atrás por la bifurcación (a resorte: vuelve al tramo de entrada).
    controls.TryMoveReverser(-1); controls.TryMoveReverser(-1);
    Power(2);
    path = Seg(); last = Seg(); t = 0f;
    while (t < 40f && !(Seg() == "Tramo_3_Recta" && train.RailPosition.Distance < 200f))
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        if (Seg() != last) { last = Seg(); path += " > " + last; }
    }
    Brake(4);
    log.Append($" MARCHA ATRÁS: recorrido {path}.");
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Bifurcacion] " + log);
}
controls.StartCoroutine(Run());
return "prueba bifurcacion iniciada";
