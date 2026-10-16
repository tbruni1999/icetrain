// Prueba del M10 paso 2 (desde el 2026-10-16 con kits): el proyecto de reparación de la bifurcación. El banco del tren arma un
// kit por fase (palanca, durmientes y riel, aguja de cambio); congelado no se puede instalar; con el hielo roto se lleva el kit
// y se instala (lo que falta no se ve); la pieza especial se recoge del taller ferroviario (M11); al terminar el cambio queda
// REPARADO y la palanca (apuntada de verdad) lo mueve.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m10_reparacion.cs" --timeout 60
// Resultado en la Console, líneas [Reparacion].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
SurvivalEnTren.Rail.RailSwitch sw = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Rail.RailSwitch>()) if (s.StableId == "rail_switch_oeste_001") sw = s;
SurvivalEnTren.Poi.FrozenDoor ice = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Poi.FrozenDoor>()) if (d.StableId == "ice_switch_oeste_001") ice = d;
SurvivalEnTren.Building.BuildSite site = null;
foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) if (b.StableId == "obra_cambio_oeste_001") site = b;
SurvivalEnTren.Rail.SwitchLever lever = null;
foreach (var l in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Rail.SwitchLever>()) if (l.Switch == sw) lever = l;
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Componentes.asset");
var needle = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/AgujaCambio.asset");
var bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
var book = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

object Target() => tInteractor.GetField("_target", flags).GetValue(who);
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
string P(SurvivalEnTren.Interaction.IInteractable i) => i?.GetPrompt(who) ?? "(sin indicación)";
void LookAtWorld(UnityEngine.Vector3 point)
{
    var eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var d = point - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
// Piezas de la obra: "visibles" (activas) y "sólidas" (visibles y con el material de verdad, no el fantasma), por fase.
string Pieces()
{
    var phases = (System.Array)typeof(SurvivalEnTren.Building.BuildSite).GetField("phasePieces", flags).GetValue(site);
    var sb = new System.Text.StringBuilder();
    int n = 1;
    foreach (var phase in phases)
    {
        int visible = 0, solid = 0, total = 0;
        foreach (var piece in (UnityEngine.GameObject[])phase.GetType().GetField("pieces").GetValue(phase))
        {
            total++;
            if (!piece.activeSelf) continue;
            visible++;
            var r = piece.GetComponentInChildren<UnityEngine.Renderer>();
            if (r != null) solid++;
        }
        sb.Append($"fase {n++}: {visible}/{total} visibles, {solid} sólidas; ");
    }
    return sb.ToString().TrimEnd(' ', ';');
}
bool ZonesOn()
{
    bool on = false;
    foreach (var c in site.GetComponentsInChildren<UnityEngine.Collider>(true)) if (c.isTrigger && c.enabled) on = true;
    return on;
}
// El banco arma el kit de una fase (se adelanta su reloj: la prueba no espera los 30 min del juego).
bool CraftKit(int phase) { bool ok = bench.StartJob(new SurvivalEnTren.Crafting.BenchRecipe(site, phase), backpack, gear); bench.FinishJobNow(); return ok; }
SurvivalEnTren.Building.UpgradeKit NewestKit() { SurvivalEnTren.Building.UpgradeKit k = null; foreach (var x in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>()) k = x; return k; }
// Lo que se lleva en la mano viaja con el jugador (los teletransportes de la prueba lo dejarían atrás).
void CarryWithMe() { var h = carrier.Holding; if (h == null) return; var p = motor.transform.position + UnityEngine.Vector3.up * 1.4f + motor.transform.forward * 0.8f; h.transform.position = p; h.Body.position = p; h.Body.linearVelocity = UnityEngine.Vector3.zero; }
// Instalar: con el kit en la mano, lo mismo que mantener E sobre la obra.
System.Collections.IEnumerator Install() { site.Interact(who); for (int i = 0; i < 5; i++) yield return wait; }
// Lo que no entra en la mochila va al estante del taller (el banco usa los dos).
void Give(SurvivalEnTren.Items.ItemDefinition item, int n)
{
    int left = n - backpack.Add(item, n);
    SurvivalEnTren.Items.StorageContainer shelf = null;
    foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.StorageContainer>()) if (s.StableId == "storage_tren_estante_precario_001") shelf = s;
    for (int i = 0; i < left; i++) shelf.Grid.TryAutoPlace(item);
}
void StandAtLever(float side = 2.2f, float aimHeight = 0.7f)
{
    var stand = UnityEngine.GameObject.Find("Cambio_2_Palanca").transform;
    var target = stand.position + new UnityEngine.Vector3(0f, aimHeight, 0f);
    motor.TeleportTo(new UnityEngine.Vector3(target.x + side, 0.1f, target.z + 1.2f), null);
    LookAtWorld(target);
}

