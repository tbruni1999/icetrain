using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Un tipo de pieza que se guarda en la mochila o en un mueble (sección 9.1: madera, metal, telas/aislantes,
    /// componentes eléctricos...). Es un dato: cuánto pesa, cuánto espacio ocupa en la cuadrícula (en casillas) y cómo
    /// se ve. Todo se ajusta en el archivo, sin tocar código (Anexo B.3). El ID estable es para el guardado futuro.
    /// DECISIÓN de Tomi: el inventario es una cuadrícula; cada pieza tiene su tamaño en casillas y su imagen (por ahora
    /// provisoria; el arte final reemplaza el icono en este mismo archivo).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Objetos/Material", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("ID estable del material (no se cambia una vez usado: lo usará el guardado).")]
        [SerializeField] private string id = "item_nuevo";
        [Tooltip("Cómo se llama en el panel y en las indicaciones (en singular y sin artículo: \"madera\").")]
        [SerializeField] private string displayName = "material";
        [Tooltip("Cómo se llama UNA pieza (\"tablón\", \"chapa\"); se usa en el panel.")]
        [SerializeField] private string pieceName = "pieza";
        [Tooltip("Cuánto pesa una pieza, en kg.")]
        [SerializeField, Range(0.01f, 50f)] private float weightKg = 1f;
        [Tooltip("Cuánto espacio ocupa una pieza en la cuadrícula, en casillas (ancho x alto). " +
                 "Una pieza larga (1 x 4) no entra en un estante chico.")]
        [SerializeField] private Vector2Int sizeCells = Vector2Int.one;
        [Tooltip("Si la pieza se puede girar (R) para que entre de costado.")]
        [SerializeField] private bool canRotate = true;
        [Tooltip("Cuántas piezas iguales entran apiladas en una casilla (la pieza de la cuadrícula). 1 = no se apila. " +
                 "DECISIÓN de Tomi (2026-10-16): la cuadrícula se queda, pero los materiales se apilan.")]
        [SerializeField, Range(1, 200)] private int maxStack = 1;
        [Tooltip("Imagen de la pieza (se estira al tamaño en casillas). Vacío = se dibuja un recuadro de color.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Color del bulto en el mundo y del recuadro de la pieza (provisorio, hasta el arte final).")]
        [SerializeField] private Color color = Color.gray;

        public string Id => id;
        public string DisplayName => displayName;
        public string PieceName => pieceName;
        public float WeightKg => weightKg;
        /// <summary>Tamaño en casillas sin girar (al menos 1 x 1).</summary>
        public Vector2Int SizeCells => new Vector2Int(Mathf.Max(1, sizeCells.x), Mathf.Max(1, sizeCells.y));
        public bool CanRotate => canRotate && sizeCells.x != sizeCells.y;
        /// <summary>Cuántas piezas iguales entran en una pila (al menos 1).</summary>
        public int MaxStack => Mathf.Max(1, maxStack);
        public Sprite Icon => icon;
        public Color Color => color;
    }
}
