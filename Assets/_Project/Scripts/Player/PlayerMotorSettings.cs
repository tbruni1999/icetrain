using UnityEngine;

namespace SurvivalEnTren.Player
{
    /// <summary>
    /// Valores del movimiento del jugador en primera persona: velocidad, salto,
    /// gravedad, mouse y medidas del cuerpo. Se editan desde el Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Jugador/Movimiento", fileName = "PlayerMotorSettings")]
    public class PlayerMotorSettings : ScriptableObject
    {
        [Header("Movimiento")]
        [Tooltip("Velocidad al caminar, en metros por segundo.")]
        [SerializeField, Range(0.5f, 10f)] private float walkSpeedMetersPerSecond = 3.5f;

        [Tooltip("Velocidad al correr (Shift mantenido), en metros por segundo.")]
        [SerializeField, Range(0.5f, 15f)] private float runSpeedMetersPerSecond = 6.5f;

        [Tooltip("Altura máxima del salto, en metros.")]
        [SerializeField, Range(0.1f, 2f)] private float jumpHeightMeters = 0.9f;

        [Tooltip("Gravedad que siente el jugador, en metros por segundo cada segundo (la real es 9,81).")]
        [SerializeField, Range(1f, 40f)] private float gravityMetersPerSecondSquared = 15f;

        [Header("Mirar")]
        [Tooltip("Cuántos grados gira la vista por cada píxel que se mueve el mouse.")]
        [SerializeField, Range(0.01f, 1f)] private float mouseSensitivityDegreesPerPixel = 0.08f;

        [Tooltip("Cuánto se puede mirar hacia arriba o hacia abajo, en grados.")]
        [SerializeField, Range(30f, 89f)] private float maxPitchDegrees = 85f;

        [Tooltip("Qué tan rápido la vista alcanza al cuerpo al subir un escalón (más alto = más brusco). " +
                 "El cuerpo sube de golpe; esto solo suaviza la cámara.")]
        [SerializeField, Range(1f, 60f)] private float stepSmoothingSpeed = 15f;

        [Header("Cuerpo")]
        [Tooltip("Altura total del cuerpo, en metros.")]
        [SerializeField, Range(1f, 2.2f)] private float bodyHeightMeters = 1.8f;

        [Tooltip("Radio del cuerpo, en metros.")]
        [SerializeField, Range(0.15f, 0.5f)] private float bodyRadiusMeters = 0.3f;

        [Tooltip("Altura de los ojos (la cámara) desde los pies, en metros.")]
        [SerializeField, Range(0.8f, 2.1f)] private float eyeHeightMeters = 1.65f;

        [Tooltip("Escalón más alto que se sube o se baja caminando (sin saltar), en metros. " +
                 "Tiene que superar la altura de las cajas chicas (35 cm) y los escalones del tren (32 cm).")]
        [SerializeField, Range(0.05f, 0.6f)] private float stepOffsetMeters = 0.4f;

        [Tooltip("Pendiente máxima por la que se puede caminar, en grados.")]
        [SerializeField, Range(10f, 80f)] private float slopeLimitDegrees = 50f;

        public float WalkSpeedMetersPerSecond => walkSpeedMetersPerSecond;
        public float RunSpeedMetersPerSecond => runSpeedMetersPerSecond;
        public float JumpHeightMeters => jumpHeightMeters;
        public float GravityMetersPerSecondSquared => gravityMetersPerSecondSquared;
        public float MouseSensitivityDegreesPerPixel => mouseSensitivityDegreesPerPixel;
        public float MaxPitchDegrees => maxPitchDegrees;
        public float StepSmoothingSpeed => stepSmoothingSpeed;
        public float BodyHeightMeters => bodyHeightMeters;
        public float BodyRadiusMeters => bodyRadiusMeters;
        public float EyeHeightMeters => eyeHeightMeters;
        public float StepOffsetMeters => stepOffsetMeters;
        public float SlopeLimitDegrees => slopeLimitDegrees;
    }
}
