// Prueba del M1 (paso 6): costo de procesador de la estrategia B (el mundo se mueve en cada tic)
// contra la C (el mundo se re-centra cada tanto), con un mundo cargado de N objetos con colisión.
// - Fase C: 10 s a 60 km/h sin re-centrar + 10 re-centrados forzados (costo puntual de cada uno).
// - Fase B: 10 s a 60 km/h moviendo todo el mundo en cada tic para que el tren quede quieto.
// Mide el tiempo de CPU del hilo principal por cuadro y el de la física (ProfilerRecorder),
// sin límite de FPS. Prototipo de B solo para medir costo: traslada el mundo, no lo rota.
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/costo_b_vs_c.cs"
// Resultado en la Console, líneas [Costo].

int N = 5000;

var origin = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.World.FloatingOrigin>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var targetField = typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", flags);

// Sin re-centrado automático durante la prueba (copia en memoria con umbral máximo).
var settings = UnityEngine.ScriptableObject.CreateInstance<SurvivalEnTren.World.FloatingOriginSettings>();
typeof(SurvivalEnTren.World.FloatingOriginSettings).GetField("recenterDistanceMeters", flags).SetValue(settings, 5000f);
typeof(SurvivalEnTren.World.FloatingOrigin).GetField("settings", flags).SetValue(origin, settings);

// Mundo de prueba: N cubos estáticos con colisión, en una grilla dentro del óvalo de la vía.
var load = new UnityEngine.GameObject("Prueba_Carga");
var mesh = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
var sharedMesh = mesh.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
var sharedMat = mesh.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial;
UnityEngine.Object.Destroy(mesh);
int side = (int)UnityEngine.Mathf.Ceil(UnityEngine.Mathf.Sqrt(N));
for (int i = 0; i < N; i++)
{
    var go = new UnityEngine.GameObject("Carga");
    go.transform.SetParent(load.transform, false);
    go.transform.localPosition = new UnityEngine.Vector3(-150f + (i % side) * 4f, 0.5f, -400f + (i / side) * 11f);
    go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = sharedMesh;
    go.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = sharedMat;
    go.AddComponent<UnityEngine.BoxCollider>();
}

var mainThread = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread", 1);
var physics = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Physics, "Physics.Simulate", 1);

System.Collections.IEnumerator Measure(float seconds, System.Action<double, double, int> result)
{
    double main = 0, phys = 0; int frames = 0; float t = 0f;
    while (t < seconds)
    {
        yield return null;
        main += mainThread.LastValue / 1e6; phys += physics.LastValue / 1e6; frames++;
        t += UnityEngine.Time.unscaledDeltaTime;
    }
    result(main / frames, phys / frames, frames);
}

bool bActive = false;
UnityEngine.Vector3 trainAnchor = UnityEngine.Vector3.zero;
double bShiftMs = 0; int bShifts = 0;
System.Collections.IEnumerator WorldMover()
{
    var wait = new UnityEngine.WaitForFixedUpdate();
    while (true)
    {
        yield return wait;
        if (!bActive) continue;
        UnityEngine.Vector3 delta = trainAnchor - train.transform.position;
        delta.y = 0f;
        origin.Shift(delta);
        bShiftMs += origin.LastShiftMilliseconds; bShifts++;
    }
}

System.Collections.IEnumerator Run()
{
    UnityEngine.QualitySettings.vSyncCount = 0;
    UnityEngine.Application.targetFrameRate = -1;
    var cc = motor.GetComponent<UnityEngine.CharacterController>();
    cc.enabled = false;
    motor.transform.position = train.transform.TransformPoint(new UnityEngine.Vector3(0f, 1.05f, 2f));
    cc.enabled = true;
    targetField.SetValue(driver, 60f);
    while (driver.CurrentSpeedKmh < 59.5f) yield return null;

    double cMain = 0, cPhys = 0, bMain = 0, bPhys = 0; int cFrames = 0, bFrames = 0;
    yield return Measure(10f, (m, p, f) => { cMain = m; cPhys = p; cFrames = f; });

    double worstShift = 0, sumShift = 0;
    for (int i = 0; i < 10; i++)
    {
        origin.Shift(new UnityEngine.Vector3(1f, 0f, 0f));
        worstShift = System.Math.Max(worstShift, origin.LastShiftMilliseconds);
        sumShift += origin.LastShiftMilliseconds;
        yield return null;
    }

    trainAnchor = train.transform.position;
    bActive = true;
    yield return Measure(10f, (m, p, f) => { bMain = m; bPhys = p; bFrames = f; });
    bActive = false;

    UnityEngine.Debug.Log($"[Costo] N={N} objetos con colisión, tren a 60 km/h, sin límite de FPS." +
        $"  C entre re-centrados: hilo principal {cMain:0.000} ms/cuadro, física {cPhys:0.000} ms/cuadro ({cFrames} cuadros)." +
        $"  C re-centrado: promedio {sumShift / 10:0.000} ms, peor {worstShift:0.000} ms (una vez cada ~2 km)." +
        $"  B (mundo movido cada tic): hilo principal {bMain:0.000} ms/cuadro, física {bPhys:0.000} ms/cuadro ({bFrames} cuadros), mover el mundo {bShiftMs / UnityEngine.Mathf.Max(1, bShifts):0.000} ms por tic ({bShifts} tics)." +
        $"  Recorders válidos: main={mainThread.Valid} physics={physics.Valid}");
    targetField.SetValue(driver, 0f);
    UnityEngine.Application.targetFrameRate = 60;
}

motor.StartCoroutine(WorldMover());
motor.StartCoroutine(Run());
return "prueba de costo iniciada con " + N + " objetos";
