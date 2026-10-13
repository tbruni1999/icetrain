using UnityEngine;

namespace SurvivalEnTren.Interaction
{
    /// <summary>
    /// Valores de llevar objetos en la mano (secciones 9.3 y 9.4: objetos enteros, el peso convierte el
    /// saqueo en decisión). Son PROPUESTA: el documento no fija pesos ni límites.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Interacción/Llevar objetos", fileName = "CarrySettings")]
    public class CarrySettings : ScriptableObject
    {
        [Tooltip("Peso máximo que se puede levantar a mano, en kg. Lo más pesado necesitará carros u otra ayuda (después de la V1).")]
        [SerializeField, Range(1f, 100f)] private float maxCarryMassKg = 25f;

        [Tooltip("Cuánto se frena el caminar por cada kg que se lleva (0,02 = 2% por kg: 10 kg = 20% más lento).")]
        [SerializeField, Range(0f, 0.1f)] private float walkSlowdownPerKg = 0.02f;

        [Tooltip("Lo más lento que se puede caminar cargando, como fracción de la velocidad normal.")]
        [SerializeField, Range(0.1f, 1f)] private float minWalkSpeedFactor = 0.45f;

        [Tooltip("A qué distancia delante de los ojos se sostiene el objeto, en metros.")]
        [SerializeField, Range(0.5f, 2f)] private float holdDistanceMeters = 1.1f;

        [Tooltip("Dónde se sostiene un objeto de una mano (un bidón), respecto de los ojos y girado con el cuerpo: " +
                 "derecha, arriba y adelante, en metros. Va a la cadera para no tapar la vista ni ensanchar al jugador en una puerta.")]
        [SerializeField] private Vector3 oneHandedHoldOffset = new Vector3(0.28f, -0.7f, 0.5f);

        [Tooltip("Si el objeto queda trabado más lejos que esto de donde debería estar, se suelta, en metros.")]
        [SerializeField, Range(0.3f, 3f)] private float dropIfSeparatedMeters = 1.2f;

        [Tooltip("Cuánto tiempo tiene que quedar trabado lejos de la mano para soltarse solo, en segundos. " +
                 "Evita que se suelte en el instante de tomarlo de lejos.")]
        [SerializeField, Range(0.05f, 2f)] private float stuckSeconds = 0.4f;

        [Tooltip("Velocidad máxima con la que el objeto corre a su lugar en la mano, en m/s (evita tirones).")]
        [SerializeField, Range(1f, 30f)] private float maxFollowSpeedMetersPerSecond = 12f;

        [Tooltip("Velocidad con la que sale un objeto lanzado (clic izquierdo), en m/s, para un objeto de 1 kg. " +
                 "Los más pesados salen más despacio.")]
        [SerializeField, Range(0f, 20f)] private float throwSpeedMetersPerSecond = 7f;

        public float MaxCarryMassKg => maxCarryMassKg;
        public float HoldDistanceMeters => holdDistanceMeters;
        public Vector3 OneHandedHoldOffset => oneHandedHoldOffset;
        public float DropIfSeparatedMeters => dropIfSeparatedMeters;
        public float StuckSeconds => stuckSeconds;
        public float MaxFollowSpeedMetersPerSecond => maxFollowSpeedMetersPerSecond;

        /// <summary>Fracción de la velocidad de caminar al llevar un objeto de esta masa.</summary>
        public float WalkSpeedFactor(float massKg) => Mathf.Max(minWalkSpeedFactor, 1f - walkSlowdownPerKg * massKg);

        /// <summary>Velocidad de lanzamiento para un objeto de esta masa (más pesado = más lento).</summary>
        public float ThrowSpeed(float massKg) => throwSpeedMetersPerSecond / Mathf.Sqrt(Mathf.Max(1f, massKg));
    }
}
