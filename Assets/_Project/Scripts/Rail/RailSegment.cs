using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Un tramo de vía: una línea de puntos (en coordenadas locales) que el tren recorre.
    /// Los tramos se conectan entre sí con "Next" y "Previous" para formar la red
    /// (Anexo C.3: segmentos conectados, no una curva infinita). Los cambios de vía
    /// llegan en M2; por ahora cada tramo tiene un solo siguiente y un solo anterior.
    /// </summary>
    public class RailSegment : MonoBehaviour
    {
        [Tooltip("Puntos del eje de la vía, en coordenadas locales, a la altura de la cara superior del riel.")]
        [SerializeField] private List<Vector3> localPoints = new List<Vector3>();

        [SerializeField] private RailSegment next;
        [SerializeField] private RailSegment previous;
        [Tooltip("Si el tramo termina en un cambio de vía, el siguiente tramo lo decide el cambio.")]
        [SerializeField] private RailSwitch exitSwitch;

        // Distancia acumulada (en metros) desde el inicio del tramo hasta cada punto.
        private float[] _cumulative;
        private float _length;

        public RailSegment Next => exitSwitch != null ? exitSwitch.ActiveExit : next;
        public RailSegment Previous => previous;
        public IReadOnlyList<Vector3> LocalPoints => localPoints;

        /// <summary>Largo del tramo en metros.</summary>
        public float Length
        {
            get
            {
                EnsureCache();
                return _length;
            }
        }

        public void SetPoints(List<Vector3> points)
        {
            localPoints = new List<Vector3>(points);
            _cumulative = null;
        }

        public void Connect(RailSegment nextSegment)
        {
            next = nextSegment;
            if (nextSegment != null) nextSegment.previous = this;
        }

        public void SetPrevious(RailSegment previousSegment) => previous = previousSegment;

        public void SetExitSwitch(RailSwitch railSwitch) => exitSwitch = railSwitch;

        /// <summary>Posición en el mundo a cierta distancia (en metros) desde el inicio del tramo.</summary>
        public Vector3 GetPosition(float distance)
        {
            return transform.TransformPoint(GetLocalPosition(distance));
        }

        /// <summary>Dirección de avance (normalizada, en el mundo) a cierta distancia del inicio del tramo.</summary>
        public Vector3 GetDirection(float distance)
        {
            EnsureCache();
            int i = FindSpan(distance);
            Vector3 d = localPoints[i + 1] - localPoints[i];
            return transform.TransformDirection(d).normalized;
        }

        public Vector3 GetLocalPosition(float distance)
        {
            EnsureCache();
            if (localPoints.Count == 0) return Vector3.zero;
            if (localPoints.Count == 1) return localPoints[0];

            distance = Mathf.Clamp(distance, 0f, _length);
            int i = FindSpan(distance);
            float spanLength = _cumulative[i + 1] - _cumulative[i];
            float t = spanLength > 0f ? (distance - _cumulative[i]) / spanLength : 0f;
            return Vector3.Lerp(localPoints[i], localPoints[i + 1], t);
        }

        // Índice del punto donde empieza el tramito que contiene "distance" (búsqueda binaria).
        private int FindSpan(float distance)
        {
            int lo = 0;
            int hi = localPoints.Count - 2;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (_cumulative[mid] <= distance) lo = mid;
                else hi = mid - 1;
            }
            return Mathf.Max(0, lo);
        }

        private void EnsureCache()
        {
            if (_cumulative != null && _cumulative.Length == localPoints.Count) return;

            _cumulative = new float[localPoints.Count];
            _length = 0f;
            for (int i = 1; i < localPoints.Count; i++)
            {
                _length += Vector3.Distance(localPoints[i - 1], localPoints[i]);
                _cumulative[i] = _length;
            }
        }

        private void OnValidate()
        {
            _cumulative = null;
        }

        private void OnDrawGizmos()
        {
            if (localPoints.Count < 2) return;
            Gizmos.color = new Color(1f, 0.8f, 0.2f);
            for (int i = 1; i < localPoints.Count; i++)
                Gizmos.DrawLine(transform.TransformPoint(localPoints[i - 1]), transform.TransformPoint(localPoints[i]));
        }
    }
}
