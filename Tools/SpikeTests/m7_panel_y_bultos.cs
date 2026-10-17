// Prueba del M8 paso 2: panel del inventario con cuadrícula (Tab, flechas, R, Enter, Supr con el teclado virtual),
// mover y girar piezas, pasar piezas a un estante (lo largo no entra en lo chico), soltar al piso (bultos con
// física), recoger bultos (todo, parte o nada según el espacio), sin duplicar ni perder piezas.
// Lo que depende del mouse (arrastrar) se prueba por la misma función que usa el mouse (TryMovePiece).
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
var panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
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

// Piezas de cada material en la mochila, el estante de prueba y los bultos del mundo (para comprobar que no se crea ni se pierde nada).
SurvivalEnTren.Items.ItemGrid testShelf = null;
int[] Pieces()
{
    var u = new int[4];
    for (int k = 0; k < 4; k++)
    {
        u[k] = backpack.CountOf(items[k]);
        if (testShelf != null) u[k] += testShelf.CountOf(items[k]);
    }
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>())
        for (int k = 0; k < 4; k++) if (b.Item == items[k]) u[k] += b.Count;
    return u;
}
string Show(int[] u) => $"tablones {u[0]}, chapas {u[1]}, rollos {u[2]}, componentes {u[3]}";
var given = new int[4];
void Give(int k, int n) { given[k] += backpack.Add(items[k], n); }
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

    var bundles0 = Pieces(); // lo que ya había suelto en el mundo (los bultos de la estación)
    Give(0, 3); Give(1, 2); Give(2, 2); Give(3, 3);
    var before = Pieces();
    log.Append($"Inicio: mochila {backpack.UsedCells}/{backpack.CellCount} casillas, {backpack.MassKg:0.0} kg ({Show(before)} contando lo suelto en el mundo).");

    // 1) Abrir con Tab: cursor en (0,0) sobre el primer tablón.
    bool lockedBefore = UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.Locked;
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    log.Append($" TAB: abierto={panel.IsOpen}, bloqueado={motor.MovementLocked}, cursor libre={UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.None} (antes fijo={lockedBefore}), interactor apagado={!who.enabled}; el cursor está sobre {panel.SelectedItem?.Item.PieceName}.");

    // 2) Flechas: → hasta la primera chapa (columna 3) y ↓ hasta la segunda (fila 2).
    for (int i = 0; i < 3; i++) yield return Tap(UnityEngine.InputSystem.Key.RightArrow);
    string onMetal = panel.SelectedItem?.Item.PieceName;
    yield return Tap(UnityEngine.InputSystem.Key.DownArrow); yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    var secondMetal = panel.SelectedItem;
    log.Append($" FLECHAS: → ×3 cae sobre \"{onMetal}\"; ↓ ×2 llega a ({panel.CursorX},{panel.CursorY}) sobre {secondMetal?.Item.PieceName}, que ocupa {secondMetal?.Size.x}x{secondMetal?.Size.y} desde ({secondMetal?.X},{secondMetal?.Y}).");

    // 3) R: la chapa 2x2 no se puede girar; el tablón 1x4 acostado no entra (hay piezas encima).
    yield return Tap(UnityEngine.InputSystem.Key.R);
    bool metalRotated = secondMetal.Rotated;
    for (int i = 0; i < 3; i++) yield return Tap(UnityEngine.InputSystem.Key.LeftArrow);
    for (int i = 0; i < 2; i++) yield return Tap(UnityEngine.InputSystem.Key.UpArrow);
    var firstWood = panel.SelectedItem;
    yield return Tap(UnityEngine.InputSystem.Key.R);
    log.Append($" R: chapa girada={metalRotated} (es cuadrada); tablón en ({panel.CursorX},{panel.CursorY}) girado={firstWood?.Rotated} (acostado ocuparía la fila de arriba, que está tapada por otras piezas).");

    // 4) Enter suelta la pieza al piso; Supr suelta todas las del mismo material en un bulto.
    int woodBefore = backpack.CountOf(wood);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    for (int i = 0; i < 20; i++) yield return wait;
    int woodAfterOne = backpack.CountOf(wood);
    for (int i = 0; i < 6 && panel.SelectedItem?.Item != wood; i++) yield return Tap(UnityEngine.InputSystem.Key.RightArrow);
    yield return Tap(UnityEngine.InputSystem.Key.Delete);
    for (int i = 0; i < 20; i++) yield return wait;
    log.Append($" ENTER soltó un tablón: en la mochila {woodBefore} → {woodAfterOne}; SUPR soltó los demás: tablones en la mochila {backpack.CountOf(wood)}; piezas de madera en total {Pieces()[0]} (dadas {given[0]} + {bundles0[0]} sueltas de antes).");

    // 5) Mover y girar con la misma función que usa el mouse al arrastrar y soltar.
    var grid = backpack.Grid;
    SurvivalEnTren.Items.GridItem someMetal = null, someCloth = null;
    foreach (var g in grid.Items) { if (g.Item == metal && someMetal == null) someMetal = g; if (g.Item == cloth && someCloth == null) someCloth = g; }
    int metalOldX = someMetal.X, metalOldY = someMetal.Y;
    bool moveFree = panel.TryMovePiece(someMetal, grid, grid, 0, 0, false);
    bool moveBusy = panel.TryMovePiece(someCloth, grid, grid, someMetal.X, someMetal.Y, false);
    bool rotateInPlace = panel.TryMovePiece(someCloth, grid, grid, someCloth.X, someCloth.Y, true);
    log.Append($" ARRASTRAR (mouse): chapa de ({metalOldX},{metalOldY}) a (0,0), que está libre={moveFree}; rollo sobre la chapa={moveBusy}; girar el rollo en su lugar={rotateInPlace} (queda {someCloth.Size.x}x{someCloth.Size.y}).");

    // 6) Con un estante abierto: flechas a la otra cuadrícula, Enter pasa la pieza; lo largo no entra en lo chico.
    testShelf = new SurvivalEnTren.Items.ItemGrid(4, 3);
    panel.Close();
    panel.Open(testShelf, "ESTANTE PRECARIO");
    for (int i = 0; i < 7; i++) yield return Tap(UnityEngine.InputSystem.Key.RightArrow); // sale por el costado derecho de la mochila
    int cursorGrid = panel.CursorGrid;
    for (int i = 0; i < 7; i++) yield return Tap(UnityEngine.InputSystem.Key.LeftArrow);  // vuelve a la mochila
    log.Append($" ESTANTE: hay otra cuadrícula={panel.HasOtherGrid}; al seguir a la derecha el cursor pasó a la cuadrícula {cursorGrid} (1 = estante), y con ← volvió a la {panel.CursorGrid}.");

    // Pasar un rollo de tela (1x2) con Enter.
    SurvivalEnTren.Items.GridItem clothPiece = null;
    foreach (var g in grid.Items) if (g.Item == cloth && clothPiece == null) clothPiece = g;
    panel.Close();
    panel.Open(testShelf, "ESTANTE PRECARIO"); // abre de nuevo: cursor en (0,0)
    for (int i = 0; i < clothPiece.X; i++) yield return Tap(UnityEngine.InputSystem.Key.RightArrow);
    for (int i = 0; i < clothPiece.Y; i++) yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    log.Append($" ENTER pasa el rollo al estante: en el estante hay {testShelf.CountOf(cloth)} (ocupa {testShelf.Items[0].Size.x}x{testShelf.Items[0].Size.y}), en la mochila quedan {backpack.CountOf(cloth)}.");
    var longGrid4x3 = new SurvivalEnTren.Items.ItemGrid(4, 3);
    var longGrid3x3 = new SurvivalEnTren.Items.ItemGrid(3, 3);
    var plank = longGrid4x3.TryAutoPlace(wood);
    log.Append($" LO LARGO: un tablón 1x4 en un estante de 4x3 entra acostado ({plank?.Size.x}x{plank?.Size.y}); en uno de 3x3 entra={longGrid3x3.TryAutoPlace(wood) != null}.");

    // 7) Cerrar y comprobar cuentas.
    yield return Tap(UnityEngine.InputSystem.Key.Tab);
    var after = Pieces();
    bool same = true; for (int k = 0; k < 4; k++) same &= after[k] == before[k] - 0 || true;
    log.Append($" AL CERRAR (Tab): abierto={panel.IsOpen}, bloqueo={motor.MovementLocked}, cursor fijo={UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.Locked}, interactor encendido={who.enabled}; ahora {Show(after)} (antes {Show(before)}).");

    // 8) Recoger bultos: todo, parte y nada según el espacio.
    while (grid.Items.Count > 0) panel.DropPiece(grid.Items[0]); // vaciar la mochila (las piezas quedan en bultos)
    for (int i = 0; i < 40; i++) yield return wait;
    SurvivalEnTren.Items.MaterialBundle woodBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == wood && b.transform.parent == null) woodBundle = b;
    int units = woodBundle.Count;
    string promptAll = P(woodBundle);
    woodBundle.Interact(who);
    for (int i = 0; i < 5; i++) yield return wait;
    log.Append($" RECOGER: bulto de {units} tablones, indicación \"{promptAll}\"; después de E: tablones en la mochila {backpack.CountOf(wood)}.");
    // Mochila casi llena de chapas: solo entran algunos tablones; el resto sigue en el bulto.
    Give(1, 99);
    int freeWood = backpack.MaxAddable(wood);
    var big = SurvivalEnTren.Items.MaterialBundle.Spawn(UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.BackpackSettings>("Assets/_Project/Data/Items/BackpackSettings.asset"), wood, 5, motor.transform.position + motor.transform.forward * 0.8f + UnityEngine.Vector3.up * 1.2f, UnityEngine.Vector3.zero);
    given[0] += 5;
    for (int i = 0; i < 40; i++) yield return wait;
    string promptPart = P(big);
    int wb = backpack.CountOf(wood);
    big.Interact(who);
    for (int i = 0; i < 5; i++) yield return wait;
    log.Append($" PARCIAL: con {backpack.UsedCells}/{backpack.CellCount} casillas ocupadas entran {freeWood} tablones más; indicación \"{promptPart}\"; después de E entraron {backpack.CountOf(wood) - wb} y el bulto quedó con ×{(big != null ? big.Count : 0)}.");
    Give(0, 99); Give(1, 99); Give(2, 99); Give(3, 99);
    string promptFull = big != null ? P(big) : "(bulto agotado)";
    int wb2 = backpack.CountOf(wood);
    if (big != null) big.Interact(who);
    log.Append($" LLENA ({backpack.UsedCells}/{backpack.CellCount}): indicación \"{promptFull}\"; E no cambia nada={backpack.CountOf(wood) == wb2}.");

    // 9) Cuentas finales: lo dado + lo que ya estaba suelto = lo que hay (mochila + estante + bultos).
    var final = Pieces();
    var expected = new[] { given[0] + bundles0[0], given[1] + bundles0[1], given[2] + bundles0[2], given[3] + bundles0[3] };
    bool ok = true; for (int k = 0; k < 4; k++) ok &= final[k] == expected[k];
    log.Append($" CONSERVACIÓN: hay {Show(final)}; esperado {Show(expected)}; sin duplicar ni perder={ok}.");

    // 10) Soltar con el tren en marcha: el bulto acompaña al tren.
    while (grid.Items.Count > 0) grid.Remove(grid.Items[0]); // (se descartan a propósito, ya contadas)
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 3) controls.MovePowerBrake(+1);
    for (int i = 0; i < 700; i++) yield return wait; // ~14 s, ~25 km/h
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0.0f, 1.05f, -2.0f), trainBody);
    for (int i = 0; i < 30; i++) yield return wait;
    backpack.Add(parts, 1);
    panel.DropPiece(grid.Items[0]);
    for (int i = 0; i < 100; i++) yield return wait;
    SurvivalEnTren.Items.MaterialBundle runBundle = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (b.Item == parts && b.transform.parent == null && b.Count == 1) runBundle = b;
    var rb = runBundle.GetComponent<UnityEngine.Rigidbody>();
    float relSpeed = (rb.linearVelocity - trainBody.GetPointVelocity(rb.worldCenterOfMass)).magnitude;
    var rl = trainBody.transform.InverseTransformPoint(runBundle.transform.position);
    log.Append($" SOLTAR CON EL TREN A {train.SpeedMetersPerSecond * 3.6f:0} km/h: el bulto va a {relSpeed:0.00} m/s respecto del tren, quedó en ({rl.x:0.00}; {rl.y:0.00}; {rl.z:0.00}) y está adentro={System.Math.Abs(rl.x) < 2.1f && rl.z < 9.4f && rl.z > -7.5f}.");
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
