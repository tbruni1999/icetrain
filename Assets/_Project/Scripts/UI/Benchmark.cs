using System.Collections;
using System.IO;
using System.Text;
using SurvivalEnTren.Player;
using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// Medición de rendimiento en la build (producción P3; Anexo E.1: "los números finales tienen que salir de una build").
    /// Solo corre si el juego se abre con "-medicion" en la línea de comandos: recorre las mismas situaciones que la prueba del
    /// M13 (adentro del tren quieto, manejando, afuera en la estación, en el taller, mirando el horizonte), mide el tiempo de
    /// cada cuadro y escribe "medicion.txt" en la carpeta de datos del juego. Después cierra el juego.
    /// </summary>
    public class Benchmark : MonoBehaviour
    {
        /// <summary>Lo pone el menú principal cuando ve "-medicion".</summary>
        public static bool Requested;

        public static bool RequestedByCommandLine()
        {
            foreach (string arg in System.Environment.GetCommandLineArgs())
                if (arg == "-medicion") return true;
            return false;
        }

        private readonly StringBuilder _log = new StringBuilder();

        private IEnumerator Start()
        {
            if (!Requested && !RequestedByCommandLine()) yield break;
            Requested = false;
            var train = FindAnyObjectByType<TrainMover>();
            var body = train.GetComponent<Rigidbody>();
            var controls = FindAnyObjectByType<TrainControls>();
            var motor = FindAnyObjectByType<PlayerMotor>();
            var look = FindAnyObjectByType<PlayerLook>();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1; // sin límite: el costo real

            _log.AppendLine($"Medición de rendimiento — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            _log.AppendLine($"Equipo: {SystemInfo.processorType} ({SystemInfo.processorCount} hilos), {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB), {SystemInfo.systemMemorySize} MB de RAM");
            _log.AppendLine($"Pantalla: {Screen.width} x {Screen.height}, calidad {GameOptions.Load().quality}");
            _log.AppendLine("Situación | cuadro promedio (ms) | FPS promedio | peor cuadro (ms) | 1% más lento (ms)");
            for (int i = 0; i < 120; i++) yield return null; // que todo se asiente

            Vector3 Inside(float x, float z) => body.position + body.rotation * new Vector3(x, 1.05f, z);
            motor.TeleportTo(Inside(0f, 2f), body);
            yield return Measure("tren quieto, adentro", 8f);

            controls.TryMoveReverser(+1);
            while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
            while (controls.PowerNotch < 4) controls.MovePowerBrake(+1);
            float accel = 0f;
            while (accel < 30f && train.SpeedMetersPerSecond < 10.5f) { yield return null; accel += Time.deltaTime; }
            yield return Measure($"manejando a {train.SpeedMetersPerSecond * 3.6f:0} km/h, adentro", 10f);
            while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
            while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
            float stop = 0f;
            while (Mathf.Abs(train.SpeedMetersPerSecond) > 0.01f && stop < 30f) { yield return null; stop += Time.deltaTime; }

            Transform station = GameObject.Find("Estacion_Mantenimiento")?.transform;
            if (station != null)
            {
                motor.TeleportTo(station.TransformPoint(new Vector3(10f, 0.1f, 0f)), null);
                yield return Measure("afuera, junto a la estación", 8f);
            }
            Transform workshop = GameObject.Find("Taller_Ferroviario")?.transform;
            if (workshop != null)
            {
                motor.TeleportTo(workshop.TransformPoint(new Vector3(0f, 0.1f, 0f)), null);
                yield return Measure("adentro del taller", 8f);
            }
            Vector3 snow = body.position + body.rotation * new Vector3(12f, 0f, 0f);
            snow.y = 0.1f;
            motor.TeleportTo(snow, null);
            motor.SetYaw(body.rotation.eulerAngles.y + 90f);
            look.SetPitch(-3f);
            yield return Measure("en la nieve, mirando el horizonte", 8f);

            string path = Path.Combine(Application.persistentDataPath, "medicion.txt");
            File.WriteAllText(path, _log.ToString());
            Debug.Log($"[Medicion] {path}\n{_log}");
            Application.Quit();
        }

        private IEnumerator Measure(string name, float seconds)
        {
            for (int i = 0; i < 30; i++) yield return null;
            var frames = new System.Collections.Generic.List<float>(2048);
            float t = 0f;
            while (t < seconds)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                t += dt;
                frames.Add(dt * 1000f);
            }
            frames.Sort();
            float sum = 0f;
            foreach (float f in frames) sum += f;
            float avg = sum / frames.Count;
            float worst = frames[frames.Count - 1];
            float slowOnePercent = frames[Mathf.Clamp(Mathf.FloorToInt(frames.Count * 0.99f), 0, frames.Count - 1)];
            _log.AppendLine($"{name} | {avg:0.00} | {1000f / avg:0} | {worst:0.0} | {slowOnePercent:0.0}");
        }
    }
}
