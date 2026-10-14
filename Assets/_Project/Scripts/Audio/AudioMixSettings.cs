using UnityEngine;

namespace SurvivalEnTren.Audio
{
    /// <summary>
    /// La mezcla de sonido (M13, sección 19 y Anexo E.5): cuánto suena cada cosa en cada lugar. Son valores para ajustar a
    /// oído en el archivo, sin tocar código. Todos son PROPUESTA.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Audio/Mezcla", fileName = "AudioMezcla")]
    public class AudioMixSettings : ScriptableObject
    {
        [Header("Viento (afuera, en un refugio, en el tren)")]
        [SerializeField, Range(0f, 1f)] private float windOutside = 0.55f;
        [SerializeField, Range(0f, 1f)] private float windShelter = 0.22f;
        [Tooltip("En el tren con las puertas cerradas: casi no se oye (la diferencia al cerrar tiene que ser fuerte, sección 19).")]
        [SerializeField, Range(0f, 1f)] private float windTrainClosed = 0.06f;
        [SerializeField, Range(0f, 1f)] private float windTrainOpen = 0.3f;
        [Tooltip("Qué tan apagado (agudos cortados) se oye el viento detrás de las paredes del tren, en Hz.")]
        [SerializeField, Range(200f, 22000f)] private float trainCutoffHz = 650f;
        [SerializeField, Range(200f, 22000f)] private float shelterCutoffHz = 1500f;
        [Tooltip("Qué tan rápido cambia el ambiente al entrar, salir o cerrar una puerta (más alto = más inmediato).")]
        [SerializeField, Range(1f, 30f)] private float fadeSpeed = 12f;

        [Header("Adentro del tren")]
        [SerializeField, Range(0f, 1f)] private float heaterVolume = 0.16f;
        [SerializeField, Range(0f, 1f)] private float engineIdleVolume = 0.22f;
        [SerializeField, Range(0f, 1f)] private float engineLoadVolume = 0.45f;
        [Tooltip("El golpeteo de las ruedas en las juntas de los rieles.")]
        [SerializeField, Range(0f, 1f)] private float railClickVolume = 0.3f;
        [Tooltip("Cada cuántos metros hay una junta de riel.")]
        [SerializeField, Range(5f, 30f)] private float railJointMeters = 12.5f;

        [Header("Golpes y aparatos")]
        [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float probeVolume = 0.2f;

        public float WindOutside => windOutside;
        public float WindShelter => windShelter;
        public float WindTrainClosed => windTrainClosed;
        public float WindTrainOpen => windTrainOpen;
        public float TrainCutoffHz => trainCutoffHz;
        public float ShelterCutoffHz => shelterCutoffHz;
        public float FadeSpeed => fadeSpeed;
        public float HeaterVolume => heaterVolume;
        public float EngineIdleVolume => engineIdleVolume;
        public float EngineLoadVolume => engineLoadVolume;
        public float RailClickVolume => railClickVolume;
        public float RailJointMeters => railJointMeters;
        public float EffectsVolume => effectsVolume;
        public float ProbeVolume => probeVolume;
    }
}
