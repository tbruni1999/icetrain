// Prueba del M8 paso 5: el ciclo completo. Salir a la estación y al pallet de afuera, desmontar todo, juntar los bultos
// (la mochila tiene lugar para unas 30 casillas por viaje: hay que hacer varios viajes), volver al tren, armar en el banco los
// kits de la cama y el estante reforzado con lo traído (desde el 2026-10-16) e instalarlos, guardar el resto en los estantes, dormir en la cama terminada y medir la energía.
// Los viajes se simulan con teletransportes; lo que se prueba es la lógica del ciclo y que no se pierda ni duplique nada.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m8_ciclo_completo.cs" --timeout 60
// Resultado en la Console, líneas [Ciclo].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var bed = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.Bed>();
SurvivalEnTren.Building.BuildSite bedSite = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (s.StableId == "obra_tren_cama_001") bedSite = s;
var bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var placer = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Building.PlacementController>();
var shelfPlan = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_EstanteReforzado.asset");
bool shelfPlaced = false;
SurvivalEnTren.Items.StorageContainer starter = null, reinforced = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.StorageContainer>())
{
    if (s.StableId == "storage_tren_estante_precario_001") starter = s;
    else if (s.StableId == "storage_tren_estante_reforzado_001") reinforced = s;
}
// El estante reforzado pide su plano: se aprende antes (el jugador lo lee de la hoja de la estación).
UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>().Learn(shelfPlan);
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Componentes.asset");
var items = new[] { wood, metal, cloth, parts };
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

