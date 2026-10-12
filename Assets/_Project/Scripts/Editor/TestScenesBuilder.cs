using System.Collections.Generic;
using System.Linq;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Player;
using SurvivalEnTren.Rail;
using SurvivalEnTren.Spike;
using SurvivalEnTren.Train;
using SurvivalEnTren.Survival;
using SurvivalEnTren.UI;
using SurvivalEnTren.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Construye las escenas de prueba: suelo nevado, vía en óvalo, postes de referencia, la
    /// locomotora greybox (con cabina y zonas reservadas para la V1), el jugador, cajas y medición.
    /// - "Survival en tren > M1 > Construir escena Spike": la del spike M1, con la conducción de
    ///   prueba por flechas (sus pruebas automáticas dependen de ella).
    /// - "Survival en tren > M2 > Construir escena de conducción": con los controles reales de la cabina.
    /// Las escenas y el prefab se regeneran enteros cada vez: no editarlos a mano, cambiar este script.
    /// </summary>
    public static class TestScenesBuilder
    {
        private enum Kind { Spike, Driving }

        private const string SpikeScenePath = "Assets/_Project/Scenes/Spike/Spike_TrenEstable.unity";
        private const string DrivingScenePath = "Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity";
        private const string TrainDriveSettingsPath = "Assets/_Project/Data/Train/TrainDriveSettings.asset";
        private const string PrefabPath = "Assets/_Project/Prefabs/Train/Locomotora_Greybox.prefab";
        private const string MaterialsFolder = "Assets/_Project/Materials/Greybox";
        private const string DriveSettingsPath = "Assets/_Project/Data/Train/TrainTestDriveSettings.asset";
        private const string PlayerSettingsPath = "Assets/_Project/Data/Player/PlayerMotorSettings.asset";
        private const string BoxPrefabPath = "Assets/_Project/Prefabs/Spike/Caja_Prueba.prefab";
        private const string FloatingOriginSettingsPath = "Assets/_Project/Data/World/FloatingOriginSettings.asset";
        private const string InteractionSettingsPath = "Assets/_Project/Data/Interaction/InteractionSettings.asset";
        private const string CarrySettingsPath = "Assets/_Project/Data/Interaction/CarrySettings.asset";
        private const string SurvivalSettingsPath = "Assets/_Project/Data/Survival/SurvivalSettings.asset";
        private const string BodyFeedbackProfilePath = "Assets/_Project/Data/Survival/BodyFeedbackProfile.asset";
        private const string InputPath = "Assets/_Project/Data/Input/IceTrainInput.inputactions";

        // Vía de prueba: un óvalo de dos rectas y dos curvas (~2,46 km por vuelta).
        private const float StraightLength = 600f;
        private const float CurveRadius = 200f;
        private const float CurvePointSpacing = 2f;
        private const float RailTopHeight = 0.6f; // suelo en 0; balasto + durmiente + riel

        // Escena de conducción: cambio de vía en la primera recta, desvío a una vía muerta y cartel de parada.
        private const float SwitchZ = -100f;             // la recta 1 va de z -300 a +300
        private const float SpurCurveRadius = 300f;
        private const float SpurAngleDegrees = 12f;
        private const float SpurStraightLength = 220f;
        private const float StopSignDistance = 150f;     // metros desde el inicio de la recta 1 (z -150)

        // Locomotora. Coordenadas locales: y = 0 es la cara superior del riel, z+ es adelante.
        private const float BodyHalfWidth = 1.7f;   // 3,4 m de ancho exterior (Tomi: con 3 m se sentía angosto)
        private const float BodyHalfLength = 9.5f;
        private const float FloorTop = 1.0f;
        private const float RoofBottom = 3.4f;
        private const float WallThickness = 0.1f;
        private const float InnerHalfWidth = BodyHalfWidth - WallThickness; // cara interior de las paredes
        private const float BogieOffsetZ = 6.5f;
        private const float DoorCenterZ = -6f;
        private const float DoorHalfWidth = 0.5f;
        private const float DoorHeight = 2.1f;
        private const float WindowBottom = FloorTop + 0.9f;
        private const float WindowTop = FloorTop + 1.8f;
        private const float EngineRoomFrontZ = -7.5f;
        private const float StepRise = 0.32f;
        private const float StepDepth = 0.28f;
        private const int StepCount = 4;

        private struct Opening
        {
            public float Min, Max, Bottom, Top;
            public Opening(float min, float max, float bottom, float top)
            {
                Min = min; Max = max; Bottom = bottom; Top = top;
            }
        }

        private class Palette
        {
            public Material Snow, Ballast, Track, Body, Floor, Furniture, Controls, Fuel, Post, PostMarker, Box, Food;
        }

        [MenuItem("Survival en tren/M1/Construir escena Spike (tren estable)")]
        public static void BuildSpike() => Build(Kind.Spike);

        [MenuItem("Survival en tren/M2/Construir escena de conducción")]
        public static void BuildDriving() => Build(Kind.Driving);

        private static void Build(Kind kind)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Escenas] Salir del modo Play antes de construir la escena.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Assets/_Project", "Scenes");
            EnsureFolder("Assets/_Project/Scenes", "Spike");
            EnsureFolder("Assets/_Project/Scenes", "Pruebas");
            EnsureFolder("Assets/_Project", "Prefabs");
            EnsureFolder("Assets/_Project/Prefabs", "Train");
            EnsureFolder("Assets/_Project/Prefabs", "Spike");
            EnsureFolder("Assets/_Project", "Materials");
            EnsureFolder("Assets/_Project/Materials", "Greybox");
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder("Assets/_Project/Data", "Train");
            EnsureFolder("Assets/_Project/Data", "Player");
            EnsureFolder("Assets/_Project/Data", "World");
            EnsureFolder("Assets/_Project/Data", "Interaction");
            EnsureFolder("Assets/_Project/Data", "Survival");

            // La escena nueva va primero: crearla descarga de memoria los assets recién
            // creados y las referencias a ellos quedarían vacías.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Palette palette = CreatePalette();
            GameObject locomotivePrefab = BuildLocomotivePrefab(palette);

            BuildEnvironment(palette);
            RailSegment firstSegment = BuildTrack(palette, kind, out RailSwitch railSwitch, out RailSegment spur);
            BuildPosts(firstSegment, palette, kind == Kind.Driving ? -1f : 1f);
            if (kind == Kind.Driving)
            {
                BuildSwitchStand(railSwitch, palette);
                BuildStopSign(firstSegment, palette);
            }

            var train = (GameObject)PrefabUtility.InstantiatePrefab(locomotivePrefab);
            var mover = train.GetComponent<TrainMover>();
            SetReference(mover, "startSegment", firstSegment);
            mover.SnapToRail();
            if (kind == Kind.Driving) BuildBufferStop(spur, mover, palette);

            var controls = new GameObject(kind == Kind.Spike ? "Spike_Controles" : "Pruebas_Controles");
            TrainTestDriver driver = null;
            PlayerMotor motor;
            if (kind == Kind.Spike)
            {
                // El spike conserva la conducción de prueba por flechas: se le quitan a esta copia del
                // tren la física de conducción y el pupitre, para que no haya dos que manejen el tren.
                Object.DestroyImmediate(train.GetComponent<TrainDynamics>());
                Object.DestroyImmediate(train.GetComponentInChildren<TrainCab>());
                // Las puertas arrancan abiertas: las pruebas del spike (bajar/subir) usan los huecos.
                foreach (SlidingDoor slidingDoor in train.GetComponentsInChildren<SlidingDoor>())
                {
                    var so = new SerializedObject(slidingDoor);
                    so.FindProperty("startOpen").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                driver = controls.AddComponent<TrainTestDriver>();
                SetReference(driver, "train", mover);
                SetReference(driver, "settings", LoadOrCreateAsset<TrainTestDriveSettings>(DriveSettingsPath));
                SetReference(driver, "faster", FindActionReference("Debug/TrainFaster"));
                SetReference(driver, "slower", FindActionReference("Debug/TrainSlower"));
                SetReference(driver, "brake", FindActionReference("Debug/TrainBrake"));

                // El jugador arranca en la nieve, frente a la puerta derecha del tren.
                Vector3 door = train.transform.TransformPoint(new Vector3(BodyHalfWidth, 0f, DoorCenterZ));
                motor = BuildPlayer(new Vector3(door.x + 6f, 0.05f, door.z), Quaternion.LookRotation(-train.transform.right, Vector3.up));
            }
            else
            {
                // El jugador arranca en la cabina, frente al pupitre.
                motor = BuildPlayer(train.transform.TransformPoint(new Vector3(-0.2f, FloorTop + 0.05f, 7.75f)), train.transform.rotation);
            }

            var metrics = controls.AddComponent<SpikeRideMetrics>();
            SetReference(metrics, "motor", motor);
            SetReference(metrics, "head", motor.transform.Find("Cabeza"));

            // Cajas de prueba apoyadas en el tren (paso 4) y tecla para soltar más.
            GameObject boxPrefab = BuildBoxPrefab(palette);
            List<Rigidbody> boxes = PlaceTrainBoxes(train.transform, boxPrefab);
            var propMetrics = controls.AddComponent<SpikePropMetrics>();
            SetReference(propMetrics, "train", train.GetComponent<Rigidbody>());
            var propsProperty = new SerializedObject(propMetrics);
            var list = propsProperty.FindProperty("props");
            list.arraySize = boxes.Count;
            for (int i = 0; i < boxes.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = boxes[i];
            propsProperty.ApplyModifiedPropertiesWithoutUndo();

            var dropper = controls.AddComponent<SpikePropDropper>();
            SetReference(dropper, "motor", motor);
            SetReference(dropper, "head", motor.transform.Find("Cabeza"));
            SetReference(dropper, "drop", FindActionReference("Debug/DropBox"));
            SetReference(dropper, "boxPrefab", boxPrefab);
            SetReference(dropper, "metrics", propMetrics);

            // Estrategia C: re-centrar el mundo cuando el jugador se aleja del origen.
            var originObject = new GameObject("Mundo_Origen");
            var floatingOrigin = originObject.AddComponent<FloatingOrigin>();
            SetReference(floatingOrigin, "settings", LoadOrCreateAsset<FloatingOriginSettings>(FloatingOriginSettingsPath));
            SetReference(floatingOrigin, "reference", motor.transform);

            var overlay = controls.AddComponent<SpikeOverlay>();
            SetReference(overlay, "floatingOrigin", floatingOrigin);
            SetReference(overlay, "train", mover);
            SetReference(overlay, "driver", driver);
            SetReference(overlay, "controls", kind == Kind.Driving ? train.GetComponent<TrainControls>() : null);
            SetReference(overlay, "motor", motor);
            SetReference(overlay, "metrics", metrics);
            SetReference(overlay, "propMetrics", propMetrics);
            SetReference(overlay, "resetMeasure", FindActionReference("Debug/ResetMeasure"));
            SetReference(overlay, "toggleOverlay", FindActionReference("Debug/ToggleOverlay"));

            // Supervivencia (M4): solo en la escena de conducción, para no afectar las pruebas del spike.
            if (kind == Kind.Driving) BuildSurvival(motor, train, overlay, palette);

            string path = kind == Kind.Spike ? SpikeScenePath : DrivingScenePath;
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Escenas] Escena construida en " + path);
        }

        // ---------------------------------------------------------------- Supervivencia (M4)

        private static void BuildSurvival(PlayerMotor motor, GameObject train, SpikeOverlay overlay, Palette p)
        {
            SurvivalSettings settings = LoadOrCreateAsset<SurvivalSettings>(SurvivalSettingsPath);

            var clockObject = new GameObject("Mundo_Reloj");
            var clock = clockObject.AddComponent<GameClock>();
            SetReference(clock, "settings", settings);

            // Clima interior del tren: del tabique de la sala de máquinas al frente, de pared a pared.
            var climate = train.AddComponent<TrainInteriorClimate>();
            SetReference(climate, "settings", settings);
            SetReference(climate, "clock", clock);
            var climateSo = new SerializedObject(climate);
            climateSo.FindProperty("interiorCenter").vector3Value =
                new Vector3(0f, (FloorTop + RoofBottom) * 0.5f, (EngineRoomFrontZ + BodyHalfLength) * 0.5f);
            climateSo.FindProperty("interiorSize").vector3Value =
                new Vector3(InnerHalfWidth * 2f, RoofBottom - FloorTop, BodyHalfLength - EngineRoomFrontZ);
            climateSo.ApplyModifiedPropertiesWithoutUndo();

            var vitals = motor.gameObject.AddComponent<PlayerVitals>();
            SetReference(vitals, "settings", settings);
            SetReference(vitals, "clock", clock);
            SetReference(vitals, "motor", motor);

            var sensor = motor.gameObject.AddComponent<ThermalSensor>();
            SetReference(sensor, "vitals", vitals);
            SetReference(sensor, "probe", motor.transform);
            var climates = new SerializedObject(sensor);
            var list = climates.FindProperty("climates");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = climate;
            climates.ApplyModifiedPropertiesWithoutUndo();

            var hud = motor.gameObject.AddComponent<SurvivalHud>();
            SetReference(hud, "vitals", vitals);

            // Feedback del cuerpo (frío, cansancio, vida baja) con el postproceso de URP: un Volume global
            // con viñeta y ajustes de color, y la cámara del jugador con postproceso activado.
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(BodyFeedbackProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, BodyFeedbackProfilePath);
                foreach (VolumeComponent component in new VolumeComponent[]
                         { profile.Add<Vignette>(true), profile.Add<ColorAdjustments>(true), profile.Add<WhiteBalance>(true) })
                {
                    component.name = component.GetType().Name;
                    AssetDatabase.AddObjectToAsset(component, profile);
                }
                AssetDatabase.SaveAssets();
            }
            var volumeObject = new GameObject("Postproceso_Cuerpo");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            motor.transform.Find("Cabeza").GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var feedback = motor.gameObject.AddComponent<BodyFeedback>();
            SetReference(feedback, "vitals", vitals);
            SetReference(feedback, "head", motor.transform.Find("Cabeza"));
            SetReference(feedback, "volume", volume);
            SetReference(feedback, "breathMaterial", BreathMaterial());

            // Despertar después de desmayarse (DECISIÓN de Tomi): en la cama del tren.
            var fader = motor.gameObject.AddComponent<ScreenFader>();
            var respawn = motor.gameObject.AddComponent<SurvivalRespawn>();
            SetReference(respawn, "vitals", vitals);
            SetReference(respawn, "motor", motor);
            SetReference(respawn, "carrier", motor.GetComponent<PlayerCarrier>());
            SetReference(respawn, "fader", fader);
            SetReference(respawn, "bed", train.GetComponentInChildren<Bed>());

            // Tres raciones básicas en el estante del taller (sección 4: "pocas raciones básicas").
            var food = new GameObject("Comida_Tren").transform;
            void Ration(string name, Vector3 local)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(food, false);
                go.transform.localScale = new Vector3(0.18f, 0.1f, 0.12f);
                go.transform.SetPositionAndRotation(train.transform.TransformPoint(local), train.transform.rotation);
                go.GetComponent<MeshRenderer>().sharedMaterial = p.Food;
                var body = go.AddComponent<Rigidbody>();
                body.mass = 0.4f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                go.AddComponent<FoodItem>();
            }
            float topShelf = FloorTop + 1.33f + 0.03f + 0.06f;
            Ration("Racion_1", new Vector3(InnerHalfWidth - 0.2f, topShelf, -2.2f));
            Ration("Racion_2", new Vector3(InnerHalfWidth - 0.2f, topShelf, -1.75f));
            Ration("Racion_3", new Vector3(InnerHalfWidth - 0.2f, topShelf, -1.45f));

            SetReference(overlay, "vitals", vitals);
            SetReference(overlay, "clock", clock);
            SetReference(overlay, "climate", climate);
            SetReference(overlay, "timeFast", FindActionReference("Debug/TimeFast"));
        }

        // Material transparente para el vaho (partículas sin iluminación de URP).
        private static Material BreathMaterial()
        {
            string path = $"{MaterialsFolder}/Vaho.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetFloat("_Surface", 1f); // transparente
            material.SetFloat("_Blend", 0f);   // alpha
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.6f));
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------------------------------------------------------------- Cajas de prueba

        private static GameObject BuildBoxPrefab(Palette p)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Caja_Prueba";
            box.transform.localScale = Vector3.one * 0.35f;
            box.GetComponent<MeshRenderer>().sharedMaterial = p.Box;
            var body = box.AddComponent<Rigidbody>();
            body.mass = 4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            box.AddComponent<Carryable>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(box, BoxPrefabPath);
            Object.DestroyImmediate(box);
            return prefab;
        }

        // Cajas sobre el workbench, el estante, la cama y el piso, en coordenadas del tren.
        // Quedan fuera de la jerarquía del tren a propósito: son cuerpos físicos libres.
        private static List<Rigidbody> PlaceTrainBoxes(Transform train, GameObject boxPrefab)
        {
            var group = new GameObject("Cajas_Tren").transform;
            var boxes = new List<Rigidbody>();

            void Box(string name, Vector3 localBottomCenter, Vector3 size, float massKg)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab, group);
                go.name = name;
                go.transform.localScale = size;
                go.transform.SetPositionAndRotation(
                    train.TransformPoint(localBottomCenter + Vector3.up * (size.y * 0.5f + 0.01f)), train.rotation);
                var body = go.GetComponent<Rigidbody>();
                body.mass = massKg;
                boxes.Add(body);
            }

            float bench = FloorTop + 0.9f;
            Box("Caja_Workbench_Chica", new Vector3(-(InnerHalfWidth - 0.4f), bench, 0.5f), Vector3.one * 0.3f, 3f);
            Box("Caja_Workbench_Herramientas", new Vector3(-(InnerHalfWidth - 0.4f), bench, -0.4f), new Vector3(0.25f, 0.2f, 0.5f), 5f);
            Box("Caja_Estante_Baja", new Vector3(InnerHalfWidth - 0.2f, FloorTop + 0.13f, -1.5f), Vector3.one * 0.25f, 2f);
            Box("Caja_Estante_Media", new Vector3(InnerHalfWidth - 0.2f, FloorTop + 0.73f, -2.0f), Vector3.one * 0.25f, 2f);
            Box("Caja_Estante_Alta", new Vector3(InnerHalfWidth - 0.2f, FloorTop + 1.33f, -1.0f), Vector3.one * 0.25f, 2f);
            Box("Caja_Cama", new Vector3(InnerHalfWidth - 0.45f, FloorTop + 0.45f, 4.5f), Vector3.one * 0.2f, 1f);
            Box("Caja_Piso_Grande", new Vector3(0.3f, FloorTop, -4.5f), Vector3.one * 0.5f, 10f);
            Box("Caja_Piso_Grande_2", new Vector3(-0.7f, FloorTop, -5.0f), Vector3.one * 0.5f, 10f);
            Box("Caja_Cabina", new Vector3(0.6f, FloorTop, 7.2f), Vector3.one * 0.3f, 3f);
            return boxes;
        }

        // ---------------------------------------------------------------- Jugador

        private static PlayerMotor BuildPlayer(Vector3 position, Quaternion rotation)
        {
            PlayerMotorSettings settings = LoadOrCreateAsset<PlayerMotorSettings>(PlayerSettingsPath);

            var player = new GameObject("Jugador");
            player.transform.SetPositionAndRotation(position, rotation);
            player.AddComponent<CharacterController>();

            var head = new GameObject("Cabeza");
            head.transform.SetParent(player.transform, false);
            head.transform.localPosition = Vector3.up * settings.EyeHeightMeters;
            var camera = head.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 2500f;
            camera.fieldOfView = 70f;
            head.AddComponent<AudioListener>();
            head.tag = "MainCamera";

            var motor = player.AddComponent<PlayerMotor>();
            SetReference(motor, "settings", settings);
            SetReference(motor, "move", FindActionReference("Player/Move"));
            SetReference(motor, "jump", FindActionReference("Player/Jump"));
            SetReference(motor, "run", FindActionReference("Player/Run"));

            var look = player.AddComponent<PlayerLook>();
            SetReference(look, "motor", motor);
            SetReference(look, "head", head.transform);
            SetReference(look, "look", FindActionReference("Player/Look"));

            var interactor = player.AddComponent<PlayerInteractor>();
            SetReference(interactor, "motor", motor);
            SetReference(interactor, "head", head.transform);
            SetReference(interactor, "interact", FindActionReference("Player/Interact"));
            SetReference(interactor, "settings", LoadOrCreateAsset<InteractionSettings>(InteractionSettingsPath));

            var carrier = player.AddComponent<PlayerCarrier>();
            SetReference(carrier, "motor", motor);
            SetReference(carrier, "look", look);
            SetReference(carrier, "interactor", interactor);
            SetReference(carrier, "settings", LoadOrCreateAsset<CarrySettings>(CarrySettingsPath));
            SetReference(carrier, "throwAction", FindActionReference("Player/Throw"));
            return motor;
        }

        // ---------------------------------------------------------------- Locomotora

        private static GameObject BuildLocomotivePrefab(Palette p)
        {
            var root = new GameObject("Locomotora_Greybox");
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            root.AddComponent<TrainMover>();

            // Exterior: chasis, bogies, piso, techo, paredes con puertas y ventanas.
            var exterior = Group("Exterior", root.transform);
            BoxMinMax("Chasis", exterior, new Vector3(-(BodyHalfWidth - 0.3f), 0.25f, -9.3f), new Vector3(BodyHalfWidth - 0.3f, FloorTop - 0.2f, 9.3f), p.Body);
            BoxMinMax("Bogie_Delantero", exterior, new Vector3(-1.0f, 0.05f, BogieOffsetZ - 1.2f), new Vector3(1.0f, 0.6f, BogieOffsetZ + 1.2f), p.Track, false);
            BoxMinMax("Bogie_Trasero", exterior, new Vector3(-1.0f, 0.05f, -BogieOffsetZ - 1.2f), new Vector3(1.0f, 0.6f, -BogieOffsetZ + 1.2f), p.Track, false);
            BoxMinMax("Piso", exterior, new Vector3(-BodyHalfWidth, FloorTop - 0.2f, -BodyHalfLength), new Vector3(BodyHalfWidth, FloorTop, BodyHalfLength), p.Floor);
            BoxMinMax("Techo", exterior, new Vector3(-BodyHalfWidth, RoofBottom, -BodyHalfLength), new Vector3(BodyHalfWidth, RoofBottom + 0.2f, BodyHalfLength), p.Body);

            var sideOpenings = new List<Opening>
            {
                new Opening(DoorCenterZ - DoorHalfWidth, DoorCenterZ + DoorHalfWidth, FloorTop, FloorTop + DoorHeight), // puerta
                new Opening(-4.5f, -3.6f, WindowBottom, WindowTop),
                new Opening(-1.5f, 1.0f, WindowBottom, WindowTop),
                new Opening(3.0f, 5.5f, WindowBottom, WindowTop),
                new Opening(7.2f, 9.0f, WindowBottom, WindowTop), // cabina
            };
            float wallX = BodyHalfWidth - WallThickness * 0.5f;
            WallAlongZ("Pared_Derecha", exterior, wallX, -BodyHalfLength, BodyHalfLength, FloorTop, RoofBottom, sideOpenings, p.Body);
            WallAlongZ("Pared_Izquierda", exterior, -wallX, -BodyHalfLength, BodyHalfLength, FloorTop, RoofBottom, sideOpenings, p.Body);

            float endZ = BodyHalfLength - WallThickness * 0.5f;
            var frontOpenings = new List<Opening> { new Opening(-(InnerHalfWidth - 0.2f), InnerHalfWidth - 0.2f, WindowBottom, WindowTop + 0.1f) };
            WallAlongX("Pared_Frente", exterior, endZ, -BodyHalfWidth + WallThickness, BodyHalfWidth - WallThickness, FloorTop, RoofBottom, frontOpenings, p.Body);
            WallAlongX("Pared_Fondo", exterior, -endZ, -BodyHalfWidth + WallThickness, BodyHalfWidth - WallThickness, FloorTop, RoofBottom, new List<Opening>(), p.Body);

            // Escalones exteriores bajo cada puerta, para bajar al suelo y volver a subir.
            foreach (float side in new[] { 1f, -1f })
            {
                string sideName = side > 0 ? "Derecha" : "Izquierda";
                for (int k = 1; k <= StepCount; k++)
                {
                    float top = FloorTop - StepRise * k;
                    float inner = BodyHalfWidth + StepDepth * (k - 1);
                    float outer = BodyHalfWidth + StepDepth * k;
                    BoxMinMax($"Escalon_{sideName}_{k}", exterior,
                        new Vector3(Mathf.Min(side * inner, side * outer), top - 0.08f, DoorCenterZ - DoorHalfWidth),
                        new Vector3(Mathf.Max(side * inner, side * outer), top, DoorCenterZ + DoorHalfWidth), p.Body);
                }
            }

            BuildDoor("Puerta_Derecha", exterior, 1f, p);
            BuildDoor("Puerta_Izquierda", exterior, -1f, p);

            BoxMinMax("Exterior_BocaCombustible", exterior, new Vector3(BodyHalfWidth, FloorTop + 0.3f, -8.7f), new Vector3(BodyHalfWidth + 0.1f, FloorTop + 0.6f, -8.3f), p.Fuel);

            // Interior dividido en zonas reservadas para los sistemas de la V1.
            // Son bloques de lugar (placeholders): no tienen funcionamiento todavía.
            var interior = Group("Interior", root.transform);

            var cab = Group("Zona_Cabina", interior);
            BuildCabControls(root, cab, p);
            BoxMinMax("Cabina_Asiento", cab, new Vector3(0.6f, FloorTop, 7.6f), new Vector3(1.1f, FloorTop + 0.5f, 8.1f), p.Furniture);
            BoxMinMax("Cabina_ConsolaSonda", cab, new Vector3(-InnerHalfWidth, FloorTop, 6.8f), new Vector3(-(InnerHalfWidth - 0.6f), FloorTop + 1.2f, 7.6f), p.Controls);

            var rest = Group("Zona_Descanso", interior);
            BoxMinMax("Descanso_Cama", rest, new Vector3(InnerHalfWidth - 0.9f, FloorTop, 3.5f), new Vector3(InnerHalfWidth, FloorTop + 0.45f, 5.5f), p.Furniture).AddComponent<Bed>(); // hoy: "lugar miserable" (M4); se mejora en M8
            BoxMinMax("Descanso_Calefaccion", rest, new Vector3(-InnerHalfWidth, FloorTop, 3.8f), new Vector3(-(InnerHalfWidth - 0.6f), FloorTop + 0.9f, 4.4f), p.Controls);

            var workshop = Group("Zona_Taller", interior);
            BoxMinMax("Taller_Workbench", workshop, new Vector3(-InnerHalfWidth, FloorTop, -1.0f), new Vector3(-(InnerHalfWidth - 0.8f), FloorTop + 0.9f, 1.0f), p.Furniture);
            var shelf = Group("Taller_Estante", workshop);
            BoxMinMax("Lateral_A", shelf, new Vector3(InnerHalfWidth - 0.4f, FloorTop, -2.53f), new Vector3(InnerHalfWidth, FloorTop + 1.8f, -2.5f), p.Furniture);
            BoxMinMax("Lateral_B", shelf, new Vector3(InnerHalfWidth - 0.4f, FloorTop, -0.53f), new Vector3(InnerHalfWidth, FloorTop + 1.8f, -0.5f), p.Furniture);
            foreach (float h in new[] { 0.1f, 0.7f, 1.3f })
                BoxMinMax($"Repisa_{h:0.0}", shelf, new Vector3(InnerHalfWidth - 0.4f, FloorTop + h, -2.5f), new Vector3(InnerHalfWidth, FloorTop + h + 0.03f, -0.53f), p.Furniture);

            var cargo = Group("Zona_EntradaCarga", interior);
            BoxMinMax("Carga_Bidon_1", cargo, new Vector3(-InnerHalfWidth, FloorTop, -7.4f), new Vector3(-(InnerHalfWidth - 0.35f), FloorTop + 0.55f, -7.05f), p.Fuel);
            BoxMinMax("Carga_Bidon_2", cargo, new Vector3(-(InnerHalfWidth - 0.4f), FloorTop, -7.4f), new Vector3(-(InnerHalfWidth - 0.75f), FloorTop + 0.55f, -7.05f), p.Fuel);

            var engine = Group("Zona_SalaMaquinas", interior);
            BoxMinMax("SalaMaquinas_Tabique", engine, new Vector3(-BodyHalfWidth + WallThickness, FloorTop, EngineRoomFrontZ - 0.1f), new Vector3(BodyHalfWidth - WallThickness, RoofBottom, EngineRoomFrontZ), p.Body);
            BoxMinMax("SalaMaquinas_MotorTanque", engine, new Vector3(-1.2f, FloorTop, -9.3f), new Vector3(1.2f, FloorTop + 2.0f, EngineRoomFrontZ - 0.2f), p.Track);

            AddInteriorLight(interior, new Vector3(0f, RoofBottom - 0.3f, 4f));
            AddInteriorLight(interior, new Vector3(0f, RoofBottom - 0.3f, -2.5f));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // Puerta corrediza de un costado (M3): la hoja va por afuera de la pared y se corre hacia atrás.
        // Una zona "trigger" en el hueco permite cerrarla apuntando al hueco desde adentro.
        private static void BuildDoor(string name, Transform parent, float side, Palette p)
        {
            var door = Group(name, parent);
            door.localPosition = new Vector3(side * BodyHalfWidth, FloorTop, DoorCenterZ);

            var leaf = Group("Hoja", door);
            BoxMinMax("Panel", leaf, new Vector3(side > 0 ? 0.01f : -0.06f, 0f, -DoorHalfWidth - 0.02f),
                new Vector3(side > 0 ? 0.06f : -0.01f, DoorHeight + 0.02f, DoorHalfWidth + 0.02f), p.Furniture);
            BoxMinMax("Manija", leaf, new Vector3(side > 0 ? 0.06f : -0.09f, 1.0f, DoorHalfWidth - 0.15f),
                new Vector3(side > 0 ? 0.09f : -0.06f, 1.2f, DoorHalfWidth - 0.1f), p.Track, false);

            var zone = BoxMinMax("Zona_Uso", door, new Vector3(-0.15f, 0.05f, -DoorHalfWidth + 0.05f),
                new Vector3(0.15f, DoorHeight - 0.05f, DoorHalfWidth - 0.05f), p.Furniture);
            zone.GetComponent<Collider>().isTrigger = true;
            Object.DestroyImmediate(zone.GetComponent<MeshRenderer>());

            var sliding = door.gameObject.AddComponent<SlidingDoor>();
            SetReference(sliding, "leaf", leaf);
            var so = new SerializedObject(sliding);
            so.FindProperty("openOffset").vector3Value = new Vector3(0f, 0f, -(DoorHalfWidth * 2f + 0.05f));
            so.FindProperty("doorwayCenter").vector3Value = new Vector3(0f, 1.05f, 0f);
            so.FindProperty("doorwayHalfExtents").vector3Value = new Vector3(0.35f, 1.0f, DoorHalfWidth);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Pupitre de conducción (M2): mueble, tres palancas (reversor, potencia, freno), velocímetro
        // de aguja y etiquetas. Agrega a la raíz del tren los controles, la física de conducción y los
        // instrumentos, y al pupitre el componente que permite tomar los controles con E.
        private static void BuildCabControls(GameObject trainRoot, Transform cab, Palette p)
        {
            var desk = Group("Cabina_Pupitre", cab);
            float top = FloorTop + 1.0f;
            BoxMinMax("Mueble", desk, new Vector3(-1.0f, FloorTop, 8.6f), new Vector3(1.0f, top, 9.3f), p.Controls);

            Transform Lever(string name, float x, Material knob)
            {
                BoxMinMax(name + "_Base", desk, new Vector3(x - 0.06f, top, 8.75f), new Vector3(x + 0.06f, top + 0.05f, 8.95f), p.Track, false);
                var pivot = Group(name + "_Eje", desk);
                pivot.localPosition = new Vector3(x, top + 0.04f, 8.85f);
                BoxMinMax("Varilla", pivot, new Vector3(-0.018f, 0f, -0.018f), new Vector3(0.018f, 0.32f, 0.018f), p.Track, false);
                BoxMinMax("Mango", pivot, new Vector3(-0.04f, 0.3f, -0.04f), new Vector3(0.04f, 0.38f, 0.04f), knob, false);
                Label(name.ToUpperInvariant(), desk, new Vector3(x, top + 0.005f, 8.66f));
                return pivot;
            }

            Transform reverser = Lever("Reversor", -0.75f, p.Furniture);
            Transform power = Lever("Potencia", -0.45f, p.Fuel);
            Transform brake = Lever("Freno", -0.15f, p.PostMarker);

            // Velocímetro: dial vertical mirando al maquinista, con marcas cada 10 km/h y aguja.
            var gauge = Group("Velocimetro", desk);
            gauge.localPosition = new Vector3(0.3f, top + 0.27f, 8.95f);
            BoxMinMax("Soporte", gauge, new Vector3(-0.03f, -0.27f, 0.02f), new Vector3(0.03f, 0f, 0.08f), p.Track, false);
            var face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            face.name = "Dial";
            Object.DestroyImmediate(face.GetComponent<Collider>());
            face.transform.SetParent(gauge, false);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            face.transform.localScale = new Vector3(0.3f, 0.01f, 0.3f);
            face.GetComponent<MeshRenderer>().sharedMaterial = p.Furniture;
            const float gaugeMaxKmh = 60f, sweep = 120f;
            for (int kmh = 0; kmh <= gaugeMaxKmh; kmh += 10)
            {
                float angle = Mathf.Lerp(sweep, -sweep, kmh / gaugeMaxKmh);
                var tick = Group($"Marca_{kmh}", gauge);
                tick.localPosition = new Vector3(0f, 0f, -0.012f);
                tick.localRotation = Quaternion.Euler(0f, 0f, angle);
                BoxMinMax("Raya", tick, new Vector3(-0.004f, 0.105f, -0.002f), new Vector3(0.004f, 0.14f, 0.002f),
                    kmh == 40 ? p.PostMarker : p.Track, false);
            }
            Label("km/h", gauge, new Vector3(0f, -0.06f, -0.013f), facingDriver: true);
            var needle = Group("Aguja_Eje", gauge);
            needle.localPosition = new Vector3(0f, 0f, -0.016f);
            BoxMinMax("Aguja", needle, new Vector3(-0.005f, -0.02f, -0.002f), new Vector3(0.005f, 0.125f, 0.002f), p.PostMarker, false);

            // Controles, física e instrumentos en la raíz del tren; tomar los controles, en el pupitre.
            TrainDriveSettings settings = LoadOrCreateAsset<TrainDriveSettings>(TrainDriveSettingsPath);
            var mover = trainRoot.GetComponent<TrainMover>();
            var controls = trainRoot.AddComponent<TrainControls>();
            SetReference(controls, "settings", settings);
            SetReference(controls, "train", mover);
            var dynamics = trainRoot.AddComponent<TrainDynamics>();
            SetReference(dynamics, "controls", controls);
            var instruments = trainRoot.AddComponent<CabInstruments>();
            SetReference(instruments, "controls", controls);
            SetReference(instruments, "train", mover);
            SetReference(instruments, "reverserPivot", reverser);
            SetReference(instruments, "powerPivot", power);
            SetReference(instruments, "brakePivot", brake);
            SetReference(instruments, "needlePivot", needle);
            var cabin = desk.gameObject.AddComponent<TrainCab>();
            SetReference(cabin, "controls", controls);
            SetReference(cabin, "powerUp", FindActionReference("Train/PowerUp"));
            SetReference(cabin, "powerDown", FindActionReference("Train/PowerDown"));
            SetReference(cabin, "reverserForward", FindActionReference("Train/ReverserForward"));
            SetReference(cabin, "reverserBack", FindActionReference("Train/ReverserBack"));
        }

        // Texto 3D chico (etiquetas del pupitre). Por defecto apoyado sobre una superficie horizontal,
        // legible desde el lado del maquinista; con facingDriver, parado y mirando al maquinista.
        private static void Label(string text, Transform parent, Vector3 localPosition, bool facingDriver = false)
        {
            var go = new GameObject("Etiqueta_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = facingDriver ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = 0.006f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.95f, 0.92f, 0.85f);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        private static void AddInteriorLight(Transform parent, Vector3 localPosition)
        {
            var go = new GameObject("Luz_Interior");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.5f);
            light.range = 7f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- Mundo

        private static void BuildEnvironment(Palette p)
        {
            var sun = new GameObject("Luz_Sol");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.9f, 1f);
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            RenderSettings.sun = light;

            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.004f;
            RenderSettings.fogColor = new Color(0.78f, 0.82f, 0.87f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Suelo_Nieve";
            ground.transform.localScale = new Vector3(300f, 1f, 300f); // 3 km x 3 km
            ground.GetComponent<MeshRenderer>().sharedMaterial = p.Snow;
        }

        // Vía de prueba: óvalo. En la escena de conducción, la primera recta se parte en un cambio de vía
        // con un desvío a una vía muerta (railSwitch y spur quedan en null en la del spike).
        private static RailSegment BuildTrack(Palette p, Kind kind, out RailSwitch railSwitch, out RailSegment spur)
        {
            var track = new GameObject("Via");
            float h = RailTopHeight;
            float halfL = StraightLength * 0.5f;
            float r = CurveRadius;
            railSwitch = null;
            spur = null;

            var curveNorth = Arc(new Vector3(0f, h, halfL), r, 0f, Mathf.PI);
            var straightWest = new List<Vector3> { new Vector3(-r, h, halfL), new Vector3(-r, h, -halfL) };
            var curveSouth = Arc(new Vector3(0f, h, -halfL), r, Mathf.PI, 2f * Mathf.PI);

            RailSegment first, lastOfStraight;
            if (kind == Kind.Spike)
            {
                first = lastOfStraight = Segment("Tramo_1_Recta", track.transform,
                    new List<Vector3> { new Vector3(r, h, -halfL), new Vector3(r, h, halfL) }, p);
            }
            else
            {
                first = Segment("Tramo_1a_Recta", track.transform,
                    new List<Vector3> { new Vector3(r, h, -halfL), new Vector3(r, h, SwitchZ) }, p);
                lastOfStraight = Segment("Tramo_1b_Recta", track.transform,
                    new List<Vector3> { new Vector3(r, h, SwitchZ), new Vector3(r, h, halfL) }, p);

                // Desvío hacia afuera del óvalo (+x): curva suave y después recta hasta un tope.
                var spurPoints = new List<Vector3>();
                var center = new Vector3(r + SpurCurveRadius, h, SwitchZ);
                float maxAngle = SpurAngleDegrees * Mathf.Deg2Rad;
                int steps = Mathf.CeilToInt(maxAngle * SpurCurveRadius / CurvePointSpacing);
                for (int i = 0; i <= steps; i++)
                {
                    float a = maxAngle * i / steps;
                    spurPoints.Add(center + new Vector3(-Mathf.Cos(a) * SpurCurveRadius, 0f, Mathf.Sin(a) * SpurCurveRadius));
                }
                var direction = new Vector3(Mathf.Sin(maxAngle), 0f, Mathf.Cos(maxAngle));
                spurPoints.Add(spurPoints[spurPoints.Count - 1] + direction * SpurStraightLength);
                spur = Segment("Tramo_Desvio_ViaMuerta", track.transform, spurPoints, p);

                first.Connect(lastOfStraight);
                var switchObject = new GameObject("Cambio_1");
                switchObject.transform.SetParent(track.transform, false);
                switchObject.transform.position = new Vector3(r, h, SwitchZ);
                railSwitch = switchObject.AddComponent<RailSwitch>();
                railSwitch.Configure(first, lastOfStraight, spur);
                EditorUtility.SetDirty(railSwitch);
                EditorUtility.SetDirty(spur);
            }

            RailSegment s2 = Segment("Tramo_2_Curva", track.transform, curveNorth, p);
            RailSegment s3 = Segment("Tramo_3_Recta", track.transform, straightWest, p);
            RailSegment s4 = Segment("Tramo_4_Curva", track.transform, curveSouth, p);
            lastOfStraight.Connect(s2);
            s2.Connect(s3);
            s3.Connect(s4);
            s4.Connect(first);
            foreach (var s in new[] { first, lastOfStraight, s2, s3, s4 }) EditorUtility.SetDirty(s);
            return first;
        }

        // Palanca del cambio y su indicador, en el suelo a la izquierda de la vía, antes del cambio.
        private static void BuildSwitchStand(RailSwitch railSwitch, Palette p)
        {
            Vector3 forward = Vector3.forward;               // la recta 1 va hacia +z
            Vector3 left = -Vector3.right;
            Vector3 basePoint = new Vector3(railSwitch.Point.x, 0f, railSwitch.Point.z) + left * 3.6f - forward * 6f;

            var stand = new GameObject("Cambio_1_Palanca").transform;
            stand.position = basePoint;
            stand.rotation = Quaternion.LookRotation(forward, Vector3.up);
            BoxMinMax("Base", stand, new Vector3(-0.3f, 0f, -0.25f), new Vector3(0.3f, 0.3f, 0.25f), p.Track);

            var leverPivot = Group("Palanca_Eje", stand);
            leverPivot.localPosition = new Vector3(0f, 0.3f, 0f);
            BoxMinMax("Varilla", leverPivot, new Vector3(-0.03f, 0f, -0.03f), new Vector3(0.03f, 0.95f, 0.03f), p.Track);
            BoxMinMax("Mango", leverPivot, new Vector3(-0.06f, 0.9f, -0.06f), new Vector3(0.06f, 1.02f, 0.06f), p.Fuel);

            BoxMinMax("Poste_Indicador", stand, new Vector3(0.45f, 0f, -0.05f), new Vector3(0.55f, 1.9f, 0.05f), p.Post);
            var indicator = Group("Indicador_Eje", stand);
            indicator.localPosition = new Vector3(0.5f, 2.1f, 0f);
            BoxMinMax("Cara_Directa", indicator, new Vector3(-0.3f, -0.2f, -0.015f), new Vector3(0.3f, 0.2f, 0.015f), p.Snow, false);
            BoxMinMax("Cara_Desvio", indicator, new Vector3(-0.016f, -0.2f, -0.3f), new Vector3(0.016f, 0.2f, 0.3f), p.Fuel, false);
            Label("CAMBIO", stand, new Vector3(0f, 0.305f, -0.15f));

            var lever = stand.gameObject.AddComponent<SwitchLever>();
            SetReference(lever, "railSwitch", railSwitch);
            SetReference(lever, "leverPivot", leverPivot);
            SetReference(lever, "indicatorPivot", indicator);
        }

        // Tope al final de la vía muerta: la cara de la placa coincide con el final de la vía; la placa
        // se hunde en el bloque cuando el tren la comprime (BufferStop), la física está en TrainDynamics.
        private static void BuildBufferStop(RailSegment spur, TrainMover train, Palette p)
        {
            Vector3 end = spur.GetPosition(spur.Length);
            Vector3 direction = spur.GetDirection(spur.Length);
            var stop = new GameObject("Tope_ViaMuerta").transform;
            stop.position = new Vector3(end.x, end.y, end.z);
            stop.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z), Vector3.up);
            BoxMinMax("Bloque", stop, new Vector3(-1.4f, -0.6f, 1.0f), new Vector3(1.4f, 1.2f, 1.8f), p.PostMarker);
            BoxMinMax("Resorte_Izq", stop, new Vector3(-0.9f, 0.4f, 0.1f), new Vector3(-0.7f, 0.6f, 1.0f), p.Track, false);
            BoxMinMax("Resorte_Der", stop, new Vector3(0.7f, 0.4f, 0.1f), new Vector3(0.9f, 0.6f, 1.0f), p.Track, false);
            var plate = Group("Placa", stop);
            BoxMinMax("Cara", plate, new Vector3(-1.2f, 0.1f, 0f), new Vector3(1.2f, 0.9f, 0.12f), p.Fuel);

            var buffer = stop.gameObject.AddComponent<BufferStop>();
            SetReference(buffer, "train", train);
            SetReference(buffer, "plate", plate);
        }

        // Cartel de parada para practicar detener el tren en un punto (sección 24: "detener el tren en un
        // punto sin errores"): la punta de la cabina tiene que quedar a la altura del cartel.
        private static void BuildStopSign(RailSegment straight, Palette p)
        {
            Vector3 at = straight.GetPosition(StopSignDistance);
            var sign = new GameObject("Cartel_Parada").transform;
            sign.position = new Vector3(at.x - 3.5f, 0f, at.z);
            BoxMinMax("Poste", sign, new Vector3(-0.05f, 0f, -0.05f), new Vector3(0.05f, 2.2f, 0.05f), p.Post);
            BoxMinMax("Tablero", sign, new Vector3(-0.45f, 2.0f, -0.08f), new Vector3(0.45f, 2.55f, -0.05f), p.Fuel);
            Label("PARADA", sign, new Vector3(0f, 2.275f, -0.085f), facingDriver: true);
        }

        private static List<Vector3> Arc(Vector3 center, float radius, float fromAngle, float toAngle)
        {
            int steps = Mathf.CeilToInt(Mathf.Abs(toAngle - fromAngle) * radius / CurvePointSpacing);
            var points = new List<Vector3>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(fromAngle, toAngle, i / (float)steps);
                points.Add(center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            return points;
        }

        private static RailSegment Segment(string name, Transform parent, List<Vector3> points, Palette p)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var segment = go.AddComponent<RailSegment>();
            segment.SetPoints(points);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { p.Ballast, p.Track };
            go.AddComponent<MeshCollider>();
            go.AddComponent<RailSegmentVisual>().Rebuild();
            return segment;
        }

        // Postes al costado de la vía cada 20 m (uno más alto cada 100 m) para notar la velocidad.
        // side = 1: a la derecha del sentido de marcha (afuera del óvalo); -1: a la izquierda (adentro),
        // para no cruzarse con el desvío de la escena de conducción.
        private static void BuildPosts(RailSegment first, Palette p, float side)
        {
            var posts = new GameObject("Postes_Referencia");
            float total = 0f;
            var segment = first;
            do
            {
                total += segment.Length;
                segment = segment.Next;
            } while (segment != null && segment != first);

            var position = new RailPosition(first, 0f);
            int index = 0;
            for (float d = 0f; d < total - 1f; d += 20f, index++)
            {
                RailPosition at = position.Advanced(d);
                Vector3 forward = at.WorldDirection;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                bool marker = index % 5 == 0;
                float height = marker ? 4f : 2.5f;
                Vector3 basePoint = at.WorldPosition + right * (3.5f * side);
                basePoint.y = 0f;

                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = marker ? $"Poste_{d:0}m" : "Poste";
                post.transform.SetParent(posts.transform, false);
                post.transform.position = basePoint + Vector3.up * (height * 0.5f);
                post.transform.localScale = new Vector3(0.2f, height, 0.2f);
                post.GetComponent<MeshRenderer>().sharedMaterial = marker ? p.PostMarker : p.Post;
            }
        }

        // ---------------------------------------------------------------- Ayudas

        private static Palette CreatePalette()
        {
            return new Palette
            {
                Snow = Mat("Nieve", new Color(0.86f, 0.88f, 0.92f)),
                Ballast = Mat("Balasto", new Color(0.36f, 0.34f, 0.32f)),
                Track = Mat("Metal_Oscuro", new Color(0.18f, 0.17f, 0.16f)),
                Body = Mat("Tren_Carroceria", new Color(0.45f, 0.22f, 0.18f)),
                Floor = Mat("Tren_Piso", new Color(0.32f, 0.27f, 0.22f)),
                Furniture = Mat("Mobiliario", new Color(0.62f, 0.6f, 0.56f)),
                Controls = Mat("Equipos", new Color(0.3f, 0.38f, 0.42f)),
                Fuel = Mat("Combustible", new Color(0.8f, 0.6f, 0.15f)),
                Post = Mat("Poste", new Color(0.25f, 0.25f, 0.27f)),
                PostMarker = Mat("Poste_Marca", new Color(0.85f, 0.35f, 0.15f)),
                Box = Mat("Caja", new Color(0.62f, 0.45f, 0.26f)),
                Food = Mat("Comida", new Color(0.55f, 0.68f, 0.38f)),
            };
        }

        private static Material Mat(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static InputActionReference FindActionReference(string mapAndAction)
        {
            AssetDatabase.ImportAsset(InputPath);
            var reference = AssetDatabase.LoadAllAssetsAtPath(InputPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && $"{r.action.actionMap.name}/{r.action.name}" == mapAndAction);
            if (reference == null) Debug.LogError($"[M1] No se encontró la acción '{mapAndAction}' en {InputPath}.");
            return reference;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject BoxMinMax(string name, Transform parent, Vector3 min, Vector3 max, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = (min + max) * 0.5f;
            go.transform.localScale = max - min;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // Pared a lo largo del tren (eje z) en una x fija, con huecos para puertas y ventanas.
        private static void WallAlongZ(string name, Transform parent, float x, float zMin, float zMax, float yMin, float yMax, List<Opening> openings, Material material)
        {
            var wall = Group(name, parent);
            float half = WallThickness * 0.5f;
            BuildWallPieces(zMin, zMax, yMin, yMax, openings, (a0, a1, y0, y1) =>
                BoxMinMax("Panel", wall, new Vector3(x - half, y0, a0), new Vector3(x + half, y1, a1), material));
        }

        // Pared a lo ancho del tren (eje x) en una z fija, con huecos.
        private static void WallAlongX(string name, Transform parent, float z, float xMin, float xMax, float yMin, float yMax, List<Opening> openings, Material material)
        {
            var wall = Group(name, parent);
            float half = WallThickness * 0.5f;
            BuildWallPieces(xMin, xMax, yMin, yMax, openings, (a0, a1, y0, y1) =>
                BoxMinMax("Panel", wall, new Vector3(a0, y0, z - half), new Vector3(a1, y1, z + half), material));
        }

        // Corta la pared en franjas donde empiezan/terminan los huecos y rellena lo que no es hueco.
        private static void BuildWallPieces(float aMin, float aMax, float yMin, float yMax, List<Opening> openings, System.Action<float, float, float, float> makePiece)
        {
            var breaks = new SortedSet<float> { aMin, aMax };
            foreach (var o in openings)
            {
                breaks.Add(Mathf.Clamp(o.Min, aMin, aMax));
                breaks.Add(Mathf.Clamp(o.Max, aMin, aMax));
            }

            var list = breaks.ToList();
            for (int i = 0; i < list.Count - 1; i++)
            {
                float a0 = list[i];
                float a1 = list[i + 1];
                if (a1 - a0 < 0.001f) continue;
                float mid = (a0 + a1) * 0.5f;
                Opening? hole = null;
                foreach (var o in openings)
                    if (mid > o.Min && mid < o.Max) hole = o;

                if (hole == null)
                {
                    makePiece(a0, a1, yMin, yMax);
                    continue;
                }
                if (hole.Value.Bottom > yMin) makePiece(a0, a1, yMin, hole.Value.Bottom);
                if (hole.Value.Top < yMax) makePiece(a0, a1, hole.Value.Top, yMax);
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
