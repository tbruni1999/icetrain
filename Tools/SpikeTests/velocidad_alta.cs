// Prueba de 2026-10-16 (Tomi: "que vaya más rápido el tren"): el M1 validó al jugador adentro del tren hasta 60 km/h; esta mide
// lo mismo a 60, 80 y 90 km/h (temblor del jugador visto desde la cámara, deriva parado y cajas apoyadas), para ver si se puede
// subir la velocidad máxima sin que el tren tiemble. Usa el conductor de prueba del M1 (se fija la velocidad directo, sin su tope).
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play (tren quieto),
//   unity command eval_file --file "Tools/SpikeTests/velocidad_alta.cs"
// Resultado en la Console, línea [Velocidad].

var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var ride = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeRideMetrics>();
var props = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikePropMetrics>();
var targetField = typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var log = new System.Text.StringBuilder();

System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 10; i++) yield return null;
    // Jugador adentro, en el pasillo entre descanso y taller.
    var pcc = motor.GetComponent<UnityEngine.CharacterController>();
    pcc.enabled = false;
    motor.transform.position = train.transform.TransformPoint(new UnityEngine.Vector3(0f, 1.05f, 2f));
    pcc.enabled = true;
    for (int i = 0; i < 120; i++) yield return null;
    props.ResetAnchors();
    foreach (float kmh in new[] { 60f, 80f, 90f })
    {
        targetField.SetValue(driver, kmh);
        // Que llegue a la velocidad y después medir por la recta y la curva.
        float t = 0f;
        while (t < 70f && train.SpeedMetersPerSecond * 3.6f < kmh - 1f) { t += UnityEngine.Time.deltaTime; yield return null; }
        float maxRide = 0f, maxProp = 0f, top = 0f;
        t = 0f;
        while (t < 45f)
        {
            maxRide = UnityEngine.Mathf.Max(maxRide, ride.JitterMillimeters);
            maxProp = UnityEngine.Mathf.Max(maxProp, props.JitterMillimeters);
            top = UnityEngine.Mathf.Max(top, train.SpeedMetersPerSecond * 3.6f);
            t += UnityEngine.Time.deltaTime;
            yield return null;
        }
        log.Append($" A {kmh:0} km/h (llegó a {top:0}): temblor del jugador {maxRide:0.00} mm, deriva {ride.DriftCentimeters:0.00} cm, temblor de cajas {maxProp:0.00} mm, cajas corridas {props.MaxDisplacementCentimeters:0.00} cm, cajas fuera {props.OutsideCount}, jugador en el tren={motor.Platform != null}.");
    }
    targetField.SetValue(driver, 0f);
    UnityEngine.Debug.Log($"[Velocidad]{log}");
}
motor.StartCoroutine(Run());
return "prueba de velocidad alta iniciada";
