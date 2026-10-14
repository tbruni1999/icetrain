// Prueba del M13 (UI mínima y final): Escape abre la pausa (el juego se detiene, el cursor queda libre), las flechas y Enter
// eligen, "Guardar" guarda, "Controles" muestra las teclas, Escape vuelve y cierra; el mouse también elige. Y el final de la
// slice: la primera vez que el tren entra a la ruta desconocida aparece el cierre ("RUTA DESCONOCIDA · continuará") y el
// juego sigue.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~12 s), con la ventana de Unity con foco,
//   unity command eval_file --file "Tools/SpikeTests/m13_menu_y_final.cs" --timeout 60
// Resultado en la Console, líneas [MenuFinal].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var trainBody = train.GetComponent<UnityEngine.Rigidbody>();
var controls = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainControls>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var menu = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.UI.PauseMenu>();
var ending = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.UI.SliceEnding>();
var saver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Save.SaveSystem>();
var route = UnityEngine.GameObject.Find("Tramo_RutaDesconocida").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var seg3 = UnityEngine.GameObject.Find("Tramo_3_Recta").GetComponent<SurvivalEnTren.Rail.RailSegment>();
var sw = UnityEngine.GameObject.Find("Cambio_2_Danado").GetComponent<SurvivalEnTren.Rail.RailSwitch>();
var log = new System.Text.StringBuilder();
UnityEngine.Application.runInBackground = true;
saver.FileName = "prueba_menu.json";

var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = inputSettings.editorInputBehaviorInPlayMode;
var oldBackground = inputSettings.backgroundBehavior;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SpikeTestKeyboard");
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("SpikeTestMouse");
mouse.MakeCurrent();
// La pausa detiene el tiempo: esperar por cuadros de pantalla (no de física).
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key key)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
    yield return Frames(3);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    yield return Frames(3);
}
System.Collections.IEnumerator Click(UnityEngine.Vector2 pos)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = pos });
    yield return Frames(3);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = pos }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true));
    yield return Frames(3);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = pos });
    yield return Frames(3);
}

System.Collections.IEnumerator Run()
{
    yield return Frames(60);
    // 1) Escape abre la pausa.
    yield return Tap(UnityEngine.InputSystem.Key.Escape);
    log.Append($"PAUSA con Escape: abierta={menu.IsOpen}, tiempo detenido={UnityEngine.Time.timeScale == 0f}, cursor libre={look.CursorFree}, no se camina={motor.MovementLocked}.");
    // 2) ↓ Enter = Guardar.
    yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    yield return Frames(5);
    log.Append($" ↓ + Enter: \"{saver.LastNotice}\", el archivo existe={saver.HasSave}, el menú sigue abierto={menu.IsOpen}.");
    // 3) ↓↓ Enter = Controles; Escape vuelve; Escape cierra.
    yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    yield return Tap(UnityEngine.InputSystem.Key.DownArrow);
    yield return Tap(UnityEngine.InputSystem.Key.Enter);
    bool help = menu.ShowingHelp;
    yield return Tap(UnityEngine.InputSystem.Key.Escape);
    bool backFromHelp = menu.IsOpen && !menu.ShowingHelp;
    yield return Tap(UnityEngine.InputSystem.Key.Escape);
    log.Append($" Controles: se ven={help}; Escape vuelve al menú={backFromHelp}; Escape cierra: abierta={menu.IsOpen}, el tiempo corre={UnityEngine.Time.timeScale == 1f}, cursor libre={look.CursorFree}.");
    // 4) Con el mouse: abrir y clic en "Seguir".
    yield return Tap(UnityEngine.InputSystem.Key.Escape);
    yield return Click(menu.OptionCenterScreen(0));
    log.Append($" Mouse: clic en \"Seguir\" → abierta={menu.IsOpen}, el tiempo corre={UnityEngine.Time.timeScale == 1f}.");

    // 5) El final: el tren ya en el desvío reparado, entrando a la ruta desconocida.
    sw.RestoreState(SurvivalEnTren.Rail.RailSwitchCondition.Repaired, true);
    train.PlaceOnRail(new SurvivalEnTren.Rail.RailPosition(seg3, 250f), 0f);
    motor.TeleportTo(trainBody.position + trainBody.rotation * new UnityEngine.Vector3(0f, 1.05f, 2f), trainBody);
    yield return Frames(120);
    bool before = ending.Shown;
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 3) controls.MovePowerBrake(+1);
    float t = 0f;
    while (t < 60f && !ending.Shown) { yield return null; t += UnityEngine.Time.deltaTime; }
    float at = train.RailPosition.Distance;
    string seg = train.RailPosition.Segment.name;
    bool showing = ending.Showing;
    float t2 = 0f;
    while (t2 < 20f && ending.Showing) { yield return null; t2 += UnityEngine.Time.unscaledDeltaTime; }
    log.Append($" FINAL: antes de entrar mostrado={before}; apareció a los {t:0} s, con el tren en {seg} a {at:0} m; se veía={showing}; duró {t2:0} s y el juego siguió (tren a {train.SpeedMetersPerSecond * 3.6f:0} km/h, tiempo corre={UnityEngine.Time.timeScale == 1f}).");
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);

    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    inputSettings.editorInputBehaviorInPlayMode = oldBehavior;
    inputSettings.backgroundBehavior = oldBackground;
    UnityEngine.Debug.Log("[MenuFinal] " + log);
}
motor.StartCoroutine(Run());
return "prueba menu y final iniciada";
