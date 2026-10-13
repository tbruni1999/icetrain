// Prueba del M7 paso 1: mochila con peso (límite, cuentas sin duplicar ni perder, peso que frena el caminar
// junto con lo que se lleva en la mano).
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
var load = (SurvivalEnTren.Items.ItemDefinition)UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivalEnTren.Items.ItemDefinition>("Assets/_Project/Data/Items/Madera.asset");
var wood = load;
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

System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 30; i++) yield return wait;
    log.Append($"Datos: madera {wood.WeightKg} kg, metal {metal.WeightKg} kg, tela {cloth.WeightKg} kg, componentes {parts.WeightKg} kg; mochila vacía {backpack.MassKg:0.0} / {backpack.CapacityKg:0} kg, caminar x{motor.SpeedMultiplier:0.00}.");

    // 1) Guardar y contar.
    int a = backpack.Add(wood, 4), b = backpack.Add(metal, 3), c = backpack.Add(cloth, 3), d = backpack.Add(parts, 2);
    log.Append($" GUARDAR madera 4, metal 3, tela 3, componentes 2: entraron {a}, {b}, {c}, {d}; pesa {backpack.MassKg:0.00} kg (esperado {4 * wood.WeightKg + 3 * metal.WeightKg + 3 * cloth.WeightKg + 2 * parts.WeightKg:0.00}); libre {backpack.FreeKg:0.00} kg; Changed={changed}.");
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" Caminar con {backpack.MassKg:0.0} kg en la mochila: x{motor.SpeedMultiplier:0.00} (esperado {UnityEngine.Mathf.Max(0.45f, 1f - 0.02f * backpack.MassKg):0.00}).");

    // 2) El límite de peso: no entra más de lo que se puede, y lo que no entra no se pierde (se devuelve).
    int canWood = backpack.MaxAddable(wood);
    int extra = backpack.Add(wood, 100);
    log.Append($" LÍMITE: entraban {canWood} madera más; al pedir 100 entraron {extra}; ahora {backpack.MassKg:0.00} / {backpack.CapacityKg:0} kg; madera {backpack.CountOf(wood)}; otra más entra={backpack.Add(wood, 1)}.");

    // 3) Sacar: no se puede sacar más de lo que hay.
    int r1 = backpack.Remove(metal, 2), r2 = backpack.Remove(metal, 5), r3 = backpack.Remove(parts, 0), r4 = backpack.Remove(parts, -3);
    log.Append($" SACAR metal 2 / 5 / componentes 0 / -3: salieron {r1}, {r2}, {r3}, {r4}; quedan metal {backpack.CountOf(metal)}, componentes {backpack.CountOf(parts)}.");

    // 4) Conservar: mover todo afuera y adentro varias veces no cambia el total.
    float before = backpack.MassKg;
    var counts = new int[] { backpack.CountOf(wood), backpack.CountOf(metal), backpack.CountOf(cloth), backpack.CountOf(parts) };
    var items = new[] { wood, metal, cloth, parts };
    for (int round = 0; round < 5; round++)
    {
        var out1 = new int[4];
        for (int k = 0; k < 4; k++) out1[k] = backpack.Remove(items[k], 999);
        float empty = backpack.MassKg;
        for (int k = 0; k < 4; k++) backpack.Add(items[k], out1[k]);
        if (round == 0) log.Append($" CONSERVAR: vaciada pesa {empty:0.00} kg");
    }
    bool same = true; for (int k = 0; k < 4; k++) same &= backpack.CountOf(items[k]) == counts[k];
    log.Append($"; después de 5 vueltas de sacar todo y volver a meter: mismas cantidades={same}, peso {backpack.MassKg:0.00} kg (antes {before:0.00}).");

    // 5) Peso en la mochila + caja de 10 kg en la mano: se suman para frenar el caminar.
    backpack.Remove(wood, 999); backpack.Remove(metal, 999); backpack.Remove(cloth, 999); backpack.Remove(parts, 999);
    backpack.Add(wood, 5); // 10 kg
    for (int i = 0; i < 10; i++) yield return wait;
    float onlyPack = motor.SpeedMultiplier;
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
    float afterDrop = motor.SpeedMultiplier;
    log.Append($" PESO COMBINADO: solo mochila 10 kg x{onlyPack:0.00}; mochila 10 kg + caja {boxKg:0} kg en la mano x{both:0.00} (esperado {UnityEngine.Mathf.Max(0.45f, 1f - 0.02f * (10f + boxKg)):0.00}); al soltar la caja x{afterDrop:0.00}.");

    // 6) Con la mochila llena el caminar llega al mínimo y se puede seguir (no se bloquea).
    backpack.Remove(wood, 999); backpack.Add(metal, 99);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" MOCHILA LLENA ({backpack.MassKg:0.0} kg): caminar x{motor.SpeedMultiplier:0.00} (mínimo 0,45), movimiento bloqueado={motor.MovementLocked}.");
    backpack.Remove(metal, 999);
    for (int i = 0; i < 10; i++) yield return wait;
    log.Append($" VACIADA: caminar x{motor.SpeedMultiplier:0.00}.");
    UnityEngine.Debug.Log("[Mochila] " + log);
}
motor.StartCoroutine(Run());
return "prueba mochila iniciada";
