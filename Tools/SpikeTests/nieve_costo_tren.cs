// Parte 1b (2026-10-17): cuánto cuesta la nieve sin repetición desde adentro del tren, contra el terreno de URP.
// Cómo correrla: escena M2_Conduccion en Play,
//   unity command eval_file --file "Tools/SpikeTests/nieve_costo_tren.cs"
// Resultado en la Console, línea [MedirTren].
var terrain = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Terrain>();
var mat = terrain.materialTemplate;
var nuevo = UnityEngine.Shader.Find("Survival en tren/Terreno Nieve");
var urp = UnityEngine.Shader.Find("Universal Render Pipeline/Terrain/Lit");
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var body = train.GetComponent<UnityEngine.Rigidbody>();
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = -1;
motor.TeleportTo(body.position + body.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), body);
motor.SetYaw(train.transform.eulerAngles.y);
var log = new System.Text.StringBuilder();
float baseDist = terrain.basemapDistance;
System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 60; i++) yield return null;
    for (int vuelta = 0; vuelta < 3; vuelta++)
        foreach (bool esNuevo in new[] { false, true })
        {
            mat.shader = esNuevo ? nuevo : urp;
            terrain.basemapDistance = 250f;
            for (int i = 0; i < 60; i++) yield return null;
            float t = 0f; int n = 0;
            while (t < 4f) { yield return null; t += UnityEngine.Time.unscaledDeltaTime; n++; }
            log.Append($" {(esNuevo ? "nuevo" : "URP")} {1000f * t / n:0.00} ms;");
        }
    mat.shader = nuevo; terrain.basemapDistance = baseDist;
    UnityEngine.Application.targetFrameRate = 60;
    UnityEngine.Debug.Log($"[MedirTren] pantalla {UnityEngine.Screen.width}x{UnityEngine.Screen.height}:{log}");
}
motor.StartCoroutine(Run());
return "midiendo";
