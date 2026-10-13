using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Un tipo de material que se guarda en la mochila (sección 9.1: madera, metal, telas/aislantes,
    /// componentes eléctricos...). Es un dato: el peso por unidad y cómo se muestra. El nombre y el peso se
    /// ajustan en el archivo, sin tocar código (Anexo B.3). El ID estable es para el guardado futuro y para
    /// distinguir un material de otro sin depender de su nombre.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Objetos/Material", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("ID estable del material (no se cambia una vez usado: lo usará el guardado).")]
        [SerializeField] private string id = "item_nuevo";
        [Tooltip("Cómo se llama en el panel y en las indicaciones (en singular y sin artículo: \"madera\").")]
        [SerializeField] private string displayName = "material";
        [Tooltip("Cuánto pesa una unidad, en kg.")]
        [SerializeField, Range(0.01f, 50f)] private float weightKg = 1f;
        [Tooltip("Color del bulto en el mundo y de la marca en el panel (provisorio, hasta el arte final).")]
        [SerializeField] private Color color = Color.gray;

        public string Id => id;
        public string DisplayName => displayName;
        public float WeightKg => weightKg;
        public Color Color => color;
    }
}
