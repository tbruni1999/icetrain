// Prueba de los brazos en primera persona y del golpe con peso (pedido de Tomi del 2026-10-16). Con las manos libres no se ven;
// con la barreta (con una mano desde el 2026-10-16) y el martillo, una mano en el mango, con el mango derecho en el puño y la
// muñeca apenas doblada; el balanceo va al ritmo de los pasos (caminando y corriendo); cargando una caja, las dos abiertas; según el traje cambian
// las mangas y los guantes; la cámara principal no dibuja las manos (las dibuja la suya, encima); un golpe contra la pared empuja
// la vista y vuelve; las manos se balancean al caminar y se quedan un poco atrás al girar.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/h7_brazos.cs" --timeout 60
// Resultado en la Console, línea [Brazos].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var body = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var belt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var arms = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.FirstPersonArms>();
var rig = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ViewmodelRig>();
var head = motor.transform.Find("Cabeza");
var mainCam = head.GetComponent<UnityEngine.Camera>();
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
int handsLayer = UnityEngine.LayerMask.NameToLayer("Manos");
int ActiveHands() { int n = 0; foreach (var t in rig.Root.GetComponentsInChildren<UnityEngine.Transform>()) if (t.name.StartsWith("Mano_") && t.parent.name.StartsWith("Brazo_")) n++; return n; }
string ActiveSleeves() { var s = new System.Collections.Generic.SortedSet<string>(); foreach (var t in rig.Root.GetComponentsInChildren<UnityEngine.Transform>()) if (t.parent.name.StartsWith("Brazo_") && (t.name.StartsWith("Antebrazo_") || t.name.StartsWith("Mano_"))) s.Add(t.name); return string.Join(",", s); }
float GripGap(string grip)
{
    var model = rig.Root.Find(belt.Selected == 0 ? "Mano_Barreta" : "Mano_Martillo");
    UnityEngine.Transform g = null;
    foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>()) if (t.name == grip) g = t;
    float best = 99f;
    foreach (var t in rig.Root.GetComponentsInChildren<UnityEngine.Transform>())
        if (t.name.StartsWith("Agarre") && t.parent.name.StartsWith("Mano_") && t.parent.parent.name.StartsWith("Brazo_")) best = UnityEngine.Mathf.Min(best, UnityEngine.Vector3.Distance(t.position, g.position));
    return best * 100f;
}
// El agarre derecho (Tomi, 2026-10-16: "sigue agarrando re chueca la barreta"): el mango tiene que cruzar el puño por su túnel
// (la línea Agarre -> Agarre_Pulgar de la mano, en la misma dirección que el mango de la herramienta) y la muñeca, apenas doblada.
string Straight()
{
    var model = rig.Root.Find(belt.Selected == 0 ? "Mano_Barreta" : "Mano_Martillo");
    UnityEngine.Transform toolGrip = null, hand = null, fore = null, ag = null, pu = null;
    foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>()) if (t.name == "Agarre_Derecha") toolGrip = t;
    foreach (var t in rig.Root.Find("Brazo_Derecho").GetComponentsInChildren<UnityEngine.Transform>())
    {
        if (t.name.Contains("_Puno")) hand = t;
        if (t.name.StartsWith("Antebrazo_")) fore = t;
    }
    foreach (UnityEngine.Transform c in hand)
    {
        if (c.name == "Agarre" || c.name.StartsWith("Agarre.")) ag = c;
        if (c.name.StartsWith("Agarre_Pulgar")) pu = c;
    }
    float handle = UnityEngine.Vector3.Angle(pu.position - ag.position, toolGrip.up);
    float wrist = UnityEngine.Vector3.Angle(fore.up, hand.up);
    return $"el mango cruza el puño desviado {handle:0.0}° (0 = derecho), la muñeca doblada {wrist:0}°";
}

