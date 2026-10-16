// Prueba del M7 paso 4: desmontar los cuatro tipos de objeto con la barreta, pesos, límite de la mochila (lo que
// no entra queda en el piso), que no se pueda desmontar dos veces, y sin crear ni perder unidades.
// Desde H2 (herramientas y forja) se desarma a golpes de barreta: los cuatro de la estación con golpes de verdad (apuntando:
// el golpe sale de los ojos), el pallet de afuera con el clic izquierdo virtual mantenido; lo que cae se junta con E.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m7_desmontaje.cs" --timeout 60
// Resultado en la Console, líneas [Desmontaje].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Componentes.asset");
var items = new[] { wood, metal, cloth, parts };
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
string P(SurvivalEnTren.Interaction.IInteractable i) => i.GetPrompt(who) ?? "(sin indicación)";
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;
// Unidades de cada material en la mochila y en los bultos del mundo.
int[] Units()
{
    var u = new int[4];
    for (int k = 0; k < 4; k++) u[k] = backpack.CountOf(items[k]);
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        for (int k = 0; k < 4; k++) if (b.Item == items[k]) u[k] += b.Count;
    return u;
}
string Show(int[] u) => $"madera {u[0]}, metal {u[1]}, tela {u[2]}, componentes {u[3]}";
UnityEngine.Vector3 CenterOf(UnityEngine.Component c)
{
    var rs = c.GetComponentsInChildren<UnityEngine.Renderer>();
    var bounds = rs[0].bounds;
    foreach (var r in rs) bounds.Encapsulate(r.bounds);
    return bounds.center;
}
// Se para a ~1,4 m del objeto (del lado del punto "from") y lo mira al centro.
void StandAndLook(UnityEngine.Vector3 center, UnityEngine.Vector3 from)
{
    var dir = from - center; dir.y = 0f; dir.Normalize();
    var stand = center + dir * 1.4f;
    motor.TeleportTo(new UnityEngine.Vector3(stand.x, 0.1f, stand.z), null);
    var eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var d = center - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}

var extra = new int[4];       // unidades que la prueba puso a mano en la mochila (para llevar la cuenta)
var salvaged = new int[4];    // unidades que salieron de objetos desmontados
void Give(int k, int n) { extra[k] += backpack.Add(items[k], n); }
var belt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
var wasted = new System.Collections.Generic.List<string>(); // golpes que pegaron en otra cosa (para ver contra qué)
belt.Struck += (h, useful) => { if (!useful) wasted.Add(h.Collider.name + (h.Collider.transform.parent ? "/" + h.Collider.transform.parent.name : "")); };
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("SpikeTestMouse");
void MouseLeft(bool down) => UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = (ushort)(down ? 1 : 0) });

// Junta con E los bultos nuevos que cayeron cerca de un punto (H2: lo desarmado cae al piso).
int PickNear(UnityEngine.Vector3 where, System.Collections.Generic.HashSet<SurvivalEnTren.Items.MaterialBundle> already)
{
    int n = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        if (!already.Contains(b) && (b.transform.position - where).sqrMagnitude < 9f) { b.Interact(who); n++; }
    return n;
}

