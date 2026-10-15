using System.Collections;
using SurvivalEnTren.Player;
using SurvivalEnTren.Survival;
using UnityEngine;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// El despertar (sección 20.1, producción P3): al empezar una partida nueva desde el menú, la pantalla arranca negra, se oye
    /// el viento y el tren; el jugador está acostado en el lugar miserable del tren (la tela en el piso) mirando el techo, y la
    /// vista se abre y se levanta despacio. Sin textos ni tutoriales. Solo con "Nueva partida" (no al cargar ni en las pruebas,
    /// que abren la escena directo).
    /// </summary>
    public class WakeUpIntro : MonoBehaviour
    {
        /// <summary>Lo pone el menú principal antes de abrir la escena con "Nueva partida".</summary>
        public static bool PlayOnNextLoad;

        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private ScreenFader fader;
        [SerializeField] private Bed bed;
        [Tooltip("Segundos en negro (se oye el viento y el tren).")]
        [SerializeField] private float blackSeconds = 2.5f;
        [Tooltip("Segundos que tarda la vista en abrirse.")]
        [SerializeField] private float fadeSeconds = 3f;
        [Tooltip("Segundos que tarda en levantarse (la vista pasa del techo al frente).")]
        [SerializeField] private float riseSeconds = 4.5f;

        public bool Playing { get; private set; }

        private IEnumerator Start()
        {
            if (!PlayOnNextLoad) yield break;
            PlayOnNextLoad = false;
            Playing = true;
            motor.MovementLocked = true;
            StartCoroutine(fader.FadeTo(1f, 0f));
            yield return null;
            motor.TeleportTo(bed.WakePoint, bed.Platform);
            // Mirando hacia el pasillo (el centro del tren), con la vista en el techo: está acostado.
            Vector3 toAisle = bed.Platform != null ? bed.Platform.position - bed.WakePoint : -bed.transform.right;
            toAisle.y = 0f;
            if (toAisle.sqrMagnitude > 0.01f) motor.SetYaw(Quaternion.LookRotation(toAisle).eulerAngles.y);
            look.SetPitch(-75f);

            yield return new WaitForSeconds(blackSeconds);
            StartCoroutine(fader.FadeTo(0f, fadeSeconds));
            float t = 0f;
            while (t < riseSeconds)
            {
                t += Time.deltaTime;
                look.SetPitch(Mathf.Lerp(-75f, 8f, Mathf.SmoothStep(0f, 1f, t / riseSeconds)));
                yield return null;
            }
            motor.MovementLocked = false;
            Playing = false;
        }
    }
}
