// Prueba de G1 paso 1 (2026-10-17): las pilas en la cuadrícula (DECISIÓN de Tomi: "cuadrícula con pilas").
// Mira: los materiales nuevos (datos e iconos), que la mochila apila, pasar una pila al estante (completa la que hay y abre otra),
// soltar una pila sobre otra del mismo material (se suman), tirar una pila al piso (un bulto con todas) y volver a juntarla, el
// peso, y guardar y cargar con pilas (las cantidades vuelven iguales).
// La prueba corre en un objeto que sobrevive a la recarga de la escena (CoroutineHost).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/g1_pilas.cs" --timeout 60
// Resultado en la Console, línea [Pilas].

var hostObject = new UnityEngine.GameObject("Prueba_Pilas");
UnityEngine.Object.DontDestroyOnLoad(hostObject);
var host = hostObject.AddComponent<SurvivalEnTren.Core.CoroutineHost>();
var items = "Assets/_Project/Data/Items/";
SurvivalEnTren.Items.ItemDefinition Item(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>(items + n + ".asset");
var wood = Item("Madera"); var metal = Item("Metal"); var stone = Item("Piedra"); var ore = Item("MineralHierro");
var coal = Item("Carbon"); var scrap = Item("Chatarra"); var cable = Item("Cable"); var mech = Item("PiezasMecanicas");
var log = new System.Text.StringBuilder();
UnityEngine.Application.runInBackground = true;

SurvivalEnTren.Items.Backpack backpack = null;
SurvivalEnTren.Items.InventoryPanel panel = null;
SurvivalEnTren.Items.StorageContainer shelf = null;
SurvivalEnTren.Interaction.PlayerInteractor who = null;
SurvivalEnTren.Player.PlayerMotor motor = null;
SurvivalEnTren.Save.SaveSystem saver = null;
void Refs()
{
    backpack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.Backpack>();
    panel = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Items.InventoryPanel>();
    who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
    motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
    saver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Save.SaveSystem>();
    saver.FileName = "prueba_pilas.json"; // no pisar la partida del jugador
    shelf = null;
    foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.StorageContainer>()) if (s.StableId == "storage_tren_estante_precario_001") shelf = s;
}
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
string Stacks(SurvivalEnTren.Items.ItemGrid grid, SurvivalEnTren.Items.ItemDefinition item)
{
    var parts = new System.Collections.Generic.List<string>();
    foreach (var g in grid.Items) if (g.Item == item) parts.Add(g.Count.ToString());
    return parts.Count == 0 ? "-" : string.Join("+", parts);
}
string Show(SurvivalEnTren.Items.ItemGrid grid)
{
    var parts = new System.Collections.Generic.List<string>();
    foreach (var g in grid.Items) parts.Add($"{g.Item.Id}@{g.X},{g.Y}{(g.Rotated ? "r" : "")}x{g.Count}");
    parts.Sort();
    return string.Join(" ", parts);
}
void Empty(SurvivalEnTren.Items.ItemGrid grid) { while (grid.Items.Count > 0) grid.Remove(grid.Items[0]); }