// H2: se para a ~1,4 m, lo mira al centro, saca la barreta con E (mirándolo) y lo golpea de verdad (el golpe sale de los ojos)
// hasta que se rompe; después junta lo que cayó.
System.Collections.IEnumerator Salvage(SurvivalEnTren.Items.SalvageProp prop, UnityEngine.Vector3 from)
{
    var recipe = prop.Recipe;
    var center = CenterOf(prop);
    StandAndLook(center, from);
    if (belt.ActiveTool.HasValue) belt.Select(-1); // manos libres: E saca la barreta
    for (int i = 0; i < 30; i++) yield return wait;
    object t = Target();
    string promptBefore = P(prop);
    prop.Interact(who); // E
    for (int i = 0; i < 30; i++) yield return wait;
    string promptTool = P(prop);
    var already = new System.Collections.Generic.HashSet<SurvivalEnTren.Items.MaterialBundle>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>());
    float energy = vitals.Energy;
    int swings0 = belt.Swings, hits0 = belt.HitCount; wasted.Clear();
    float t0 = 0f;
    while (prop != null && t0 < 30f)
    {
        belt.TrySwing();
        yield return wait;
        t0 += UnityEngine.Time.fixedDeltaTime;
    }
    for (int i = 0; i < 40; i++) yield return wait;
    bool gone = prop == null;
    int picked = PickNear(center, already);
    for (int i = 0; i < 5; i++) yield return wait;
    if (gone)
        foreach (var y in recipe.Yields)
            for (int k = 0; k < 4; k++) if (items[k] == y.item) salvaged[k] += y.count;
    bool crowbarOut = belt.ActiveTool == SurvivalEnTren.Equipment.GearId.Crowbar;
    log.Append($" [{recipe.DisplayName}: apuntando el objetivo es {Name(t)}; sin la barreta en la mano \"{promptBefore}\"; E la sacó={crowbarOut}, \"{promptTool}\"; {belt.Swings - swings0} golpes, pegaron {belt.HitCount - hits0} (receta {recipe.HitsToBreak}{(wasted.Count > 0 ? "; en otra cosa: " + string.Join(", ", wasted) : "")}) en {t0:0.0} s; energía {energy:0.0} → {vitals.Energy:0.0}; ya no existe={gone}; juntó {picked} bultos]");
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;
    var props = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>();
    SurvivalEnTren.Items.SalvageProp pallet = null, locker = null, mattress = null, panelProp = null, outsidePallet = null;
    foreach (var p in props)
    {
        if (p.StableId == "salv_estacion_pallet_001") pallet = p;
        else if (p.StableId == "salv_estacion_armario_001") locker = p;
        else if (p.StableId == "salv_estacion_colchon_001") mattress = p;
        else if (p.StableId == "salv_estacion_tablero_001") panelProp = p;
        else if (p.StableId == "salv_prueba_pallet_001") outsidePallet = p;
    }
    var stationCenter = locker.transform.parent.parent.position; // el marcador de la estación
    var east = stationCenter + new UnityEngine.Vector3(4f, 0f, 0f);
    var before = Units();
    log.Append($"Objetos desmontables en la escena: {props.Length}. Material suelto en bultos al empezar: {Show(before)}.");

    // 0) Sin barreta: no hay con qué. Con la sierra en la mano, el golpe rebota (la receta pide la barreta).
    string noTool = P(pallet);
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Crowbar));
    string afterOne = belt.ActiveTool.HasValue ? belt.ActiveTool.Value.ToString() : "manos libres";
    log.Append($" SIN BARRETA: \"{noTool}\", la tecla 1 deja {afterOne}.");
    gear.Give(SurvivalEnTren.Equipment.GearId.Saw);
    StandAndLook(CenterOf(pallet), east);
    belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Saw));
    for (int i = 0; i < 30; i++) yield return wait;
    int sawHits = belt.HitCount;
    belt.TrySwing();
    for (int i = 0; i < 45; i++) yield return wait;
    log.Append($" CON LA SIERRA: pegó {belt.HitCount - sawHits} vez, sirvió={belt.LastHitUseful} (debe rebotar), avance del pallet {pallet.Progress * 100f:0}%.");
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    belt.Select(-1);

    // 1) Pallet, colchón y tablero de la estación, uno por uno.
    yield return Salvage(pallet, east);
    log.Append($" → mochila: madera {backpack.CountOf(wood)} (el pallet da 4), pesa {backpack.MassKg:0.0} kg.");
    yield return Salvage(mattress, east);
    log.Append($" → mochila: tela {backpack.CountOf(cloth)} (el colchón da 3), pesa {backpack.MassKg:0.0} kg.");
    yield return Salvage(panelProp, stationCenter); // el tablero está contra la pared este: se lo mira desde adentro
    log.Append($" → mochila: componentes {backpack.CountOf(parts)} (el tablero da 2), pesa {backpack.MassKg:0.0} kg.");

    // 2) El armario (metal ×3) con la mochila casi llena: al juntar, lo que no entra queda en el piso.
    float free = backpack.CellCount - backpack.UsedCells;
    int metalBefore = backpack.CountOf(metal);
    yield return Salvage(locker, east);
    int bundleMetal = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == metal) bundleMetal += b.Count;
    log.Append($" ARMARIO con {free:0} casillas libres (una chapa pide un hueco de 2x2): en la mochila metal {backpack.CountOf(metal) - metalBefore} nuevas, en el piso {bundleMetal - before[1]} nuevas (+{before[1]} sueltas que ya había); mochila {backpack.UsedCells}/{backpack.CellCount} casillas, {backpack.MassKg:0.0} kg.");

    // 3) Ya no hay nada que desmontar de nuevo.
    int left = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>().Length;
    log.Append($" Quedan {left} objetos desmontables (eran {props.Length}: se llevaron 4 y falta el pallet de afuera).");

    // 4) Vaciar la mochila soltando todo (queda en bultos, sin perder nada) y romper el pallet de afuera con el clic izquierdo
    //    real mantenido; juntar lo que cae con la tecla E real.
    var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
    while (backpack.Grid.Items.Count > 0) panel.DropPiece(backpack.Grid.Items[0]);
    var outsideCenter = CenterOf(outsidePallet);
    int hitsNeeded = outsidePallet.Recipe.HitsToBreak;
    StandAndLook(outsideCenter, outsideCenter + new UnityEngine.Vector3(5f, 0f, 0f));
    if (belt.ActiveTool != SurvivalEnTren.Equipment.GearId.Crowbar) belt.Select(belt.SlotOf(SurvivalEnTren.Equipment.GearId.Crowbar)); // si ya está en la mano, apretar de nuevo la guardaría
    for (int i = 0; i < 40; i++) yield return wait;
    var alreadyOut = new System.Collections.Generic.HashSet<SurvivalEnTren.Items.MaterialBundle>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>());
    string promptOut = P(outsidePallet);
    int swingsOut = belt.Swings;
    MouseLeft(true);
    float held = 0f;
    while (outsidePallet != null && held < hitsNeeded * 1.2f + 3f) { yield return wait; held += UnityEngine.Time.fixedDeltaTime; }
    MouseLeft(false);
    for (int i = 0; i < 40; i++) yield return wait;
    bool outsideGone = outsidePallet == null;
    if (outsideGone) salvaged[0] += 4;
    int pickedReal = 0;
    var newOnes = new System.Collections.Generic.List<SurvivalEnTren.Items.MaterialBundle>();
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        if (!alreadyOut.Contains(b) && (b.transform.position - outsideCenter).sqrMagnitude < 9f) newOnes.Add(b);
    foreach (var b in newOnes)
    {
        var at = b.transform.position;
        StandAndLook(at, at + new UnityEngine.Vector3(1.5f, 0f, 0.3f));
        for (int i = 0; i < 20; i++) yield return wait;
        KeyE(true); yield return wait; yield return wait; KeyE(false);
        for (int i = 0; i < 10; i++) yield return wait;
        if (b == null) pickedReal++;
    }
    log.Append($" PALLET DE AFUERA con el clic izquierdo real mantenido: indicación \"{promptOut}\"; {belt.Swings - swingsOut} golpes (receta {hitsNeeded}) en {held:0.0} s; ya no existe={outsideGone}; juntó con E real {pickedReal} de {newOnes.Count} bultos; mochila madera {backpack.CountOf(wood)}, pesa {backpack.MassKg:0.0} kg.");

    // 5) Cuentas finales: lo que había + lo desmontado + lo que se puso a mano = lo que hay (mochila + bultos).
    var after = Units();
    var expected = new int[4];
    for (int k = 0; k < 4; k++) expected[k] = before[k] + salvaged[k] + extra[k];
    bool same = true; for (int k = 0; k < 4; k++) same &= after[k] == expected[k];
    log.Append($" CUENTAS: hay {Show(after)}; esperado (había + desmontado + puesto a mano) {Show(expected)}; sin duplicar ni perder={same}.");

    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    belt.Select(-1);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Desmontaje] " + log);
}
motor.StartCoroutine(Run());
return "prueba desmontaje iniciada";
