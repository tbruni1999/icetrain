// Prueba del M1: bajar del borde de una caja (0,5 m) no debería sentirse como un tirón al piso.
// Pone al jugador justo pasando el borde de la caja grande (tren detenido) y registra la caída:
// velocidad vertical al despegar, tiempo hasta tocar el piso y mayor bajada de la cámara en un cuadro.
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/caida_de_caja.cs"
// Resultado en la Console, líneas [Caida].

var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var cam = UnityEngine.Camera.main.transform;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var t = typeof(SurvivalEnTren.Player.PlayerMotor);

void PlaceOnTrain(UnityEngine.Vector3 local)
{
    // Ubica al jugador y actualiza su posición relativa al tren (si no, el tren lo devuelve a donde estaba).
    var cc = motor.GetComponent<UnityEngine.CharacterController>();
    cc.enabled = false;
    motor.transform.position = train.transform.TransformPoint(local);
    cc.enabled = true;
    UnityEngine.Physics.SyncTransforms();
    var rel = UnityEngine.Quaternion.Inverse(trainBody.rotation) * (motor.transform.position - trainBody.position);
    if ((UnityEngine.Rigidbody)t.GetField("_platform", flags).GetValue(motor) == trainBody)
    {
        t.GetField("_currentLocal", flags).SetValue(motor, rel);
        t.GetField("_previousLocal", flags).SetValue(motor, rel);
    }
}

System.Collections.IEnumerator Run()
{
    // Arriba de la caja grande (centro local x 0,3 z -4,5, arriba en y 1,5).
    PlaceOnTrain(new UnityEngine.Vector3(0.3f, 1.55f, -4.5f));
    for (int i = 0; i < 60; i++) yield return null;
    // Justo pasando el borde (+x), todavía a la altura de arriba de la caja.
    PlaceOnTrain(new UnityEngine.Vector3(0.88f, 1.52f, -4.5f));

    var wait = new UnityEngine.WaitForEndOfFrame();
    float startY = cam.position.y, lastY = cam.position.y, maxDrop = 0f, time = 0f, firstVy = float.NaN;
    bool left = false;
    while (time < 1.5f)
    {
        yield return wait;
        float vy = (float)t.GetField("_verticalSpeed", flags).GetValue(motor);
        if (!left && !motor.IsGrounded) { left = true; firstVy = vy; }
        maxDrop = UnityEngine.Mathf.Max(maxDrop, lastY - cam.position.y);
        lastY = cam.position.y;
        time += UnityEngine.Time.deltaTime;
        if (left && motor.IsGrounded) break;
    }
    UnityEngine.Debug.Log($"[Caida] velocidad vertical al despegar: {firstVy:0.00} m/s  tiempo de caída: {time:0.00} s  bajada total de cámara: {startY - cam.position.y:0.00} m  mayor bajada en un cuadro: {maxDrop * 100f:0.0} cm  (60 FPS)");
}

motor.StartCoroutine(Run());
return "prueba de caída iniciada";
