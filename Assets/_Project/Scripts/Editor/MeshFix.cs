using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Arreglo de mallas del arte (producción P4, pedido de Tomi: "mega pulido de los assets, que la malla quede impecable").
    /// "Survival en tren > Arte > Arreglar mallas" hace una copia corregida de cada malla de cada modelo de Assets/_Project/Art en
    /// Art/Generado/Mallas/&lt;modelo&gt;/ (los FBX no se tocan) y el armado de las escenas usa las copias:
    /// - saca los triángulos sin área y los repetidos que se pisan (parpadean);
    /// - da vuelta los triángulos que miran al revés de sus normales (se veían oscuros o desaparecían);
    /// - rehace la textura de los triángulos con la textura aplastada (rayas): proyección de caja a tamaño real, con el tamaño de
    ///   la textura de su material (catalogo.json);
    /// - suelda los vértices repetidos (misma posición, normal, textura y color).
    /// Lo que no se arregla solo (agujeros y bordes abiertos) queda en Docs/Revision_Mallas.md para el chat de arte.
    /// </summary>
    public static class MeshFix
    {
        public const string GeneratedRoot = "Assets/_Project/Art/Generado/Mallas";

        public struct Stats { public int Degenerate, Duplicates, Flipped, Reuv, Welded; }

        [MenuItem("Survival en tren/Arte/Arreglar mallas")]
        public static void Run()
        {
            var lines = new List<string>();
            int meshes = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string model = Path.GetFileNameWithoutExtension(path);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                string folder = $"{GeneratedRoot}/{model}";
                EnsureFolder(folder);
                foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    Mesh source = filter.sharedMesh;
                    if (source == null) continue;
                    var renderer = filter.GetComponent<MeshRenderer>();
                    Material[] materials = renderer != null ? renderer.sharedMaterials : new Material[0];
                    var meters = new float[source.subMeshCount];
                    for (int s = 0; s < meters.Length; s++)
                        meters[s] = s < materials.Length && materials[s] != null ? TestScenesBuilder.TextureMetersFor(materials[s].name) : 1f;
                    Mesh fixedMesh = Fix(source, meters, out Stats stats);
                    Save(fixedMesh, $"{folder}/{source.name}.asset");
                    meshes++;
                    if (stats.Degenerate + stats.Duplicates + stats.Flipped + stats.Reuv + stats.Welded > 0)
                        lines.Add($"| {model} | {source.name} | {stats.Degenerate} | {stats.Duplicates} | {stats.Flipped} | {stats.Reuv} | {stats.Welded} |");
                }
            }
            AssetDatabase.SaveAssets();

            // El informe: lo que se arregló, debajo de la revisión.
            MeshAudit.Run();
            var report = new StringBuilder(File.ReadAllText("Docs/Revision_Mallas.md"));
            report.AppendLine();
            report.AppendLine("## Lo que se arregló (copias en Art/Generado/Mallas; los FBX no se tocan)");
            report.AppendLine();
            report.AppendLine($"\"Survival en tren > Arte > Arreglar mallas\" ({System.DateTime.Now:yyyy-MM-dd HH:mm}): {meshes} mallas revisadas. La tabla de arriba es de los FBX tal como se importan; esta, lo que se corrigió en las copias que usa el juego.");
            report.AppendLine();
            report.AppendLine("| Modelo | Malla | Triángulos sin área quitados | Repetidos quitados | Dados vuelta | Textura rehecha (triángulos) | Vértices soldados |");
            report.AppendLine("|---|---|---|---|---|---|---|");
            foreach (string line in lines) report.AppendLine(line);
            File.WriteAllText("Docs/Revision_Mallas.md", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"[Mallas] arregladas {lines.Count} de {meshes} mallas (copias en {GeneratedRoot})");
        }

        /// <summary>La copia arreglada de una malla de un modelo del arte, si existe.</summary>
        public static Mesh FixedFor(string model, Mesh original) =>
            original == null ? null : AssetDatabase.LoadAssetAtPath<Mesh>($"{GeneratedRoot}/{model}/{original.name}.asset");

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static void Save(Mesh mesh, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return; }
            string name = existing.name;
            EditorUtility.CopySerialized(mesh, existing);
            existing.name = name;
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
        }

        private static long PositionKey(Vector3 p) =>
            ((long)Mathf.RoundToInt(p.x / 0.0005f) * 73856093L) ^ ((long)Mathf.RoundToInt(p.y / 0.0005f) * 19349663L) ^ ((long)Mathf.RoundToInt(p.z / 0.0005f) * 83492791L);

        public static Mesh Fix(Mesh source, float[] textureMeters, out Stats stats)
        {
            stats = new Stats();
            var v = new List<Vector3>(source.vertices);
            var n = new List<Vector3>(source.normals);
            var tg = new List<Vector4>(source.tangents);
            var c = new List<Color>(source.colors);
            var uv0 = new List<Vector2>(); source.GetUVs(0, uv0);
            var uv1 = new List<Vector2>(); source.GetUVs(1, uv1);
            bool hasN = n.Count == v.Count, hasT = tg.Count == v.Count, hasC = c.Count == v.Count, hasUv0 = uv0.Count == v.Count, hasUv1 = uv1.Count == v.Count;

            // Copia un vértice (para rehacerle la textura sin tocar a sus vecinos).
            int CloneVertex(int i, Vector2 newUv)
            {
                v.Add(v[i]);
                if (hasN) n.Add(n[i]);
                if (hasT) tg.Add(tg[i]);
                if (hasC) c.Add(c[i]);
                if (hasUv0) uv0.Add(newUv);
                if (hasUv1) uv1.Add(uv1[i]);
                return v.Count - 1;
            }

            var subs = new List<int>[source.subMeshCount];
            var welded = new long[v.Count];
            for (int i = 0; i < v.Count; i++) welded[i] = PositionKey(v[i]);
            (long, long, long) Oriented(long p, long q, long r)
            {
                int rot = p < q ? (p < r ? 0 : 2) : (q < r ? 1 : 2);
                long[] w = { p, q, r };
                return (w[rot], w[(rot + 1) % 3], w[(rot + 2) % 3]);
            }
            // Las caras dobles a propósito (el mismo triángulo mirando a los dos lados) no se tocan.
            var allOriented = new HashSet<(long, long, long)>();
            for (int s = 0; s < source.subMeshCount; s++)
            {
                int[] t = source.GetTriangles(s);
                for (int k = 0; k < t.Length; k += 3) allOriented.Add(Oriented(welded[t[k]], welded[t[k + 1]], welded[t[k + 2]]));
            }
            for (int s = 0; s < source.subMeshCount; s++)
            {
                subs[s] = new List<int>();
                var seen = new HashSet<(long, long, long)>();
                int[] t = source.GetTriangles(s);
                for (int k = 0; k < t.Length; k += 3)
                {
                    int a = t[k], b = t[k + 1], d = t[k + 2];
                    Vector3 cross = Vector3.Cross(v[b] - v[a], v[d] - v[a]);
                    if (cross.magnitude < 1e-9f) { stats.Degenerate++; continue; }
                    Vector3 faceNormal = cross.normalized;
                    bool doubleSided = allOriented.Contains(Oriented(welded[a], welded[d], welded[b]));
                    if (hasN && !doubleSided && Vector3.Dot(faceNormal, (n[a] + n[b] + n[d]).normalized) < -0.2f)
                    {
                        (b, d) = (d, b); // la vuelta del triángulo, para que mire hacia donde dicen sus normales
                        faceNormal = -faceNormal;
                        stats.Flipped++;
                    }
                    if (!seen.Add(Oriented(welded[a], welded[b], welded[d]))) { stats.Duplicates++; continue; }

                    if (hasUv0)
                    {
                        float uvArea = Mathf.Abs((uv0[b].x - uv0[a].x) * (uv0[d].y - uv0[a].y) - (uv0[d].x - uv0[a].x) * (uv0[b].y - uv0[a].y));
                        if (uvArea < 1e-10f)
                        {
                            // Proyección de caja: el plano según hacia dónde mira la cara, a tamaño real.
                            float m = Mathf.Max(0.05f, s < textureMeters.Length ? textureMeters[s] : 1f);
                            Vector3 an = new Vector3(Mathf.Abs(faceNormal.x), Mathf.Abs(faceNormal.y), Mathf.Abs(faceNormal.z));
                            Vector2 Project(Vector3 p) => an.x >= an.y && an.x >= an.z ? new Vector2(p.z, p.y) / m
                                                       : an.y >= an.z ? new Vector2(p.x, p.z) / m : new Vector2(p.x, p.y) / m;
                            a = CloneVertex(a, Project(v[a]));
                            b = CloneVertex(b, Project(v[b]));
                            d = CloneVertex(d, Project(v[d]));
                            stats.Reuv++;
                        }
                    }
                    subs[s].Add(a); subs[s].Add(b); subs[s].Add(d);
                }
            }

            // Soldar lo idéntico (y de paso quedan afuera los vértices que ya no usa nadie).
            var map = new Dictionary<string, int>();
            var remap = new Dictionary<int, int>();
            var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nt = new List<Vector4>(); var nc = new List<Color>();
            var nu0 = new List<Vector2>(); var nu1 = new List<Vector2>();
            int used = 0;
            foreach (List<int> list in subs)
                for (int k = 0; k < list.Count; k++)
                {
                    int i = list[k];
                    if (!remap.TryGetValue(i, out int j))
                    {
                        used++;
                        string key = $"{v[i].x:F5},{v[i].y:F5},{v[i].z:F5}|{(hasN ? n[i].ToString("F3") : "")}|{(hasUv0 ? $"{uv0[i].x:F5},{uv0[i].y:F5}" : "")}|{(hasUv1 ? $"{uv1[i].x:F5},{uv1[i].y:F5}" : "")}|{(hasC ? c[i].ToString("F3") : "")}|{(hasT ? Mathf.Sign(tg[i].w).ToString() : "")}";
                        if (!map.TryGetValue(key, out j))
                        {
                            j = nv.Count;
                            map[key] = j;
                            nv.Add(v[i]);
                            if (hasN) nn.Add(n[i]);
                            if (hasT) nt.Add(tg[i]);
                            if (hasC) nc.Add(c[i]);
                            if (hasUv0) nu0.Add(uv0[i]);
                            if (hasUv1) nu1.Add(uv1[i]);
                        }
                        remap[i] = j;
                    }
                    list[k] = j;
                }
            stats.Welded = used - nv.Count;

            var mesh = new Mesh { name = source.name };
            mesh.indexFormat = nv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(nv);
            if (hasN) mesh.SetNormals(nn);
            if (hasC) mesh.SetColors(nc);
            if (hasUv0) mesh.SetUVs(0, nu0);
            if (hasUv1) mesh.SetUVs(1, nu1);
            mesh.subMeshCount = subs.Length;
            for (int s = 0; s < subs.Length; s++) mesh.SetTriangles(subs[s], s);
            if (hasT && stats.Reuv == 0) mesh.SetTangents(nt);
            else mesh.RecalculateTangents(); // con textura rehecha, las tangentes se recalculan
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
