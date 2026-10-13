using System.Collections;
using SurvivalEnTren.Building;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Train;
using SurvivalEnTren.UI;
using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Lugar para dormir (sección 7: dormir es la recuperación principal de energía; la calidad de
    /// la cama decide cuánto se recupera). Hoy es el "lugar miserable" del inicio: descanso
    /// deficiente. Mejorar la cama (M8) solo cambia recoveryPerHour.
    /// - E dormir: fundido a negro, pasan las horas (el reloj las simula: hambre, calor, el tren) y
    ///   se despierta con más energía.
    /// - No se puede dormir con el tren en marcha (PROPUESTA: nadie maneja mientras dormís).
    /// - También es el lugar donde se despierta después de desmayarse (SurvivalRespawn).
    /// </summary>
    public class Bed : MonoBehaviour, IInteractable
    {
        [Tooltip("Horas que se duerme.")]
        [SerializeField, Range(1f, 16f)] private float sleepHours = 8f;
        [Tooltip("Energía que se recupera por hora durmiendo acá (lugar miserable: 6 → 8 h dan +48).")]
        [SerializeField, Range(0f, 50f)] private float recoveryPerHour = 6f;
        [Tooltip("La obra que mejora esta cama (M8): si existe, la recuperación por hora sale de ella y sube con lo aportado.")]
        [SerializeField] private BuildSite site;
        [Tooltip("Dónde aparece el jugador al despertar, desde el centro de la cama y girado con ella (metros, sin escala).")]
        [SerializeField] private Vector3 wakePointLocal = new Vector3(-0.85f, -0.2f, 0f);

        private TrainMover _train;
        private bool _busy;

        /// <summary>Cuánta energía se recupera por hora durmiendo acá ahora (con la obra, depende de cuánto se construyó).</summary>
        public float RecoveryPerHour => site != null ? site.Value : recoveryPerHour;
        public Vector3 WakePoint => transform.position + transform.rotation * wakePointLocal;
        public Rigidbody Platform => _train != null ? _train.GetComponent<Rigidbody>() : null;

        private void Awake()
        {
            _train = GetComponentInParent<TrainMover>();
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (interactor.GetComponent<PlayerVitals>() == null || _busy) return null;
            return TrainIsMoving ? "dormir (primero frená el tren)" : "dormir";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            var vitals = interactor.GetComponent<PlayerVitals>();
            var fader = interactor.GetComponent<ScreenFader>();
            if (vitals == null || fader == null || _busy || TrainIsMoving) return;
            StartCoroutine(Sleep(interactor, vitals, fader));
        }

        private bool TrainIsMoving => _train != null && Mathf.Abs(_train.SpeedMetersPerSecond) > 0.15f;

        private IEnumerator Sleep(PlayerInteractor interactor, PlayerVitals vitals, ScreenFader fader)
        {
            _busy = true;
            interactor.Motor.MovementLocked = true;
            yield return fader.FadeTo(1f, 1f);
            vitals.IsSleeping = true;
            vitals.SleepRecoveryPerHour = RecoveryPerHour;
            vitals.Clock.Skip(sleepHours);
            vitals.IsSleeping = false;
            yield return fader.FadeTo(1f, 1.2f, $"{sleepHours:0} horas después");
            yield return fader.FadeTo(0f, 1f);
            interactor.Motor.MovementLocked = false;
            _busy = false;
        }
    }
}
