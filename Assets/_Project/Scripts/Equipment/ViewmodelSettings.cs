using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Cómo se mueve lo que se lleva en las manos (pedido de Tomi del 2026-10-16: "no es nada inmersivo, ni la barreta ni el
    /// golpe"): el balanceo al caminar, el retraso al girar la cabeza, el empujón de la cámara al pegar y la cámara aparte de
    /// las manos (para que nada se meta en las paredes). Todo se ajusta acá, sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Manos en primera persona", fileName = "Manos")]
    public class ViewmodelSettings : ScriptableObject
    {
        [Header("Cámara de las manos")]
        [Tooltip("Campo de visión de la cámara de las manos, en grados (igual que la cámara principal, 70: así las manos calzan con lo que se carga, que está en el mundo).")]
        [SerializeField, Range(30f, 90f)] private float fieldOfView = 70f;

        [Header("Balanceo al caminar")]
        [Tooltip("Cuánto sube y baja lo que se lleva en las manos con cada paso, en metros.")]
        [SerializeField, Range(0f, 0.05f)] private float bobVertical = 0.012f;
        [Tooltip("Cuánto va de lado a lado con cada paso, en metros.")]
        [SerializeField, Range(0f, 0.05f)] private float bobHorizontal = 0.009f;
        [Tooltip("Largo de un paso, en metros (un ciclo de lado a lado son dos pasos).")]
        [SerializeField, Range(0.3f, 1.5f)] private float stepLengthMeters = 0.75f;
        [Tooltip("Cuánto más se balancea corriendo.")]
        [SerializeField, Range(1f, 3f)] private float runBobMultiplier = 1.7f;

        [Header("Retraso al girar")]
        [Tooltip("Qué tan rápido alcanzan las manos a la mirada (más alto = más pegadas).")]
        [SerializeField, Range(2f, 40f)] private float swayFollowSpeed = 11f;
        [Tooltip("Cuánto se pueden quedar atrás como mucho, en grados.")]
        [SerializeField, Range(0f, 15f)] private float swayMaxDegrees = 5f;

        [Header("Empujón de la cámara al pegar")]
        [Tooltip("Rigidez del resorte que devuelve la cámara a su lugar (más alto = vuelve más rápido).")]
        [SerializeField, Range(20f, 600f)] private float kickStiffness = 220f;
        [Tooltip("Freno del resorte (más alto = menos rebote).")]
        [SerializeField, Range(1f, 60f)] private float kickDamping = 20f;
        [Tooltip("Cuánto más fuerte es el empujón cuando el golpe rebota (herramienta equivocada o algo duro).")]
        [SerializeField, Range(1f, 4f)] private float reboundKickMultiplier = 1.8f;

        [Header("Sacar y guardar")]
        [Tooltip("Cuánto baja y gira la herramienta cuando está guardada (metros y grados): sube desde abajo al sacarla.")]
        [SerializeField] private Vector3 loweredOffset = new Vector3(0.05f, -0.42f, -0.05f);
        [SerializeField] private Vector3 loweredRotation = new Vector3(35f, 0f, 15f);

        public float FieldOfView => fieldOfView;
        public float BobVertical => bobVertical;
        public float BobHorizontal => bobHorizontal;
        public float StepLengthMeters => stepLengthMeters;
        public float RunBobMultiplier => runBobMultiplier;
        public float SwayFollowSpeed => swayFollowSpeed;
        public float SwayMaxDegrees => swayMaxDegrees;
        public float KickStiffness => kickStiffness;
        public float KickDamping => kickDamping;
        public float ReboundKickMultiplier => reboundKickMultiplier;
        public Vector3 LoweredOffset => loweredOffset;
        public Vector3 LoweredRotation => loweredRotation;
    }
}
