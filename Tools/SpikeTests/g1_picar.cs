// Prueba de G1 pasos 2, 3 y 5 (2026-10-17): picar el mundo "a lo 7 Days".
// Mira: el hacha y el pico improvisados se fabrican en el banco con piedras juntadas del piso; picar una roca chica con el pico
// (cada golpe da piedra directo a la mochila y avisa a la derecha; el total es el de la receta); la herramienta equivocada (el
// hacha en la roca rebota; la barreta pega poco); cortar un árbol muerto con el hacha; con la mochila llena lo que no entra cae
// al piso; y guardar a medias una roca con veta de hierro, cargar y terminarla (sin duplicar ni perder nada).
// La prueba corre en un objeto que sobrevive a la recarga de la escena (CoroutineHost).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/g1_picar.cs" --timeout 60
// Resultado en la Console, línea [Picar].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var hostObject = new UnityEngine.GameObject("Prueba_Picar");
UnityEngine.Object.DontDestroyOnLoad(hostObject);
var host = hostObject.AddComponent<SurvivalEnTren.Core.CoroutineHost>();
var items = "Assets/_Project/Data/Items/";
SurvivalEnTren.Items.ItemDefinition Item(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + n + ".asset");
var stone = Item("Piedra"); var ore = Item("MineralHierro"); var wood = Item("Madera"); var cloth = Item("Tela"); var parts = Item("Componentes");
SurvivalEnTren.Crafting.Blueprint Plan(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_" + n + ".asset");
var axePlan = Plan("HachaImprovisada"); var pickPlan = Plan("PicoImprovisado");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;

SurvivalEnTren.Player.PlayerMotor motor = null; SurvivalEnTren.Player.PlayerLook look = null;
SurvivalEnTren.Interaction.PlayerInteractor who = null; SurvivalEnTren.Equipment.PlayerGear gear = null;
SurvivalEnTren.Items.Backpack backpack = null; SurvivalEnTren.Equipment.ToolBelt belt = null;
SurvivalEnTren.UI.PickupFeed feed = null; SurvivalEnTren.Crafting.Workbench bench = null; SurvivalEnTren.Save.SaveSystem saver = null;
int useful = 0, rebounds = 0;
void Refs()
{
    motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
    look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
    who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
    gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
    backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
    belt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
    feed = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.UI.PickupFeed>();
    bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
    saver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Save.SaveSystem>();
    saver.FileName = "prueba_picar.json"; // no pisar la partida del jugador
    belt.Struck += (h, ok) => { if (ok) useful++; else rebounds++; };
    UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>().SpeedMultiplier = 0f;
}
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
SurvivalEnTren.Items.SalvageProp Find(string recipeFile)
{
    SurvivalEnTren.Items.SalvageProp best = null;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>())
        if (p.Recipe != null && p.Recipe.name == "Desmontaje_" + recipeFile && !p.IsDone && (best == null || string.CompareOrdinal(p.StableId, best.StableId) < 0)) best = p;
    return best;
}
UnityEngine.Vector3 CenterOf(UnityEngine.Component c)
{
    var rs = c.GetComponentsInChildren<UnityEngine.Collider>();
    var b = rs[0].bounds;
    foreach (var r in rs) b.Encapsulate(r.bounds);
    return b.center;
}
// Se para a "distance" metros del centro (en la dirección "from") y lo mira.
void StandAndLook(UnityEngine.Component c, float distance, float height = 0f)
{
    var center = CenterOf(c);
    var dir = new UnityEngine.Vector3(1f, 0f, 0.3f).normalized;
    var stand = center + dir * distance;
    motor.TeleportTo(new UnityEngine.Vector3(stand.x, center.y + 1f, stand.z), null);
    var eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var target = new UnityEngine.Vector3(center.x, center.y + height, center.z);
    var d = target - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
// Golpea "swings" veces (o hasta que se rompa, con swings = 0) con la herramienta que ya está en la mano.
System.Collections.IEnumerator Strike(SurvivalEnTren.Items.SalvageProp prop, int swings)
{
    int start = belt.Swings;
    float t = 0f;
    while (prop != null && !prop.IsDone && t < 60f && (swings == 0 || belt.Swings - start < swings))
    {
        belt.TrySwing();
        yield return wait;
        t += UnityEngine.Time.fixedDeltaTime;
    }
    yield return Frames(40);
}
string Prompt(SurvivalEnTren.Items.SalvageProp p) => p == null ? "(roto)" : (p.GetPrompt(who) ?? "(sin indicación)");

System.Collections.IEnumerator Run()
{
    Refs();
    yield return Frames(30);
    foreach (var g in new System.Collections.Generic.List<SurvivalEnTren.Items.GridItem>(backpack.Grid.Items)) backpack.Grid.Remove(g);
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);

    // 1) Las piedras sueltas del piso y las herramientas improvisadas en el banco.
    int looseBundles = 0, looseStones = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        if (b.Item == stone && b.transform.parent != null && b.transform.parent.name == "Piedras_Sueltas") { looseBundles++; looseStones += b.Count; }
    int picked = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
    {
        if (picked >= 5 || b.Item != stone || b.transform.parent == null || b.transform.parent.name != "Piedras_Sueltas") continue;
        picked += b.Count;
        b.Interact(who);
        yield return Frames(2);
    }
    backpack.Add(wood, 2); backpack.Add(cloth, 2);
    bool offersAxe = bench.Recipes(UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>()).Exists(r => r.Blueprint == axePlan);
    bool offersPick = bench.Recipes(UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>()).Exists(r => r.Blueprint == pickPlan);
    int stonesBefore = backpack.CountOf(stone);
    var axeBlock = bench.Check(axePlan, backpack, gear);
    bool axeStarted = bench.StartJob(new SurvivalEnTren.Crafting.BenchRecipe(axePlan), backpack, gear); bench.FinishJobNow();
    yield return Frames(10);
    var pickBlock = bench.Check(pickPlan, backpack, gear);
    bool pickStarted = bench.StartJob(new SurvivalEnTren.Crafting.BenchRecipe(pickPlan), backpack, gear); bench.FinishJobNow();
    yield return Frames(10);
    log.Append($"PIEDRAS SUELTAS: {looseBundles} bultos con {looseStones} piedras junto a la vía; juntó {backpack.CountOf(stone) + 0} (de a bulto, con E). BANCO: ofrece hacha={offersAxe}, pico={offersPick}; hacha: bloqueo {axeBlock}, empezó={axeStarted}; pico: bloqueo {pickBlock}, empezó={pickStarted}; tiene hacha={gear.Has(SurvivalEnTren.Equipment.GearId.Axe)}, pico={gear.Has(SurvivalEnTren.Equipment.GearId.Pickaxe)}; piedras {stonesBefore} → {backpack.CountOf(stone)} (esperado -5).");

    // 2) Una roca chica con el pico: E saca el pico; cada golpe da piedra a la mochila; al romperse, el total de la receta.
    var smallRock = Find("RocaChica");
    StandAndLook(smallRock, 1.5f, 0.1f);
    if (belt.ActiveTool.HasValue) belt.Select(-1);
    yield return Frames(30);
    string before = Prompt(smallRock);
    smallRock.Interact(who);
    yield return Frames(30);
    string withPick = Prompt(smallRock);
    int s0 = backpack.CountOf(stone), u0 = useful;
    yield return Strike(smallRock, 1);
    int afterOne = backpack.CountOf(stone) - s0;
    string feedText = feed != null ? feed.LastText : "(sin aviso)";
    yield return Strike(smallRock, 0);
    log.Append($" ROCA CHICA: \"{before}\" → E: \"{withPick}\" (en la mano {belt.ActiveTool}); el primer golpe dio {afterOne} piedra(s) y avisó \"{feedText}\"; golpes que sirvieron {useful - u0} (receta {UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.SalvageRecipe>(items + "Desmontaje_RocaChica.asset").HitsToBreak}); rota={smallRock == null}; piedras +{backpack.CountOf(stone) - s0} (esperado 6).");

    // 3) La herramienta equivocada: el hacha en una roca grande rebota; la barreta pega poco.
    var bigRock = Find("RocaGrande");
    StandAndLook(bigRock, 1.9f, 0.1f);
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Axe));
    yield return Frames(30);
    string axePrompt = Prompt(bigRock);
    int r0 = rebounds; float p0 = bigRock.Progress;
    yield return Strike(bigRock, 3);
    int axeRebounds = rebounds - r0; float axeProgress = bigRock.Progress - p0;
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Crowbar));
    yield return Frames(30);
    string barPrompt = Prompt(bigRock);
    p0 = bigRock.Progress; u0 = useful;
    yield return Strike(bigRock, 4);
    float barProgress = bigRock.Progress - p0;
    log.Append($" HERRAMIENTA EQUIVOCADA: con el hacha \"{axePrompt}\": 3 golpes, rebotaron {axeRebounds}, avance +{axeProgress * 100f:0}%; con la barreta \"{barPrompt}\": 4 golpes sirvieron {useful - u0}, avance +{barProgress * 100f:0.0}% (con el pico serían {4f / UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.SalvageRecipe>(items + "Desmontaje_RocaGrande.asset").HitsToBreak * 100f:0.0}%).");

    // 4) Un árbol muerto con el hacha.
    var tree = Find("ArbolMuerto");
    StandAndLook(tree, 1.3f, 0.4f);
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Axe));
    yield return Frames(30);
    string treePrompt = Prompt(tree);
    int w0 = backpack.CountOf(wood); u0 = useful;
    yield return Strike(tree, 0);
    log.Append($" ÁRBOL MUERTO: \"{treePrompt}\"; golpes que sirvieron {useful - u0}; cortado={tree == null}; madera +{backpack.CountOf(wood) - w0} (esperado 10).");

    // 5) Con la mochila llena: lo que no entra cae al piso al lado del golpe.
    int fill = 0; while (backpack.MaxAddable(parts) > 0 && fill++ < 2000) backpack.Add(parts, 30);
    var rock2 = Find("RocaChica");
    StandAndLook(rock2, 1.5f, 0.1f);
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Pickaxe));
    yield return Frames(30);
    var known = new System.Collections.Generic.HashSet<SurvivalEnTren.Items.MaterialBundle>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>());
    int packStones = backpack.CountOf(stone);
    yield return Strike(rock2, 0);
    int dropped = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (!known.Contains(b) && b.Item == stone) dropped += b.Count;
    string droppedText = feed != null ? feed.LastText : "";
    log.Append($" MOCHILA LLENA: {backpack.UsedCells}/{backpack.CellCount} casillas; piedras en la mochila +{backpack.CountOf(stone) - packStones}, al piso {dropped} (esperado 6 entre las dos); aviso \"{droppedText}\".");
    backpack.Remove(parts, 100000);

    // 6) Guardar a medias una roca con veta de hierro, cargar y terminarla: el total tiene que ser justo el de la receta.
    var iron = Find("RocaHierro");
    string ironId = iron.StableId;
    StandAndLook(iron, 1.9f, 0.1f);
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Pickaxe));
    yield return Frames(30);
    int st0 = backpack.CountOf(stone), or0 = backpack.CountOf(ore);
    yield return Strike(iron, 9);
    float savedProgress = iron.Progress;
    int midStone = backpack.CountOf(stone) - st0, midOre = backpack.CountOf(ore) - or0;
    bool saved = saver.Save();
    int loads = SurvivalEnTren.Save.SaveSystem.LoadsApplied;
    bool loading = saver.Load();
    int guard = 0;
    while (SurvivalEnTren.Save.SaveSystem.LoadsApplied <= loads && guard++ < 600) yield return null;
    yield return Frames(30);
    useful = 0; rebounds = 0;
    Refs();
    SurvivalEnTren.Items.SalvageProp ironAgain = null;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (p.StableId == ironId) ironAgain = p;
    float loadedProgress = ironAgain != null ? ironAgain.Progress : -1f;
    int afterLoadStone = backpack.CountOf(stone) - st0, afterLoadOre = backpack.CountOf(ore) - or0;
    StandAndLook(ironAgain, 1.9f, 0.1f);
    if (!gear.Has(SurvivalEnTren.Equipment.GearId.Pickaxe)) log.Append(" (después de cargar no tiene el pico)");
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Pickaxe));
    yield return Frames(30);
    yield return Strike(ironAgain, 0);
    log.Append($" GUARDAR A MEDIAS: 9 golpes al hierro (avance {savedProgress * 100f:0}%, dio piedra {midStone} y mineral {midOre}); guardó={saved}, cargó={loading}; la roca volvió con {loadedProgress * 100f:0}% y la mochila con piedra {afterLoadStone}, mineral {afterLoadOre}; terminada={ironAgain == null}: total piedra {backpack.CountOf(stone) - st0} (esperado 10), mineral {backpack.CountOf(ore) - or0} (esperado 8).");

    UnityEngine.Debug.Log("[Picar] " + log);
    UnityEngine.Object.Destroy(hostObject);
}
host.StartCoroutine(Run());
return "prueba de picar iniciada";
