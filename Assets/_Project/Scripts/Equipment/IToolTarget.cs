using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>Un golpe con una herramienta de la mano: con qué, dónde y desde dónde.</summary>
    public readonly struct ToolHit
    {
        public readonly GearId Tool;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly Collider Collider;
        public readonly ToolBelt By;

        public ToolHit(GearId tool, Vector3 point, Vector3 normal, Collider collider, ToolBelt by)
        {
            Tool = tool;
            Point = point;
            Normal = normal;
            Collider = collider;
            By = by;
        }
    }

    /// <summary>Lo que reacciona a los golpes de una herramienta (H2: lo que se desarma a golpes; H4: la pieza en el yunque).</summary>
    public interface IToolTarget
    {
        /// <summary>Recibe el golpe. Devuelve true si el golpe sirvió (false = rebota: herramienta equivocada).</summary>
        bool OnToolHit(in ToolHit hit);
    }
}
