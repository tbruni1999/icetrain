using SurvivalEnTren.Interaction;
using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Palanca manual de un cambio de vía, al costado de la vía (sección 6.2: "frenar, bajar,
    /// accionar una palanca y volver al tren"). Se usa mirándola y manteniendo E un momento.
    /// - La palanca se inclina hacia un lado u otro según la posición del cambio.
    /// - El indicador (un cartel que gira) muestra hacia dónde va el tren: cara clara = vía directa,
    ///   cara ámbar = desvío (sección 18.4: ámbar = atención).
    /// - Si hay un tren pisando el cambio, la palanca no se mueve: se sacude y vuelve.
    /// - Si el cambio está congelado o roto (sección 6.3) no se mueve y la indicación dice por qué.
    /// </summary>
    public class SwitchLever : MonoBehaviour, IInteractable
    {
        [SerializeField] private RailSwitch railSwitch;
        [SerializeField] private Transform leverPivot;
        [SerializeField] private Transform indicatorPivot;
        [Tooltip("Inclinación de la palanca en cada posición, en grados.")]
        [SerializeField] private float leverDegrees = 40f;
        [Tooltip("Qué tan rápido se mueven la palanca y el indicador (más alto = más brusco).")]
        [SerializeField] private float speed = 8f;
        [Tooltip("Cuánto hay que mantener E para mover el cambio, en segundos (mover un cambio a mano cuesta esfuerzo).")]
        [SerializeField, Range(0f, 5f)] private float holdSeconds = 1f;
        [Tooltip("Sacudida cuando el cambio está trabado por un tren encima: grados y segundos.")]
        [SerializeField] private float blockedShakeDegrees = 6f;
        [SerializeField] private float blockedShakeSeconds = 0.35f;

        private float _lever;
        private float _indicator;
        private float _shakeTimeLeft;

        public RailSwitch Switch => railSwitch;

        private void Start()
        {
            _lever = LeverTarget;
            _indicator = IndicatorTarget;
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            switch (railSwitch.Condition)
            {
                case RailSwitchCondition.Frozen: return "el hielo traba el cambio";
                case RailSwitchCondition.Broken: return "la palanca está rota";
                default: return "mover el cambio";
            }
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => railSwitch.CanMove ? holdSeconds : 0f;

        public void Interact(PlayerInteractor interactor)
        {
            if (!railSwitch.CanMove) return; // congelado o roto: no hay nada que mover
            if (!railSwitch.TrySetDiverging(!railSwitch.IsDiverging, IsTrainOnSwitch))
                _shakeTimeLeft = blockedShakeSeconds;
        }

        private bool IsTrainOnSwitch(Vector3 switchPoint)
        {
            foreach (TrainMover train in FindObjectsByType<TrainMover>())
                if ((train.transform.position - switchPoint).magnitude < railSwitch.OccupiedRadiusMeters)
                    return true;
            return false;
        }

        // Palanca hacia un lado o al otro; el cartel gira un cuarto de vuelta para mostrar la otra cara.
        private float LeverTarget => railSwitch.IsDiverging ? leverDegrees : -leverDegrees;
        private float IndicatorTarget => railSwitch.IsDiverging ? 90f : 0f;

        private void LateUpdate()
        {
            float k = 1f - Mathf.Exp(-speed * Time.deltaTime);
            _lever = Mathf.Lerp(_lever, LeverTarget, k);
            _indicator = Mathf.Lerp(_indicator, IndicatorTarget, k);

            float shake = 0f;
            if (_shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= Time.deltaTime;
                shake = Mathf.Sin(_shakeTimeLeft * 45f) * blockedShakeDegrees * Mathf.Clamp01(_shakeTimeLeft / blockedShakeSeconds);
            }
            leverPivot.localRotation = Quaternion.Euler(0f, 0f, _lever + shake);
            indicatorPivot.localRotation = Quaternion.Euler(0f, _indicator, 0f);
        }
    }
}
