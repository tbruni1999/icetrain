using SurvivalEnTren.Player;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Lo que se lleva en las manos, en primera persona (pedido de Tomi del 2026-10-16). Es la raíz de las manos y las
    /// herramientas, hija de la cabeza:
    /// - Se dibuja con su propia cámara (encima de la escena): la barreta no se mete en las paredes aunque estés pegado a una.
    /// - Se balancea al caminar (más corriendo) y se queda un poquito atrás al girar la cabeza: tiene peso.
    /// - Empujón de cámara: un golpe que pega empuja la vista un instante (más si rebota) y vuelve con un resorte.
    /// Los valores están en ViewmodelSettings (Data/Equipment/Manos.asset).
    /// </summary>
    [DefaultExecutionOrder(50)] // después de PlayerLook (que pone la cabeza en su lugar en LateUpdate)
    public class ViewmodelRig : MonoBehaviour
    {
        [SerializeField] private ViewmodelSettings settings;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [Tooltip("La raíz de las manos (hija de la cabeza): se balancea y se retrasa.")]
        [SerializeField] private Transform root;

        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _stepPhase;
        private float _bobWeight;
        private Quaternion _lagged = Quaternion.identity;
        private Quaternion _lastHead = Quaternion.identity;
        private Vector3 _kick, _kickVelocity;

        public Transform Root => root;
        public ViewmodelSettings Settings => settings;
        /// <summary>El empujón de cámara ahora (grados: arriba-abajo, izquierda-derecha, inclinación).</summary>
        public Vector3 KickDegrees => _kick;

        private void Awake()
        {
            if (root == null) return;
            _restPosition = root.localPosition;
            _restRotation = root.localRotation;
        }

        /// <summary>Empuja la vista (en grados; se suma a lo que ya venía) y la deja volver sola.</summary>
        public void AddKick(Vector3 degrees, bool rebound)
        {
            float scale = rebound ? settings.ReboundKickMultiplier : 1f;
            _kickVelocity += degrees * scale * 18f; // un impulso: sale rápido y el resorte lo trae
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // El resorte del empujón (se aplica a la cámara en PlayerLook).
            Vector3 accel = -settings.KickStiffness * _kick - settings.KickDamping * _kickVelocity;
            _kickVelocity += accel * dt;
            _kick += _kickVelocity * dt;
            if (look != null) look.Kick = Quaternion.Euler(_kick);

            // Balanceo al caminar: avanza con lo que se camina (no con el tren: con la intención de caminar).
            Vector3 walk = motor.WalkVelocity;
            float speed = new Vector2(walk.x, walk.z).magnitude;
            bool walking = motor.IsGrounded && speed > 0.2f;
            _bobWeight = Mathf.MoveTowards(_bobWeight, walking ? 1f : 0f, dt * 4f);
            _stepPhase += speed * dt / settings.StepLengthMeters * Mathf.PI;
        }

        private void LateUpdate()
        {
            if (root == null || Time.deltaTime <= 0f) return;
            float run = motor.IsRunning ? settings.RunBobMultiplier : 1f;
            float w = _bobWeight * run;
            Vector3 bob = new Vector3(Mathf.Sin(_stepPhase) * settings.BobHorizontal, -Mathf.Abs(Mathf.Cos(_stepPhase)) * settings.BobVertical, 0f) * w;

            // Retraso al girar: las manos siguen a la cabeza con un poco de demora (en espacio de la cabeza).
            Quaternion head = transform.rotation;
            Quaternion turned = Quaternion.Inverse(head) * _lastHead; // cuánto giró la cabeza desde el cuadro anterior, visto desde ahora
            _lastHead = head;
            _lagged = turned * _lagged;
            _lagged = Quaternion.Slerp(_lagged, Quaternion.identity, 1f - Mathf.Exp(-settings.SwayFollowSpeed * Time.deltaTime));
            float angle = Quaternion.Angle(Quaternion.identity, _lagged);
            if (angle > settings.SwayMaxDegrees) _lagged = Quaternion.Slerp(Quaternion.identity, _lagged, settings.SwayMaxDegrees / angle);

            root.localPosition = _restPosition + bob;
            root.localRotation = _lagged * _restRotation;
        }
    }
}
