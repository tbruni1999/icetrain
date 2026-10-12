using UnityEngine;

namespace SurvivalEnTren.World
{
    /// <summary>
    /// Lo implementa cualquier componente que guarde posiciones del mundo por su cuenta
    /// (fuera del Transform) y que tenga que corregirlas cuando FloatingOrigin re-centra el mundo.
    /// </summary>
    public interface IOriginShiftListener
    {
        /// <summary>Se llamó justo después de mover todo el mundo "delta" metros.</summary>
        void OnOriginShifted(Vector3 delta);
    }
}
