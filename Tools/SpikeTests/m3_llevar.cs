// Prueba del M3 paso 3: llevar objetos en la mano, también con el tren en marcha.
// Cómo correrla: escena M2_Conduccion recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/m3_llevar.cs"
// Resultado en la Console, líneas [Llevar].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var cam = UnityEngine.Camera.main.transform;
var cc = motor.GetComponent<UnityEngine.CharacterController>();
var tm = typeof(SurvivalEnTren.Player.PlayerMotor);
var big = UnityEngine.GameObject.Find("Caja_Piso_Grande").GetComponent<SurvivalEnTren.Interaction.Carryable>();
var small = UnityEngine.GameObject.Find("Caja_Cabina").GetComponent<SurvivalEnTren.Interaction.Carryable>();
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();

void PlaceLocal(UnityEngine.Vector3 local, float yawFromTrain)
{
    cc.enabled = false; motor.transform.position = train.transform.TransformPoint(local); cc.enabled = true;
    UnityEngine.Physics.SyncTransforms();
    var rel = UnityEngine.Quaternion.Inverse(trainBody.rotation) * (motor.transform.position - trainBody.position);
    if ((UnityEngine.Rigidbody)tm.GetField("_platform", flags).GetValue(motor) == trainBody) { tm.GetField("_currentLocal", flags).SetValue(motor, rel); tm.GetField("_previousLocal", flags).SetValue(motor, rel); }
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, train.transform.eulerAngles.y + yawFromTrain));
}
float RelSpeed(UnityEngine.Rigidbody b) => (b.linearVelocity - trainBody.GetPointVelocity(b.worldCenterOfMass)).magnitude;

System.Collections.IEnumerator Run()
{
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, 15f);
    // 1) Tomar la caja grande (10 kg) parado al lado.
    PlaceLocal(new UnityEngine.Vector3(0.3f, 1.05f, -3.2f), 180f);
    for (int i = 0; i < 30; i++) yield return wait;
    big.Interact(who);
    for (int i = 0; i < 30; i++) yield return wait;
    log.Append($"Tomar 10 kg: sostiene={carrier.Holding == big}, indicación=\"{big.GetPrompt(who)}\", velocidad de caminar x{motor.SpeedMultiplier:0.00}.");

    // 2) Sostenerla con el tren a 40 km/h, 20 s (incluye llegar a velocidad).
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 4) controls.MovePowerBrake(+1);
    var endOfFrame = new UnityEngine.WaitForEndOfFrame();
    UnityEngine.Vector3 l1 = UnityEngine.Vector3.zero, l2 = UnityEngine.Vector3.zero; int n = 0; float maxJitter = 0f, maxOffset = 0f, t = 0f;
    while (t < 20f && carrier.Holding == big)
    {
        yield return endOfFrame;
        t += UnityEngine.Time.deltaTime;
        var local = cam.InverseTransformPoint(big.transform.position); // la caja vista desde la cámara
        if (n >= 2) maxJitter = UnityEngine.Mathf.Max(maxJitter, (local - 2f * l1 + l2).magnitude * 1000f);
        l2 = l1; l1 = local; n++;
        maxOffset = UnityEngine.Mathf.Max(maxOffset, UnityEngine.Mathf.Abs(local.z - 1.1f));
    }
    log.Append($" Sosteniendo a {train.SpeedMetersPerSecond * 3.6f:0} km/h por {t:0} s: sigue en la mano={carrier.Holding == big}, temblor visto desde la cámara máx. {maxJitter:0.0} mm, se alejó de la mano hasta {maxOffset * 100f:0} cm.");

    // 3) Soltarla en marcha: tiene que salir con la velocidad del tren.
    big.Interact(who);
    float maxRel = 0f;
    for (int i = 0; i < 100; i++) { yield return wait; maxRel = UnityEngine.Mathf.Max(maxRel, RelSpeed(big.Body)); }
    var rest = train.transform.InverseTransformPoint(big.transform.position);
    log.Append($" Soltarla en marcha: velocidad máx. respecto del tren {maxRel:0.00} m/s (caída), quedó adentro={UnityEngine.Mathf.Abs(rest.x) < 2.1f && rest.y > 0.8f}.");

    // 4) Lanzar la chica (3 kg) hacia adelante del tren.
    PlaceLocal(new UnityEngine.Vector3(0.6f, 1.05f, 5.8f), 180f);
    for (int i = 0; i < 20; i++) yield return wait;
    var smallStart = small.transform.position;
    PlaceLocal(train.transform.InverseTransformPoint(small.transform.position) + new UnityEngine.Vector3(0f, 0.05f, 1.2f), 180f);
    for (int i = 0; i < 20; i++) yield return wait;
    small.Interact(who);
    for (int i = 0; i < 20; i++) yield return wait;
    bool heldSmall = carrier.Holding == small;
    typeof(SurvivalEnTren.Interaction.PlayerCarrier).GetMethod("Drop").Invoke(carrier, new object[] { true });
    yield return wait;
    log.Append($" Lanzar 3 kg: la tenía={heldSmall}, sale a {RelSpeed(small.Body):0.00} m/s respecto del tren (esperado ~4).");
    for (int i = 0; i < 100; i++) yield return wait;

    // 5) Demasiado pesado.
    float m = big.Body.mass; big.Body.mass = 30f;
    log.Append($" Con 30 kg: indicación=\"{big.GetPrompt(who)}\", se puede levantar={carrier.CanLift(big)}.");
    big.Body.mass = m;

    // 6) Trabarla contra una pared: tiene que soltarse sola.
    PlaceLocal(new UnityEngine.Vector3(0.6f, 1.05f, 2.0f), 0f);
    for (int i = 0; i < 10; i++) yield return wait;
    var smallLocal = train.transform.InverseTransformPoint(small.transform.position);
    PlaceLocal(smallLocal + new UnityEngine.Vector3(0f, 0.05f, -1.0f), 0f);
    for (int i = 0; i < 10; i++) yield return wait;
    small.Interact(who);
    for (int i = 0; i < 10; i++) yield return wait;
    bool heldBefore = carrier.Holding == small;
    PlaceLocal(new UnityEngine.Vector3(1.7f, 1.05f, 2.0f), 90f); // mirando la pared derecha (cara interior en 2,1), a 0,4 m
    for (int i = 0; i < 60; i++) yield return wait;
    log.Append($" Contra la pared: la tenía={heldBefore}, la sigue teniendo apoyada contra la pared={carrier.Holding == small}, no la atravesó (sigue adentro del tren)={UnityEngine.Mathf.Abs(train.transform.InverseTransformPoint(small.transform.position).x) < 2.15f}.");

    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
    UnityEngine.Debug.Log("[Llevar] " + log);
}
motor.StartCoroutine(Run());
return "prueba llevar iniciada";