System.Collections.IEnumerator Run()
{
    Refs();
    yield return Frames(20);

    // 1) Los materiales nuevos: datos e iconos.
    var all = new[] { wood, metal, stone, ore, coal, scrap, cable, mech };
    var data = new System.Collections.Generic.List<string>();
    foreach (var i in all) data.Add($"{i.DisplayName} {i.WeightKg:0.0} kg {i.SizeCells.x}x{i.SizeCells.y} pila {i.MaxStack}{(i.Icon == null ? " SIN ICONO" : "")}");
    log.Append($"MATERIALES: {string.Join("; ", data)}.");

    // 2) La mochila apila: 40 piedras = 25 + 15; 5 de carbón = una pila; distintos materiales no se mezclan.
    Empty(backpack.Grid);
    int s40 = backpack.Add(stone, 40), c5 = backpack.Add(coal, 5), o3 = backpack.Add(ore, 3);
    float mass = backpack.MassKg;
    log.Append($" MOCHILA: piedras {s40} de 40 en {Stacks(backpack.Grid, stone)} (esperado 25+15); carbón {c5} en {Stacks(backpack.Grid, coal)}; mineral {o3} en {Stacks(backpack.Grid, ore)}; usa {backpack.UsedCells} casillas (esperado 4); pesa {mass:0.0} kg (esperado {40 * stone.WeightKg + 5 * coal.WeightKg + 3 * ore.WeightKg:0.0}).");

    // 3) Pasar al estante: la pila de 15 piedras va al estante; después la de 25 completa la que hay (hasta 25) y abre otra.
    Empty(shelf.Grid);
    panel.Open(shelf.Grid, "ESTANTE PRECARIO");
    yield return Frames(3);
    SurvivalEnTren.Items.GridItem small = null, big = null;
    foreach (var g in backpack.Grid.Items) if (g.Item == stone) { if (g.Count == 15) small = g; else big = g; }
    bool m1 = panel.TryMovePiece(small, backpack.Grid, shelf.Grid, 0, 0, false);
    var onShelf = shelf.Grid.ItemAt(0, 0);
    bool m2 = panel.TryMovePiece(big, backpack.Grid, shelf.Grid, 0, 0, false); // encima de la pila de 15: se suman 10, quedan 15
    log.Append($" AL ESTANTE: pila de 15 al estante={m1}; soltar la de 25 encima={m2}: en el estante {Stacks(shelf.Grid, stone)} (esperado 25), en la mochila {Stacks(backpack.Grid, stone)} (esperado 15); total {shelf.Grid.CountOf(stone) + backpack.CountOf(stone)} (esperado 40).");

    // 4) Soltar una pila sobre otra en la misma cuadrícula: 2 pilas de chapa (10 + 3) en la mochila; la de 3 sobre la de 10 no
    //    entra (llena); al revés, la de 10 sobre la de 3 completa hasta 10 y deja 3.
    Empty(backpack.Grid);
    backpack.Add(metal, 13);
    SurvivalEnTren.Items.GridItem full = null, partial = null;
    foreach (var g in backpack.Grid.Items) if (g.Item == metal) { if (g.Count == metal.MaxStack) full = g; else partial = g; }
    bool intoFull = panel.TryMovePiece(partial, backpack.Grid, backpack.Grid, full.X, full.Y, false);
    bool fullOnPartial = panel.TryMovePiece(full, backpack.Grid, backpack.Grid, partial.X, partial.Y, false);
    log.Append($" SOBRE OTRA PILA: la de 3 sobre la llena entró={intoFull} (debe ser False); la llena sobre la de 3={fullOnPartial}: chapas {Stacks(backpack.Grid, metal)} (esperado 3+10 o 10+3), total {backpack.CountOf(metal)} (esperado 13).");
    panel.Close();

    // 5) Tirar una pila al piso: un bulto con todas; juntarlo de nuevo con E.
    Empty(backpack.Grid);
    backpack.Add(cable, 12);
    var cablePile = backpack.Grid.Items[0];
    var before = new System.Collections.Generic.HashSet<SurvivalEnTren.Items.MaterialBundle>(UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>());
    bool dropped = panel.DropPiece(cablePile);
    yield return Frames(60);
    SurvivalEnTren.Items.MaterialBundle fresh = null;
    foreach (var b in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Items.MaterialBundle>()) if (!before.Contains(b)) fresh = b;
    int inBundle = fresh != null ? fresh.Count : 0;
    if (fresh != null) fresh.Interact(who);
    yield return Frames(5);
    log.Append($" AL PISO: soltó={dropped}, un bulto nuevo con {inBundle} cables (esperado 12); al juntarlo con E, en la mochila {backpack.CountOf(cable)} en {Stacks(backpack.Grid, cable)} (esperado 12), el bulto ya no está={fresh == null}.");

    // 6) Guardar y cargar con pilas: mochila y estante con varias pilas a medias; después de cargar, iguales.
    Empty(backpack.Grid); Empty(shelf.Grid);
    backpack.Add(stone, 31); backpack.Add(scrap, 7); backpack.Add(mech, 3); backpack.Add(wood, 26);
    shelf.Grid.AddUnits(coal, 33); shelf.Grid.AddUnits(cable, 4);
    string packBefore = Show(backpack.Grid), shelfBefore = Show(shelf.Grid);
    bool saved = saver.Save();
    Empty(backpack.Grid); Empty(shelf.Grid); backpack.Add(ore, 2);
    int loads = SurvivalEnTren.Save.SaveSystem.LoadsApplied;
    bool loading = saver.Load();
    int guard = 0;
    while (SurvivalEnTren.Save.SaveSystem.LoadsApplied <= loads && guard++ < 600) yield return null;
    yield return Frames(30);
    Refs();
    string packAfter = Show(backpack.Grid), shelfAfter = Show(shelf.Grid);
    log.Append($" GUARDAR Y CARGAR: guardó={saved}, cargó={loading}; mochila {(packAfter == packBefore ? "IGUAL" : "DISTINTA: antes " + packBefore + " / ahora " + packAfter)}; estante {(shelfAfter == shelfBefore ? "IGUAL" : "DISTINTO: antes " + shelfBefore + " / ahora " + shelfAfter)} ({shelfAfter}).");

    Empty(backpack.Grid); Empty(shelf.Grid);
    UnityEngine.Debug.Log("[Pilas] " + log);
    UnityEngine.Object.Destroy(hostObject);
}
host.StartCoroutine(Run());
return "prueba de pilas iniciada";
