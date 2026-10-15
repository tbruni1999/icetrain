using System.Collections.Generic;
using SurvivalEnTren.Rail;
using SurvivalEnTren.UI;
using SurvivalEnTren.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SurvivalEnTren.EditorTools
{
    // Producción P3 — Inicio del juego: la escena del menú principal (una vista del vagón en la nieve, al atardecer, con nevada) y
    // la build de Windows. La escena se regenera entera con el menú "Survival en tren > Juego > Construir menú principal".
    public static partial class TestScenesBuilder
    {
        private const string MenuScenePath = "Assets/_Project/Scenes/Menu_Principal.unity";
        private const string BuildFolder = "Builds/Windows";

        [MenuItem("Survival en tren/Juego/Construir menú principal")]
        public static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Luz baja y fría de la tarde; niebla y el cielo de invierno (los de la P1).
            var sun = new GameObject("Luz_Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.95f, 0.88f, 0.8f);
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(9f, 205f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.48f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.4f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.32f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.55f, 0.6f, 0.68f);
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (sky != null) RenderSettings.skybox = sky;

            // El suelo nevado y un tramo de vía con el vagón.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Suelo_Nieve";
            ground.transform.position = new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(400f, 0.1f, 400f);
            Dress(ground.GetComponent<MeshRenderer>(), "MT_Nieve");

            var track = new GameObject("Via");
            var points = new List<Vector3>();
            for (float z = -60f; z <= 60f; z += 2f) points.Add(new Vector3(0f, RailTopHeight, z));
            var segment = track.AddComponent<RailSegment>();
            segment.SetPoints(points);
            track.AddComponent<MeshFilter>();
            track.AddComponent<MeshRenderer>().sharedMaterials = new[] { LibraryMaterial("MT_Balasto"), LibraryMaterial("MT_Durmiente"), LibraryMaterial("MT_AceroOxidado") };
            var visual = track.AddComponent<RailSegmentVisual>();
            visual.SetTextureMeters(TextureMeters("MT_Balasto"), TextureMeters("MT_Durmiente"), TextureMeters("MT_AceroOxidado"));
            visual.Rebuild();

            var wagon = new GameObject("Vagon");
            wagon.transform.position = new Vector3(0f, RailTopHeight, 0f);
            GameObject art = InstantiateArt(WagonArtPath, wagon.transform);
            if (art != null) art.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // la cabina hacia +Z, como en el juego
            foreach (float z in new[] { 4f, -2.5f }) // las luces cálidas de adentro: las ventanas brillan al atardecer
            {
                var lamp = new GameObject("Luz_Interior").AddComponent<Light>();
                lamp.transform.SetParent(wagon.transform, false);
                lamp.transform.localPosition = new Vector3(0f, RoofBottom - 0.3f, z);
                lamp.type = LightType.Point;
                lamp.color = new Color(1f, 0.78f, 0.5f);
                lamp.range = 7f;
                lamp.intensity = 2.2f;
            }

            // La cámara: baja, de costado, gira despacio alrededor del vagón.
            var cameraObject = new GameObject("Camara_Menu");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 800f;
            cameraObject.AddComponent<AudioListener>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cameraObject.transform.position = new Vector3(-16f, 2.2f, 11f);
            cameraObject.transform.LookAt(new Vector3(0f, 2.2f, 0f));

            // El ambiente de la P1: post-proceso y nieve que cae alrededor de la cámara.
            var volume = new GameObject("Postproceso_Atmosfera").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AtmospherePostPath);
            var weather = new GameObject("Clima_Nieve").AddComponent<SnowWeather>();
            SetReference(weather, "settings", LoadOrCreateAsset<AtmosphereSettings>(AtmosphereSettingsPath));
            SetReference(weather, "follow", cameraObject.transform);
            SetReference(weather, "flakeMaterial", SnowParticleMaterial("Nieve_Copo", FlakeTexture()));
            SetReference(weather, "driftMaterial", SnowParticleMaterial("Nieve_Racha", SoftDotTexture()));

            var menu = new GameObject("Menu_Principal").AddComponent<MainMenu>();
            SetReference(menu, "orbitCenter", wagon.transform);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            // El menú es la primera escena del juego (la build arranca ahí); "Salir al menú" la busca por nombre.
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            buildScenes.RemoveAll(s => s.path == MenuScenePath);
            buildScenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[Escenas] Menú principal construido en " + MenuScenePath);
        }

        // La build de Windows: el menú y la escena de juego (sin la del spike, que es de pruebas). Queda en Builds/Windows (fuera de
        // Git). Para medir el rendimiento: abrir SurvivalEnTren.exe con "-medicion" (escribe medicion.txt y se cierra).
        [MenuItem("Survival en tren/Juego/Exportar build de Windows")]
        public static void BuildWindows()
        {
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MenuScenePath, DrivingScenePath },
                locationPathName = $"{BuildFolder}/SurvivalEnTren.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Build] {report.summary.result}: {report.summary.outputPath}, {report.summary.totalSize / (1024f * 1024f):0} MB, {report.summary.totalTime.TotalMinutes:0.0} min, errores {report.summary.totalErrors}");
        }
    }
}
