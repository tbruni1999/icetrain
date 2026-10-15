using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Dibujo de un tramo: balasto, durmientes y dos rieles.
    /// Está separado de RailSegment a propósito (Anexo C.3): la vía lógica y la visual
    /// se relacionan pero no son el mismo objeto. La malla se genera al cargar y no
    /// se guarda en la escena, así la escena no pesa. Usa tres materiales (producción P2: con texturas de la librería MR-04):
    /// el primero para el balasto, el segundo para los durmientes y el tercero para los rieles. Las coordenadas de textura van en
    /// metros divididos por el tamaño real de cada textura (los campos "...TextureMeters"), así se ven a escala.
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

        [Tooltip("Tamaño real de la textura de cada material, en metros (catalogo.json de la librería: escala_m).")]
        [SerializeField] private float ballastTextureMeters = 2f;
        [SerializeField] private float sleeperTextureMeters = 1f;
        [SerializeField] private float railTextureMeters = 1f;

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

        public void SetTextureMeters(float ballast, float sleeper, float rail)
        {
            ballastTextureMeters = Mathf.Max(0.05f, ballast);
            sleeperTextureMeters = Mathf.Max(0.05f, sleeper);
            railTextureMeters = Mathf.Max(0.05f, rail);
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
            var uvs = new List<Vector2>();
            var ballastTriangles = new List<int>();
            var sleeperTriangles = new List<int>();
            var railTriangles = new List<int>();

            // El eje de la vía está a la altura de la cara superior del riel.
            float railBottom = -RailHeight;
            float sleeperBottom = railBottom - SleeperHeight;
            float ballastBottom = sleeperBottom - BallastHeight;

            // Balasto y rieles: tiras continuas a lo largo de la vía.
            float length = segment.Length;
            int samples = Mathf.Max(2, Mathf.CeilToInt(length / SampleStep) + 1);
            var frames = new List<(Vector3 pos, Vector3 right, float d)>(samples);
            for (int i = 0; i < samples; i++)
            {
                float d = Mathf.Min(length, i * SampleStep);
                var (pos, right) = LocalFrame(segment, d);
                frames.Add((pos, right, d));
            }

            AddStrip(vertices, uvs, ballastTriangles, frames, 0f, BallastWidth, sleeperBottom, ballastBottom, ballastTextureMeters);
            AddStrip(vertices, uvs, railTriangles, frames, -Gauge * 0.5f, RailWidth, 0f, railBottom, railTextureMeters);
            AddStrip(vertices, uvs, railTriangles, frames, Gauge * 0.5f, RailWidth, 0f, railBottom, railTextureMeters);

            // Durmientes: cajas sueltas cada cierta distancia (la textura corrida en cada uno, para que no se repitan iguales).
            for (float d = SleeperSpacing * 0.5f; d < length; d += SleeperSpacing)
            {
                var (pos, right) = LocalFrame(segment, d);
                Vector3 forward = Vector3.Cross(right, Vector3.up);
                var offset = new Vector2(Mathf.Repeat(d * 0.37f, 1f), Mathf.Repeat(d * 0.61f, 1f));
                AddBox(vertices, uvs, sleeperTriangles, pos + Vector3.up * (railBottom - SleeperHeight * 0.5f),
                    right * (SleeperLength * 0.5f), Vector3.up * (SleeperHeight * 0.5f), forward * (SleeperWidth * 0.5f), sleeperTextureMeters, offset);
            }

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetUVs(0, uvs);
            _mesh.subMeshCount = 3;
            _mesh.SetTriangles(ballastTriangles, 0);
            _mesh.SetTriangles(sleeperTriangles, 1);
            _mesh.SetTriangles(railTriangles, 2);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
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

        // Tira con sección rectangular (tapa superior y dos laterales) que sigue los puntos. Cada cara con sus propios vértices,
        // para que la textura corra bien: a lo largo de la vía (metros recorridos) y a lo ancho o a lo alto.
        private static void AddStrip(List<Vector3> v, List<Vector2> uv, List<int> t, List<(Vector3 pos, Vector3 right, float d)> frames,
            float lateralOffset, float width, float top, float bottom, float textureMeters)
        {
            float half = width * 0.5f;
            void Face(System.Func<Vector3, Vector3, Vector3> a, System.Func<Vector3, Vector3, Vector3> b, float ua, float ub)
            {
                int start = v.Count;
                foreach (var (pos, right, d) in frames)
                {
                    Vector3 c = pos + right * lateralOffset;
                    v.Add(a(c, right)); uv.Add(new Vector2(ua / textureMeters, d / textureMeters));
                    v.Add(b(c, right)); uv.Add(new Vector2(ub / textureMeters, d / textureMeters));
                }
                for (int i = 0; i < frames.Count - 1; i++)
                {
                    int p = start + i * 2;
                    AddQuad(t, p, p + 2, p + 3, p + 1);
                }
            }
            float height = top - bottom;
            Face((c, r) => c - r * half + Vector3.up * top, (c, r) => c + r * half + Vector3.up * top, -half, half);             // tapa
            Face((c, r) => c - r * half + Vector3.up * bottom, (c, r) => c - r * half + Vector3.up * top, 0f, height);           // lateral izquierdo
            Face((c, r) => c + r * half + Vector3.up * top, (c, r) => c + r * half + Vector3.up * bottom, height, 0f);           // lateral derecho
        }

        private static void AddBox(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 center, Vector3 ex, Vector3 ey, Vector3 ez,
            float textureMeters, Vector2 offset)
        {
            // Seis caras con vértices propios para que las normales queden planas.
            AddFace(v, uv, t, center + ey, ex, ez, textureMeters, offset);
            AddFace(v, uv, t, center - ey, ex, -ez, textureMeters, offset);
            AddFace(v, uv, t, center + ex, ez, ey, textureMeters, offset);
            AddFace(v, uv, t, center - ex, -ez, ey, textureMeters, offset);
            AddFace(v, uv, t, center + ez, -ex, ey, textureMeters, offset);
            AddFace(v, uv, t, center - ez, ex, ey, textureMeters, offset);
        }

        private static void AddFace(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 c, Vector3 u, Vector3 w, float textureMeters, Vector2 offset)
        {
            int s = v.Count;
            float a = u.magnitude / textureMeters, b = w.magnitude / textureMeters;
            v.Add(c - u - w); uv.Add(offset + new Vector2(-a, -b));
            v.Add(c - u + w); uv.Add(offset + new Vector2(-a, b));
            v.Add(c + u + w); uv.Add(offset + new Vector2(a, b));
            v.Add(c + u - w); uv.Add(offset + new Vector2(a, -b));
            AddQuad(t, s, s + 1, s + 2, s + 3);
        }

        private static void AddQuad(List<int> t, int a, int b, int c, int d)
        {
            t.Add(a); t.Add(b); t.Add(c);
            t.Add(a); t.Add(c); t.Add(d);
        }
    }
}
