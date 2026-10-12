// Prueba del M6 paso 2: equipo (barreta, traje), bidón, sifón del vehículo, bomba, boca de carga.
// Los trasvases se simulan llamando a Channel() con un paso de tiempo fijo (lo mismo que hace el
// interactor cada cuadro con E apretado); lo que mira el interactor se lee de verdad (apuntando).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/m6_equipo_y_bidon.cs"
// Resultado en la Console, líneas [Equipo].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var vitals = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.PlayerVitals>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var port = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.FuelPort>();
var vehicle = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.FuelSource>();
var rack = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.SuitRack>();
var crowbar = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.GearPickup>();
var can1 = UnityEngine.GameObject.Find("Bidon_1").GetComponent<SurvivalEnTren.Train.FuelCan>();
var canCarry = can1.GetComponent<SurvivalEnTren.Interaction.Carryable>();
var fuelSettings = tank.Settings;
var tInteractor = typeof(SurvivalEnTren.Interaction.PlayerInteractor);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

object Target() => tInteractor.GetField("_target", flags).GetValue(who);
bool UsableWhileEngaged() => (bool)tInteractor.GetField("_usableWhileEngaged", flags).GetValue(who);
string P(SurvivalEnTren.Interaction.IInteractable i) => i.GetPrompt(who) ?? "(sin indicación)";
string Name(object o) => o == null ? "ninguno" : o.GetType().Name;

// Mira hacia un punto del mundo desde donde está parado el jugador.
void LookAtWorld(UnityEngine.Vector3 point)
{
    UnityEngine.Vector3 eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    UnityEngine.Vector3 dir = point - eye;
    float yaw = UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, yaw));
    float pitch = -UnityEngine.Mathf.Atan2(dir.y, new UnityEngine.Vector2(dir.x, dir.z).magnitude) * UnityEngine.Mathf.Rad2Deg;
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, pitch);
}

// Se para en el suelo, afuera, y mira un punto del mundo.
void StandOnGroundLooking(UnityEngine.Vector3 stand, UnityEngine.Vector3 lookAt)
{
    motor.TeleportTo(new UnityEngine.Vector3(stand.x, 0.1f, stand.z), null);
    LookAtWorld(lookAt);
}