string P(SurvivalEnTren.Interaction.IInteractable i) => i.GetPrompt(who) ?? "";
string Show(int[] u) => $"tablones {u[0]}, chapas {u[1]}, rollos {u[2]}, componentes {u[3]}";
UnityEngine.Vector3 CenterOf(UnityEngine.Component c)
{
    var rs = c.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs[0].bounds;
    foreach (var r in rs) b.Encapsulate(r.bounds);
    return b.center;
}
void StandNear(UnityEngine.Vector3 target)
{
    var stand = target + new UnityEngine.Vector3(1.2f, 0f, 0f);
    motor.TeleportTo(new UnityEngine.Vector3(stand.x, 0.1f, stand.z), null);
    var d = target - (motor.transform.position + UnityEngine.Vector3.up * 1.6f);
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
bool IsOutside(UnityEngine.Component c) => UnityEngine.Vector3.Distance(c.transform.position, trainBody.position) > 25f;
// El ciclo del M8 usa solo el mundo de ese milestone: la estación y los pallets de afuera. Lo que se agregó después (los
// objetos de la bifurcación del M10, los lugares del M11, la aguja de cambio) queda fuera de esta prueba.
var stationCenter = new UnityEngine.Vector3(40f, 0f, -200f);
bool M8Prop(SurvivalEnTren.Items.SalvageProp p) => p.StableId.StartsWith("salv_estacion_") || p.StableId.StartsWith("salv_prueba_");
bool M8Bundle(SurvivalEnTren.Items.MaterialBundle b) => System.Array.IndexOf(items, b.Item) >= 0 &&
    (UnityEngine.Vector3.Distance(b.transform.position, stationCenter) < 40f || UnityEngine.Vector3.Distance(b.transform.position, trainBody.position) < 60f);
// Cuántas piezas de cada material hay en el mundo: mochila + estantes + bultos + lo que todavía hay que desmontar (por receta)
// + lo ya aportado a las obras (no se puede mirar adentro: se cuenta aparte).
int[] InWorld()
{
    var u = new int[4];
    for (int k = 0; k < 4; k++)
    {
        u[k] = backpack.CountOf(items[k]) + starter.Grid.CountOf(items[k]);
        if (reinforced != null && reinforced.Grid != null) u[k] += reinforced.Grid.CountOf(items[k]);
    }
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (M8Bundle(b))
        for (int k = 0; k < 4; k++) if (b.Item == items[k]) u[k] += b.Count;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (M8Prop(p))
        foreach (var y in p.Recipe.Yields) for (int k = 0; k < 4; k++) if (items[k] == y.item) u[k] += y.count;
    return u;
}

// H2 (herramientas y forja): desarmar es a golpes de barreta, uno cada ~0,75 s como el jugador, y lo que sale cae al piso
// como bultos: se juntan con E (solo los que cayeron de este objeto).
System.Collections.IEnumerator HitDown(SurvivalEnTren.Items.SalvageProp target)
{
    var hitBelt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
    var where = target.transform.position;
    var already = new System.Collections.Generic.HashSet<SurvivalEnTren.Items.MaterialBundle>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>());
    int hits = 0;
    while (target != null && !target.IsDone && hits++ < 60)
    {
        target.OnToolHit(new SurvivalEnTren.Equipment.ToolHit(target.Recipe.RequiredTool, where + UnityEngine.Vector3.up * 0.5f, UnityEngine.Vector3.up, null, hitBelt));
        for (int i = 0; i < 38; i++) yield return new UnityEngine.WaitForFixedUpdate();
    }
    for (int i = 0; i < 40; i++) yield return new UnityEngine.WaitForFixedUpdate(); // que caigan los bultos
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        if (!already.Contains(b) && (b.transform.position - where).sqrMagnitude < 9f) b.Interact(who);
    for (int i = 0; i < 5; i++) yield return new UnityEngine.WaitForFixedUpdate();
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    var total0 = InWorld();
    log.Append($"EN EL MUNDO al empezar (bultos + objetos por desmontar): {Show(total0)}. Cama: nivel {bedSite.Level}/{bedSite.Project.PhaseCount}; estante reforzado: se arma en el banco (pide chapa ×2 y madera ×6).");

    int trips = 0;
    while (trips < 8)
    {
        // ---- Ida: desmontar y juntar hasta llenar la mochila.
        var targets = new System.Collections.Generic.List<UnityEngine.Component>();
        foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (M8Prop(p)) targets.Add(p);
        foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (IsOutside(b) && M8Bundle(b)) targets.Add(b);
        if (targets.Count == 0) break;
        trips++;
        int taken = 0;
        foreach (var target in targets)
        {
            if (target == null) continue;
            if (backpack.UsedCells >= 26) break; // mochila casi llena: se vuelve al tren
            StandNear(CenterOf(target));
            for (int i = 0; i < 30; i++) yield return wait;
            if (target is SurvivalEnTren.Items.SalvageProp prop)
            {
                yield return HitDown(prop);
                for (int i = 0; i < 10; i++) yield return wait;
            }
            else if (target is SurvivalEnTren.Items.MaterialBundle bundle)
            {
                while (bundle != null && bundle.Count > 0 && backpack.MaxAddable(bundle.Item) > 0) { int c = bundle.Count; bundle.Interact(who); yield return wait; if (bundle != null && bundle.Count == c) break; }
            }
            taken++;
        }
        var carried = new[] { backpack.CountOf(wood), backpack.CountOf(metal), backpack.CountOf(cloth), backpack.CountOf(parts) };

        // ---- Vuelta: al tren. El banco usa lo de la mochila y lo de los estantes: arma los kits de la cama (en orden) y el
        //      estante reforzado cuando alcanza; cada kit se toma y se instala (el estante, contra la pared izquierda).
        motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.0f, 1.05f, 4.0f), trainBody);
        for (int i = 0; i < 30; i++) yield return wait;
        int stored = 0;
        foreach (var piece in new System.Collections.Generic.List<SurvivalEnTren.Items.GridItem>(backpack.Grid.Items))
        {
            foreach (var shelf in new[] { reinforced != null ? reinforced.Grid : null, starter.Grid })
            {
                if (shelf == null) continue;
                if (shelf.FindFreeSpot(piece.Item, out int x, out int y, out bool rot))
                {
                    backpack.Grid.Remove(piece);
                    shelf.TryPlace(piece.Item, x, y, rot);
                    stored++;
                    break;
                }
            }
        }
        // Lo que no entra en los estantes se deja en el piso del tren (el banco también lo usa): la mochila vuelve vacía.
        int dropped = 0;
        var bundleSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.BackpackSettings>("Assets/_Project/Data/Items/BackpackSettings.asset");
        foreach (var piece in new System.Collections.Generic.List<SurvivalEnTren.Items.GridItem>(backpack.Grid.Items))
        {
            backpack.Grid.Remove(piece);
            SurvivalEnTren.Items.MaterialBundle.Spawn(bundleSettings, piece.Item, 1, trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.9f, 1.4f, 0.6f + 0.25f * (dropped % 4)), trainBody.linearVelocity);
            dropped++;
        }
        // El banco arma con lo de la mochila, los estantes y el piso del tren.
        for (int round = 0; round < 6; round++)
        {
            SurvivalEnTren.Crafting.BenchRecipe recipe = null;
            if (!bedSite.IsComplete && bench.Check(new SurvivalEnTren.Crafting.BenchRecipe(bedSite, bedSite.Level), backpack, gear) == SurvivalEnTren.Crafting.CraftBlock.None) recipe = new SurvivalEnTren.Crafting.BenchRecipe(bedSite, bedSite.Level);
            else if (!shelfPlaced && bench.Check(new SurvivalEnTren.Crafting.BenchRecipe(shelfPlan), backpack, gear) == SurvivalEnTren.Crafting.CraftBlock.None) recipe = new SurvivalEnTren.Crafting.BenchRecipe(shelfPlan);
            if (recipe == null) break;
            bench.StartJob(recipe, backpack, gear);
            bench.FinishJobNow();
            for (int i = 0; i < 5; i++) yield return wait;
            SurvivalEnTren.Building.UpgradeKit kit = null;
            foreach (var k in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>()) kit = k;
            carrier.Pick(kit.GetComponent<SurvivalEnTren.Interaction.Carryable>());
            for (int i = 0; i < 3; i++) yield return wait;
            if (recipe.IsSitePhase) { bedSite.Interact(who); for (int i = 0; i < 3; i++) yield return wait; continue; }
            // El estante: parado en el pasillo, mirando la pared izquierda; el kit viaja con el jugador; clic.
            motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.2f, 1.05f, 2.7f), trainBody);
            var hp = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.2f, 2.3f, 2.7f); kit.transform.position = hp; carrier.Holding.Body.position = hp;
            var d = (trainBody.position + trainBody.rotation * new UnityEngine.Vector3(-1.7f, 1.9f, 2.7f)) - (motor.transform.position + UnityEngine.Vector3.up * 1.6f);
            motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
            typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
            for (int i = 0; i < 20; i++) yield return wait;
            shelfPlaced = placer.Confirm();
            for (int i = 0; i < 5; i++) yield return wait;
            var placed = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Building.Placeable>();
            if (placed != null) reinforced = placed.GetComponent<SurvivalEnTren.Items.StorageContainer>();
        }
        log.Append($" [VIAJE {trips}: se juntó {Show(carried)}; en el tren: cama nivel {bedSite.Level}, estante reforzado colocado={shelfPlaced}, guardó {stored} piezas en los estantes y dejó {dropped} en el piso del tren]");
    }

    // ---- Resultado.
    int outsideBundles = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (IsOutside(b) && M8Bundle(b)) outsideBundles++;
    int propsLeft = 0; foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) if (M8Prop(p)) propsLeft++;
    log.Append($" TERMINÓ en {trips} viajes: cama {(bedSite.IsComplete ? "terminada" : "nivel " + bedSite.Level)}, estante reforzado {(shelfPlaced ? "colocado" : "no")} ({(reinforced == null || reinforced.Grid == null ? "sin cuadrícula" : reinforced.Grid.Width + "x" + reinforced.Grid.Height)}), objetos por desmontar {propsLeft}, bultos afuera {outsideBundles}.");
    var starterSummary = $"estante precario {starter.Grid.UsedCells}/{starter.Grid.CellCount} casillas ({starter.Grid.Items.Count} piezas)";
    var reinforcedSummary = reinforced == null || reinforced.Grid == null ? "" : $"; estante reforzado {reinforced.Grid.UsedCells}/{reinforced.Grid.CellCount} ({reinforced.Grid.Items.Count} piezas)";
    log.Append($" GUARDADO: {starterSummary}{reinforcedSummary}; mochila {backpack.Grid.Items.Count} piezas.");

    // ---- Dormir en la cama terminada.
    vitals.Revive(100f, 80f, 10f, 100f);
    float before = vitals.Energy, rate = bed.RecoveryPerHour;
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.0f, 1.05f, 4.0f), trainBody);
    for (int i = 0; i < 30; i++) yield return wait;
    bed.Interact(who);
    for (int i = 0; i < 400; i++) yield return wait;
    log.Append($" DORMIR en la cama {(bedSite.IsComplete ? "terminada" : "a medias")}: recupera {rate:0.0}/h → energía {before:0} → {vitals.Energy:0} (+{vitals.Energy - before:0}); con el lugar miserable del principio eran +48.");

    // ---- Cuentas: lo que había al empezar (6 tablones + 5 chapas + 5 rollos + 2 componentes) ahora está en las obras o guardado.
    var totalNow = InWorld();
    int woodUsed = bedSite.ContributedOf(wood) + (shelfPlaced ? 6 : 0);
    int metalUsed = bedSite.ContributedOf(metal) + (shelfPlaced ? 2 : 0);
    int clothUsed = bedSite.ContributedOf(cloth);
    log.Append($" CUENTAS: había {Show(total0)}; ahora en el mundo (mochila + estantes + bultos) {Show(totalNow)}; usado en la cama y el estante: tablones {woodUsed}, chapas {metalUsed}, rollos {clothUsed}; suma {Show(new[] { totalNow[0] + woodUsed, totalNow[1] + metalUsed, totalNow[2] + clothUsed, totalNow[3] })}; sin duplicar ni perder={totalNow[0] + woodUsed == total0[0] && totalNow[1] + metalUsed == total0[1] && totalNow[2] + clothUsed == total0[2] && totalNow[3] == total0[3]}.");

    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Ciclo] " + log);
}
motor.StartCoroutine(Run());
return "prueba ciclo iniciada";
