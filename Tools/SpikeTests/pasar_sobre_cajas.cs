// Prueba del M1: caminar por encima de varias cajas sueltas en el piso del tren.
// Pone 4 cajas de prueba en fila en el pasillo (giradas al azar) y hace caminar al jugador por
// encima con el teclado virtual (W), con el tren detenido y a 20 km/h. Mide si avanza o se traba,
// cuántas veces queda en el aire, cuántas cambia de referencia (tren/aire) y el mayor salto
// vertical de la cámara en un cuadro.
// IMPORTANTE: la ventana de Unity tiene que estar al frente (si no, el teclado virtual no llega).
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/pasar_sobre_cajas.cs"
// Resultado en la Console, líneas [SobreCajas].

var s = UnityEngine.InputSystem.InputSystem.settings;
s.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
s.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
var kb = (UnityEngine.InputSystem.Keyboard)UnityEngine.InputSystem.InputSystem.GetDevice("SpikeTestKeyboard") ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var dropper = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikePropDropper>();
var cam = UnityEngine.Camera.main.transform;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var t = typeof(SurvivalEnTren.Player.PlayerMotor);
var targetField = typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", flags);
var boxPrefab = (UnityEngine.GameObject)typeof(SurvivalEnTren.Spike.SpikePropDropper).GetField("boxPrefab", flags).GetValue(dropper);
var random = new System.Random(7);

void PlaceAt(UnityEngine.Vector3 world)
{
    var cc = motor.GetComponent<UnityEngine.CharacterController>();
    cc.enabled = false; motor.transform.position = world; cc.enabled = true; UnityEngine.Physics.SyncTransforms();
    var rel = UnityEngine.Quaternion.Inverse(trainBody.rotation) * (world - trainBody.position);
    if ((UnityEngine.Rigidbody)t.GetField("_platform", flags).GetValue(motor) == trainBody)
    { t.GetField("_currentLocal", flags).SetValue(motor, rel); t.GetField("_previousLocal", flags).SetValue(motor, rel); }
}

System.Collections.IEnumerator Pass(string label, System.Action<string> result)
{
    // 4 cajas en fila por el pasillo, giradas al azar.
    var boxes = new System.Collections.Generic.List<UnityEngine.GameObject>();
    for (int i = 0; i < 4; i++)
    {
        var local = new UnityEngine.Vector3(0.2f, 1.2f, 0.9f - i * 0.65f);
        var rot = train.transform.rotation * UnityEngine.Quaternion.Euler(0f, (float)random.NextDouble() * 90f, 0f);
        var box = UnityEngine.Object.Instantiate(boxPrefab, train.transform.TransformPoint(local), rot);
        box.GetComponent<UnityEngine.Rigidbody>().linearVelocity = trainBody.GetPointVelocity(box.transform.position);
        boxes.Add(box);
    }
    PlaceAt(train.transform.TransformPoint(new UnityEngine.Vector3(0.2f, 1.05f, 2.4f)));
    for (int i = 0; i < 90; i++) yield return null;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, train.transform.eulerAngles.y + 180f));

    var wait = new UnityEngine.WaitForEndOfFrame();
    float time = 0f, maxCamStep = 0f, lastCamY = cam.position.y, maxFeet = 0f;
    int airborneFrames = 0, refChanges = 0;
    var lastRef = motor.Platform;
    float startZ = train.transform.InverseTransformPoint(motor.transform.position).z;
    while (time < 2.2f)
    {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W));
        yield return wait;
        float dt = UnityEngine.Time.deltaTime;
        if (dt > 0.008f) maxCamStep = UnityEngine.Mathf.Max(maxCamStep, UnityEngine.Mathf.Abs(cam.position.y - lastCamY));
        lastCamY = cam.position.y;
        var local = train.transform.InverseTransformPoint(motor.RenderFeetPosition);
        maxFeet = UnityEngine.Mathf.Max(maxFeet, local.y - 1f);
        if (!motor.IsGrounded) airborneFrames++;
        if (motor.Platform != lastRef) { refChanges++; lastRef = motor.Platform; }
        time += dt;
    }
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    float endZ = train.transform.InverseTransformPoint(motor.transform.position).z;
    result($"{label}: avanzó {startZ - endZ:0.00} m en 2,2 s (las cajas ocupan de z 0,9 a -1,1; trabado = menos de ~3,5 m)  altura máx. sobre el piso {maxFeet:0.00} m  cuadros en el aire {airborneFrames}  cambios de referencia {refChanges}  mayor salto vertical de cámara {maxCamStep * 100f:0.0} cm/cuadro");
    for (int i = 0; i < 30; i++) yield return null;
    foreach (var b in boxes) UnityEngine.Object.Destroy(b);
    yield return null;
}

System.Collections.IEnumerator Run()
{
    // Espera a que alguien haga clic en la ventana de Unity (máx. 5 minutos).
    for (float w = 0f; !UnityEngine.Application.isFocused && w < 300f; w += UnityEngine.Time.unscaledDeltaTime) yield return null;
    UnityEngine.Debug.Log("[SobreCajas] Unity con foco, empieza la prueba");
    for (int i = 0; i < 60; i++) yield return null;
    string a = "", b = "";
    targetField.SetValue(driver, 0f);
    yield return Pass("Tren detenido", r => a = r);
    targetField.SetValue(driver, 20f);
    while (driver.CurrentSpeedKmh < 19.5f) yield return null;
    yield return Pass("Tren a 20 km/h", r => b = r);
    targetField.SetValue(driver, 0f);
    UnityEngine.Debug.Log("[SobreCajas] " + a + "  ||  " + b + "  ||  foco de Unity: " + UnityEngine.Application.isFocused);
}

motor.StartCoroutine(Run());
return "prueba pasar sobre cajas iniciada, foco=" + UnityEngine.Application.isFocused;
