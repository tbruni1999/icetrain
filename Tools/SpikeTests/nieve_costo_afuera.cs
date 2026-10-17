// Parte 1b (2026-10-17): cuánto cuesta la nieve sin repetición mirando el campo, contra el terreno de URP.
// Cómo correrla: escena M2_Conduccion en Play, con el jugador afuera mirando el campo nevado,
//   unity command eval_file --file "Tools/SpikeTests/nieve_costo_afuera.cs"
// Resultado en la Console, línea [MedirNieve].
var terrain = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Terrain>();
var mat = terrain.materialTemplate;
var nuevo = UnityEngine.Shader.Find("Survival en tren/Terreno Nieve");
var urp = UnityEngine.Shader.Find("Universal Render Pipeline/Terrain/Lit");
terrain.basemapDistance = 250f;
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = -1;
var log = new System.Text.StringBuilder();
System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 30; i++) yield return null;
    for (int vuelta = 0; vuelta < 2; vuelta++)
        foreach (var s in new[] { urp, nuevo })
        {
            mat.shader = s;
            for (int i = 0; i < 60; i++) yield return null; // que compile y se asiente
            float t = 0f; int n = 0;
            while (t < 4f) { yield return null; t += UnityEngine.Time.unscaledDeltaTime; n++; }
            log.Append($" {(s == urp ? "URP" : "nuevo")} {1000f * t / n:0.00} ms;");
        }
    mat.shader = nuevo;
    UnityEngine.Application.targetFrameRate = 60;
    UnityEngine.Debug.Log($"[MedirNieve] pantalla {UnityEngine.Screen.width}x{UnityEngine.Screen.height}:{log}");
}
motor.StartCoroutine(Run());
return "midiendo";