System.Collections.IEnumerator Run()
{
    yield return Frames(20);
    motor.TeleportTo(body.position + body.rotation * new UnityEngine.Vector3(0.2f, 1.05f, -1.0f), body);
    motor.SetYaw(train.transform.eulerAngles.y + 180f);
    look.SetPitch(5f);
    foreach (var g in new[] { SurvivalEnTren.Equipment.GearId.Crowbar, SurvivalEnTren.Equipment.GearId.Hammer }) gear.Give(g);
    gear.SetSuit(true);
    yield return Frames(20);
    log.Append($"CÁMARAS: la principal dibuja las manos={(mainCam.cullingMask & (1 << handsLayer)) != 0}; la de las manos existe={head.Find("Camara_Manos") != null}, dibuja solo las manos={head.Find("Camara_Manos")?.GetComponent<UnityEngine.Camera>().cullingMask == (1 << handsLayer)}.");
    log.Append($" MANOS LIBRES: pose \"{arms.Pose}\", manos a la vista {ActiveHands()}.");

    belt.Select(0);
    for (int i = 0; i < 100 && !belt.IsRaised; i++) yield return wait;
    yield return Frames(5);
    log.Append($" BARRETA (con el traje): pose \"{arms.Pose}\", manos {ActiveHands()} ({ActiveSleeves()}); la mano a {GripGap("Agarre_Derecha"):0.0} cm del agarre; {Straight()}.");
    gear.SetSuit(false);
    yield return Frames(3);
    log.Append($" SIN EL TRAJE: {ActiveSleeves()}.");
    gear.SetSuit(true);

    // El golpe contra la pared de enfrente: el empujón de la vista sube y vuelve.
    motor.TeleportTo(body.position + body.rotation * new UnityEngine.Vector3(0.2f, 1.05f, -8.0f), body);
    yield return Frames(10);
    float peak = 0f; int frames = 0; bool hit = false;
    belt.TrySwing();
    int hits0 = belt.HitCount;
    while (frames++ < 120) { yield return null; peak = UnityEngine.Mathf.Max(peak, rig.KickDegrees.magnitude); if (belt.HitCount > hits0) hit = true; if (hit && !belt.IsSwinging) break; }
    yield return Frames(40);
    log.Append($" GOLPE: pegó={hit} en \"{belt.LastHitName}\" (sirvió={belt.LastHitUseful}); empujón máximo de la vista {peak:0.0}°, después de 0,8 s queda {rig.KickDegrees.magnitude:0.00}°.");

    // Girar la cabeza: las manos se quedan un poco atrás y vuelven.
    var root = rig.Root;
    motor.AddYaw(25f);
    yield return null;
    float lag = UnityEngine.Quaternion.Angle(root.localRotation, UnityEngine.Quaternion.identity);
    yield return Frames(40);
    log.Append($" GIRAR 25°: las manos quedaron {lag:0.0}° atrás; al rato {UnityEngine.Quaternion.Angle(root.localRotation, UnityEngine.Quaternion.identity):0.0}°.");

    // El ritmo del balanceo: caminando y corriendo de verdad (W y Shift), se cuentan los pasos (cada bajada de las manos).
    var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
    var oldBehavior = inputSettings.editorInputBehaviorInPlayMode; var oldBackground = inputSettings.backgroundBehavior;
    inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
    var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("PruebaTeclado");
    var cadence = new float[2]; var speeds = new float[2]; var runPose = new float[2];
    for (int mode = 0; mode < 2; mode++)
    {
        motor.TeleportTo(body.position + body.rotation * new UnityEngine.Vector3(0f, 1.05f, -8.5f), body);
        motor.SetYaw(train.transform.eulerAngles.y); // hacia la cabina: 17 m de pasillo
        yield return Frames(10);
        var keys = mode == 0 ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W)
                             : new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W, UnityEngine.InputSystem.Key.LeftShift);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, keys);
        float t = 0f, lastY = 0f, lastDy = 0f, firstDip = -1f, lastDip = -1f, dist = 0f; int dips = 0;
        var start = motor.transform.position;
        while (t < 2.0f)
        {
            yield return null; t += UnityEngine.Time.deltaTime;
            float y = root.localPosition.y, dy = y - lastY;
            if (t > 0.5f && lastDy < 0f && dy >= 0f) { dips++; if (firstDip < 0f) firstDip = t; lastDip = t; }
            lastY = y; lastDy = dy;
            runPose[mode] = rig.RunWeight;
        }
        dist = UnityEngine.Vector3.Distance(start, motor.transform.position);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        cadence[mode] = dips > 1 ? (dips - 1) / (lastDip - firstDip) : 0f;
        speeds[mode] = dist / 2.0f;
        yield return Frames(30);
    }
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior; inputSettings.backgroundBehavior = oldBackground;
    log.Append($" RITMO: caminando a {speeds[0]:0.0} m/s, {cadence[0]:0.0} pasos por segundo; corriendo a {speeds[1]:0.0} m/s, {cadence[1]:0.0} pasos por segundo (pose de correr {runPose[1]:0.0}).");

    // El martillo: una mano.
    belt.Select(3);
    for (int i = 0; i < 100 && !belt.IsRaised; i++) yield return wait;
    yield return Frames(5);
    log.Append($" MARTILLO: pose \"{arms.Pose}\", manos {ActiveHands()}, la mano a {GripGap("Agarre_Derecha"):0.0} cm del mango.");

    // Cargar una caja: las dos manos abiertas a los costados.
    belt.Select(-1);
    yield return Frames(10);
    SurvivalEnTren.Interaction.Carryable box = null;
    foreach (var c in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Interaction.Carryable>()) if (c.name.StartsWith("Caja") && !c.OneHanded) box = c;
    box.transform.position = head.position + head.forward * 0.6f; box.Body.position = box.transform.position;
    carrier.Pick(box);
    yield return Frames(40);
    var b = box.GetComponent<UnityEngine.Renderer>().bounds;
    float handToBox = 99f;
    foreach (var t in rig.Root.GetComponentsInChildren<UnityEngine.Transform>())
        if (t.name.StartsWith("Agarre") && t.parent.name.Contains("Abierta") && t.parent.parent.name.StartsWith("Brazo_"))
            handToBox = UnityEngine.Mathf.Min(handToBox, UnityEngine.Mathf.Sqrt(b.SqrDistance(t.position)));
    log.Append($" CAJA: pose \"{arms.Pose}\", manos {ActiveHands()} ({ActiveSleeves()}); la caja a {UnityEngine.Vector3.Distance(head.position, b.center):0.00} m de los ojos; la palma más cerca a {handToBox * 100f:0.0} cm de la caja.");
    carrier.Drop(false);
    yield return Frames(5);
    log.Append($" SOLTÓ: pose \"{arms.Pose}\", manos {ActiveHands()}.");
    UnityEngine.Debug.Log($"[Brazos] {log}");
}
motor.StartCoroutine(Run());
return "prueba brazos iniciada";
