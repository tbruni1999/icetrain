// Prueba del M1: el jugador parado arriba de una caja dentro del tren en movimiento
// tiene que viajar con la caja (y la caja con el tren), sin quedarse atrás.
// Cómo correrla: escena Spike_TrenEstable recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/parado_en_caja.cs"
// Resultado en la Console, líneas [Caja].

var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var driver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainTestDriver>();
var box = UnityEngine.GameObject.Find("Caja_Piso_Grande").GetComponent<UnityEngine.Rigidbody>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var targetField = typeof(SurvivalEnTren.Train.TrainTestDriver).GetField("_targetSpeedKmh", flags);

// Posición dibujada (suavizada) del jugador, comparable con la del tren y la caja dibujados.
UnityEngine.Vector3 PlayerOnTrain() => train.transform.InverseTransformPoint(motor.RenderFeetPosition);
UnityEngine.Vector3 BoxOnTrain() => train.transform.InverseTransformPoint(box.transform.position);

System.Collections.IEnumerator Run()
{
    var cc = motor.GetComponent<UnityEngine.CharacterController>();
    cc.enabled = false;
    motor.transform.position = box.transform.position + UnityEngine.Vector3.up * 0.3f;
    cc.enabled = true;
    for (int i = 0; i < 90; i++) yield return null;

    var startPlayer = PlayerOnTrain();
    var startBox = BoxOnTrain();
    string platformName = motor.Platform != null ? motor.Platform.name : "ninguna";
    float maxPlayerShift = 0f;
    float maxBoxShift = 0f;

    System.Collections.IEnumerator Phase(float kmh, float seconds)
    {
        targetField.SetValue(driver, kmh);
        float t = 0f;
        while (t < seconds)
        {
            maxPlayerShift = UnityEngine.Mathf.Max(maxPlayerShift, (PlayerOnTrain() - startPlayer).magnitude);
            maxBoxShift = UnityEngine.Mathf.Max(maxBoxShift, (BoxOnTrain() - startBox).magnitude);
            t += UnityEngine.Time.deltaTime;
            yield return null;
        }
    }

    yield return Phase(60f, 40f);
    string platformAtSpeed = motor.Platform != null ? motor.Platform.name : "ninguna";
    yield return Phase(0f, 20f);

    UnityEngine.Debug.Log($"[Caja] pisando al empezar: {platformName}, a 60 km/h: {platformAtSpeed}." +
        $"  Corrimiento máx. respecto del tren: jugador {maxPlayerShift * 100f:0.00} cm, caja {maxBoxShift * 100f:0.00} cm (acelerar a 60, curva, frenar).");
}

motor.StartCoroutine(Run());
return "prueba parado en caja iniciada";
