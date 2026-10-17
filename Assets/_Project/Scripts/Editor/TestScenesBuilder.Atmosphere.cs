using System.Collections.Generic;
using SurvivalEnTren.Player;
using SurvivalEnTren.Rail;
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
            EnsureShadowQuality();

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
            if (sky.GetFloat("_SunSize") < 0.1f) { sky.SetFloat("_SunSize", 0.6f); EditorUtility.SetDirty(sky); } // el valor viejo (cielo procedural) era otra escala
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
            BuildSnowTerrain(atmosphere);
        }

        // ------------------------------------------------------------------------------------------------ Terreno (paso 4)

        private const string TerrainSettingsPath = "Assets/_Project/Data/World/Terreno.asset";
        private const string TerrainFolder = "Assets/_Project/Data/World/Terreno";
        private const float TerrainSize = 3000f;      // como el suelo plano de antes: 3 x 3 km
        private const float TerrainMaxHeight = 20f;
        private const int TerrainResolution = 513;    // 5,9 m entre muestras
        private const string SnowTerrainShader = "Survival en tren/Terreno Nieve"; // Shaders/TerrenoNieve.shader

        // 5) El suelo nevado con relieve (Terrain de Unity, sin paquetes): dunas suaves alargadas por el viento, planas cerca de la
        // vía y de los lugares (lo que se camina no cambia); nieve compacta junto a la vía y pisada cerca de los lugares; y restos
        // altos medio enterrados lejos, que se recortan en la niebla. Reemplaza al plano de nieve de la escena de juego.
        private static void BuildSnowTerrain(AtmosphereSettings atmosphere)
        {
            SnowTerrainSettings settings = LoadOrCreateAsset<SnowTerrainSettings>(TerrainSettingsPath);
            if (!AssetDatabase.IsValidFolder(TerrainFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data/World", "Terreno");
            GameObject oldGround = GameObject.Find("Suelo_Nieve");
            float cell = TerrainSize / (TerrainResolution - 1);
            var corner = new Vector3(-TerrainSize * 0.5f, 0f, -TerrainSize * 0.5f);

            // Distancia de cada muestra a la vía y a los lugares (se "estampa" alrededor de cada punto: rápido).
            var trackDistance = new float[TerrainResolution, TerrainResolution];
            var placeDistance = new float[TerrainResolution, TerrainResolution];
            for (int z = 0; z < TerrainResolution; z++)
                for (int x = 0; x < TerrainResolution; x++) { trackDistance[z, x] = 9999f; placeDistance[z, x] = 9999f; }
            void Stamp(float[,] grid, Bounds area, float reach)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt((area.min.x - reach - corner.x) / cell));
                int x1 = Mathf.Min(TerrainResolution - 1, Mathf.CeilToInt((area.max.x + reach - corner.x) / cell));
                int z0 = Mathf.Max(0, Mathf.FloorToInt((area.min.z - reach - corner.z) / cell));
                int z1 = Mathf.Min(TerrainResolution - 1, Mathf.CeilToInt((area.max.z + reach - corner.z) / cell));
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float px = corner.x + x * cell, pz = corner.z + z * cell;
                        float dx = Mathf.Max(0f, Mathf.Abs(px - area.center.x) - area.extents.x);
                        float dz = Mathf.Max(0f, Mathf.Abs(pz - area.center.z) - area.extents.z);
                        float d = Mathf.Sqrt(dx * dx + dz * dz);
                        if (d < grid[z, x]) grid[z, x] = d;
                    }
            }
            float trackReach = settings.TrackFlat + settings.Blend + cell;
            var trackPoints = new List<Vector3>(); // cada 3 m de vía (también para ubicar los restos lejos)
            foreach (RailSegment segment in Object.FindObjectsByType<RailSegment>())
                for (float d = 0f; d <= segment.Length; d += 3f)
                {
                    Vector3 point = segment.GetPosition(d);
                    trackPoints.Add(point);
                    Stamp(trackDistance, new Bounds(point, Vector3.zero), trackReach);
                }
            float placeReach = settings.PlaceFlat + settings.Blend + cell;
            foreach (Collider c in Object.FindObjectsByType<Collider>())
            {
                if (c.isTrigger || c.gameObject == oldGround) continue;
                if (c.GetComponentInParent<RailSegment>() != null || c.GetComponentInParent<TrainMover>() != null || c.GetComponentInParent<PlayerMotor>() != null) continue;
                Bounds b = c.bounds;
                if (b.min.y > 3f || b.size.x > 200f || b.size.z > 200f) continue;
                Stamp(placeDistance, b, placeReach);
            }

            // Alturas: dunas alargadas a lo largo del viento + lomas grandes; nada en las zonas planas.
            Vector3 along = atmosphere.WindDirection;
            var across = new Vector3(along.z, 0f, -along.x);
            float s = settings.Seed * 13.37f;
            var heights = new float[TerrainResolution, TerrainResolution];
            for (int z = 0; z < TerrainResolution; z++)
                for (int x = 0; x < TerrainResolution; x++)
                {
                    float px = corner.x + x * cell, pz = corner.z + z * cell;
                    float a = px * along.x + pz * along.z, c = px * across.x + pz * across.z;
                    float dunes = Mathf.PerlinNoise(s + a / settings.DuneLength, s * 0.7f + c / settings.DuneWidth);
                    dunes = dunes * 0.8f + 0.2f * Mathf.PerlinNoise(s * 1.3f + a / (settings.DuneLength * 0.45f), s * 0.2f + c / (settings.DuneWidth * 0.45f));
                    dunes = Mathf.Clamp01((dunes - 0.3f) / 0.6f);
                    float swell = Mathf.PerlinNoise(s * 0.5f + px / 320f, s * 0.9f + pz / 320f);
                    float flatTrack = Mathf.SmoothStep(0f, 1f, (trackDistance[z, x] - settings.TrackFlat) / settings.Blend);
                    float flatPlace = Mathf.SmoothStep(0f, 1f, (placeDistance[z, x] - settings.PlaceFlat) / settings.Blend);
                    float h = (settings.DuneHeight * dunes + settings.SwellHeight * swell) * Mathf.Min(flatTrack, flatPlace);
                    heights[z, x] = Mathf.Clamp01(h / TerrainMaxHeight);
                }

            // Que se pueda caminar: la pendiente más empinada (entre muestras vecinas) no pasa del máximo; si pasa, se achata todo.
            float steepest = 0f;
            for (int z = 1; z < TerrainResolution; z++)
                for (int x = 1; x < TerrainResolution; x++)
                {
                    float dx = Mathf.Abs(heights[z, x] - heights[z, x - 1]), dz = Mathf.Abs(heights[z, x] - heights[z - 1, x]);
                    steepest = Mathf.Max(steepest, Mathf.Max(dx, dz) * TerrainMaxHeight / cell);
                }
            float allowed = Mathf.Tan(settings.MaxSlopeDegrees * Mathf.Deg2Rad);
            float flatten = steepest > allowed ? allowed / steepest : 1f;
            if (flatten < 1f)
                for (int z = 0; z < TerrainResolution; z++)
                    for (int x = 0; x < TerrainResolution; x++) heights[z, x] *= flatten;
            Debug.Log($"[Terreno] pendiente más empinada {Mathf.Atan(steepest * flatten) * Mathf.Rad2Deg:0.0}° (máximo {settings.MaxSlopeDegrees:0}°; sin achatar {Mathf.Atan(steepest) * Mathf.Rad2Deg:0.0}°)");

            // Las capas de textura de la librería (a tamaño real): nieve, nieve compacta (junto a la vía y en manchas que junta el
            // viento), pisada (cerca de los lugares) y sucia (pegada a la vía).
            TerrainLayer Layer(string material, float tileMeters)
            {
                string path = $"{TerrainFolder}/{material}.terrainlayer";
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, path); }
                Texture2D Find(string suffix)
                {
                    foreach (string guid in AssetDatabase.FindAssets($"{material}_{suffix} t:Texture2D", new[] { "Assets/_Project/Art/Libreria" }))
                    {
                        string p = AssetDatabase.GUIDToAssetPath(guid);
                        if (System.IO.Path.GetFileNameWithoutExtension(p) == $"{material}_{suffix}") return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                    }
                    return null;
                }
                layer.diffuseTexture = Find("BaseColor");
                layer.normalMapTexture = Find("Normal");
                layer.maskMapTexture = Find("MetalSmooth");
                layer.tileSize = new Vector2(tileMeters, tileMeters);
                layer.metallic = 0f;
                layer.smoothness = 0.35f;
                EditorUtility.SetDirty(layer);
                return layer;
            }
            TerrainLayer[] layers = { Layer("MT_Nieve", 2f), Layer("MT_NieveCompacta", 2.5f), Layer("MT_NievePisada", 2f), Layer("MT_NieveSucia", 2f) };

            string dataPath = $"{TerrainFolder}/Terreno_Nieve.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
            if (data == null) { data = new TerrainData(); AssetDatabase.CreateAsset(data, dataPath); }
            data.heightmapResolution = TerrainResolution;
            data.size = new Vector3(TerrainSize, TerrainMaxHeight, TerrainSize);
            data.SetHeights(0, 0, heights);
            data.terrainLayers = layers;
            data.alphamapResolution = TerrainResolution - 1;
            int splat = data.alphamapResolution;
            var weights = new float[splat, splat, 4];
            for (int z = 0; z < splat; z++)
                for (int x = 0; x < splat; x++)
                {
                    // Manchas de nieve compactada por el viento en el campo (rompen el blanco parejo a lo lejos).
                    float px = corner.x + (x + 0.5f) * TerrainSize / splat, pz = corner.z + (z + 0.5f) * TerrainSize / splat;
                    float wind = Mathf.PerlinNoise(s * 1.7f + px / settings.WindPatchSize, s * 0.3f + pz / settings.WindPatchSize) * 0.75f
                               + Mathf.PerlinNoise(s * 0.4f + px / (settings.WindPatchSize * 0.37f), s * 2.1f + pz / (settings.WindPatchSize * 0.37f)) * 0.25f;
                    float windPatch = Mathf.SmoothStep(0f, 1f, (wind - (1f - settings.WindPatchCover)) / 0.18f) * settings.WindPatchStrength;
                    float compact = 1f - Mathf.SmoothStep(0f, 1f, (trackDistance[z, x] - 3f) / 10f);
                    compact = Mathf.Max(compact, windPatch);
                    float trodden = (1f - Mathf.SmoothStep(0f, 1f, (placeDistance[z, x] - 4f) / 14f)) * (1f - compact);
                    float dirty = (1f - Mathf.SmoothStep(0f, 1f, (trackDistance[z, x] - 1f) / settings.DirtyTrackWidth)) * settings.DirtyTrackStrength;
                    weights[z, x, 0] = Mathf.Max(0f, 1f - compact - trodden) * (1f - dirty);
                    weights[z, x, 1] = compact * (1f - dirty);
                    weights[z, x, 2] = trodden * (1f - dirty);
                    weights[z, x, 3] = dirty;
                }
            data.SetAlphamaps(0, 0, weights);
            EditorUtility.SetDirty(data);

            string materialPath = $"{TerrainFolder}/Terreno_Lit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find(SnowTerrainShader)) { name = "Terreno_Lit" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            // La nieve sin repetición (Parte 1b, 2026-10-17): el sombreador propio. Los valores de la mezcla quedan en el material
            // (se ajustan ahí y no se pisan al reconstruir); solo se pone el lado del terreno.
            Shader snowShader = Shader.Find(SnowTerrainShader);
            if (snowShader != null && material.shader != snowShader) material.shader = snowShader;
            material.SetFloat("_NieveTerrenoTamano", TerrainSize);
            EditorUtility.SetDirty(material);

            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Terreno_Nieve";
            terrainObject.transform.position = corner;
            var terrain = terrainObject.GetComponent<Terrain>();
            terrain.materialTemplate = material;
            terrain.heightmapPixelError = 6f;
            terrain.basemapDistance = 250f; // desde acá Unity usa su imagen de lejos (sin la nieve sin repetición), pero la niebla ya tapa el 97 %
            terrain.drawInstanced = true;
            if (oldGround != null) Object.DestroyImmediate(oldGround); // el plano de nieve de antes

            float HeightAt(Vector3 p)
            {
                int x = Mathf.Clamp(Mathf.RoundToInt((p.x - corner.x) / cell), 0, TerrainResolution - 1);
                int z = Mathf.Clamp(Mathf.RoundToInt((p.z - corner.z) / cell), 0, TerrainResolution - 1);
                return heights[z, x] * TerrainMaxHeight;
            }
            float TrackDistanceAt(Vector3 p)
            {
                int x = Mathf.Clamp(Mathf.RoundToInt((p.x - corner.x) / cell), 0, TerrainResolution - 1);
                int z = Mathf.Clamp(Mathf.RoundToInt((p.z - corner.z) / cell), 0, TerrainResolution - 1);
                if (placeDistance[z, x] < 60f) return 0f; // cerca de un lugar: no
                float best = float.MaxValue; // la distancia de verdad a la vía (la grilla solo la tiene cerca)
                foreach (Vector3 t in trackPoints) best = Mathf.Min(best, (new Vector2(t.x, t.z) - new Vector2(p.x, p.z)).sqrMagnitude);
                return Mathf.Sqrt(best);
            }
            BuildHorizonRemnants(settings, HeightAt, TrackDistanceAt);
        }

        // Restos altos y medio enterrados (sección 19: "ruinas parcialmente enterradas y restos verticales que rompan el
        // horizonte"): torres de alta tensión, chimeneas, postes de una línea caída y un tanque de agua, inclinados, con los
        // materiales de la librería. Lejos de la vía y de los lugares; tienen colisión por si el jugador llega caminando.
        private static void BuildHorizonRemnants(SnowTerrainSettings settings, System.Func<Vector3, float> heightAt, System.Func<Vector3, float> trackDistanceAt)
        {
            var root = new GameObject("Restos_Horizonte").transform;
            var random = new System.Random(settings.Seed * 31 + 5);
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
            Material rust = LibraryMaterial("MT_AceroOxidado"), brick = LibraryMaterial("MT_Ladrillo"),
                concrete = LibraryMaterial("MT_Hormigon"), wood = LibraryMaterial("MT_MaderaIntemperie"), sheet = LibraryMaterial("MT_ChapaCorrugada");

            Transform Beam(Transform parent, Vector3 from, Vector3 to, float thickness, Material material)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Pieza";
                go.transform.SetParent(parent, false);
                go.transform.localPosition = (from + to) * 0.5f;
                go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
                go.transform.localScale = new Vector3(thickness, (to - from).magnitude, thickness);
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
                return go.transform;
            }
            Transform Cylinder(Transform parent, Vector3 bottom, float diameter, float height, Material material)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "Pieza";
                go.transform.SetParent(parent, false);
                go.transform.localPosition = bottom + Vector3.up * height * 0.5f;
                go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
                return go.transform;
            }

            int placed = 0;
            for (int attempt = 0; attempt < 2000 && placed < settings.Remnants; attempt++)
            {
                var spot = new Vector3(Range(-1250f, 1250f), 0f, Range(-1250f, 1250f));
                float d = trackDistanceAt(spot);
                if (d < settings.RemnantMinDistance || d > settings.RemnantMaxDistance) continue;
                int kind = placed % 4;
                var item = new GameObject(kind == 0 ? "Torre" : kind == 1 ? "Chimenea" : kind == 2 ? "Postes" : "Tanque").transform;
                item.SetParent(root, false);
                item.position = new Vector3(spot.x, heightAt(spot) - Range(0.8f, 2.5f), spot.z); // medio enterrado
                var tilt = new Vector3(Range(-1f, 1f), 0f, Range(-1f, 1f)).normalized;
                switch (kind)
                {
                    case 0: // torre de alta tensión: cuatro patas que se juntan arriba y anillos
                    {
                        float h = Range(16f, 22f), b = 1.6f, t = 0.45f;
                        for (int i = 0; i < 4; i++)
                        {
                            var corner = new Vector3((i & 1) == 0 ? -b : b, 0f, (i & 2) == 0 ? -b : b);
                            Beam(item, corner, new Vector3(corner.x * t / b, h, corner.z * t / b), 0.18f, rust);
                        }
                        for (float y = 3f; y < h - 1f; y += 4f)
                        {
                            float w = Mathf.Lerp(b, t, y / h);
                            Beam(item, new Vector3(-w, y, -w), new Vector3(w, y, -w), 0.1f, rust);
                            Beam(item, new Vector3(-w, y, w), new Vector3(w, y, w), 0.1f, rust);
                            Beam(item, new Vector3(-w, y, -w), new Vector3(-w, y, w), 0.1f, rust);
                            Beam(item, new Vector3(w, y, -w), new Vector3(w, y, w), 0.1f, rust);
                        }
                        Beam(item, new Vector3(-3.5f, h - 1.5f, 0f), new Vector3(3.5f, h - 1.5f, 0f), 0.2f, rust); // la cruceta
                        item.rotation = Quaternion.AngleAxis(Range(4f, 14f), Vector3.Cross(Vector3.up, tilt)) * Quaternion.Euler(0f, Range(0f, 360f), 0f);
                        break;
                    }
                    case 1: // chimenea de ladrillo de una fábrica enterrada
                        Cylinder(item, Vector3.zero, Range(1.8f, 2.6f), Range(20f, 28f), brick);
                        item.rotation = Quaternion.AngleAxis(Range(1f, 4f), Vector3.Cross(Vector3.up, tilt));
                        break;
                    case 2: // tres postes de una línea caída, en fila
                    {
                        var line = Quaternion.Euler(0f, Range(0f, 360f), 0f) * Vector3.forward;
                        for (int i = 0; i < 3; i++)
                        {
                            var pole = new GameObject("Poste").transform;
                            pole.SetParent(item, false);
                            pole.localPosition = line * (i * 38f);
                            float h = Range(8f, 10f);
                            Material m = i == 1 ? wood : concrete;
                            Beam(pole, Vector3.zero, Vector3.up * h, 0.32f, m);
                            Beam(pole, new Vector3(-1.2f, h - 0.6f, 0f), new Vector3(1.2f, h - 0.6f, 0f), 0.14f, m);
                            pole.localRotation = Quaternion.AngleAxis(Range(5f, 28f), Vector3.Cross(Vector3.up, new Vector3(Range(-1f, 1f), 0f, Range(-1f, 1f)).normalized))
                                                 * Quaternion.LookRotation(line);
                            pole.localPosition += Vector3.up * (heightAt(item.position + pole.localPosition) - item.position.y - 0.8f);
                        }
                        break;
                    }
                    default: // tanque de agua sobre patas
                    {
                        float legs = Range(5f, 7f);
                        for (int i = 0; i < 4; i++)
                        {
                            var corner = new Vector3((i & 1) == 0 ? -1.6f : 1.6f, 0f, (i & 2) == 0 ? -1.6f : 1.6f);
                            Beam(item, corner, corner * 0.9f + Vector3.up * legs, 0.22f, rust);
                        }
                        Cylinder(item, Vector3.up * legs, 4.2f, 4.5f, sheet);
                        item.rotation = Quaternion.AngleAxis(Range(3f, 9f), Vector3.Cross(Vector3.up, tilt));
                        break;
                    }
                }
                placed++;
            }
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
                material.SetFloat("_QueueOffset", 10f); // URP recalcula la cola desde acá al revisar el material: sin esto vuelve a 3000
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

        // Sombras (pedido de Tomi, 2026-10-15: "las sombras en general se ven recontra mal"). Estaban con una sola cascada para
        // 50 m (cada píxel de sombra cubría ~2,4 cm) y sin sombras suaves: bordes en escalera y dientes. Ahora: 4 cascadas (mucho
        // detalle cerca de la cámara), sombras suaves, menos desfase (menos "luz colada" en uniones finas) y sombras también para
        // las luces de adentro del tren. Es la configuración de URP del proyecto (Data/Rendering/URP_PipelineAsset.asset).
        private static void EnsureShadowQuality()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/_Project/Data/Rendering/URP_PipelineAsset.asset");
            if (pipeline == null) return;
            var so = new SerializedObject(pipeline);
            so.FindProperty("m_ShadowDistance").floatValue = 60f;
            so.FindProperty("m_ShadowCascadeCount").intValue = 4;
            so.FindProperty("m_Cascade4Split").vector3Value = new Vector3(0.067f, 0.2f, 0.467f);
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_SoftShadowQuality").intValue = 2; // media
            so.FindProperty("m_ShadowDepthBias").floatValue = 0.6f;
            so.FindProperty("m_ShadowNormalBias").floatValue = 0.5f;
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
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
