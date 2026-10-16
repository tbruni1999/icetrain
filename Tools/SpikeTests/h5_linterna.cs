// Prueba de la linterna (decisión de Tomi del 2026-10-16: en el pecho, con F). Arranca apagada; la tecla F real la prende y la
// apaga; ilumina hacia donde se mira (con un poco de retraso); se lleva junto con una herramienta en la mano; se guarda.
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/h5_linterna.cs"
// Resultado en la Console, línea [Linterna].

var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var lamp = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerFlashlight>();
var gear = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.PlayerGear>();
var belt = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Equipment.ToolBelt>();
var save = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Save.SaveSystem>();
var light = lamp.GetComponentInChildren<UnityEngine.Light>(true) ?? UnityEngine.GameObject.Find("Linterna")?.GetComponent<UnityEngine.Light>();
var head = motor.transform.Find("Cabeza");
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
var settings = UnityEngine.InputSystem.InputSystem.settings;
var oldBehavior = settings.editorInputBehaviorInPlayMode; var oldBackground = settings.backgroundBehavior;
settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("PruebaTeclado");
void KeyF(bool down) => UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, down ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F) : new UnityEngine.InputSystem.LowLevel.KeyboardState());
System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return wait; }
System.Collections.IEnumerator PressF() { KeyF(true); yield return Frames(3); KeyF(false); yield return Frames(3); }

System.Collections.IEnumerator Run()
{
    yield return Frames(20);
    log.Append($"AL EMPEZAR: prendida={lamp.IsOn}; spot de {light.spotAngle:0}°, alcance {light.range:0} m, intensidad {light.intensity:0.0}, sombra {light.shadows}.");
    yield return PressF();
    log.Append($" F: prendida={lamp.IsOn}.");
    look.SetPitch(25f);
    yield return Frames(40);
    float angle = UnityEngine.Vector3.Angle(light.transform.forward, head.forward);
    float below = head.position.y - light.transform.position.y;
    log.Append($" SIGUE LA MIRADA: entre la luz y la vista quedan {angle:0.0}° (después de 0,8 s), la luz va {below * 100f:0} cm debajo de los ojos (en el pecho).");
    gear.Give(SurvivalEnTren.Equipment.GearId.Crowbar);
    belt.Select(0);
    yield return Frames(30);
    log.Append($" CON LA BARRETA EN LA MANO: barreta={belt.IsRaised}, linterna prendida={lamp.IsOn}.");
    var fileField = typeof(SurvivalEnTren.Save.SaveSystem).GetField("fileName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    string oldFile = (string)fileField.GetValue(save);
    fileField.SetValue(save, "prueba_linterna.json");
    save.Save();
    var data = UnityEngine.JsonUtility.FromJson<SurvivalEnTren.Save.SaveGame>(System.IO.File.ReadAllText(save.SavePath));
    System.IO.File.Delete(save.SavePath);
    fileField.SetValue(save, oldFile);
    yield return PressF();
    log.Append($" GUARDADO con la linterna prendida: {data.player.flashlightOn}. F otra vez: prendida={lamp.IsOn}.");
    belt.Select(-1);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    settings.editorInputBehaviorInPlayMode = oldBehavior; settings.backgroundBehavior = oldBackground;
    UnityEngine.Debug.Log($"[Linterna] {log}");
}
motor.StartCoroutine(Run());
return "prueba linterna iniciada";
