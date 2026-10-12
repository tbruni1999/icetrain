// Prueba del M4 paso 3: comer una ración, dormir en la cama, no dormir con el tren en marcha, y
// desmayarse (vida 0) llevando una caja: despertar en la cama con hambre y poca energía, la caja
// queda donde cayó, pasan las horas.
// Cómo correrla: escena M2_Conduccion recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/m4_comer_dormir_morir.cs"
// Resultado en la Console, líneas [ComerDormir].

var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var respawn = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.SurvivalRespawn>();
var bed = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.Bed>();
var clock = vitals.Clock;
var motor = who.Motor;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var ration = UnityEngine.GameObject.Find("Racion_1").GetComponent<SurvivalEnTren.Survival.FoodItem>();
var box = UnityEngine.GameObject.Find("Caja_Cabina").GetComponent<SurvivalEnTren.Interaction.Carryable>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var T = typeof(SurvivalEnTren.Survival.PlayerVitals);
void Set(string prop, float v) => T.GetProperty(prop).SetValue(vitals, v);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();

System.Collections.IEnumerator Run()
{
    // 1) Comer.
    Set("Satiety", 30f);
    ration.Interact(who);
    yield return null;
    log.Append($"Comer: saciedad 30 -> {vitals.Satiety:0}, la ración desapareció={ration == null}.");

    // 2) Dormir con el tren detenido.
    Set("Energy", 20f);
    double before = clock.TotalHours; float hungerBefore = vitals.Satiety;
    bed.Interact(who);
    for (float t = 0f; t < 5f; t += UnityEngine.Time.unscaledDeltaTime) yield return null;
    log.Append($" Dormir: energía 20 -> {vitals.Energy:0}, pasaron {clock.TotalHours - before:0.0} h, saciedad {hungerBefore:0} -> {vitals.Satiety:0}, puede moverse otra vez={!motor.MovementLocked}.");

    // 3) No dormir con el tren en marcha.
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    controls.MovePowerBrake(+1);
    while (train.SpeedMetersPerSecond < 1f) yield return wait;
    double beforeMoving = clock.TotalHours;
    string prompt = bed.GetPrompt(who);
    bed.Interact(who);
    for (int i = 0; i < 30; i++) yield return null;
    log.Append($" Con el tren en marcha: indicación=\"{prompt}\", durmió={clock.TotalHours - beforeMoving > 1.0}.");
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;

    // 4) Desmayarse llevando una caja.
    carrier.Pick(box);
    for (int i = 0; i < 20; i++) yield return wait;
    var fallLocal = train.transform.InverseTransformPoint(motor.transform.position);
    double beforeDeath = clock.TotalHours;
    vitals.ApplyDamage(500f);
    for (float t = 0f; t < 6f; t += UnityEngine.Time.unscaledDeltaTime) yield return null;
    var boxLocal = train.transform.InverseTransformPoint(box.transform.position);
    var wakeDist = (motor.transform.position - bed.WakePoint).magnitude;
    log.Append($" Desmayo: veces={respawn.KnockoutCount}, pasaron {clock.TotalHours - beforeDeath:0.0} h, despertó a {wakeDist:0.00} m del punto junto a la cama, " +
        $"estados vida {vitals.Health:0} comida {vitals.Satiety:0} energía {vitals.Energy:0} calor {vitals.Warmth:0}, " +
        $"la caja la tiene={carrier.Holding == box} y quedó a {UnityEngine.Vector3.Distance(new UnityEngine.Vector3(boxLocal.x, 0, boxLocal.z), new UnityEngine.Vector3(fallLocal.x, 0, fallLocal.z)):0.0} m de donde cayó, puede moverse={!motor.MovementLocked}.");
    UnityEngine.Debug.Log("[ComerDormir] " + log);
}
motor.StartCoroutine(Run());
return "prueba comer/dormir/desmayo iniciada";
