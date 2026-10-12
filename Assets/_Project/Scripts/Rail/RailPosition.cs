using UnityEngine;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Dónde está algo sobre la vía: en qué tramo y a cuántos metros de su inicio.
    /// Es la forma en que el tren conoce su posición (Anexo C.3); sirve después
    /// para guardar la partida, la sonda y las rutas.
    /// </summary>
    [System.Serializable]
    public struct RailPosition
    {
        public RailSegment Segment;
        public float Distance;

        public RailPosition(RailSegment segment, float distance)
        {
            Segment = segment;
            Distance = distance;
        }

        public bool IsValid => Segment != null;

        public Vector3 WorldPosition => Segment.GetPosition(Distance);
        public Vector3 WorldDirection => Segment.GetDirection(Distance);

        /// <summary>
        /// Devuelve la posición movida "meters" metros (negativo = hacia atrás),
        /// pasando a los tramos conectados cuando se termina el actual.
        /// Si no hay tramo conectado, se queda en el extremo.
        /// </summary>
        /// <summary>
        /// Cuántos metros de vía quedan hasta un final sin continuación (un tope), yendo hacia adelante
        /// (direction &gt; 0) o hacia atrás. Si no hay final dentro de maxDistance, devuelve infinito.
        /// Sigue los cambios de vía igual que Advanced.
        /// </summary>
        public float DistanceToEnd(float direction, float maxDistance)
        {
            RailSegment segment = Segment;
            float distance = direction > 0f ? segment.Length - Distance : Distance;
            for (int guard = 0; guard < 64; guard++)
            {
                if (distance >= maxDistance) return float.PositiveInfinity;
                RailSegment following = direction > 0f ? segment.Next : segment.Previous;
                if (following == null) return distance;
                segment = following;
                distance += segment.Length;
            }
            return float.PositiveInfinity;
        }

        public RailPosition Advanced(float meters) => Advanced(meters, out _);

        /// <summary>Igual que Advanced, y avisa si se llegó a un extremo de vía sin continuación (un tope).</summary>
        public RailPosition Advanced(float meters, out bool reachedEnd)
        {
            RailSegment segment = Segment;
            float distance = Distance + meters;

            // Límite de vueltas por seguridad, por si alguna vez hay tramos de largo cero en bucle.
            for (int guard = 0; guard < 64; guard++)
            {
                if (distance > segment.Length && segment.Next != null)
                {
                    distance -= segment.Length;
                    segment = segment.Next;
                }
                else if (distance < 0f && segment.Previous != null)
                {
                    segment = segment.Previous;
                    distance += segment.Length;
                }
                else
                {
                    break;
                }
            }

            reachedEnd = distance > segment.Length || distance < 0f;
            return new RailPosition(segment, Mathf.Clamp(distance, 0f, segment.Length));
        }
    }
}
