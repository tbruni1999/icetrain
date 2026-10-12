// Prueba del M4 paso 2: temperatura del interior del tren y del jugador.
// Reloj x30. Puertas cerradas, después las dos abiertas, después cerradas otra vez; al final el
// jugador sale afuera. Mide la temperatura del interior y el calor del cuerpo.
// Cómo correrla: escena M2_Conduccion recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/m4_temperatura.cs"
// Resultado en la Console, líneas [Temperatura].

var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var climate = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.TrainInteriorClimate>();
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var sensor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.ThermalSensor>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var doors = train.GetComponentsInChildren<SurvivalEnTren.Interaction.SlidingDoor>();
var log = new System.Text.StringBuilder();
string State(string label) => $" | {label}: hora {clock.HourOfDay:0.0}, interior {climate.InteriorTemperatureC:0.0} °C, puertas abiertas {climate.OpenDoors}, jugador {(sensor.IsSheltered ? "adentro" : "afuera")} aire {vitals.AmbientTemperatureC:0} °C, calor {vitals.Warmth:0}";

System.Collections.IEnumerator WaitGameHours(float h) { double end = clock.TotalHours + h; while (clock.TotalHours < end) yield return null; }

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 30f;
    log.Append(State("inicio"));
    yield return WaitGameHours(1f);
    log.Append(State("1 h con puertas cerradas"));
    foreach (var d in doors) d.Interact(null);
    yield return WaitGameHours(0.25f);
    log.Append(State("15 min con las 2 puertas abiertas"));
    yield return WaitGameHours(0.75f);
    log.Append(State("1 h con las 2 abiertas"));
    foreach (var d in doors) d.Interact(null);
    yield return WaitGameHours(1f);
    log.Append(State("1 h después de cerrar"));
    yield return WaitGameHours(1f);
    log.Append(State("2 h después de cerrar"));
    // Sacar al jugador afuera, al lado del tren.
    var cc = motor.GetComponent<UnityEngine.CharacterController>();
    cc.enabled = false; motor.transform.position = train.transform.TransformPoint(new UnityEngine.Vector3(6f, -0.55f, 0f)); cc.enabled = true;
    UnityEngine.Physics.SyncTransforms();
    // Actualizar la posición relativa al tren (si no, el tren lo "devuelve" a donde estaba).
    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    var tb = train.GetComponent<UnityEngine.Rigidbody>();
    var rel = UnityEngine.Quaternion.Inverse(tb.rotation) * (motor.transform.position - tb.position);
    typeof(SurvivalEnTren.Player.PlayerMotor).GetField("_currentLocal", flags).SetValue(motor, rel);
    typeof(SurvivalEnTren.Player.PlayerMotor).GetField("_previousLocal", flags).SetValue(motor, rel);
    yield return WaitGameHours(2f);
    log.Append(State("2 h afuera"));
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Temperatura]" + log);
}
motor.StartCoroutine(Run());
return "prueba de temperatura iniciada";
