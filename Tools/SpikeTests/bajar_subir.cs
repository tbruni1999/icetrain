// Prueba automática del paso 5 del M1 (bajar/subir del tren). No es parte del juego.
// Cómo correrla: con la escena Spike_TrenEstable en Play, permitir teclado virtual y luego
//   unity command eval_file --file "Tools/SpikeTests/bajar_subir.cs"
// (antes: Application.runInBackground = true y, en InputSystem.settings, backgroundBehavior = IgnoreFocus y
//  editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView; solo en memoria).
// Resultado en la Console, líneas [Test].

var kb = (UnityEngine.InputSystem.Keyboard)UnityEngine.InputSystem.InputSystem.GetDevice("SpikeTestKeyboard") ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var cam = UnityEngine.Camera.main.transform;
bool running = true;
var targetField = typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

void Keys(params UnityEngine.InputSystem.Key[] keys) { UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(keys)); }

void Steer(UnityEngine.Vector3 target)
{
    UnityEngine.Vector3 to = target - motor.transform.position;
    to.y = 0f;
    if (to.sqrMagnitude < 1e-4f) return;
    float desired = UnityEngine.Mathf.Atan2(to.x, to.z) * UnityEngine.Mathf.Rad2Deg;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, desired));
}

float maxStep = 0f;
string maxStepWhere = "";
UnityEngine.Vector3 lastCam = cam.position;
string phase = "";

void TrackCamera()
{
    float step = (cam.position - lastCam).magnitude / UnityEngine.Mathf.Max(1e-4f, UnityEngine.Time.deltaTime); // velocidad aparente, m/s
    if (step > maxStep) { maxStep = step; maxStepWhere = phase + " t=" + UnityEngine.Time.time.ToString("F1") + " local=" + train.transform.InverseTransformPoint(motor.transform.position).ToString("F2"); }
    if (step > 10f) UnityEngine.Debug.Log("[Test] salto de cámara " + step.ToString("F1") + " m/s (dt=" + UnityEngine.Time.deltaTime.ToString("F3") + ") en " + phase + " local=" + train.transform.InverseTransformPoint(motor.transform.position).ToString("F2") + " plat=" + (motor.Platform != null));
    lastCam = cam.position;
}

// Camina (o corre) hacia un punto del tren, hasta cumplir la condición o agotar el tiempo.
System.Collections.IEnumerator WalkTo(System.Func<UnityEngine.Vector3> target, System.Func<bool> done, float timeout, bool runKey, string label, System.Action<bool> result)
{
    phase = label;
    float t = 0f;
    while (t < timeout && !done())
    {
        Steer(target());
        if (runKey) Keys(UnityEngine.InputSystem.Key.W, UnityEngine.InputSystem.Key.LeftShift); else Keys(UnityEngine.InputSystem.Key.W);
        t += UnityEngine.Time.deltaTime;
        yield return null;
    }
    Keys();
    if (!runKey) for (int i = 0; i < 20; i++) { yield return null; }
    result(done());
}

System.Collections.IEnumerator Run()
{
    UnityEngine.Vector3 Outside() => train.transform.TransformPoint(new UnityEngine.Vector3(4.5f, -0.6f, -6f));
    UnityEngine.Vector3 Inside() => train.transform.TransformPoint(new UnityEngine.Vector3(0f, 1f, -6f));
    bool OnGround() => motor.Platform == null && motor.IsGrounded && (motor.transform.position - Outside()).magnitude < 1.2f;
    bool OnTrain() => motor.Platform == trainBody;
    // Punto en el suelo al costado de la puerta, para alcanzarla antes de subir.
    UnityEngine.Vector3 Beside() => train.transform.TransformPoint(new UnityEngine.Vector3(3.2f, -0.6f, -5.6f));
    bool NearBeside() { var l = train.transform.InverseTransformPoint(motor.transform.position); return l.x > 2.4f && UnityEngine.Mathf.Abs(l.z + 6f) < 0.6f; }

    int ok = 0, total = 0;
    bool r = false;

    { var cc = motor.GetComponent<UnityEngine.CharacterController>(); cc.enabled = false; motor.transform.position = Inside() + UnityEngine.Vector3.up * 0.05f; cc.enabled = true; }
    for (int i = 0; i < 30; i++) yield return null;
    lastCam = cam.position;
    // A) Tren detenido: bajar y subir 5 veces.
    targetField.SetValue(driver, 0f);
    for (int i = 1; i <= 5; i++)
    {
        yield return WalkTo(Outside, OnGround, 8f, false, "A" + i + " bajar", v => r = v); total++; if (r) ok++;
        UnityEngine.Debug.Log($"[Test] A{i} bajar (detenido): {(r ? "OK" : "FALLÓ")}");
        yield return WalkTo(Inside, OnTrain, 8f, false, "A" + i + " subir", v => r = v); total++; if (r) ok++;
        UnityEngine.Debug.Log($"[Test] A{i} subir (detenido): {(r ? "OK" : "FALLÓ")}");
    }

    // B) Tren a 10 km/h: bajar, correr detrás y volver a subir, 3 veces.
    targetField.SetValue(driver, 10f);
    while (driver.CurrentSpeedKmh < 9.9f) { yield return null; }
    for (int i = 1; i <= 3; i++)
    {
        yield return WalkTo(Outside, () => motor.Platform == null && motor.IsGrounded, 8f, false, "B" + i + " bajar", v => r = v); total++; if (r) ok++;
        UnityEngine.Debug.Log($"[Test] B{i} bajar (10 km/h): {(r ? "OK" : "FALLÓ")}");
        yield return WalkTo(Beside, NearBeside, 15f, true, "B" + i + " alcanzar puerta", v => r = v);
        yield return WalkTo(Inside, OnTrain, 6f, true, "B" + i + " subir", v => r = v); total++; if (r) ok++;
        UnityEngine.Debug.Log($"[Test] B{i} subir corriendo (10 km/h): {(r ? "OK" : "FALLÓ")}");
    }

    targetField.SetValue(driver, 0f);
    running = false;
    UnityEngine.Debug.Log($"[Test] RESULTADO bajar/subir: {ok}/{total} OK. Mayor velocidad aparente de la cámara: {maxStep:0.0} m/s (correr + tren = 9,3 m/s) ({maxStepWhere}).");
}

System.Collections.IEnumerator Sampler()
{
    var wait = new UnityEngine.WaitForEndOfFrame();
    while (running) { yield return wait; TrackCamera(); }
}
motor.StartCoroutine(Sampler());
motor.StartCoroutine(Run());
return "prueba iniciada";
