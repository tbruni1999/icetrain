using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SurvivalEnTren.Building;
using SurvivalEnTren.Crafting;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using SurvivalEnTren.Player;
using SurvivalEnTren.Poi;
using SurvivalEnTren.Probe;
using SurvivalEnTren.Rail;
using SurvivalEnTren.Survival;
using SurvivalEnTren.Train;
using SurvivalEnTren.World;
using SurvivalEnTren.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SurvivalEnTren.Save
{
    /// <summary>
    /// Guardar y cargar la partida (M12; Anexo B.4 y sección 25.2). F5 guarda, F9 carga.
    /// - Guarda un archivo JSON versionado en la carpeta de datos del juego (no PlayerPrefs, DECISIÓN TÉCNICA del Anexo B.4).
    /// - Qué se guarda: el jugador (lugar seguro, cuerpo, equipo, mochila, planos), el tren (posición en la vía, velocidad,
    ///   palancas, combustible, temperatura), las obras con lo aportado, los muebles con lo guardado, los cambios de vía,
    ///   lo desmontado, los bultos y objetos sueltos, los hielos rotos, las puertas, la comida y el combustible del mundo.
    /// - Cargar recarga la escena (la región hecha a mano, la "seed" de la V1) y aplica encima lo guardado: así lo que se
    ///   desmontó después de guardar vuelve a estar y lo que se había desmontado antes no reaparece (sin duplicar nada).
    /// - Lo que estaba adentro del tren se guarda relativo al tren; lo de afuera, en coordenadas absolutas del mundo
    ///   (descontando los re-centrados del origen flotante).
    /// - No es un objeto global: vive en la escena; lo único que cruza la recarga es la partida pedida (un dato).
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        // La partida pedida al cargar: la escena se recarga y el SaveSystem nuevo la aplica.
        private static SaveGame _pendingLoad;

        /// <summary>Cuántas cargas se aplicaron desde que arrancó el juego (lo usan las pruebas automáticas).</summary>
        public static int LoadsApplied { get; private set; }

        [Tooltip("Nombre del archivo de la partida (dentro de la carpeta de datos del juego, subcarpeta saves).")]
        [SerializeField] private string fileName = "partida.json";
        [Header("Jugador")]
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private PlayerVitals vitals;
        [SerializeField] private PlayerGear gear;
        [SerializeField] private Backpack backpack;
        [SerializeField] private BlueprintBook book;
        [SerializeField] private InventoryPanel inventory;
        [SerializeField] private WorkbenchPanel workbench;
        [Header("Tren y mundo")]
        [SerializeField] private TrainMover train;
        [SerializeField] private TrainControls controls;
        [SerializeField] private FuelTank tank;
        [SerializeField] private TrainInteriorClimate climate;
        [SerializeField] private GameClock clock;
        [SerializeField] private ProbeScanner scanner;
        [SerializeField] private FloatingOrigin origin;
        [Header("Para crear de nuevo lo que no está en la escena")]
        [SerializeField] private BackpackSettings backpackSettings;
        [Tooltip("El bidón que sale del banco de trabajo (los fabricados se vuelven a crear al cargar).")]
        [SerializeField] private GameObject jerrycanPrefab;
        [Tooltip("Todos los materiales y piezas que pueden estar en la mochila, los muebles o los bultos.")]
        [SerializeField] private ItemDefinition[] items = new ItemDefinition[0];
        [Header("Teclas")]
        [SerializeField] private InputActionReference saveAction;
        [SerializeField] private InputActionReference loadAction;

        private readonly HashSet<string> _initialProps = new HashSet<string>();
        private readonly HashSet<string> _initialIce = new HashSet<string>();
        private string _notice = "";
        private float _noticeUntil;
        private GUIStyle _style;

        /// <summary>Dónde se guarda la partida.</summary>
        public string SavePath => Path.Combine(Application.persistentDataPath, "saves", fileName);
        public bool HasSave => File.Exists(SavePath);
        /// <summary>El archivo de la partida (las pruebas usan uno propio para no pisar la del jugador).</summary>
        public string FileName { get => fileName; set => fileName = value; }
        public string LastNotice => _notice;

        /// <summary>Marca de los bidones que salen del banco: no están en la escena y al cargar se crean de nuevo.</summary>
        public const string CraftedCanName = "Bidon_Fabricado";

        private void OnEnable()
        {
            if (saveAction != null) { saveAction.action.performed += OnSave; saveAction.action.Enable(); }
            if (loadAction != null) { loadAction.action.performed += OnLoad; loadAction.action.Enable(); }
        }

        private void OnDisable()
        {
            if (saveAction != null) saveAction.action.performed -= OnSave;
            if (loadAction != null) loadAction.action.performed -= OnLoad;
        }

        private void OnSave(InputAction.CallbackContext _) => Save();
        private void OnLoad(InputAction.CallbackContext _) => Load();

        private IEnumerator Start()
        {
            // Lo que trae la escena al arrancar: así se sabe qué se desmontó o se rompió después.
            foreach (SalvageProp prop in FindObjectsByType<SalvageProp>()) _initialProps.Add(prop.StableId);
            foreach (FrozenDoor ice in FindObjectsByType<FrozenDoor>()) _initialIce.Add(ice.StableId);
            if (_pendingLoad == null) yield break;

            SaveGame data = _pendingLoad;
            _pendingLoad = null;
            // Esperar a que todo haya arrancado (obras, cambios, sonda) antes de aplicar.
            yield return null;
            yield return new WaitForFixedUpdate();
            Apply(data);
            LoadsApplied++;
            Say("PARTIDA CARGADA");
        }

        // ---------------------------------------------------------------- Guardar

        /// <summary>Guarda la partida. Devuelve false si ahora no se puede (por ejemplo, durmiendo).</summary>
        public bool Save()
        {
            if (vitals != null && vitals.IsSleeping) { Say("no se puede guardar mientras dormís"); return false; }
            if (inventory != null && inventory.IsOpen) inventory.Close();
            if (workbench != null && workbench.IsOpen) workbench.Close();
            var placer = motor.GetComponent<PlacementController>();
            if (placer != null) placer.CancelAll(); // un mueble a medio mover vuelve a su lugar
            if (carrier != null && carrier.Holding != null) carrier.Drop(false); // lo que está en la mano se guarda donde cae

            SaveGame data = Capture();
            string json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            // Se escribe primero a un archivo aparte y se reemplaza: si algo falla a mitad, la partida anterior queda.
            string temp = SavePath + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(SavePath)) File.Copy(SavePath, SavePath + ".bak", true);
            File.Copy(temp, SavePath, true);
            File.Delete(temp);
            Say("PARTIDA GUARDADA");
            return true;
        }

        private SaveGame Capture()
        {
            var data = new SaveGame
            {
                scene = SceneManager.GetActiveScene().name,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                gameHours = clock.TotalHours
            };
            Transform t = train.transform;

            // Jugador.
            PlayerState p = data.player;
            Vector3 feet = motor.transform.position;
            Vector3 local = t.InverseTransformPoint(feet);
            p.inTrain = InsideTrain(local);
            p.position = p.inTrain ? local : ToAbsolute(feet);
            p.yaw = motor.Yaw;
            p.pitch = look.Pitch;
            p.health = vitals.Health;
            p.satiety = vitals.Satiety;
            p.energy = vitals.Energy;
            p.warmth = vitals.Warmth;
            p.crowbar = gear.Has(GearId.Crowbar);
            p.handPump = gear.Has(GearId.HandPump);
            p.saw = gear.Has(GearId.Saw);
            p.hammer = gear.Has(GearId.Hammer);
            p.wrench = gear.Has(GearId.Wrench);
            var lampNow = motor.GetComponent<PlayerFlashlight>();
            p.flashlightOn = lampNow != null && lampNow.IsOn;
            p.suit = gear.SuitWorn;
            data.blueprints.AddRange(book.LearnedIds);
            CaptureGrid(backpack.Grid, data.backpack);

            // Tren.
            TrainState tr = data.train;
            tr.segment = train.RailPosition.Segment.name;
            tr.distance = train.RailPosition.Distance;
            tr.speed = train.SpeedMetersPerSecond;
            tr.reverser = (int)controls.Reverser;
            tr.power = controls.PowerNotch;
            tr.brake = controls.BrakeNotch;
            tr.fuelLiters = tank.Liters;
            tr.interiorC = climate.InteriorTemperatureC;
            Forge forge = FindAnyObjectByType<Forge>();
            if (forge != null)
                data.forge = new ForgeState { present = true, woodHours = forge.WoodHours, valveOpen = forge.ValveOpen, temperature = forge.Temperature,
                    pieceAt = (int)forge.PieceAt, pieceHeat = forge.PieceHeat, pieceHits = forge.PieceHits };

            // Muebles, obras y cambios de vía.
            foreach (StorageContainer storage in FindObjectsByType<StorageContainer>())
            {
                if (storage.Grid == null) continue;
                var state = new StorageState { id = storage.StableId };
                CaptureGrid(storage.Grid, state.items);
                data.storages.Add(state);
            }
            Workbench bench = FindAnyObjectByType<Workbench>();
            if (bench != null && bench.IsBusy) data.bench = new BenchState { job = bench.JobKey, minutesLeft = bench.JobMinutesLeft };
            foreach (Placeable placeable in FindObjectsByType<Placeable>())
            {
                if (string.IsNullOrEmpty(placeable.StableId)) continue; // una vista previa todavía sin colocar
                Transform pt = placeable.transform;
                data.placeables.Add(new PlaceableState { id = placeable.StableId, key = placeable.Key, placedInGame = placeable.PlacedInGame,
                    position = t.InverseTransformPoint(pt.position), rotation = Quaternion.Inverse(t.rotation) * pt.rotation });
            }
            foreach (BuildSite site in FindObjectsByType<BuildSite>())
                data.sites.Add(new SiteState { id = site.StableId, level = site.Level, contributed = site.ContributedSnapshot() });
            foreach (RailSwitch rail in FindObjectsByType<RailSwitch>())
                if (!string.IsNullOrEmpty(rail.StableId))
                    data.switches.Add(new SwitchState { id = rail.StableId, condition = (int)rail.Condition, diverging = rail.IsDiverging });

            // Lo desmontado y lo roto: lo que estaba al arrancar y ya no está.
            var props = new HashSet<string>();
            foreach (SalvageProp prop in FindObjectsByType<SalvageProp>())
            {
                props.Add(prop.StableId);
                if (prop.Progress > 0f) data.salvageProgress.Add(new ProgressEntry { id = prop.StableId, value = prop.Progress });
            }
            foreach (string id in _initialProps) if (!props.Contains(id)) data.salvageDone.Add(id);
            var ices = new HashSet<string>();
            foreach (FrozenDoor ice in FindObjectsByType<FrozenDoor>())
            {
                if (ice.IsBroken) continue;
                ices.Add(ice.StableId);
                if (ice.Progress > 0f) data.iceProgress.Add(new ProgressEntry { id = ice.StableId, value = ice.Progress });
            }
            foreach (string id in _initialIce) if (!ices.Contains(id)) data.iceBroken.Add(id);

            // Lo suelto: bultos, objetos que se llevan en la mano, comida.
            foreach (MaterialBundle bundle in FindObjectsByType<MaterialBundle>())
            {
                if (bundle.Count <= 0 || bundle.Item == null) continue;
                Vector3 where = bundle.transform.position;
                Vector3 inTrainLocal = t.InverseTransformPoint(where);
                bool inside = InsideTrain(inTrainLocal);
                data.bundles.Add(new BundleState { item = bundle.Item.Id, count = bundle.Count, inTrain = inside, position = inside ? inTrainLocal : ToAbsolute(where) });
            }
            foreach (Carryable carry in FindObjectsByType<Carryable>())
            {
                Vector3 where = carry.transform.position;
                Vector3 inTrainLocal = t.InverseTransformPoint(where);
                bool inside = InsideTrain(inTrainLocal);
                var can = carry.GetComponent<FuelCan>();
                var kit = carry.GetComponent<UpgradeKit>();
                data.carryables.Add(new CarryState
                {
                    key = PathOf(carry.transform),
                    crafted = carry.name.StartsWith(CraftedCanName) || kit != null,
                    kit = kit != null ? kit.SaveKey : "",
                    inTrain = inside,
                    position = inside ? inTrainLocal : ToAbsolute(where),
                    rotation = inside ? Quaternion.Inverse(t.rotation) * carry.transform.rotation : carry.transform.rotation,
                    liters = can != null ? can.Liters : -1f
                });
            }
            foreach (FoodItem food in FindObjectsByType<FoodItem>()) data.foodLeft.Add(PathOf(food.transform));

            // Puertas, combustible del mundo y la sonda.
            foreach (SlidingDoor door in FindObjectsByType<SlidingDoor>())
                data.doors.Add(new DoorState { key = PathOf(door.transform), open = door.IsOpen });
            foreach (FuelSource source in FindObjectsByType<FuelSource>())
                data.fuelSources.Add(new ProgressEntry { id = source.StableId, value = source.Liters });
            if (scanner != null && scanner.Target != null) data.probeTarget = scanner.Target.StableId;
            return data;
        }

        private static void CaptureGrid(ItemGrid grid, List<GridEntry> into)
        {
            foreach (GridItem piece in grid.Items)
                into.Add(new GridEntry { item = piece.Item.Id, x = piece.X, y = piece.Y, rotated = piece.Rotated });
        }

        // ---------------------------------------------------------------- Cargar

        /// <summary>¿Hay una partida guardada? (Para el menú principal, que no tiene un SaveSystem en su escena.)</summary>
        public static bool SaveExists(string file = "partida.json") => File.Exists(Path.Combine(Application.persistentDataPath, "saves", file));

        /// <summary>Desde el menú principal (P3): abre la escena de la partida guardada y le aplica lo guardado, como F9.</summary>
        public static bool LoadFromMenu(string fallbackScene, string file = "partida.json")
        {
            string path = Path.Combine(Application.persistentDataPath, "saves", file);
            if (!File.Exists(path)) return false;
            SaveGame data;
            try
            {
                data = JsonUtility.FromJson<SaveGame>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Guardado] No se pudo leer {path}: {e.Message}");
                return false;
            }
            if (data == null || data.version > SaveGame.CurrentVersion) return false;
            _pendingLoad = data;
            SceneManager.LoadScene(string.IsNullOrEmpty(data.scene) ? fallbackScene : data.scene);
            return true;
        }

        /// <summary>Carga la partida: recarga la escena y aplica lo guardado. Devuelve false si no hay partida.</summary>
        public bool Load()
        {
            if (!HasSave) { Say("no hay partida guardada"); return false; }
            SaveGame data;
            try
            {
                data = JsonUtility.FromJson<SaveGame>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Guardado] No se pudo leer {SavePath}: {e.Message}");
                Say("la partida guardada está dañada");
                return false;
            }
            if (data == null || data.version > SaveGame.CurrentVersion)
            {
                Say("la partida guardada es de una versión más nueva");
                return false;
            }
            _pendingLoad = data;
            string scene = string.IsNullOrEmpty(data.scene) ? SceneManager.GetActiveScene().name : data.scene;
            SceneManager.LoadScene(scene);
            return true;
        }

        private void Apply(SaveGame data)
        {
            clock.SetTotalHours(data.gameHours);

            // Conocimiento y equipo (y lo que ya no está en el mundo porque se tomó).
            book.Restore(data.blueprints);
            foreach (BlueprintPickup sheet in FindObjectsByType<BlueprintPickup>())
                if (book.Knows(sheet.Blueprint)) Destroy(sheet.gameObject);
            if (data.player.crowbar) gear.Give(GearId.Crowbar);
            if (data.player.handPump) gear.Give(GearId.HandPump);
            if (data.player.saw) gear.Give(GearId.Saw);
            if (data.player.hammer) gear.Give(GearId.Hammer);
            if (data.player.wrench) gear.Give(GearId.Wrench);
            var lampSaved = motor.GetComponent<PlayerFlashlight>();
            if (lampSaved != null) lampSaved.SetOn(data.player.flashlightOn);
            gear.SetSuit(data.player.suit);
            foreach (SuitRack rack in FindObjectsByType<SuitRack>()) rack.Sync(gear.SuitWorn);
            foreach (GearPickup pickup in FindObjectsByType<GearPickup>())
                if (gear.Has(pickup.Gear)) Destroy(pickup.gameObject);

            // Hielos primero (el del cambio de vía habilita su obra), después obras y cambios.
            foreach (FrozenDoor ice in FindObjectsByType<FrozenDoor>())
            {
                if (data.iceBroken.Contains(ice.StableId)) ice.RestoreBroken();
                else if (TryGet(data.iceProgress, ice.StableId, out float progress)) ice.RestoreProgress(progress);
            }
            foreach (BuildSite site in FindObjectsByType<BuildSite>())
            {
                SiteState state = data.sites.Find(s => s.id == site.StableId);
                if (state != null) site.Restore(state.level, state.contributed);
            }
            foreach (RailSwitch rail in FindObjectsByType<RailSwitch>())
            {
                SwitchState state = data.switches.Find(s => s.id == rail.StableId);
                if (state != null) rail.RestoreState((RailSwitchCondition)state.condition, state.diverging);
            }

            // Los muebles colocados (antes que lo guardado en ellos): los del jugador se crean de nuevo; los demás se mueven.
            Workbench savedBench = FindAnyObjectByType<Workbench>();
            foreach (PlaceableState state in data.placeables)
            {
                Placeable placeable = null;
                foreach (Placeable candidate in FindObjectsByType<Placeable>())
                    if (candidate.StableId == state.id) { placeable = candidate; break; }
                if (placeable == null && state.placedInGame && savedBench != null)
                {
                    GameObject prefab = savedBench.PlaceablePrefab(state.key);
                    if (prefab == null) continue;
                    GameObject made = Instantiate(prefab, train.transform);
                    made.name = prefab.name;
                    placeable = made.GetComponent<Placeable>();
                    placeable.AssignId(state.id);
                }
                if (placeable == null) continue;
                placeable.transform.SetParent(train.transform, false);
                placeable.transform.localPosition = state.position;
                placeable.transform.localRotation = state.rotation;
            }
            if (savedBench != null && data.bench != null && !string.IsNullOrEmpty(data.bench.job)) savedBench.RestoreJob(data.bench.job, data.bench.minutesLeft);

            // Mochila y muebles (los muebles ya tienen su tamaño: las obras se restauraron antes).
            RestoreGrid(backpack.Grid, data.backpack);
            foreach (StorageContainer storage in FindObjectsByType<StorageContainer>())
            {
                StorageState state = data.storages.Find(s => s.id == storage.StableId);
                if (state != null && storage.Grid != null) RestoreGrid(storage.Grid, state.items);
            }

            // Lo desmontado no vuelve; lo que estaba a medias sigue a medias.
            foreach (SalvageProp prop in FindObjectsByType<SalvageProp>())
            {
                if (data.salvageDone.Contains(prop.StableId)) Destroy(prop.gameObject);
                else if (TryGet(data.salvageProgress, prop.StableId, out float progress)) prop.RestoreProgress(progress);
            }
            foreach (FuelSource source in FindObjectsByType<FuelSource>())
                if (TryGet(data.fuelSources, source.StableId, out float liters)) source.RestoreLiters(liters);

            // El tren (antes que lo que va adentro: las posiciones son relativas a él).
            GameObject segmentObject = GameObject.Find(data.train.segment);
            RailSegment segment = segmentObject != null ? segmentObject.GetComponent<RailSegment>() : null;
            if (segment != null) train.PlaceOnRail(new RailPosition(segment, data.train.distance), data.train.speed);
            controls.Restore((ReverserPosition)data.train.reverser, data.train.power, data.train.brake);
            tank.Draw(tank.Liters);
            tank.Add(data.train.fuelLiters);
            climate.RestoreTemperature(data.train.interiorC);
            Forge savedForge = FindAnyObjectByType<Forge>();
            if (savedForge != null && data.forge != null && data.forge.present)
                savedForge.Restore(data.forge.woodHours, data.forge.valveOpen, data.forge.temperature, (ForgePieceAt)data.forge.pieceAt, data.forge.pieceHeat, data.forge.pieceHits);
            Transform t = train.transform;
            Rigidbody trainBody = train.GetComponent<Rigidbody>();

            foreach (SlidingDoor door in FindObjectsByType<SlidingDoor>())
            {
                DoorState state = data.doors.Find(d => d.key == PathOf(door.transform));
                if (state != null) door.SetOpenImmediate(state.open);
            }

            // Bultos: los que trae la escena se reemplazan por los guardados (ni se pierden ni se duplican).
            foreach (MaterialBundle bundle in FindObjectsByType<MaterialBundle>()) Destroy(bundle.gameObject);
            foreach (BundleState state in data.bundles)
            {
                ItemDefinition item = ItemById(state.item);
                if (item == null) continue;
                Vector3 where = state.inTrain ? t.TransformPoint(state.position) : FromAbsolute(state.position);
                MaterialBundle.Spawn(backpackSettings, item, state.count, where, Vector3.zero);
            }

            var foodLeft = new HashSet<string>(data.foodLeft);
            foreach (FoodItem food in FindObjectsByType<FoodItem>())
                if (!foodLeft.Contains(PathOf(food.transform))) Destroy(food.gameObject);

            foreach (CarryState state in data.carryables)
            {
                Carryable carry = null;
                if (state.crafted && !string.IsNullOrEmpty(state.kit))
                {
                    UpgradeKit kit = savedBench != null ? savedBench.CreateKit(state.kit) : null;
                    if (kit == null) continue;
                    carry = kit.GetComponent<Carryable>();
                }
                else if (state.crafted)
                {
                    if (jerrycanPrefab == null) continue;
                    GameObject made = Instantiate(jerrycanPrefab);
                    made.name = CraftedCanName;
                    carry = made.GetComponent<Carryable>();
                }
                else
                {
                    foreach (Carryable candidate in FindObjectsByType<Carryable>())
                        if (PathOf(candidate.transform) == state.key) { carry = candidate; break; }
                }
                if (carry == null) continue;
                Vector3 where = state.inTrain ? t.TransformPoint(state.position) : FromAbsolute(state.position);
                Quaternion rotation = state.inTrain ? t.rotation * state.rotation : state.rotation;
                carry.transform.SetPositionAndRotation(where, rotation);
                carry.Body.position = where;
                carry.Body.rotation = rotation;
                carry.Body.linearVelocity = Vector3.zero;
                carry.Body.angularVelocity = Vector3.zero;
                var can = carry.GetComponent<FuelCan>();
                if (can != null && state.liters >= 0f)
                {
                    can.Draw(can.Liters);
                    can.Add(state.liters);
                }
            }

            // El jugador.
            PlayerState p = data.player;
            vitals.Revive(p.health, p.satiety, p.energy, p.warmth);
            if (p.inTrain) motor.TeleportTo(t.TransformPoint(p.position), trainBody);
            else motor.TeleportTo(FromAbsolute(p.position), null);
            motor.SetYaw(p.yaw);
            look.SetPitch(p.pitch);

            if (scanner != null && !string.IsNullOrEmpty(data.probeTarget))
                foreach (ProbeSignal signal in FindObjectsByType<ProbeSignal>())
                    if (signal.StableId == data.probeTarget) scanner.SelectTarget(signal);
        }

        private void RestoreGrid(ItemGrid grid, List<GridEntry> entries)
        {
            foreach (GridItem piece in new List<GridItem>(grid.Items)) grid.Remove(piece);
            foreach (GridEntry entry in entries)
            {
                ItemDefinition item = ItemById(entry.item);
                if (item == null) continue;
                if (grid.TryPlace(item, entry.x, entry.y, entry.rotated) == null) grid.TryAutoPlace(item); // por si cambió el tamaño
            }
        }

        // ---------------------------------------------------------------- Ayudas

        private ItemDefinition ItemById(string id)
        {
            foreach (ItemDefinition item in items) if (item != null && item.Id == id) return item;
            return null;
        }

        private static bool TryGet(List<ProgressEntry> list, string id, out float value)
        {
            foreach (ProgressEntry entry in list)
                if (entry.id == id) { value = entry.value; return true; }
            value = 0f;
            return false;
        }

        // Adentro del tren (coordenadas locales del tren): de pared a pared y del piso al techo.
        private static bool InsideTrain(Vector3 local) =>
            Mathf.Abs(local.x) < 1.75f && Mathf.Abs(local.z) < 9.6f && local.y > 0.6f && local.y < 3.6f;

        private Vector3 OriginOffset => origin != null ? new Vector3((float)origin.AbsoluteOriginX, 0f, (float)origin.AbsoluteOriginZ) : Vector3.zero;
        private Vector3 ToAbsolute(Vector3 world) => world + OriginOffset;
        private Vector3 FromAbsolute(Vector3 absolute) => absolute - OriginOffset;

        /// <summary>El lugar de un objeto en la escena (nombres de padres e hijo): su identificador si no tiene uno propio.</summary>
        public static string PathOf(Transform transform)
        {
            var names = new List<string>();
            for (Transform t = transform; t != null; t = t.parent) names.Add(t.name);
            names.Reverse();
            return string.Join("/", names);
        }

        private void Say(string text)
        {
            _notice = text;
            _noticeUntil = Time.unscaledTime + 2.5f;
        }

        // Dibuja con GUI.* (sin el maquetado automático de GUILayout): menos memoria nueva por cuadro (M13).
        private void Awake() => useGUILayout = false;

        private void OnGUI()
        {
            Matrix4x4 uiOld = UiScale.Begin(); // la interfaz en puntos, agrandada según la pantalla
            DrawGui();
            GUI.matrix = uiOld;
        }

        private void DrawGui()
        {
            if (Time.unscaledTime >= _noticeUntil) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            float fade = Mathf.Clamp01((_noticeUntil - Time.unscaledTime) / 0.5f);
            var rect = new Rect(UiScale.Width * 0.5f - 200f, 20f, 400f, 36f);
            GUI.color = new Color(0.08f, 0.09f, 0.1f, 0.85f * fade);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            _style.normal.textColor = new Color(1f, 1f, 1f, fade);
            GUI.Label(rect, _notice, _style);
        }
    }
}
