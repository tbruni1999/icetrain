// Prueba del M2: choque contra el tope de la vía muerta (resorte amortiguado con rebote).
// Pone el cambio en desvío, va a VELOCIDAD km/h sin frenar contra el tope y mide la compresión
// máxima, la desaceleración máxima, la velocidad de rebote y cuánto se movieron las cajas.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (cambiar VELOCIDAD: 1..4 = 10..40 km/h),
//   unity command eval_file --file "Tools/SpikeTests/m2_tope.cs"
// Resultado en la Console, líneas [Tope].

int POTENCIA = 1;

var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
SurvivalEnTren.Rail.SwitchLever lever = null;
foreach (var l in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Rail.SwitchLever>()) if (l.Switch.gameObject.name == "Cambio_1") lever = l; // el cambio manual del M2 (hay otro, el de la bifurcación dañada del M10)
var props = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikePropMetrics>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
bool hardStop = false;
train.ReachedEndOfTrack += () => hardStop = true;
var wait = new UnityEngine.WaitForFixedUpdate();

System.Collections.IEnumerator Run()
{
    if (!lever.Switch.IsDiverging) lever.Interact(null);
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < POTENCIA) controls.MovePowerBrake(+1);
    props.ResetAnchors();

    // Esperar a estar en la vía muerta y cerca del tope.
    while (train.GapToTrackEnd(1) > 3f) yield return wait;
    float impactKmh = train.SpeedMetersPerSecond * 3.6f;
    float maxCompression = 0f, maxDecel = 0f, minSpeed = 0f, last = train.SpeedMetersPerSecond, t = 0f;
    while (t < 6f)
    {
        yield return wait;
        float dt = UnityEngine.Time.fixedDeltaTime;
        t += dt;
        maxCompression = UnityEngine.Mathf.Max(maxCompression, -train.GapToTrackEnd(1));
        maxDecel = UnityEngine.Mathf.Max(maxDecel, (last - train.SpeedMetersPerSecond) / dt);
        minSpeed = UnityEngine.Mathf.Min(minSpeed, train.SpeedMetersPerSecond);
        last = train.SpeedMetersPerSecond;
        if (t > 0.5f && controls.PowerNotch > 0) { while (controls.PowerNotch > 0) controls.MovePowerBrake(-1); }
    }
    UnityEngine.Debug.Log($"[Tope] choque a {impactKmh:0.0} km/h: compresión máx. {maxCompression * 100f:0} cm, desaceleración máx. {maxDecel:0.0} m/s² ({maxDecel / 9.81f:0.0} g), " +
        $"rebote hacia atrás {-minSpeed * 3.6f:0.0} km/h, freno de seguridad usado: {hardStop}. Cajas: corrimiento máx. {props.MaxDisplacementCentimeters:0} cm, fuera del tren {props.OutsideCount}. Jugador en el tren: {motor.Platform != null}.");
}
controls.StartCoroutine(Run());
return "prueba del tope iniciada, potencia " + POTENCIA;
