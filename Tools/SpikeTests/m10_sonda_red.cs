// Prueba del M10 paso 3: la sonda lee la red. La bifurcación dañada aparece en la consola de la cabina como lectura de la red
// (rombo blanco, no punto verde) con su estado (BLOQUEADA / ABIERTA) y "ruta desconocida"; la sonda portátil, con la bifurcación
// fijada como objetivo, lleva hasta ella solo con la aguja ámbar; y cuando se repara el cambio el texto pasa a ABIERTA.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m10_sonda_red.cs" --timeout 60
// Resultado en la Console, líneas [SondaRed].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var scanner = train.GetComponent<SurvivalEnTren.Probe.ProbeScanner>();
var console = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Probe.ProbeConsole>();
var probe = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Probe.PortableProbe>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var cc = motor.GetComponent<UnityEngine.CharacterController>();
var tm = typeof(SurvivalEnTren.Player.PlayerMotor);
var toLocal = tm.GetMethod("ToReferenceLocal", flags);
SurvivalEnTren.Rail.RailSwitch sw = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Rail.RailSwitch>()) if (s.StableId == "rail_switch_oeste_001") sw = s;
SurvivalEnTren.Poi.FrozenDoor ice = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Poi.FrozenDoor>()) if (d.StableId == "ice_switch_oeste_001") ice = d;
SurvivalEnTren.Building.BuildSite site = null;
foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (b.StableId == "obra_cambio_oeste_001") site = b;
SurvivalEnTren.Probe.ProbeSignal fork = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Probe.ProbeSignal>()) if (s.StableId == "net_rail_oeste_001") fork = s;
var seg3 = UnityEngine.GameObject.Find("Tramo_3_Recta").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var items = "Assets/_Project/Data/Items/";
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Metal.asset");
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Madera.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Componentes.asset");
var needle = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "AgujaCambio.asset");
var trainText = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.PortableProbe).GetField("trainText", flags).GetValue(probe);
var targetText = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.PortableProbe).GetField("targetText", flags).GetValue(probe);
var line1 = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.ProbeConsole).GetField("line1", flags).GetValue(console);
var line2 = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.ProbeConsole).GetField("line2", flags).GetValue(console);
var line3 = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.ProbeConsole).GetField("line3", flags).GetValue(console);
var blips = (UnityEngine.Transform[])typeof(SurvivalEnTren.Probe.ProbeConsole).GetField("blips", flags).GetValue(console);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

float Flat(UnityEngine.Vector3 a, UnityEngine.Vector3 b) { a.y = 0f; b.y = 0f; return UnityEngine.Vector3.Distance(a, b); }
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
void Select(SurvivalEnTren.Probe.ProbeSignal s) { int g = 0; while (scanner.Target != s && g++ < 8) scanner.SelectNext(); }

