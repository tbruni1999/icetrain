using UnityEngine;

namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Valores de la interacción (Anexo B.3: "tuning de interacción" en datos): alcance y cómo se
    /// muestran la indicación y el anillo de "mantener". Lo que hay que mantener y cuánto lo decide
    /// cada objeto (una palanca pesada pide más que una liviana).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Interacción/Interacción", fileName = "InteractionSettings")]
    public class InteractionSettings : ScriptableObject
    {
        [Tooltip("Distancia máxima a la que se puede usar algo, en metros.")]
        [SerializeField, Range(0.5f, 5f)] private float reachMeters = 2.2f;

        [Tooltip("Tamaño del texto de la indicación (\"E abrir\"), en píxeles.")]
        [SerializeField, Range(10, 40)] private int promptFontSize = 16;

        [Tooltip("Radio del anillo que se llena al mantener E, en píxeles.")]
        [SerializeField, Range(6f, 60f)] private float holdRingRadiusPixels = 16f;

        public float ReachMeters => reachMeters;
        public int PromptFontSize => promptFontSize;
        public float HoldRingRadiusPixels => holdRingRadiusPixels;
    }
}
