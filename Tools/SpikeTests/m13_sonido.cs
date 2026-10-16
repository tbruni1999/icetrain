// Prueba del M13 (sonido): el ambiente cambia según dónde está el jugador (afuera, refugio, tren con puertas abiertas o
// cerradas: sección 19 y E.5), el motor sube con la potencia y se apaga sin combustible, las ruedas golpean en las juntas
// mientras el tren rueda y las cosas del mundo suenan (palancas, puertas, hielo, desarme, obra, banco, plano).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m13_sonido.cs" --timeout 60
// Resultado en la Console, líneas [Sonido].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var ambient = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Audio.AmbientAudio>();
var engine = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Audio.TrainEngineAudio>();
var world = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Audio.WorldSounds>();
var climate = train.GetComponent<SurvivalEnTren.Survival.TrainInteriorClimate>();
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
string[] placeName = { "afuera", "refugio", "tren con puerta abierta", "tren cerrado" };
string Amb() => $"{placeName[ambient.Place]}: viento {ambient.WindVolume:0.00} (agudos hasta {ambient.WindCutoffHz:0} Hz), calefacción {ambient.HeaterVolume:0.00}";
void Power(int notch) { while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1); while (controls.PowerNotch < notch) controls.MovePowerBrake(+1); while (controls.PowerNotch > notch) controls.MovePowerBrake(-1); }
void Brake(int notch) { while (controls.PowerNotch > 0) controls.MovePowerBrake(-1); while (controls.BrakeNotch < notch) controls.MovePowerBrake(-1); }

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
    clock.SpeedMultiplier = 0f;
    yield return Frames(60);

    Step("1");
    // 1) El ambiente según el lugar.
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    yield return Frames(90);
    log.Append($"AMBIENTE: {Amb()}");
    var doorRight = UnityEngine.GameObject.Find("Puerta_Derecha").GetComponent<SurvivalEnTren.Interaction.SlidingDoor>();
    int before = world.Played;
    doorRight.Interact(who);
    yield return Frames(90);
    log.Append($" | puerta abierta (sonó \"{world.LastSound}\", {world.Played - before} sonido): {Amb()}");
    doorRight.Interact(who);
    yield return Frames(90);
    log.Append($" | cerrada otra vez: {Amb()}");
    motor.TeleportTo(new UnityEngine.Vector3(trainBody.position.x + 30f, 0.1f, trainBody.position.z), null);
    yield return Frames(90);
    log.Append($" | {Amb()}");
    var station = UnityEngine.GameObject.Find("Estacion_Mantenimiento").transform;
    motor.TeleportTo(station.TransformPoint(new UnityEngine.Vector3(1.5f, 0.1f, 0f)), null);
    yield return Frames(90);
    log.Append($" | en la estación, {Amb()}.");

    Step("2");
    // 2) El motor: ralentí, carga con la potencia y las ruedas en las juntas.
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    yield return Frames(60);
    float idle0 = engine.IdleVolume, load0 = engine.LoadVolume;
    controls.TryMoveReverser(+1);
    Power(4);
    int clicks0 = engine.JointClicks;
    float d0 = 0f, t = 0f;
    while (t < 15f) { yield return wait; t += UnityEngine.Time.fixedDeltaTime; d0 += UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) * UnityEngine.Time.fixedDeltaTime; }
    log.Append($" MOTOR: quieto ralentí {idle0:0.00}, carga {load0:0.00}; con potencia 4 a {train.SpeedMetersPerSecond * 3.6f:0} km/h: ralentí {engine.IdleVolume:0.00}, carga {engine.LoadVolume:0.00} (tono {engine.LoadPitch:0.00}); juntas: {engine.JointClicks - clicks0} golpes en {d0:0} m (una cada 12,5 m: esperado ~{d0 / 12.5f:0}).");
    Brake(4);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;
    float fuel = tank.Liters;
    tank.Draw(fuel);
    yield return Frames(150);
    log.Append($" Sin combustible: ralentí {engine.IdleVolume:0.00}, carga {engine.LoadVolume:0.00} (motor apagado).");
    tank.Add(fuel);

    Step("3");
    // 3) Las cosas del mundo: palanca, hielo, desarme, obra, banco, plano.
    yield return Frames(30);
    int p0 = world.Played;
    var heard = new System.Collections.Generic.List<string>();
    void Note() { if (world.Played > p0) { heard.Add(world.LastSound); p0 = world.Played; } }
    controls.MovePowerBrake(+1); yield return Frames(5); Note();
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    SurvivalEnTren.Poi.FrozenDoor ice = null;
    foreach (var d in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Poi.FrozenDoor>()) if (d.StableId == "door_prueba_estacion_001") ice = d;
    while (ice != null && !ice.IsBroken && ice.Channel(who, 0.05f)) yield return wait;
    yield return Frames(5); Note();
    SurvivalEnTren.Items.SalvageProp pallet = null;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (p.StableId == "salv_prueba_pallet_001") pallet = p;
    yield return HitDown(pallet);
    yield return Frames(5); Note();
    SurvivalEnTren.Building.BuildSite bed = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (b.StableId == "obra_tren_cama_001") bed = b;
    // La cama: el banco arma el kit de la base (con su reloj adelantado) y se instala (desde el 2026-10-16).
    var bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
    var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
    Step("3a madera=" + backpack.CountOf(UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset")));
    bench.StartJob(new SurvivalEnTren.Crafting.BenchRecipe(bed, 0), backpack, gear);
    bench.FinishJobNow();
    yield return Frames(5);
    SurvivalEnTren.Building.UpgradeKit kit = null;
    foreach (var k in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>()) kit = k;
    carrier.Pick(kit.GetComponent<SurvivalEnTren.Interaction.Carryable>());
    yield return Frames(3);
    bed.Interact(who);
    yield return Frames(5); Note();
    backpack.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset"), 1);
    backpack.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset"), 1);
    bench.Complete(UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_SierraManual.asset"), backpack, gear);
    yield return Frames(5); Note();
    UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintPickup>().Interact(who);
    yield return Frames(5); Note();
    log.Append($" COSAS DEL MUNDO: sonaron {string.Join(", ", heard)}.");

    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Sonido] " + log);
}
motor.StartCoroutine(Run());
return "prueba sonido iniciada";
