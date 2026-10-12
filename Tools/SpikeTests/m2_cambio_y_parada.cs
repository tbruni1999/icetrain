// Prueba del M2 paso 3: cambio de vía, vía muerta con tope, marcha atrás por el cambio y
// parada junto al cartel. El jugador va en la cabina todo el tiempo (se mide su estabilidad).
// Cómo correrla: escena M2_Conduccion recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/m2_cambio_y_parada.cs"
// Resultado en la Console, líneas [M2].

var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var lever = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Rail.SwitchLever>();
var railSwitch = lever.Switch;
var ride = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeRideMetrics>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var sign = UnityEngine.GameObject.Find("Cartel_Parada").transform;
var log = new System.Text.StringBuilder();
bool hitEnd = false;
train.ReachedEndOfTrack += () => hitEnd = true;
var wait = new UnityEngine.WaitForFixedUpdate();

void Power(int notch) { while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1); while (controls.PowerNotch < notch) controls.MovePowerBrake(+1); while (controls.PowerNotch > notch) controls.MovePowerBrake(-1); }
void Brake(int notch) { while (controls.PowerNotch > 0) controls.MovePowerBrake(-1); while (controls.BrakeNotch < notch) controls.MovePowerBrake(-1); while (controls.BrakeNotch > notch) controls.MovePowerBrake(+1); }
string Seg() => train.RailPosition.Segment.name;
float Kmh() => train.SpeedMetersPerSecond * 3.6f;

System.Collections.IEnumerator Run()
{
    float maxJitter = 0f;
    // 1) Cambio hacia el desvío, con el tren lejos.
    lever.Interact(null);
    log.Append($"Palanca con el tren lejos: desvío={railSwitch.IsDiverging}.");

    // 2) Adelante a 20 km/h hasta el tope de la vía muerta.
    controls.TryMoveReverser(+1);
    Power(2);
    bool triedBlocked = false, blockedOk = false;
    string lastSeg = Seg(), path = Seg();
    float t = 0f;
    bool touched = false;
    while (!(touched && train.SpeedMetersPerSecond <= 0f) && t < 120f)
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        maxJitter = UnityEngine.Mathf.Max(maxJitter, ride.JitterMillimeters);
        if (train.GapToTrackEnd(1) < 0f) touched = true; // la punta entró en el resorte del tope
        if (Seg() != lastSeg) { lastSeg = Seg(); path += " > " + lastSeg; }
        if (!triedBlocked && (train.transform.position - railSwitch.Point).magnitude < 8f)
        {
            triedBlocked = true;
            lever.Interact(null);
            blockedOk = railSwitch.IsDiverging; // tiene que seguir en desvío
        }
    }
    var spur = train.RailPosition.Segment;
    float frontToEnd = spur.Length - (train.RailPosition.Distance + 9.5f);
    log.Append($" Recorrido: {path}. Palanca con el tren encima: {(blockedOk ? "trabada (bien)" : "SE MOVIÓ (mal)")}. Tope: {(touched ? "lo tocó y el resorte lo frenó" : "NO llegó")}{(hitEnd ? " (usó el freno de seguridad)" : "")}, v al soltar el resorte={Kmh():0.0} km/h. Temblor máx. del jugador pasando el cambio: {maxJitter:0.00} mm. Jugador en el tren: {motor.Platform != null}.");

    // 3) Marcha atrás por el cambio hasta la recta 1a.
    Brake(4);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait; // esperar que termine el rebote
    controls.TryMoveReverser(-1); controls.TryMoveReverser(-1);
    Power(2);
    lastSeg = Seg(); path = Seg(); t = 0f;
    while (t < 90f && !(Seg() == "Tramo_1a_Recta" && train.RailPosition.Distance < 40f))
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        if (Seg() != lastSeg) { lastSeg = Seg(); path += " > " + lastSeg; }
    }
    log.Append($" Marcha atrás: {path}, reversor={controls.Reverser}.");
    Brake(4);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;

    // 4) Parada en el cartel: adelante a 20 km/h y freno 2 justo a tiempo (cálculo de frenado).
    lever.Interact(null); // volver a vía directa (tren lejos)
    controls.TryMoveReverser(+1); controls.TryMoveReverser(+1);
    Power(2);
    var settings = controls.Settings;
    float decel = settings.BrakeDeceleration(2) + settings.RollingResistanceMetersPerSecondSquared;
    t = 0f;
    while (t < 90f)
    {
        yield return wait; t += UnityEngine.Time.fixedDeltaTime;
        float front = train.transform.position.z + 9.5f; // la recta 1 va hacia +z
        float v = train.SpeedMetersPerSecond;
        if (sign.position.z - front <= v * v / (2f * decel) && Kmh() > 15f) break;
    }
    Brake(2);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.001f) yield return wait;
    float error = (train.transform.position.z + 9.5f) - sign.position.z;
    log.Append($" Parada en el cartel con freno 2 desde 20 km/h: la punta quedó a {error:+0.00;-0.00} m del cartel (+ = pasado), desvío al final={railSwitch.IsDiverging}.");
    Brake(4);
    UnityEngine.Debug.Log("[M2] " + log);
}
controls.StartCoroutine(Run());
return "prueba M2 iniciada";
