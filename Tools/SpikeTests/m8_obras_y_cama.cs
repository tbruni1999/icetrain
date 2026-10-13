// Prueba del M8 paso 3: obras por fases (BuildSite) y la cama mejorable: aportar materiales de la mochila, piezas sólidas
// y fantasmas, aporte parcial que se conserva, cada fase mejora la recuperación de energía (se mide durmiendo 8 h en cada
// nivel), y que no se pierdan ni dupliquen materiales.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m8_obras_y_cama.cs" --timeout 60
// Resultado en la Console, líneas [Obras].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var bed = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.Bed>();
SurvivalEnTren.Building.BuildSite site = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (s.StableId == "obra_tren_cama_001") site = s;
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = inputSettings.editorInputBehaviorInPlayMode;
var oldBackground = inputSettings.backgroundBehavior;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
void KeyE(bool down) => UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, down ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E) : new UnityEngine.InputSystem.LowLevel.KeyboardState());

object Target() => tInteractor.GetField("_target", flags).GetValue(who);
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
string P(SurvivalEnTren.Interaction.IInteractable i) => i.GetPrompt(who) ?? "(sin indicación)";
int woodGiven = 0, clothGiven = 0;
void Give(SurvivalEnTren.Items.ItemDefinition item, int n) { int a = backpack.Add(item, n); if (item == wood) woodGiven += a; else clothGiven += a; }

// Cuántas piezas de cada fase están sólidas (colliders prendidos), fantasma (visibles sin collider) y ocultas.
string Visuals()
{
    var phases = (System.Array)typeof(SurvivalEnTren.Building.BuildSite).GetField("phasePieces", flags).GetValue(site);
    var sb = new System.Text.StringBuilder();
    int p = 0;
    foreach (var phase in phases)
    {
        var pieces = (UnityEngine.GameObject[])phase.GetType().GetField("pieces").GetValue(phase);
        int solid = 0, ghost = 0, hidden = 0;
        foreach (var piece in pieces)
        {
            if (!piece.activeSelf) { hidden++; continue; }
            var col = piece.GetComponentInChildren<UnityEngine.Collider>(true);
            if (col != null && col.enabled) solid++; else ghost++;
        }
        sb.Append($"fase {p + 1}: {solid} sólidas, {ghost} fantasmas, {hidden} ocultas; ");
        p++;
    }
    return sb.ToString().TrimEnd(' ', ';');
}

System.Collections.IEnumerator Sleep(string label)
{
    vitals.Revive(100f, 80f, 10f, 100f);
    float before = vitals.Energy;
    float rate = bed.RecoveryPerHour;
    bed.Interact(who);
    for (int i = 0; i < 400; i++) { yield return wait; if (vitals.Energy > before + 1f && !vitals.IsSleeping && i > 200) break; }
    for (int i = 0; i < 200; i++) yield return wait; // termina el fundido
    log.Append($" [{label}: recupera {rate:0.00}/h → durmió 8 h: energía {before:0} → {vitals.Energy:0} (+{vitals.Energy - before:0})]");
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;
    // Parado junto a la cama, mirándola.
    var bedCenter = bed.transform.position + UnityEngine.Vector3.up * 0.3f;
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.0f, 1.05f, 4.5f), trainBody);
    var eye = motor.transform.position + UnityEngine.Vector3.up * 1.6f;
    var d = bedCenter - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
    for (int i = 0; i < 40; i++) yield return wait;

    var project = site.Project;
    log.Append($"OBRA \"{project.DisplayName}\": {project.PhaseCount} fases; nivel {site.Level}, valor {site.Value:0.00}, fracción {site.Fraction:0.00}; {Visuals()}.");
    object t0 = Target();
    log.Append($" Apuntando a la cama el objetivo es {Name(t0)} (ve la obra y no solo la tela): indicación sin materiales \"{P(site)}\".");

    // Nivel 0: dormir en el lugar miserable.
    yield return Sleep("nivel 0");

    // 1) Con materiales: prompt de construir y aporte con la tecla E de verdad (1 s = 2 piezas).
    Give(wood, 4); Give(cloth, 5);
    for (int i = 0; i < 10; i++) yield return wait;
    string promptBuild = P(site);
    int woodPack0 = backpack.CountOf(wood);
    KeyE(true);
    for (int i = 0; i < 65; i++) yield return wait; // ~1,3 s
    KeyE(false);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" APORTAR con E mantenido ~1,3 s: indicación \"{promptBuild}\"; salieron de la mochila {woodPack0 - backpack.CountOf(wood)} tablones; fracción {site.Fraction:0.00}, valor {site.Value:0.00} (6 + 1,5 × {site.Fraction:0.00}); {Visuals()}.");
    log.Append($" Indicación a mitad: \"{P(site)}\".");
    yield return Sleep("a mitad de la fase 1");

    // 2) El avance se conserva: soltar E y volver más tarde; completar la fase 1.
    int levelBefore = site.Level;
    KeyE(true);
    int held = 0;
    while (site.Level == levelBefore && held < 300) { yield return wait; held++; }
    KeyE(false);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" COMPLETAR la fase 1: nivel {levelBefore} → {site.Level} (se cortó el aporte al completar), valor {site.Value:0.00}, tablones en la mochila {backpack.CountOf(wood)}, en la obra 4; {Visuals()}; indicación \"{P(site)}\".");
    yield return Sleep("fase 1 completa");

    // 3) Fase 2 (relleno) con 3 rollos de tela; a mitad.
    KeyE(true);
    for (int i = 0; i < 40; i++) yield return wait; // ~0,8 s: 1 pieza
    KeyE(false);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" FASE 2 con 1 rollo de 3: fracción {site.Fraction:0.00}, valor {site.Value:0.00} (7,5 + 1,5 × {site.Fraction:0.00}); {Visuals()}.");
    yield return Sleep("a mitad de la fase 2");
    levelBefore = site.Level;
    KeyE(true); held = 0;
    while (site.Level == levelBefore && held < 300) { yield return wait; held++; }
    KeyE(false);
    for (int i = 0; i < 10; i++) yield return wait;
    yield return Sleep("fase 2 completa");

    // 4) Fase 3 (manta): termina la obra.
    levelBefore = site.Level;
    KeyE(true); held = 0;
    while (site.Level == levelBefore && held < 300) { yield return wait; held++; }
    KeyE(false);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" OBRA TERMINADA: nivel {site.Level} de {project.PhaseCount}, completa={site.IsComplete}, {Visuals()}; indicación \"{P(site)}\" (solo dormir).");
    yield return Sleep("cama terminada");

    // 5) Cuentas: lo que se dio a la mochila = lo que queda en la mochila + lo que está en la obra.
    log.Append($" CUENTAS: tablones dados {woodGiven}, en la mochila {backpack.CountOf(wood)}, en la obra 4; rollos dados {clothGiven}, en la mochila {backpack.CountOf(cloth)}, en la obra 5; sin duplicar ni perder={woodGiven == backpack.CountOf(wood) + 4 && clothGiven == backpack.CountOf(cloth) + 5}.");

    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Obras] " + log);
}
motor.StartCoroutine(Run());
return "prueba obras iniciada";
