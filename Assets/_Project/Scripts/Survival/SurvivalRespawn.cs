using System.Collections;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Player;
using SurvivalEnTren.UI;
using UnityEngine;

namespace SurvivalEnTren.Survival
{
    /// <summary>
    /// Qué pasa cuando la vida llega a 0 (DECISIÓN de Tomi, 2026-10-12: el castigo es de recursos,
    /// no de progreso):
    /// - Lo que llevaba en la mano queda donde cayó (desde M7, también lo que llevaba encima).
    /// - Pasan varias horas "fuera de combate" (SurvivalSettings.knockoutHours): el mundo sigue
    ///   (desde M6 la calefacción gasta combustible).
    /// - Despierta en la cama del tren con hambre y poca energía (SurvivalSettings.wakeUpVitals).
    /// - No se pierde nada construido.
    /// </summary>
    public class SurvivalRespawn : MonoBehaviour
    {
        [SerializeField] private PlayerVitals vitals;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private ScreenFader fader;
        [SerializeField] private Bed bed;

        public int KnockoutCount { get; private set; }

        private void OnEnable() => vitals.Died += OnDied;
        private void OnDisable() => vitals.Died -= OnDied;

        private void OnDied() => StartCoroutine(KnockOut());

        private IEnumerator KnockOut()
        {
            KnockoutCount++;
            if (carrier != null && carrier.Holding != null) carrier.Drop(throwIt: false);
            motor.MovementLocked = true;
            yield return fader.FadeTo(1f, 1.5f);

            SurvivalSettings s = vitals.Settings;
            vitals.Clock.Skip(s.KnockoutHours);
            motor.TeleportTo(bed.WakePoint, bed.Platform);
            Vector4 wake = s.WakeUpVitals;
            vitals.Revive(wake.x, wake.y, wake.z, wake.w);

            yield return fader.FadeTo(1f, 1.5f, $"Despertás en el tren, {s.KnockoutHours:0} horas después");
            yield return fader.FadeTo(0f, 1.2f);
            motor.MovementLocked = false;
        }
    }
}
