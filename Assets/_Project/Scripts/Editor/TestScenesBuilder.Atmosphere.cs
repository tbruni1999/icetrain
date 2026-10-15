using SurvivalEnTren.Player;
using SurvivalEnTren.Survival;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SurvivalEnTren.EditorTools
{
    // Producción P1 — Atmósfera y clima (pedido de Tomi, 2026-10-14: "lo veo super verde"; sección 19, Anexo E.2 y E.3 y la guía
    // de arte, Fuentes/Guia_arte_MR04.md). Solo en la escena de juego (M2_Conduccion); la del spike queda como está.
    // Todos los valores son PROPUESTA y están en datos (Data/World): el perfil del post-proceso, el cielo y Atmosfera.asset.
    public static partial class TestScenesBuilder
    {
        private const string AtmospherePostPath = "Assets/_Project/Data/World/Atmosfera_Postproceso.asset";
        private const string SkyMaterialPath = "Assets/_Project/Data/World/Cielo.mat";
        private const string RendererDataPath = "Assets/_Project/Data/Rendering/URP_Renderer.asset";

        private static void BuildAtmosphere(PlayerMotor motor)
        {
            // 1) Post-proceso de la guía de arte: tonemapping, bloom bajo, viñeta leve y corrección leve hacia frío. Un Volume
            // global con menos prioridad que el del cuerpo (M4); el del cuerpo toma estos valores como base y suma el frío, el
            // cansancio y las heridas encima (BodyFeedback.baseline).
            VolumeProfile post = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AtmospherePostPath);
            if (post == null) // se crea una sola vez: si Tomi lo ajusta, reconstruir la escena no lo pisa
            {
                post = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(post, AtmospherePostPath);
                var tonemapping = post.Add<Tonemapping>();
                tonemapping.mode.Override(TonemappingMode.ACES);
                var bloom = post.Add<Bloom>();
                bloom.intensity.Override(0.25f);
                bloom.threshold.Override(1.1f);
                bloom.scatter.Override(0.6f);
                var vignette = post.Add<Vignette>();
                vignette.intensity.Override(0.18f);
                vignette.smoothness.Override(0.5f);
                var white = post.Add<WhiteBalance>();
                white.temperature.Override(-8f);
                var color = post.Add<ColorAdjustments>();
                color.contrast.Override(8f);
                color.saturation.Override(-6f);
                foreach (VolumeComponent component in post.components)
                {
                    component.name = component.GetType().Name;
                    AssetDatabase.AddObjectToAsset(component, post);
                }
                AssetDatabase.SaveAssets();
            }
            var volumeObject = new GameObject("Postproceso_Atmosfera");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = -1f;
            volume.sharedProfile = post;
            BodyFeedback feedback = motor.GetComponent<BodyFeedback>();
            if (feedback != null) SetReference(feedback, "baseline", post);

            // Sombras de contacto (SSAO, la guía de arte): un "renderer feature" de URP, una sola vez en el renderer del proyecto.
            EnsureAmbientOcclusion();

            // 2) Cielo de invierno (Shaders/CieloNiebla.shader): en el horizonte toma el color de la niebla, así la nieve lejana se
            // funde con el cielo sin línea; arriba, gris azulado. El día y la noche (M13) le cambian la exposición y la niebla.
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            Shader skyShader = Shader.Find("SurvivalEnTren/CieloNiebla");
            if (sky == null)
            {
                sky = new Material(skyShader) { name = "Cielo" };
                AssetDatabase.CreateAsset(sky, SkyMaterialPath);
            }
            else if (sky.shader != skyShader)
            {
                sky.shader = skyShader; // el primer cielo (procedural de Unity) dejaba el horizonte amarillo y una línea con la niebla
                EditorUtility.SetDirty(sky);
            }
            RenderSettings.skybox = sky;
        }

        private static void EnsureAmbientOcclusion()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererDataPath);
            if (renderer == null) return;
            foreach (ScriptableRendererFeature existing in renderer.rendererFeatures)
                if (existing is ScreenSpaceAmbientOcclusion) return;

            var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            feature.name = "SombrasDeContacto_SSAO";
            feature.hideFlags |= HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
            var featureSo = new SerializedObject(feature);
            featureSo.FindProperty("m_Settings.Intensity").floatValue = 0.6f;
            featureSo.FindProperty("m_Settings.Radius").floatValue = 0.25f;
            featureSo.FindProperty("m_Settings.DirectLightingStrength").floatValue = 0.2f;
            featureSo.FindProperty("m_Settings.Downsample").boolValue = true;
            featureSo.ApplyModifiedPropertiesWithoutUndo();

            // Como lo hace el editor de URP: la lista de features y su mapa de identificadores crecen juntos.
            var so = new SerializedObject(renderer);
            SerializedProperty features = so.FindProperty("m_RendererFeatures");
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
        }
    }
}
