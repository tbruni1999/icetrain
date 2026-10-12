using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Configuración de una sola vez del M0: crea los assets de URP, los activa
    /// y pone el Input System como sistema de entrada. Se ejecuta desde el menú
    /// "Survival en tren > M0 > Configurar proyecto". Se puede repetir sin problema.
    /// </summary>
    public static class M0ProjectSetup
    {
        private const string RenderingFolder = "Assets/_Project/Data/Rendering";
        private const string RendererPath = RenderingFolder + "/URP_Renderer.asset";
        private const string PipelinePath = RenderingFolder + "/URP_PipelineAsset.asset";

        [MenuItem("Survival en tren/M0/Configurar proyecto (URP + Input System)")]
        public static void Run()
        {
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder("Assets/_Project/Data", "Rendering");

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                Debug.Log("[M0] Assets de URP creados en " + RenderingFolder);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            Debug.Log("[M0] URP activado en Graphics y en todos los niveles de calidad.");

            bool needsRestart = SetInputSystemOnly();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (needsRestart)
            {
                EditorUtility.DisplayDialog("M0 configurado",
                    "URP quedó activo y el Input System quedó como sistema de entrada.\n\n" +
                    "Unity va a necesitar reiniciar el Editor para aplicar el cambio de entrada. " +
                    "Guardá el proyecto (File > Save Project) y reabrilo.", "OK");
            }
        }

        // Pone "Active Input Handling" = Input System Package (New). Devuelve true si había que cambiarlo.
        private static bool SetInputSystemOnly()
        {
            const string path = "ProjectSettings/ProjectSettings.asset";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[M0] No se pudo leer ProjectSettings.asset; cambiar a mano en Project Settings > Player > Active Input Handling.");
                return false;
            }

            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null)
            {
                Debug.LogWarning("[M0] No se encontró 'activeInputHandler'; cambiar a mano en Project Settings > Player > Active Input Handling.");
                return false;
            }

            if (prop.intValue == 1) return false; // 1 = solo Input System nuevo

            prop.intValue = 1;
            so.ApplyModifiedProperties();
            Debug.Log("[M0] Active Input Handling = Input System Package (New).");
            return true;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
