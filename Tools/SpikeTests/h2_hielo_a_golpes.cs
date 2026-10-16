// Prueba de herramientas y forja, paso H2: el hielo de la puerta de la estación también se rompe a golpes de barreta
// (mantener E sigue andando). Apunta de verdad: se para frente al hielo, lo mira y golpea hasta que cede.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/h2_hielo_a_golpes.cs"
// Resultado en la Console, línea [HieloGolpes].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var belt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
var sounds = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Audio.WorldSounds>();
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;

SurvivalEnTren.Poi.FrozenDoor ice = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Poi.FrozenDoor>()) if (d.StableId.Contains("estacion")) ice = d;

System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 20; i++) yield return wait;
    // El centro del hielo y hacia dónde mira la puerta (afuera = el lado del que se llega).
    var rs = ice.GetComponentsInChildren<UnityEngine.Renderer>();
    var bounds = rs[0].bounds; foreach (var r in rs) bounds.Encapsulate(r.bounds);
    var center = bounds.center;
    var outward = ice.transform.right; outward.y = 0f; outward.Normalize();
    var stand = center + outward * 1.2f;
    // Del lado de afuera: si ahí hay una pared (adentro de la estación), probar el otro lado.
    if (UnityEngine.Physics.Raycast(center + outward * 0.4f + UnityEngine.Vector3.up * 0.2f, outward, 0.6f)) stand = center - outward * 1.2f;
    motor.TeleportTo(new UnityEngine.Vector3(stand.x, 0.1f, stand.z), null);
    var eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var d = center - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
    for (int i = 0; i < 30; i++) yield return wait;
    string noTool = ice.GetPrompt(who);
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    string handsFree = ice.GetPrompt(who);
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Crowbar));
    for (int i = 0; i < 30; i++) yield return wait;
    string inHand = ice.GetPrompt(who);
    int swings0 = belt.Swings, played0 = sounds != null ? sounds.Played : 0;
    float t = 0f, halfway = -1f;
    while (ice != null && !ice.IsBroken && t < 20f)
    {
        belt.TrySwing();
        yield return wait;
        t += UnityEngine.Time.fixedDeltaTime;
        if (halfway < 0f && ice != null && ice.Progress >= 0.5f) halfway = t;
    }
    bool broken = ice == null || ice.IsBroken;
    log.Append($"SIN BARRETA: \"{noTool}\". CON LA BARRETA AL CINTO: \"{handsFree}\". EN LA MANO: \"{inHand}\". GOLPES: {belt.Swings - swings0} en {t:0.0} s (mitad a los {halfway:0.0} s), roto={broken}; sonidos del mundo {(sounds != null ? sounds.Played - played0 : -1)} (último \"{(sounds != null ? sounds.LastSound : "")}\").");
    belt.Select(-1);
    UnityEngine.Debug.Log($"[HieloGolpes] {log}");
}
motor.StartCoroutine(Run());
return "prueba hielo a golpes iniciada";
