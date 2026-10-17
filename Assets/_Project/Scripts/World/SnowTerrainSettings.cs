using UnityEngine;

namespace SurvivalEnTren.World
{
    /// <summary>
    /// Cómo se arma el suelo nevado de la región (producción P1; sección 19: "nieve con acumulaciones, viento y zonas
    /// erosionadas"; "restos verticales que rompan el horizonte"). Lo usa el constructor de la escena: cambiar un valor acá y
    /// reconstruir la escena. Todo es PROPUESTA.
    /// - Dunas suaves alargadas por el viento; planas cerca de la vía y de los lugares (para no cambiar lo que se camina).
    /// - Restos altos y medio enterrados lejos de la vía, que se recortan en la niebla.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Terreno nevado", fileName = "Terreno")]
    public class SnowTerrainSettings : ScriptableObject
    {
        [Header("Dunas")]
        [Tooltip("Altura máxima de las dunas, en metros.")]
        [SerializeField, Range(0f, 8f)] private float duneHeight = 3.2f;
        [Tooltip("Largo de una duna a lo largo del viento, en metros.")]
        [SerializeField, Range(10f, 200f)] private float duneLength = 80f;
        [Tooltip("Ancho de una duna (de cresta a cresta, contra el viento), en metros.")]
        [SerializeField, Range(5f, 100f)] private float duneWidth = 34f;
        [Tooltip("Pendiente máxima del terreno, en grados: si las dunas quedan más empinadas, se achatan hasta esto (que se pueda caminar).")]
        [SerializeField, Range(5f, 35f)] private float maxSlopeDegrees = 15f;
        [Tooltip("Ondulación grande del terreno, en metros de altura (lomas suaves).")]
        [SerializeField, Range(0f, 10f)] private float swellHeight = 1.5f;
        [Tooltip("Número para variar el dibujo de las dunas (el mismo número da el mismo terreno).")]
        [SerializeField] private int seed = 7;

        [Header("Zonas planas")]
        [Tooltip("Distancia a la vía que queda plana, en metros.")]
        [SerializeField, Range(0f, 40f)] private float trackFlat = 12f;
        [Tooltip("Distancia a los lugares (estación, taller, refugio, vehículo, cosas en la nieve) que queda plana, en metros.")]
        [SerializeField, Range(0f, 40f)] private float placeFlat = 14f;
        [Tooltip("Cuánto tarda en subir de plano a la duna completa, en metros.")]
        [SerializeField, Range(1f, 80f)] private float blend = 35f;

        [Header("Capas de nieve (Parte 1b)")]
        [Tooltip("Tamaño de las manchas de nieve compactada por el viento en el campo, en metros.")]
        [SerializeField, Range(10f, 300f)] private float windPatchSize = 45f;
        [Tooltip("Qué parte del campo tiene manchas (0 = nada, 1 = todo).")]
        [SerializeField, Range(0f, 1f)] private float windPatchCover = 0.4f;
        [Tooltip("Cuánto se ve la nieve compactada en las manchas (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float windPatchStrength = 0.65f;
        [Tooltip("Ancho de la nieve sucia a cada lado de la vía, en metros.")]
        [SerializeField, Range(0f, 20f)] private float dirtyTrackWidth = 5f;
        [Tooltip("Cuánto se ve la nieve sucia pegada a la vía (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float dirtyTrackStrength = 0.4f;

        [Header("Restos en el horizonte")]
        [Tooltip("Cuántos restos altos (torres, chimeneas, postes, un tanque) hay alrededor.")]
        [SerializeField, Range(0, 40)] private int remnants = 12;
        [Tooltip("Distancia mínima y máxima a la vía, en metros.")]
        [SerializeField, Range(30f, 1000f)] private float remnantMinDistance = 140f;
        [SerializeField, Range(50f, 1400f)] private float remnantMaxDistance = 650f;

        public float DuneHeight => duneHeight;
        public float DuneLength => duneLength;
        public float DuneWidth => duneWidth;
        public float SwellHeight => swellHeight;
        public float MaxSlopeDegrees => maxSlopeDegrees;
        public int Seed => seed;
        public float TrackFlat => trackFlat;
        public float PlaceFlat => placeFlat;
        public float Blend => blend;
        public float WindPatchSize => windPatchSize;
        public float WindPatchCover => windPatchCover;
        public float WindPatchStrength => windPatchStrength;
        public float DirtyTrackWidth => Mathf.Max(0.5f, dirtyTrackWidth);
        public float DirtyTrackStrength => dirtyTrackStrength;
        public int Remnants => remnants;
        public float RemnantMinDistance => remnantMinDistance;
        public float RemnantMaxDistance => Mathf.Max(remnantMinDistance + 10f, remnantMaxDistance);
    }
}
