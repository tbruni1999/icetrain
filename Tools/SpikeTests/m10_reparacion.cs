// Prueba del M10 paso 2: el proyecto de reparación de la bifurcación. Congelado, la obra no se ve ni se puede empezar; con el
// hielo roto aparece como fantasmas; se aporta por fases (palanca, durmientes y riel, aguja de cambio), el avance parcial se
// conserva entre viajes, la pieza especial se recoge de la segunda sala de la estación, y al terminar el cambio queda
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
var ghost = (UnityEngine.Material)typeof(SurvivalEnTren.Building.BuildSite).GetField("ghostMaterial", flags).GetValue(site);
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
            if (r != null && r.sharedMaterial != ghost) solid++;
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
System.Collections.IEnumerator Contribute(int maxLoops = 60)
{
    int g = 0;
    while (site.Channel(who, 0.55f) && g++ < maxLoops) yield return null;
    for (int i = 0; i < 3; i++) yield return wait;
}
void Give(SurvivalEnTren.Items.ItemDefinition item, int n) { backpack.Add(item, n); }
void StandAtLever(float side = 2.2f, float aimHeight = 0.7f)
{
    var stand = UnityEngine.GameObject.Find("Cambio_2_Palanca").transform;
    var target = stand.position + new UnityEngine.Vector3(0f, aimHeight, 0f);
    motor.TeleportTo(new UnityEngine.Vector3(target.x + side, 0.1f, target.z + 1.2f), null);
    LookAtWorld(target);
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;

    // 0) Datos y lugares: la pieza especial, los objetos desmontables de la bifurcación.
    SurvivalEnTren.Items.MaterialBundle needleBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == needle) needleBundle = b;
    int woodSup = 0, metalSup = 0, partsSup = 0, supProps = 0;
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>())
    {
        if (!p.StableId.StartsWith("salv_cambio_")) continue;
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
    log.Append($"DATOS: pieza especial \"{needle.DisplayName}\" {needle.SizeCells.x}x{needle.SizeCells.y}, {needle.WeightKg:0.0} kg, rota={needle.CanRotate}, en la segunda sala de la estación={(needleBundle != null)} (a {(needleBundle != null ? UnityEngine.Vector3.Distance(needleBundle.transform.position, new UnityEngine.Vector3(40f, 0f, -200f)) : -1f):0} m del centro de la estación); proyecto \"{req.DisplayName}\":{reqText} objetos desmontables junto a la bifurcación: {supProps} (madera ×{woodSup}, chapas ×{metalSup}, componentes ×{partsSup}).");

    // 1) Congelado: la obra no se ve ni se puede empezar.
    StandAtLever();
    for (int i = 0; i < 40; i++) yield return wait;
    object t0 = Target();
    log.Append($" CONGELADO: obra bloqueada={site.Locked}; {Pieces()}; zonas de uso prendidas={ZonesOn()}; apuntando el objetivo es {Name(t0)} (el hielo), indicación \"{P(t0 as SurvivalEnTren.Interaction.IInteractable)}\".");
    Give(metal, 2); Give(parts, 1);
    yield return Contribute();
    log.Append($" Con chapas y componente en la mochila, la obra no recibe nada: nivel {site.Level}, aportado {site.ContributedUnits}.");
    backpack.Remove(metal, 2); backpack.Remove(parts, 1);

    // 2) Hielo roto: la obra aparece (fantasmas).
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    int g = 0;
    while (ice != null && !ice.IsBroken && g++ < 1000) { if (!ice.Channel(who, UnityEngine.Time.fixedDeltaTime)) break; yield return wait; }
    for (int i = 0; i < 30; i++) yield return wait;
    LookAtWorld(UnityEngine.GameObject.Find("Cambio_2_Palanca").transform.position + new UnityEngine.Vector3(0f, 0.7f, 0f));
    for (int i = 0; i < 20; i++) yield return wait;
    object t1 = Target();
    log.Append($" SIN HIELO: estado {sw.Condition}; obra bloqueada={site.Locked}; {Pieces()}; zonas prendidas={ZonesOn()}; apuntando el objetivo es {Name(t1)}, indicación \"{P(t1 as SurvivalEnTren.Interaction.IInteractable)}\"; el muñón de la palanca rota se ve={UnityEngine.GameObject.Find("Palanca_Rota") != null && UnityEngine.GameObject.Find("Palanca_Rota").activeSelf}.");

    // 3) Fase 1: la palanca (chapa ×2 + componente ×1), aportando en dos tandas.
    Give(metal, 2);
    yield return Contribute();
    log.Append($" FASE 1 (solo las chapas): nivel {site.Level}, fracción {site.Fraction:0.00}; {Pieces()}.");
    Give(parts, 1);
    yield return Contribute();
    LookAtWorld(UnityEngine.GameObject.Find("Cambio_2_Palanca").transform.position + new UnityEngine.Vector3(0f, 0.7f, 0f));
    yield return Frames(10);
    log.Append($" FASE 1 completa: nivel {site.Level}; {Pieces()}; el muñón se ve={UnityEngine.GameObject.Find("Palanca_Rota") != null && UnityEngine.GameObject.Find("Palanca_Rota").activeSelf}; estado del cambio {sw.Condition}; moverlo → {(sw.TrySetDiverging(true, null) ? "SE MOVIÓ (mal)" : "no se mueve (bien)")}.");

    // 4) Fase 2: durmientes y riel (madera ×4 + chapa ×3), con el avance parcial conservado entre viajes.
    Give(wood, 2);
    yield return Contribute();
    float partial = site.Fraction;
    int woodIn = site.ContributedOf(wood);
    // "Se fue a buscar el resto": lejos de la obra un rato, y vuelve.
    motor.TeleportTo(new UnityEngine.Vector3(sw.Point.x + 40f, 0.1f, sw.Point.z + 40f), null);
    yield return Frames(60);
    StandAtLever();
    yield return Frames(20);
    log.Append($" FASE 2 a medias: madera aportada {woodIn} de 4 (fracción {partial:0.00}); después de irse y volver sigue {site.ContributedOf(wood)} (fracción {site.Fraction:0.00}), la mochila quedó con {backpack.CountOf(wood)} tablones; {Pieces()}.");
    Give(wood, 2); Give(metal, 3);
    yield return Contribute();
    log.Append($" FASE 2 completa: nivel {site.Level}; {Pieces()}; el cambio sigue {sw.Condition}.");

    // 5) Fase 3: la pieza especial. Sin ella no avanza; se recoge de la segunda sala de la estación.
    LookAtWorld(sw.Point + new UnityEngine.Vector3(0f, 0.6f, -4f));
    yield return Frames(30);
    object t2 = Target();
    log.Append($" FASE 3 sin la pieza: objetivo {Name(t2)}, indicación \"{P(t2 as SurvivalEnTren.Interaction.IInteractable)}\".");
    Give(metal, 5); // chapas de sobra: no sirven para esta fase
    yield return Contribute();
    log.Append($" Con chapas de sobra no avanza: nivel {site.Level}.");
    backpack.Remove(metal, 5);

    var bundlePos = needleBundle.transform.position;
    motor.TeleportTo(new UnityEngine.Vector3(bundlePos.x + 0.9f, 0.1f, bundlePos.z), null);
    LookAtWorld(bundlePos + new UnityEngine.Vector3(0f, 0.15f, 0f));
    yield return Frames(30);
    object t3 = Target();
    string pickPrompt = P(t3 as SurvivalEnTren.Interaction.IInteractable);
    needleBundle.Interact(who);
    yield return Frames(10);
    bool took = backpack.CountOf(needle) == 1;
    var needlePiece = backpack.Grid.Items.Count > 0 ? backpack.Grid.Items[0] : null;
    log.Append($" PIEZA ESPECIAL: apuntándola el objetivo es {Name(t3)}, indicación \"{pickPrompt}\"; la guardó en la mochila={took} (ocupa {(needlePiece != null ? needlePiece.Size.x + "x" + needlePiece.Size.y : "?")} casillas, {backpack.MassKg:0.0} kg).");

    StandAtLever();
    yield return Frames(20);
    yield return Contribute();
    yield return Frames(10);
    log.Append($" FASE 3 con la pieza: nivel {site.Level}/{site.Project.PhaseCount}, obra completa={site.IsComplete}; {Pieces()}; estado del cambio {sw.Condition}; la mochila quedó con {backpack.CountOf(needle)} agujas.");

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

    // 7) Cuentas: lo aportado es exactamente lo pedido.
    log.Append($" CUENTAS: aportado madera {site.ContributedOf(wood)}, chapas {site.ContributedOf(metal)}, componentes {site.ContributedOf(parts)}, agujas {site.ContributedOf(needle)} (pedido: 4, 5, 1, 1); en la mochila quedó madera {backpack.CountOf(wood)}, chapas {backpack.CountOf(metal)}, componentes {backpack.CountOf(parts)}.");
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Reparacion] " + log);
}
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
motor.StartCoroutine(Run());
return "prueba reparacion iniciada";
