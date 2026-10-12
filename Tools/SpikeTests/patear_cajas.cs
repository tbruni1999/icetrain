// Prueba del M1: ¿el jugador "patea" las cajas al pisarlas o rozarlas?
// Con el tren a 20 km/h, ubica al jugador apenas metido (5 cm) en el costado y en el borde de
// arriba de varias cajas, como pasa al caminar sobre ellas, y mide la velocidad máxima que toma
// cada caja respecto del tren en el segundo siguiente.
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/patear_cajas.cs"
// Resultado en la Console, líneas [Patear].

var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var t = typeof(SurvivalEnTren.Player.PlayerMotor);
typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", flags).SetValue(driver, 20f);
string[] names = { "Caja_Piso_Grande", "Caja_Piso_Grande_2", "Caja_Cabina" };

void PlaceAt(UnityEngine.Vector3 world)
{
    var cc = motor.GetComponent<UnityEngine.CharacterController>();
    cc.enabled = false;
    motor.transform.position = world;
    cc.enabled = true;
    UnityEngine.Physics.SyncTransforms();
    var rel = UnityEngine.Quaternion.Inverse(trainBody.rotation) * (world - trainBody.position);
    if ((UnityEngine.Rigidbody)t.GetField("_platform", flags).GetValue(motor) == trainBody)
    {
        t.GetField("_currentLocal", flags).SetValue(motor, rel);
        t.GetField("_previousLocal", flags).SetValue(motor, rel);
    }
}

System.Collections.IEnumerator Watch(UnityEngine.Rigidbody box, System.Action<float> result)
{
    float max = 0f, time = 0f;
    while (time < 1f)
    {
        yield return new UnityEngine.WaitForFixedUpdate();
        var rel = box.linearVelocity - trainBody.GetPointVelocity(box.worldCenterOfMass);
        max = UnityEngine.Mathf.Max(max, rel.magnitude);
        time += UnityEngine.Time.fixedDeltaTime;
    }
    result(max);
}

System.Collections.IEnumerator Run()
{
    while (driver.CurrentSpeedKmh < 19.5f) yield return null;
    // Arranca adentro, en el pasillo.
    PlaceAt(train.transform.TransformPoint(new UnityEngine.Vector3(0f, 1.05f, 2f)));
    for (int i = 0; i < 60; i++) yield return null;

    string report = "";
    float worst = 0f;
    foreach (var name in names)
    {
        var box = UnityEngine.GameObject.Find(name).GetComponent<UnityEngine.Rigidbody>();
        var size = box.transform.lossyScale;
        // a) Rozando el costado de la caja (5 cm metido), a la altura del piso.
        var c = box.transform.position;
        var side = train.transform.right * (size.x * 0.5f + 0.3f - 0.05f);
        PlaceAt(new UnityEngine.Vector3(c.x, c.y - size.y * 0.5f + 0.02f, c.z) + side);
        float a = 0f; yield return Watch(box, v => a = v);
        // b) Parado en el borde de arriba (medio cuerpo afuera).
        PlaceAt(new UnityEngine.Vector3(c.x, c.y + size.y * 0.5f + 0.02f, c.z) + train.transform.right * (size.x * 0.5f));
        float b = 0f; yield return Watch(box, v => b = v);
        // c) Caminando de verdad (CharacterController.Move a 3,5 m/s) contra la caja y por encima, 1,2 s.
        PlaceAt(new UnityEngine.Vector3(c.x, c.y - size.y * 0.5f + 0.02f, c.z) + train.transform.right * (size.x * 0.5f + 0.9f));
        for (int i = 0; i < 10; i++) yield return new UnityEngine.WaitForFixedUpdate();
        float w = 0f;
        var cc = motor.GetComponent<UnityEngine.CharacterController>();
        System.Collections.IEnumerator WalkInto()
        {
            for (float time = 0f; time < 1.2f; time += UnityEngine.Time.fixedDeltaTime)
            {
                yield return new UnityEngine.WaitForFixedUpdate();
                cc.Move(-train.transform.right * 3.5f * UnityEngine.Time.fixedDeltaTime);
                // mantener la posición relativa al tren al día, para que el tren no lo devuelva
                var rel = UnityEngine.Quaternion.Inverse(trainBody.rotation) * (motor.transform.position - trainBody.position);
                if ((UnityEngine.Rigidbody)t.GetField("_platform", flags).GetValue(motor) == trainBody)
                    t.GetField("_currentLocal", flags).SetValue(motor, rel);
            }
        }
        motor.StartCoroutine(WalkInto());
        yield return Watch(box, v => w = v);
        report += $"  {name}: rozando {a:0.00} m/s, en el borde {b:0.00} m/s, caminando contra/encima {w:0.00} m/s;";
        worst = UnityEngine.Mathf.Max(worst, UnityEngine.Mathf.Max(a, UnityEngine.Mathf.Max(b, w)));
        PlaceAt(train.transform.TransformPoint(new UnityEngine.Vector3(0f, 1.05f, 2f)));
        for (int i = 0; i < 30; i++) yield return null;
    }
    UnityEngine.Debug.Log($"[Patear] velocidad máxima que toma cada caja respecto del tren (0 = no la mueve):{report}  peor: {worst:0.00} m/s");
    typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", flags).SetValue(driver, 0f);
}

motor.StartCoroutine(Run());
return "prueba de patear cajas iniciada";
