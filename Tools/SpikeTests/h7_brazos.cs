// Prueba de los brazos en primera persona y del golpe con peso (pedido de Tomi del 2026-10-16). Con las manos libres no se ven;
// con la barreta (con una mano desde el 2026-10-16) y el martillo, una mano en el mango, con el mango derecho en el puño y la
// muñeca apenas doblada (el rig de Tomi, vestido según el traje); el balanceo va al ritmo de los pasos (caminando y corriendo); cargando una caja, las dos abiertas; según el traje cambian
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
// Los brazos son el rig de Tomi ("Brazos_Rig"): se cuenta la ropa que se ve (un guante por mano a la vista).
var rigRoot = rig.Root.Find("Brazos_Rig");
UnityEngine.Transform Bone(string n) { foreach (var t in rigRoot.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == n) return t; return null; }
int ActiveHands() { int n = 0; foreach (var r in rigRoot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)) if (r.enabled && r.name.StartsWith("Guante_")) n++; return n; }
string ActiveSleeves() { var s = new System.Collections.Generic.SortedSet<string>(); foreach (var r in rigRoot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)) if (r.enabled && r.name.EndsWith("_R")) s.Add(r.name); return string.Join(",", s); }
UnityEngine.Transform ToolGrip()
{
    var model = rig.Root.Find(belt.Selected == 0 ? "Mano_Barreta" : "Mano_Martillo");
    foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>()) if (t.name == "Agarre_Derecha") return t;
    return null;
}
float GripGap(string grip) => UnityEngine.Vector3.Distance(Bone("agarre.R").position, ToolGrip().position) * 100f;
// El agarre derecho (Tomi, 2026-10-16: "sigue agarrando re chueca la barreta"): el mango tiene que cruzar el puño por su túnel
// (la línea agarre -> agarre_pulgar de la mano, en la misma dirección que el mango de la herramienta) y la muñeca, apenas doblada
// (el ángulo entre el antebrazo y el eje de la mano, de la muñeca al nudillo del dedo mayor).
string Straight()
{
    float handle = UnityEngine.Vector3.Angle(Bone("agarre_pulgar.R").position - Bone("agarre.R").position, ToolGrip().up);
    var hand = Bone("hand.R");
    float wrist = UnityEngine.Vector3.Angle(hand.position - Bone("forearm.R").position, Bone("f_middle.01.R").position - hand.position);
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
    foreach (string palm in new[] { "palma.R", "palma.L" })
        handToBox = UnityEngine.Mathf.Min(handToBox, UnityEngine.Mathf.Sqrt(b.SqrDistance(Bone(palm).position)));
    log.Append($" CAJA: pose \"{arms.Pose}\", manos {ActiveHands()} ({ActiveSleeves()}); la caja a {UnityEngine.Vector3.Distance(head.position, b.center):0.00} m de los ojos; la palma más cerca a {handToBox * 100f:0.0} cm de la caja.");
    carrier.Drop(false);
    yield return Frames(5);
    log.Append($" SOLTÓ: pose \"{arms.Pose}\", manos {ActiveHands()}.");
    UnityEngine.Debug.Log($"[Brazos] {log}");
}
motor.StartCoroutine(Run());
return "prueba brazos iniciada";
