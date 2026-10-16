// Prueba de herramientas y forja, paso H4: la forja del tren de punta a punta.
// Construir la obra (hogar, fuelle, yunque; desde el 2026-10-16 con kits del banco instalados en el lugar), el martillo en la barra (4), encender con leña, meter una chapa al fuego, pasarla al
// yunque al rojo y martillarla con golpes de verdad (apuntando); si se enfría, de vuelta al fuego; sale una planchuela. Martillar
// en frío rebota. El combustible del tanque por la válvula. La forja calienta el vagón (con la calefacción apagada).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/h4_forja.cs"
// Resultado en la Console, línea [Forja].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var body = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var belt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var climate = train.GetComponent<SurvivalEnTren.Survival.TrainInteriorClimate>();
var forge = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Forge>();
var site = forge.GetComponent<SurvivalEnTren.Building.BuildSite>();
var bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
SurvivalEnTren.Crafting.ForgePart mouth = null, firebox = null, valve = null, anvil = null;
foreach (var part in forge.GetComponentsInChildren<SurvivalEnTren.Crafting.ForgePart>(true))
{
    if (part.Role == SurvivalEnTren.Crafting.ForgePartRole.Mouth) mouth = part;
    else if (part.Role == SurvivalEnTren.Crafting.ForgePartRole.Firebox) firebox = part;
    else if (part.Role == SurvivalEnTren.Crafting.ForgePartRole.Valve) valve = part;
    else anvil = part;
}
var s = forge.Settings;
var wood = s.WoodItem; var metal = s.RawItem; var bar = s.ForgedItem;
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;

