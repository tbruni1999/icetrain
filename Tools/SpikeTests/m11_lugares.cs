// Prueba del M11 paso 1: los dos lugares nuevos de la mini-región. El taller ferroviario (junto a la masa metálica) y el refugio
// (la señal de calor residual): que existan donde dice la sonda, con lo que tienen adentro (chapas, componentes y la aguja de
// cambio; madera, tela y raciones), que la puerta se abra y se pueda entrar caminando de verdad (teclado virtual), que adentro
// no haya viento (más templado que afuera), que la sonda portátil tiemble dentro del taller y que lo que no entra en la mochila
// al desarmar quede adentro del galpón.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s), con la ventana de Unity con foco,
//   unity command eval_file --file "Tools/SpikeTests/m11_lugares.cs" --timeout 60
// Resultado en la Console, líneas [Lugares].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var scanner = train.GetComponent<SurvivalEnTren.Probe.ProbeScanner>();
var probe = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Probe.PortableProbe>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var items = "Assets/_Project/Data/Items/";
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Metal.asset");
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Madera.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Tela.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Componentes.asset");
var needle = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "AgujaCambio.asset");
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
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
SurvivalEnTren.Probe.ProbeSignal SignalOf(SurvivalEnTren.Probe.ProbeSignalType type)
{
    foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Probe.ProbeSignal>()) if (s.Type == type) return s;
    return null;
}
// Qué tiene adentro un lugar: los objetos desmontables (por ID) y lo suelto (bultos y raciones) dentro de su caja.
string Contents(UnityEngine.Transform shed, string prefix, out int props)
{
    int w = 0, m = 0, c = 0, pa = 0, n = 0, rations = 0;
    props = 0;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>())
    {
        if (!p.StableId.StartsWith(prefix)) continue;
        props++;
        foreach (var y in p.Recipe.Yields) { if (y.item == wood) w += y.count; if (y.item == metal) m += y.count; if (y.item == cloth) c += y.count; if (y.item == parts) pa += y.count; }
    }
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        if (UnityEngine.Vector3.Distance(b.transform.position, shed.position) < 5f && b.Item == needle) n += b.Count;
    foreach (var f in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Survival.FoodItem>())
        if (UnityEngine.Vector3.Distance(f.transform.position, shed.position) < 5f) rations++;
    return $"{props} objetos desmontables (madera ×{w}, chapas ×{m}, tela ×{c}, componentes ×{pa}), aguja de cambio ×{n}, raciones ×{rations}";
}
// Camina con W de verdad (teclado virtual) durante unos segundos.
System.Collections.IEnumerator WalkForward(float seconds)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W));
    float t = 0f;
    while (t < seconds) { yield return wait; t += UnityEngine.Time.fixedDeltaTime; }
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    yield return Frames(20);
}
bool Inside(UnityEngine.Transform shed, float halfX, float halfZ)
{
    var local = shed.InverseTransformPoint(motor.transform.position);
    return UnityEngine.Mathf.Abs(local.x) < halfX - 0.1f && UnityEngine.Mathf.Abs(local.z) < halfZ - 0.1f;
}
// Entra al lugar por la puerta: se para afuera frente a la puerta, la abre con E (apuntada) y camina con W.
System.Collections.IEnumerator EnterThroughDoor(UnityEngine.Transform shed, int doorSide, float halfX, float halfZ, string[] result)
{
    var door = shed.Find("Puerta").GetComponent<SurvivalEnTren.Interaction.SlidingDoor>();
    var outside = shed.TransformPoint(new UnityEngine.Vector3(doorSide * (halfX + 2.2f), 0f, 0f));
    motor.TeleportTo(new UnityEngine.Vector3(outside.x, outside.y + 0.1f, outside.z), null);
    yield return Frames(20);
    LookAtWorld(shed.TransformPoint(new UnityEngine.Vector3(doorSide * halfX, 1.2f, 0f)));
    yield return Frames(30);
    object target = Target();
    string prompt = (target as SurvivalEnTren.Interaction.IInteractable)?.GetPrompt(who) ?? "(sin indicación)";
    bool closedBefore = !door.IsOpen;
    door.Interact(who);
    yield return Frames(60);
    LookAtWorld(shed.TransformPoint(new UnityEngine.Vector3(0f, 1.6f, 0f)));
    yield return Frames(10);
    yield return WalkForward(2.6f);
    result[0] = $"puerta cerrada al llegar={closedBefore}, apuntándola el objetivo es {Name(target)} \"{prompt}\", abierta={door.IsOpen}, entró caminando={Inside(shed, halfX, halfZ)}";
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 60; i++) yield return wait;
    var metalSignal = SignalOf(SurvivalEnTren.Probe.ProbeSignalType.MasaMetalica);
    var heatSignal = SignalOf(SurvivalEnTren.Probe.ProbeSignalType.CalorResidual);
    var workshop = UnityEngine.GameObject.Find("Taller_Ferroviario").transform;
    var shelter = UnityEngine.GameObject.Find("Refugio").transform;
    var station = UnityEngine.GameObject.Find("Estacion_Mantenimiento").transform;

    // 1) Dónde están y qué tienen.
    log.Append($"LUGARES: taller ferroviario a {UnityEngine.Vector3.Distance(workshop.position, metalSignal.transform.position):0} m de la señal de masa metálica ({metalSignal.StableId}); refugio sobre la señal de calor residual ({heatSignal.StableId}, intensidad {heatSignal.Strength:0.00}, se detecta desde {scanner.Settings.MainRangeMeters * heatSignal.Strength:0} m).");
    log.Append($" TALLER: {Contents(workshop, "salv_taller_", out int wp)}. REFUGIO: {Contents(shelter, "salv_refugio_", out int sp)}. ESTACIÓN (sin cambios salvo la aguja): {Contents(station, "salv_estacion_", out int stp)}.");
    int provisional = 0;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (p.StableId.StartsWith("salv_cambio_")) provisional++;
    log.Append($" Objetos provisorios junto a la bifurcación (M10): {provisional}.");

    // 2) Entrar al taller caminando, aire adentro y la sonda que tiembla por el metal.
    var res = new string[1];
    yield return EnterThroughDoor(workshop, -1, 3f, 3.5f, res);
    yield return Frames(30);
    float airInside = vitals.AmbientTemperatureC;
    log.Append($" TALLER: {res[0]}; aire adentro {airInside:0} °C.");
    int g = 0; while (scanner.Target != metalSignal && g++ < 8) scanner.SelectNext();
    probe.SetOut(true);
    yield return Frames(120);
    log.Append($" Sonda portátil adentro del taller: interferencia {probe.Interference:0.00}, calidad {probe.Quality:0.00}.");
    probe.SetOut(false);

    // 3) La aguja sobre la mesa; desarmar un armario con la mochila casi llena: lo que no entra queda adentro del galpón.
    SurvivalEnTren.Items.MaterialBundle needleBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == needle) needleBundle = b;
    var needleLocal = workshop.InverseTransformPoint(needleBundle.transform.position);
    log.Append($" AGUJA: sobre la mesa (altura {needleLocal.y:0.00} m, la mesa mide 0,90) en ({needleLocal.x:0.0}, {needleLocal.z:0.0}).");
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    backpack.Add(wood, 6); // 24 de 30 casillas ocupadas: entra solo una chapa (2x2)
    SurvivalEnTren.Items.SalvageProp locker = null;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (p.StableId == "salv_taller_armario_001") locker = p;
    var lockerPos = locker.transform.position;
    int guard = 0;
    while (locker != null && locker.Channel(who, 0.1f) && guard++ < 200) yield return wait;
    yield return Frames(60);
    int leftInside = 0, leftOutside = 0, metalBundles = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
    {
        if (b.Item != metal || UnityEngine.Vector3.Distance(b.transform.position, lockerPos) > 6f) continue;
        metalBundles += b.Count;
        var l = workshop.InverseTransformPoint(b.transform.position);
        if (UnityEngine.Mathf.Abs(l.x) < 3f && UnityEngine.Mathf.Abs(l.z) < 3.5f && l.y > -0.1f) leftInside += b.Count; else leftOutside += b.Count;
    }
    log.Append($" DESARMAR con la mochila casi llena: armario desarmado={locker == null}; chapas en la mochila {backpack.CountOf(metal)}, en bultos {metalBundles} (adentro del galpón {leftInside}, afuera {leftOutside}); total {backpack.CountOf(metal) + metalBundles} (el armario da 3).");
    backpack.Remove(wood, 6);

    // 4) Entrar al refugio: puerta al este, adentro más templado (sin viento) y la estufa encendida.
    yield return EnterThroughDoor(shelter, 1, 2.5f, 2.0f, res);
    yield return Frames(30);
    float airShelter = vitals.AmbientTemperatureC;
    motor.TeleportTo(shelter.TransformPoint(new UnityEngine.Vector3(5f, 0.1f, 4f)), null);
    yield return Frames(30);
    float airOut = vitals.AmbientTemperatureC;
    var heat = shelter.Find("Calor").GetComponent<UnityEngine.Light>();
    log.Append($" REFUGIO: {res[0]}; aire adentro {airShelter:0} °C, afuera {airOut:0} °C; la estufa tiene luz={heat != null && heat.enabled}.");

    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Lugares] " + log);
}
motor.StartCoroutine(Run());
return "prueba lugares iniciada";
