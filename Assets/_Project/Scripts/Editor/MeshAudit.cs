using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Revisión de mallas del arte (producción P4, pedido de Tomi: "mega pulido de los assets, que la malla quede impecable").
    /// "Survival en tren > Arte > Revisar mallas" recorre cada modelo de Assets/_Project/Art y escribe Docs/Revision_Mallas.md:
    /// - triángulos degenerados (sin área) y caras repetidas (dos triángulos en el mismo lugar: parpadean);
    /// - vértices repetidos (misma posición, normal y UV: se pueden soldar);
    /// - bordes abiertos (agujeros o piezas sueltas) y bordes "no-manifold" (compartidos por más de dos caras);
    /// - normales al revés (la normal del vértice contra la de su cara);
    /// - UV: si falta el canal 0 o el 1 (el de luz horneada), y triángulos con la textura aplastada (UV sin área);
    /// - tangentes (sin tangentes el relieve del mapa de normales sale mal).
    /// Solo mira: los arreglos van en "Arreglar mallas" (copias corregidas; los originales no se tocan).
    /// </summary>
    public static class MeshAudit
    {
        private const string ArtRoot = "Assets/_Project/Art";
        private const string ReportPath = "Docs/Revision_Mallas.md";

        public struct Result
        {
            public string Model, Mesh;
            public int Triangles, Vertices, Degenerate, DuplicateFaces, DuplicateVertices, OpenEdges, NonManifoldEdges, FlippedNormals, FlatUv;
            public bool HasUv0, HasUv1, HasTangents;
            public Vector3 Size;
        }

        [MenuItem("Survival en tren/Arte/Revisar mallas")]
        public static void Run()
        {
            var results = new List<Result>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
                    results.Add(Audit(Path.GetFileNameWithoutExtension(path), mesh));
            }
            File.WriteAllText(ReportPath, Report(results));
            AssetDatabase.Refresh();
            Debug.Log($"[Mallas] revisadas {results.Count} mallas: {ReportPath}");
        }

        private static long Key(Vector3 p, float cell = 0.0005f) =>
            ((long)Mathf.RoundToInt(p.x / cell) * 73856093L) ^ ((long)Mathf.RoundToInt(p.y / cell) * 19349663L) ^ ((long)Mathf.RoundToInt(p.z / cell) * 83492791L);

        public static Result Audit(string model, Mesh mesh)
        {
            Vector3[] v = mesh.vertices;
            Vector3[] n = mesh.normals;
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            mesh.GetUVs(0, uv0);
            mesh.GetUVs(1, uv1);
            int[] t = mesh.triangles;
            var r = new Result
            {
                Model = model, Mesh = mesh.name, Triangles = t.Length / 3, Vertices = v.Length,
                HasUv0 = uv0.Count == v.Length, HasUv1 = uv1.Count == v.Length, HasTangents = mesh.tangents.Length == v.Length,
                Size = mesh.bounds.size,
            };

            // Posiciones soldadas (los vértices partidos por UV o normal cuentan como uno para la topología).
            var weld = new Dictionary<long, int>();
            var welded = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                long k = Key(v[i]);
                if (!weld.TryGetValue(k, out int id)) { id = weld.Count; weld[k] = id; }
                welded[i] = id;
            }
            // Vértices repetidos de verdad (posición, normal y UV iguales).
            var exact = new HashSet<string>();
            for (int i = 0; i < v.Length; i++)
            {
                string k = $"{welded[i]}|{(n.Length > i ? n[i].ToString("F3") : "")}|{(r.HasUv0 ? uv0[i].ToString("F4") : "")}";
                if (!exact.Add(k)) r.DuplicateVertices++;
            }

            var edges = new Dictionary<(int, int), int>();
            var faces = new HashSet<(int, int, int)>();
            for (int i = 0; i < t.Length; i += 3)
            {
                int a = t[i], b = t[i + 1], c = t[i + 2];
                Vector3 cross = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                if (cross.magnitude < 1e-9f) { r.Degenerate++; continue; }
                Vector3 faceNormal = cross.normalized;
                if (n.Length == v.Length && Vector3.Dot(faceNormal, (n[a] + n[b] + n[c]).normalized) < -0.2f) r.FlippedNormals++;
                if (r.HasUv0)
                {
                    float uvArea = Mathf.Abs((uv0[b].x - uv0[a].x) * (uv0[c].y - uv0[a].y) - (uv0[c].x - uv0[a].x) * (uv0[b].y - uv0[a].y));
                    if (uvArea < 1e-10f) r.FlatUv++;
                }
                // Repetida = otro triángulo con los mismos tres puntos Y mirando para el mismo lado (se pisan y parpadean). Dos
                // triángulos con los mismos puntos mirando a lados opuestos son una cara doble a propósito: no cuentan.
                int[] w = { welded[a], welded[b], welded[c] };
                int rot = w[0] < w[1] ? (w[0] < w[2] ? 0 : 2) : (w[1] < w[2] ? 1 : 2);
                var oriented = (w[rot], w[(rot + 1) % 3], w[(rot + 2) % 3]); // mismo orden de vuelta, empezando por el menor
                if (!faces.Add(oriented)) r.DuplicateFaces++;
                foreach (var (p, q) in new[] { (welded[a], welded[b]), (welded[b], welded[c]), (welded[c], welded[a]) })
                {
                    var e = p < q ? (p, q) : (q, p);
                    edges[e] = edges.TryGetValue(e, out int count) ? count + 1 : 1;
                }
            }
            foreach (int count in edges.Values)
            {
                if (count == 1) r.OpenEdges++;
                else if (count > 2) r.NonManifoldEdges++;
            }
            return r;
        }

        private static string Report(List<Result> results)
        {
            var s = new StringBuilder();
            s.AppendLine("# Revisión de mallas del arte");
            s.AppendLine();
            s.AppendLine($"Generado por \"Survival en tren > Arte > Revisar mallas\" ({System.DateTime.Now:yyyy-MM-dd HH:mm}). Una fila por malla.");
            s.AppendLine();
            s.AppendLine("- **Degenerados**: triángulos sin área (sobran). **Caras repetidas**: dos triángulos en el mismo lugar (parpadean).");
            s.AppendLine("- **Vértices repetidos**: misma posición, normal y UV (se pueden soldar). **Bordes abiertos**: agujeros o piezas planas/sueltas (en una pieza cerrada tendrían que ser 0). **No-manifold**: bordes de más de dos caras.");
            s.AppendLine("- **Normales al revés**: triángulos cuya normal apunta al revés de la cara. **UV aplastadas**: triángulos con la textura sin área (se ve una raya).");
            s.AppendLine();
            s.AppendLine("| Modelo | Malla | Triángulos | Vértices | Degenerados | Caras repetidas | Vértices repetidos | Bordes abiertos | No-manifold | Normales al revés | UV aplastadas | UV0 / UV1 / tangentes | Medida (m) |");
            s.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Result r in results.OrderBy(x => x.Model).ThenBy(x => x.Mesh))
                s.AppendLine($"| {r.Model} | {r.Mesh} | {r.Triangles} | {r.Vertices} | {r.Degenerate} | {r.DuplicateFaces} | {r.DuplicateVertices} | {r.OpenEdges} | {r.NonManifoldEdges} | {r.FlippedNormals} | {r.FlatUv} | {(r.HasUv0 ? "sí" : "NO")} / {(r.HasUv1 ? "sí" : "no")} / {(r.HasTangents ? "sí" : "no")} | {r.Size.x:0.00} × {r.Size.y:0.00} × {r.Size.z:0.00} |");
            return s.ToString();
        }
    }
}
