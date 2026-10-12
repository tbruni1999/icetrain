using UnityEngine;

namespace SurvivalEnTren.World
{
    /// <summary>Configuración del re-centrado del mundo (estrategia C del spike M1).</summary>
    [CreateAssetMenu(menuName = "Survival en tren/Mundo/Re-centrado del mundo", fileName = "FloatingOriginSettings")]
    public class FloatingOriginSettings : ScriptableObject
    {
        [Tooltip("Cuando el jugador se aleja esta distancia horizontal del origen (0,0,0), todo el mundo se mueve " +
                 "para que vuelva a quedar cerca del origen. En metros. Medido: hasta ~5 km la precisión es buena.")]
        [SerializeField, Range(10f, 5000f)] private float recenterDistanceMeters = 2000f;

        public float RecenterDistanceMeters => recenterDistanceMeters;
    }
}
