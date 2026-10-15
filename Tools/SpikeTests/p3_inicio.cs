// Prueba de la producción P3: el inicio del juego, de punta a punta.
// - El menú principal: las opciones se abren y se guardan (y se vuelven a leer igual).
// - "Nueva partida": abre la escena de juego con el despertar (pantalla negra, acostado en la cama, la vista se levanta) y
//   después se puede caminar.
// - Guardar en el juego, "Salir al menú" desde la pausa, y "Continuar": vuelve a la partida guardada (se aplica lo guardado).
// Cómo correrla: escena Menu_Principal recién puesta en Play (esperar ~12 s),
//   unity command eval_file --file "Tools/SpikeTests/p3_inicio.cs" --timeout 60
// Resultado en la Console, línea [Inicio].

var host = new UnityEngine.GameObject("Prueba_Inicio").AddComponent<SurvivalEnTren.Core.CoroutineHost>();
UnityEngine.Object.DontDestroyOnLoad(host.gameObject); // la prueba sigue viva al cambiar de escena
var log = new System.Text.StringBuilder();
// La prueba usa la partida de verdad (Continuar la busca ahí): si Tomi tiene una, se guarda aparte y se devuelve al final.
var savePath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "saves", "partida.json");
var backupPath = savePath + ".antes_de_la_prueba";
bool hadSave = System.IO.File.Exists(savePath);
if (hadSave) System.IO.File.Copy(savePath, backupPath, true);
UnityEngine.Application.runInBackground = true;

System.Collections.IEnumerator Run()
{
    var menu = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.UI.MainMenu>();
    log.Append($"MENÚ: escena {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, hay partida guardada={menu.HasSave}. ");

    // Opciones: se abren, se cambian y se guardan.
    menu.Choose(2);
    yield return null;
    var before = SurvivalEnTren.UI.GameOptions.Load();
    var changed = SurvivalEnTren.UI.GameOptions.Load();
    changed.volume = 0.55f; changed.sensitivity = 1.7f; changed.quality = SurvivalEnTren.UI.GameOptions.Quality.Media;
    changed.Save();
    var reread = SurvivalEnTren.UI.GameOptions.Load();
    log.Append($"OPCIONES: abiertas={menu.OptionsOpen}; guardadas y leídas otra vez: volumen {reread.volume:0.00}, sensibilidad {reread.sensitivity:0.0}, calidad {reread.quality} (iguales={reread.volume == 0.55f && reread.sensitivity == 1.7f && reread.quality == SurvivalEnTren.UI.GameOptions.Quality.Media}). ");

    // Nueva partida: el despertar.
    float t0 = UnityEngine.Time.realtimeSinceStartup;
    menu.Choose(0);
    SurvivalEnTren.UI.WakeUpIntro intro = null;
    while (intro == null && UnityEngine.Time.realtimeSinceStartup - t0 < 20f) { yield return null; intro = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.UI.WakeUpIntro>(); }
    yield return null; yield return null;
    var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
    var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
    var bed = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.Bed>();
    bool lockedAtStart = motor.MovementLocked;
    float pitchAtStart = look.Pitch;
    float distanceToBed = UnityEngine.Vector3.Distance(motor.transform.position, bed.WakePoint);
    float wait = 0f;
    while (intro.Playing && wait < 20f) { yield return null; wait += UnityEngine.Time.deltaTime; }
    log.Append($"NUEVA PARTIDA: escena {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}; al empezar quieto={lockedAtStart}, mirando {pitchAtStart:0}° (negativo = hacia arriba), a {distanceToBed:0.00} m del lugar de despertar de la cama; el despertar duró {wait:0.0} s; después: quieto={motor.MovementLocked}, mirando {look.Pitch:0}°; sensibilidad del jugador {look.SensitivityScale:0.0} (de las opciones). ");

    // Guardar, salir al menú y continuar.
    var saver = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Save.SaveSystem>();
    var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
    clock.SetTotalHours(clock.TotalHours + 5.0);
    double savedHours = clock.TotalHours;
    bool saved = saver.Save();
    var pause = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.UI.PauseMenu>();
    pause.Open();
    pause.Choose(5); // salir al menú
    t0 = UnityEngine.Time.realtimeSinceStartup;
    menu = null;
    while (menu == null && UnityEngine.Time.realtimeSinceStartup - t0 < 20f) { yield return null; menu = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.UI.MainMenu>(); }
    yield return null;
    log.Append($"GUARDÓ={saved} a las {savedHours:0.0} h; SALIR AL MENÚ: escena {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, el tiempo corre={UnityEngine.Time.timeScale == 1f}, hay partida guardada={menu.HasSave}. ");
    int loadsBefore = SurvivalEnTren.Save.SaveSystem.LoadsApplied;
    menu.Choose(1); // continuar
    t0 = UnityEngine.Time.realtimeSinceStartup;
    while (SurvivalEnTren.Save.SaveSystem.LoadsApplied == loadsBefore && UnityEngine.Time.realtimeSinceStartup - t0 < 20f) yield return null;
    yield return null;
    clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
    intro = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.UI.WakeUpIntro>();
    log.Append($"CONTINUAR: escena {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, se aplicó la partida={SurvivalEnTren.Save.SaveSystem.LoadsApplied > loadsBefore}, hora {clock.TotalHours:0.0} (guardada {savedHours:0.0}), sin despertar={!intro.Playing}.");

    before.Save(); // las opciones como estaban
    if (hadSave) { System.IO.File.Copy(backupPath, savePath, true); System.IO.File.Delete(backupPath); }
    else if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);
    UnityEngine.Debug.Log("[Inicio] " + log);
    UnityEngine.Object.Destroy(host.gameObject);
}
host.StartCoroutine(Run());
return "prueba inicio iniciada";
