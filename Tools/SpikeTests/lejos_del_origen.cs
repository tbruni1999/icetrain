// Prueba del M1 (paso 6): ¿la estrategia A sigue estable lejos del origen del mundo?
// Corre todo el mundo (vía, suelo, postes, tren, cajas, jugador) OFFSET metros en X,
// mete al jugador en el tren, acelera a 60 km/h y mide temblor/deriva del jugador y de las cajas.
// Cómo correrla: con la escena Spike_TrenEstable recién puesta en Play (tren quieto),
//   unity command eval_file --file "Tools/SpikeTests/lejos_del_origen.cs"
// Cambiar OFFSET abajo. Resultado en la Console, líneas [Lejos].

float OFFSET = 20000f;

var shift = new UnityEngine.Vector3(OFFSET, 0f, 0f);
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var ride = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeRideMetrics>();
var props = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikePropMetrics>();
var targetField = typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

// 1) Correr todo el mundo.
foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
{
    if (root.name == "Spike_Controles" || root.name == "Luz_Sol") continue;
    var cc = root.GetComponent<UnityEngine.CharacterController>();
    if (cc != null) cc.enabled = false;
    root.transform.position += shift;
    foreach (var body in root.GetComponentsInChildren<UnityEngine.Rigidbody>())
        body.position = body.transform.position;
    if (cc != null) cc.enabled = true;
}
UnityEngine.Physics.SyncTransforms();
train.SnapToRail();

System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 10; i++) yield return null;
    // 2) Jugador adentro, en el pasillo entre descanso y taller.
    var pcc = motor.GetComponent<UnityEngine.CharacterController>();
    pcc.enabled = false;
    motor.transform.position = train.transform.TransformPoint(new UnityEngine.Vector3(0f, 1.05f, 2f));
    pcc.enabled = true;
    for (int i = 0; i < 120; i++) yield return null;
    props.ResetAnchors();

    // 3) A 60 km/h por la recta y la curva.
    targetField.SetValue(driver, 60f);
    float maxRideJitter = 0f, maxPropJitter = 0f, t = 0f;
    while (t < 75f)
    {
        maxRideJitter = UnityEngine.Mathf.Max(maxRideJitter, ride.JitterMillimeters);
        maxPropJitter = UnityEngine.Mathf.Max(maxPropJitter, props.JitterMillimeters);
        t += UnityEngine.Time.deltaTime;
        yield return null;
    }
    UnityEngine.Debug.Log($"[Lejos] OFFSET={OFFSET:0} m  tren x={train.transform.position.x:0}  tramo={train.RailPosition.Segment.name}  jugadorEnTren={motor.Platform != null}" +
        $"  temblorJugador={maxRideJitter:0.00} mm  deriva={ride.DriftCentimeters:0.00} cm en {ride.StillSeconds:0} s" +
        $"  temblorCajas={maxPropJitter:0.00} mm  corrimientoCajas={props.MaxDisplacementCentimeters:0.00} cm  cajasFuera={props.OutsideCount}");
    targetField.SetValue(driver, 0f);
}
motor.StartCoroutine(Run());
return "prueba lejos del origen iniciada, OFFSET=" + OFFSET;
