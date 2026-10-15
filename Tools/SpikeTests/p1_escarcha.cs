// Prueba de la producción P1 (paso 3): la escarcha de las ventanas del tren sigue a la temperatura de adentro, y el post-proceso
// del ambiente se suma al del cuerpo (paso 1).
// - Con la calefacción andando (interior templado): poca escarcha, solo en los bordes.
// - Sin calefacción, el tren se enfría: la escarcha crece despacio (minutos de juego) hasta cubrir los vidrios.
// - Con la calefacción otra vez: vuelve a los bordes.
// - Post-proceso: la viñeta leve del ambiente está siempre; con el cuerpo frío, la viñeta del frío la supera.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/p1_escarcha.cs" --timeout 60
// Resultado en la Console, línea [Escarcha].

var frost = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.WindowFrost>();
var climate = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.TrainInteriorClimate>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var feedback = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.BodyFeedback>();
var layers = UnityEngine.GameObject.Find("Escarcha_Ventanas");
var log = new System.Text.StringBuilder();
UnityEngine.Application.runInBackground = true;
UnityEngine.Application.targetFrameRate = 60;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var bodyVolume = (UnityEngine.Rendering.Volume)typeof(SurvivalEnTren.Survival.BodyFeedback).GetField("volume", flags).GetValue(feedback);

float Vignette() { bodyVolume.profile.TryGet(out UnityEngine.Rendering.Universal.Vignette v); return v.intensity.value; }
float Temperature() { bodyVolume.profile.TryGet(out UnityEngine.Rendering.Universal.WhiteBalance w); return w.temperature.value; }

System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 30; i++) yield return null;
    log.Append($"VIDRIOS con escarcha: {layers.transform.childCount / 2} (dos capas cada uno). TEMPLADO: interior {climate.InteriorTemperatureC:0.0} °C, escarcha {frost.Amount:0.00}. ");
    log.Append($"POST-PROCESO con el cuerpo bien (calor {vitals.Warmth:0}): viñeta {Vignette():0.00}, temperatura de color {Temperature():0}. ");

    // Se apaga la calefacción y el tren se enfría (el tiempo corre 30 veces más rápido: la escarcha sigue a los minutos de juego).
    climate.HeaterOn = false;
    var samples = new System.Text.StringBuilder();
    for (int step = 1; step <= 6; step++)
    {
        double h0 = clock.TotalHours; clock.SpeedMultiplier = 30f; // 1 s real = media hora de juego
        while (clock.TotalHours - h0 < 0.5) yield return null;
        clock.SpeedMultiplier = 1f;
        samples.Append($" {step * 30} min: {climate.InteriorTemperatureC:0} °C → {frost.Amount:0.00};");
    }
    log.Append($"SIN CALEFACCIÓN (cada media hora de juego):{samples}");
    climate.HeaterOn = true;
    samples.Clear();
    for (int step = 1; step <= 6; step++)
    {
        double h0 = clock.TotalHours; clock.SpeedMultiplier = 30f; // 1 s real = media hora de juego
        while (clock.TotalHours - h0 < 0.5) yield return null;
        clock.SpeedMultiplier = 1f;
        samples.Append($" {step * 30} min: {climate.InteriorTemperatureC:0} °C → {frost.Amount:0.00};");
    }
    log.Append($" CON CALEFACCIÓN OTRA VEZ:{samples}");

    // Cuerpo frío: la viñeta del frío supera a la del ambiente y la imagen se enfría más.
    typeof(SurvivalEnTren.Survival.PlayerVitals).GetProperty("Warmth").SetValue(vitals, 10f);
    for (int i = 0; i < 10; i++) yield return null;
    log.Append($" CUERPO FRÍO (calor 10): viñeta {Vignette():0.00}, temperatura de color {Temperature():0}.");
    typeof(SurvivalEnTren.Survival.PlayerVitals).GetProperty("Warmth").SetValue(vitals, 100f);
    UnityEngine.Debug.Log("[Escarcha] " + log);
}
frost.StartCoroutine(Run());
return "prueba escarcha iniciada";
