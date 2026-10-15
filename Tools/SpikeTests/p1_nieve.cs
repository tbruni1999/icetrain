// Prueba de la producción P1 (paso 2): la nieve que cae y el viento.
// - Afuera: hay copos alrededor del jugador (cerca y a la altura de los ojos), el viento sopla con ráfagas.
// - Adentro del tren andando a ~40 km/h: no hay copos adentro (la caja "Zona_SinNieve" los apaga aunque el tren se mueva).
// - El origen flotante re-centra el mundo: los copos quedan donde estaban respecto del jugador.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/p1_nieve.cs" --timeout 60
// Resultado en la Console, línea [Nieve].

var weather = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.World.SnowWeather>();
var flakes = weather.transform.Find("Nieve_Copos").GetComponent<UnityEngine.ParticleSystem>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var origin = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.World.FloatingOrigin>();
var noSnow = train.transform.Find("Zona_SinNieve").GetComponent<UnityEngine.BoxCollider>();
var head = UnityEngine.Camera.main.transform;
var log = new System.Text.StringBuilder();
var buffer = new UnityEngine.ParticleSystem.Particle[flakes.main.maxParticles];
UnityEngine.Application.runInBackground = true;
UnityEngine.Application.targetFrameRate = 60;

int Near(float radius)
{
    int n = flakes.GetParticles(buffer), count = 0;
    for (int i = 0; i < n; i++) { var d = buffer[i].position - head.position; if (d.magnitude < radius) count++; }
    return count;
}
int InsideTrain()
{
    int n = flakes.GetParticles(buffer), count = 0;
    var box = noSnow.transform;
    for (int i = 0; i < n; i++)
    {
        var local = box.InverseTransformPoint(buffer[i].position) - noSnow.center;
        var half = noSnow.size * 0.5f - new UnityEngine.Vector3(0.15f, 0.15f, 0.15f); // un margen: un copo que recién toca el borde muere en el cuadro siguiente
        if (UnityEngine.Mathf.Abs(local.x) < half.x && UnityEngine.Mathf.Abs(local.y) < half.y && UnityEngine.Mathf.Abs(local.z) < half.z) count++;
    }
    return count;
}

System.Collections.IEnumerator Run()
{
    // 1) Afuera, quieto en la nieve.
    var outside = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(9f, 0f, 0f); outside.y = 0.1f;
    motor.TeleportTo(outside, null);
    for (int i = 0; i < 600; i++) yield return null; // 10 s: que la nevada se asiente
    float windMin = 99f, windMax = 0f;
    for (int i = 0; i < 300; i++) { yield return null; float w = weather.Wind.magnitude; if (w < windMin) windMin = w; if (w > windMax) windMax = w; }
    log.Append($"AFUERA: copos vivos {weather.AliveFlakes}, a menos de 5 m de los ojos {Near(5f)}, a menos de 10 m {Near(10f)}; rachas a ras del suelo {weather.AliveDrift}; nevada {weather.Intensity:0.00}; viento entre {windMin:0.0} y {windMax:0.0} m/s en 5 s.");

    // 2) Adentro del tren, andando.
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 0f), trainBody);
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 4) controls.MovePowerBrake(+1);
    int maxInside = 0; float t = 0f;
    while (t < 25f) { yield return null; t += UnityEngine.Time.deltaTime; int inside = InsideTrain(); if (inside > maxInside) maxInside = inside; }
    log.Append($" ADENTRO DEL TREN a {train.SpeedMetersPerSecond * 3.6f:0} km/h durante 25 s: copos adentro como máximo {maxInside} (debe ser 0); al costado del tren, a menos de 6 m del jugador {Near(6f)} y de 10 m {Near(10f)} (se ven pasar por las ventanas).");
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);

    // 3) Re-centrado del origen: los copos acompañan.
    int n0 = flakes.GetParticles(buffer);
    var before = buffer[n0 / 2].position - head.position; uint seed = buffer[n0 / 2].randomSeed;
    origin.Shift(new UnityEngine.Vector3(-700f, 0f, 400f));
    int n1 = flakes.GetParticles(buffer); var after = before; bool found = false;
    for (int i = 0; i < n1; i++) if (buffer[i].randomSeed == seed) { after = buffer[i].position - head.position; found = true; }
    log.Append($" RE-CENTRADO (806 m): un copo respecto del jugador antes {before:F2}, después {after:F2} (encontrado {found}, diferencia {(after - before).magnitude * 100f:0.0} cm).");
    UnityEngine.Debug.Log("[Nieve] " + log);
}
motor.StartCoroutine(Run());
return "prueba nieve iniciada";
