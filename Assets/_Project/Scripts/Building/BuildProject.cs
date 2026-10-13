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

    /// <summary>Una fase de una obra: cómo se llama y qué materiales pide.</summary>
    [Serializable]
    public struct BuildPhase
    {
        public string name;
        public BuildRequirement[] requirements;

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
    /// las fases, los materiales de cada una y qué vale el efecto en cada nivel (la cama recupera más energía, el estante
    /// guarda más). Todo se ajusta en el archivo, sin tocar código (Anexo B.3).
    /// </summary>
    [CreateAssetMenu(menuName = "Survival en tren/Construcción/Obra", fileName = "Obra")]
    public class BuildProject : ScriptableObject
    {
        [Tooltip("Cómo se llama la obra (\"cama improvisada\").")]
        [SerializeField] private string displayName = "obra";
        [Tooltip("Las fases, en orden. Cada una pide materiales y muestra su parte de la obra cuando se completa.")]
        [SerializeField] private BuildPhase[] phases = Array.Empty<BuildPhase>();
        [Tooltip("Cuánto vale el efecto de la obra en cada nivel: 0 = sin avance, 1 = primera fase completa, y así hasta el " +
                 "final. Debe tener una más que las fases. Dentro de una fase el valor sube de a poco con lo aportado " +
                 "(sección 11.2: una fase incompleta ya aporta valor).")]
        [SerializeField] private float[] levelValues = Array.Empty<float>();
        [Tooltip("Solo para muebles con cuadrícula (un estante): el tamaño de la cuadrícula en cada nivel, en casillas (ancho, alto). " +
                 "(0, 0) = todavía no sirve para guardar. Debe tener una más que las fases. Solo crece de un nivel al siguiente.")]
        [SerializeField] private Vector2Int[] levelGridSizes = Array.Empty<Vector2Int>();
        [Tooltip("Cuántas piezas se aportan por segundo manteniendo E.")]
        [SerializeField, Range(0.5f, 20f)] private float unitsPerSecond = 2f;

        public string DisplayName => displayName;
        public int PhaseCount => phases.Length;
        public BuildPhase Phase(int index) => phases[index];
        public float UnitsPerSecond => unitsPerSecond;

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
