using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    // El tren más ancho y más alto (Tomi, 2026-10-16: "el tren se ve muy chico, muy angosto"): el arte del vagón MR-04 se hizo a
    // 3,4 m de ancho y 2,4 m de alto interior; el tren pasa a 4,4 m y 2,9 m (BodyHalfWidth y RoofBottom). El arte se adapta al
    // armar la escena, sin tocar el FBX y sin deformar lo que no debe:
    // - Superficies (carrocería, revestimiento, vidrios, nieve, carámbanos, faldones, quitanieves, rejillas): se estiran a lo
    //   ancho y, del piso para arriba, a lo alto.
    // - Muebles, utilería y chasis (piezas sueltas dentro de una misma malla): cada pieza contra una pared se corre hasta la pared
    //   nueva sin cambiar de tamaño; la que cruza de lado a lado se estira a lo ancho; la del medio queda donde está. La que llega
    //   al techo, también se estira a lo alto.
    // - La sala de máquinas se estira (llega al techo nuevo).
    // - Ruedas, bogies y la cabina (el pupitre con sus palancas) quedan como están: las ruedas siguen sobre la vía.
    // - Las puertas se corren a la pared nueva y se estiran a lo alto (el hueco es más alto); el escape y las luces del techo suben.
    // Las mallas nuevas se guardan en Art/Tren/MR04/Generado (se rehacen cada vez que se arma la escena).
    public static partial class TestScenesBuilder
    {
        private const float ArtOldBodyHalfWidth = 1.7f;   // el ancho para el que se hizo el arte (y el greybox de antes)
        private const float ArtOldInnerHeight = 2.4f;
        private const float ArtInnerHalfWidth = 1.6f;     // la cara de adentro de las paredes del arte

        private static float ArtWiden => BodyHalfWidth / ArtOldBodyHalfWidth;
        private static float ArtRaise => (RoofBottom - FloorTop) / ArtOldInnerHeight;
        /// <summary>Cuánto se corre hacia afuera lo que está contra una pared (la pared de adentro del arte, estirada).</summary>
        private static float ArtSideShift => ArtInnerHalfWidth * (ArtWiden - 1f);

        private enum WidenMode { Keep, Surface, Pieces, Door, Raise }

        private static WidenMode WidenModeFor(string piece)
        {
            if (piece.StartsWith("Carroceria") || piece.StartsWith("Interior") || piece.StartsWith("Vidrios") || piece.StartsWith("Nieve") ||
                piece.StartsWith("Carambanos") || piece.StartsWith("Faldones") || piece.StartsWith("Quitanieves") || piece.StartsWith("Rejillas") ||
                piece.StartsWith("Sala_Maquinas"))
                return WidenMode.Surface;
            if (piece.StartsWith("Muebles") || piece.StartsWith("Utileria") || piece.StartsWith("Chasis") || piece == "Sierra" || piece == "Tapa_Combustible")
                return WidenMode.Pieces;
            if (piece.StartsWith("Puerta_")) return WidenMode.Door;
            if (piece.StartsWith("Escape") || piece.StartsWith("Luz_Techo")) return WidenMode.Raise;
            return WidenMode.Keep; // ruedas, bogies, cabina (pupitre, palancas, agujas), lo que el juego reemplaza
        }

        // La altura nueva de un punto: del piso para arriba se estira; abajo del piso (chasis, ruedas) no cambia.
        private static float RaiseY(float y) => y <= FloorTop ? y : FloorTop + (y - FloorTop) * ArtRaise;

        private static void WidenWagonArt(GameObject art)
        {
            if (Mathf.Approximately(ArtWiden, 1f) && Mathf.Approximately(ArtRaise, 1f)) return;
            foreach (Transform piece in art.transform)
            {
                WidenMode mode = WidenModeFor(piece.name);
                if (mode == WidenMode.Keep) continue;
                Vector3 lp = piece.localPosition;
                if (mode == WidenMode.Raise)
                {
                    piece.localPosition = new Vector3(lp.x, RaiseY(lp.y), lp.z); // el escape y las luces suben con el techo
                    continue;
                }
                // Las puertas: la bisagra (el origen de la pieza) va a la pared nueva y la hoja se estira como la carrocería.
                var before = new Dictionary<MeshFilter, Matrix4x4>();
                foreach (MeshFilter mf in piece.GetComponentsInChildren<MeshFilter>(true))
                    before[mf] = art.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                if (mode == WidenMode.Door) piece.localPosition = new Vector3(lp.x * ArtWiden, RaiseY(lp.y), lp.z);
                foreach (MeshFilter mf in piece.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    Matrix4x4 fromArt = (art.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix).inverse;
                    mf.sharedMesh = WidenMesh(mf.sharedMesh, before[mf], fromArt, mode == WidenMode.Door ? WidenMode.Surface : mode,
                        mf.sharedMesh.name + "_Ancho");
                }
                foreach (Light l in piece.GetComponentsInChildren<Light>(true))
                {
                    Vector3 p = art.transform.InverseTransformPoint(l.transform.position);
                    l.transform.position = art.transform.TransformPoint(new Vector3(p.x * ArtWiden, RaiseY(p.y), p.z));
                }
            }
        }

        // Una copia de la malla con cada punto llevado a su lugar nuevo (en el espacio del arte: "toArt").
        private static Mesh WidenMesh(Mesh source, Matrix4x4 toArt, Matrix4x4 fromArt, WidenMode mode, string name)
        {
            Vector3[] v = source.vertices;
            Vector3[] n = source.normals;
            Vector4[] t = source.tangents;
            var p = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++) p[i] = toArt.MultiplyPoint3x4(v[i]);

            // Para las piezas sueltas: qué hace cada pieza (por grupos de triángulos unidos).
            var shift = new float[v.Length];
            var scaleX = new bool[v.Length];
            var raise = new bool[v.Length];
            if (mode == WidenMode.Pieces)
            {
                foreach (List<int> part in ConnectedParts(source, p))
                {
                    var b = new Bounds(p[part[0]], Vector3.zero);
                    foreach (int i in part) b.Encapsulate(p[i]);
                    bool spans = b.size.x > 1.5f;                                   // cruza de lado a lado: se estira
                    float s = spans || Mathf.Abs(b.center.x) < 0.45f ? 0f : Mathf.Sign(b.center.x) * ArtSideShift;
                    bool tall = b.max.y > FloorTop + ArtOldInnerHeight - 0.2f;     // llega al techo
                    foreach (int i in part) { shift[i] = s; scaleX[i] = spans; raise[i] = tall; }
                }
            }

            var outV = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 q = p[i];
                switch (mode)
                {
                    case WidenMode.Surface: q = new Vector3(q.x * ArtWiden, RaiseY(q.y), q.z); break;
                    case WidenMode.Pieces: q = new Vector3(scaleX[i] ? q.x * ArtWiden : q.x + shift[i], raise[i] ? RaiseY(q.y) : q.y, q.z); break;
                }
                outV[i] = fromArt.MultiplyPoint3x4(q);
            }

            // Normales y tangentes: con lo estirado, las normales se corrigen al revés del estiramiento.
            Vector3 stretch = mode == WidenMode.Surface ? new Vector3(ArtWiden, ArtRaise, 1f) : Vector3.one;
            var outN = new Vector3[n.Length];
            for (int i = 0; i < n.Length; i++)
            {
                Vector3 local = toArt.MultiplyVector(n[i]);
                Vector3 s = mode == WidenMode.Pieces && scaleX[i] ? new Vector3(ArtWiden, raise[i] ? ArtRaise : 1f, 1f)
                          : mode == WidenMode.Pieces && raise[i] ? new Vector3(1f, ArtRaise, 1f) : stretch;
                if (p[i].y <= FloorTop) s.y = 1f;
                outN[i] = fromArt.MultiplyVector(new Vector3(local.x / s.x, local.y / s.y, local.z / s.z)).normalized;
            }
            var outT = new Vector4[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                Vector3 local = toArt.MultiplyVector(t[i]);
                Vector3 s = stretch;
                if (p[i].y <= FloorTop) s.y = 1f;
                Vector3 d = fromArt.MultiplyVector(new Vector3(local.x * s.x, local.y * s.y, local.z * s.z)).normalized;
                outT[i] = new Vector4(d.x, d.y, d.z, t[i].w);
            }

            var mesh = Object.Instantiate(source);
            mesh.name = name;
            mesh.vertices = outV;
            if (outN.Length > 0) mesh.normals = outN;
            if (outT.Length > 0) mesh.tangents = outT;
            mesh.RecalculateBounds();
            return SaveGenerated(mesh);
        }

        // Los grupos de triángulos unidos de una malla (los vértices en el mismo lugar cuentan como uno: las costuras de UV no
        // parten una pieza).
        private static List<List<int>> ConnectedParts(Mesh mesh, Vector3[] points)
        {
            int count = points.Length;
            var parent = new int[count];
            for (int i = 0; i < count; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            void Union(int a, int b) { a = Find(a); b = Find(b); if (a != b) parent[a] = b; }
            var byPlace = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < count; i++)
            {
                var key = Vector3Int.RoundToInt(points[i] * 1000f);
                if (byPlace.TryGetValue(key, out int other)) Union(i, other); else byPlace[key] = i;
            }
            for (int sm = 0; sm < mesh.subMeshCount; sm++)
            {
                int[] tris = mesh.GetTriangles(sm);
                for (int i = 0; i < tris.Length; i += 3) { Union(tris[i], tris[i + 1]); Union(tris[i], tris[i + 2]); }
            }
            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < count; i++)
            {
                int r = Find(i);
                if (!groups.TryGetValue(r, out List<int> list)) groups[r] = list = new List<int>();
                list.Add(i);
            }
            return new List<List<int>>(groups.Values);
        }

        private static Mesh SaveGenerated(Mesh mesh)
        {
            if (!AssetDatabase.IsValidFolder(GeneratedArtFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art/Tren/MR04", "Generado");
            string path = $"{GeneratedArtFolder}/{mesh.name}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            string keep = mesh.name;
            EditorUtility.CopySerialized(mesh, existing);
            existing.name = keep;
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