System.IO.File.WriteAllText("Temp/prueba_paso.txt", "");
void Step(string s) { System.IO.File.AppendAllText("Temp/prueba_paso.txt", s + "\n"); }

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;

    Step("0");
    // 0) Datos y lugares: la pieza especial y los objetos desmontables de los lugares del M11.
    SurvivalEnTren.Items.MaterialBundle needleBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == needle) needleBundle = b;
    int woodSup = 0, metalSup = 0, partsSup = 0, supProps = 0;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>())
    {
        if (!p.StableId.StartsWith("salv_taller_") && !p.StableId.StartsWith("salv_refugio_")) continue; // los lugares del M11
        supProps++;
        foreach (var y in p.Recipe.Yields) { if (y.item == wood) woodSup += y.count; if (y.item == metal) metalSup += y.count; if (y.item == parts) partsSup += y.count; }
    }
    var req = site.Project;
    string reqText = "";
    for (int i = 0; i < req.PhaseCount; i++)
    {
        var ph = req.Phase(i);
        var parts2 = new System.Collections.Generic.List<string>();
        foreach (var r in ph.requirements) parts2.Add($"{r.item.PieceName} ×{r.count}");
        reqText += $" fase {i + 1} \"{ph.name}\" pide {string.Join(" + ", parts2)};";
    }
    log.Append($"DATOS: pieza especial \"{needle.DisplayName}\" {needle.SizeCells.x}x{needle.SizeCells.y}, {needle.WeightKg:0.0} kg, rota={needle.CanRotate}, en el taller ferroviario={(needleBundle != null)} (a {(needleBundle != null ? UnityEngine.Vector3.Distance(needleBundle.transform.position, new UnityEngine.Vector3(480f, 0f, 150f)) : -1f):0} m de la masa metálica); proyecto \"{req.DisplayName}\":{reqText} objetos desmontables en el taller y el refugio: {supProps} (madera ×{woodSup}, chapas ×{metalSup}, componentes ×{partsSup}).");

    Step("1");
    // 1) Congelado: el banco ya arma el kit de la palanca, pero con el hielo encima no se puede instalar.
    Give(metal, 2); Give(parts, 1);
    bool made1 = CraftKit(0);
    yield return Frames(5);
    var kit1 = NewestKit();
    carrier.Pick(kit1.GetComponent<SurvivalEnTren.Interaction.Carryable>());
    StandAtLever();
    CarryWithMe();
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($" CONGELADO: el banco ofrece {string.Join(", ", bench.Recipes(book).FindAll(r => r.Site == site).ConvertAll(r => r.DisplayName))}; armó el kit={made1}; obra bloqueada={site.Locked}; {Pieces()}; zonas de uso prendidas={ZonesOn()}; con el kit, la obra lo acepta={site.Accepts(kit1)}, indicación de la obra \"{P(site)}\".");

    Step("2");
    // 2) Hielo roto: se puede instalar (lo que falta no se ve: sin fantasmas).
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    carrier.Drop(false);
    int g = 0;
    while (ice != null && !ice.IsBroken && g++ < 1000) { if (!ice.Channel(who, UnityEngine.Time.fixedDeltaTime)) break; yield return wait; }
    for (int i = 0; i < 30; i++) yield return wait;
    carrier.Pick(kit1.GetComponent<SurvivalEnTren.Interaction.Carryable>());
    CarryWithMe();
    LookAtWorld(UnityEngine.GameObject.Find("Cambio_2_Palanca").transform.position + new UnityEngine.Vector3(0f, 0.7f, 0f));
    for (int i = 0; i < 20; i++) yield return wait;
    object t1 = Target();
    log.Append($" SIN HIELO: estado {sw.Condition}; obra bloqueada={site.Locked}; {Pieces()}; zonas prendidas={ZonesOn()}; con el kit en la mano el objetivo es {Name(t1)}, indicación \"{P(t1 as SurvivalEnTren.Interaction.IInteractable)}\"; el muñón de la palanca rota se ve={UnityEngine.GameObject.Find("Palanca_Rota") != null && UnityEngine.GameObject.Find("Palanca_Rota").activeSelf}.");

    Step("3");
    // 3) Fase 1: instalar la palanca (mantener E con el kit: lo mismo que Interact).
    yield return Install();
    LookAtWorld(UnityEngine.GameObject.Find("Cambio_2_Palanca").transform.position + new UnityEngine.Vector3(0f, 0.7f, 0f));
    yield return Frames(10);
    log.Append($" FASE 1 instalada: nivel {site.Level}; {Pieces()}; el muñón se ve={UnityEngine.GameObject.Find("Palanca_Rota") != null && UnityEngine.GameObject.Find("Palanca_Rota").activeSelf}; estado del cambio {sw.Condition}; moverlo → {(sw.TrySetDiverging(true, null) ? "SE MOVIÓ (mal)" : "no se mueve (bien)")}.");

    Step("4");
    // 4) Fase 2: durmientes y riel (madera ×4 + chapa ×3). Con la mitad de la madera el banco no lo arma.
    Give(wood, 2); Give(metal, 3);
    var block2 = bench.Check(new SurvivalEnTren.Crafting.BenchRecipe(site, 1), backpack, gear);
    Give(wood, 2);
    bool made2 = CraftKit(1);
    yield return Frames(5);
    carrier.Pick(NewestKit().GetComponent<SurvivalEnTren.Interaction.Carryable>());
    StandAtLever();
    CarryWithMe();
    yield return Frames(20);
    yield return Install();
    log.Append($" FASE 2: con 2 tablones de 4 el banco dice {block2}; con los 4 armó el kit={made2} y se instaló: nivel {site.Level}; {Pieces()}; el cambio sigue {sw.Condition}.");

    Step("5");
    // 5) Fase 3: la pieza especial. Sin ella el banco no arma el kit; se recoge del taller ferroviario.
    var block3 = bench.Check(new SurvivalEnTren.Crafting.BenchRecipe(site, 2), backpack, gear);
    var bundlePos = needleBundle.transform.position;
    motor.TeleportTo(new UnityEngine.Vector3(bundlePos.x + 0.9f, 0.1f, bundlePos.z), null);
    LookAtWorld(bundlePos + new UnityEngine.Vector3(0f, 0.15f, 0f));
    yield return Frames(30);
    object t3 = Target();
    string pickPrompt = P(t3 as SurvivalEnTren.Interaction.IInteractable);
    needleBundle.Interact(who);
    yield return Frames(10);
    bool took = backpack.CountOf(needle) == 1;
    log.Append($" FASE 3 sin la pieza: el banco dice {block3}. PIEZA ESPECIAL: apuntándola el objetivo es {Name(t3)}, indicación \"{pickPrompt}\"; la guardó en la mochila={took} ({backpack.MassKg:0.0} kg).");
    bool made3 = CraftKit(2);
    yield return Frames(5);
    carrier.Pick(NewestKit().GetComponent<SurvivalEnTren.Interaction.Carryable>());
    StandAtLever();
    CarryWithMe();
    yield return Frames(20);
    yield return Install();
    yield return Frames(10);
    log.Append($" FASE 3 con la pieza: armó el kit={made3}; nivel {site.Level}/{site.Project.PhaseCount}, obra completa={site.IsComplete}; {Pieces()}; estado del cambio {sw.Condition}; la mochila quedó con {backpack.CountOf(needle)} agujas.");

    Step("6");
    // 6) Reparado: las zonas de la obra se apagan y la palanca se apunta de verdad (hay que estar al alcance: 2,2 m).
    StandAtLever(1.0f, 0.15f); // se apunta a la base del soporte (la varilla se inclina al mover la palanca)
    yield return Frames(20);
    object t4 = Target();
    log.Append($" REPARADO: zonas de la obra prendidas={ZonesOn()}; apuntando a la palanca el objetivo es {Name(t4)}, indicación \"{P(t4 as SurvivalEnTren.Interaction.IInteractable)}\", mantener {(t4 as SurvivalEnTren.Interaction.IInteractable)?.GetHoldSeconds(who):0.0} s.");
    lever.Interact(who);
    yield return Frames(5);
    log.Append($" Palanca: desvío={sw.IsDiverging}, el tren iría a {sw.ActiveExit.name}.");
    lever.Interact(who);
    log.Append($" Otra vez: desvío={sw.IsDiverging}.");

    Step("7");
    // 7) Cuentas: lo aportado es exactamente lo pedido.
    log.Append($" CUENTAS: aportado madera {site.ContributedOf(wood)}, chapas {site.ContributedOf(metal)}, componentes {site.ContributedOf(parts)}, agujas {site.ContributedOf(needle)} (pedido: 4, 5, 1, 1); en la mochila quedó madera {backpack.CountOf(wood)}, chapas {backpack.CountOf(metal)}, componentes {backpack.CountOf(parts)}.");
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Reparacion] " + log);
}
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
motor.StartCoroutine(Run());
return "prueba reparacion iniciada";
