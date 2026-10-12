namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Cálculos de recorrido sobre la red de vías (Anexo C.3: tramos conectados y cambios).
    /// </summary>
    public static class RailNavigation
    {
        /// <summary>
        /// Distancia por la vía desde "from" hasta un punto de la red, siguiendo los cambios como
        /// están puestos ahora. Positiva si está hacia adelante (en el sentido de los tramos), negativa
        /// si está hacia atrás. Devuelve false si no se llega dentro de maxMeters por ninguno de los
        /// dos lados (por ejemplo, está en un ramal y el cambio apunta al otro).
        /// </summary>
        public static bool TryDistanceAlong(RailPosition from, RailSegment targetSegment, float targetDistance,
            float maxMeters, out float signedMeters)
        {
            signedMeters = 0f;
            if (!from.IsValid || targetSegment == null) return false;

            float best = float.PositiveInfinity;
            bool found = false;

            // Hacia adelante.
            RailSegment segment = from.Segment;
            float travelled = -from.Distance;
            for (int guard = 0; guard < 256 && travelled <= maxMeters; guard++)
            {
                if (segment == targetSegment)
                {
                    float d = travelled + targetDistance;
                    if (d >= 0f && d <= maxMeters && d < best) { best = d; found = true; signedMeters = d; }
                }
                travelled += segment.Length;
                segment = segment.Next;
                if (segment == null || (segment == from.Segment && guard > 0)) break;
            }

            // Hacia atrás.
            segment = from.Segment;
            travelled = from.Distance - from.Segment.Length;
            for (int guard = 0; guard < 256 && travelled <= maxMeters; guard++)
            {
                if (segment == targetSegment)
                {
                    float d = travelled + (segment.Length - targetDistance);
                    if (d >= 0f && d <= maxMeters && d < best) { best = d; found = true; signedMeters = -d; }
                }
                travelled += segment.Length;
                segment = segment.Previous;
                if (segment == null || (segment == from.Segment && guard > 0)) break;
            }
            return found;
        }
    }
}
