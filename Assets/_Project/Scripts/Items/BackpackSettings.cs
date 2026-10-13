using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Valores de la mochila (secciones 9.4 y 22.2: "el peso y el volumen convierten el saqueo en decisión").
    /// PROPUESTA: el documento deja abierto si es por slots o por peso; para la V1 es solo peso, en kg.
    /// Lo que se lleva en la mochila se suma a lo que se lleva en la mano para frenar el caminar
    /// (la fórmula está en CarrySettings).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Objetos/Mochila", fileName = "BackpackSettings")]
    public class BackpackSettings : ScriptableObject
    {
        [Tooltip("Peso máximo que entra en la mochila, en kg. Más allá de esto no entra nada más.")]
        [SerializeField, Range(1f, 200f)] private float capacityKg = 30f;

        public float CapacityKg => capacityKg;
    }
}
