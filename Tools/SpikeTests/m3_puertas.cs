// Prueba del M3 paso 2: puertas corredizas de la locomotora.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (tren detenido),
//   unity command eval_file --file "Tools/SpikeTests/m3_puertas.cs"
// Resultado en la Console, líneas [Puertas].

var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var cc = motor.GetComponent<UnityEngine.CharacterController>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var tm = typeof(SurvivalEnTren.Player.PlayerMotor);
SurvivalEnTren.Interaction.SlidingDoor door = null;
foreach (var d in train.GetComponentsInChildren<SurvivalEnTren.Interaction.SlidingDoor>()) if (d.name == "Puerta_Derecha") door = d;
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();

void PlaceLocal(UnityEngine.Vector3 local)
{
    cc.enabled = false; motor.transform.position = train.transform.TransformPoint(local); cc.enabled = true;
    UnityEngine.Physics.SyncTransforms();
    var rel = UnityEngine.Quaternion.Inverse(trainBody.rotation) * (motor.transform.position - trainBody.position);
    if ((UnityEngine.Rigidbody)tm.GetField("_platform", flags).GetValue(motor) == trainBody) { tm.GetField("_currentLocal", flags).SetValue(motor, rel); tm.GetField("_previousLocal", flags).SetValue(motor, rel); }
}

// Empuja al jugador hacia la puerta derecha (+x local) con CharacterController.Move durante 1,5 s.
System.Collections.IEnumerator WalkOut(System.Action<float> result)
{
    PlaceLocal(new UnityEngine.Vector3(0.6f, 1.05f, -6f));
    for (int i = 0; i < 10; i++) yield return wait;
    for (float t = 0f; t < 1.5f; t += UnityEngine.Time.fixedDeltaTime)
    {
        yield return wait;
        cc.Move(train.transform.right * 3.5f * UnityEngine.Time.fixedDeltaTime);
        var rel = UnityEngine.Quaternion.Inverse(trainBody.rotation) * (motor.transform.position - trainBody.position);
        if ((UnityEngine.Rigidbody)tm.GetField("_platform", flags).GetValue(motor) == trainBody) tm.GetField("_currentLocal", flags).SetValue(motor, rel);
    }
    result(train.transform.InverseTransformPoint(motor.transform.position).x);
}

System.Collections.IEnumerator Run()
{
    log.Append($"Al empezar: {(door.IsOpen ? "abierta" : "cerrada")}.");
    float xClosed = 0f; yield return WalkOut(v => xClosed = v);
    log.Append($" Caminando contra la puerta cerrada: quedó en x={xClosed:0.00} (pared en 1,6 → no pasa si x < 1,6).");

    door.Interact(who);
    for (int i = 0; i < 60; i++) yield return wait;
    float xOpen = 0f; yield return WalkOut(v => xOpen = v);
    log.Append($" Abierta ({door.IsOpen}): llegó a x={xOpen:0.00} (afuera si x > 1,7).");

    // Parado en el hueco: no tiene que cerrar.
    PlaceLocal(new UnityEngine.Vector3(1.6f, 1.05f, -6f));
    for (int i = 0; i < 10; i++) yield return wait;
    door.Interact(who);
    log.Append($" Con el jugador en el hueco: {(door.IsOpen ? "no cerró (bien)" : "CERRÓ (mal)")}.");

    // Desde adentro, mirando el hueco abierto: ¿apunta a la puerta?
    PlaceLocal(new UnityEngine.Vector3(0.3f, 1.05f, -6f));
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, train.transform.eulerAngles.y + 90f));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, 10f);
    for (int i = 0; i < 10; i++) yield return null;
    var target = typeof(SurvivalEnTren.Interaction.PlayerInteractor).GetField("_target", flags).GetValue(who) as SurvivalEnTren.Interaction.IInteractable;
    log.Append($" Mirando el hueco desde adentro: objetivo={(target != null ? target.GetType().Name + " \"" + target.GetPrompt(who) + "\"" : "nada")}.");
    door.Interact(who);
    for (int i = 0; i < 60; i++) yield return wait;
    log.Append($" Después de E: {(door.IsOpen ? "abierta" : "cerrada")}.");
    UnityEngine.Debug.Log("[Puertas] " + log);
}
motor.StartCoroutine(Run());
return "prueba de puertas iniciada";
