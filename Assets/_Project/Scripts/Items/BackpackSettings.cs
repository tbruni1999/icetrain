using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Valores de la mochila (secciones 9.4 y 22.2: "el peso y el volumen convierten el saqueo en decisión").
    /// DECISIÓN de Tomi: la mochila es una cuadrícula de casillas; cada pieza ocupa su tamaño (ItemDefinition).
    /// El peso de lo que se lleva suma al de la mano para frenar el caminar (la fórmula está en CarrySettings).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Objetos/Mochila", fileName = "BackpackSettings")]
    public class BackpackSettings : ScriptableObject
    {
        [Header("Cuadrícula de la mochila (en casillas)")]
        [SerializeField, Range(1, 20)] private int gridWidth = 6;
        [SerializeField, Range(1, 20)] private int gridHeight = 5;

        [Header("Bultos en el piso (lo que se suelta de la mochila)")]
        [Tooltip("Material (Lit, blanco) de los bultos; el color de cada uno sale del material que contiene.")]
        [SerializeField] private Material bundleMaterial;
        [Tooltip("Tamaño de un bulto de 1 unidad, en metros.")]
        [SerializeField, Range(0.05f, 1f)] private float bundleBaseSizeMeters = 0.22f;
        [Tooltip("Cuánto crece el bulto por cada raíz cúbica de unidades (así 8 unidades miden el doble que 1).")]
        [SerializeField, Range(0f, 0.5f)] private float bundleGrowthMeters = 0.1f;
        [Tooltip("A qué distancia delante de los ojos se suelta un bulto, en metros.")]
        [SerializeField, Range(0.3f, 2f)] private float dropDistanceMeters = 0.9f;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public Material BundleMaterial => bundleMaterial;
        public float BundleBaseSizeMeters => bundleBaseSizeMeters;
        public float BundleGrowthMeters => bundleGrowthMeters;
        public float DropDistanceMeters => dropDistanceMeters;
    }
}
