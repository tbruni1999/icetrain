// Prueba del M13 (iluminación): el día y la noche siguen la hora del reloj. Mide a varias horas cuánta luz hay, desde dónde
// viene (de noche tiene que venir de arriba: la luna, nunca desde abajo del suelo), el color de la niebla y la exposición del
// cielo; y que adentro del tren las luces cálidas sigan prendidas (el contraste de la sección 19).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m13_dia_noche.cs" --timeout 60
// Resultado en la Console, líneas [DiaNoche].

var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var cycle = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.World.DayNightCycle>();
var sun = UnityEngine.RenderSettings.sun;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;
    double day0 = System.Math.Floor(clock.TotalHours / 24.0) * 24.0;
    foreach (float h in new[] { 6f, 7.5f, 9f, 12.75f, 16f, 18f, 19.5f, 22f, 2f })
    {
        clock.SetTotalHours(day0 + h);
        for (int i = 0; i < 5; i++) yield return null;
        var fog = UnityEngine.RenderSettings.fogColor;
        float lightFromAbove = UnityEngine.Vector3.Dot(-sun.transform.forward, UnityEngine.Vector3.up);
        log.Append($"[{h:00.00} h] luz del día {cycle.Daylight:0.00}, sol a {cycle.SunElevation:0}°, luz {sun.intensity:0.00} (viene de arriba={lightFromAbove > 0.1f}), niebla {fog.grayscale:0.00}, cielo {UnityEngine.RenderSettings.skybox.GetFloat("_Exposure"):0.00}  ");
    }
    int warm = 0;
    foreach (var l in train.GetComponentsInChildren<UnityEngine.Light>()) if (l.enabled && l.type == UnityEngine.LightType.Point) warm++;
    log.Append($"| luces prendidas adentro del tren: {warm}.");
    clock.SetTotalHours(day0 + 8);
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[DiaNoche] " + log);
}
UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>().StartCoroutine(Run());
return "prueba dia noche iniciada";
