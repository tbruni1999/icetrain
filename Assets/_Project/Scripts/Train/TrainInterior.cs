using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// El espacio de adentro del tren, en coordenadas del tren (las mismas medidas que arma el constructor de escenas: 4,4 m de
    /// ancho por fuera, piso a 1 m y techo a 3,9 m, desde el 2026-10-16). Lo usan el banco (los bultos del piso del tren) y el
    /// guardado (lo que viaja con el tren). Si el tren cambia de tamaño, se cambia acá.
    /// </summary>
    public static class TrainInterior
    {
        public const float HalfWidth = 2.15f;
        public const float HalfLength = 9.6f;
        public const float Bottom = 0.6f;
        public const float Top = 4.0f;

        /// <summary>¿Este punto (en coordenadas del tren) está adentro del tren?</summary>
        public static bool Contains(Vector3 local) =>
            Mathf.Abs(local.x) < HalfWidth && Mathf.Abs(local.z) < HalfLength && local.y > Bottom && local.y < Top;
    }
}
