using System.Collections.Generic;
using SurvivalEnTren.Building;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using SurvivalEnTren.Survival;
using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Crafting
{
    /// <summary>Por qué todavía no se puede fabricar algo (o None si se puede).</summary>
    public enum CraftBlock
    {
        None,
        /// <summary>Faltan materiales en la mochila.</summary>
        MissingMaterials,
        /// <summary>Falta una herramienta que la receta pide (no se gasta, pero hay que tenerla).</summary>
        MissingTool,
        /// <summary>Es una herramienta y ya se tiene: no tiene sentido fabricar otra.</summary>
        AlreadyHave,
        /// <summary>Lo que sale necesita un lugar libre sobre la mesa y está ocupado.</summary>
        OutputBlocked,
        /// <summary>El banco ya está armando otra cosa (un kit o un mueble).</summary>
        Busy
    }

    /// <summary>
    /// El banco de trabajo del taller (secciones 4, 10.1 y 20.6): el mueble donde se fabrican cosas con los planos que se
    /// conocen. Acá vive la lógica de "¿se puede fabricar?" y "fabricar"; el panel (WorkbenchPanel) solo la muestra.
    /// - Usa los materiales de la mochila, los guardados en los estantes del tren y los bultos del piso del tren (2026-10-16:
    ///   un estante entero no entra en la mochila; así se juntan materiales en varios viajes). Gasta primero lo de la mochila.
    /// - Las herramientas que pide una receta se tienen que tener, pero no se gastan.
    /// - Herramientas y bidón: se fabrican en el momento, con el jugador en el banco (M9).
    /// - Mejoras (decisión de Tomi del 2026-10-16: "que se mejoren en el workbench y tengan un timer"): los kits de las
    ///   obras (cama, forja, bifurcación) y los muebles (estante) se arman con un tiempo del reloj del juego. Los materiales
    ///   se gastan al empezar; el banco sigue solo (se puede ir, dormir) y al terminar deja un kit en el piso, al lado.
    ///   Se arma una cosa por vez. Cancelar devuelve los materiales a la mochila.
    /// </summary>
    public class Workbench : MonoBehaviour, IInteractable
    {
        [Tooltip("ID estable del banco (para el guardado).")]
        [SerializeField] private string stableId = "taller_banco_001";
        [SerializeField] private string displayName = "banco de trabajo";
        [Tooltip("Los planos que se pueden fabricar en este banco (se muestran los que el jugador conoce).")]
        [SerializeField] private Blueprint[] catalog = new Blueprint[0];
        [Tooltip("Dónde aparece lo que se fabrica y ocupa lugar (el bidón), sobre la mesa.")]
        [SerializeField] private Transform outputPoint;
        [Tooltip("El bidón vacío que sale de la receta del bidón.")]
        [SerializeField] private GameObject jerrycanPrefab;
        [Header("Mejoras con tiempo")]
        [Tooltip("Las obras cuyos kits se arman en este banco (la cama, la forja, la bifurcación).")]
        [SerializeField] private BuildSite[] sites = new BuildSite[0];
        [Tooltip("El cajón del kit que sale (con Carryable y UpgradeKit).")]
        [SerializeField] private GameObject kitPrefab;
        [Tooltip("Dónde aparece el kit: en el piso, al lado del banco.")]
        [SerializeField] private Transform kitPoint;
        [Tooltip("El reloj del juego (el tiempo de las mejoras corre con él: también mientras se duerme).")]
        [SerializeField] private GameClock clock;

        private BenchRecipe _job;
        private double _readyAtHours;
        private double _startedAtHours;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public Blueprint[] Catalog => catalog;
        public Transform OutputPoint => outputPoint;
        public Transform KitPoint => kitPoint;

        /// <summary>Lo que el banco está armando ahora (null si está libre).</summary>
        public BenchRecipe Job => _job;
        public bool IsBusy => _job != null;
        /// <summary>Minutos del juego que le faltan a lo que se está armando (0 si terminó o está libre).</summary>
        public float JobMinutesLeft => _job == null ? 0f : Mathf.Max(0f, (float)((_readyAtHours - Now) * 60.0));
        public float JobProgress01 => _job == null ? 0f : Mathf.Clamp01((float)((Now - _startedAtHours) / System.Math.Max(1e-6, _readyAtHours - _startedAtHours)));
        /// <summary>Terminó pero el lugar del kit está ocupado: sale apenas se libere.</summary>
        public bool WaitingForSpace => _job != null && Now >= _readyAtHours;

        private double Now => clock != null ? clock.TotalHours : Time.timeSinceLevelLoadAsDouble / 3600.0;

        /// <summary>Se dispara cuando se termina de fabricar algo (también cuando sale un kit).</summary>
        public event System.Action Crafted;
        /// <summary>Se dispara cuando sale un kit (el kit recién salido).</summary>
        public event System.Action<UpgradeKit> KitReady;

        /// <summary>Los planos del banco que el jugador conoce ahora, en el orden del banco.</summary>
        public List<Blueprint> KnownRecipes(BlueprintBook book)
        {
            var list = new List<Blueprint>();
            foreach (Blueprint blueprint in catalog)
                if (blueprint != null && blueprint.Kind == BlueprintKind.Craft && book != null && book.Knows(blueprint)) list.Add(blueprint);
            return list;
        }

        /// <summary>
        /// Todo lo que el banco ofrece ahora: los planos conocidos y, de cada obra, el kit de la fase que sigue (contando los
        /// kits que ya están hechos y lo que se está armando: así se puede adelantar el kit siguiente).
        /// </summary>
        public List<BenchRecipe> Recipes(BlueprintBook book)
        {
            var list = new List<BenchRecipe>();
            foreach (Blueprint blueprint in KnownRecipes(book)) list.Add(new BenchRecipe(blueprint));
            foreach (BuildSite site in sites)
            {
                if (site == null || site.IsComplete) continue;
                if (site.RequiredBlueprint != null && (book == null || !book.Knows(site.RequiredBlueprint))) continue;
                int next = NextPhaseToMake(site);
                if (next < site.Project.PhaseCount) list.Add(new BenchRecipe(site, next));
            }
            return list;
        }

        // La fase que sigue de una obra: después de lo instalado, de los kits que andan sueltos y de lo que está en el banco.
        private int NextPhaseToMake(BuildSite site)
        {
            int next = site.Level;
            foreach (UpgradeKit kit in FindObjectsByType<UpgradeKit>())
                if (kit.Kind == KitKind.SitePhase && kit.SiteId == site.StableId) next = Mathf.Max(next, kit.Phase + 1);
            if (_job != null && _job.Site == site) next = Mathf.Max(next, _job.Phase + 1);
            return next;
        }

        // ---------------------------------------------------------------- Se puede fabricar?

        /// <summary>Qué impide fabricar ahora (None si se puede). El orden importa: lo primero que se le dice al jugador.</summary>
        public CraftBlock Check(Blueprint blueprint, Backpack backpack, PlayerGear gear) => Check(new BenchRecipe(blueprint), backpack, gear);

        public CraftBlock Check(BenchRecipe recipe, Backpack backpack, PlayerGear gear)
        {
            Blueprint blueprint = recipe.Blueprint;
            if (blueprint != null && blueprint.OutputKind == CraftOutputKind.Gear && gear.Has(blueprint.OutputGear)) return CraftBlock.AlreadyHave;
            foreach (GearId tool in recipe.RequiredTools)
                if (!gear.Has(tool)) return CraftBlock.MissingTool;
            foreach (CraftIngredient ingredient in recipe.Ingredients)
                if (Available(ingredient.item, backpack) < ingredient.count) return CraftBlock.MissingMaterials;
            if (recipe.Timed && IsBusy) return CraftBlock.Busy;
            if (blueprint != null && blueprint.OutputKind == CraftOutputKind.Jerrycan && OutputSpotBusy()) return CraftBlock.OutputBlocked;
            return CraftBlock.None;
        }

        /// <summary>Lo que falta, en palabras ("chapa ×1", "la sierra manual").</summary>
        public string MissingText(Blueprint blueprint, Backpack backpack, PlayerGear gear) => MissingText(new BenchRecipe(blueprint), backpack, gear);

        public string MissingText(BenchRecipe recipe, Backpack backpack, PlayerGear gear)
        {
            var parts = new List<string>();
            foreach (GearId tool in recipe.RequiredTools)
                if (!gear.Has(tool)) parts.Add("la " + Blueprint.ToolName(tool));
            foreach (CraftIngredient ingredient in recipe.Ingredients)
            {
                int missing = ingredient.count - Available(ingredient.item, backpack);
                if (missing > 0) parts.Add($"{ingredient.item.PieceName} ×{missing}");
            }
            return string.Join(", ", parts);
        }

        // ---------------------------------------------------------------- De dónde salen los materiales

        // La mochila primero y después los estantes del mismo tren que el banco.
        private List<ItemGrid> Stores(Backpack backpack)
        {
            var list = new List<ItemGrid>();
            if (backpack != null) list.Add(backpack.Grid);
            TrainMover train = GetComponentInParent<TrainMover>();
            if (train == null) return list;
            foreach (StorageContainer storage in FindObjectsByType<StorageContainer>())
                if (storage.Grid != null && storage.GetComponentInParent<TrainMover>() == train) list.Add(storage.Grid);
            return list;
        }

        // Los bultos que están en el piso del tren (lo que se dejó ahí al volver de una expedición).
        private List<MaterialBundle> FloorBundles(ItemDefinition item)
        {
            var list = new List<MaterialBundle>();
            TrainMover train = GetComponentInParent<TrainMover>();
            if (train == null) return list;
            foreach (MaterialBundle bundle in FindObjectsByType<MaterialBundle>())
            {
                if (bundle.Item != item || bundle.Count <= 0) continue;
                Vector3 local = train.transform.InverseTransformPoint(bundle.transform.position);
                if (Mathf.Abs(local.x) < 1.75f && Mathf.Abs(local.z) < 9.6f && local.y > 0.6f && local.y < 3.6f) list.Add(bundle);
            }
            return list;
        }

        /// <summary>Cuántas piezas de un material hay para el banco: en la mochila, en los estantes y en el piso del tren.</summary>
        public int Available(ItemDefinition item, Backpack backpack)
        {
            int total = 0;
            foreach (ItemGrid grid in Stores(backpack)) total += grid.CountOf(item);
            foreach (MaterialBundle bundle in FloorBundles(item)) total += bundle.Count;
            return total;
        }

        // Saca piezas: primero de la mochila, después de los estantes y por último del piso del tren.
        private void Take(ItemDefinition item, int count, Backpack backpack)
        {
            foreach (ItemGrid grid in Stores(backpack))
            {
                for (int i = grid.Items.Count - 1; i >= 0 && count > 0; i--)
                {
                    GridItem piece = grid.Items[i];
                    if (piece.Item != item) continue;
                    grid.Remove(piece);
                    count--;
                }
                if (count == 0) return;
            }
            foreach (MaterialBundle bundle in FloorBundles(item))
            {
                count -= bundle.TakeUnits(count);
                if (count <= 0) return;
            }
        }

        // El lugar de la mesa donde sale el bidón: libre si no hay nada sólido encima (no cuenta el propio banco ni el jugador).
        private bool OutputSpotBusy()
        {
            if (outputPoint == null) return false;
            Vector3 center = outputPoint.position + Vector3.up * 0.27f;
            return SpotBusy(center, new Vector3(0.2f, 0.25f, 0.2f), outputPoint.rotation, true);
        }

        // El lugar del piso donde sale el kit (el jugador parado ahí también lo ocupa).
        private bool KitSpotBusy()
        {
            if (kitPoint == null) return false;
            return SpotBusy(kitPoint.position + Vector3.up * 0.22f, new Vector3(0.3f, 0.18f, 0.25f), kitPoint.rotation, false);
        }

        private bool SpotBusy(Vector3 center, Vector3 halfExtents, Quaternion rotation, bool ignorePlayer)
        {
            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                if (ignorePlayer && hit.GetComponentInParent<CharacterController>() != null) continue;
                if (hit.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- Fabricar en el momento (herramientas, bidón)

        /// <summary>
        /// Gasta los materiales y entrega lo fabricado. Devuelve false (sin gastar nada) si ya no se puede. Para lo que lleva
        /// tiempo (kits, muebles) se usa StartJob.
        /// </summary>
        public bool Complete(Blueprint blueprint, Backpack backpack, PlayerGear gear)
        {
            if (blueprint.OutputKind == CraftOutputKind.Placeable) return StartJob(new BenchRecipe(blueprint), backpack, gear);
            if (Check(blueprint, backpack, gear) != CraftBlock.None) return false;
            foreach (CraftIngredient ingredient in blueprint.Ingredients) Take(ingredient.item, ingredient.count, backpack);

            switch (blueprint.OutputKind)
            {
                case CraftOutputKind.Gear:
                    gear.Give(blueprint.OutputGear);
                    break;
                case CraftOutputKind.Jerrycan:
                    if (jerrycanPrefab != null && outputPoint != null)
                    {
                        // El nombre marca que es fabricado: no está en la escena y al cargar una partida se vuelve a crear.
                        GameObject made = Instantiate(jerrycanPrefab, outputPoint.position + Vector3.up * 0.02f, outputPoint.rotation);
                        made.name = "Bidon_Fabricado";
                    }
                    break;
            }
            Crafted?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------- Mejoras con tiempo (kits y muebles)

        /// <summary>Empieza a armar un kit o un mueble: gasta los materiales ya y el banco sigue solo. Devuelve si empezó.</summary>
        public bool StartJob(BenchRecipe recipe, Backpack backpack, PlayerGear gear)
        {
            if (!recipe.Timed || Check(recipe, backpack, gear) != CraftBlock.None) return false;
            foreach (CraftIngredient ingredient in recipe.Ingredients) Take(ingredient.item, ingredient.count, backpack);
            _job = recipe;
            _startedAtHours = Now;
            _readyAtHours = Now + recipe.Minutes / 60.0;
            return true;
        }

        /// <summary>Cancela lo que se está armando y devuelve los materiales a la mochila. False si no entran (no cancela).</summary>
        public bool CancelJob(Backpack backpack)
        {
            if (_job == null) return false;
            var pieces = new List<(ItemDefinition, int)>();
            foreach (CraftIngredient ingredient in _job.Ingredients) pieces.Add((ingredient.item, ingredient.count));
            if (!backpack.WouldFitAll(pieces)) return false;
            foreach (CraftIngredient ingredient in _job.Ingredients) backpack.Add(ingredient.item, ingredient.count);
            _job = null;
            return true;
        }

        /// <summary>Para el guardado: qué se está armando y cuántos minutos le faltan.</summary>
        public string JobKey => _job != null ? _job.Key : "";

        /// <summary>Al cargar una partida: vuelve a poner en el banco lo que se estaba armando.</summary>
        public void RestoreJob(string key, float minutesLeft)
        {
            _job = FindRecipe(key);
            if (_job == null) return;
            _readyAtHours = Now + Mathf.Max(0f, minutesLeft) / 60.0;
            _startedAtHours = _readyAtHours - _job.Minutes / 60.0;
        }

        /// <summary>Termina ya lo que se está armando (lo usan las pruebas automáticas, en lugar de esperar el reloj).</summary>
        public void FinishJobNow() { if (_job != null) _readyAtHours = Now; }

        private BenchRecipe FindRecipe(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.StartsWith("plano:"))
            {
                foreach (Blueprint blueprint in catalog)
                    if (blueprint != null && "plano:" + blueprint.Id == key) return new BenchRecipe(blueprint);
                return null;
            }
            string[] parts = key.Split(':');
            if (parts.Length != 3 || parts[0] != "obra" || !int.TryParse(parts[2], out int phase)) return null;
            foreach (BuildSite site in sites)
                if (site != null && site.StableId == parts[1] && phase < site.Project.PhaseCount) return new BenchRecipe(site, phase);
            return null;
        }

        private void Update()
        {
            if (_job == null || Now < _readyAtHours || KitSpotBusy()) return;
            BenchRecipe done = _job;
            _job = null;
            UpgradeKit kit = CreateKit(done.Key);
            if (kit != null)
            {
                kit.transform.SetPositionAndRotation(kitPoint.position + Vector3.up * 0.01f, kitPoint.rotation);
                var body = kit.GetComponent<Rigidbody>();
                body.position = kit.transform.position;
                body.rotation = kit.transform.rotation;
                // Si el tren anda, el kit sale moviéndose con él (si no, resbalaría por el piso).
                Rigidbody platform = GetComponentInParent<Rigidbody>();
                if (platform != null) body.linearVelocity = platform.GetPointVelocity(body.position);
                KitReady?.Invoke(kit);
            }
            Crafted?.Invoke();
        }

        /// <summary>
        /// Crea un kit a partir de su clave ("obra:ID:fase" o "plano:ID", o la clave de guardado del kit, "mueble:ID"), con su
        /// nombre y su peso (lo que pesan sus materiales). Lo usan el banco al terminar y el guardado al cargar.
        /// </summary>
        public UpgradeKit CreateKit(string key)
        {
            if (kitPrefab == null || string.IsNullOrEmpty(key)) return null;
            if (key.StartsWith("mueble:")) key = "plano:" + key.Substring("mueble:".Length);
            BenchRecipe recipe = FindRecipe(key);
            if (recipe == null) return null;
            GameObject made = Instantiate(kitPrefab, kitPoint != null ? kitPoint.position : transform.position, kitPoint != null ? kitPoint.rotation : Quaternion.identity);
            made.name = UpgradeKit.ObjectName;
            var kit = made.GetComponent<UpgradeKit>();
            if (recipe.IsSitePhase) kit.ConfigureSite(recipe.Site.StableId, recipe.Phase, recipe.KitName);
            else kit.ConfigurePlaceable(recipe.Blueprint.Id, recipe.Blueprint.PlaceablePrefab, recipe.KitName);
            float mass = 0f;
            foreach (CraftIngredient ingredient in recipe.Ingredients) mass += ingredient.item.WeightKg * ingredient.count;
            made.GetComponent<Rigidbody>().mass = Mathf.Max(2f, mass);
            return kit;
        }

        /// <summary>El mueble de un plano (por su ID), para volver a crearlo al cargar una partida.</summary>
        public GameObject PlaceablePrefab(string blueprintId)
        {
            foreach (Blueprint blueprint in catalog)
                if (blueprint != null && blueprint.Id == blueprintId) return blueprint.PlaceablePrefab;
            return null;
        }

        // ---------------------------------------------------------------- Interacción

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (interactor.GetComponent<WorkbenchPanel>() == null) return null;
            if (_job == null) return $"usar el {displayName}";
            return WaitingForSpace
                ? $"usar el {displayName}   ·   {_job.KitName}: listo, falta lugar al lado del banco"
                : $"usar el {displayName}   ·   armando {_job.KitName} ({Mathf.CeilToInt(JobMinutesLeft)} min)";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var panel = interactor.GetComponent<WorkbenchPanel>();
            if (panel != null) panel.Open(this);
        }
    }
}