// Camina siguiendo la aguja ámbar hasta que el texto diga que llegó.
System.Collections.IEnumerator Walk(System.Func<float> needleDeg, UnityEngine.TextMesh text, string arrived, float[] result)
{
    float t = 0f, path = 0f, stuck = 0f;
    while (t < 120f && text.text != arrived && stuck < 2f)
    {
        yield return wait;
        float dt = UnityEngine.Time.fixedDeltaTime;
        t += dt;
        motor.AddYaw(UnityEngine.Mathf.Clamp(needleDeg(), -120f * dt, 120f * dt));
        var before = motor.transform.position;
        cc.Move(motor.transform.forward * 5f * dt);
        var local = toLocal.Invoke(motor, new object[] { motor.transform.position });
        tm.GetField("_currentLocal", flags).SetValue(motor, local);
        tm.GetField("_previousLocal", flags).SetValue(motor, local);
        float moved = UnityEngine.Vector3.ProjectOnPlane(motor.transform.position - before, UnityEngine.Vector3.up).magnitude;
        path += moved;
        stuck = moved < 5f * dt * 0.2f ? stuck + dt : 0f;
    }
    result[0] = t; result[1] = path;
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 90; i++) yield return wait; // que la sonda lea

    // 1) La consola detecta la bifurcación como lectura de la red.
    int detected = scanner.Readings.Count;
    bool seen = scanner.TryGetReading(fork, out var reading);
    log.Append($"LECTURA: la bifurcación \"{fork.StableId}\" es lectura de la red={fork.IsNetworkMarker}, estado \"{fork.StatusText}\" (bloqueada={fork.IsBlocked}); la sonda detecta {detected} señales en total, la detecta={seen}; ~{reading.Distance:0} m, dirección {reading.BearingDegrees:0}°, por la vía {(reading.AlongKnown ? reading.AlongMeters.ToString("0") + " m" : "(fuera de la ruta)")}.");

    // 2) Fijarla como objetivo y leer la pantalla de la cabina.
    Select(fork);
    yield return Frames(30);
    int idx = -1;
    for (int i = 0; i < scanner.Readings.Count; i++) if (scanner.Readings[i].Signal == fork) idx = i;
    float blipAngle = idx >= 0 ? blips[idx].localEulerAngles.z : -1f;
    log.Append($" PANTALLA: objetivo fijado={scanner.Target == fork}; \"{line1.text}\" / \"{line2.text}\" / \"{line3.text}\"; el punto de la bifurcación está girado {blipAngle:0}° (45 = rombo).");
    string cleanSignal = "";
    foreach (var r in scanner.Readings) if (!r.Signal.IsNetworkMarker) { cleanSignal = r.Signal.StableId; break; }
    Select(scanner.Readings[0].Signal.IsNetworkMarker ? scanner.Readings[1].Signal : scanner.Readings[0].Signal);
    yield return Frames(30);
    log.Append($" Con una señal de un lugar ({cleanSignal}): \"{line1.text}\" / \"{line3.text}\" (sin rombo ni estado).");

    // 3) La sonda portátil: el tren queda cerca de la bifurcación y se camina siguiendo la aguja ámbar.
    var tRail = typeof(SurvivalEnTren.Train.TrainMover).GetField("_railPosition", flags);
    tRail.SetValue(train, new SurvivalEnTren.Rail.RailPosition(seg3, 140f));
    train.SnapToRail();
    yield return Frames(120);
    Select(fork);
    var door = train.transform.TransformPoint(new UnityEngine.Vector3(1.7f + 2.5f, 0f, -6f));
    motor.TeleportTo(new UnityEngine.Vector3(door.x, 0.6f, door.z), null);
    yield return Frames(30);
    probe.SetOut(true);
    yield return Frames(60);
    float straight = Flat(sw.Point, motor.transform.position);
    string first = targetText.text;
    var res = new float[2];
    yield return Walk(() => probe.TargetNeedleDegrees, targetText, "OBJ.  ~10 M", res);
    float miss = Flat(sw.Point, motor.transform.position);
    var stand = UnityEngine.GameObject.Find("Cambio_2_Palanca").transform;
    log.Append($" PORTÁTIL: sonda afuera={probe.IsOut}, texto al empezar \"{first}\" (a {straight:0} m reales); IDA: {res[0]:0} s, caminó {res[1]:0} m para {straight:0} m en línea recta, terminó a {miss:0.0} m de la bifurcación y a {Flat(stand.position, motor.transform.position):0.0} m de la palanca, texto \"{targetText.text}\".");

    // 4) Repararlo: el texto pasa de BLOQUEADA a ABIERTA.
    Select(fork);
    yield return Frames(20);
    string before = line1.text;
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    int g2 = 0;
    while (ice != null && !ice.IsBroken && g2++ < 1000) { if (!ice.Channel(who, UnityEngine.Time.fixedDeltaTime)) break; yield return wait; }
    yield return Frames(20);
    string broken = line1.text;
    // Se aporta por tandas (la mochila es de 6x5 casillas: el tablón es largo y la chapa es 2x2, no entra todo junto).
    var giveByPhase = new System.Action[] {
        () => { backpack.Add(metal, 2); backpack.Add(parts, 1); },
        () => { backpack.Add(wood, 4); },
        () => { backpack.Add(metal, 3); },
        () => { backpack.Add(needle, 1); } };
    for (int phase = 0; phase < giveByPhase.Length; phase++) { giveByPhase[phase](); int g = 0; while (site.Channel(who, 0.55f) && g++ < 60) yield return null; yield return Frames(3); }
    yield return Frames(30);
    log.Append($" REPARACIÓN: antes \"{before}\"; con el hielo roto \"{broken}\"; cambio reparado={sw.Condition} → \"{line1.text}\"; el rombo pasó a estado {fork.StatusText}.");
    probe.SetOut(false);
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[SondaRed] " + log);
}
motor.StartCoroutine(Run());
return "prueba sonda red iniciada";
