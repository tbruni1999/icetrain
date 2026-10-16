using SurvivalEnTren.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// La linterna del jugador (decisión de Tomi del 2026-10-16: "el jugador tiene una linterna", en el pecho, con F). Deja las
    /// manos libres: se lleva junto con la barreta, el martillo o la sonda. Sigue la mirada con un poco de retraso, como una luz
    /// colgada del pecho. Mientras se maneja, F es el reversor: la linterna no responde.
    /// Los valores de la luz están en el componente (se ajustan en la escena del jugador).
    /// </summary>
    public class PlayerFlashlight : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private InputActionReference toggle;
        [Tooltip("La luz (un spot hijo del jugador).")]
        [SerializeField] private Light lamp;
        [Tooltip("Dónde va colgada, respecto de la cabeza (a la altura del pecho).")]
        [SerializeField] private Vector3 chestOffset = new Vector3(0.12f, -0.38f, 0.12f);
        [Tooltip("Qué tan rápido sigue la mirada (más alto = más pegada).")]
        [SerializeField, Range(1f, 40f)] private float followSpeed = 14f;
        [SerializeField] private bool startOn;

        private AudioSource _audio;
        private AudioClip _click;

        public bool IsOn => lamp != null && lamp.enabled;

        private void Awake()
        {
            if (lamp != null) lamp.enabled = startOn;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _click = SurvivalEnTren.Audio.ProceduralClips.Clank("Linterna", 1400f, 0.06f, 241);
        }

        private void OnEnable() => toggle.action.Enable();

        public void SetOn(bool on)
        {
            if (lamp == null || lamp.enabled == on) return;
            lamp.enabled = on;
            _audio.PlayOneShot(_click, 0.4f);
        }

        private void Update()
        {
            bool driving = interactor != null && interactor.IsEngaged;
            if (!driving && Time.timeScale > 0f && toggle.action.WasPressedThisFrame()) SetOn(!IsOn);
        }

        private void LateUpdate()
        {
            if (lamp == null || head == null) return;
            Transform t = lamp.transform;
            t.position = head.TransformPoint(chestOffset);
            t.rotation = Quaternion.Slerp(t.rotation, head.rotation, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }
    }
}
