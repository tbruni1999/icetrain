// Prueba del M12: guardar y cargar (Anexo B.4 y sección 25.2: "guardar/cargar conserva recursos, construcciones parciales,
// combustible y estado de switches; no se duplican ítems ni blueprints al recargar").
//   A) Se arma un estado: barreta tomada, traje puesto, plano leído, pallet desarmado y armario a medias, hielo de la estación
//      roto y puerta abierta, cama a medio construir, sierra y bidón fabricados (con combustible adentro), un bulto tirado
//      afuera, hielo de la bifurcación roto y su obra empezada, una ración comida, el tren movido, el jugador afuera.
//   Se guarda (en un archivo de prueba, no en la partida del jugador) y se toma la "huella" del estado.
//   B) Se cambian muchas cosas (más desarme, más comida, más obra, el tren en otro lado): la huella cambia.
//   C) Se carga: la escena se recarga y la huella tiene que ser la de A.
//   D) Se guarda y se carga otra vez: tiene que seguir igual (nada se duplica al recargar).
// La prueba corre en un objeto que sobrevive a la recarga de la escena (CoroutineHost).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m12_guardado.cs" --timeout 60
// Resultado en la Console, líneas [Guardado].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var hostObject = new UnityEngine.GameObject("Prueba_Guardado");
UnityEngine.Object.DontDestroyOnLoad(hostObject);
var host = hostObject.AddComponent<SurvivalEnTren.Core.CoroutineHost>();
var items = "Assets/_Project/Data/Items/";
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Metal.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Tela.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "Componentes.asset");
var needle = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + "AgujaCambio.asset");
var allItems = new[] { wood, metal, cloth, parts, needle };
var sawPlan = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_SierraManual.asset");
var canPlan = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Crafting.Blueprint>("Assets/_Project/Data/Crafting/Plano_Bidon.asset");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

// Referencias de la escena actual (se vuelven a buscar después de cada recarga).
SurvivalEnTren.Save.SaveSystem saver = null;
SurvivalEnTren.Train.TrainMover train = null;
UnityEngine.Rigidbody trainBody = null;
SurvivalEnTren.Train.TrainControls controls = null;
SurvivalEnTren.Player.PlayerMotor motor = null;
SurvivalEnTren.Interaction.PlayerInteractor who = null;
SurvivalEnTren.Equipment.PlayerGear gear = null;
SurvivalEnTren.Items.Backpack backpack = null;
SurvivalEnTren.Crafting.BlueprintBook book = null;
SurvivalEnTren.Survival.PlayerVitals vitals = null;
SurvivalEnTren.Survival.GameClock clock = null;
SurvivalEnTren.Train.FuelTank tank = null;
SurvivalEnTren.Probe.ProbeScanner scanner = null;
SurvivalEnTren.World.FloatingOrigin origin = null;
void Refs()
{
    saver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Save.SaveSystem>();
    train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
    trainBody = train.GetComponent<UnityEngine.Rigidbody>();
    controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
    motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
    who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
    gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
    backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
    book = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintBook>();
    vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
    clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
    tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
    scanner = train.GetComponent<SurvivalEnTren.Probe.ProbeScanner>();
    origin = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.World.FloatingOrigin>();
    saver.FileName = "prueba_guardado.json"; // no pisar la partida del jugador
    clock.SpeedMultiplier = 0f;              // el tiempo quieto mientras se compara
}
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
T Find<T>(System.Func<T, bool> match) where T : UnityEngine.Object
{
    foreach (var o in UnityEngine.Object.FindObjectsByType<T>()) if (match(o)) return o;
    return null;
}
SurvivalEnTren.Items.SalvageProp Prop(string id) => Find<SurvivalEnTren.Items.SalvageProp>(p => p.StableId == id);
SurvivalEnTren.Building.BuildSite Site(string id) => Find<SurvivalEnTren.Building.BuildSite>(s => s.StableId == id);
SurvivalEnTren.Poi.FrozenDoor Ice(string id) => Find<SurvivalEnTren.Poi.FrozenDoor>(d => d.StableId == id);
UnityEngine.Vector3 Abs(UnityEngine.Vector3 w) => w + new UnityEngine.Vector3((float)origin.AbsoluteOriginX, 0f, (float)origin.AbsoluteOriginZ);

