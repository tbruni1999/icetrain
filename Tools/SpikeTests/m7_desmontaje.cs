// Prueba del M7 paso 4: desmontar los cuatro tipos de objeto con la barreta, pesos, límite de la mochila (lo que
// no entra queda en el piso), que no se pueda desmontar dos veces, y sin crear ni perder unidades.
// Los cuatro de la estación se desmontan con Channel() (lo mismo que hace el interactor cada cuadro con E
// apretado, apuntando de verdad); el pallet de afuera, con la tecla E virtual de verdad.
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

System.Collections.IEnumerator Salvage(SurvivalEnTren.Items.SalvageProp prop, UnityEngine.Vector3 from)
{
    var recipe = prop.Recipe;
    StandAndLook(CenterOf(prop), from);
    for (int i = 0; i < 30; i++) yield return wait;
    object t = Target();
    string prompt = P(prop);
    float t0 = 0f;
    while (prop != null && prop.Channel(who, UnityEngine.Time.fixedDeltaTime)) { yield return wait; t0 += UnityEngine.Time.fixedDeltaTime; }
    t0 += UnityEngine.Time.fixedDeltaTime;
    for (int i = 0; i < 10; i++) yield return wait;
    bool gone = prop == null;
    if (gone)
        foreach (var y in recipe.Yields)
            for (int k = 0; k < 4; k++) if (items[k] == y.item) salvaged[k] += y.count;
    log.Append($" [{recipe.DisplayName}: apuntando el objetivo es {Name(t)}; indicación \"{prompt}\"; tardó {t0:0.0} s (receta {recipe.SalvageSeconds:0.0}); ya no existe={gone}]");
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

    // 0) Sin barreta.
    string noTool = P(pallet);
    bool channelNoTool = pallet.Channel(who, 1f);
    log.Append($" SIN BARRETA: \"{noTool}\", avanza={channelNoTool}.");
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);

    // 1) Pallet, colchón y tablero de la estación, uno por uno.
    yield return Salvage(pallet, east);
    log.Append($" → mochila: madera {backpack.CountOf(wood)} (el pallet da 4), pesa {backpack.MassKg:0.0} kg.");
    yield return Salvage(mattress, east);
    log.Append($" → mochila: tela {backpack.CountOf(cloth)} (el colchón da 3), pesa {backpack.MassKg:0.0} kg.");
    yield return Salvage(panelProp, stationCenter); // el tablero está contra la pared este: se lo mira desde adentro
    log.Append($" → mochila: componentes {backpack.CountOf(parts)} (el tablero da 2), pesa {backpack.MassKg:0.0} kg.");

    // 2) El armario (metal ×3 = 10,5 kg) con la mochila casi llena: lo que no entra queda en el piso.
    // (la mochila ya tiene 24 casillas ocupadas por el pallet, el colchón y el tablero: del armario, las chapas 2x2 que entren
    //  entran y las que no, quedan en el piso)
    float free = backpack.CellCount - backpack.UsedCells;
    int metalBefore = backpack.CountOf(metal);
    yield return Salvage(locker, east);
    int bundleMetal = 0;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == metal) bundleMetal += b.Count;
    log.Append($" ARMARIO con {free:0} casillas libres (una chapa pide un hueco de 2x2): en la mochila metal {backpack.CountOf(metal) - metalBefore} nuevas, en el piso {bundleMetal - before[1]} nuevas (+{before[1]} sueltas que ya había); mochila {backpack.UsedCells}/{backpack.CellCount} casillas, {backpack.MassKg:0.0} kg.");

    // 3) Ya no hay nada que desmontar de nuevo.
    int left = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.SalvageProp>().Length;
    log.Append($" Quedan {left} objetos desmontables (eran {props.Length}: se llevaron 4 y falta el pallet de afuera).");

    // 4) Vaciar la mochila soltando todo (queda en bultos, sin perder nada) y desmontar el pallet de afuera con la tecla E real.
    var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
    while (backpack.Grid.Items.Count > 0) panel.DropPiece(backpack.Grid.Items[0]);
    var outsideCenter = CenterOf(outsidePallet);
    float seconds = outsidePallet.Recipe.SalvageSeconds;
    StandAndLook(outsideCenter, outsideCenter + new UnityEngine.Vector3(5f, 0f, 0f));
    for (int i = 0; i < 40; i++) yield return wait;
    object tOut = Target();
    string promptOut = P(outsidePallet);
    KeyE(true);
    float held = 0f;
    while (outsidePallet != null && held < seconds + 3f) { yield return wait; held += UnityEngine.Time.fixedDeltaTime; }
    KeyE(false);
    for (int i = 0; i < 10; i++) yield return wait;
    bool outsideGone = outsidePallet == null;
    if (outsideGone) salvaged[0] += 4;
    log.Append($" PALLET DE AFUERA con E real mantenido: objetivo {Name(tOut)}, indicación \"{promptOut}\"; terminó a los {held:0.0} s (receta {seconds:0.0} s), mochila madera {backpack.CountOf(wood)}, pesa {backpack.MassKg:0.0} kg, ya no existe={outsideGone}.");

    // 5) Cuentas finales: lo que había + lo desmontado + lo que se puso a mano = lo que hay (mochila + bultos).
    var after = Units();
    var expected = new int[4];
    for (int k = 0; k < 4; k++) expected[k] = before[k] + salvaged[k] + extra[k];
    bool same = true; for (int k = 0; k < 4; k++) same &= after[k] == expected[k];
    log.Append($" CUENTAS: hay {Show(after)}; esperado (había + desmontado + puesto a mano) {Show(expected)}; sin duplicar ni perder={same}.");

    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Desmontaje] " + log);
}
motor.StartCoroutine(Run());
return "prueba desmontaje iniciada";
