using System;
using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.Building
{
    /// <summary>Una cantidad de piezas de un material que pide una fase de una obra.</summary>
    [Serializable]
    public struct BuildRequirement
    {
        public ItemDefinition item;
        [Min(1)] public int count;
    }

    /// <summary>Una fase de una obra: cómo se llama, qué materiales pide y cuánto tarda el banco en armar su kit.</summary>
    [Serializable]
    public struct BuildPhase
    {
        public string name;
        public BuildRequirement[] requirements;
        [Tooltip("Minutos del reloj del juego que el banco tarda en armar el kit de esta fase (0 = el valor de la obra).")]
        [Min(0f)] public float workMinutes;

        public int TotalUnits
        {
            get
            {
                int total = 0;
                if (requirements != null) foreach (BuildRequirement r in requirements) total += r.count;
                return total;
            }
        }
    }

    /// <summary>
    /// Una obra del tren que se construye por etapas visibles (sección 11, DECISIÓN: toda mejora importante tiene
    /// presencia física, se construye por etapas y muestra un cambio en cada avance). Es un dato por tipo de obra:
    /// las fases, los materiales de cada una y qué vale el efecto en cada nivel (la cama recupera más energía).
    /// Desde el 2026-10-16 (decisión de Tomi: "que se mejoren en el workbench y tengan un timer") cada fase se arma en el
    /// banco como un kit, con su tiempo, y el jugador lo lleva hasta la obra y lo instala. Todo se ajusta en el archivo,
    /// sin tocar código (Anexo B.3).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Construcción/Obra", fileName = "Obra")]
    public class BuildProject : ScriptableObject
    {
        [Tooltip("Cómo se llama la obra (\"cama improvisada\").")]
        [SerializeField] private string displayName = "obra";
        [Tooltip("Las fases, en orden. Cada una pide materiales y muestra su parte de la obra cuando se instala.")]
        [SerializeField] private BuildPhase[] phases = Array.Empty<BuildPhase>();
        [Tooltip("Cuánto vale el efecto de la obra en cada nivel: 0 = sin avance, 1 = primera fase instalada, y así hasta el " +
                 "final. Debe tener una más que las fases.")]
        [SerializeField] private float[] levelValues = Array.Empty<float>();
        [Tooltip("Solo para muebles con cuadrícula: el tamaño de la cuadrícula en cada nivel, en casillas (ancho, alto). " +
                 "(0, 0) = todavía no sirve para guardar. Debe tener una más que las fases. Solo crece de un nivel al siguiente.")]
        [SerializeField] private Vector2Int[] levelGridSizes = Array.Empty<Vector2Int>();
        [Tooltip("Minutos del reloj del juego que el banco tarda en armar el kit de una fase que no dice otro valor.")]
        [SerializeField, Range(1f, 600f)] private float workMinutes = 30f;
        [Tooltip("Cuántos segundos hay que mantener E para instalar un kit en la obra.")]
        [SerializeField, Range(0.2f, 10f)] private float installSeconds = 2.5f;

        public string DisplayName => displayName;
        public int PhaseCount => phases.Length;
        public BuildPhase Phase(int index) => phases[index];
        public float InstallSeconds => installSeconds;
        /// <summary>Minutos del juego que tarda el banco en armar el kit de una fase.</summary>
        public float PhaseMinutes(int index) => phases[index].workMinutes > 0f ? phases[index].workMinutes : workMinutes;

        /// <summary>Tamaño de la cuadrícula del mueble en un nivel ((0, 0) si todavía no guarda nada).</summary>
        public Vector2Int LevelGridSize(int level)
        {
            if (levelGridSizes.Length == 0) return Vector2Int.zero;
            return levelGridSizes[Mathf.Clamp(level, 0, levelGridSizes.Length - 1)];
        }

        /// <summary>Valor del efecto en un nivel (el último si se pide uno de más).</summary>
        public float LevelValue(int level)
        {
            if (levelValues.Length == 0) return 0f;
            return levelValues[Mathf.Clamp(level, 0, levelValues.Length - 1)];
        }
    }
}
