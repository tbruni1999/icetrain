// Prueba del M4 paso 4: feedback del cuerpo (frío, cansancio) y daño por caída.
// Cómo correrla: escena M2_Conduccion recién puesta en Play,
//   unity command eval_file --file "Tools/SpikeTests/m4_feedback_y_caida.cs"
// Resultado en la Console, líneas [Feedback]. Deja al jugador frío para una captura al final.

var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var feedback = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.BodyFeedback>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var T = typeof(SurvivalEnTren.Survival.PlayerVitals);
void Set(string prop, float v) => T.GetProperty(prop).SetValue(vitals, v);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();

System.Collections.IEnumerator Run()
{
    // Caída de 6 m sobre la nieve, al costado del tren.
    var ground = train.transform.TransformPoint(new UnityEngine.Vector3(7f, -0.6f, 0f));
    motor.TeleportTo(ground + UnityEngine.Vector3.up * 6f, null);
    float before = vitals.Health;
    for (int i = 0; i < 120; i++) yield return wait;
    log.Append($"Caída de 6 m: vida {before:0} -> {vitals.Health:0} (esperado ~ -65).");
    // Salto normal: no tiene que doler.
    Set("Health", 100f);
    motor.TeleportTo(ground + UnityEngine.Vector3.up * 0.9f, null);
    for (int i = 0; i < 60; i++) yield return wait;
    log.Append($" Caída de 0,9 m (como un salto): vida 100 -> {vitals.Health:0}.");

    // Cansancio.
    Set("Energy", 8f);
    for (int i = 0; i < 10; i++) yield return null;
    log.Append($" Energía 8: cansancio={feedback.Tiredness:0.00}, puede correr={motor.CanRun}.");
    Set("Energy", 0f);
    for (int i = 0; i < 10; i++) yield return null;
    log.Append($" Energía 0: puede correr={motor.CanRun}, velocidad x{motor.FatigueMultiplier:0.00}, movimiento bloqueado={motor.MovementLocked}.");
    Set("Energy", 80f);

    // Frío (afuera, a -25 °C).
    Set("Warmth", 15f);
    for (int i = 0; i < 20; i++) yield return null;
    log.Append($" Calor 15 afuera: escarcha={feedback.Frost:0.00}, temblor={feedback.Shiver:0.00}, vaho={feedback.BreathVisible}, aire {vitals.AmbientTemperatureC:0} °C.");
    UnityEngine.Debug.Log("[Feedback] " + log);
}
motor.StartCoroutine(Run());
return "prueba de feedback iniciada";