// ---- La huella del estado: exacta (texto) y aproximada (números con tolerancia).
var exactA = new System.Collections.Generic.SortedDictionary<string, string>();
var approxA = new System.Collections.Generic.SortedDictionary<string, float>();
void Fingerprint(System.Collections.Generic.SortedDictionary<string, string> ex, System.Collections.Generic.SortedDictionary<string, float> ap)
{
    ex.Clear(); ap.Clear();
    ex["gear"] = $"barreta {gear.Has(SurvivalEnTren.Equipment.GearId.Crowbar)} bomba {gear.Has(SurvivalEnTren.Equipment.GearId.HandPump)} sierra {gear.Has(SurvivalEnTren.Equipment.GearId.Saw)} traje {gear.SuitWorn}";
    ex["gear.pickups"] = string.Join(",", System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Select(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Equipment.GearPickup>(), g => g.name), n => n));
    ex["planos"] = string.Join(",", System.Linq.Enumerable.OrderBy(book.LearnedIds, n => n)) + " | hojas en el mundo " + UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Crafting.BlueprintPickup>().Length;
    var packList = new System.Collections.Generic.List<string>();
    foreach (var piece in backpack.Grid.Items) packList.Add($"{piece.Item.Id}@{piece.X},{piece.Y}{(piece.Rotated ? "r" : "")}");
    packList.Sort();
    ex["mochila"] = string.Join(" ", packList);
    foreach (var st in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.StorageContainer>())
    {
        var l = new System.Collections.Generic.List<string>();
        if (st.Grid != null) foreach (var piece in st.Grid.Items) l.Add($"{piece.Item.Id}@{piece.X},{piece.Y}{(piece.Rotated ? "r" : "")}");
        l.Sort();
        ex["mueble " + st.StableId] = st.Grid == null ? "(sin cuadrícula)" : $"{st.Grid.Width}x{st.Grid.Height}: " + string.Join(" ", l);
    }
    foreach (var site in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>())
        ex["obra " + site.StableId] = $"nivel {site.Level} aportado [{string.Join(",", site.ContributedSnapshot())}] bloqueada {site.Locked}";
    foreach (var rail in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Rail.RailSwitch>())
        ex["cambio " + rail.name] = $"{rail.Condition} desvío {rail.IsDiverging}";
    var props = new System.Collections.Generic.List<string>();
    foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) { props.Add(p.StableId); if (p.Progress > 0f) ap["desarme " + p.StableId] = p.Progress; }
    props.Sort();
    ex["por desarmar"] = props.Count + ": " + string.Join(",", props);
    var ices = new System.Collections.Generic.List<string>();
    foreach (var ice in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Poi.FrozenDoor>()) if (!ice.IsBroken) ices.Add(ice.StableId);
    ices.Sort();
    ex["hielos"] = string.Join(",", ices);
    var doors = new System.Collections.Generic.List<string>();
    foreach (var door in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Interaction.SlidingDoor>()) if (door.IsOpen) doors.Add(SurvivalEnTren.Save.SaveSystem.PathOf(door.transform));
    doors.Sort();
    ex["puertas abiertas"] = string.Join(",", doors);
    var foods = new System.Collections.Generic.List<string>();
    foreach (var f in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Survival.FoodItem>()) foods.Add(f.name);
    foods.Sort();
    ex["comida"] = string.Join(",", foods);
    // Bultos: por material (cantidades) y cuántos adentro del tren.
    var bundleTotals = new System.Collections.Generic.SortedDictionary<string, int>();
    int bundlesInTrain = 0, bundleCount = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
    {
        if (b.Item == null || b.Count <= 0) continue;
        bundleCount++;
        bundleTotals.TryGetValue(b.Item.Id, out int c); bundleTotals[b.Item.Id] = c + b.Count;
        var local = train.transform.InverseTransformPoint(b.transform.position);
        if (UnityEngine.Mathf.Abs(local.x) < 1.75f && UnityEngine.Mathf.Abs(local.z) < 9.6f && local.y > 0.6f && local.y < 3.6f) bundlesInTrain++;
    }
    ex["bultos"] = $"{bundleCount} bultos ({bundlesInTrain} en el tren): " + string.Join(", ", System.Linq.Enumerable.Select(bundleTotals, kv => kv.Key + " ×" + kv.Value));
    // Cuentas de cada material en todo el mundo (no se pierde ni se duplica nada).
    foreach (var item in allItems)
    {
        int total = backpack.CountOf(item);
        foreach (var st in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.StorageContainer>()) if (st.Grid != null) total += st.Grid.CountOf(item);
        foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == item) total += b.Count;
        foreach (var p in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>()) foreach (var y in p.Recipe.Yields) if (y.item == item) total += y.count;
        foreach (var site in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Building.BuildSite>()) total += site.ContributedOf(item);
        ex["total " + item.Id] = total.ToString();
    }
    // Objetos que se llevan en la mano: cuántos, los fabricados y el combustible de los bidones.
    int crafted = 0, carries = 0;
    var canLiters = new System.Collections.Generic.List<string>();
    foreach (var c in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Interaction.Carryable>())
    {
        carries++;
        if (c.name.StartsWith(SurvivalEnTren.Save.SaveSystem.CraftedCanName)) crafted++;
        var can = c.GetComponent<SurvivalEnTren.Train.FuelCan>();
        if (can != null) canLiters.Add(can.Liters.ToString("0.0"));
        var local = train.transform.InverseTransformPoint(c.transform.position);
        bool inside = UnityEngine.Mathf.Abs(local.x) < 1.75f && UnityEngine.Mathf.Abs(local.z) < 9.6f && local.y > 0.6f && local.y < 3.6f;
        var pos = inside ? local : Abs(c.transform.position);
        string key = SurvivalEnTren.Save.SaveSystem.PathOf(c.transform);
        ap[$"objeto {key} x"] = pos.x; ap[$"objeto {key} z"] = pos.z;
    }
    canLiters.Sort();
    ex["objetos"] = $"{carries} (fabricados {crafted}), bidones [{string.Join(", ", canLiters)}] L";
    foreach (var src in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelSource>()) ap["fuente " + src.StableId] = src.Liters;
    ap["tanque"] = tank.Liters;
    ex["tren"] = $"{train.RailPosition.Segment.name} {train.RailPosition.Distance:0.0} m, {train.SpeedMetersPerSecond * 3.6f:0.0} km/h, reversor {controls.Reverser}, potencia {controls.PowerNotch}, freno {controls.BrakeNotch}";
    var pl = train.transform.InverseTransformPoint(motor.transform.position);
    bool playerIn = UnityEngine.Mathf.Abs(pl.x) < 1.75f && UnityEngine.Mathf.Abs(pl.z) < 9.6f && pl.y > 0.6f && pl.y < 3.6f;
    ex["jugador"] = playerIn ? "en el tren" : "afuera";
    var pp = playerIn ? pl : Abs(motor.transform.position);
    ap["jugador x"] = pp.x; ap["jugador z"] = pp.z;
    ap["vida"] = vitals.Health; ap["comida"] = vitals.Satiety; ap["energía"] = vitals.Energy; ap["calor"] = vitals.Warmth;
    ap["hora"] = (float)clock.TotalHours;
    ex["sonda"] = scanner.Target != null ? scanner.Target.StableId : "(ninguna)";
}
// Compara con la huella A: devuelve las diferencias en palabras.
string Compare(System.Collections.Generic.SortedDictionary<string, string> ex, System.Collections.Generic.SortedDictionary<string, float> ap)
{
    var diffs = new System.Collections.Generic.List<string>();
    foreach (var kv in exactA)
    {
        ex.TryGetValue(kv.Key, out string v);
        if (v != kv.Value) diffs.Add($"{kv.Key}: guardado \"{kv.Value}\" / ahora \"{v ?? "(no está)"}\"");
    }
    foreach (var kv in ex) if (!exactA.ContainsKey(kv.Key)) diffs.Add($"{kv.Key}: no estaba al guardar (\"{kv.Value}\")");
    foreach (var kv in approxA)
    {
        float tol = kv.Key.StartsWith("objeto") ? 0.3f : kv.Key.StartsWith("jugador") ? 0.3f : kv.Key == "hora" ? 0.05f : 0.6f;
        if (!ap.TryGetValue(kv.Key, out float v)) { diffs.Add($"{kv.Key}: no está"); continue; }
        if (UnityEngine.Mathf.Abs(v - kv.Value) > tol) diffs.Add($"{kv.Key}: guardado {kv.Value:0.00} / ahora {v:0.00}");
    }
    return diffs.Count == 0 ? "IGUAL" : $"{diffs.Count} DIFERENCIAS: " + string.Join("; ", diffs);
}

