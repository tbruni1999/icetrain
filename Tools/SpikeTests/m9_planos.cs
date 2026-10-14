// Prueba del M9 paso 1: planos y conocimiento. Los planos iniciales se saben; el del estante reforzado hay que encontrarlo
// en la segunda sala de la estación; sin él la pared del taller está vacía; al leerlo se aprende una sola vez, la hoja
// desaparece y aparece el marco fantasma de la obra.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m9_planos.cs" --timeout 60
// Resultado en la Console, líneas [Planos].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var book = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
SurvivalEnTren.Building.BuildSite shelfSite = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (s.StableId == "obra_tren_estante_001") shelfSite = s;
var sheet = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintPickup>();
var saw = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_SierraManual.asset");
var jerrycan = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_Bidon.asset");
var shelf = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_EstanteReforzado.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

object Target() => tInteractor.GetField("_target", flags).GetValue(who);
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
string P(SurvivalEnTren.Interaction.IInteractable i) => i?.GetPrompt(who) ?? "(sin indicación)";
// Cuántas piezas de la obra están activas (visibles) y si la zona de uso está prendida.
string ShelfVisuals()
{
    var phases = (System.Array)typeof(SurvivalEnTren.Building.BuildSite).GetField("phasePieces", flags).GetValue(shelfSite);
    int visible = 0, total = 0;
    foreach (var phase in phases)
        foreach (var piece in (UnityEngine.GameObject[])phase.GetType().GetField("pieces").GetValue(phase)) { total++; if (piece.activeSelf) visible++; }
    var zone = shelfSite.GetComponentInChildren<UnityEngine.BoxCollider>(true);
    bool zoneOn = false;
    foreach (var c in shelfSite.GetComponentsInChildren<UnityEngine.Collider>(true)) if (c.isTrigger && c.enabled) zoneOn = true;
    return $"{visible} de {total} piezas visibles, zona de uso prendida={zoneOn}";
}
void LookAt(UnityEngine.Vector3 world)
{
    var eye = motor.transform.position + UnityEngine.Vector3.up * 1.6f;
    var d = world - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;

    // 1) Qué se sabe al empezar.
    log.Append($"PLANOS AL EMPEZAR: sierra manual (inicial) {book.Knows(saw)}, bidón (inicial) {book.Knows(jerrycan)}, estante reforzado (hay que encontrarlo) {book.Knows(shelf)}.");

    // 2) La obra del estante sin el plano: pared vacía (nada se ve, nada se apunta, no hay indicación).
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.0f, 1.05f, 2.4f), trainBody);
    LookAt(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-1.4f, 1.0f, 2.4f));
    backpack.Add(metal, 2);
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($" SIN EL PLANO: la obra está bloqueada={shelfSite.Locked}; {ShelfVisuals()}; apuntando a la pared el objetivo es {Name(Target())}; indicación \"{P(shelfSite)}\" (aunque tenga chapas en la mochila).");

    // 3) El plano está sobre el escritorio de la segunda sala de la estación: apuntarlo desde adentro y leerlo.
    var sheetPos = sheet.transform.position;
    motor.TeleportTo(new UnityEngine.Vector3(sheetPos.x + 1.2f, 0.1f, sheetPos.z), null);
    LookAt(sheetPos);
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($" LA HOJA en la segunda sala: apuntándola el objetivo es {Name(Target())}, indicación \"{P(sheet)}\".");
    var sheetObject = sheet.gameObject;
    sheet.Interact(who);
    for (int i = 0; i < 5; i++) yield return wait;
    log.Append($" Después de E: el plano se aprendió={book.Knows(shelf)}, la hoja ya no está={sheetObject == null}, planos aprendidos en el libro {string.Join(", ", book.LearnedIds)}.");
    bool second = book.Learn(shelf);
    log.Append($" Aprenderlo otra vez devuelve {second} (no se duplica).");

    // 4) Con el plano la obra aparece: marco fantasma y se puede construir.
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.0f, 1.05f, 2.4f), trainBody);
    LookAt(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-1.4f, 1.0f, 2.4f));
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($" CON EL PLANO: bloqueada={shelfSite.Locked}; {ShelfVisuals()}; apuntando a la pared el objetivo es {Name(Target())}, indicación \"{P(shelfSite)}\".");

    // 5) Pallets de afuera: ahora hay dos.
    int outsidePallets = 0;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (p.StableId.StartsWith("salv_prueba_pallet")) outsidePallets++;
    log.Append($" PALLETS DE AFUERA: {outsidePallets} (uno más para la madera de la sierra y del estante).");

    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Planos] " + log);
}
motor.StartCoroutine(Run());
return "prueba planos iniciada";
