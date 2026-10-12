using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Dibujo provisorio (greybox) de un tramo: balasto, durmientes y dos rieles.
    /// Está separado de RailSegment a propósito (Anexo C.3): la vía lógica y la visual
    /// se relacionan pero no son el mismo objeto. La malla se genera al cargar y no
    /// se guarda en la escena, así la escena no pesa. Usa dos materiales:
    /// el primero para el balasto y el segundo para durmientes y rieles.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RailSegment), typeof(MeshFilter), typeof(MeshRenderer))]
    public class RailSegmentVisual : MonoBehaviour
    {
        // Medidas del dibujo, en metros. Son geometría, no balance.
        private const float Gauge = 1.435f;          // trocha estándar
        private const float RailWidth = 0.07f;
        private const float RailHeight = 0.15f;
        private const float SleeperLength = 2.6f;
        private const float SleeperWidth = 0.25f;
        private const float SleeperHeight = 0.15f;
        private const float SleeperSpacing = 1.2f;
        private const float BallastWidth = 4f;
        private const float BallastHeight = 0.3f;
        private const float SampleStep = 1f;

        private Mesh _mesh;

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnDisable()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying) Destroy(_mesh);
                else DestroyImmediate(_mesh);
                _mesh = null;
            }
        }

        public void Rebuild()
        {
            var segment = GetComponent<RailSegment>();
            if (segment.Length <= 0f) return;

            if (_mesh == null)
            {
                _mesh = new Mesh { name = "RailSegmentVisual (generada)", hideFlags = HideFlags.DontSave };
                _mesh.indexFormat = IndexFormat.UInt32;
            }

            var vertices = new List<Vector3>();
            var ballastTriangles = new List<int>();
            var trackTriangles = new List<int>();

            // El eje de la vía está a la altura de la cara superior del riel.
            float railBottom = -RailHeight;
            float sleeperBottom = railBottom - SleeperHeight;
            float ballastBottom = sleeperBottom - BallastHeight;

            // Balasto y rieles: tiras continuas a lo largo de la vía.
            float length = segment.Length;
            int samples = Mathf.Max(2, Mathf.CeilToInt(length / SampleStep) + 1);
            var frames = new List<(Vector3 pos, Vector3 right)>(samples);
            for (int i = 0; i < samples; i++)
            {
                float d = Mathf.Min(length, i * SampleStep);
                frames.Add(LocalFrame(segment, d));
            }

            AddStrip(vertices, ballastTriangles, frames, 0f, BallastWidth, sleeperBottom, ballastBottom);
            AddStrip(vertices, trackTriangles, frames, -Gauge * 0.5f, RailWidth, 0f, railBottom);
            AddStrip(vertices, trackTriangles, frames, Gauge * 0.5f, RailWidth, 0f, railBottom);

            // Durmientes: cajas sueltas cada cierta distancia.
            for (float d = SleeperSpacing * 0.5f; d < length; d += SleeperSpacing)
            {
                var (pos, right) = LocalFrame(segment, d);
                Vector3 forward = Vector3.Cross(right, Vector3.up);
                AddBox(vertices, trackTriangles, pos + Vector3.up * (railBottom - SleeperHeight * 0.5f),
                    right * (SleeperLength * 0.5f), Vector3.up * (SleeperHeight * 0.5f), forward * (SleeperWidth * 0.5f));
            }

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(ballastTriangles, 0);
            _mesh.SetTriangles(trackTriangles, 1);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = _mesh;
            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null) meshCollider.sharedMesh = _mesh;
        }

        private static (Vector3 pos, Vector3 right) LocalFrame(RailSegment segment, float distance)
        {
            Vector3 pos = segment.GetLocalPosition(distance);
            Vector3 ahead = segment.GetLocalPosition(Mathf.Min(segment.Length, distance + 0.5f));
            Vector3 behind = segment.GetLocalPosition(Mathf.Max(0f, distance - 0.5f));
            Vector3 forward = ahead - behind;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward.normalized);
            return (pos, right);
        }

        // Tira con sección rectangular (tapa superior y dos laterales) que sigue los puntos.
        private static void AddStrip(List<Vector3> v, List<int> t, List<(Vector3 pos, Vector3 right)> frames,
            float lateralOffset, float width, float top, float bottom)
        {
            float half = width * 0.5f;
            int start = v.Count;
            foreach (var (pos, right) in frames)
            {
                Vector3 c = pos + right * lateralOffset;
                v.Add(c - right * half + Vector3.up * top);    // 0 arriba izquierda
                v.Add(c + right * half + Vector3.up * top);    // 1 arriba derecha
                v.Add(c - right * half + Vector3.up * bottom); // 2 abajo izquierda
                v.Add(c + right * half + Vector3.up * bottom); // 3 abajo derecha
            }

            for (int i = 0; i < frames.Count - 1; i++)
            {
                int a = start + i * 4;
                int b = a + 4;
                AddQuad(t, a + 0, b + 0, b + 1, a + 1); // tapa superior
                AddQuad(t, a + 2, b + 2, b + 0, a + 0); // lateral izquierdo
                AddQuad(t, a + 1, b + 1, b + 3, a + 3); // lateral derecho
            }
        }

        private static void AddBox(List<Vector3> v, List<int> t, Vector3 center, Vector3 ex, Vector3 ey, Vector3 ez)
        {
            // Seis caras con vértices propios para que las normales queden planas.
            AddFace(v, t, center + ey, ex, ez);
            AddFace(v, t, center - ey, ex, -ez);
            AddFace(v, t, center + ex, ez, ey);
            AddFace(v, t, center - ex, -ez, ey);
            AddFace(v, t, center + ez, -ex, ey);
            AddFace(v, t, center - ez, ex, ey);
        }

        private static void AddFace(List<Vector3> v, List<int> t, Vector3 c, Vector3 u, Vector3 w)
        {
            int s = v.Count;
            v.Add(c - u - w);
            v.Add(c - u + w);
            v.Add(c + u + w);
            v.Add(c + u - w);
            AddQuad(t, s, s + 1, s + 2, s + 3);
        }

        private static void AddQuad(List<int> t, int a, int b, int c, int d)
        {
            t.Add(a); t.Add(b); t.Add(c);
            t.Add(a); t.Add(c); t.Add(d);
        }
    }
}
