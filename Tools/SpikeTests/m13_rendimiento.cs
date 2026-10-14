// Prueba del M13 (performance pass): mide, sin adivinar (CLAUDE.md: "no afirmar que algo está optimizado sin medirlo"), en
// varias situaciones del loop: tiempo por cuadro (CPU del hilo principal), memoria nueva por cuadro (GC), lotes de dibujo,
// cambios de material y triángulos. Mide en el Editor (que suma su propio costo): sirve para comparar, no como número final
// de una build.
// Situaciones: adentro del tren quieto; manejando a 40 km/h; afuera junto a la estación; adentro del taller; con la mochila
// abierta; con el panel de pruebas (F1) visible.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m13_rendimiento.cs" --timeout 60
// Resultado en la Console, líneas [Rendimiento].

var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var inventory = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
var overlay = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeOverlay>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var log = new System.Text.StringBuilder();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
int oldRate = UnityEngine.Application.targetFrameRate;
UnityEngine.Application.targetFrameRate = 60; // sin límite solo mientras se mide (si no, el editor deja de atender pedidos)

var mainThread = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread", 15);
var gcAlloc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame", 15);
var batches = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Batches Count", 15);
var setPass = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "SetPass Calls Count", 15);
var tris = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Triangles Count", 15);

System.Collections.IEnumerator Measure(string name, float seconds)
{
    for (int i = 0; i < 30; i++) yield return null; // que se asiente
    UnityEngine.Application.targetFrameRate = -1; // sin límite: el costo real, no la espera del límite de 60
    double cpu = 0, cpuMax = 0, gc = 0, b = 0, sp = 0, tr = 0, frame = 0, frameMax = 0;
    int n = 0;
    float t = 0f;
    while (t < seconds)
    {
        yield return null;
        t += UnityEngine.Time.unscaledDeltaTime;
        double ms = mainThread.LastValue / 1e6;
        cpu += ms; if (ms > cpuMax) cpuMax = ms;
        double f = UnityEngine.Time.unscaledDeltaTime * 1000.0;
        frame += f; if (f > frameMax) frameMax = f;
        gc += gcAlloc.LastValue; b += batches.LastValue; sp += setPass.LastValue; tr += tris.LastValue;
        n++;
    }
    UnityEngine.Application.targetFrameRate = 60;
    log.Append($" [{name}] cuadro {frame / n:0.0} ms (máx. {frameMax:0}), CPU hilo principal {cpu / n:0.0} ms (máx. {cpuMax:0}), memoria nueva {gc / n / 1024.0:0.0} KB/cuadro, lotes {b / n:0}, cambios de material {sp / n:0}, triángulos {tr / n / 1000.0:0} mil;");
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    bool overlayWas = overlay != null && overlay.Visible;
    if (overlay != null) overlay.Visible = false;
    for (int i = 0; i < 60; i++) yield return null;

    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    yield return Measure("tren quieto, adentro", 5f);

    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 4) controls.MovePowerBrake(+1);
    float accel = 0f; while (accel < 30f && train.SpeedMetersPerSecond < 10.5f) { yield return null; accel += UnityEngine.Time.deltaTime; } // hasta ~40 km/h
    yield return Measure($"manejando a {train.SpeedMetersPerSecond * 3.6f:0} km/h, adentro", 6f);
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return null;

    var station = UnityEngine.GameObject.Find("Estacion_Mantenimiento").transform;
    motor.TeleportTo(station.TransformPoint(new UnityEngine.Vector3(10f, 0.1f, 0f)), null);
    yield return Measure("afuera, junto a la estación", 5f);
    var workshop = UnityEngine.GameObject.Find("Taller_Ferroviario").transform;
    motor.TeleportTo(workshop.TransformPoint(new UnityEngine.Vector3(0f, 0.1f, 0f)), null);
    yield return Measure("adentro del taller", 5f);

    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    inventory.Open();
    yield return Measure("mochila abierta", 4f);
    inventory.Close();
    if (overlay != null) overlay.Visible = true;
    yield return Measure("panel de pruebas (F1) visible", 4f);
    if (overlay != null) overlay.Visible = overlayWas;

    mainThread.Dispose(); gcAlloc.Dispose(); batches.Dispose(); setPass.Dispose(); tris.Dispose();
    UnityEngine.Application.targetFrameRate = oldRate;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Rendimiento]" + log);
}
motor.StartCoroutine(Run());
return "prueba rendimiento iniciada";