// H2: desarmar es a golpes; si se rompe, lo que sale cae al piso y se junta con E (los bultos nuevos de ahí cerca).
System.Collections.IEnumerator Salvage(string id, float upTo)
{
    var prop = Prop(id);
    if (prop == null) yield break;
    var where = prop.transform.position;
    var already = new System.Collections.Generic.HashSet<SurvivalEnTren.Items.MaterialBundle>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>());
    while (prop != null && prop.Progress < upTo)
    {
        prop.OnToolHit(new SurvivalEnTren.Equipment.ToolHit(prop.Recipe.RequiredTool, where, UnityEngine.Vector3.up, null, null));
        yield return Frames(3);
    }
    yield return Frames(40);
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        if (!already.Contains(b) && (b.transform.position - where).sqrMagnitude < 9f) b.Interact(who);
    yield return Frames(10);
}
System.Collections.IEnumerator WaitLoad(int before)
{
    int g = 0;
    while (SurvivalEnTren.Save.SaveSystem.LoadsApplied <= before && g++ < 600) yield return null;
    yield return Frames(20);
    Refs();
    yield return Frames(10);
}

System.Collections.IEnumerator Run()
{
    Refs();
    yield return Frames(60);

    // ---- A) Armar un estado.
    var crowbarPickup = Find<SurvivalEnTren.Equipment.GearPickup>(g => g.Gear == SurvivalEnTren.Equipment.GearId.Crowbar);
    crowbarPickup.Interact(who);                                    // la barreta del perchero
    gear.SetSuit(true);
    foreach (var rack in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Equipment.SuitRack>()) rack.Sync(true);
    UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.BlueprintPickup>().Interact(who); // el plano de la estación
    yield return Salvage("salv_estacion_pallet_001", 2f);           // madera ×4 a la mochila
    yield return Salvage("salv_estacion_armario_001", 0.5f);        // a medias
    var stationIce = Ice("door_prueba_estacion_001");
    while (stationIce != null && !stationIce.IsBroken && stationIce.Channel(who, 0.05f)) yield return wait;
    yield return Frames(5);
    foreach (var door in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Interaction.SlidingDoor>())
        if (SurvivalEnTren.Save.SaveSystem.PathOf(door.transform).Contains("Estacion_Mantenimiento")) door.Interact(who);
    var bed = Site("obra_tren_cama_001");
    int g1 = 0; while (backpack.CountOf(wood) > 2 && bed.Channel(who, 0.55f) && g1++ < 20) { } // cama: 2 tablones de 4
    var bench = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Crafting.Workbench>();
    backpack.Add(metal, 3);
    bench.Complete(sawPlan, backpack, gear);                          // la sierra (tablón + chapa)
    bench.Complete(canPlan, backpack, gear);                          // un bidón (2 chapas), sobre la mesa
    yield return Frames(40);
    foreach (var can in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelCan>())
        if (can.name.StartsWith(SurvivalEnTren.Save.SaveSystem.CraftedCanName)) can.Add(7f);
    // Un tablón tirado afuera, junto al tren.
    backpack.Remove(wood, 1);
    SurvivalEnTren.Items.MaterialBundle.Spawn(UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.BackpackSettings>(items + "BackpackSettings.asset"),
        wood, 1, trainBody.position + trainBody.rotation * new UnityEngine.Vector3(5f, 0.5f, 0f), UnityEngine.Vector3.zero);
    // La bifurcación: hielo roto y la obra empezada.
    var forkIce = Ice("ice_switch_oeste_001");
    while (forkIce != null && !forkIce.IsBroken && forkIce.Channel(who, 0.05f)) yield return wait;
    yield return Frames(5);
    backpack.Add(metal, 1);
    var fork = Site("obra_cambio_oeste_001");
    int g2 = 0; while (fork.Channel(who, 0.55f) && g2++ < 20) { }
    // Comer una ración del tren.
    var ration = Find<SurvivalEnTren.Survival.FoodItem>(f => true);
    ration.Interact(who);
    // El tren, un poco más adelante; el jugador afuera, junto a la estación.
    controls.TryMoveReverser(+1);
    while (controls.PowerNotch < 2) controls.MovePowerBrake(+1);
    yield return Frames(400);
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;
    motor.TeleportTo(new UnityEngine.Vector3(46f, 0.1f, -197f), null);
    int g3 = 0; while (scanner.Target == null && g3++ < 8) scanner.SelectNext();
    yield return Frames(60);
    Fingerprint(exactA, approxA);
    bool saved = saver.Save();
    yield return Frames(5);
    log.Append($"A) ESTADO ARMADO Y GUARDADO={saved} en {saver.SavePath}. Huella: {exactA.Count + approxA.Count} datos. Mochila: {exactA["mochila"]}; obras: cama \"{exactA["obra obra_tren_cama_001"]}\", bifurcación \"{exactA["obra obra_cambio_oeste_001"]}\"; {exactA["objetos"]}; {exactA["bultos"]}; tren {exactA["tren"]}; tanque {approxA["tanque"]:0.0} L; jugador {exactA["jugador"]}.");

    // ---- B) Cambiar muchas cosas después de guardar.
    yield return Salvage("salv_estacion_armario_001", 2f);
    yield return Salvage("salv_estacion_colchon_001", 2f);
    var ration2 = Find<SurvivalEnTren.Survival.FoodItem>(f => true);
    if (ration2 != null) ration2.Interact(who);
    backpack.Add(wood, 2);
    int g4 = 0; while (bed.Channel(who, 0.55f) && g4++ < 20) { }
    clock.Skip(3f);
    controls.TryMoveReverser(+1);
    while (controls.PowerNotch < 3) controls.MovePowerBrake(+1);
    yield return Frames(300);
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
    while (UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f) yield return wait;
    var exB = new System.Collections.Generic.SortedDictionary<string, string>();
    var apB = new System.Collections.Generic.SortedDictionary<string, float>();
    Fingerprint(exB, apB);
    string diffB = Compare(exB, apB);
    log.Append($" B) DESPUÉS DE GUARDAR se cambió todo: la huella {(diffB == "IGUAL" ? "NO cambió (mal)" : "cambió (bien): " + diffB.Substring(0, System.Math.Min(160, diffB.Length)) + "…")}");

    // ---- C) Cargar: la escena se recarga y vuelve todo a A.
    int loads = SurvivalEnTren.Save.SaveSystem.LoadsApplied;
    float t0 = UnityEngine.Time.realtimeSinceStartup;
    bool loading = saver.Load();
    yield return WaitLoad(loads);
    float tookLoad = UnityEngine.Time.realtimeSinceStartup - t0;
    var exC = new System.Collections.Generic.SortedDictionary<string, string>();
    var apC = new System.Collections.Generic.SortedDictionary<string, float>();
    Fingerprint(exC, apC);
    log.Append($" C) CARGAR={loading} (recarga de la escena + aplicar: {tookLoad:0.0} s; cargas aplicadas {SurvivalEnTren.Save.SaveSystem.LoadsApplied}): comparado con lo guardado → {Compare(exC, apC)}.");

    // ---- D) Otro estado, ahora con el jugador ADENTRO del tren, cosas en la mochila, en el estante y un bulto en el piso del
    // tren; guardar y cargar: tiene que volver igual (lo de adentro del tren se guarda relativo al tren).
    backpack.Add(metal, 1);
    backpack.Add(cloth, 1);
    var starter = Find<SurvivalEnTren.Items.StorageContainer>(s => s.StableId == "storage_tren_estante_precario_001");
    starter.Grid.TryAutoPlace(wood);
    SurvivalEnTren.Items.MaterialBundle.Spawn(UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.BackpackSettings>(items + "BackpackSettings.asset"),
        parts, 1, trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.6f, 1.3f, -3f), UnityEngine.Vector3.zero);
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 1.5f), trainBody);
    yield return Frames(90);
    Fingerprint(exactA, approxA);
    saver.Save();
    yield return Frames(5);
    log.Append($" D) OTRO ESTADO (jugador {exactA["jugador"]}, mochila \"{exactA["mochila"]}\", estante \"{exactA["mueble storage_tren_estante_precario_001"]}\", {exactA["bultos"]}) guardado;");
    loads = SurvivalEnTren.Save.SaveSystem.LoadsApplied;
    saver.Load();
    yield return WaitLoad(loads);
    var exD = new System.Collections.Generic.SortedDictionary<string, string>();
    var apD = new System.Collections.Generic.SortedDictionary<string, float>();
    Fingerprint(exD, apD);
    log.Append($" cargado → {Compare(exD, apD)}.");

    // ---- E) Cargar otra vez la misma partida (sin guardar): nada se duplica.
    loads = SurvivalEnTren.Save.SaveSystem.LoadsApplied;
    saver.Load();
    yield return WaitLoad(loads);
    var exE = new System.Collections.Generic.SortedDictionary<string, string>();
    var apE = new System.Collections.Generic.SortedDictionary<string, float>();
    Fingerprint(exE, apE);
    log.Append($" E) CARGAR OTRA VEZ: → {Compare(exE, apE)}. Cuentas del mundo: madera {exE["total item_madera"]}, chapas {exE["total item_metal"]}, tela {exE["total item_tela"]}, componentes {exE["total item_componentes"]}, agujas {exE["total item_aguja_cambio"]}.");

    // Sin partida: un archivo que no existe no carga.
    saver.FileName = "no_existe.json";
    bool none = saver.Load();
    log.Append($" Cargar sin partida guardada: {(none ? "CARGÓ (mal)" : "no carga y avisa")} \"{saver.LastNotice}\".");
    saver.FileName = "prueba_guardado.json";
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Guardado] " + log);
    UnityEngine.Object.Destroy(hostObject);
}
host.StartCoroutine(Run());
return "prueba guardado iniciada";
