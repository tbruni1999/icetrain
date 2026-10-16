// Prueba de herramientas y forja, paso H1: las herramientas en la mano y la barra de abajo.
// Elegir con los números (Select), volver a apretar = manos libres, la rueda (Cycle), la sonda como casilla (y Q sigue andando),
// golpear (TrySwing) contra una pared y al aire, guardarla al llevar una caja y al no poder moverse (manejar, paneles).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/h1_herramientas.cs"
// Resultado en la Console, línea [Herramientas].

var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var body = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var belt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var probe = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Probe.PortableProbe>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var big = UnityEngine.GameObject.Find("Caja_Piso_Grande").GetComponent<SurvivalEnTren.Interaction.Carryable>();
var hangingSaw = train.transform.Find("Interior/Zona_Taller/Taller_Sierra/Sierra");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
// Los modelos arrancan apagados: buscarlos por la cabeza.
var headT = motor.transform.Find("Cabeza");
var crowbarT = headT.Find("Mano_Barreta");
var sawT = headT.Find("Mano_Sierra");

System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
string Name(int slot) => slot < 0 ? "manos libres" : belt.Settings.Slots[slot].label;
UnityEngine.Vector3 Inside(float x, float z) => body.position + body.rotation * new UnityEngine.Vector3(x, 1.05f, z);

System.Collections.IEnumerator Run()
{
    // 0) Al empezar: manos libres; sin herramientas la barra no deja elegirlas.
    log.Append($"AL EMPEZAR: elegida {Name(belt.Selected)}; casillas {belt.Settings.Slots.Length} ({string.Join(", ", System.Array.ConvertAll(belt.Settings.Slots, s => s.label))}); barreta disponible={belt.Available(0)}, sierra={belt.Available(1)}, sonda={belt.Available(2)}.");
    belt.Select(0);
    log.Append($" Sin barreta, 1 → {Name(belt.Selected)}.");
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    gear.Give(SurvivalEnTren.Equipment.GearId.Saw);
    motor.TeleportTo(Inside(0.4f, 2f), body);
    yield return Frames(20);

    // 1) Elegir, volver a apretar, la sierra (y la colgada se esconde).
    belt.Select(0);
    yield return Frames(30);
    log.Append($" CON LAS DOS: 1 → {Name(belt.Selected)}, en la mano={belt.IsRaised}, modelo visible={crowbarT.gameObject.activeSelf}.");
    belt.Select(0);
    yield return Frames(30);
    log.Append($" 1 otra vez → {Name(belt.Selected)}, modelo visible={crowbarT.gameObject.activeSelf}.");
    belt.Select(1);
    yield return Frames(30);
    log.Append($" 2 → {Name(belt.Selected)}, en la mano={belt.IsRaised}, sierra visible={sawT.gameObject.activeSelf}, la del taller colgada={hangingSaw.gameObject.activeSelf}.");

    // 2) La sonda como casilla, y Q (SetOut) mueve la barra.
    belt.Select(2);
    yield return Frames(40);
    log.Append($" 3 → {Name(belt.Selected)}, sonda afuera={probe.IsOut}, sierra visible={sawT.gameObject.activeSelf}, colgada otra vez={hangingSaw.gameObject.activeSelf}.");
    probe.SetOut(false);
    yield return Frames(5);
    log.Append($" Q guarda la sonda → {Name(belt.Selected)}.");
    probe.SetOut(true);
    yield return Frames(5);
    log.Append($" Q la saca → {Name(belt.Selected)}.");

    // 3) La rueda: sonda → manos libres → barreta → sierra.
    var order = new System.Collections.Generic.List<string>();
    for (int i = 0; i < 4; i++) { belt.Cycle(+1); yield return Frames(2); order.Add(Name(belt.Selected)); }
    belt.Cycle(-1); yield return Frames(2);
    log.Append($" RUEDA abajo: {string.Join(" → ", order)}; arriba una: {Name(belt.Selected)}.");

    // 4) Golpe contra la pared derecha del tren (a ~1,2 m) y al aire.
    belt.Select(0);
    motor.TeleportTo(Inside(0.4f, 2f), body);
    motor.SetYaw(train.transform.eulerAngles.y + 90f);
    look.SetPitch(0f);
    yield return Frames(40);
    bool started = belt.TrySwing();
    bool twice = belt.TrySwing();
    float maxAngle = 0f;
    for (int i = 0; i < 60; i++)
    {
        yield return wait;
        float a = UnityEngine.Quaternion.Angle(crowbarT.localRotation, UnityEngine.Quaternion.Euler(30f, -14f, -10f));
        maxAngle = UnityEngine.Mathf.Max(maxAngle, a);
    }
    log.Append($" GOLPE A LA PARED: empezó={started}, otro encima={twice} (debe ser False); golpes {belt.Swings}, pegó {belt.HitCount} vez en \"{belt.LastHitName}\" (sirvió={belt.LastHitUseful}: la pared no se desarma); la barreta giró hasta {maxAngle:0}°; terminó={!belt.IsSwinging}.");
    var p = body.position + body.rotation * new UnityEngine.Vector3(8f, 0f, 2f); p.y = 0.1f;
    motor.TeleportTo(p, null);
    motor.SetYaw(train.transform.eulerAngles.y + 90f);
    look.SetPitch(-35f); // hacia arriba: nada adelante
    yield return Frames(40);
    int hitsBefore = belt.HitCount;
    belt.TrySwing();
    yield return Frames(60);
    log.Append($" AL AIRE: golpes {belt.Swings}, pegó {belt.HitCount - hitsBefore} veces (debe ser 0).");

    // 5) Con una caja en la mano se guarda; al soltarla vuelve. Sin poder moverse (manejar, paneles) también se guarda.
    var boxLocal = train.transform.InverseTransformPoint(big.transform.position);
    motor.TeleportTo(train.transform.TransformPoint(boxLocal + new UnityEngine.Vector3(0f, 0.1f, 1.0f)), body);
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, train.transform.eulerAngles.y + 180f));
    look.SetPitch(35f);
    yield return Frames(30);
    big.Interact(who);
    yield return Frames(40);
    bool held = carrier.Holding == big;
    bool hidden = !crowbarT.gameObject.activeSelf, swingWhileHolding = belt.TrySwing();
    big.Interact(who);
    yield return Frames(40);
    log.Append($" CAJA: la tomó={held}, barreta guardada={hidden}, golpe con la caja={swingWhileHolding} (debe ser False); la soltó={carrier.Holding == null}, barreta de vuelta={belt.IsRaised}.");
    motor.MovementLocked = true;
    yield return Frames(30);
    bool lockedHidden = !crowbarT.gameObject.activeSelf;
    motor.MovementLocked = false;
    yield return Frames(30);
    who.enabled = false;
    yield return Frames(30);
    bool panelHidden = !crowbarT.gameObject.activeSelf;
    who.enabled = true;
    yield return Frames(30);
    log.Append($" SIN PODER MOVERSE (manejando): guardada={lockedHidden}; CON UN PANEL ABIERTO: guardada={panelHidden}; después, en la mano={belt.IsRaised}.");
    belt.Select(-1);
    UnityEngine.Debug.Log($"[Herramientas] {log}");
}
motor.StartCoroutine(Run());
return "prueba herramientas iniciada";
