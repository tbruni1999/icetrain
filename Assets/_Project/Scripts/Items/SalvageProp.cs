using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Un objeto del mundo que se puede desmontar (sección 9.2): con la herramienta que pide su receta, mantener
    /// E lo va desarmando (se achica) y al terminar se rompe en pedazos y deja los materiales de la receta
    /// (sección 9.3: se entiende de dónde salió cada material). Lo que no entra en la mochila por el peso queda
    /// en el piso como bultos (MaterialBundle): nunca se pierde ni se duplica.
    /// - El avance se conserva si se suelta E (como el hielo de una puerta).
    /// - Una vez desmontado el objeto ya no existe: no se puede desmontar dos veces.
    /// - El ID estable es para el guardado futuro (loot ya retirado, Anexo D.2).
    /// - Va en la raíz del objeto (con el pivote en su base) y los colliders en sus hijos.
    /// </summary>
    public class SalvageProp : MonoBehaviour, IChanneledInteractable
    {
        [SerializeField] private SalvageRecipe recipe;
        [Tooltip("Para crear los bultos de lo que no entra en la mochila.")]
        [SerializeField] private BackpackSettings backpackSettings;
        [Tooltip("ID estable del objeto (para el guardado futuro).")]
        [SerializeField] private string stableId;
        [Tooltip("Qué tan chico queda justo antes de romperse (fracción del tamaño original).")]
        [SerializeField, Range(0.05f, 1f)] private float thinnestScale = 0.4f;

        private float _progress;
        private Vector3 _fullScale;
        private Renderer _renderer;

        public string StableId => stableId;
        public bool IsDone { get; private set; }
        public float Progress => _progress;
        public SalvageRecipe Recipe => recipe;

        private void Awake()
        {
            _fullScale = transform.localScale;
            _renderer = GetComponentInChildren<Renderer>();
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            var backpack = interactor.GetComponent<Backpack>();
            if (IsDone || backpack == null || recipe == null) return null;
            var equipment = interactor.GetComponent<PlayerGear>();
            if (equipment == null || !equipment.Has(recipe.RequiredTool)) return $"{recipe.DisplayName}: hace falta una barreta";

            string what = $"{recipe.YieldText()}, {recipe.TotalMassKg:0.0} kg";
            string progress = _progress > 0.01f ? $"   [{_progress * 100f:0}%]" : "";
            string overflow = recipe.TotalMassKg > backpack.FreeKg + 1e-3f ? "   (no entra todo: el resto queda en el piso)" : "";
            return $"mantener para desarmar {recipe.DisplayName}   → {what}{overflow}{progress}";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor) { } // todo pasa en Channel

        public bool Channel(PlayerInteractor interactor, float deltaTime)
        {
            var backpack = interactor.GetComponent<Backpack>();
            var equipment = interactor.GetComponent<PlayerGear>();
            if (IsDone || backpack == null || recipe == null || equipment == null || !equipment.Has(recipe.RequiredTool)) return false;

            _progress = Mathf.Min(1f, _progress + deltaTime / recipe.SalvageSeconds);
            transform.localScale = _fullScale * Mathf.Lerp(1f, thinnestScale, _progress);
            if (_progress < 1f) return true;

            Complete(backpack, interactor.GetComponent<CharacterController>());
            return false;
        }

        private void Complete(Backpack backpack, CharacterController breaker)
        {
            IsDone = true; // antes de crear nada: que no se pueda repetir
            Vector3 center = transform.position + Vector3.up * 0.5f;
            int bundles = 0;
            foreach (SalvageYield y in recipe.Yields)
            {
                if (y.item == null) continue;
                int added = backpack.Add(y.item, y.count);
                int left = y.count - added;
                if (left <= 0) continue;
                // Lo que no entra por el peso queda en el piso, alrededor de donde estaba el objeto.
                float angle = bundles++ * 2.2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.55f;
                MaterialBundle.Spawn(backpackSettings, y.item, left, center + offset, Vector3.zero);
            }

            Material material = _renderer != null ? _renderer.sharedMaterial : null;
            for (int i = 0; i < recipe.ShardCount; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Pedazo";
                shard.transform.localScale = new Vector3(Random.Range(0.05f, 0.14f), Random.Range(0.03f, 0.08f), Random.Range(0.05f, 0.2f));
                shard.transform.position = center + Random.insideUnitSphere * 0.4f;
                shard.transform.rotation = Random.rotation;
                if (material != null) shard.GetComponent<MeshRenderer>().sharedMaterial = material;
                var body = shard.AddComponent<Rigidbody>();
                body.mass = 0.3f;
                body.linearVelocity = Random.insideUnitSphere * 1.5f + Vector3.up;
                // Los pedazos no frenan a quien desmontó (no se traba pisando escombros).
                if (breaker != null) Physics.IgnoreCollision(shard.GetComponent<Collider>(), breaker);
                Destroy(shard, 8f);
            }
            Destroy(gameObject);
        }
    }
}
