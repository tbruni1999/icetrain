using System.Collections.Generic;
using System.Linq;
using SurvivalEnTren.Player;
using SurvivalEnTren.Rail;
using SurvivalEnTren.Spike;
using SurvivalEnTren.Train;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Construye la escena del spike M1 (tren estable): suelo nevado, vía en óvalo,
    /// postes de referencia y la locomotora greybox con sus zonas reservadas para la V1.
    /// Se ejecuta desde "Survival en tren > M1 > Construir escena Spike".
    /// La escena y el prefab se regeneran enteros cada vez: no editarlos a mano
    /// mientras dure el spike, cambiar este script.
    /// </summary>
    public static class M1SpikeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Spike/Spike_TrenEstable.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/Train/Locomotora_Greybox.prefab";
        private const string MaterialsFolder = "Assets/_Project/Materials/Greybox";
        private const string DriveSettingsPath = "Assets/_Project/Data/Train/TrainTestDriveSettings.asset";
        private const string PlayerSettingsPath = "Assets/_Project/Data/Player/PlayerMotorSettings.asset";
        private const string BoxPrefabPath = "Assets/_Project/Prefabs/Spike/Caja_Prueba.prefab";
        private const string InputPath = "Assets/_Project/Data/Input/IceTrainInput.inputactions";

        // Vía de prueba: un óvalo de dos rectas y dos curvas (~2,46 km por vuelta).
        private const float StraightLength = 600f;
        private const float CurveRadius = 200f;
        private const float CurvePointSpacing = 2f;
        private const float RailTopHeight = 0.6f; // suelo en 0; balasto + durmiente + riel

        // Locomotora. Coordenadas locales: y = 0 es la cara superior del riel, z+ es adelante.
        private const float BodyHalfWidth = 1.5f;
        private const float BodyHalfLength = 9.5f;
        private const float FloorTop = 1.0f;
        private const float RoofBottom = 3.4f;
        private const float WallThickness = 0.1f;
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
            public Material Snow, Ballast, Track, Body, Floor, Furniture, Controls, Fuel, Post, PostMarker, Box;
        }

        [MenuItem("Survival en tren/M1/Construir escena Spike (tren estable)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[M1] Salir del modo Play antes de construir la escena.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Assets/_Project", "Scenes");
            EnsureFolder("Assets/_Project/Scenes", "Spike");
            EnsureFolder("Assets/_Project", "Prefabs");
            EnsureFolder("Assets/_Project/Prefabs", "Train");
            EnsureFolder("Assets/_Project/Prefabs", "Spike");
            EnsureFolder("Assets/_Project", "Materials");
            EnsureFolder("Assets/_Project/Materials", "Greybox");
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder("Assets/_Project/Data", "Train");
            EnsureFolder("Assets/_Project/Data", "Player");

            // La escena nueva va primero: crearla descarga de memoria los assets recién
            // creados y las referencias a ellos quedarían vacías.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Palette palette = CreatePalette();
            TrainTestDriveSettings driveSettings = LoadOrCreateAsset<TrainTestDriveSettings>(DriveSettingsPath);
            GameObject locomotivePrefab = BuildLocomotivePrefab(palette);

            BuildEnvironment(palette);
            RailSegment firstSegment = BuildTrack(palette);
            BuildPosts(firstSegment, palette);

            var train = (GameObject)PrefabUtility.InstantiatePrefab(locomotivePrefab);
            var mover = train.GetComponent<TrainMover>();
            SetReference(mover, "startSegment", firstSegment);
            mover.SnapToRail();

            var controls = new GameObject("Spike_Controles");
            var driver = controls.AddComponent<TrainTestDriver>();
            SetReference(driver, "train", mover);
            SetReference(driver, "settings", driveSettings);
            SetReference(driver, "faster", FindActionReference("Debug/TrainFaster"));
            SetReference(driver, "slower", FindActionReference("Debug/TrainSlower"));
            SetReference(driver, "brake", FindActionReference("Debug/TrainBrake"));
            // El jugador arranca en la nieve, frente a la puerta derecha del tren.
            Vector3 door = train.transform.TransformPoint(new Vector3(BodyHalfWidth, 0f, DoorCenterZ));
            PlayerMotor motor = BuildPlayer(new Vector3(door.x + 6f, 0.05f, door.z), Quaternion.LookRotation(-train.transform.right, Vector3.up));

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

            var overlay = controls.AddComponent<SpikeOverlay>();
            SetReference(overlay, "driver", driver);
            SetReference(overlay, "motor", motor);
            SetReference(overlay, "metrics", metrics);
            SetReference(overlay, "propMetrics", propMetrics);
            SetReference(overlay, "resetMeasure", FindActionReference("Debug/ResetMeasure"));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[M1] Escena del spike construida en " + ScenePath);
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
            Box("Caja_Workbench_Chica", new Vector3(-1.0f, bench, 0.5f), Vector3.one * 0.3f, 3f);
            Box("Caja_Workbench_Herramientas", new Vector3(-1.0f, bench, -0.4f), new Vector3(0.25f, 0.2f, 0.5f), 5f);
            Box("Caja_Estante_Baja", new Vector3(1.2f, FloorTop + 0.13f, -1.5f), Vector3.one * 0.25f, 2f);
            Box("Caja_Estante_Media", new Vector3(1.2f, FloorTop + 0.73f, -2.0f), Vector3.one * 0.25f, 2f);
            Box("Caja_Estante_Alta", new Vector3(1.2f, FloorTop + 1.33f, -1.0f), Vector3.one * 0.25f, 2f);
            Box("Caja_Cama", new Vector3(0.95f, FloorTop + 0.45f, 4.5f), Vector3.one * 0.2f, 1f);
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
            BoxMinMax("Chasis", exterior, new Vector3(-1.2f, 0.25f, -9.3f), new Vector3(1.2f, FloorTop - 0.2f, 9.3f), p.Body);
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
            var frontOpenings = new List<Opening> { new Opening(-1.2f, 1.2f, WindowBottom, WindowTop + 0.1f) };
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

            BoxMinMax("Exterior_BocaCombustible", exterior, new Vector3(BodyHalfWidth, FloorTop + 0.3f, -8.7f), new Vector3(BodyHalfWidth + 0.1f, FloorTop + 0.6f, -8.3f), p.Fuel);

            // Interior dividido en zonas reservadas para los sistemas de la V1.
            // Son bloques de lugar (placeholders): no tienen funcionamiento todavía.
            var interior = Group("Interior", root.transform);

            var cab = Group("Zona_Cabina", interior);
            BoxMinMax("Cabina_PupitreControles", cab, new Vector3(-1.0f, FloorTop, 8.6f), new Vector3(1.0f, FloorTop + 1.0f, 9.3f), p.Controls);
            BoxMinMax("Cabina_Asiento", cab, new Vector3(-0.25f, FloorTop, 7.6f), new Vector3(0.25f, FloorTop + 0.5f, 8.1f), p.Furniture);
            BoxMinMax("Cabina_ConsolaSonda", cab, new Vector3(-1.4f, FloorTop, 6.8f), new Vector3(-0.8f, FloorTop + 1.2f, 7.6f), p.Controls);

            var rest = Group("Zona_Descanso", interior);
            BoxMinMax("Descanso_Cama", rest, new Vector3(0.5f, FloorTop, 3.5f), new Vector3(1.4f, FloorTop + 0.45f, 5.5f), p.Furniture);
            BoxMinMax("Descanso_Calefaccion", rest, new Vector3(-1.4f, FloorTop, 3.8f), new Vector3(-0.8f, FloorTop + 0.9f, 4.4f), p.Controls);

            var workshop = Group("Zona_Taller", interior);
            BoxMinMax("Taller_Workbench", workshop, new Vector3(-1.4f, FloorTop, -1.0f), new Vector3(-0.6f, FloorTop + 0.9f, 1.0f), p.Furniture);
            var shelf = Group("Taller_Estante", workshop);
            BoxMinMax("Lateral_A", shelf, new Vector3(1.0f, FloorTop, -2.53f), new Vector3(1.4f, FloorTop + 1.8f, -2.5f), p.Furniture);
            BoxMinMax("Lateral_B", shelf, new Vector3(1.0f, FloorTop, -0.53f), new Vector3(1.4f, FloorTop + 1.8f, -0.5f), p.Furniture);
            foreach (float h in new[] { 0.1f, 0.7f, 1.3f })
                BoxMinMax($"Repisa_{h:0.0}", shelf, new Vector3(1.0f, FloorTop + h, -2.5f), new Vector3(1.4f, FloorTop + h + 0.03f, -0.53f), p.Furniture);

            var cargo = Group("Zona_EntradaCarga", interior);
            BoxMinMax("Carga_Bidon_1", cargo, new Vector3(-1.4f, FloorTop, -7.4f), new Vector3(-1.05f, FloorTop + 0.55f, -7.05f), p.Fuel);
            BoxMinMax("Carga_Bidon_2", cargo, new Vector3(-1.0f, FloorTop, -7.4f), new Vector3(-0.65f, FloorTop + 0.55f, -7.05f), p.Fuel);

            var engine = Group("Zona_SalaMaquinas", interior);
            BoxMinMax("SalaMaquinas_Tabique", engine, new Vector3(-BodyHalfWidth + WallThickness, FloorTop, EngineRoomFrontZ - 0.1f), new Vector3(BodyHalfWidth - WallThickness, RoofBottom, EngineRoomFrontZ), p.Body);
            BoxMinMax("SalaMaquinas_MotorTanque", engine, new Vector3(-1.2f, FloorTop, -9.3f), new Vector3(1.2f, FloorTop + 2.0f, EngineRoomFrontZ - 0.2f), p.Track);

            AddInteriorLight(interior, new Vector3(0f, RoofBottom - 0.3f, 4f));
            AddInteriorLight(interior, new Vector3(0f, RoofBottom - 0.3f, -2.5f));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
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

        private static RailSegment BuildTrack(Palette p)
        {
            var track = new GameObject("Via");
            float h = RailTopHeight;
            float halfL = StraightLength * 0.5f;
            float r = CurveRadius;

            var straightEast = new List<Vector3> { new Vector3(r, h, -halfL), new Vector3(r, h, halfL) };
            var curveNorth = Arc(new Vector3(0f, h, halfL), r, 0f, Mathf.PI);
            var straightWest = new List<Vector3> { new Vector3(-r, h, halfL), new Vector3(-r, h, -halfL) };
            var curveSouth = Arc(new Vector3(0f, h, -halfL), r, Mathf.PI, 2f * Mathf.PI);

            RailSegment s1 = Segment("Tramo_1_Recta", track.transform, straightEast, p);
            RailSegment s2 = Segment("Tramo_2_Curva", track.transform, curveNorth, p);
            RailSegment s3 = Segment("Tramo_3_Recta", track.transform, straightWest, p);
            RailSegment s4 = Segment("Tramo_4_Curva", track.transform, curveSouth, p);
            s1.Connect(s2);
            s2.Connect(s3);
            s3.Connect(s4);
            s4.Connect(s1);
            foreach (var s in new[] { s1, s2, s3, s4 }) EditorUtility.SetDirty(s);
            return s1;
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
        private static void BuildPosts(RailSegment first, Palette p)
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
                Vector3 basePoint = at.WorldPosition + right * 3.5f;
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
