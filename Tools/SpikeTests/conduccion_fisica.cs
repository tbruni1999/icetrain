// Prueba del M2 paso 1: física de conducción (reversor, potencia, freno, inercia, marcha atrás).
// Usa la escena del spike: apaga la conducción de prueba del M1 y agrega TrainControls +
// TrainDynamics al tren en memoria. Mide tiempos para llegar a cada velocidad, distancia de
// frenado y que el reversor solo se mueva con el tren detenido.
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/conduccion_fisica.cs"
// Resultado en la Console, líneas [Conduccion].

var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>().enabled = false;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var settings = UnityEngine.ScriptableObject.CreateInstance<SurvivalEnTren.Train.TrainDriveSettings>();

var holder = new UnityEngine.GameObject("Prueba_Controles");
holder.SetActive(false);
var controls = holder.AddComponent<SurvivalEnTren.Train.TrainControls>();
typeof(SurvivalEnTren.Train.TrainControls).GetField("settings", flags).SetValue(controls, settings);
typeof(SurvivalEnTren.Train.TrainControls).GetField("train", flags).SetValue(controls, train);
holder.SetActive(true);
var dynamics = train.gameObject.AddComponent<SurvivalEnTren.Train.TrainDynamics>();
typeof(SurvivalEnTren.Train.TrainDynamics).GetField("controls", flags).SetValue(dynamics, controls);

float Kmh() => train.SpeedMetersPerSecond * 3.6f;
var log = new System.Text.StringBuilder();

System.Collections.IEnumerator WaitUntilKmh(float kmh, float timeout, System.Action<float> result)
{
    float t = 0f;
    var wait = new UnityEngine.WaitForFixedUpdate();
    while (UnityEngine.Mathf.Abs(Kmh()) < kmh && t < timeout) { yield return wait; t += UnityEngine.Time.fixedDeltaTime; }
    result(t);
}

System.Collections.IEnumerator BrakeToStop(int notch, System.Action<float, float> result)
{
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < notch) controls.MovePowerBrake(-1);
    float distance = 0f, t = 0f;
    var wait = new UnityEngine.WaitForFixedUpdate();
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.001f && t < 120f)
    {
        yield return wait;
        distance += UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) * UnityEngine.Time.fixedDeltaTime;
        t += UnityEngine.Time.fixedDeltaTime;
    }
    result(distance, t);
}

System.Collections.IEnumerator Run()
{
    var wait = new UnityEngine.WaitForFixedUpdate();
    log.Append($"Arranque: reversor={controls.Reverser} potencia={controls.PowerNotch} freno={controls.BrakeNotch}.");
    // Con el freno puesto y potencia, no se tiene que mover.
    bool rev = controls.TryMoveReverser(+1);
    log.Append($" Reversor adelante detenido: {(rev ? "OK" : "NO")}.");
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);

    float t1 = 0f, t2 = 0f, t3 = 0f, t4 = 0f;
    controls.MovePowerBrake(+1); yield return WaitUntilKmh(9.5f, 60f, v => t1 = v);
    bool revMoving = controls.TryMoveReverser(-1);
    for (int i = 0; i < 100; i++) yield return wait;
    float steady1 = Kmh();
    controls.MovePowerBrake(+1); yield return WaitUntilKmh(19f, 60f, v => t2 = v);
    controls.MovePowerBrake(+1); yield return WaitUntilKmh(28.5f, 60f, v => t3 = v);
    controls.MovePowerBrake(+1); yield return WaitUntilKmh(38f, 60f, v => t4 = v);
    for (int i = 0; i < 300; i++) yield return wait;
    float steady4 = Kmh();
    log.Append($" Tiempos 0->10: {t1:0.0} s, 10->20: {t2:0.0} s, 20->30: {t3:0.0} s, 30->40: {t4:0.0} s. Estable en pos.1: {steady1:0.0} km/h, en pos.4: {steady4:0.0} km/h. Reversor en movimiento: {(revMoving ? "SE MOVIÓ (mal)" : "bloqueado (bien)")}.");

    // Inercia: sacar la potencia 10 s.
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    for (int i = 0; i < 500; i++) yield return wait;
    log.Append($" Sin potencia ni freno 10 s: de {steady4:0.0} a {Kmh():0.0} km/h.");

    // Frenado desde 40 km/h con freno 2 y con freno 4.
    float d2 = 0f, s2 = 0f, d4 = 0f, s4 = 0f;
    yield return BrakeToStop(2, (d, s) => { d2 = d; s2 = s; });
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 4) controls.MovePowerBrake(+1);
    float tt = 0f; yield return WaitUntilKmh(39.5f, 120f, v => tt = v);
    yield return BrakeToStop(4, (d, s) => { d4 = d; s4 = s; });
    log.Append($" Frenado desde 40 km/h: freno 2 = {d2:0} m en {s2:0.0} s; freno 4 = {d4:0} m en {s4:0.0} s.");

    // Marcha atrás.
    bool toNeutral = controls.TryMoveReverser(-1), toBack = controls.TryMoveReverser(-1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    controls.MovePowerBrake(+1);
    float tb = 0f; yield return WaitUntilKmh(9.5f, 60f, v => tb = v);
    log.Append($" Marcha atrás: reversor a atrás {(toNeutral && toBack ? "OK" : "NO")}, potencia 1 -> {Kmh():0.0} km/h en {tb:0.0} s (negativo = hacia atrás).");
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < settings.BrakeNotchCount) controls.MovePowerBrake(-1);
    UnityEngine.Debug.Log("[Conduccion] " + log);
}
controls.StartCoroutine(Run());
return "prueba de conducción iniciada";