// Pone el bidón en la mano, delante de la cámara (se simula que lo llevó caminando hasta ahí).
void PutCanInHand()
{
    UnityEngine.Vector3 eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var fwd = UnityEngine.Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f) * UnityEngine.Quaternion.Euler(look.Pitch, 0f, 0f) * UnityEngine.Vector3.forward;
    canCarry.Body.position = eye + fwd * 1.1f;
    canCarry.Body.linearVelocity = UnityEngine.Vector3.zero;
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f; // el reloj quieto: la calefacción no gasta mientras se prueba
    for (int i = 0; i < 30; i++) yield return wait;
    UnityEngine.Vector3 portWorld = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(1.5f, 0.55f, -8.6f);
    UnityEngine.Vector3 standByPort = trainBody.position + trainBody.rotation * new UnityEngine.Vector3(4.0f, 0f, -8.6f);

    // 1) Barreta: tomarla del perchero.
    string crowbarPrompt = P(crowbar);
    bool hadBefore = gear.Has(SurvivalEnTren.Equipment.GearId.Crowbar);
    crowbar.Interact(who);
    yield return wait; yield return wait;
    log.Append($"BARRETA: indicación \"{crowbarPrompt}\", la tenía antes={hadBefore}, después de E la tiene={gear.Has(SurvivalEnTren.Equipment.GearId.Crowbar)}, el objeto sigue en el perchero={crowbar != null}.");

    // 2) Traje: pérdida de calor afuera en 1 h de juego, sin traje y con traje.
    StandOnGroundLooking(standByPort, portWorld);
    for (int i = 0; i < 20; i++) yield return wait;
    vitals.Revive(100f, 80f, 75f, 100f);
    vitals.Simulate(1f, false);
    float lossBare = 100f - vitals.Warmth;
    string rackPrompt1 = P(rack);
    rack.Interact(who);
    string rackPrompt2 = P(rack);
    bool hangingHidden = !((UnityEngine.GameObject)typeof(SurvivalEnTren.Equipment.SuitRack).GetField("hangingSuit", flags).GetValue(rack)).activeSelf;
    vitals.Revive(100f, 80f, 75f, 100f);
    vitals.Simulate(1f, false);
    float lossSuit = 100f - vitals.Warmth;
    log.Append($" TRAJE: indicación \"{rackPrompt1}\" → \"{rackPrompt2}\", traje puesto={gear.SuitWorn}, colgado oculto={hangingHidden}; calor perdido en 1 h a {vitals.AmbientTemperatureC:0} °C: sin traje {lossBare:0.0}, con traje {lossSuit:0.0}.");
    rack.Interact(who);
    vitals.Revive(100f, 80f, 75f, 100f);

    // 3) Apuntar a la boca de carga con las manos libres.
    StandOnGroundLooking(standByPort, portWorld);
    for (int i = 0; i < 20; i++) yield return wait;
    yield return null;
    log.Append($" BOCA DE CARGA: apuntando desde afuera el objetivo es {Name(Target())}, indicación con las manos libres \"{P(port)}\".");

    // 4) Bidón: tomarlo del piso del tren (vacío).
    UnityEngine.Vector3 canSpot = canCarry.transform.position;
    motor.TeleportTo(canSpot + trainBody.rotation * new UnityEngine.Vector3(0.9f, 0.06f, 0f), trainBody);
    LookAtWorld(canSpot + UnityEngine.Vector3.up * 0.2f);
    for (int i = 0; i < 40; i++) yield return wait;
    string canPrompt = P(canCarry);
    canCarry.Interact(who);
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($" BIDÓN: indicación \"{canPrompt}\", lo sostiene={carrier.Holding == canCarry}, pesa {canCarry.Body.mass:0.0} kg vacío, caminar x{motor.SpeedMultiplier:0.00}.");

    // Afuera con el bidón en la mano, mirando la boca.
    StandOnGroundLooking(standByPort, portWorld);
    PutCanInHand();
    for (int i = 0; i < 40; i++) yield return wait;
    log.Append($" Bidón vacío en la mano apuntando a la boca: objetivo={Name(Target())} (no debe ser el propio bidón), indicación de la boca \"{P(port)}\" (vacío: ninguna, E suelta el bidón).");

    // 5) Vehículo: sifón.
    UnityEngine.Vector3 tankPos = vehicle.transform.position;
    StandOnGroundLooking(tankPos + vehicle.transform.right * 1.6f, tankPos);
    for (int i = 0; i < 20; i++) yield return wait;
    PutCanInHand();
    for (int i = 0; i < 30; i++) yield return wait;
    string siphonPrompt = P(vehicle);
    log.Append($" VEHÍCULO: apuntando al tanque el objetivo es {Name(Target())}, usable con el bidón en la mano={UsableWhileEngaged()}, indicación \"{siphonPrompt}\".");

    float sec = 0f; float before = vehicle.Liters;
    while (vehicle.Channel(who, 0.1f) && sec < 120f) sec += 0.1f;
    sec += 0.1f;
    for (int i = 0; i < 30; i++) yield return wait;
    log.Append($" Sifonó {before - vehicle.Liters:0.0} L en {sec:0.0} s (esperado {fuelSettings.AbandonedVehicleLiters / fuelSettings.SiphonLitersPerSecond:0.0} s a {fuelSettings.SiphonLitersPerSecond} L/s); bidón {can1.Liters:0.0} L, pesa {canCarry.Body.mass:0.0} kg (esperado {fuelSettings.CanEmptyMassKg + can1.Liters * fuelSettings.FuelKgPerLiter:0.0}), caminar x{motor.SpeedMultiplier:0.00}; vehículo: \"{P(vehicle)}\".");

    // 6) Verter en el tanque del tren por la boca de carga.
    tank.Draw(tank.Liters - 20f); // tanque en 20 L
    StandOnGroundLooking(standByPort, portWorld);
    PutCanInHand();
    for (int i = 0; i < 40; i++) yield return wait;
    string pourPrompt = P(port);
    float tankBefore = tank.Liters; sec = 0f;
    while (port.Channel(who, 0.1f) && sec < 120f) sec += 0.1f;
    sec += 0.1f;
    float poured = tank.Liters - tankBefore;
    log.Append($" CARGA: indicación \"{pourPrompt}\"; vertió {poured:0.0} L en {sec:0.0} s (esperado {poured / fuelSettings.PourLitersPerSecond:0.0} s a {fuelSettings.PourLitersPerSecond} L/s); tanque {tankBefore:0.0} → {tank.Liters:0.0} L, bidón {can1.Liters:0.0} L pesa {canCarry.Body.mass:0.0} kg; con el bidón vacío la boca dice \"{P(port)}\".");

    // 7) Tanque lleno: la boca no ofrece cargar.
    can1.Add(5f); tank.Add(1000f);
    log.Append($" TANQUE LLENO: indicación de la boca \"{P(port)}\" (bidón de {can1.Liters:0} L).");

    // 8) Depósito con bomba: sin la bomba y con la bomba.
    var depotGo = new UnityEngine.GameObject("Deposito_Prueba");
    depotGo.transform.position = vehicle.transform.position + new UnityEngine.Vector3(0f, 2f, 0f);
    var depot = depotGo.AddComponent<SurvivalEnTren.Train.FuelSource>();
    var tSource = typeof(SurvivalEnTren.Train.FuelSource);
    tSource.GetField("settings", flags).SetValue(depot, fuelSettings);
    tSource.GetField("method", flags).SetValue(depot, SurvivalEnTren.Train.FuelSource.Method.HandPump);
    tSource.GetField("displayName", flags).SetValue(depot, "depósito");
    tSource.GetField("liters", flags).SetValue(depot, 150f);
    can1.Draw(100f);
    string noPump = P(depot);
    gear.Give(SurvivalEnTren.Equipment.GearId.HandPump);
    string withPump = P(depot);
    sec = 0f;
    while (depot.Channel(who, 0.1f) && sec < 120f) sec += 0.1f;
    sec += 0.1f;
    log.Append($" DEPÓSITO: sin bomba \"{noPump}\"; con bomba \"{withPump}\"; llenó el bidón ({can1.Liters:0} L) en {sec:0.0} s (esperado {can1.Capacity / fuelSettings.HandPumpLitersPerSecond:0.0} s a {fuelSettings.HandPumpLitersPerSecond} L/s; el sifón tardaría {can1.Capacity / fuelSettings.SiphonLitersPerSecond:0} s); quedan {depot.Liters:0} L.");

    // 9) Con una caja en la mano frente al vehículo, E tiene que soltar la caja (el vehículo no se usa).
    carrier.Drop(false);
    for (int i = 0; i < 20; i++) yield return wait;
    StandOnGroundLooking(tankPos + vehicle.transform.right * 1.6f, tankPos);
    var box = UnityEngine.GameObject.Find("Caja_Cabina").GetComponent<SurvivalEnTren.Interaction.Carryable>();
    box.Body.position = motor.transform.position + UnityEngine.Vector3.up * 1.0f;
    box.Interact(who);
    for (int i = 0; i < 20; i++) yield return wait;
    log.Append($" CAJA EN LA MANO frente al vehículo: sostiene={carrier.Holding == box}, indicación del vehículo \"{P(vehicle)}\" (debe ser ninguna), E suelta la caja={!UsableWhileEngaged()}.");
    carrier.Drop(false);

    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[Equipo] " + log);
}
motor.StartCoroutine(Run());
return "prueba equipo iniciada";
