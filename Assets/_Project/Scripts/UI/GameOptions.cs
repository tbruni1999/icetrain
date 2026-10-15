using System;
using System.IO;
using SurvivalEnTren.Player;
using SurvivalEnTren.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// Las opciones del jugador (producción P3): volumen, sensibilidad del mouse, calidad gráfica y pantalla completa. Se guardan
    /// en un archivo propio (opciones.json, al lado de las partidas; nada de PlayerPrefs) y se aplican al arrancar y al cambiarlas.
    /// La calidad cambia cosas de la escena, no la configuración del proyecto: sombras de la cámara, cuánta nieve cae y el detalle
    /// del terreno lejano.
    /// </summary>
    [Serializable]
    public class GameOptions
    {
        public enum Quality { Baja = 0, Media = 1, Alta = 2 }

        public float volume = 1f;          // 0 a 1
        public float sensitivity = 1f;     // 0,3 a 3 (veces la sensibilidad de base)
        public Quality quality = Quality.Alta;
        public bool fullscreen = true;

        private static string FilePath => Path.Combine(Application.persistentDataPath, "opciones.json");

        public static GameOptions Load()
        {
            try
            {
                if (File.Exists(FilePath)) return JsonUtility.FromJson<GameOptions>(File.ReadAllText(FilePath)) ?? new GameOptions();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Opciones] no se pudo leer {FilePath}: {e.Message}");
            }
            return new GameOptions();
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Opciones] no se pudo guardar {FilePath}: {e.Message}");
            }
        }

        /// <summary>Lo general: volumen y pantalla completa.</summary>
        public void ApplyGlobal()
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            if (!Application.isEditor && Screen.fullScreen != fullscreen) Screen.fullScreen = fullscreen;
        }

        /// <summary>Lo de la escena de juego: la sensibilidad del jugador y la calidad.</summary>
        public void ApplyToGame(PlayerLook look)
        {
            ApplyGlobal();
            if (look != null) look.SensitivityScale = Mathf.Clamp(sensitivity, 0.3f, 3f);
            Camera camera = Camera.main;
            if (camera != null && camera.TryGetComponent(out UniversalAdditionalCameraData data))
                data.renderShadows = quality != Quality.Baja;
            SnowWeather snow = UnityEngine.Object.FindAnyObjectByType<SnowWeather>();
            if (snow != null) snow.DensityScale = quality == Quality.Alta ? 1f : quality == Quality.Media ? 0.6f : 0.35f;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null) terrain.heightmapPixelError = quality == Quality.Alta ? 6f : quality == Quality.Media ? 10f : 16f;
        }
    }
}
