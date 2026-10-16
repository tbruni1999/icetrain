using SurvivalEnTren.Building;
using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Un mueble donde guardar piezas (secciones 4, 9.4 y 11: "un estante o cajas muy limitadas", que mejora con obras).
    /// DECISIÓN de Tomi: cada mueble tiene su cuadrícula con su tamaño; lo largo no entra en lo chico. E abre el panel con
    /// la mochila y la cuadrícula del mueble lado a lado.
    /// - Un mueble fijo (el estante precario del taller) tiene siempre el mismo tamaño.
    /// - Un mueble que se construye (el estante reforzado) toma el tamaño de su obra según el nivel (BuildProject): crece
    ///   al completar fases y no se achica. Mientras mide 0 x 0 todavía no sirve para guardar.
    /// - Lo guardado se ve en el mueble (cubos del color de cada material), para que el tren muestre lo que se juntó
    ///   (sección 11.3).
    /// - El ID estable es para el guardado futuro (contenido de los muebles, Anexo B.4).
    /// </summary>
    public class StorageContainer : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("ID estable del mueble (para el guardado futuro).")]
        [SerializeField] private string stableId;
        [Tooltip("Cómo se llama en el panel y en la indicación (\"estante precario\").")]
        [SerializeField] private string displayName = "estante";
        [Tooltip("Tamaño fijo de la cuadrícula, en casillas, si el mueble no depende de una obra.")]
        [SerializeField] private Vector2Int fixedGridSize = new Vector2Int(4, 3);
        [Tooltip("Si el mueble se construye: la obra que le da el tamaño según su nivel.")]
        [SerializeField] private BuildSite site;
        [Tooltip("Para dibujar lo guardado en el mueble (material y tamaño de los bultos).")]
        [SerializeField] private BackpackSettings settings;
        [Tooltip("Lugares del mueble donde se ven los cubos de lo guardado (uno por material distinto).")]
        [SerializeField] private Transform[] displaySlots = new Transform[0];

        private ItemGrid _grid;
        private Transform[] _cubes;
        private MaterialPropertyBlock _block;

        public string StableId => stableId;
        public string DisplayName => displayName;

        /// <summary>Un mueble colocado por el jugador recibe su ID al instalarse (Placeable).</summary>
        public void SetStableId(string id) => stableId = id;

        /// <summary>La cuadrícula del mueble (null mientras todavía no sirve para guardar).</summary>
        public ItemGrid Grid
        {
            get
            {
                EnsureGrid();
                return _grid;
            }
        }

        private Vector2Int CurrentSize => site != null ? site.Project.LevelGridSize(site.Level) : fixedGridSize;

        private void Awake()
        {
            EnsureGrid();
            if (site != null) site.Changed += EnsureGrid;
        }

        private void OnDestroy()
        {
            if (site != null) site.Changed -= EnsureGrid;
        }

        // Crea la cuadrícula la primera vez que el tamaño no es 0 y la agranda cuando la obra sube de nivel.
        private void EnsureGrid()
        {
            Vector2Int size = CurrentSize;
            if (size.x <= 0 || size.y <= 0) return;
            if (_grid == null)
            {
                _grid = new ItemGrid(size.x, size.y);
                _grid.Changed += RefreshDisplay;
            }
            else _grid.Resize(Mathf.Max(_grid.Width, size.x), Mathf.Max(_grid.Height, size.y));
            RefreshDisplay();
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (Grid == null || interactor.GetComponent<InventoryPanel>() == null) return null;
            return $"abrir {displayName}   ({Grid.UsedCells}/{Grid.CellCount} casillas)";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var panel = interactor.GetComponent<InventoryPanel>();
            if (Grid == null || panel == null) return;
            panel.Open(Grid, displayName.ToUpperInvariant());
        }

        // Un cubo por material distinto, en los lugares del mueble; su tamaño crece con la cantidad.
        private void RefreshDisplay()
        {
            if (_grid == null || displaySlots == null || displaySlots.Length == 0 || settings == null) return;
            if (_cubes == null)
            {
                _cubes = new Transform[displaySlots.Length];
                _block = new MaterialPropertyBlock();
                for (int i = 0; i < displaySlots.Length; i++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "Guardado_" + i;
                    Destroy(cube.GetComponent<Collider>()); // solo se ve: no estorba ni se apunta
                    cube.transform.SetParent(displaySlots[i], false);
                    cube.transform.localPosition = Vector3.zero;
                    if (settings.BundleMaterial != null) cube.GetComponent<MeshRenderer>().sharedMaterial = settings.BundleMaterial;
                    _cubes[i] = cube.transform;
                }
            }

            var order = new System.Collections.Generic.List<ItemDefinition>();
            var counts = new System.Collections.Generic.Dictionary<ItemDefinition, int>();
            foreach (GridItem g in _grid.Items)
            {
                if (!counts.ContainsKey(g.Item)) { counts[g.Item] = 0; order.Add(g.Item); }
                counts[g.Item]++;
            }
            for (int i = 0; i < _cubes.Length; i++)
            {
                bool used = i < order.Count;
                _cubes[i].gameObject.SetActive(used);
                if (!used) continue;
                ItemDefinition item = order[i];
                float size = (settings.BundleBaseSizeMeters + settings.BundleGrowthMeters * (Mathf.Pow(counts[item], 1f / 3f) - 1f)) * 0.8f;
                _cubes[i].localScale = Vector3.one * size;
                _cubes[i].localPosition = Vector3.up * (size * 0.5f);
                _block.SetColor(BaseColor, item.Color);
                _cubes[i].GetComponent<Renderer>().SetPropertyBlock(_block);
            }
        }
    }
}
