// Prueba del M7 paso 2: panel de la mochila (Tab, con el teclado virtual), soltar (bultos con física),
// recoger (todo, parte o nada según el peso), sin duplicar ni perder unidades.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m7_panel_y_bultos.cs" --timeout 60
// Resultado en la Console, líneas [Panel].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var controls = train.GetComponent<SurvivalEnTren.Train.TrainControls>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.BackpackPanel>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

// Teclado virtual (Unity sin foco: se deja que las teclas lleguen igual y se restaura al terminar).
var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = inputSettings.editorInputBehaviorInPlayMode;
var oldBackground = inputSettings.backgroundBehavior;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key key)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
    yield return wait; yield return wait; yield return wait;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    yield return wait; yield return wait; yield return wait;
}

// Cuenta unidades de cada material en la mochila y en los bultos del mundo (para comprobar que no se crea ni se pierde nada).
int Units(SurvivalEnTren.Items.ItemDefinition item)
{
    int total = backpack.CountOf(item);
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == item) total += b.Count;
    return total;
}
var given = new System.Collections.Generic.Dictionary<SurvivalEnTren.Items.ItemDefinition, int> { { wood, 0 }, { metal, 0 }, { cloth, 0 } };
int Give(SurvivalEnTren.Items.ItemDefinition item, int n) { int a = backpack.Add(item, n); given[item] += a; return a; }
string P(SurvivalEnTren.Interaction.IInteractable i) => i.GetPrompt(who) ?? "(sin indicación)";

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;
    // En el piso del tren, junto a la cama, para soltar cosas sobre el piso.
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.2f, 1.05f, 1.0f), trainBody);
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, trainBody.transform.eulerAngles.y));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, 20f);
    for (int i = 0; i < 40; i++) yield return wait;

    Give(wood, 4); Give(metal, 2); Give(cloth, 3);
    int w0 = Units(wood), m0 = Units(metal), c0 = Units(cloth);
    log.Append($"Inicio: mochila {backpack.MassKg:0.0} kg (madera {backpack.CountOf(wood)}, metal {backpack.CountOf(metal)}, tela {backpack.CountOf(cloth)}).");

    // 1) Abrir con Tab.
    bool lockedBefore = UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.Locked;
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    log.Append($" TAB: abierto={panel.IsOpen}, no se camina (bloqueado)={motor.MovementLocked}, cursor libre={UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.None} (antes fijo={lockedBefore}), interactor apagado={!who.enabled}.");

    // 2) Elegir la fila 2 (metal) con ↓ y soltar 1 con Enter; soltar todo con Supr.
    yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    int selectedRow = panel.Selected;
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    for (int i = 0; i < 20; i++) yield return wait;
    var bundlesAfterOne = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>();
    log.Append($" ↓ eligió la fila {selectedRow} (metal) y Enter soltó 1: mochila metal {backpack.CountOf(metal)} (era 2), bultos en el mundo {bundlesAfterOne.Length}.");
    yield return Tap(UnityEngine.InputSystem.Key.Delete);
    for (int i = 0; i < 20; i++) yield return wait;
    log.Append($" Supr soltó el resto: metal en la mochila {backpack.CountOf(metal)}, filas {backpack.Stacks.Count}, selección {panel.Selected}; unidades de metal en total {Units(metal)} (eran {m0}).");

    // 3) Cerrar con Tab: todo vuelve.
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    log.Append($" TAB de nuevo: abierto={panel.IsOpen}, bloqueo={motor.MovementLocked}, cursor fijo={UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.Locked}, interactor encendido={who.enabled}.");

    // 4) Los bultos físicos: caen al piso del tren, acompañan al tren y se ven con su peso.
    var bundles = UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>();
    SurvivalEnTren.Items.MaterialBundle metalBundle = null;
    foreach (var b in bundles) if (b.Item == metal) metalBundle = b;
    for (int i = 0; i < 60; i++) yield return wait;
    var local = trainBody.transform.InverseTransformPoint(metalBundle.transform.position);
    log.Append($" BULTO de metal ×{metalBundle.Count}: pesa {metalBundle.GetComponent<UnityEngine.Rigidbody>().mass:0.0} kg (esperado {metal.WeightKg * metalBundle.Count:0.0}), apoyado en el piso del tren (y local {local.y:0.00}, el piso está en 1,00), adentro del tren={System.Math.Abs(local.x) < 1.7f && System.Math.Abs(local.z) < 9.5f}.");

    // 5) Recoger: indicación y E. Todo, parte y nada.
    var woodStack = backpack.Stacks[0];
    panel.Drop(0, all: true); // soltar toda la madera (4 = 8 kg)
    for (int i = 0; i < 40; i++) yield return wait;
    SurvivalEnTren.Items.MaterialBundle woodBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == wood) woodBundle = b;
    string promptAll = P(woodBundle);
    woodBundle.Interact(who);
    for (int i = 0; i < 5; i++) yield return wait;
    log.Append($" RECOGER madera: indicación \"{promptAll}\"; después de E: madera en la mochila {backpack.CountOf(wood)}, el bulto desapareció={woodBundle == null}.");

    // Mochila casi llena: sobran 4 kg libres. Un bulto de 5 madera (10 kg) solo entra en parte (2 unidades).
    Give(metal, backpack.MaxAddable(metal));
    float freeAfterFill = backpack.FreeKg;
    int toDrop = backpack.CountOf(metal);
    panel.Drop(backpack.Stacks.Count - 1, all: false); // suelta 1 metal para hacer lugar
    var big = SurvivalEnTren.Items.MaterialBundle.Spawn(UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.BackpackSettings>("Assets/_Project/Data/Items/BackpackSettings.asset"), wood, 5, motor.transform.position + motor.transform.forward * 0.8f + UnityEngine.Vector3.up * 1.2f, UnityEngine.Vector3.zero);
    given[wood] += 5; // el bulto de 5 madera que se agrega al mundo
    for (int i = 0; i < 40; i++) yield return wait;
    float free = backpack.FreeKg;
    int fits = backpack.MaxAddable(wood);
    string promptPart = P(big);
    int woodBefore = backpack.CountOf(wood);
    big.Interact(who);
    for (int i = 0; i < 5; i++) yield return wait;
    log.Append($" PARCIAL: libres {free:0.0} kg (entran {fits} madera); indicación \"{promptPart}\"; después de E: entraron {backpack.CountOf(wood) - woodBefore}, el bulto quedó con ×{(big != null ? big.Count : 0)} y pesa {(big != null ? big.GetComponent<UnityEngine.Rigidbody>().mass : 0f):0.0} kg.");
    // Llenar del todo y probar "no entra".
    Give(wood, 99); Give(cloth, 99); Give(metal, 99);
    string promptFull = big != null ? P(big) : "(bulto agotado)";
    int before = backpack.CountOf(wood);
    if (big != null) big.Interact(who);
    log.Append($" LLENA ({backpack.MassKg:0.0} / {backpack.CapacityKg:0} kg): indicación \"{promptFull}\"; E no cambia nada={backpack.CountOf(wood) == before}.");

    // 6) Conservación total: nada se creó ni se perdió moviendo unidades entre mochila y mundo.
    log.Append($" CONSERVACIÓN: madera {Units(wood)} unidades (se dieron {given[wood]}), metal {Units(metal)} ({given[metal]}), tela {Units(cloth)} ({given[cloth]}); todo igual={Units(wood) == given[wood] && Units(metal) == given[metal] && Units(cloth) == given[cloth]}.");

    // 7) Soltar con una pared adelante: el bulto no queda dentro de la pared.
    while (backpack.Stacks.Count > 0) backpack.Remove(backpack.Stacks[0].Item, 999);
    backpack.Add(cloth, 2);
    var wallPoint = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 5.5f); // frente a la pared del fondo de la cabina (z = 9,4)
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.0f, 1.05f, 8.2f), trainBody);
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, trainBody.transform.eulerAngles.y));
    for (int i = 0; i < 30; i++) yield return wait;
    panel.Drop(0, all: false);
    for (int i = 0; i < 40; i++) yield return wait;
    SurvivalEnTren.Items.MaterialBundle clothBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == cloth && b.transform.position.y > 0f) clothBundle = b;
    var cl = trainBody.transform.InverseTransformPoint(clothBundle.transform.position);
    log.Append($" SOLTAR CERCA DE UNA PARED (jugador a z=8,2, pared en 9,4): el bulto quedó en z={cl.z:0.00} (adentro del tren si < 9,4)={cl.z < 9.4f}.");

    // 8) Soltar con el tren en marcha: el bulto acompaña al tren.
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 3) controls.MovePowerBrake(+1);
    for (int i = 0; i < 700; i++) yield return wait; // ~14 s, ~25 km/h
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.0f, 1.05f, -2.0f), trainBody);
    for (int i = 0; i < 30; i++) yield return wait;
    backpack.Add(wood, 1);
    int units = panel.Drop(backpack.Stacks.Count - 1, all: false);
    for (int i = 0; i < 100; i++) yield return wait;
    SurvivalEnTren.Items.MaterialBundle runBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == wood && b.Count == 1) runBundle = b;
    var rb = runBundle.GetComponent<UnityEngine.Rigidbody>();
    float relSpeed = (rb.linearVelocity - trainBody.GetPointVelocity(rb.worldCenterOfMass)).magnitude;
    var rl = trainBody.transform.InverseTransformPoint(runBundle.transform.position);
    log.Append($" SOLTAR CON EL TREN A {train.SpeedMetersPerSecond * 3.6f:0} km/h: soltó {units}; el bulto va a {relSpeed:0.00} m/s respecto del tren y está adentro={System.Math.Abs(rl.x) < 1.7f && rl.z < 9.4f && rl.z > -7.5f}.");
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);

    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Panel] " + log);
}
motor.StartCoroutine(Run());
return "prueba panel iniciada";
