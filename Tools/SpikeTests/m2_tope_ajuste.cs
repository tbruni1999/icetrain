// Ajuste del tope (M2): prueba combinaciones de rigidez/amortiguación a varias velocidades.
// Pone el tren en la vía muerta a 12 m del tope, ya a la velocidad pedida, y lo deja chocar
// (sin potencia ni freno). Los valores se cambian solo en una copia en memoria de la
// configuración (no toca el asset).
// Cómo correrla: escena M2_Conduccion recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/m2_tope_ajuste.cs"
// Resultado en la Console, líneas [TopeAjuste].

float[][] COMBOS = { new[] { 30f, 10f } };
float[] SPEEDS_KMH = { 5f, 10f, 20f, 30f };

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var lever = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Rail.SwitchLever>();
var spur = UnityEngine.GameObject.Find("Tramo_Desvio_ViaMuerta").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var original = (SurvivalEnTren.Train.TrainDriveSettings)typeof(SurvivalEnTren.Train.TrainControls).GetField("settings", flags).GetValue(controls);
var copy = UnityEngine.Object.Instantiate(original);
typeof(SurvivalEnTren.Train.TrainControls).GetField("settings", flags).SetValue(controls, copy);
var T = typeof(SurvivalEnTren.Train.TrainDriveSettings);
var wait = new UnityEngine.WaitForFixedUpdate();
var log = new System.Text.StringBuilder();

void Place(float kmh)
{
    typeof(SurvivalEnTren.Train.TrainMover).GetField("_railPosition", flags).SetValue(train, new SurvivalEnTren.Rail.RailPosition(spur, spur.Length - 9.5f - 12f));
    train.SnapToRail();
    train.SpeedMetersPerSecond = kmh / 3.6f;
}

System.Collections.IEnumerator Run()
{
    if (!lever.Switch.IsDiverging) lever.Interact(null);
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    foreach (var c in COMBOS)
    {
        T.GetField("bufferStiffnessPerSecondSquared", flags).SetValue(copy, c[0]);
        T.GetField("bufferDampingPerSecond", flags).SetValue(copy, c[1]);
        log.Append($" | k={c[0]:0} c={c[1]:0}:");
        foreach (float kmh in SPEEDS_KMH)
        {
            Place(kmh);
            for (int i = 0; i < 3; i++) yield return wait;
            float maxCompression = 0f, maxDecel = 0f, minSpeed = 0f, last = train.SpeedMetersPerSecond, t = 0f;
            while (t < 12f)
            {
                yield return wait;
                float dt = UnityEngine.Time.fixedDeltaTime; t += dt;
                maxCompression = UnityEngine.Mathf.Max(maxCompression, -train.GapToTrackEnd(1));
                maxDecel = UnityEngine.Mathf.Max(maxDecel, (last - train.SpeedMetersPerSecond) / dt);
                minSpeed = UnityEngine.Mathf.Min(minSpeed, train.SpeedMetersPerSecond);
                last = train.SpeedMetersPerSecond;
            }
            log.Append($" {kmh:0} km/h → {maxCompression * 100f:0} cm, {maxDecel / 9.81f:0.0} g, rebote {-minSpeed * 3.6f:0.0} km/h ({-minSpeed * 3.6f / kmh * 100f:0}%);");
        }
    }
    typeof(SurvivalEnTren.Train.TrainControls).GetField("settings", flags).SetValue(controls, original);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
    UnityEngine.Debug.Log("[TopeAjuste]" + log);
}
controls.StartCoroutine(Run());
return "ajuste del tope iniciado";