System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
string P(SurvivalEnTren.Interaction.IInteractable i) => i.GetPrompt(who) ?? "(sin indicación)";
UnityEngine.Vector3 Inside(float x, float z) => body.position + body.rotation * new UnityEngine.Vector3(x, 1.05f, z);
void LookAtLocal(UnityEngine.Vector3 local)
{
    var target = train.transform.TransformPoint(local);
    var eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var d = target - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
int rebounds = 0;
belt.Struck += (h, useful) => { if (!useful) rebounds++; };

System.Collections.IEnumerator Run()
{
    yield return Frames(20);
    // 0) Al empezar.
    log.Append($"AL EMPEZAR: obra \"{site.Project.DisplayName}\" {site.Project.PhaseCount} fases, nivel {site.Level}; lista para usar={forge.Ready}; la boca dice \"{P(mouth)}\"; planchuela: {bar.DisplayName} {bar.SizeCells.x}x{bar.SizeCells.y}, {bar.WeightKg:0.0} kg.");

    // 1) Construir la obra: el banco arma cada kit (se adelanta su reloj), se toma y se instala en la forja (lo mismo que
    //    mantener E con el kit en la mano).
    backpack.Add(metal, 4); backpack.Add(wood, 2); backpack.Add(cloth, 2);
    motor.TeleportTo(Inside(0.0f, -7.0f), body);
    LookAtLocal(new UnityEngine.Vector3(1.05f, 1.5f, -7.0f));
    yield return Frames(20);
    string sitePrompt = "";
    var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
    int rounds = 0;
    while (!site.IsComplete && rounds++ < 5)
    {
        if (!bench.StartJob(new SurvivalEnTren.Crafting.BenchRecipe(site, site.Level), backpack, gear)) break;
        bench.FinishJobNow();
        yield return Frames(5);
        SurvivalEnTren.Building.UpgradeKit kit = null;
        foreach (var k in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.UpgradeKit>()) kit = k;
        carrier.Pick(kit.GetComponent<SurvivalEnTren.Interaction.Carryable>());
        yield return Frames(3);
        sitePrompt = P(site);
        site.Interact(who);
        yield return Frames(5);
    }
    log.Append($" OBRA: \"{sitePrompt}\" → nivel {site.Level}/{site.Project.PhaseCount}, terminada={site.IsComplete}, lista={forge.Ready}; quedan en la mochila chapas {backpack.CountOf(metal)}, tablones {backpack.CountOf(wood)}, tela {backpack.CountOf(cloth)}.");

    // 2) El martillo: un plano del banco y la casilla 4.
    bool inBench = false;
    var catalogField = typeof(SurvivalEnTren.Crafting.Workbench).GetField("catalog", flags);
    var arr = catalogField != null ? catalogField.GetValue(bench) as SurvivalEnTren.Crafting.Blueprint[] : null;
    string hammerPlan = "";
    if (arr != null) foreach (var b in arr) if (b != null && b.OutputGear == SurvivalEnTren.Equipment.GearId.Hammer && b.OutputKind == SurvivalEnTren.Crafting.CraftOutputKind.Gear) { inBench = true; hammerPlan = $"{b.DisplayName}: " + string.Join(", ", System.Array.ConvertAll(b.Ingredients, i => $"{i.item.PieceName} ×{i.count}")); }
    bool slot4Before = belt.Available(3);
    gear.Give(SurvivalEnTren.Equipment.GearId.Hammer);
    log.Append($" MARTILLO: en el banco={inBench} ({hammerPlan}); casilla 4 \"{belt.Settings.Slots[3].label}\" antes={slot4Before}, después de tenerlo={belt.Available(3)}.");

    // 3) Encender con leña.
    backpack.Add(wood, 4); backpack.Add(metal, 2);
    motor.TeleportTo(Inside(-0.2f, -7.0f), body);
    LookAtLocal(new UnityEngine.Vector3(0.61f, 1.3f, -7.03f));
    yield return Frames(20);
    string fireboxPrompt = P(firebox);
    for (int i = 0; i < 4; i++) firebox.Interact(who); // 4 tablones: 1,6 h de fuego
    float t = 0f, half = -1f;
    while (forge.Temperature < 0.99f && t < 40f) { yield return wait; t += UnityEngine.Time.fixedDeltaTime; if (half < 0f && forge.Temperature >= 0.5f) half = t; }
    log.Append($" FUEGO: \"{fireboxPrompt}\"; 4 tablones = {forge.WoodHours * 60f:0} min de fuego; ardiendo={forge.Burning}; a la mitad a los {half:0.0} s, a pleno a los {t:0.0} s; calienta el vagón {climate.ExtraHeatCPerHour:0.0} °C/h; luz encendida={forge.GetComponentInChildren<UnityEngine.Light>().enabled}.");

    // 4) La chapa al fuego, hasta el rojo, al yunque.
    LookAtLocal(new UnityEngine.Vector3(1.08f, 1.8f, -7.02f));
    yield return Frames(10);
    string mouthPrompt = P(mouth);
    int metalBefore = backpack.CountOf(metal);
    mouth.Interact(who);
    var placed = forge.PieceAt;
    t = 0f;
    while (forge.PieceHeat < 0.95f && t < 30f) { yield return wait; t += UnityEngine.Time.fixedDeltaTime; } // hasta "al rojo vivo: lista"
    string hotPrompt = P(mouth);
    mouth.Interact(who);
    log.Append($" PIEZA: \"{mouthPrompt}\" → en el fuego={placed}, chapas {metalBefore} → {backpack.CountOf(metal)}; al rojo a los {t:0.0} s (\"{hotPrompt}\"); E la pasó al {forge.PieceAt}.");

    // 5) Martillar con golpes de verdad; si se enfría, de vuelta al fuego.
    motor.TeleportTo(Inside(0.0f, -4.9f), body);
    LookAtLocal(new UnityEngine.Vector3(1.0f, 1.74f, -4.9f));
    belt.Select(3);
    yield return Frames(30);
    string anvilPrompt = P(anvil);
    int reheats = 0, swings0 = belt.Swings, rebounds0 = rebounds;
    t = 0f;
    while (forge.Forged == 0 && t < 120f)
    {
        if (forge.PieceAt == SurvivalEnTren.Crafting.ForgePieceAt.Anvil && !forge.PieceHot)
        {
            anvil.Interact(who); // E: de vuelta al fuego
            reheats++;
            float w = 0f;
            while (forge.PieceHeat < 0.95f && w < 30f) { yield return wait; w += UnityEngine.Time.fixedDeltaTime; t += UnityEngine.Time.fixedDeltaTime; }
            mouth.Interact(who); // al yunque
        }
        belt.TrySwing();
        yield return wait;
        t += UnityEngine.Time.fixedDeltaTime;
    }
    yield return Frames(40);
    log.Append($" YUNQUE: \"{anvilPrompt}\"; {belt.Swings - swings0} golpes ({s.HitsToForge} sirven), rebotes {rebounds - rebounds0}, volvió al fuego {reheats} veces, en {t:0.0} s; planchuelas hechas {forge.Forged}.");

    // 6) Juntar la planchuela.
    int bars = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        if (b.Item == bar && (b.transform.position - forge.AnvilSlot.position).sqrMagnitude < 4f) { b.Interact(who); bars++; }
    yield return Frames(5);
    log.Append($" PLANCHUELA: bultos sobre el yunque {bars}; en la mochila {backpack.CountOf(bar)}.");

    // 7) Martillar en frío rebota.
    mouth.Interact(who); // otra chapa al fuego (fría)
    mouth.Interact(who); // directo al yunque, sin calentar
    yield return Frames(5);
    int coldHits = forge.PieceHits, r0 = rebounds;
    belt.TrySwing();
    yield return Frames(50);
    log.Append($" EN FRÍO: calor {forge.PieceHeat:0.00}, \"{P(anvil)}\"; golpes que sirvieron {forge.PieceHits - coldHits}, rebotes {rebounds - r0}.");
    anvil.Interact(who); // vuelve al fuego (queda ahí)

    // 8) Combustible del tanque y calor del vagón (calefacción apagada, puertas cerradas), con saltos de reloj.
    belt.Select(-1);
    climate.HeaterOn = false;
    float woodLeft = forge.WoodHours;
    clock.Skip(woodLeft + 0.05f); // se termina la leña
    valve.Interact(who);
    string valvePrompt = P(valve);
    float tank0 = tank.Liters, temp0 = climate.InteriorTemperatureC;
    clock.Skip(3f);
    float withForge = climate.InteriorTemperatureC, used = tank0 - tank.Liters;
    valve.Interact(who);
    clock.Skip(1f);
    float coldForge = forge.Temperature;
    clock.Skip(2f);
    float without = climate.InteriorTemperatureC;
    log.Append($" COMBUSTIBLE: válvula \"{valvePrompt}\"; 3 h abierta gastó {used:0.0} L del tanque ({s.DieselLitersPerHour:0.0} L/h); CALOR del vagón con la calefacción apagada: {temp0:0.0} °C → con la forja 3 h {withForge:0.0} °C; cerrada, la forja a la hora está a {coldForge * 100f:0}%; 2 h más sin forja {without:0.0} °C.");
    climate.HeaterOn = true;

    // 9) Guardado: el archivo lleva la forja y el martillo (a otro archivo, para no tocar la partida de Tomi).
    var save = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Save.SaveSystem>();
    var fileField = typeof(SurvivalEnTren.Save.SaveSystem).GetField("fileName", flags);
    string oldFile = (string)fileField.GetValue(save);
    fileField.SetValue(save, "prueba_forja.json");
    bool saved = save.Save();
    var data = UnityEngine.JsonUtility.FromJson<SurvivalEnTren.Save.SaveGame>(System.IO.File.ReadAllText(save.SavePath));
    log.Append($" GUARDADO={saved}: forja presente={data.forge.present}, leña {data.forge.woodHours:0.00} h (en el juego {forge.WoodHours:0.00}), válvula {data.forge.valveOpen}, calor {data.forge.temperature:0.00} ({forge.Temperature:0.00}), pieza en {(SurvivalEnTren.Crafting.ForgePieceAt)data.forge.pieceAt} ({forge.PieceAt}); martillo={data.player.hammer}; obra de la forja guardada={data.sites.Exists(x => x.id == "obra_tren_forja_001")}.");
    System.IO.File.Delete(save.SavePath);
    fileField.SetValue(save, oldFile);
    UnityEngine.Debug.Log($"[Forja] {log}");
}
motor.StartCoroutine(Run());
return "prueba forja iniciada";
