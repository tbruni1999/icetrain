using System.Collections.Generic;
using SurvivalEnTren.Player;
using SurvivalEnTren.Train;
using SurvivalEnTren.Survival;
using SurvivalEnTren.World;
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

        private const string AtmosphereSettingsPath = "Assets/_Project/Data/World/Atmosfera.asset";
        private const string WeatherMaterialsFolder = "Assets/_Project/Materials/Clima";

        // Textura del copo: un disco con el centro lleno y el borde suave (la mancha del vaho es demasiado difusa: de lejos no se ve).
        private static Texture2D FlakeTexture()
        {
            if (!AssetDatabase.IsValidFolder(WeatherMaterialsFolder)) AssetDatabase.CreateFolder("Assets/_Project/Materials", "Clima");
            string path = $"{WeatherMaterialsFolder}/Copo.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0.45f, 1f, r)));
                }
            texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Material de partícula iluminado por la escena (como el vaho del M13: de noche no brilla).
        private static Material SnowParticleMaterial(string name, Texture2D texture)
        {
            if (!AssetDatabase.IsValidFolder(WeatherMaterialsFolder)) AssetDatabase.CreateFolder("Assets/_Project/Materials", "Clima");
            string path = $"{WeatherMaterialsFolder}/{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Surface", 1f); // transparente
            material.SetFloat("_Blend", 0f);   // alpha
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildAtmosphere(PlayerMotor motor, GameObject train)
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

            // 3) Nieve que cae y viento visible (SnowWeather). Adentro del tren no nieva: una caja invisible del tamaño del tren
            // (un trigger: no lo ve la interacción ni frena a nadie) apaga los copos que la tocan, aunque el tren se mueva.
            AtmosphereSettings atmosphere = LoadOrCreateAsset<AtmosphereSettings>(AtmosphereSettingsPath);
            var noSnow = new GameObject("Zona_SinNieve");
            noSnow.transform.SetParent(train.transform, false);
            var noSnowBox = noSnow.AddComponent<BoxCollider>();
            noSnowBox.isTrigger = true;
            float bottom = FloorTop - 0.2f, top = RoofBottom + 0.35f; // del piso al techo, con la nieve que se apoya arriba
            noSnowBox.center = new Vector3(0f, (bottom + top) * 0.5f, 0f);
            noSnowBox.size = new Vector3(BodyHalfWidth * 2f + 0.2f, top - bottom, BodyHalfLength * 2f + 0.4f);

            var weather = new GameObject("Clima_Nieve").AddComponent<SnowWeather>();
            SetReference(weather, "settings", atmosphere);
            SetReference(weather, "follow", motor.transform.Find("Cabeza"));
            SetReference(weather, "clock", Object.FindAnyObjectByType<GameClock>());
            SetReference(weather, "flakeMaterial", SnowParticleMaterial("Nieve_Copo", FlakeTexture()));
            SetReference(weather, "driftMaterial", SnowParticleMaterial("Nieve_Racha", SoftDotTexture()));
            var weatherSo = new SerializedObject(weather);
            var zones = weatherSo.FindProperty("noSnowZones");
            zones.arraySize = 1;
            zones.GetArrayElementAtIndex(0).objectReferenceValue = noSnowBox;
            weatherSo.ApplyModifiedPropertiesWithoutUndo();

            BuildWindowFrost(train, atmosphere);
        }

        // 4) Escarcha en las ventanas del tren (WindowFrost): dos capas del lado de adentro de cada vidrio del arte (el borde y la
        // pareja), con las texturas de escarcha de la librería. Los vidrios se encuentran en la malla del arte (Vidrios_LOD0): los
        // planos de vidrio de más de 20 cm de lado.
        private static void BuildWindowFrost(GameObject train, AtmosphereSettings atmosphere)
        {
            Transform glass = train.transform.Find("Arte_MR04/Vidrios_LOD0");
            Material edgeSource = LibraryMaterial("MT_DecalEscarchaBorde");
            Material fullSource = LibraryMaterial("MT_Escarcha");
            if (glass == null || edgeSource == null || fullSource == null) return;

            // Copias propias que se dibujan después del vidrio (el vidrio también es transparente).
            Material FrostMaterial(Material source, string name)
            {
                string path = $"{WeatherMaterialsFolder}/{name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(source) { name = name }; AssetDatabase.CreateAsset(material, path); }
                else material.CopyPropertiesFromMaterial(source);
                material.renderQueue = (int)RenderQueue.Transparent + 10;
                EditorUtility.SetDirty(material);
                return material;
            }
            Material edgeMaterial = FrostMaterial(edgeSource, "Escarcha_Ventana_Borde");
            Material fullMaterial = FrostMaterial(fullSource, "Escarcha_Ventana");

            var group = Group("Escarcha_Ventanas", train.transform);
            var edges = new List<Renderer>();
            var fulls = new List<Renderer>();
            foreach (Pane pane in FindPanes(glass.GetComponent<MeshFilter>().sharedMesh, train.transform.worldToLocalMatrix * glass.localToWorldMatrix))
            {
                // Hacia adentro del tren (el eje del tren): el lado de donde se mira la escarcha.
                Vector3 inward = Vector3.Dot(pane.Normal, -new Vector3(pane.Center.x, 0f, pane.Center.z)) > 0f ? pane.Normal : -pane.Normal;
                foreach (bool full in new[] { false, true })
                {
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = full ? "Escarcha" : "Escarcha_Borde";
                    Object.DestroyImmediate(quad.GetComponent<Collider>());
                    quad.transform.SetParent(group, false);
                    quad.transform.localPosition = pane.Center + inward * (full ? 0.006f : 0.008f);
                    quad.transform.localRotation = Quaternion.LookRotation(-inward, Vector3.up); // el frente del Quad mira hacia -z
                    quad.transform.localScale = new Vector3(pane.Width, pane.Height, 1f);
                    var renderer = quad.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = full ? fullMaterial : edgeMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    (full ? fulls : edges).Add(renderer);
                }
            }

            var frost = train.AddComponent<WindowFrost>();
            SetReference(frost, "settings", atmosphere);
            SetReference(frost, "climate", train.GetComponent<TrainInteriorClimate>());
            SetReference(frost, "clock", Object.FindAnyObjectByType<GameClock>());
            var so = new SerializedObject(frost);
            void Fill(string field, List<Renderer> list)
            {
                SerializedProperty array = so.FindProperty(field);
                array.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
            Fill("edgeLayers", edges);
            Fill("fullLayers", fulls);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private struct Pane
        {
            public Vector3 Center, Normal;
            public float Width, Height;
        }

        // Los vidrios planos de una malla (en las coordenadas que da "toSpace"): los triángulos que miran derecho a un costado
        // (x) o al frente (z), agrupados por plano y por rectángulos que se tocan. Los dos lados del mismo vidrio cuentan una vez.
        private static List<Pane> FindPanes(Mesh mesh, Matrix4x4 toSpace)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            var groups = new List<(int axis, float plane, Bounds box)>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = toSpace.MultiplyPoint3x4(vertices[triangles[i]]);
                Vector3 b = toSpace.MultiplyPoint3x4(vertices[triangles[i + 1]]);
                Vector3 c = toSpace.MultiplyPoint3x4(vertices[triangles[i + 2]]);
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-8f) continue;
                n.Normalize();
                int axis = Mathf.Abs(n.x) > 0.95f ? 0 : Mathf.Abs(n.z) > 0.95f ? 2 : -1;
                if (axis < 0) continue;
                float plane = Mathf.Round(a[axis] * 100f) / 100f;
                var box = new Bounds(a, Vector3.zero);
                box.Encapsulate(b);
                box.Encapsulate(c);
                box.Expand(0.02f);
                int found = -1;
                for (int g = 0; g < groups.Count; g++)
                    if (groups[g].axis == axis && Mathf.Abs(groups[g].plane - plane) < 0.02f && groups[g].box.Intersects(box)) { found = g; break; }
                if (found < 0) groups.Add((axis, plane, box));
                else { Bounds merged = groups[found].box; merged.Encapsulate(box); groups[found] = (axis, plane, merged); }
            }

            var panes = new List<Pane>();
            foreach (var (axis, plane, box) in groups)
            {
                Vector3 size = box.size - Vector3.one * 0.04f; // sin el margen de 2 cm de cada lado
                float width = axis == 0 ? size.z : size.x;
                if (width < 0.2f || size.y < 0.2f) continue; // los cantos de 0 cm de los vidrios no son vidrios
                Vector3 center = box.center;
                center[axis] = plane;
                panes.Add(new Pane { Center = center, Normal = axis == 0 ? Vector3.right : Vector3.forward, Width = width, Height = size.y });
            }
            return panes;
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
