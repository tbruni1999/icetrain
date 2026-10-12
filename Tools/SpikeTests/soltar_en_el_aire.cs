// Prueba del M1: soltar una caja (G) mientras el jugador está en el aire (saltando) dentro del
// tren a 40 km/h. La caja tiene que salir con la velocidad del jugador (que viaja con el tren),
// no quieta; si sale quieta, el tren la "patea".
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/soltar_en_el_aire.cs"
// Resultado en la Console, líneas [SoltarAire].

var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var dropper = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikePropDropper>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var t = typeof(SurvivalEnTren.Player.PlayerMotor);
var onDrop = typeof(SurvivalEnTren.Spike.SpikePropDropper).GetMethod("OnDrop", flags);
typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", flags).SetValue(driver, 40f);

System.Collections.IEnumerator Run()
{
    while (driver.CurrentSpeedKmh < 39.5f) yield return null;
    var cc = motor.GetComponent<UnityEngine.CharacterController>();
    cc.enabled = false; motor.transform.position = train.transform.TransformPoint(new UnityEngine.Vector3(0f, 1.05f, 2f)); cc.enabled = true;
    UnityEngine.Physics.SyncTransforms();
    for (int i = 0; i < 90; i++) yield return null;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, train.transform.eulerAngles.y));
    t.GetField("_jumpRequested", flags).SetValue(motor, true);
    for (int i = 0; i < 12; i++) yield return new UnityEngine.WaitForFixedUpdate();
    bool inAir = !motor.IsGrounded;
    var before = new System.Collections.Generic.HashSet<UnityEngine.Rigidbody>(UnityEngine.Object.FindObjectsByType<UnityEngine.Rigidbody>());
    onDrop.Invoke(dropper, new object[] { default(UnityEngine.InputSystem.InputAction.CallbackContext) });
    UnityEngine.Rigidbody box = null; foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.Rigidbody>()) if (!before.Contains(b)) box = b;
    float startRel = (box.linearVelocity - trainBody.GetPointVelocity(box.worldCenterOfMass)).magnitude;
    float max = 0f;
    for (int i = 0; i < 75; i++) { yield return new UnityEngine.WaitForFixedUpdate(); max = UnityEngine.Mathf.Max(max, (box.linearVelocity - trainBody.GetPointVelocity(box.worldCenterOfMass)).magnitude); }
    UnityEngine.Debug.Log($"[SoltarAire] jugador en el aire al soltar: {inAir}.  Velocidad de la caja respecto del tren al nacer: {startRel:0.00} m/s, máxima en 1,5 s: {max:0.00} m/s (una caída normal da ~4 m/s; el tren va a 11,1 m/s).");
    typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", flags).SetValue(driver, 0f);
}
motor.StartCoroutine(Run());
return "prueba soltar en el aire iniciada";
