// Prueba del M8 paso 1: inventario por cuadrícula (piezas con tamaño en casillas, que entran, se giran o no entran),
// la lógica de la cuadrícula en sí (bordes, choques, mover, girar, agrandar) y el peso que frena el caminar.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/m7_mochila.cs" --timeout 60
// Resultado en la Console, líneas [Mochila].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
var trainBody = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>().GetComponent<UnityEngine.Rigidbody>();
var box = UnityEngine.GameObject.Find("Caja_Piso_Grande").GetComponent<SurvivalEnTren.Interaction.Carryable>(); // 10 kg
var wood = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var metal = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Tela.asset");
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Componentes.asset");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;
int changed = 0;
backpack.Changed += () => changed++;
string Sz(SurvivalEnTren.Items.ItemDefinition i) => $"{i.SizeCells.x}x{i.SizeCells.y}";

System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 30; i++) yield return wait;
    log.Append($"DATOS: tablón {Sz(wood)} {wood.WeightKg} kg, chapa {Sz(metal)} {metal.WeightKg} kg, rollo de tela {Sz(cloth)} {cloth.WeightKg} kg, componente {Sz(parts)} {parts.WeightKg} kg; íconos: {(wood.Icon != null)}, {(metal.Icon != null)}, {(cloth.Icon != null)}, {(parts.Icon != null)}; mochila {backpack.Grid.Width}x{backpack.Grid.Height} = {backpack.CellCount} casillas, vacía pesa {backpack.MassKg:0.0} kg, caminar x{motor.SpeedMultiplier:0.00}.");

    // 1) Llenar: 4 tablones (1x4) en vertical, 3 chapas (2x2) de las que solo entran 2, y los rollos de tela (1x2) que entran girados.
    int a = backpack.Add(wood, 4);
    int b = backpack.Add(metal, 3);
    int rotatedBefore = 0; foreach (var g in backpack.Grid.Items) if (g.Rotated) rotatedBefore++;
    int c = backpack.Add(cloth, 3);
    int rotatedCloth = 0; foreach (var g in backpack.Grid.Items) if (g.Item == cloth && g.Rotated) rotatedCloth++;
    log.Append($" LLENAR: tablones entraron {a} de 4; chapas {b} de 3 (la tercera no entra: no queda un hueco de 2x2); rollos de tela {c} de 3, girados {rotatedCloth} (en la fila de abajo solo caben acostados); usa {backpack.UsedCells} de {backpack.CellCount} casillas, pesa {backpack.MassKg:0.0} kg (esperado {4 * wood.WeightKg + 2 * metal.WeightKg + 3 * cloth.WeightKg:0.0}); Changed={changed}.");
    log.Append($" LLENA: caben más tablones={backpack.MaxAddable(wood)}, chapas={backpack.MaxAddable(metal)}, rollos={backpack.MaxAddable(cloth)}, componentes={backpack.MaxAddable(parts)}.");
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" Caminar con {backpack.MassKg:0.0} kg: x{motor.SpeedMultiplier:0.00} (esperado {UnityEngine.Mathf.Max(0.45f, 1f - 0.02f * backpack.MassKg):0.00}).");

    // 2) Sacar y volver a meter: mismas cantidades, y sacar de más no pasa nada.
    int r1 = backpack.Remove(metal, 1), r2 = backpack.Remove(metal, 5), r3 = backpack.Remove(parts, 3), r4 = backpack.Remove(parts, -2);
    log.Append($" SACAR chapa 1 / 5 / componentes 3 / -2: salieron {r1}, {r2}, {r3}, {r4}; quedan chapas {backpack.CountOf(metal)}, tablones {backpack.CountOf(wood)}.");
    int woodBefore = backpack.CountOf(wood), clothBefore = backpack.CountOf(cloth);
    float massBefore = backpack.MassKg;
    for (int round = 0; round < 5; round++)
    {
        int w = backpack.Remove(wood, 99), cl = backpack.Remove(cloth, 99);
        backpack.Add(wood, w); backpack.Add(cloth, cl);
    }
    log.Append($" CONSERVAR: 5 vueltas de sacar y volver a meter: tablones {backpack.CountOf(wood)} (antes {woodBefore}), rollos {backpack.CountOf(cloth)} (antes {clothBefore}), peso {backpack.MassKg:0.0} (antes {massBefore:0.0}).");
    while (backpack.Grid.Items.Count > 0) backpack.Grid.Remove(backpack.Grid.Items[0]);

    // 3) La lógica de la cuadrícula en sí, en una chica de 4x3 (como un estante precario).
    var shelf = new SurvivalEnTren.Items.ItemGrid(4, 3);
    bool longVertical = shelf.CanPlace(wood, 0, 0, false);           // 1x4 parado: alto 4 > 3
    bool longLying = shelf.CanPlace(wood, 0, 0, true);               // 4x1 acostado: entra
    bool outOfBounds = shelf.CanPlace(metal, 3, 0, false);           // 2x2 en la última columna
    var tiny = new SurvivalEnTren.Items.ItemGrid(3, 3);
    bool tinyAuto = tiny.TryAutoPlace(wood) != null;                 // 4 de largo en un lado de 3: no entra ni girado
    var p1 = shelf.TryAutoPlace(wood);                               // se acuesta solo en la fila de arriba
    var p2 = shelf.TryAutoPlace(metal);
    var overlap = shelf.CanPlace(cloth, p2.X, p2.Y, false);          // encima de la chapa
    var cloneCheck = shelf.Clone(); cloneCheck.TryAutoPlace(parts);
    int moved = 0;
    if (shelf.TryMove(p2, 2, 1, false)) moved++;                     // chapa a otro lugar libre
    bool moveBlocked = shelf.TryMove(p2, 1, 0, false);               // pisaría al tablón de arriba
    bool rotateOk = shelf.TryRotate(p1);                             // 4x1 -> 1x4 no entra en alto 3
    log.Append($" CUADRÍCULA 4x3: tablón 1x4 parado entra={longVertical}, acostado entra={longLying}; chapa 2x2 en la última columna entra={outOfBounds}; tablón en una de 3x3 entra={tinyAuto}; el tablón se acostó solo: girado={p1?.Rotated}, ocupa {p1?.Size.x}x{p1?.Size.y} en ({p1?.X},{p1?.Y}); chapa en ({p2?.X},{p2?.Y}); algo sobre la chapa entra={overlap}; mover la chapa a un lugar libre={moved == 1}, a uno ocupado={moveBlocked}; girar el tablón acostado={rotateOk}; copia independiente (la original sigue con {shelf.Items.Count} piezas, la copia con {cloneCheck.Items.Count}); casilla (0,0) tiene {shelf.ItemAt(0, 0)?.Item.PieceName}; agrandar a 3x3 (más chico)={shelf.Resize(3, 3)}, a 6x3={shelf.Resize(6, 3)}.");

    // 4) Peso en la mochila + caja de 10 kg en la mano: se suman para frenar el caminar.
    backpack.Add(wood, 2); // 4 kg... y 2 chapas más (7 kg) para ~11 kg
    backpack.Add(metal, 2);
    for (int i = 0; i < 10; i++) yield return wait;
    float onlyPack = motor.SpeedMultiplier, packKg = backpack.MassKg;
    motor.TeleportTo(box.transform.position + trainBody.rotation * new UnityEngine.Vector3(0f, 0.1f, 1.0f), trainBody);
    var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
    var dir = box.transform.position - (motor.transform.position + UnityEngine.Vector3.up * 1.6f);
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, 30f);
    for (int i = 0; i < 30; i++) yield return wait;
    box.Interact(who);
    for (int i = 0; i < 40; i++) yield return wait;
    float both = motor.SpeedMultiplier;
    float boxKg = box.Body.mass;
    carrier.Drop(false);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" PESO COMBINADO: mochila {packKg:0.0} kg x{onlyPack:0.00}; con la caja de {boxKg:0} kg en la mano x{both:0.00} (esperado {UnityEngine.Mathf.Max(0.45f, 1f - 0.02f * (packKg + boxKg)):0.00}); al soltar la caja x{motor.SpeedMultiplier:0.00}.");

    // 5) Muy cargada: el caminar llega al mínimo y se puede seguir.
    backpack.Add(metal, 99); backpack.Add(wood, 99); backpack.Add(cloth, 99); backpack.Add(parts, 99);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" MUY CARGADA ({backpack.MassKg:0.0} kg, {backpack.UsedCells} de {backpack.CellCount} casillas): caminar x{motor.SpeedMultiplier:0.00} (mínimo 0,45), movimiento bloqueado={motor.MovementLocked}.");
    while (backpack.Grid.Items.Count > 0) backpack.Grid.Remove(backpack.Grid.Items[0]);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" VACIADA: caminar x{motor.SpeedMultiplier:0.00}.");
    UnityEngine.Debug.Log("[Mochila] " + log);
}
motor.StartCoroutine(Run());
return "prueba mochila iniciada";
