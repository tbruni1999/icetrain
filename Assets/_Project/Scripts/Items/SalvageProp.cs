using System;
using SurvivalEnTren.Crafting;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.UI;
using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Un objeto del mundo que se puede desmontar (sección 9.2), a golpes (herramientas y forja, paso H2; DECISIÓN de Tomi del
    /// 2026-10-13: "se va a farmear pegándole a los objetos con la herramienta, no manteniendo E").
    /// - Con la herramienta de su receta en la mano, cada golpe lo va rompiendo: se sacude, saltan astillas o chispas y sus
    ///   piezas se van aflojando a la vista. Con la herramienta equivocada el golpe rebota.
    /// - Al último golpe se rompe en pedazos y los materiales de la receta caen al piso como bultos (MaterialBundle) para
    ///   levantar con E (sección 9.3: se entiende de dónde salió cada material). Nunca se pierde ni se duplica nada.
    /// - Mirarlo con la herramienta al cinto: E la saca a la mano.
    /// - El avance queda (se puede volver más tarde) y se guarda (M12). Una vez roto ya no existe.
    /// - Va en la raíz del objeto (con el pivote en su base) y los colliders en sus hijos.
    /// - Desde G1 (2026-10-17, "grindeo a lo 7 Days"): si la receta lo pide, cada golpe que sirve da su parte de los materiales
    ///   directo a la mochila (lo que no entra cae como bulto al lado) y se avisa a la derecha de la pantalla (PickupFeed); el
    ///   último golpe da lo que falta. Otras herramientas de la receta también sirven, pero pegan menos.
    /// </summary>
    public class SalvageProp : MonoBehaviour, IInteractable, IToolTarget
    {
        [SerializeField] private SalvageRecipe recipe;
        [Tooltip("Para crear los bultos de lo que sale.")]
        [SerializeField] private BackpackSettings backpackSettings;
        [Tooltip("ID estable del objeto (para el guardado).")]
        [SerializeField] private string stableId;
        [Tooltip("Cuánto se aflojan sus piezas justo antes de romperse: metros y grados.")]
        [SerializeField, Range(0f, 0.2f)] private float loosenMeters = 0.05f;
        [SerializeField, Range(0f, 30f)] private float loosenDegrees = 9f;
        [Tooltip("Cuánto se sacude con cada golpe (metros) y cuánto dura (segundos).")]
        [SerializeField, Range(0f, 0.1f)] private float shakeMeters = 0.03f;
        [SerializeField, Range(0.02f, 0.5f)] private float shakeSeconds = 0.18f;
        [Tooltip("Cuántas astillas o chispas saltan con cada golpe.")]
        [SerializeField, Range(0, 12)] private int chipsPerHit = 4;

        private float _progress;
        private Transform[] _parts;
        private Vector3[] _partPositions;
        private Quaternion[] _partRotations;
        private Vector3[] _partDrift;
        private Vector3[] _partAxis;
        private Vector3 _shakeLocal;
        private float _shakeLeft;
        private Material _material;
        private int[] _given; // cuánto de cada material de la receta ya se entregó golpe a golpe (G1)

        public string StableId => stableId;
        public bool IsDone { get; private set; }
        public float Progress => _progress;
        public SalvageRecipe Recipe => recipe;
        /// <summary>Golpes que le faltan para romperse.</summary>
        public int HitsLeft => Mathf.Max(0, Mathf.CeilToInt((1f - _progress) * recipe.HitsToBreak - 0.001f));

        /// <summary>Cada golpe que sirve: dónde y de qué material (para el ruido).</summary>
        public event Action<Vector3, SalvageSurface> Hit;
        /// <summary>Se dispara cuando el objeto se rompe (justo antes de desaparecer).</summary>
        public event Action Salvaged;

        private void Awake()
        {
            // Sus piezas (los hijos con algo que se ve), con una dirección fija para aflojarse (la misma en cada partida).
            var random = new System.Random(string.IsNullOrEmpty(stableId) ? name.GetHashCode() : stableId.GetHashCode());
            var parts = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform) parts.Add(child);
            _parts = parts.ToArray();
            _partPositions = new Vector3[_parts.Length];
            _partRotations = new Quaternion[_parts.Length];
            _partDrift = new Vector3[_parts.Length];
            _partAxis = new Vector3[_parts.Length];
            float R() => (float)random.NextDouble() * 2f - 1f;
            for (int i = 0; i < _parts.Length; i++)
            {
                _partPositions[i] = _parts[i].localPosition;
                _partRotations[i] = _parts[i].localRotation;
                _partDrift[i] = new Vector3(R(), Mathf.Abs(R()) * 0.5f, R()).normalized;
                _partAxis[i] = new Vector3(R(), R(), R()).normalized;
            }
            Renderer r = GetComponentInChildren<Renderer>();
            _material = r != null ? r.sharedMaterial : null;
        }

        /// <summary>Deja el objeto a medio romper (al cargar una partida).</summary>
        public void RestoreProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            // Lo que ya se había entregado golpe a golpe sale del avance (la misma cuenta que al golpear): no se vuelve a dar.
            if (recipe != null && recipe.YieldPerHit)
            {
                _given = new int[recipe.Yields.Length];
                for (int i = 0; i < _given.Length; i++) _given[i] = Due(recipe.Yields[i].count);
            }
            Pose();
        }

        // Cuánto de un material corresponde al avance de ahora (todo, al romperse).
        private int Due(int count) => _progress >= 1f ? count : Mathf.FloorToInt(count * _progress + 0.0001f);

        // La herramienta que le sirve más de las que tiene el jugador (la de la receta primero), o null si no tiene ninguna.
        private GearId? BestToolOf(PlayerGear gear)
        {
            if (gear == null || recipe == null) return null;
            if (gear.Has(recipe.RequiredTool)) return recipe.RequiredTool;
            GearId? best = null;
            float power = 0f;
            foreach (ToolEfficiency t in recipe.OtherTools)
                if (t.factor > power && gear.Has(t.tool)) { best = t.tool; power = t.factor; }
            return best;
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (IsDone || recipe == null) return null;
            var equipment = interactor.GetComponent<PlayerGear>();
            var belt = interactor.GetComponent<ToolBelt>();
            string what = $"{recipe.YieldText()}, {recipe.TotalMassKg:0.0} kg";
            string progress = _progress > 0.01f ? $"   [{_progress * 100f:0}%]" : "";
            GearId? inHand = belt != null ? belt.ActiveTool : null;
            if (inHand.HasValue && recipe.PowerOf(inHand.Value) > 0f)
            {
                string weak = inHand.Value != recipe.RequiredTool ? $" (con {Blueprint.ToolWithArticle(inHand.Value, false)} pega poco)" : "";
                return $"golpear para {recipe.Verb} {recipe.DisplayName}{weak}   → {what}{progress}";
            }
            GearId? best = BestToolOf(equipment);
            if (!best.HasValue) return $"{recipe.DisplayName}: hace falta {Blueprint.ToolWithArticle(recipe.RequiredTool, false)}";
            return $"sacar {Blueprint.ToolWithArticle(best.Value, true)} para {recipe.Verb} {recipe.DisplayName}   → {what}{progress}";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        // E: saca la herramienta que mejor le sirve de las que tiene el jugador (si no la lleva ya en la mano).
        public void Interact(PlayerInteractor interactor)
        {
            var belt = interactor.GetComponent<ToolBelt>();
            GearId? best = BestToolOf(interactor.GetComponent<PlayerGear>());
            if (IsDone || recipe == null || belt == null || !best.HasValue) return;
            if (belt.ActiveTool != best.Value) belt.Select(belt.SlotOf(best.Value));
        }

        public bool OnToolHit(in ToolHit hit)
        {
            float power = recipe != null ? recipe.PowerOf(hit.Tool) : 0f;
            if (IsDone || power <= 0f) return false; // herramienta que no sirve: rebota
            _progress = Mathf.Min(1f, _progress + power / recipe.HitsToBreak + 0.0001f);
            _shakeLocal = transform.InverseTransformDirection(-hit.Normal) * shakeMeters;
            _shakeLeft = shakeSeconds;
            var breaker = hit.By != null ? hit.By.GetComponent<CharacterController>() : null;
            bool sparks = recipe.Surface == SalvageSurface.Metal || recipe.Surface == SalvageSurface.Electrico;
            if (sparks) SpawnChips(hit.Point, hit.Normal, chipsPerHit + 2, 0.012f, 0.35f, 4f, breaker, SparkMaterial());      // chispas
            else if (recipe.Surface == SalvageSurface.Piedra) SpawnChips(hit.Point, hit.Normal, chipsPerHit + 2, 0.02f, 1.2f, 2.2f, breaker, _material); // polvo y lascas
            else SpawnChips(hit.Point, hit.Normal, chipsPerHit, 0.03f, 2.5f, 1.5f, breaker, _material);                        // astillas, pelusa
            if (recipe.YieldPerHit) GiveShares(hit);
            Hit?.Invoke(hit.Point, recipe.Surface);
            if (_progress >= 1f) Break(breaker);
            else Pose();
            return true;
        }

        private void Update()
        {
            if (_shakeLeft <= 0f) return;
            _shakeLeft -= Time.deltaTime;
            Pose();
        }

        // Las piezas, aflojadas según el avance y corridas por la sacudida del último golpe.
        private void Pose()
        {
            if (_parts == null) return;
            float shake = _shakeLeft > 0f ? Mathf.Sin(_shakeLeft / shakeSeconds * Mathf.PI * 3f) * (_shakeLeft / shakeSeconds) : 0f;
            float loose = _progress * _progress; // casi firme al principio, floja al final
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null) continue;
                _parts[i].localPosition = _partPositions[i] + _partDrift[i] * (loosenMeters * loose) + _shakeLocal * shake;
                _parts[i].localRotation = _partRotations[i] * Quaternion.AngleAxis(loosenDegrees * loose, _partAxis[i]);
            }
        }

        // La parte de cada material que corresponde a este golpe (G1): a la mochila del que golpea; lo que no entra, al piso.
        private void GiveShares(in ToolHit hit)
        {
            if (_given == null || _given.Length != recipe.Yields.Length) _given = new int[recipe.Yields.Length];
            Backpack pack = hit.By != null ? hit.By.GetComponent<Backpack>() : null;
            PickupFeed feed = hit.By != null ? hit.By.GetComponent<PickupFeed>() : null;
            for (int i = 0; i < recipe.Yields.Length; i++)
            {
                SalvageYield y = recipe.Yields[i];
                if (y.item == null) continue;
                int due = Due(y.count) - _given[i];
                if (due <= 0) continue;
                _given[i] += due;
                int kept = pack != null ? pack.Add(y.item, due) : 0;
                if (kept > 0 && feed != null) feed.Show(y.item, kept, pack.CountOf(y.item));
                if (due > kept)
                {
                    MaterialBundle.Spawn(backpackSettings, y.item, due - kept, hit.Point + hit.Normal * 0.35f, Vector3.up * 0.5f);
                    if (feed != null) feed.ShowDropped(y.item, due - kept);
                }
            }
        }

        private void Break(CharacterController breaker)
        {
            IsDone = true; // antes de crear nada: que no se pueda repetir
            Salvaged?.Invoke();
            Vector3 center = transform.position + Vector3.up * 0.5f;
            // Picando golpe a golpe (G1) ya se entregó todo; si no, los materiales salen ahora, juntos.
            if (!recipe.YieldPerHit)
            {
                int bundles = 0;
                foreach (SalvageYield y in recipe.Yields)
                {
                    if (y.item == null) continue;
                    // Los materiales caen al piso alrededor de donde estaba el objeto, para levantarlos con E.
                    float angle = bundles++ * 2.2f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.45f;
                    MaterialBundle.Spawn(backpackSettings, y.item, y.count, center + offset, Vector3.up * 0.5f);
                }
            }
            SpawnChips(center, Vector3.up, recipe.ShardCount, 0.08f, 8f, 1.5f, breaker, _material);
            Destroy(gameObject);
        }

        private static Material _spark;

        // Las chispas: un material sin luz, amarillo anaranjado (se ve brillar aunque esté oscuro).
        private static Material SparkMaterial()
        {
            if (_spark != null) return _spark;
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) return null;
            _spark = new Material(unlit) { name = "Chispa" };
            _spark.SetColor("_BaseColor", new Color(1f, 0.72f, 0.3f));
            return _spark;
        }

        // Pedazos con física que saltan desde un punto (astillas o chispas al golpear; los restos al romperse). No frenan al jugador.
        private void SpawnChips(Vector3 at, Vector3 normal, int count, float size, float lifeSeconds, float speed, CharacterController breaker, Material material)
        {
            for (int i = 0; i < count; i++)
            {
                var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chip.name = "Pedazo";
                chip.layer = 2; // "Ignore Raycast": los golpes y la E los atraviesan
                chip.transform.localScale = new Vector3(size * UnityEngine.Random.Range(0.6f, 1.6f), size * UnityEngine.Random.Range(0.4f, 1f),
                    size * UnityEngine.Random.Range(0.8f, 2.4f));
                chip.transform.position = at + normal * 0.05f + UnityEngine.Random.insideUnitSphere * size;
                chip.transform.rotation = UnityEngine.Random.rotation;
                if (material != null) chip.GetComponent<MeshRenderer>().sharedMaterial = material;
                chip.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var body = chip.AddComponent<Rigidbody>();
                body.mass = 0.1f;
                body.linearVelocity = (normal + UnityEngine.Random.insideUnitSphere * 0.8f).normalized * speed + Vector3.up * 0.8f;
                if (breaker != null) Physics.IgnoreCollision(chip.GetComponent<Collider>(), breaker);
                Destroy(chip, lifeSeconds);
            }
        }
    }
}
