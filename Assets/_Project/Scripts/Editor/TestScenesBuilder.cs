using System.Collections.Generic;
using System.Linq;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using SurvivalEnTren.Player;
using SurvivalEnTren.Poi;
using SurvivalEnTren.Probe;
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
        private const string ProbeSettingsPath = "Assets/_Project/Data/Probe/ProbeSettings.asset";
        private const string FuelSettingsPath = "Assets/_Project/Data/Train/FuelSettings.asset";
        private const string BackpackSettingsPath = "Assets/_Project/Data/Items/BackpackSettings.asset";
        private const string ItemsFolderPath = "Assets/_Project/Data/Items";
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
            EnsureFolder("Assets/_Project/Data", "Probe");
            EnsureFolder("Assets/_Project/Data", "Items");

            // La escena nueva va primero: crearla descarga de memoria los assets recién
            // creados y las referencias a ellos quedarían vacías.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Palette palette = CreatePalette();
            GameObject locomotivePrefab = BuildLocomotivePrefab(palette);

            BuildEnvironment(palette, kind == Kind.Driving ? 0.009f : 0.004f); // más niebla en la de M2: sin ver el tren de lejos (M5)
            RailSegment firstSegment = BuildTrack(palette, kind, out RailSwitch railSwitch, out RailSegment spur);
            BuildPosts(firstSegment, palette, kind == Kind.Driving ? -1f : 1f);
            if (kind == Kind.Driving)
            {
                BuildSwitchStand(railSwitch, palette);
                BuildStopSign(firstSegment, palette);
                BuildProbeSignals(palette);
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
            if (kind == Kind.Driving)
            {
                BuildSurvival(motor, train, overlay, palette);
                BuildPortableProbe(motor, train, palette);
            }

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
            SetReference(climate, "fuel", train.GetComponent<FuelTank>());

            // El motor gasta combustible con el reloj del juego (la calefacción gasta en el clima).
            var burner = train.AddComponent<TrainFuelBurner>();
            SetReference(burner, "tank", train.GetComponent<FuelTank>());
            SetReference(burner, "controls", train.GetComponent<TrainControls>());
            SetReference(burner, "clock", clock);
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
            var zoneList = climates.FindProperty("zones");
            ThermalZone[] zones = Object.FindObjectsByType<ThermalZone>();
            zoneList.arraySize = zones.Length;
            for (int i = 0; i < zones.Length; i++) zoneList.GetArrayElementAtIndex(i).objectReferenceValue = zones[i];
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
            SetReference(overlay, "fuel", train.GetComponent<FuelTank>());
            BuildEquipment(motor, train, vitals, overlay, p);
            SetReference(overlay, "timeFast", FindActionReference("Debug/TimeFast"));
        }

        // ---------------------------------------------------------------- Equipo y combustible (M6)

        // Rincón de equipo del tren (junto a la puerta de atrás, pared izquierda): perchero con el traje y
        // la barreta, y dos bidones en el piso. Más un vehículo abandonado cerca de la vía para sifonar.
        // El equipo del jugador (PlayerGear) y los objetos que se usan con E.
        private static void BuildEquipment(PlayerMotor motor, GameObject train, PlayerVitals vitals, SpikeOverlay overlay, Palette p)
        {
            FuelSettings fuel = LoadOrCreateAsset<FuelSettings>(FuelSettingsPath);

            var gear = motor.gameObject.AddComponent<PlayerGear>();
            SetReference(gear, "vitals", vitals);
            SetReference(overlay, "gear", gear);

            // Mochila (M7): materiales con peso y un límite; el peso se suma al de la mano para frenar el caminar.
            var backpackSettings = LoadBackpackSettings();
            var backpack = motor.gameObject.AddComponent<Backpack>();
            SetReference(backpack, "settings", backpackSettings);
            SetReference(motor.GetComponent<PlayerCarrier>(), "backpack", backpack);
            SetReference(overlay, "backpack", backpack);

            // Panel de la mochila (Tab): lista, peso y soltar; lo soltado queda como bulto con física.
            var panel = motor.gameObject.AddComponent<BackpackPanel>();
            SetReference(panel, "backpack", backpack);
            SetReference(panel, "settings", backpackSettings);
            SetReference(panel, "motor", motor);
            SetReference(panel, "look", motor.GetComponent<PlayerLook>());
            SetReference(panel, "interactor", motor.GetComponent<PlayerInteractor>());
            SetReference(panel, "head", motor.transform.Find("Cabeza"));
            SetReference(panel, "toggle", FindActionReference("Player/ToggleBackpack"));
            SetReference(panel, "up", FindActionReference("Backpack/Up"));
            SetReference(panel, "down", FindActionReference("Backpack/Down"));
            SetReference(panel, "dropOne", FindActionReference("Backpack/DropOne"));
            SetReference(panel, "dropAll", FindActionReference("Backpack/DropAll"));
            LoadMaterials(); // crea los archivos de datos de los materiales (los usan los objetos desmontables)

            var corner = Group("Equipo_Tren", train.transform);
            float wall = -InnerHalfWidth;

            // Perchero: tabla en la pared con el traje colgado (se ve mientras no lo llevás puesto).
            Material suitMat = Mat("Traje", new Color(0.88f, 0.48f, 0.12f));
            var board = BoxMinMax("Perchero", corner, new Vector3(wall, FloorTop + 0.95f, -7.35f), new Vector3(wall + 0.05f, FloorTop + 2.0f, -6.6f), p.Furniture);
            var hanging = Group("Traje_Colgado", corner); // hermano del perchero (el perchero está escalado: no puede ser su padre)
            BoxMinMax("Traje_Torso", hanging, new Vector3(wall + 0.05f, FloorTop + 1.15f, -7.28f), new Vector3(wall + 0.17f, FloorTop + 1.85f, -6.97f), suitMat, false);
            BoxMinMax("Traje_Capucha", hanging, new Vector3(wall + 0.05f, FloorTop + 1.85f, -7.2f), new Vector3(wall + 0.15f, FloorTop + 1.97f, -7.05f), suitMat, false);
            BoxMinMax("Traje_Piernas", hanging, new Vector3(wall + 0.05f, FloorTop + 1.0f, -7.26f), new Vector3(wall + 0.14f, FloorTop + 1.15f, -6.99f), suitMat, false);
            var rack = board.AddComponent<SuitRack>();
            SetReference(rack, "hangingSuit", hanging.gameObject);

            // Barreta colgada de dos ganchos (la herramienta del inicio, sección 4). Una zona transparente
            // más grande la hace fácil de apuntar.
            var crowbar = Group("Barreta", corner);
            BoxMinMax("Hierro", crowbar, new Vector3(wall + 0.05f, FloorTop + 1.0f, -6.82f), new Vector3(wall + 0.11f, FloorTop + 1.8f, -6.76f), p.Track, false);
            BoxMinMax("Punta", crowbar, new Vector3(wall + 0.05f, FloorTop + 1.78f, -6.82f), new Vector3(wall + 0.17f, FloorTop + 1.84f, -6.76f), p.Track, false);
            BoxMinMax("Gancho_A", crowbar, new Vector3(wall + 0.05f, FloorTop + 1.55f, -6.85f), new Vector3(wall + 0.09f, FloorTop + 1.6f, -6.73f), p.Furniture, false);
            BoxMinMax("Gancho_B", crowbar, new Vector3(wall + 0.05f, FloorTop + 1.2f, -6.85f), new Vector3(wall + 0.09f, FloorTop + 1.25f, -6.73f), p.Furniture, false);
            var crowbarZone = BoxMinMax("Zona_Uso", crowbar, new Vector3(wall + 0.0f, FloorTop + 0.95f, -6.95f), new Vector3(wall + 0.4f, FloorTop + 1.9f, -6.65f), p.Track);
            crowbarZone.GetComponent<Collider>().isTrigger = true;
            Object.DestroyImmediate(crowbarZone.GetComponent<MeshRenderer>());
            var pickup = crowbar.gameObject.AddComponent<GearPickup>();
            var pickupSo = new SerializedObject(pickup);
            pickupSo.FindProperty("gear").enumValueIndex = (int)GearId.Crowbar;
            pickupSo.FindProperty("displayName").stringValue = "barreta";
            pickupSo.ApplyModifiedPropertiesWithoutUndo();

            // Dos bidones vacíos en el piso (sección 4: "un recipiente/bidón"; acá hay dos para que sobre).
            Material canMat = Mat("Bidon", new Color(0.7f, 0.16f, 0.1f));
            var cans = new GameObject("Bidones_Tren").transform;
            foreach (var (name, x) in new[] { ("Bidon_1", wall + 0.2f), ("Bidon_2", wall + 0.62f) })
                BuildJerrycan(name, cans, train.transform.TransformPoint(new Vector3(x, FloorTop, -7.2f)), train.transform.rotation, canMat, p, fuel);

            // Vehículo abandonado junto a la vía, a la vista desde la cabina: se saca combustible con sifón.
            BuildAbandonedVehicle(train.transform, p, fuel);

            // Un pallet tirado cerca del vehículo, para probar el desmontaje sin ir a la estación.
            Vector3 palletSpot = train.transform.position + train.transform.forward * 31f + train.transform.right * 10f;
            BuildPallet(null, new Vector3(palletSpot.x, 0f, palletSpot.z), train.transform.eulerAngles.y + 20f, "salv_prueba_pallet_001",
                LoadOrCreateRecipe("Pallet", "pallet", 3f, (LoadMaterials().Wood, 4)), backpackSettings, p);
        }

        // Los cuatro materiales de la V1 (sección 9.1) como archivos de datos. Se crean una sola vez: si Tomi cambia
        // un peso en el archivo, reconstruir la escena no lo pisa.
        private sealed class MaterialSet
        {
            public ItemDefinition Wood, Metal, Cloth, Parts;
        }

        private static MaterialSet LoadMaterials() => new MaterialSet
        {
            Wood = LoadOrCreateItem("Madera", "item_madera", "madera", 2f, new Color(0.62f, 0.45f, 0.26f)),
            Metal = LoadOrCreateItem("Metal", "item_metal", "metal", 3.5f, new Color(0.55f, 0.57f, 0.6f)),
            Cloth = LoadOrCreateItem("Tela", "item_tela", "tela", 0.8f, new Color(0.55f, 0.62f, 0.72f)),
            Parts = LoadOrCreateItem("Componentes", "item_componentes", "componentes", 0.3f, new Color(0.3f, 0.7f, 0.45f)),
        };

        private static ItemDefinition LoadOrCreateItem(string fileName, string id, string displayName, float weightKg, Color color)
        {
            string path = $"{ItemsFolderPath}/{fileName}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null) return item;
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            AssetDatabase.CreateAsset(item, path);
            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("weightKg").floatValue = weightKg;
            so.FindProperty("color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static void BuildJerrycan(string name, Transform parent, Vector3 position, Quaternion rotation, Material material, Palette p, FuelSettings fuel)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.23f, 0f);
            box.size = new Vector3(0.3f, 0.46f, 0.16f);
            BoxMinMax("Cuerpo", go.transform, new Vector3(-0.15f, 0f, -0.08f), new Vector3(0.15f, 0.4f, 0.08f), material, false);
            BoxMinMax("Asa", go.transform, new Vector3(-0.08f, 0.4f, -0.03f), new Vector3(0.04f, 0.46f, 0.03f), material, false);
            BoxMinMax("Pico", go.transform, new Vector3(0.06f, 0.4f, -0.025f), new Vector3(0.13f, 0.45f, 0.025f), p.Fuel, false);
            var body = go.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var carryable = go.AddComponent<Carryable>();
            var carryableSo = new SerializedObject(carryable);
            carryableSo.FindProperty("oneHanded").boolValue = true; // una mano libre para la sonda portátil
            carryableSo.ApplyModifiedPropertiesWithoutUndo();
            SetReference(go.AddComponent<FuelCan>(), "settings", fuel);
        }

        // Camioneta enterrada a medias en la nieve, 38 m adelante del tren y 14 m a la derecha. Su tanque
        // (zona amarilla en el costado) es la fuente: FuelSource con sifón.
        private static void BuildAbandonedVehicle(Transform train, Palette p, FuelSettings fuel)
        {
            Material paint = Mat("Vehiculo", new Color(0.27f, 0.34f, 0.3f));
            Vector3 spot = train.position + train.forward * 38f + train.right * 14f;
            var root = Group("Vehiculo_Abandonado", null);
            root.position = new Vector3(spot.x, 0f, spot.z);
            root.rotation = Quaternion.Euler(0f, train.eulerAngles.y + 28f, 3f);
            BoxMinMax("Caja", root, new Vector3(-1.1f, 0.15f, -3.2f), new Vector3(1.1f, 1.45f, 0.4f), paint);
            BoxMinMax("Cabina", root, new Vector3(-1.05f, 0.15f, 0.4f), new Vector3(1.05f, 1.75f, 2.0f), paint);
            BoxMinMax("Capot", root, new Vector3(-1.05f, 0.15f, 2.0f), new Vector3(1.05f, 1.15f, 3.1f), paint);
            BoxMinMax("Parabrisas", root, new Vector3(-0.95f, 1.0f, 1.95f), new Vector3(0.95f, 1.6f, 2.05f), p.Track, false);

            // Tapa y zona del tanque, en el costado derecho (+x del vehículo), detrás de la cabina.
            var tank = Group("Tanque", root);
            tank.localPosition = new Vector3(1.1f, 0.8f, -0.4f);
            BoxMinMax("Tapa", tank, new Vector3(0f, -0.1f, -0.1f), new Vector3(0.06f, 0.1f, 0.1f), p.Fuel, false);
            var zone = BoxMinMax("Zona_Uso", tank, new Vector3(0f, -0.5f, -0.6f), new Vector3(0.8f, 0.5f, 0.6f), p.Fuel);
            zone.GetComponent<Collider>().isTrigger = true;
            Object.DestroyImmediate(zone.GetComponent<MeshRenderer>());
            var source = tank.gameObject.AddComponent<FuelSource>();
            SetReference(source, "settings", fuel);
            var so = new SerializedObject(source);
            so.FindProperty("stableId").stringValue = "fuel_prueba_vehiculo_001";
            so.FindProperty("method").enumValueIndex = (int)FuelSource.Method.Siphon;
            so.FindProperty("displayName").stringValue = "vehículo";
            so.FindProperty("liters").floatValue = fuel.AbandonedVehicleLiters;
            so.ApplyModifiedPropertiesWithoutUndo();
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

        // ---------------------------------------------------------------- Señales de sonda (M5)

        // Tres señales preparadas a mano (sección 21.2: "1–3 señales"), a 150–400 m de la vía, con un
        // marcador simple en la nieve (los POIs reales son del M6). Cada una con su ancla en la vía.
        private static void BuildProbeSignals(Palette p)
        {
            var root = new GameObject("Senales_Sonda").transform;
            RailSegment straight1a = GameObject.Find("Tramo_1a_Recta").GetComponent<RailSegment>();
            RailSegment straight1b = GameObject.Find("Tramo_1b_Recta").GetComponent<RailSegment>();
            RailSegment straight3 = GameObject.Find("Tramo_3_Recta").GetComponent<RailSegment>();

            Signal("poi_prueba_rail1a_001", ProbeSignalType.EstructuraBajoNieve, 1f, new Vector3(40f, 0f, -200f), straight1a, root,
                marker => BuildStation(marker, p));
            Signal("poi_prueba_rail1b_002", ProbeSignalType.MasaMetalica, 1f, new Vector3(480f, 0f, 150f), straight1b, root,
                marker =>
                {
                    BoxMinMax("Restos_Metalicos", marker, new Vector3(-1.5f, 0f, -3.5f), new Vector3(1.5f, 2.2f, 3.5f), p.Track);
                    // El metal interfiere la sonda portátil de cerca (sección 13.2), para probar las interferencias.
                    var zone = new SerializedObject(marker.gameObject.AddComponent<ProbeInterference>());
                    zone.FindProperty("radiusMeters").floatValue = 40f;
                    zone.FindProperty("strength").floatValue = 0.7f;
                    zone.ApplyModifiedPropertiesWithoutUndo();
                });
            Signal("poi_prueba_rail3_003", ProbeSignalType.CalorResidual, 0.25f, new Vector3(-520f, 0f, 50f), straight3, root,
                marker =>
                {
                    BoxMinMax("Refugio", marker, new Vector3(-1.5f, 0f, -1.5f), new Vector3(1.5f, 2.4f, 1.5f), p.Body);
                    var light = new GameObject("Calor").AddComponent<Light>();
                    light.transform.SetParent(marker, false);
                    light.transform.localPosition = new Vector3(0f, 1.2f, -1.8f);
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.6f, 0.3f);
                    light.range = 6f;
                    light.intensity = 3f;
                });
        }

        // ---------------------------------------------------------------- Estación de mantenimiento (M6)

        // Primer POI (sección 20.3 a 20.7), hecho a mano: pequeña estación de la Red Blanca, medio enterrada en
        // nieve, junto a la vía (la puerta mira a la vía, hacia +x). Una sala principal con el depósito de
        // combustible, la bomba manual vieja y un poco de material, y una segunda sala vacía (la tentación
        // opcional de 20.6: el blueprint llega en M9). Sin calefacción: frío, pero sin viento.
        private static void BuildStation(Transform marker, Palette p)
        {
            FuelSettings fuel = LoadOrCreateAsset<FuelSettings>(FuelSettingsPath);
            Material concrete = Mat("Estacion_Muro", new Color(0.5f, 0.52f, 0.54f));
            Material floorMat = Mat("Estacion_Piso", new Color(0.3f, 0.3f, 0.32f));
            Material roofMat = Mat("Estacion_Techo", new Color(0.32f, 0.33f, 0.36f));
            Material doorMat = Mat("Estacion_Puerta", new Color(0.38f, 0.5f, 0.45f));
            Material iceMat = Mat("Hielo", new Color(0.72f, 0.88f, 0.97f));

            var station = Group("Estacion_Mantenimiento", marker);

            // Cáscara: piso, cuatro paredes (puerta al este), techo y un tabique con vano entre las dos salas.
            BoxMinMax("Piso", station, new Vector3(-4.1f, 0f, -3.1f), new Vector3(4.1f, 0.05f, 3.1f), floorMat);
            var doorway = new List<Opening> { new Opening(-0.55f, 0.55f, 0f, 2.1f) };
            WallAlongZ("Pared_Este", station, 4.05f, -3.1f, 3.1f, 0f, 2.8f, doorway, concrete);
            WallAlongZ("Pared_Oeste", station, -4.05f, -3.1f, 3.1f, 0f, 2.8f, new List<Opening>(), concrete);
            WallAlongX("Pared_Norte", station, 3.05f, -4.1f, 4.1f, 0f, 2.8f, new List<Opening>(), concrete);
            WallAlongX("Pared_Sur", station, -3.05f, -4.1f, 4.1f, 0f, 2.8f, new List<Opening>(), concrete);
            WallAlongZ("Tabique", station, -1.5f, -3.0f, 3.0f, 0f, 2.8f, doorway, concrete);
            BoxMinMax("Techo", station, new Vector3(-4.3f, 2.8f, -3.3f), new Vector3(4.3f, 3.0f, 3.3f), roofMat);

            // Medio enterrada: nieve acumulada contra el lado norte y el oeste y sobre la mitad del techo.
            BoxMinMax("Nieve_Techo", station, new Vector3(-4.3f, 3.0f, -3.3f), new Vector3(0.5f, 3.4f, 3.3f), p.Snow);
            var north = BoxMinMax("Nieve_Norte", station, new Vector3(-5f, -1.2f, -2.5f), new Vector3(5f, 1.2f, 2.5f), p.Snow);
            north.transform.localPosition = new Vector3(0f, 0.9f, 5.3f);
            north.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            var west = BoxMinMax("Nieve_Oeste", station, new Vector3(-2.5f, -1.2f, -4f), new Vector3(2.5f, 1.2f, 4f), p.Snow);
            west.transform.localPosition = new Vector3(-6.3f, 0.9f, 0f);
            west.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);

            // Puerta corrediza por afuera de la pared (como las del tren), cerrada, y el hielo que la traba.
            var door = Group("Puerta", station);
            door.localPosition = new Vector3(4.1f, 0f, 0f);
            var leaf = Group("Hoja", door);
            BoxMinMax("Panel", leaf, new Vector3(0.01f, 0f, -0.57f), new Vector3(0.07f, 2.12f, 0.57f), doorMat);
            BoxMinMax("Manija", leaf, new Vector3(0.07f, 0.95f, 0.35f), new Vector3(0.1f, 1.05f, 0.5f), p.Track, false);
            var doorZone = BoxMinMax("Zona_Uso", door, new Vector3(-0.15f, 0.05f, -0.5f), new Vector3(0.15f, 2.05f, 0.5f), doorMat);
            doorZone.GetComponent<Collider>().isTrigger = true;
            Object.DestroyImmediate(doorZone.GetComponent<MeshRenderer>());
            var sliding = door.gameObject.AddComponent<SlidingDoor>();
            SetReference(sliding, "leaf", leaf);
            var slidingSo = new SerializedObject(sliding);
            slidingSo.FindProperty("openOffset").vector3Value = new Vector3(0f, 0f, -1.15f);
            slidingSo.FindProperty("doorwayCenter").vector3Value = new Vector3(0f, 1.05f, 0f);
            slidingSo.FindProperty("doorwayHalfExtents").vector3Value = new Vector3(0.35f, 1.0f, 0.55f);
            slidingSo.ApplyModifiedPropertiesWithoutUndo();

            // Hielo grueso en el marco: más grueso que la zona de uso de la puerta, así se apunta primero al hielo.
            var ice = Group("Hielo", station);
            ice.localPosition = new Vector3(4.17f, 0f, 0f);
            BoxMinMax("Bloque_Principal", ice, new Vector3(0f, 0f, -0.64f), new Vector3(0.3f, 2.2f, 0.64f), iceMat);
            BoxMinMax("Bloque_Pie", ice, new Vector3(0f, 0f, -0.75f), new Vector3(0.42f, 0.9f, 0.5f), iceMat);
            BoxMinMax("Bloque_Alto", ice, new Vector3(0f, 1.4f, -0.3f), new Vector3(0.38f, 2.3f, 0.78f), iceMat);
            var frozen = ice.gameObject.AddComponent<FrozenDoor>();
            var frozenSo = new SerializedObject(frozen);
            frozenSo.FindProperty("stableId").stringValue = "door_prueba_estacion_001";
            frozenSo.ApplyModifiedPropertiesWithoutUndo();

            // Sala principal: depósito de combustible contra la pared norte (se saca con la bomba manual).
            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = "Deposito";
            tank.transform.SetParent(station, false);
            tank.transform.localPosition = new Vector3(1.8f, 0.85f, 2.1f);
            tank.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tank.transform.localScale = new Vector3(1.3f, 1.7f, 1.3f);
            tank.GetComponent<MeshRenderer>().sharedMaterial = p.Track;
            foreach (float legX in new[] { 0.7f, 2.9f })
                BoxMinMax("Pata_Deposito", station, new Vector3(legX - 0.1f, 0.05f, 1.7f), new Vector3(legX + 0.1f, 0.3f, 2.5f), p.Track);
            var valve = Group("Valvula", station);
            valve.localPosition = new Vector3(1.8f, 0.9f, 1.42f);
            BoxMinMax("Rueda", valve, new Vector3(-0.1f, -0.1f, -0.08f), new Vector3(0.1f, 0.1f, 0.0f), p.Fuel, false);
            var depot = tank.AddComponent<FuelSource>(); // todo el tanque se puede apuntar, no solo la válvula
            SetReference(depot, "settings", fuel);
            var depotSo = new SerializedObject(depot);
            depotSo.FindProperty("stableId").stringValue = "fuel_estacion_deposito_001";
            depotSo.FindProperty("method").enumValueIndex = (int)FuelSource.Method.HandPump;
            depotSo.FindProperty("displayName").stringValue = "depósito";
            depotSo.FindProperty("liters").floatValue = fuel.StationDepotLiters;
            depotSo.ApplyModifiedPropertiesWithoutUndo();

            // Mesa de trabajo contra la pared sur: la bomba manual vieja y una ración.
            BoxMinMax("Mesa", station, new Vector3(0.4f, 0f, -2.95f), new Vector3(2.6f, 0.85f, -2.3f), p.Furniture);
            var pump = Group("Bomba_Manual", station);
            pump.localPosition = new Vector3(1.2f, 0.85f, -2.62f);
            BoxMinMax("Cuerpo", pump, new Vector3(-0.08f, 0f, -0.08f), new Vector3(0.08f, 0.32f, 0.08f), p.Track, false);
            BoxMinMax("Palanca", pump, new Vector3(-0.22f, 0.3f, -0.02f), new Vector3(0.1f, 0.34f, 0.02f), p.Fuel, false);
            BoxMinMax("Manguera", pump, new Vector3(0.08f, 0.02f, -0.02f), new Vector3(0.3f, 0.06f, 0.02f), p.Track, false);
            var pumpZone = BoxMinMax("Zona_Uso", pump, new Vector3(-0.3f, 0f, -0.3f), new Vector3(0.35f, 0.6f, 0.3f), p.Track);
            pumpZone.GetComponent<Collider>().isTrigger = true;
            Object.DestroyImmediate(pumpZone.GetComponent<MeshRenderer>());
            var pumpPickup = new SerializedObject(pump.gameObject.AddComponent<GearPickup>());
            pumpPickup.FindProperty("gear").enumValueIndex = (int)GearId.HandPump;
            pumpPickup.FindProperty("displayName").stringValue = "bomba manual";
            pumpPickup.ApplyModifiedPropertiesWithoutUndo();

            var ration = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ration.name = "Racion_Estacion";
            ration.transform.SetParent(station, false);
            ration.transform.localPosition = new Vector3(2.1f, 0.9f, -2.62f);
            ration.transform.localScale = new Vector3(0.18f, 0.1f, 0.12f);
            ration.GetComponent<MeshRenderer>().sharedMaterial = p.Food;
            var rationBody = ration.AddComponent<Rigidbody>();
            rationBody.mass = 0.4f;
            rationBody.interpolation = RigidbodyInterpolation.Interpolate;
            ration.AddComponent<FoodItem>();

            // Material con lógica de lugar (sección 15, regla de loot; sección 9): objetos que se desmontan con la
            // barreta (un pallet del taller, un armario metálico, un colchón viejo, un tablero eléctrico) y unos
            // pocos bultos ya sueltos (tablones, tela, chapas). Los usos de los materiales son de M8 y M9.
            MaterialSet materials = LoadMaterials();
            BackpackSettings backpackSettings = LoadBackpackSettings();
            SalvageRecipe palletRecipe = LoadOrCreateRecipe("Pallet", "pallet", 3f, (materials.Wood, 4));
            SalvageRecipe lockerRecipe = LoadOrCreateRecipe("ArmarioMetalico", "armario metálico", 6f, (materials.Metal, 3));
            SalvageRecipe mattressRecipe = LoadOrCreateRecipe("Colchon", "colchón", 4f, (materials.Cloth, 3));
            SalvageRecipe panelRecipe = LoadOrCreateRecipe("TableroElectrico", "tablero eléctrico", 5f, (materials.Parts, 2));
            BuildPallet(station, new Vector3(-0.8f, 0f, 1.2f), 12f, "salv_estacion_pallet_001", palletRecipe, backpackSettings, p);
            BuildLocker(station, new Vector3(3.5f, 0f, -2.6f), 0f, "salv_estacion_armario_001", lockerRecipe, backpackSettings, p);
            BuildMattress(station, new Vector3(-0.55f, 0f, -2.3f), 0f, "salv_estacion_colchon_001", mattressRecipe, backpackSettings);
            BuildPowerPanel(station, new Vector3(3.8f, 0f, 1.0f), 90f, "salv_estacion_tablero_001", panelRecipe, backpackSettings);
            PlaceBundle(station, backpackSettings, materials.Wood, 2, new Vector3(-1.1f, 0.3f, -1.3f));
            PlaceBundle(station, backpackSettings, materials.Metal, 2, new Vector3(0.5f, 0.3f, -0.3f));
            PlaceBundle(station, backpackSettings, materials.Cloth, 2, new Vector3(2.8f, 0.3f, -1.4f));

            // Segunda sala (oeste): vacía por ahora, con un escritorio. Reservada para la tentación opcional de 20.6.
            var annex = Group("Sala_Opcional", station);
            BoxMinMax("Escritorio", annex, new Vector3(-3.9f, 0f, -0.6f), new Vector3(-3.2f, 0.8f, 0.6f), p.Furniture);

            // Luz de emergencia tenue (sin energía, pero hay que poder ver adentro).
            var lamp = new GameObject("Luz_Emergencia").AddComponent<Light>();
            lamp.transform.SetParent(station, false);
            lamp.transform.localPosition = new Vector3(0.5f, 2.2f, 0f);
            lamp.type = LightType.Point;
            lamp.color = new Color(0.75f, 0.85f, 1f);
            lamp.range = 7f;
            lamp.intensity = 0.8f;
            lamp.shadows = LightShadows.None;

            // Frío sin viento adentro, y la sonda pierde un poco de señal (paredes gruesas y metal).
            var thermal = Group("Zona_Termica", station);
            var thermalSo = new SerializedObject(thermal.gameObject.AddComponent<ThermalZone>());
            thermalSo.FindProperty("center").vector3Value = new Vector3(0f, 1.4f, 0f);
            thermalSo.FindProperty("size").vector3Value = new Vector3(8.0f, 2.8f, 6.0f);
            thermalSo.ApplyModifiedPropertiesWithoutUndo();
            var interference = new SerializedObject(marker.gameObject.AddComponent<ProbeInterference>());
            interference.FindProperty("radiusMeters").floatValue = 10f;
            interference.FindProperty("strength").floatValue = 0.35f;
            interference.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Objetos desmontables (M7)

        private static BackpackSettings LoadBackpackSettings()
        {
            var settings = LoadOrCreateAsset<BackpackSettings>(BackpackSettingsPath);
            var so = new SerializedObject(settings);
            if (so.FindProperty("bundleMaterial").objectReferenceValue == null)
            {
                so.FindProperty("bundleMaterial").objectReferenceValue = Mat("Bulto", Color.white);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return settings;
        }

        // Receta de desmontaje como archivo de datos: se crea una sola vez (si se ajusta en el archivo, no se pisa).
        private static SalvageRecipe LoadOrCreateRecipe(string fileName, string displayName, float seconds, params (ItemDefinition item, int count)[] yields)
        {
            string path = $"{ItemsFolderPath}/Desmontaje_{fileName}.asset";
            var recipe = AssetDatabase.LoadAssetAtPath<SalvageRecipe>(path);
            if (recipe != null) return recipe;
            recipe = ScriptableObject.CreateInstance<SalvageRecipe>();
            AssetDatabase.CreateAsset(recipe, path);
            var so = new SerializedObject(recipe);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredTool").enumValueIndex = (int)GearId.Crowbar;
            so.FindProperty("salvageSeconds").floatValue = seconds;
            var list = so.FindProperty("yields");
            list.arraySize = yields.Length;
            for (int i = 0; i < yields.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue = yields[i].item;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = yields[i].count;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return recipe;
        }

        // Raíz de un objeto desmontable: pivote en la base, con la receta y el ID estable.
        private static Transform NewSalvageProp(string name, Transform parent, Vector3 localPosition, float yaw, SalvageRecipe recipe, BackpackSettings settings, string id)
        {
            var root = Group(name, parent);
            root.localPosition = localPosition;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var prop = root.gameObject.AddComponent<SalvageProp>();
            SetReference(prop, "recipe", recipe);
            SetReference(prop, "backpackSettings", settings);
            var so = new SerializedObject(prop);
            so.FindProperty("stableId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        // Pallet: tres travesaños abajo y cinco tablas arriba.
        private static Transform BuildPallet(Transform parent, Vector3 localPosition, float yaw, string id, SalvageRecipe recipe, BackpackSettings settings, Palette p)
        {
            var root = NewSalvageProp("Pallet", parent, localPosition, yaw, recipe, settings, id);
            for (int i = 0; i < 3; i++)
                BoxMinMax("Travesano", root, new Vector3(-0.6f, 0f, -0.5f + i * 0.45f), new Vector3(0.6f, 0.09f, -0.4f + i * 0.45f), p.Box);
            for (int i = 0; i < 5; i++)
                BoxMinMax("Tabla", root, new Vector3(-0.6f, 0.09f, -0.5f + i * 0.2f), new Vector3(0.6f, 0.14f, -0.36f + i * 0.2f), p.Box);
            return root;
        }

        // Armario metálico con la puerta marcada.
        private static Transform BuildLocker(Transform parent, Vector3 localPosition, float yaw, string id, SalvageRecipe recipe, BackpackSettings settings, Palette p)
        {
            Material paint = Mat("Armario", new Color(0.33f, 0.42f, 0.38f));
            var root = NewSalvageProp("Armario_Metalico", parent, localPosition, yaw, recipe, settings, id);
            BoxMinMax("Cuerpo", root, new Vector3(-0.4f, 0f, -0.25f), new Vector3(0.4f, 1.9f, 0.25f), paint);
            BoxMinMax("Puerta_Linea", root, new Vector3(-0.01f, 0.1f, -0.27f), new Vector3(0.01f, 1.8f, -0.25f), p.Track, false);
            BoxMinMax("Manija", root, new Vector3(0.1f, 0.9f, -0.3f), new Vector3(0.16f, 1.1f, -0.25f), p.Track, false);
            return root;
        }

        // Colchón viejo en el piso.
        private static Transform BuildMattress(Transform parent, Vector3 localPosition, float yaw, string id, SalvageRecipe recipe, BackpackSettings settings)
        {
            Material cloth = Mat("Colchon", new Color(0.55f, 0.55f, 0.5f));
            Material stripe = Mat("Colchon_Franja", new Color(0.4f, 0.42f, 0.5f));
            var root = NewSalvageProp("Colchon", parent, localPosition, yaw, recipe, settings, id);
            BoxMinMax("Cuerpo", root, new Vector3(-0.9f, 0f, -0.4f), new Vector3(0.9f, 0.18f, 0.4f), cloth);
            BoxMinMax("Franja", root, new Vector3(-0.2f, 0.18f, -0.4f), new Vector3(0.2f, 0.19f, 0.4f), stripe, false);
            return root;
        }

        // Tablero eléctrico de pie contra la pared, con su luz apagada.
        private static Transform BuildPowerPanel(Transform parent, Vector3 localPosition, float yaw, string id, SalvageRecipe recipe, BackpackSettings settings)
        {
            Material box = Mat("Tablero", new Color(0.25f, 0.3f, 0.28f));
            Material lamp = UnlitMat("Tablero_Luz", new Color(0.1f, 0.2f, 0.12f));
            var root = NewSalvageProp("Tablero_Electrico", parent, localPosition, yaw, recipe, settings, id);
            BoxMinMax("Cuerpo", root, new Vector3(-0.3f, 0f, -0.15f), new Vector3(0.3f, 1.3f, 0.15f), box);
            BoxMinMax("Luz", root, new Vector3(-0.04f, 1.05f, -0.17f), new Vector3(0.04f, 1.12f, -0.15f), lamp, false);
            BoxMinMax("Tapa", root, new Vector3(-0.22f, 0.3f, -0.17f), new Vector3(0.22f, 0.9f, -0.15f), box, false);
            return root;
        }

        // Un bulto de material ya suelto en un POI (se arma con la misma pieza que lo que se suelta de la mochila).
        private static void PlaceBundle(Transform parent, BackpackSettings settings, ItemDefinition item, int count, Vector3 localPosition)
        {
            MaterialBundle bundle = MaterialBundle.Spawn(settings, item, count, parent.TransformPoint(localPosition), Vector3.zero);
            bundle.transform.SetParent(parent, true);
        }

        private static void Signal(string id, ProbeSignalType type, float strength, Vector3 position, RailSegment segment,
            Transform root, System.Action<Transform> buildMarker)
        {
            var go = new GameObject($"Senal_{type}");
            go.transform.SetParent(root, false);
            go.transform.position = position;
            buildMarker(go.transform);
            var signal = go.AddComponent<ProbeSignal>();

            // Ancla: el punto del tramo más cercano, de qué lado está y a qué distancia.
            float bestDistance = 0f, bestSqr = float.PositiveInfinity;
            for (float d = 0f; d <= segment.Length; d += 1f)
            {
                float sqr = (segment.GetPosition(d) - position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; bestDistance = d; }
            }
            Vector3 onRail = segment.GetPosition(bestDistance);
            Vector3 right = Vector3.Cross(Vector3.up, segment.GetDirection(bestDistance));
            var so = new SerializedObject(signal);
            so.FindProperty("stableId").stringValue = id;
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("strength").floatValue = strength;
            so.FindProperty("anchorSegment").objectReferenceValue = segment;
            so.FindProperty("anchorDistanceMeters").floatValue = bestDistance;
            so.FindProperty("anchorSide").intValue = Vector3.Dot(position - onRail, right) >= 0f ? 1 : -1;
            Vector3 lateral = position - onRail;
            lateral.y = 0f;
            so.FindProperty("anchorLateralMeters").floatValue = lateral.magnitude;
            so.ApplyModifiedPropertiesWithoutUndo();
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


            // Interior dividido en zonas reservadas para los sistemas de la V1.
            // Son bloques de lugar (placeholders): no tienen funcionamiento todavía.
            var interior = Group("Interior", root.transform);

            var cab = Group("Zona_Cabina", interior);
            BuildCabControls(root, cab, p);
            BuildFuelSystem(root, cab, p);
            BoxMinMax("Cabina_Asiento", cab, new Vector3(0.6f, FloorTop, 7.6f), new Vector3(1.1f, FloorTop + 0.5f, 8.1f), p.Furniture);
            BuildProbeConsole(root, cab, p);

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

        // Combustible del tren (M6): el tanque en la raíz del tren y el medidor en el pupitre, a la derecha
        // del velocímetro: aguja (vacío a la izquierda, lleno a la derecha) y luz de reserva.
        // El motor deja de dar potencia con el tanque vacío; el gasto lo agrega BuildSurvival (necesita el reloj).
        private static void BuildFuelSystem(GameObject trainRoot, Transform cab, Palette p)
        {
            var tank = trainRoot.AddComponent<FuelTank>();
            SetReference(tank, "settings", LoadOrCreateAsset<FuelSettings>(FuelSettingsPath));
            SetReference(trainRoot.GetComponent<TrainDynamics>(), "fuel", tank);

            // Boca de carga exterior (zona reservada de la V1), en el costado derecho del chasis, a la altura
            // de la cintura de quien está parado en la nieve. La zona transparente de adelante la hace fácil
            // de apuntar: con un bidón en la mano, mantener E carga el tanque.
            var port = Group("Exterior_BocaCombustible", trainRoot.transform.Find("Exterior"));
            port.localPosition = new Vector3(BodyHalfWidth - 0.3f, 0.55f, -8.6f);
            BoxMinMax("Tapa", port, new Vector3(0f, -0.17f, -0.3f), new Vector3(0.1f, 0.17f, 0.3f), p.Fuel);
            BoxMinMax("Tapa_Asa", port, new Vector3(0.1f, -0.03f, -0.1f), new Vector3(0.14f, 0.03f, 0.1f), p.Track, false);
            var portZone = BoxMinMax("Zona_Uso", port, new Vector3(0f, -0.35f, -0.5f), new Vector3(0.7f, 0.35f, 0.5f), p.Fuel);
            portZone.GetComponent<Collider>().isTrigger = true;
            Object.DestroyImmediate(portZone.GetComponent<MeshRenderer>());
            SetReference(port.gameObject.AddComponent<FuelPort>(), "tank", tank);

            Transform desk = cab.Find("Cabina_Pupitre");
            float top = FloorTop + 1.0f;
            const float sweep = 60f;
            var gauge = Group("Medidor_Combustible", desk);
            gauge.localPosition = new Vector3(0.72f, top + 0.22f, 8.95f);
            BoxMinMax("Soporte", gauge, new Vector3(-0.03f, -0.22f, 0.02f), new Vector3(0.03f, 0f, 0.08f), p.Track, false);
            var face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            face.name = "Dial";
            Object.DestroyImmediate(face.GetComponent<Collider>());
            face.transform.SetParent(gauge, false);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            face.transform.localScale = new Vector3(0.22f, 0.01f, 0.22f);
            face.GetComponent<MeshRenderer>().sharedMaterial = p.Furniture;
            for (int i = 0; i <= 4; i++) // vacío (rojo), 1/4, 1/2, 3/4, lleno
            {
                var tick = Group($"Marca_{i}", gauge);
                tick.localPosition = new Vector3(0f, 0f, -0.012f);
                tick.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(sweep, -sweep, i / 4f));
                BoxMinMax("Raya", tick, new Vector3(-0.004f, 0.075f, -0.002f), new Vector3(0.004f, 0.105f, 0.002f),
                    i == 0 ? p.PostMarker : p.Track, false);
            }
            Label("E", gauge, new Vector3(-0.072f, 0.032f, -0.013f), facingDriver: true);
            Label("F", gauge, new Vector3(0.072f, 0.032f, -0.013f), facingDriver: true);
            Label("COMB.", gauge, new Vector3(0f, -0.075f, -0.013f), facingDriver: true);
            var needle = Group("Aguja_Eje", gauge);
            needle.localPosition = new Vector3(0f, 0f, -0.016f);
            BoxMinMax("Aguja", needle, new Vector3(-0.004f, -0.015f, -0.002f), new Vector3(0.004f, 0.095f, 0.002f), p.PostMarker, false);
            var lamp = BoxMinMax("Luz_Reserva", gauge, new Vector3(-0.012f, -0.05f, -0.016f), new Vector3(0.012f, -0.03f, -0.012f),
                UnlitMat("Luz_Reserva", new Color(0.22f, 0.04f, 0.04f)), false);

            var display = gauge.gameObject.AddComponent<FuelGauge>();
            SetReference(display, "tank", tank);
            SetReference(display, "needlePivot", needle);
            SetReference(display, "lamp", lamp.GetComponent<Renderer>());
            var displaySo = new SerializedObject(display);
            displaySo.FindProperty("sweepDegrees").floatValue = sweep;
            displaySo.ApplyModifiedPropertiesWithoutUndo();
        }

        // Consola de la sonda principal (M5): mueble con una pantalla tipo CRT verde que mira al pasillo.
        // Agrega a la raíz del tren el ProbeScanner (los cálculos) y a la consola la pantalla (ProbeConsole).
        private static void BuildProbeConsole(GameObject trainRoot, Transform cab, Palette p)
        {
            var console = Group("Cabina_ConsolaSonda", cab);
            float top = FloorTop + 1.2f;
            BoxMinMax("Mueble", console, new Vector3(-InnerHalfWidth, FloorTop, 6.8f), new Vector3(-(InnerHalfWidth - 0.6f), top, 7.6f), p.Controls);

            Material back = UnlitMat("Pantalla_Fondo", new Color(0.02f, 0.06f, 0.035f));
            Material trace = UnlitMat("Pantalla_Trazo", new Color(0.45f, 1f, 0.55f));
            Material dim = UnlitMat("Pantalla_Trazo_Tenue", new Color(0.12f, 0.32f, 0.16f));

            // Pantalla inclinada, mirando al pasillo (+x del tren) y un poco hacia arriba.
            var screen = Group("Pantalla", console);
            screen.localPosition = new Vector3(-InnerHalfWidth + 0.32f, top + 0.21f, 7.2f);
            screen.localRotation = Quaternion.LookRotation(Vector3.left) * Quaternion.Euler(20f, 0f, 0f);
            BoxMinMax("Carcasa", screen, new Vector3(-0.27f, -0.22f, 0.012f), new Vector3(0.27f, 0.22f, 0.07f), p.Track);
            BoxMinMax("Vidrio", screen, new Vector3(-0.25f, -0.2f, 0f), new Vector3(0.25f, 0.2f, 0.012f), back);

            // Radar: centro, anillos (1,25 y 2,5 km), el tren apuntando arriba (= adelante) y el barrido.
            const float radius = 0.13f;
            var center = Group("Radar_Centro", screen);
            center.localPosition = new Vector3(0f, 0.045f, -0.002f);
            foreach (float ring in new[] { radius * 0.5f, radius })
                for (int i = 0; i < 32; i++)
                {
                    float a = i / 32f * Mathf.PI * 2f;
                    Vector3 c = center.localPosition + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * ring;
                    BoxMinMax("Anillo", screen, c - Vector3.one * 0.002f, c + Vector3.one * 0.002f, dim, false);
                }
            BoxMinMax("Tren", screen, center.localPosition + new Vector3(-0.004f, -0.012f, -0.001f),
                center.localPosition + new Vector3(0.004f, 0.012f, 0.001f), trace, false);
            var sweep = Group("Barrido", screen);
            sweep.localPosition = center.localPosition;
            BoxMinMax("Linea", sweep, new Vector3(-0.0012f, 0f, -0.0012f), new Vector3(0.0012f, radius, 0.0005f), trace, false);
            var blips = new Transform[5];
            for (int i = 0; i < blips.Length; i++)
                blips[i] = BoxMinMax($"Punto_{i}", screen, center.localPosition - Vector3.one * 0.006f, center.localPosition + Vector3.one * 0.006f, trace, false).transform;

            TextMesh ScreenText(string name, float y)
            {
                var go = new GameObject(name);
                go.transform.SetParent(screen, false);
                go.transform.localPosition = new Vector3(-0.235f, y, -0.003f);
                var mesh = go.AddComponent<TextMesh>();
                mesh.fontSize = 48;
                mesh.characterSize = 0.0032f;
                mesh.anchor = TextAnchor.UpperLeft;
                mesh.color = new Color(0.45f, 1f, 0.55f);
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                mesh.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                return mesh;
            }

            var scanner = trainRoot.AddComponent<ProbeScanner>();
            SetReference(scanner, "settings", LoadOrCreateAsset<ProbeSettings>(ProbeSettingsPath));
            SetReference(scanner, "train", trainRoot.GetComponent<TrainMover>());

            var display = console.gameObject.AddComponent<ProbeConsole>();
            SetReference(display, "scanner", scanner);
            SetReference(display, "radarCenter", center);
            SetReference(display, "sweep", sweep);
            var so = new SerializedObject(display);
            so.FindProperty("radarRadius").floatValue = radius;
            var list = so.FindProperty("blips");
            list.arraySize = blips.Length;
            for (int i = 0; i < blips.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = blips[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            SetReference(display, "header", ScreenText("Encabezado", 0.19f));
            SetReference(display, "line1", ScreenText("Linea_1", -0.105f));
            SetReference(display, "line2", ScreenText("Linea_2", -0.135f));
            SetReference(display, "line3", ScreenText("Linea_3", -0.165f));
        }

        // Sonda portátil (M5): aparato chico en la mano derecha, hijo de la cámara. Pantalla verde con un
        // dial (arriba = hacia donde mira el jugador), dos agujas, dos líneas de texto y la barra de señal.
        // Sin colisionadores ni sombras: es parte de la vista, no del mundo.
        private static void BuildPortableProbe(PlayerMotor motor, GameObject train, Palette p)
        {
            Transform head = motor.transform.Find("Cabeza");
            var device = Group("Sonda_Portatil", head);
            device.localRotation = Quaternion.Euler(28f, -8f, 0f); // inclinada hacia los ojos
            BoxMinMax("Cuerpo", device, new Vector3(-0.08f, -0.085f, 0.002f), new Vector3(0.08f, 0.075f, 0.032f), p.Controls, false);
            BoxMinMax("Pantalla", device, new Vector3(-0.07f, -0.075f, 0f), new Vector3(0.07f, 0.065f, 0.002f),
                UnlitMat("Pantalla_Fondo", new Color(0.02f, 0.06f, 0.035f)), false);

            Material dim = UnlitMat("Pantalla_Trazo_Tenue", new Color(0.12f, 0.32f, 0.16f));
            Material trace = UnlitMat("Pantalla_Trazo", new Color(0.45f, 1f, 0.55f));
            var center = new Vector3(0f, 0.02f, -0.0005f);
            const float radius = 0.038f;
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                Vector3 c = center + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * radius;
                float size = i == 0 ? 0.0028f : 0.0012f; // la marca de arriba (= adelante) más grande
                BoxMinMax("Dial", device, c - Vector3.one * size, c + Vector3.one * size, i == 0 ? trace : dim, false);
            }

            Transform Needle(string name, Color color, float length, float z)
            {
                var pivot = Group(name, device);
                pivot.localPosition = new Vector3(center.x, center.y, z);
                BoxMinMax("Aguja", pivot, new Vector3(-0.0016f, -0.004f, -0.0004f), new Vector3(0.0016f, length, 0.0004f),
                    UnlitMat(name, color), false);
                return pivot;
            }
            Transform trainNeedle = Needle("Sonda_Aguja_Tren", new Color(0.95f, 0.95f, 0.92f), radius * 0.9f, -0.001f);
            Transform targetNeedle = Needle("Sonda_Aguja_Objetivo", new Color(1f, 0.72f, 0.2f), radius * 0.75f, -0.002f);

            TextMesh Text(string name, Vector3 position, float size)
            {
                var go = new GameObject(name);
                go.transform.SetParent(device, false);
                go.transform.localPosition = position;
                var mesh = go.AddComponent<TextMesh>();
                mesh.fontSize = 48;
                mesh.characterSize = size;
                mesh.anchor = TextAnchor.UpperLeft;
                mesh.color = new Color(0.45f, 1f, 0.55f);
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                mesh.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                return mesh;
            }
            TextMesh trainText = Text("Texto_Tren", new Vector3(-0.064f, -0.022f, -0.001f), 0.0021f);
            TextMesh targetText = Text("Texto_Objetivo", new Vector3(-0.064f, -0.036f, -0.001f), 0.0021f);
            trainText.color = new Color(0.95f, 0.95f, 0.92f);
            targetText.color = new Color(1f, 0.72f, 0.2f);
            Text("Texto_Senal", new Vector3(-0.064f, -0.053f, -0.001f), 0.0017f).text = "SEÑAL";

            var bars = new Renderer[5];
            for (int i = 0; i < bars.Length; i++)
            {
                float x = 0.002f + i * 0.012f;
                bars[i] = BoxMinMax($"Barra_{i}", device, new Vector3(x, -0.066f, -0.001f),
                    new Vector3(x + 0.008f, -0.06f + i * 0.002f, -0.0002f), trace, false).GetComponent<Renderer>();
            }
            foreach (Renderer r in device.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            var probe = motor.gameObject.AddComponent<PortableProbe>();
            SetReference(probe, "settings", LoadOrCreateAsset<ProbeSettings>(ProbeSettingsPath));
            SetReference(probe, "scanner", train.GetComponent<ProbeScanner>());
            SetReference(probe, "head", head);
            SetReference(probe, "carrier", motor.GetComponent<PlayerCarrier>());
            SetReference(probe, "toggle", FindActionReference("Player/ToggleProbe"));
            SetReference(probe, "device", device);
            SetReference(probe, "trainNeedle", trainNeedle);
            SetReference(probe, "targetNeedle", targetNeedle);
            SetReference(probe, "trainText", trainText);
            SetReference(probe, "targetText", targetText);
            var so = new SerializedObject(probe);
            var list = so.FindProperty("qualityBars");
            list.arraySize = bars.Length;
            for (int i = 0; i < bars.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = bars[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material UnlitMat(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
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

        private static void BuildEnvironment(Palette p, float fogDensity)
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
            RenderSettings.fogDensity = fogDensity;
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
