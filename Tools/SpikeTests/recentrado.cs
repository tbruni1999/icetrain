// Prueba del M1 (paso 6): ¿se nota el re-centrado del mundo (estrategia C)?
// Baja el umbral de re-centrado a UMBRAL metros (copia en memoria, no toca el asset), mete al
// jugador en el tren, va a 60 km/h y mide en cada cuadro (al final del cuadro, lo que se dibuja):
// - saltos de la cámara y del tren en el mundo, descontando el desplazamiento del re-centrado,
// - temblor/deriva del jugador y de las cajas respecto del tren, y el costo de cada re-centrado.
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/recentrado.cs"
// Resultado en la Console, líneas [Recentrado].

float UMBRAL = 150f;
float DURACION = 75f;

var origin = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.World.FloatingOrigin>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var ride = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeRideMetrics>();
var props = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikePropMetrics>();
var cam = UnityEngine.Camera.main.transform;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var targetField = typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", flags);

// Umbral bajo solo para esta prueba (copia en memoria del asset de configuración).
var settings = UnityEngine.ScriptableObject.CreateInstance<SurvivalEnTren.World.FloatingOriginSettings>();
typeof(SurvivalEnTren.World.FloatingOriginSettings).GetField("recenterDistanceMeters", flags).SetValue(settings, UMBRAL);
typeof(SurvivalEnTren.World.FloatingOrigin).GetField("settings", flags).SetValue(origin, settings);

UnityEngine.Vector3 lastCam = cam.position, lastTrain = train.transform.position;
float maxCamSpeed = 0f, maxTrainSpeed = 0f, maxCamSpeedAtShift = 0f, maxTrainSpeedAtShift = 0f, maxShiftMs = 0f;
float maxRide = 0f, maxProps = 0f;
bool shiftedThisFrame = false;
bool measuring = false;
origin.Shifted += d => { lastCam += d; lastTrain += d; shiftedThisFrame = true; maxShiftMs = UnityEngine.Mathf.Max(maxShiftMs, origin.LastShiftMilliseconds); };

System.Collections.IEnumerator Sampler()
{
    var wait = new UnityEngine.WaitForEndOfFrame();
    while (true)
    {
        yield return wait;
        float dt = UnityEngine.Mathf.Max(1e-4f, UnityEngine.Time.deltaTime);
        float camSpeed = (cam.position - lastCam).magnitude / dt;
        float trainSpeed = (train.transform.position - lastTrain).magnitude / dt;
        lastCam = cam.position;
        lastTrain = train.transform.position;
        if (measuring && dt > 0.008f)
        {
            maxCamSpeed = UnityEngine.Mathf.Max(maxCamSpeed, camSpeed);
            maxTrainSpeed = UnityEngine.Mathf.Max(maxTrainSpeed, trainSpeed);
            if (shiftedThisFrame)
            {
                maxCamSpeedAtShift = UnityEngine.Mathf.Max(maxCamSpeedAtShift, camSpeed);
                maxTrainSpeedAtShift = UnityEngine.Mathf.Max(maxTrainSpeedAtShift, trainSpeed);
            }
            maxRide = UnityEngine.Mathf.Max(maxRide, ride.JitterMillimeters);
            maxProps = UnityEngine.Mathf.Max(maxProps, props.JitterMillimeters);
        }
        shiftedThisFrame = false;
    }
}

System.Collections.IEnumerator Run()
{
    var cc = motor.GetComponent<UnityEngine.CharacterController>();
    cc.enabled = false;
    motor.transform.position = train.transform.TransformPoint(new UnityEngine.Vector3(0f, 1.05f, 2f));
    cc.enabled = true;
    for (int i = 0; i < 120; i++) yield return null;
    props.ResetAnchors();
    int shiftsBefore = origin.ShiftCount;
    targetField.SetValue(driver, 60f);
    measuring = true;
    float t = 0f;
    while (t < DURACION) { t += UnityEngine.Time.deltaTime; yield return null; }
    measuring = false;
    UnityEngine.Debug.Log($"[Recentrado] umbral={UMBRAL:0} m  re-centrados={origin.ShiftCount - shiftsBefore}  costo máx.={maxShiftMs:0.000} ms  v={driver.CurrentSpeedKmh:0} km/h (60 km/h = 16,7 m/s)" +
        $"  velAparenteMáx: cámara={maxCamSpeed:0.0} m/s tren={maxTrainSpeed:0.0} m/s  en cuadros de re-centrado: cámara={maxCamSpeedAtShift:0.0} tren={maxTrainSpeedAtShift:0.0}" +
        $"  temblorJugador={maxRide:0.00} mm  deriva={ride.DriftCentimeters:0.00} cm en {ride.StillSeconds:0} s  temblorCajas={maxProps:0.00} mm  corrimientoCajas={props.MaxDisplacementCentimeters:0.00} cm  cajasFuera={props.OutsideCount}" +
        $"  origenAbsoluto=({origin.AbsoluteOriginX:0},{origin.AbsoluteOriginZ:0})");
    targetField.SetValue(driver, 0f);
}

motor.StartCoroutine(Sampler());
motor.StartCoroutine(Run());
return "prueba de re-centrado iniciada, umbral " + UMBRAL + " m";
