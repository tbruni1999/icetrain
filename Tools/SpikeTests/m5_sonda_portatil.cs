// Prueba del M5 paso 3: ir a una señal y volver al tren usando solo la sonda portátil (sección 24),
// interferencias y "con una caja en la mano no se usa".
// El "jugador" de la prueba solo mira la sonda: gira hacia donde apunta la aguja y camina; se frena
// cuando el texto de la sonda dice que llegó. No usa las posiciones reales para orientarse.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/m5_sonda_portatil.cs"
// Resultado en la Console, líneas [SondaPortatil].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var scanner = train.GetComponent<SurvivalEnTren.Probe.ProbeScanner>();
var probe = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Probe.PortableProbe>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var cc = motor.GetComponent<UnityEngine.CharacterController>();
var respawn = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.SurvivalRespawn>();
var tm = typeof(SurvivalEnTren.Player.PlayerMotor);
var toLocal = tm.GetMethod("ToReferenceLocal", flags);
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;
var device = (UnityEngine.Transform)typeof(SurvivalEnTren.Probe.PortableProbe).GetField("device", flags).GetValue(probe);
var trainText = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.PortableProbe).GetField("trainText", flags).GetValue(probe);
var targetText = (UnityEngine.TextMesh)typeof(SurvivalEnTren.Probe.PortableProbe).GetField("targetText", flags).GetValue(probe);
var big = UnityEngine.GameObject.Find("Caja_Piso_Grande").GetComponent<SurvivalEnTren.Interaction.Carryable>();
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;

float Flat(UnityEngine.Vector3 a, UnityEngine.Vector3 b) { a.y = 0f; b.y = 0f; return UnityEngine.Vector3.Distance(a, b); }

// Camina siguiendo una aguja hasta que el texto diga que llegó (o se trabe / se acabe el tiempo).
System.Collections.IEnumerator Walk(System.Func<float> needle, UnityEngine.TextMesh text, string arrived, float[] result)
{
    float t = 0f, path = 0f, stuck = 0f;
    while (t < 120f && text.text != arrived && stuck < 2f)
    {
        yield return wait;
        float dt = UnityEngine.Time.fixedDeltaTime;
        t += dt;
        motor.AddYaw(UnityEngine.Mathf.Clamp(needle(), -120f * dt, 120f * dt)); // gira como con el mouse, sin teletransportar la vista
        var before = motor.transform.position;
        // Trotar pegado al piso: el mismo empuje hacia abajo que usa el motor (2 m/s). Sin él, en las bajadas de las dunas
        // (producción P1) el paso de la prueba despega al jugador y el motor acumula velocidad de caída hasta "matarlo".
        cc.Move(motor.transform.forward * 5f * dt + UnityEngine.Vector3.down * 2f * dt);
        // Que el suavizado del motor no deshaga el paso (en la referencia actual: el mundo o lo que pisa).
        var local = toLocal.Invoke(motor, new object[] { motor.transform.position });
        tm.GetField("_currentLocal", flags).SetValue(motor, local);
        tm.GetField("_previousLocal", flags).SetValue(motor, local);
        float moved = UnityEngine.Vector3.ProjectOnPlane(motor.transform.position - before, UnityEngine.Vector3.up).magnitude;
        path += moved;
        stuck = moved < 5f * dt * 0.2f ? stuck + dt : 0f;
    }
    result[0] = t; result[1] = path;
}

