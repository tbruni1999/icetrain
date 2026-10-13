using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Un bulto de material en el mundo (sección 9.3: "los recursos deben representar algo físico que el
    /// jugador entiende de dónde salió y dónde terminó"): lo que se suelta de la mochila, lo que sobra al
    /// desmontar algo y los materiales sueltos de un POI. Mirándolo, E lo guarda en la mochila (si no entra
    /// todo, guarda lo que entra y el resto queda en el bulto). Es un objeto con física: se apoya en el piso
    /// del tren como las cajas. Nunca crea ni pierde unidades: las unidades pasan del bulto a la mochila y
    /// viceversa.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MaterialBundle : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private BackpackSettings settings;
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int count = 1;

        private Rigidbody _body;
        private MaterialPropertyBlock _block;

        public ItemDefinition Item => item;
        public int Count => count;

        private void Awake() => Refresh();

        /// <summary>Pone el contenido del bulto y ajusta su tamaño, color y peso.</summary>
        public void Setup(BackpackSettings backpackSettings, ItemDefinition material, int units)
        {
            settings = backpackSettings;
            item = material;
            count = Mathf.Max(1, units);
            Refresh();
        }

        // Tamaño según la cantidad, el color del material y la masa de lo que contiene.
        private void Refresh()
        {
            if (item == null || settings == null) return;
            _body = _body != null ? _body : GetComponent<Rigidbody>();
            float size = settings.BundleBaseSizeMeters + settings.BundleGrowthMeters * (Mathf.Pow(count, 1f / 3f) - 1f);
            transform.localScale = Vector3.one * size;
            _body.mass = Mathf.Max(0.1f, item.WeightKg * count);
            var rendererOfBundle = GetComponent<Renderer>();
            if (rendererOfBundle != null)
            {
                _block ??= new MaterialPropertyBlock();
                _block.SetColor(BaseColor, item.Color);
                rendererOfBundle.SetPropertyBlock(_block);
            }
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            var backpack = interactor.GetComponent<Backpack>();
            if (backpack == null || item == null) return null;
            // Con algo en la mano (o manejando) E suelta eso: este objeto no se usa.
            if (interactor.IsEngaged) return null;
            int fits = backpack.MaxAddable(item);
            float kg = item.WeightKg * count;
            if (fits <= 0) return $"{item.DisplayName} ×{count} ({kg:0.0} kg): no entra, mochila llena";
            if (fits >= count) return $"guardar {item.DisplayName} ×{count}   ({kg:0.0} kg)";
            return $"guardar {item.DisplayName} ×{fits} de {count}   (entra lo que da el peso)";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var backpack = interactor.GetComponent<Backpack>();
            if (backpack == null || item == null) return;
            int added = backpack.Add(item, count);
            if (added <= 0) return;
            count -= added;
            if (count <= 0) Destroy(gameObject);
            else Refresh();
        }

        /// <summary>
        /// Crea un bulto con física en un punto del mundo (con la velocidad de quien lo suelta, para que
        /// acompañe al tren en marcha). Devuelve el bulto.
        /// </summary>
        public static MaterialBundle Spawn(BackpackSettings settings, ItemDefinition material, int units, Vector3 position, Vector3 velocity)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Bulto_{material.DisplayName}";
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (settings.BundleMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = settings.BundleMaterial;
            var body = go.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = velocity;
            var bundle = go.AddComponent<MaterialBundle>();
            bundle.Setup(settings, material, units);
            return bundle;
        }
    }
}
