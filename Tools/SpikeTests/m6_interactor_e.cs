// Prueba del M6 paso 2, camino real de la tecla E: con un teclado virtual se mantiene E frente al
// vehículo (sifón) y frente a la boca de carga, y se toca E con una caja en la mano.
// Necesita que la ventana de Unity tenga el foco (si no, las teclas virtuales no llegan: se avisa).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/m6_interactor_e.cs"
// Resultado en la Console, líneas [InteractorE].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var carrier = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerCarrier>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
SurvivalEnTren.Train.FuelSource vehicle = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<SurvivalEnTren.Train.FuelSource>()) if (s.StableId == "fuel_prueba_vehiculo_001") vehicle = s;
var can1 = UnityEngine.GameObject.Find("Bidon_1").GetComponent<SurvivalEnTren.Train.FuelCan>();
var canCarry = can1.GetComponent<SurvivalEnTren.Interaction.Carryable>();
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

// Unity sin foco en la ventana: que el teclado virtual llegue igual (se restaura al terminar).
var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = inputSettings.editorInputBehaviorInPlayMode;
var oldBackground = inputSettings.backgroundBehavior;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
void Press(bool down) =>
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, down ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E) : new UnityEngine.InputSystem.LowLevel.KeyboardState());

void LookAtWorld(UnityEngine.Vector3 point)
{
    UnityEngine.Vector3 eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    UnityEngine.Vector3 dir = point - eye;
    motor.AddYaw(UnityEngine.Mathf.DeltaAngle(motor.transform.eulerAngles.y, UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg));
    typeof(SurvivalEnTren.Player.PlayerLook).GetField("_pitch", flags).SetValue(look, -UnityEngine.Mathf.Atan2(dir.y, new UnityEngine.Vector2(dir.x, dir.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
}
void PutCanInHand()
{
    UnityEngine.Vector3 eye = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
    var fwd = UnityEngine.Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f) * UnityEngine.Quaternion.Euler(look.Pitch, 0f, 0f) * UnityEngine.Vector3.forward;
    canCarry.Body.position = eye + fwd * 1.1f;
    canCarry.Body.linearVelocity = UnityEngine.Vector3.zero;
}

System.Collections.IEnumerator Run()
{
    clock.SpeedMultiplier = 0f;
    for (int i = 0; i < 30; i++) yield return wait;

    // Tomar el bidón cerca, y llevarlo al tanque del vehículo.
    UnityEngine.Vector3 canSpot = canCarry.transform.position;
    motor.TeleportTo(canSpot + trainBody.rotation * new UnityEngine.Vector3(0.9f, 0.06f, 0f), trainBody);
    LookAtWorld(canSpot + UnityEngine.Vector3.up * 0.2f);
    for (int i = 0; i < 30; i++) yield return wait;
    canCarry.Interact(UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>());
    for (int i = 0; i < 30; i++) yield return wait;

    UnityEngine.Vector3 tankPos = vehicle.transform.position;
    motor.TeleportTo(new UnityEngine.Vector3((tankPos + vehicle.transform.right * 1.6f).x, 0.1f, (tankPos + vehicle.transform.right * 1.6f).z), null);
    LookAtWorld(tankPos);
    for (int i = 0; i < 20; i++) yield return wait;
    PutCanInHand();
    for (int i = 0; i < 30; i++) yield return wait;

    // 1) Mantener E 5 s: tiene que sifonar ~2 L; soltar E: se corta.
    float v0 = vehicle.Liters;
    Press(true);
    for (int i = 0; i < 300; i++) { PutCanInHand(); yield return wait; }
    float v1 = vehicle.Liters;
    Press(false);
    for (int i = 0; i < 60; i++) yield return wait;
    float v2 = vehicle.Liters;
    bool inputArrived = v1 < v0;
    log.Append($"SIFÓN con E mantenido 5 s: vehículo {v0:0.00} → {v1:0.00} L (esperado ~-2,0), bidón {can1.Liters:0.00} L; E soltado: sigue sifonando={(v2 < v1 - 0.01f)}, sostiene el bidón={carrier.Holding == canCarry}.");
    if (!inputArrived) log.Append(" (NO LLEGÓ LA TECLA: Unity sin foco; pedir un clic en la ventana de Unity y repetir)");

    // 2) Tocar E fuera del vehículo con el bidón en la mano: lo suelta.
    motor.TeleportTo(new UnityEngine.Vector3(tankPos.x + 6f, 0.1f, tankPos.z), null);
    LookAtWorld(tankPos + new UnityEngine.Vector3(30f, 0f, 0f));
    for (int i = 0; i < 20; i++) { PutCanInHand(); yield return wait; }
    Press(true); yield return wait; yield return wait; Press(false);
    for (int i = 0; i < 20; i++) yield return wait;
    log.Append($" E tocada mirando a otro lado con el bidón en la mano: lo soltó={carrier.Holding == null}.");

    // 3) Con la caja en la mano frente al vehículo: E suelta la caja.
    var box = UnityEngine.GameObject.Find("Caja_Cabina").GetComponent<SurvivalEnTren.Interaction.Carryable>();
    motor.TeleportTo(new UnityEngine.Vector3((tankPos + vehicle.transform.right * 1.6f).x, 0.1f, (tankPos + vehicle.transform.right * 1.6f).z), null);
    LookAtWorld(tankPos);
    {
        UnityEngine.Vector3 eyeBox = motor.transform.position + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
        var fwdBox = UnityEngine.Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f) * UnityEngine.Quaternion.Euler(look.Pitch, 0f, 0f) * UnityEngine.Vector3.forward;
        box.Body.position = eyeBox + fwdBox * 1.1f;
        box.Body.linearVelocity = UnityEngine.Vector3.zero;
    }
    for (int i = 0; i < 10; i++) yield return wait;
    box.Interact(UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>());
    for (int i = 0; i < 20; i++) yield return wait;
    bool held = carrier.Holding == box;
    Press(true); yield return wait; yield return wait; Press(false);
    for (int i = 0; i < 20; i++) yield return wait;
    log.Append($" Caja en la mano frente al vehículo: la tenía={held}, después de tocar E la soltó={carrier.Holding == null}.");

    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    clock.SpeedMultiplier = 1f;
    UnityEngine.Debug.Log("[InteractorE] " + log);
}
motor.StartCoroutine(Run());
return "prueba interactor E iniciada";
