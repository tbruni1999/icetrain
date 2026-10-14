// Diagnóstico del M13 (performance pass): cuánta memoria nueva por cuadro genera cada parte del juego. Mide todo prendido y
// después apaga una parte por vez (solo para medir; al final vuelve todo a como estaba). Sin límite de cuadros por segundo.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m13_memoria_por_parte.cs" --timeout 60
// Resultado en la Console, líneas [MemoriaPorParte].

var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var log = new System.Text.StringBuilder();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
int oldRate = UnityEngine.Application.targetFrameRate;
UnityEngine.Application.targetFrameRate = -1;
var gcAlloc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame", 15);
var mainThread = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread", 15);

System.Collections.IEnumerator Measure(string name)
{
    for (int i = 0; i < 20; i++) yield return null;
    double gc = 0, cpu = 0; int n = 0; float t = 0f;
    while (t < 3f) { yield return null; t += UnityEngine.Time.unscaledDeltaTime; gc += gcAlloc.LastValue; cpu += mainThread.LastValue / 1e6; n++; }
    log.Append($" [{name}] {gc / n / 1024.0:0.0} KB/cuadro, {cpu / n:0.00} ms;");
}
// Apaga todas las instancias de un tipo, mide, y las vuelve a prender.
System.Collections.IEnumerator Without<T>(string name) where T : UnityEngine.Behaviour
{
    var list = new System.Collections.Generic.List<T>();
    foreach (var c in UnityEngine.Object.FindObjectsByType<T>()) if (c.enabled) { c.enabled = false; list.Add(c); }
    yield return Measure("sin " + name);
    foreach (var c in list) c.enabled = true;
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    for (int i = 0; i < 60; i++) yield return null;
    yield return Measure("todo prendido");
    yield return Without<SurvivalEnTren.Interaction.PlayerInteractor>("indicación (PlayerInteractor)");
    yield return Without<SurvivalEnTren.UI.SurvivalHud>("barras (SurvivalHud)");
    yield return Without<SurvivalEnTren.Probe.ProbeConsole>("pantalla de la sonda");
    yield return Without<SurvivalEnTren.Probe.ProbeScanner>("sonda (lectura)");
    yield return Without<SurvivalEnTren.Spike.SpikeOverlay>("panel de pruebas");
    yield return Without<SurvivalEnTren.Spike.SpikeRideMetrics>("medición del jugador");
    yield return Without<SurvivalEnTren.Spike.SpikePropMetrics>("medición de cajas");
    yield return Without<SurvivalEnTren.Survival.BodyFeedback>("feedback del cuerpo");
    yield return Without<SurvivalEnTren.Train.CabInstruments>("instrumentos");
    yield return Without<SurvivalEnTren.Audio.AmbientAudio>("ambiente");
    yield return Without<SurvivalEnTren.Audio.TrainEngineAudio>("motor (sonido)");
    yield return Without<SurvivalEnTren.World.DayNightCycle>("día y noche");
    yield return Without<SurvivalEnTren.Crafting.BlueprintBook>("libro de planos (OnGUI)");
    yield return Without<SurvivalEnTren.Save.SaveSystem>("guardado (OnGUI)");
    yield return Without<SurvivalEnTren.UI.PauseMenu>("pausa (OnGUI)");
    yield return Without<SurvivalEnTren.UI.SliceEnding>("final (OnGUI)");
    yield return Without<SurvivalEnTren.Items.InventoryPanel>("mochila (OnGUI)");
    yield return Without<SurvivalEnTren.Crafting.WorkbenchPanel>("banco (OnGUI)");
    gcAlloc.Dispose(); mainThread.Dispose();
    UnityEngine.Application.targetFrameRate = oldRate;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[MemoriaPorParte]" + log);
}
motor.StartCoroutine(Run());
return "diagnostico iniciado";