System.Collections.IEnumerator Run()
{
    // 0) Fijar en la consola la señal más cercana y bajar del tren por la puerta.
    scanner.SelectNext();
    var target = scanner.Target;
    var door = train.transform.TransformPoint(new UnityEngine.Vector3(1.7f + 2.5f, 0f, -6f));
    motor.TeleportTo(new UnityEngine.Vector3(door.x, 0.6f, door.z), null);
    for (int i = 0; i < 30; i++) yield return wait;
    probe.SetOut(true);
    for (int i = 0; i < 60; i++) yield return wait;
    log.Append($"Objetivo: {target.StableId} a {Flat(target.transform.position, motor.transform.position):0} m reales. Sonda afuera={probe.IsOut}, aparato visible={device.gameObject.activeSelf}, \"{trainText.text}\" / \"{targetText.text}\", calidad {probe.Quality:0.00}.");

    // 1) Ir a la señal solo con la aguja ámbar.
    var r = new float[2];
    float straight = Flat(target.transform.position, motor.transform.position);
    yield return Walk(() => probe.TargetNeedleDegrees, targetText, "OBJ.  ~10 M", r);
    float miss = Flat(target.transform.position, motor.transform.position);
    log.Append($" IDA: {r[0]:0} s, caminó {r[1]:0} m para {straight:0} m en línea recta ({r[1] / straight * 100f:0}%), terminó a {miss:0.0} m de la señal, texto \"{targetText.text}\", desmayos {respawn.KnockoutCount} (debe ser 0).");

    // 2) Volver al tren solo con la aguja blanca.
    straight = Flat(train.transform.position, motor.transform.position);
    yield return Walk(() => probe.TrainNeedleDegrees, trainText, "TREN  ~10 M", r);
    float back = Flat(train.transform.position, motor.transform.position);
    log.Append($" VUELTA: {r[0]:0} s, caminó {r[1]:0} m para {straight:0} m en línea recta, terminó a {back:0.0} m del centro del tren (el tren mide ~22 m), texto \"{trainText.text}\".");

    // 3) Interferencia: al lado de la masa metálica (zona de 40 m) contra lejos de ella.
    SurvivalEnTren.Probe.ProbeSignal metal = null;
    for (int i = 0; i < 5 && (metal == null || scanner.Target != metal); i++)
    {
        scanner.SelectNext();
        if (scanner.Target != null && scanner.Target.Type == SurvivalEnTren.Probe.ProbeSignalType.MasaMetalica) metal = scanner.Target;
    }
    System.Collections.IEnumerator Jitter(float offset, float[] o)
    {
        var p = metal.transform.position + new UnityEngine.Vector3(offset, 0.6f, 0f);
        motor.TeleportTo(p, null);
        for (int i = 0; i < 90; i++) yield return wait;
        float min = 999f, max = -999f, q = 0f; int n = 0;
        for (int i = 0; i < 150; i++)
        {
            yield return wait;
            float a = probe.TargetNeedleDegrees;
            min = UnityEngine.Mathf.Min(min, a); max = UnityEngine.Mathf.Max(max, a); q += probe.Quality; n++;
        }
        o[0] = max - min; o[1] = q / n; o[2] = probe.Interference;
    }
    var nearZone = new float[3]; var farZone = new float[3];
    yield return Jitter(10f, nearZone);
    yield return Jitter(70f, farZone);
    log.Append($" INTERFERENCIA (objetivo {metal?.StableId}): a 10 m del metal la aguja ámbar se movió {nearZone[0]:0} grados en 3 s, interferencia {nearZone[2]:0.00}, calidad media {nearZone[1]:0.00}; a 70 m (fuera de la zona) se movió {farZone[0]:0.0} grados, interferencia {farZone[2]:0.00}, calidad {farZone[1]:0.00}.");

    // 4) Con una caja en la mano la sonda se guarda; al soltarla vuelve.
    var boxLocal = train.transform.InverseTransformPoint(big.transform.position);
    motor.TeleportTo(train.transform.TransformPoint(boxLocal + new UnityEngine.Vector3(0f, 0.1f, 1.0f)), trainBody);
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, train.transform.eulerAngles.y + 180f));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>(), 35f);
    for (int i = 0; i < 30; i++) yield return wait;
    big.Interact(who);
    for (int i = 0; i < 40; i++) yield return wait;
    bool held = carrier.Holding == big, hiddenWhileHolding = !device.gameObject.activeSelf;
    big.Interact(who);
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($" CAJA: la tomó={held}, sonda guardada con la caja={hiddenWhileHolding}; la soltó={carrier.Holding == null}, la sonda volvió={device.gameObject.activeSelf}.");

    // 5) Guardar con la tecla (SetOut false) y volver a sacar.
    probe.SetOut(false);
    for (int i = 0; i < 40; i++) yield return wait;
    bool stowed = !device.gameObject.activeSelf;
    probe.SetOut(true);
    UnityEngine.Debug.Log($"[SondaPortatil] {log} GUARDAR: se guardó={stowed}.");
}
motor.StartCoroutine(Run());
return "prueba sonda portátil iniciada";
