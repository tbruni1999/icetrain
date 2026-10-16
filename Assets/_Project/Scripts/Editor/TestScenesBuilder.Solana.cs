using System.Collections.Generic;
using SurvivalEnTren.Player;
using SurvivalEnTren.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Blockout del shopping Solana (documento "Solana y POIs grandes", Parte A, sección 12, pasos 1 a 4; pedido de Tomi del
    /// 2026-10-16: "vos seguí y crea lo que sea creable"). Es una escena de prueba aparte (no toca el juego): volúmenes simples a
    /// escala real para caminar y medir, con las salas de los planos, sus puertas, los núcleos, la azotea con la duna y el
    /// derrumbe, la torre de estacionamiento, el cartel SOLANA y la vía al sur. Sin llaves, fauna ni loot (pasos 5 y 6).
    /// Coordenadas: el origen es la esquina suroeste del edificio en la planta baja; x al este, z al norte, y arriba (metros).
    /// </summary>
    public static partial class TestScenesBuilder
    {
        private const string SolanaScenePath = "Assets/_Project/Scenes/Pruebas/Solana_Blockout.unity";
        private const float SW = 128f, SD = 70f;              // huella del edificio
        private const float SnowWest = 10f, SnowEast = 5f;     // nieve: +10 al oeste, sur y norte; +5 al este
        private const float WallT = 0.25f, ExtT = 0.4f, SlabT = 0.3f;
        private const float DoorH = 2.6f;

        private enum Loot { Materiales, Medico, Electronica, Comida, Agua, Otros, Nucleo }

        private readonly struct SRoom
        {
            public readonly string Id, Name; public readonly Loot Loot; public readonly float X0, Z0, X1, Z1;
            public SRoom(string id, string name, Loot loot, float x0, float z0, float x1, float z1)
            { Id = id; Name = name; Loot = loot; X0 = x0; Z0 = z0; X1 = x1; Z1 = z1; }
        }

        // Una abertura en una pared: alongX = la pared corre a lo largo de x (está en z = line); center es la coordenada a lo largo.
        private readonly struct SDoor
        {
            public readonly bool AlongX; public readonly float Line, Center, Width;
            public SDoor(bool alongX, float line, float center, float width) { AlongX = alongX; Line = line; Center = center; Width = width; }
        }

        private sealed class SLevel
        {
            public string Id, Name; public float Floor, Ceiling;
            public List<SRoom> Rooms = new List<SRoom>();
            public List<SDoor> Doors = new List<SDoor>();
        }

        [MenuItem("Survival en tren/Solana/Construir blockout")]
        public static void BuildSolanaBlockout()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Solana] Salir del modo Play antes de construir."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Palette p = CreatePalette();

            // Luz de blockout: pareja, para leer los volúmenes (la luz real por nivel es el paso 7).
            var sun = new GameObject("Luz_Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0025f;
            RenderSettings.fogColor = new Color(0.8f, 0.84f, 0.89f);

            var root = new GameObject("Solana_Blockout").transform;
            var mats = new SolanaMats();
            List<SLevel> levels = SolanaLevels();
            foreach (SLevel level in levels) BuildSolanaLevel(level, root, mats);
            BuildSolanaSlabs(root, mats);
            BuildSolanaExterior(root, mats);
            BuildSolanaCores(levels, root, mats);
            BuildSolanaRoof(root, mats);
            BuildSolanaSnow(root, p);
            BuildSolanaTower(root, mats);
            BuildSolanaSign(root, mats);
            BuildSolanaTrack(root, mats, p);
            BuildSolanaRoutes(levels, root);

            // Los carteles respetan lo que tienen adelante (el material de texto de Unity se ve a través de todo).
            const string textMatPath = "Assets/_Project/Materials/Texto_Con_Profundidad.mat";
            var textMat = AssetDatabase.LoadAssetAtPath<Material>(textMatPath);
            if (textMat == null)
            {
                EnsureFolder("Assets/_Project", "Materials");
                textMat = new Material(Shader.Find("SurvivalEnTren/TextoConProfundidad"));
                AssetDatabase.CreateAsset(textMat, textMatPath);
            }
            SetReference(root.gameObject.AddComponent<DepthTextMaterial>(), "material", textMat);

            // El jugador al pie de la duna oeste, mirando al este (E1: subir la duna contra el parapeto oeste).
            BuildPlayer(new Vector3(-45f, SnowWest + 0.05f, 35f), Quaternion.Euler(0f, 90f, 0f));

            EnsureFolder("Assets/_Project/Scenes", "Pruebas");
            EditorSceneManager.SaveScene(scene, SolanaScenePath);
            Debug.Log($"[Solana] Blockout construido en {SolanaScenePath}: {levels.Count} niveles, {CountRooms(levels)} salas.");
        }

        private static int CountRooms(List<SLevel> levels) { int n = 0; foreach (var l in levels) n += l.Rooms.Count; return n; }

        private sealed class SolanaMats
        {
            public readonly Material Slab = Mat("Solana_Losa", new Color(0.62f, 0.62f, 0.6f));
            public readonly Material Wall = Mat("Solana_Pared", new Color(0.78f, 0.77f, 0.74f));
            public readonly Material Exterior = Mat("Solana_Fachada", new Color(0.46f, 0.47f, 0.5f));
            public readonly Material Core = Mat("Solana_Nucleo", new Color(0.55f, 0.53f, 0.5f));
            public readonly Material Ramp = Mat("Solana_Rampa", new Color(0.5f, 0.5f, 0.52f));
            public readonly Material Glass = Mat("Solana_Vidrio", new Color(0.62f, 0.78f, 0.86f));
            public readonly Material Sealed = Mat("Solana_Sellado", new Color(0.35f, 0.42f, 0.55f));
            public readonly Material RedBlanca = Mat("Solana_RedBlanca", new Color(0.92f, 0.45f, 0.12f));
            public readonly Material Rust = Mat("Solana_Oxido", new Color(0.42f, 0.27f, 0.18f));
            public readonly Material Yellow = Mat("Solana_Amarillo", new Color(0.96f, 0.78f, 0.22f));
            public readonly Material Turquoise = Mat("Solana_Turquesa", new Color(0.25f, 0.72f, 0.7f));
            public readonly Material Coral = Mat("Solana_Coral", new Color(0.94f, 0.47f, 0.4f));
            public readonly Material Pink = Mat("Solana_Rosa", new Color(0.93f, 0.55f, 0.7f));
            public readonly Material Green = Mat("Solana_Verde", new Color(0.45f, 0.75f, 0.42f));
            public readonly Material Orange = Mat("Solana_Naranja", new Color(0.96f, 0.6f, 0.25f));
            public readonly Material Blue = Mat("Solana_Azul", new Color(0.4f, 0.6f, 0.85f));
            public readonly Material Lilac = Mat("Solana_Lila", new Color(0.65f, 0.55f, 0.85f));
            public Material Loot(Loot l)
            {
                switch (l)
                {
                    case TestScenesBuilder.Loot.Materiales: return Mat("Solana_Zona_Materiales", new Color(0.96f, 0.79f, 0.48f));
                    case TestScenesBuilder.Loot.Medico: return Mat("Solana_Zona_Medico", new Color(0.56f, 0.83f, 0.75f));
                    case TestScenesBuilder.Loot.Electronica: return Mat("Solana_Zona_Electronica", new Color(0.74f, 0.71f, 0.92f));
                    case TestScenesBuilder.Loot.Comida: return Mat("Solana_Zona_Comida", new Color(0.96f, 0.67f, 0.55f));
                    case TestScenesBuilder.Loot.Agua: return Mat("Solana_Zona_Agua", new Color(0.66f, 0.8f, 0.91f));
                    case TestScenesBuilder.Loot.Nucleo: return Mat("Solana_Zona_Nucleo", new Color(0.74f, 0.72f, 0.68f));
                    default: return Mat("Solana_Zona_Otros", new Color(0.85f, 0.84f, 0.81f));
                }
            }
        }

        // ------------------------------------------------------------------ Datos de los planos (sección 3 del documento)

        private static List<SLevel> SolanaLevels()
        {
            var s1 = new SLevel { Id = "S1", Name = "Subsuelo", Floor = -4.5f, Ceiling = -SlabT };
            s1.Rooms.AddRange(new[]
            {
                new SRoom("S1-02", "Cisterna y bombas", Loot.Agua, 0f, 54f, 26f, 70f),
                new SRoom("S1-03", "Rampa vehicular (sellada)", Loot.Otros, 108f, 48f, 128f, 70f),
                new SRoom("S1-04", "Muelle de carga", Loot.Materiales, 9f, 0f, 30f, 24f),
                new SRoom("S1-05", "Depósito general", Loot.Materiales, 30f, 0f, 56f, 24f),
                new SRoom("S1-06", "Sala del generador", Loot.Electronica, 56f, 0f, 78f, 24f),
                new SRoom("S1-07", "Calderas y tanques", Loot.Materiales, 90f, 0f, 128f, 24f),
                new SRoom("S1-NE", "Escalera de emergencia", Loot.Nucleo, 78f, 0f, 90f, 16f),
                new SRoom("S1-NM", "Montacargas", Loot.Nucleo, 0f, 16f, 9f, 24f),
                new SRoom("S1-ML", "Muelle (lado del montacargas)", Loot.Materiales, 0f, 0f, 9f, 16f),
            });
            s1.Doors.AddRange(new[]
            {
                new SDoor(true, 54f, 13f, 2.5f),   // cisterna
                new SDoor(true, 24f, 20f, 3f),     // muelle
                new SDoor(true, 24f, 43f, 2.5f),   // L8 depósito
                new SDoor(true, 24f, 67f, 2.5f),   // L5 generador
                new SDoor(true, 16f, 84f, 2.5f),   // escalera de emergencia → vestíbulo del núcleo
                new SDoor(true, 24f, 109f, 2.5f),  // L6 calderas
                new SDoor(true, 24f, 4.5f, 2f),    // L9 montacargas
                new SDoor(false, 9f, 8f, 5f),      // el muelle sigue al lado del montacargas
            });

            var pb = new SLevel { Id = "PB", Name = "Planta baja", Floor = 0f, Ceiling = 6f - SlabT };
            pb.Rooms.AddRange(new[]
            {
                new SRoom("PB-03", "Farmacia", Loot.Medico, 0f, 54f, 30f, 70f),
                new SRoom("PB-04", "Celulares y accesorios", Loot.Electronica, 30f, 54f, 64f, 70f),
                new SRoom("PB-05", "Café Solana", Loot.Comida, 64f, 54f, 86f, 70f),
                new SRoom("PB-06", "Baños", Loot.Otros, 86f, 54f, 100f, 70f),
                new SRoom("PB-07", "Perfumería", Loot.Otros, 100f, 54f, 128f, 70f),
                new SRoom("PB-08", "Supermercado", Loot.Comida, 0f, 16f, 38f, 54f),
                new SRoom("PB-08C", "Cámara frigorífica", Loot.Comida, 0f, 40f, 14f, 54f),
                new SRoom("PB-09", "Ferretería", Loot.Materiales, 90f, 16f, 128f, 54f),
                new SRoom("PB-09B", "Bodega de la ferretería", Loot.Materiales, 114f, 40f, 128f, 54f),
                new SRoom("PB-10", "Calzado", Loot.Otros, 0f, 0f, 30f, 16f),
                new SRoom("PB-11", "Kiosco y cajero", Loot.Otros, 30f, 0f, 50f, 16f),
                new SRoom("PB-01", "Vestíbulo (entrada principal sepultada)", Loot.Otros, 50f, 0f, 78f, 16f),
                new SRoom("PB-NE", "Escalera de emergencia", Loot.Nucleo, 78f, 0f, 90f, 16f),
                new SRoom("PB-12", "Bazar y regalos", Loot.Otros, 90f, 0f, 128f, 16f),
                new SRoom("PB-NM", "Montacargas", Loot.Nucleo, 0f, 16f, 9f, 24f),
            });
            pb.Doors.AddRange(new[]
            {
                new SDoor(true, 54f, 15f, 3f),     // L3 farmacia → supermercado
                new SDoor(true, 54f, 51f, 3f), new SDoor(true, 54f, 75f, 3f), new SDoor(true, 54f, 88f, 2.5f),
                new SDoor(true, 54f, 105f, 3f),    // perfumería → ferretería
                new SDoor(false, 38f, 35f, 6f),    // supermercado ↔ atrio (entrada libre)
                new SDoor(false, 14f, 47f, 2.5f),  // cámara frigorífica (puerta manual)
                new SDoor(false, 90f, 35f, 6f),    // ferretería ↔ atrio
                new SDoor(true, 40f, 116f, 2.5f),  // L11 bodega
                new SDoor(true, 16f, 20f, 3f),     // calzado → supermercado
                new SDoor(true, 16f, 44f, 3f), new SDoor(true, 16f, 64f, 8f), new SDoor(true, 16f, 84f, 2.5f),
                new SDoor(true, 16f, 100f, 3f),    // bazar → ferretería
                new SDoor(true, 24f, 4.5f, 2f),    // L9
            });

            var p1 = new SLevel { Id = "P1", Name = "Piso 1", Floor = 6f, Ceiling = 11f - SlabT };
            p1.Rooms.AddRange(new[]
            {
                new SRoom("P1-01", "Juguetería", Loot.Otros, 0f, 54f, 30f, 70f),
                new SRoom("P1-02", "Librería y papelería", Loot.Otros, 30f, 54f, 64f, 70f),
                new SRoom("P1-03", "Música y regalos", Loot.Electronica, 64f, 54f, 86f, 70f),
                new SRoom("P1-04", "Baños", Loot.Otros, 86f, 54f, 100f, 70f),
                new SRoom("P1-05", "Óptica", Loot.Otros, 100f, 54f, 128f, 70f),
                new SRoom("P1-06", "Solana Hogar y Moda", Loot.Materiales, 0f, 16f, 38f, 54f),
                new SRoom("P1-07", "Electro Solana", Loot.Electronica, 90f, 16f, 128f, 54f),
                new SRoom("P1-08", "Administración y Seguridad", Loot.Otros, 0f, 0f, 30f, 16f),
                new SRoom("P1-09", "Montaña Sur Outdoor", Loot.Materiales, 30f, 0f, 78f, 16f),
                new SRoom("P1-NE", "Escalera de emergencia", Loot.Nucleo, 78f, 0f, 90f, 16f),
                new SRoom("P1-10", "Enfermería del Punto de Abrigo", Loot.Medico, 90f, 0f, 128f, 16f),
                new SRoom("P1-NM", "Montacargas", Loot.Nucleo, 0f, 16f, 9f, 24f),
            });
            p1.Doors.AddRange(new[]
            {
                new SDoor(true, 54f, 15f, 3f), new SDoor(true, 54f, 51f, 3f), new SDoor(true, 54f, 75f, 3f), new SDoor(true, 54f, 88f, 2.5f),
                new SDoor(true, 54f, 110f, 3f),    // óptica → electro
                new SDoor(false, 38f, 35f, 6f),    // hogar ↔ atrio
                new SDoor(false, 90f, 35f, 6f),    // electro ↔ atrio (reja)
                new SDoor(true, 16f, 15f, 2.5f),   // L2 administración (desde hogar)
                new SDoor(true, 16f, 54f, 3f),     // L12 montaña sur
                new SDoor(true, 16f, 84f, 2.5f),   // escalera
                new SDoor(true, 16f, 100f, 3f),    // enfermería → electro
                new SDoor(true, 24f, 4.5f, 2f),    // L9
            });

            var p2 = new SLevel { Id = "P2", Name = "Piso 2", Floor = 11f, Ceiling = 17f - SlabT };
            p2.Rooms.AddRange(new[]
            {
                new SRoom("P2-01", "Arcade Solana Fun", Loot.Otros, 0f, 54f, 30f, 70f),
                new SRoom("P2-02", "Patio de comidas", Loot.Comida, 30f, 54f, 86f, 70f),
                new SRoom("P2-03", "Cine: hall y boletería", Loot.Otros, 86f, 54f, 128f, 70f),
                new SRoom("P2-06", "Gimnasio Solana Fit (colapsado)", Loot.Materiales, 0f, 16f, 38f, 54f),
                new SRoom("P2-04", "Cine Sala 1 (dormitorio del refugio)", Loot.Otros, 90f, 35f, 128f, 54f),
                new SRoom("P2-05", "Cine Sala 2 (proyección)", Loot.Otros, 90f, 16f, 128f, 35f),
                new SRoom("P2-NE", "Escalera de emergencia", Loot.Nucleo, 78f, 0f, 90f, 16f),
                new SRoom("P2-NM", "Montacargas", Loot.Nucleo, 0f, 16f, 9f, 24f),
            });
            p2.Doors.AddRange(new[]
            {
                new SDoor(true, 54f, 15f, 3f),     // arcade ↔ gimnasio
                new SDoor(true, 54f, 62f, 10f),    // patio de comidas ↔ balcón
                new SDoor(true, 54f, 88f, 2.5f),   // hall del cine
                new SDoor(false, 38f, 35f, 6f),    // gimnasio ↔ balcón (la entrada estándar)
                new SDoor(false, 90f, 45f, 3f),    // sala 1
                new SDoor(false, 90f, 25.6f, 2.5f),// L13 sala 2
                new SDoor(true, 24f, 4.5f, 2f),    // L9
            });
            return new List<SLevel> { s1, pb, p1, p2 };
        }

        // ------------------------------------------------------------------ Salas, paredes y carteles de cada nivel

        private static void BuildSolanaLevel(SLevel level, Transform root, SolanaMats m)
        {
            var group = Group($"Nivel_{level.Id}", root);
            var floorTiles = Group("Pisos_Por_Sala", group);
            var labels = Group("Carteles_Salas", group);
            var markers = GameObject.Find("Salas_Marcadores")?.transform ?? Group("Salas_Marcadores", root);
            for (int ri = 0; ri < level.Rooms.Count; ri++)
            {
                SRoom r = level.Rooms[ri];
                float lift = 0.03f + ri * 0.004f; // las salas dentro de otra (cámara fría, bodega, montacargas) quedan apenas más arriba: sin parpadeo
                BoxMinMax($"Piso_{r.Id}", floorTiles, new Vector3(r.X0 + 0.15f, level.Floor, r.Z0 + 0.15f), new Vector3(r.X1 - 0.15f, level.Floor + lift, r.Z1 - 0.15f), m.Loot(r.Loot), false);
                var center = new Vector3((r.X0 + r.X1) * 0.5f, level.Floor, (r.Z0 + r.Z1) * 0.5f);
                if (!r.Id.EndsWith("NM")) // el pozo del montacargas: sin cartel adentro
                    for (int side = 0; side < 2; side++)
                        SolanaLabel(labels, $"{r.Id}\n{r.Name}", center + Vector3.up * 2.4f, side == 0 ? 180f : 0f, 0.09f, Color.black);
                var marker = new GameObject($"Sala_{r.Id}").transform;
                marker.SetParent(markers, false);
                marker.position = center;
            }

            // Paredes interiores: la unión de los bordes de todas las salas por línea, menos las puertas (cada borde compartido se
            // hace una sola vez). Los bordes del edificio son fachada (otra función).
            var walls = Group("Paredes", group);
            var lines = new Dictionary<(bool, float), List<Vector2>>();
            void Add(bool alongX, float line, float a, float b)
            {
                if (alongX && (Mathf.Approximately(line, 0f) || Mathf.Approximately(line, SD))) return;
                if (!alongX && (Mathf.Approximately(line, 0f) || Mathf.Approximately(line, SW))) return;
                if (level.Id == "P2" && alongX && Mathf.Approximately(line, 16f)) return; // fachada del bloque del Piso 2
                var key = (alongX, line);
                if (!lines.TryGetValue(key, out var list)) lines[key] = list = new List<Vector2>();
                list.Add(new Vector2(Mathf.Min(a, b), Mathf.Max(a, b)));
            }
            foreach (SRoom r in level.Rooms)
            {
                Add(true, r.Z0, r.X0, r.X1); Add(true, r.Z1, r.X0, r.X1);
                Add(false, r.X0, r.Z0, r.Z1); Add(false, r.X1, r.Z0, r.Z1);
            }
            foreach (var kv in lines)
            {
                var gaps = new List<SDoor>();
                foreach (SDoor d in level.Doors) if (d.AlongX == kv.Key.Item1 && Mathf.Approximately(d.Line, kv.Key.Item2)) gaps.Add(d);
                foreach (Vector2 seg in Union(kv.Value))
                    WallLine(walls, kv.Key.Item1, kv.Key.Item2, seg.x, seg.y, level.Floor, level.Ceiling, WallT, gaps, m.Wall);
            }
            for (int side = 0; side < 2; side++)
                SolanaLabel(group, $"{level.Id} · {level.Name.ToUpperInvariant()}", new Vector3(64f, level.Floor + 4f, 19f), side * 180f, 0.25f, new Color(0.15f, 0.15f, 0.15f));
        }

        private static List<Vector2> Union(List<Vector2> segments)
        {
            segments.Sort((a, b) => a.x.CompareTo(b.x));
            var result = new List<Vector2>();
            foreach (Vector2 s in segments)
            {
                if (result.Count > 0 && s.x <= result[result.Count - 1].y + 0.01f)
                    result[result.Count - 1] = new Vector2(result[result.Count - 1].x, Mathf.Max(result[result.Count - 1].y, s.y));
                else result.Add(s);
            }
            return result;
        }

        // Una pared de "from" a "to" sobre una línea, del piso al techo, con aberturas (y un dintel arriba de cada una).
        private static void WallLine(Transform parent, bool alongX, float line, float from, float to, float floor, float top, float thickness,
            List<SDoor> gaps, Material material)
        {
            var cuts = new List<Vector2>();
            foreach (SDoor g in gaps)
                if (g.Center > from - 0.01f && g.Center < to + 0.01f) cuts.Add(new Vector2(g.Center - g.Width * 0.5f, g.Center + g.Width * 0.5f));
            cuts.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = from;
            void Piece(float a, float b, float y0, float y1)
            {
                if (b - a < 0.05f || y1 - y0 < 0.05f) return;
                Vector3 min = alongX ? new Vector3(a, y0, line - thickness * 0.5f) : new Vector3(line - thickness * 0.5f, y0, a);
                Vector3 max = alongX ? new Vector3(b, y1, line + thickness * 0.5f) : new Vector3(line + thickness * 0.5f, y1, b);
                BoxMinMax("Pared", parent, min, max, material);
            }
            foreach (Vector2 c in cuts)
            {
                Piece(cursor, Mathf.Max(cursor, c.x), floor, top);
                Piece(Mathf.Max(from, c.x), Mathf.Min(to, c.y), floor + DoorH, top); // dintel
                cursor = Mathf.Max(cursor, c.y);
            }
            Piece(cursor, to, floor, top);
        }

        private static void SolanaLabel(Transform parent, string text, Vector3 position, float yaw, float size, Color color)
        {
            var go = new GameObject("Cartel");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        // ------------------------------------------------------------------ Losas con sus huecos

        private static void BuildSolanaSlabs(Transform root, SolanaMats m)
        {
            var group = Group("Losas", root);
            var mont = new Rect(0f, 16f, 9f, 8f);
            var stairs = new Rect(84f, 0f, 6f, 13f);              // el carril este de la escalera de emergencia
            var atrium = new Rect(44f, 22f, 40f, 26f);
            var all = new Rect(0f, 0f, SW, SD);
            Slab(group, "Losa_S1", all, -4.5f, new List<Rect>(), m.Slab);
            Slab(group, "Losa_PB", all, 0f, new List<Rect> { mont, stairs }, m.Slab);
            Slab(group, "Losa_P1", all, 6f, new List<Rect> { mont, stairs, atrium }, m.Slab);
            Slab(group, "Losa_P2", all, 11f, new List<Rect> { mont, stairs, atrium }, m.Slab);
            // La azotea (+17) cubre solo el bloque del Piso 2: el ala oeste colapsada (boca del derrumbe) y la claraboya del atrio,
            // que se abre hasta el borde norte del balcón para la bajada de la E2.
            Slab(group, "Losa_Azotea", new Rect(0f, 16f, SW, 54f), 17f, new List<Rect> { new Rect(1f, 19f, 33f, 32f), new Rect(44f, 22f, 40f, 30f) }, m.Slab);
            // La fuente helada del atrio (hito central; sin colisión de detalle).
            BoxMinMax("Fuente_Helada", group, new Vector3(60f, 0f, 31f), new Vector3(68f, 0.6f, 39f), m.Glass);
        }

        // Una losa con su cara de arriba en "top", partida en rectángulos alrededor de los huecos.
        private static void Slab(Transform parent, string name, Rect area, float top, List<Rect> holes, Material material)
        {
            var xs = new SortedSet<float> { area.xMin, area.xMax };
            var zs = new SortedSet<float> { area.yMin, area.yMax };
            foreach (Rect h in holes)
            {
                xs.Add(Mathf.Clamp(h.xMin, area.xMin, area.xMax)); xs.Add(Mathf.Clamp(h.xMax, area.xMin, area.xMax));
                zs.Add(Mathf.Clamp(h.yMin, area.yMin, area.yMax)); zs.Add(Mathf.Clamp(h.yMax, area.yMin, area.yMax));
            }
            var xl = new List<float>(xs); var zl = new List<float>(zs);
            var slab = Group(name, parent);
            for (int j = 0; j < zl.Count - 1; j++)
            {
                float runStart = float.NaN;
                for (int i = 0; i <= xl.Count - 1; i++)
                {
                    bool solid = false;
                    if (i < xl.Count - 1)
                    {
                        var c = new Vector2((xl[i] + xl[i + 1]) * 0.5f, (zl[j] + zl[j + 1]) * 0.5f);
                        solid = true;
                        foreach (Rect h in holes) if (h.Contains(c)) { solid = false; break; }
                    }
                    if (solid && float.IsNaN(runStart)) runStart = xl[i];
                    if (!solid && !float.IsNaN(runStart))
                    {
                        BoxMinMax("Tramo", slab, new Vector3(runStart, top - SlabT, zl[j]), new Vector3(xl[i], top, zl[j + 1]), material);
                        runStart = float.NaN;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ Fachadas, terraza y salidas selladas

        private static void BuildSolanaExterior(Transform root, SolanaMats m)
        {
            var group = Group("Fachadas", root);
            var none = new List<SDoor>();
            foreach (var (floor, top) in new[] { (-4.5f, -SlabT), (0f, 6f - SlabT), (6f, 11f - SlabT) })
            {
                WallLine(group, true, 0f, 0f, SW, floor, top, ExtT, none, m.Exterior);
                WallLine(group, true, SD, 0f, SW, floor, top, ExtT, none, m.Exterior);
                WallLine(group, false, 0f, 0f, SD, floor, top, ExtT, none, m.Exterior);
                WallLine(group, false, SW, 0f, SD, floor, top, ExtT, none, m.Exterior);
            }
            // El bloque del Piso 2 (z 16 a 70): la puerta a la terraza, el cortafuego de la escalera (L1) y la pasarela de la torre.
            WallLine(group, true, 16f, 0f, SW, 11f, 17f - SlabT, ExtT, new List<SDoor> { new SDoor(true, 16f, 60f, 3f), new SDoor(true, 16f, 84f, 2.5f) }, m.Exterior);
            WallLine(group, true, SD, 0f, SW, 11f, 17f - SlabT, ExtT, none, m.Exterior);
            WallLine(group, false, 0f, 16f, SD, 11f, 17f - SlabT, ExtT, none, m.Exterior);
            WallLine(group, false, SW, 16f, SD, 11f, 17f - SlabT, ExtT, new List<SDoor> { new SDoor(false, SW, 58f, 3.5f) }, m.Exterior);
            WallLine(group, true, 0f, 78f, 90f, 11f, 17f, ExtT, none, m.Exterior); // la escalera, del lado de la terraza

            // Terraza sur (+11, techo del Piso 1): parapeto de 1,2 m.
            WallLine(group, true, 0f, 0f, 78f, 11f, 12.2f, ExtT, none, m.Exterior);
            WallLine(group, true, 0f, 90f, SW, 11f, 12.2f, ExtT, none, m.Exterior);
            WallLine(group, false, 0f, 0f, 16f, 11f, 12.2f, ExtT, none, m.Exterior);
            WallLine(group, false, SW, 0f, 16f, 11f, 12.2f, ExtT, none, m.Exterior);

            // E3: la compuerta térmica de la brigada (vestíbulo, sur) y el túnel sellado; E4: la persiana del muelle (subsuelo, sur).
            BoxMinMax("E3_Compuerta_Brigada_Sellada", group, new Vector3(61f, 0f, -0.3f), new Vector3(67f, 3.2f, 0.25f), m.RedBlanca);
            BoxMinMax("E3_Tunel_Brigada_Sellado", group, new Vector3(58f, -0.3f, -11f), new Vector3(70f, 3.5f, -0.4f), m.Sealed, false);
            BoxMinMax("E4_Persiana_Muelle_Sellada", group, new Vector3(10f, -4.5f, -0.3f), new Vector3(20f, -1f, 0.25f), m.Sealed);
            SolanaLabel(group, "SOUTH TUNNEL · SUPPORTS CHECKED", new Vector3(64f, 3.8f, 0.6f), 180f, 0.07f, new Color(0.9f, 0.45f, 0.1f));
        }

        // ------------------------------------------------------------------ Núcleos: escalera de emergencia, montacargas, escaleras mecánicas

        private static void BuildSolanaCores(List<SLevel> levels, Transform root, SolanaMats m)
        {
            var group = Group("Nucleos", root);
            var stops = new List<float> { -4.5f, 0f, 6f, 11f, 17f };
            for (int i = 0; i < stops.Count - 1; i++)
            {
                float yl = stops[i], yu = stops[i + 1], mid = (yl + yu) * 0.5f;
                var flight = Group($"Escalera_{i}_{yl:0.#}_a_{yu:0.#}", group);
                Ramp("Tramo_1", flight, new Vector3(81f, yl, 13f), new Vector3(81f, mid, 3f), 5.2f, m.Ramp);
                BoxMinMax("Descanso", flight, new Vector3(78.4f, mid - SlabT, 0.4f), new Vector3(89.6f, mid, 3f), m.Ramp);
                Ramp("Tramo_2", flight, new Vector3(87f, mid, 3f), new Vector3(87f, yu, 13f), 5.2f, m.Ramp);
            }
            // Arriba (+17): el descanso de llegada, la caseta de salida y su puerta a la azotea.
            BoxMinMax("Escalera_Llegada_Azotea", group, new Vector3(78f, 17f - SlabT, 13f), new Vector3(90f, 17f, 16f), m.Ramp);
            var none = new List<SDoor>();
            WallLine(group, false, 78f, 0f, 16f, 17f, 20f, ExtT, none, m.Core); // abajo de +17 las paredes son las de cada nivel
            WallLine(group, false, 90f, 0f, 16f, 17f, 20f, ExtT, none, m.Core);
            WallLine(group, true, 0f, 78f, 90f, 17f, 20f, ExtT, none, m.Core);
            WallLine(group, true, 16f, 78f, 90f, 17f, 20f, ExtT, new List<SDoor> { new SDoor(true, 16f, 84f, 2.5f) }, m.Core);
            BoxMinMax("Caseta_Techo", group, new Vector3(77.8f, 20f, -0.2f), new Vector3(90.2f, 20.3f, 16.2f), m.Core);
            SolanaLabel(group, "EXIT", new Vector3(84f, 19.5f, 16.4f), 180f, 0.12f, new Color(0.1f, 0.6f, 0.2f));

            // El montacargas: el pozo atraviesa del subsuelo al Piso 2 (la cabina está trabada; es un atajo vertical: paso 5).
            BoxMinMax("Montacargas_Cabina_Trabada", group, new Vector3(1f, -4.5f, 17f), new Vector3(8f, -1.8f, 23f), m.Core);

            // Escaleras mecánicas congeladas en el atrio: PB → P1 (carril sur) y P1 → P2 (carril norte), con sus descansos.
            Ramp("Escalera_Mecanica_PB_P1", group, new Vector3(69f, 0f, 25f), new Vector3(83f, 6f, 25f), 4f, m.Ramp);
            Ramp("Escalera_Mecanica_P1_P2", group, new Vector3(83f, 6f, 29f), new Vector3(69f, 11f, 29f), 4f, m.Ramp);
            BoxMinMax("Escalera_Mecanica_Descanso_P1", group, new Vector3(83f, 6f - SlabT, 23f), new Vector3(84.6f, 6f, 31f), m.Ramp);
            BoxMinMax("Escalera_Mecanica_Descanso_P2", group, new Vector3(62f, 11f - SlabT, 22f), new Vector3(69f, 11f, 31f), m.Ramp);
        }

        // Una rampa de "from" a "to" (su cara de arriba pasa por los dos puntos), de cierto ancho.
        private static GameObject Ramp(string name, Transform parent, Vector3 from, Vector3 to, float width, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            Vector3 dir = to - from;
            Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            go.transform.rotation = rot;
            go.transform.position = (from + to) * 0.5f - rot * Vector3.up * (SlabT * 0.5f);
            go.transform.localScale = new Vector3(width, SlabT, dir.magnitude + 0.3f);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        // ------------------------------------------------------------------ Azotea: parapetos, derrumbe, claraboya, máquinas

        private static void BuildSolanaRoof(Transform root, SolanaMats m)
        {
            var group = Group("Azotea", root);
            var none = new List<SDoor>();
            WallLine(group, true, 16f, 0f, 78f, 17f, 18.2f, ExtT, none, m.Exterior);
            WallLine(group, true, 16f, 90f, SW, 17f, 18.2f, ExtT, none, m.Exterior);
            WallLine(group, true, SD, 0f, SW, 17f, 18.2f, ExtT, none, m.Exterior);
            WallLine(group, false, 0f, 16f, SD, 17f, 18.2f, ExtT, new List<SDoor> { new SDoor(false, 0f, 35f, 30f) }, m.Exterior); // la duna sube por acá
            WallLine(group, false, SW, 16f, SD, 17f, 18.2f, ExtT, none, m.Exterior);
            // A-01 sala de máquinas, A-02 caseta de la antena, A-05 tanque de agua.
            BoxMinMax("A-01_Sala_de_Maquinas", group, new Vector3(0.5f, 17f, 54.5f), new Vector3(29.5f, 21f, 69.5f), m.Core);
            BoxMinMax("A-02_Caseta_Antena", group, new Vector3(92f, 17f, 55f), new Vector3(127.5f, 20f, 69.5f), m.Lilac);
            BoxMinMax("A-02_Antena", group, new Vector3(120f, 20f, 64f), new Vector3(120.6f, 34f, 64.6f), m.Rust, false);
            BoxMinMax("A-05_Tanque_de_Agua", group, new Vector3(108f, 17f, 40f), new Vector3(126f, 25f, 52f), m.Blue);
            SolanaLabel(group, "A-01 Sala de máquinas", new Vector3(15f, 21.8f, 54.2f), 0f, 0.12f, Color.black);
            // E2: la pasarela técnica sobre la claraboya, unos paneles de vidrio sanos y la bajada por el panel roto (con nieve)
            // hasta el borde norte del balcón del Piso 2.
            BoxMinMax("E2_Pasarela_Tecnica", group, new Vector3(44f, 17f - 0.2f, 34.4f), new Vector3(84f, 17f, 35.6f), m.Ramp);
            for (int i = 0; i < 6; i++)
                if (i != 2 && i != 4)
                    BoxMinMax($"Claraboya_Panel_{i}", group, new Vector3(44f + i * 6.6f, 17.05f, 22.2f), new Vector3(50.4f + i * 6.6f, 17.15f, 34.2f), m.Glass);
            Ramp("E2_Panel_Roto_Bajada", group, new Vector3(45f, 17f, 50f), new Vector3(64f, 11f, 50f), 3.6f, m.Glass);
            SolanaLabel(group, "E2 · CLARABOYA", new Vector3(64f, 18.5f, 21.5f), 180f, 0.15f, new Color(0.1f, 0.4f, 0.15f));
            // El ala oeste colapsada: vigas rotas sobre la boca del derrumbe.
            for (int i = 0; i < 5; i++)
            {
                var beam = BoxMinMax($"Viga_Rota_{i}", group, new Vector3(-0.2f, 16.6f, 20f + i * 7f), new Vector3(0.2f, 17f, 21f + i * 7f), m.Rust, false);
                beam.transform.localScale = new Vector3(14f, 0.4f, 0.6f);
                beam.transform.position = new Vector3(8f + (i % 2) * 6f, 15.5f + (i % 3) * 0.6f, 21f + i * 7f);
                beam.transform.rotation = Quaternion.Euler(0f, 12f * (i - 2), 14f + i * 5f);
            }
            SolanaLabel(group, "E1 · BOCA DEL DERRUMBE", new Vector3(-6f, 19.5f, 35f), 90f, 0.15f, new Color(0.1f, 0.4f, 0.15f));
        }

        // ------------------------------------------------------------------ Nieve: +10 al oeste, sur y norte, +5 al este; la duna y el cono

        private static void BuildSolanaSnow(Transform root, Palette p)
        {
            var group = Group("Nieve", root);
            const float far = 450f, bottom = -8f;
            float tx0 = 143.7f, tx1 = 183.7f, tz0 = 40f, tz1 = 66f; // la torre
            BoxMinMax("Nieve_Oeste", group, new Vector3(-far, bottom, -far), new Vector3(0f, SnowWest, far + SD), p.Snow);
            BoxMinMax("Nieve_Sur", group, new Vector3(0f, bottom, -far), new Vector3(SW, SnowWest, 0f), p.Snow);
            BoxMinMax("Nieve_Norte", group, new Vector3(0f, bottom, SD), new Vector3(SW, SnowWest, far + SD), p.Snow);
            BoxMinMax("Nieve_Este_Sur", group, new Vector3(SW, bottom, -far), new Vector3(SW + far, SnowEast, tz0), p.Snow);
            BoxMinMax("Nieve_Este_Norte", group, new Vector3(SW, bottom, tz1), new Vector3(SW + far, SnowEast, far + SD), p.Snow);
            BoxMinMax("Nieve_Este_Entre", group, new Vector3(SW, bottom, tz0), new Vector3(tx0, SnowEast, tz1), p.Snow);
            BoxMinMax("Nieve_Este_Lejos", group, new Vector3(tx1, bottom, tz0), new Vector3(SW + far, SnowEast, tz1), p.Snow);
            // Transición de +10 a +5 al sureste y al noreste (sin escalón de 5 m).
            Wedge("Nieve_Bajada_Sureste", group, new Vector3(SW, SnowEast, -far), new Vector3(SW + 20f, SnowEast, 0f), SnowWest, SnowEast, true, p.Snow);
            Wedge("Nieve_Bajada_Noreste", group, new Vector3(SW, SnowEast, SD), new Vector3(SW + 20f, SnowEast, far + SD), SnowWest, SnowEast, true, p.Snow);
            // E1: la duna oeste sube de la nieve (+10) hasta el parapeto de la azotea (+17,2), y el cono de nieve baja adentro hasta
            // el piso del gimnasio colapsado (+11).
            Wedge("E1_Duna_Oeste", group, new Vector3(-40f, SnowWest, 16f), new Vector3(0f, SnowWest, 54f), SnowWest, 17.2f, true, p.Snow);
            Wedge("E1_Cono_de_Nieve", group, new Vector3(0f, 11f, 19f), new Vector3(24f, 11f, 51f), 17.2f, 11.3f, true, p.Snow);
            // E5: la nieve del este sube hasta el nivel 3 de la torre (+7,4) en su cara este.
            Wedge("E5_Rampa_Nieve_Torre", group, new Vector3(tx1, SnowEast, 38f), new Vector3(tx1 + 15f, SnowEast, 45f), 7.45f, SnowEast, true, p.Snow);
            // El cono de nieve bajo la claraboya, en el atrio (decorado).
            var cone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cone.name = "Nieve_Cono_Atrio";
            Object.DestroyImmediate(cone.GetComponent<Collider>());
            cone.transform.SetParent(group, false);
            cone.transform.position = new Vector3(56f, 0f, 41f);
            cone.transform.localScale = new Vector3(9f, 2.4f, 7f);
            cone.GetComponent<MeshRenderer>().sharedMaterial = p.Snow;
        }

        // Una cuña de nieve sobre un rectángulo (min.xz a max.xz, base en min.y): la altura va de hLow a hHigh a lo largo de x
        // (alongX) o de z. Con colisión de malla.
        private static GameObject Wedge(string name, Transform parent, Vector3 min, Vector3 max, float hAtMin, float hAtMax, bool alongX, Material material)
        {
            float y0 = min.y;
            float H(float x, float z) => alongX ? Mathf.Lerp(hAtMin, hAtMax, (x - min.x) / (max.x - min.x)) : Mathf.Lerp(hAtMin, hAtMax, (z - min.z) / (max.z - min.z));
            Vector3[] c =
            {
                new Vector3(min.x, y0, min.z), new Vector3(max.x, y0, min.z), new Vector3(max.x, y0, max.z), new Vector3(min.x, y0, max.z),
                new Vector3(min.x, H(min.x, min.z), min.z), new Vector3(max.x, H(max.x, min.z), min.z), new Vector3(max.x, H(max.x, max.z), max.z), new Vector3(min.x, H(min.x, max.z), max.z),
            };
            int[][] faces = { new[] { 4, 7, 6, 5 }, new[] { 0, 1, 2, 3 }, new[] { 0, 4, 5, 1 }, new[] { 1, 5, 6, 2 }, new[] { 2, 6, 7, 3 }, new[] { 3, 7, 4, 0 } };
            var verts = new List<Vector3>(); var tris = new List<int>();
            foreach (int[] f in faces)
            {
                int b = verts.Count;
                foreach (int i in f) verts.Add(c[i]);
                tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        // ------------------------------------------------------------------ Torre de estacionamiento (40 × 26 m, 14 niveles de 3,7 m)

        private static void BuildSolanaTower(Transform root, SolanaMats m)
        {
            var group = Group("Torre_Estacionamiento", root);
            const float x0 = 143.7f, x1 = 183.7f, z0 = 40f, z1 = 66f, step = 3.7f;
            float laneA0 = 177f, laneA1 = 180.4f, laneB0 = 180.4f, laneB1 = 183.6f;
            Material[] bands = { m.Yellow, m.Turquoise, m.Coral, m.Pink, m.Green, m.Orange, m.Blue, m.Lilac };
            string[] animals = { "BALLENA", "FLAMENCO", "TORTUGA", "PULPO", "JIRAFA", "LORO", "OSO", "PINGÜINO" };
            var none = new List<SDoor>();
            for (int n = 1; n <= 14; n++)
            {
                float y = (n - 1) * step;
                var lv = Group($"Nivel_{n:00}", group);
                var holes = new List<Rect>();
                if (n > 1) holes.Add((n - 1) % 2 == 1 ? new Rect(laneA0, 43f, laneA1 - laneA0, 20f) : new Rect(laneB0, 43f, laneB1 - laneB0, 20f));
                if (n == 7) holes.Add(new Rect(150f, 47f, 10f, 10f)); // la losa colapsada del nivel 7, que hay que rodear
                Slab(lv, "Losa", new Rect(x0, z0, x1 - x0, z1 - z0), y, holes, m.Slab);
                if (n == 7)
                {
                    var broken = BoxMinMax("Losa_Colapsada", lv, new Vector3(150f, y - 3.2f, 47f), new Vector3(160f, y - 2.9f, 57f), m.Slab);
                    broken.transform.rotation = Quaternion.Euler(14f, 0f, -9f);
                }
                if (n <= 14) // la del 14 sube al techo (mirador)
                {
                    if (n % 2 == 1) Ramp($"Rampa_{n}_{n + 1}", lv, new Vector3((laneA0 + laneA1) * 0.5f, y, 43f), new Vector3((laneA0 + laneA1) * 0.5f, y + step, 63f), laneA1 - laneA0, m.Ramp);
                    else Ramp($"Rampa_{n}_{n + 1}", lv, new Vector3((laneB0 + laneB1) * 0.5f, y, 63f), new Vector3((laneB0 + laneB1) * 0.5f, y + step, 43f), laneB1 - laneB0, m.Ramp);
                    foreach (float px in new[] { 153.7f, 163.7f, 173.7f })
                        foreach (float pz in new[] { 47f, 59f })
                            BoxMinMax("Pilar", lv, new Vector3(px - 0.3f, y, pz - 0.3f), new Vector3(px + 0.3f, y + step - SlabT, pz + 0.3f), m.Exterior);
                }
                if (n <= 2)
                {
                    // Enterrados: paredes cerradas.
                    WallLine(lv, true, z0, x0, x1, y, y + step - SlabT, ExtT, none, m.Exterior);
                    WallLine(lv, true, z1, x0, x1, y, y + step - SlabT, ExtT, none, m.Exterior);
                    WallLine(lv, false, x0, z0, z1, y, y + step - SlabT, ExtT, none, m.Exterior);
                    WallLine(lv, false, x1, z0, z1, y, y + step - SlabT, ExtT, none, m.Exterior);
                }
                else
                {
                    // Abiertos al viento: parapeto pintado del color del nivel (se repite cada ocho).
                    Material band = bands[(n - 1) % bands.Length];
                    var east = n == 3 ? new List<SDoor> { new SDoor(false, x1, 41.5f, 3f) } : none;   // E5
                    var west = n == 4 ? new List<SDoor> { new SDoor(false, x0, 58f, 3.5f) } : none;   // la pasarela
                    WallLine(lv, true, z0, x0, x1, y, y + 1f, ExtT, none, band);
                    WallLine(lv, true, z1, x0, x1, y, y + 1f, ExtT, none, band);
                    WallLine(lv, false, x0, z0, z1, y, y + 1f, ExtT, west, band);
                    WallLine(lv, false, x1, z0, z1, y, y + 1f, ExtT, east, band);
                    SolanaLabel(lv, $"LEVEL {n}  ·  {animals[(n - 1) % animals.Length]}", new Vector3(163.7f, y + 2.2f, 46.6f), 0f, 0.1f, Color.black);
                    SolanaLabel(lv, "PARK & SMILE", new Vector3(178.7f, y + 2.6f, 42.5f), 0f, 0.07f, new Color(0.3f, 0.3f, 0.3f));
                }
            }
            // El techo (+51,8): parapeto, letras PARKING, el mástil y el sol giratorio hasta +60.
            float roof = 14 * step;
            Slab(group, "Techo", new Rect(x0, z0, x1 - x0, z1 - z0), roof, new List<Rect> { new Rect(laneB0, 43f, laneB1 - laneB0, 20f) }, m.Slab);
            WallLine(group, true, z0, x0, x1, roof, roof + 1.1f, ExtT, none, m.Exterior);
            WallLine(group, true, z1, x0, x1, roof, roof + 1.1f, ExtT, none, m.Exterior);
            WallLine(group, false, x0, z0, z1, roof, roof + 1.1f, ExtT, none, m.Exterior);
            WallLine(group, false, x1, z0, z1, roof, roof + 1.1f, ExtT, none, m.Exterior);
            SolanaLabel(group, "PARKING", new Vector3(163.7f, roof + 3.5f, 39.6f), 0f, 0.6f, new Color(0.95f, 0.75f, 0.2f));
            BoxMinMax("Sol_Mastil", group, new Vector3(163.4f, roof, 52.7f), new Vector3(164f, roof + 5.8f, 53.3f), m.Rust);
            var sunPivot = Group("Sol_Giratorio", group);
            sunPivot.position = new Vector3(163.7f, roof + 5.8f, 53f);
            sunPivot.gameObject.AddComponent<WindSpinner>();
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Sol_Disco";
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(sunPivot, false);
            disc.transform.localPosition = Vector3.up * 2.2f;
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = new Vector3(4.4f, 0.15f, 4.4f);
            disc.GetComponent<MeshRenderer>().sharedMaterial = m.Yellow;
            for (int i = 0; i < 8; i++)
            {
                var ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ray.name = "Sol_Rayo";
                Object.DestroyImmediate(ray.GetComponent<Collider>());
                ray.transform.SetParent(sunPivot, false);
                float a = i * 45f;
                ray.transform.localPosition = Vector3.up * 2.2f + Quaternion.Euler(0f, 0f, a) * Vector3.up * 3.1f;
                ray.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                ray.transform.localScale = new Vector3(0.35f, 1.6f, 0.15f);
                ray.GetComponent<MeshRenderer>().sharedMaterial = m.Orange;
            }
            // La pasarela cubierta (nivel 4, +11) hasta el hall del cine (P2-03), con un tramo de vidrio roto.
            var bridge = Group("PK-03_Pasarela", group);
            BoxMinMax("Piso", bridge, new Vector3(SW, 11f - SlabT, 56f), new Vector3(x0, 11f, 60f), m.Slab);
            BoxMinMax("Techo", bridge, new Vector3(SW, 14f, 56f), new Vector3(x0, 14.3f, 60f), m.Exterior);
            WallLine(bridge, true, 56f, SW, x0, 11f, 12f, WallT, none, m.Exterior);
            WallLine(bridge, true, 60f, SW, x0, 11f, 12f, WallT, none, m.Exterior);
            BoxMinMax("Vidrio_Sur", bridge, new Vector3(SW, 12f, 55.9f), new Vector3(136f, 14f, 56.05f), m.Glass);
            BoxMinMax("Vidrio_Norte", bridge, new Vector3(SW, 12f, 59.95f), new Vector3(x0, 14f, 60.1f), m.Glass);
            SolanaLabel(group, "E5 · RAMPA", new Vector3(x1 + 0.6f, 9.5f, 41.5f), 270f, 0.12f, new Color(0.1f, 0.4f, 0.15f));
        }

        // ------------------------------------------------------------------ El cartel SOLANA (80 m, letras de 10 m, mira al sur)

        private static readonly Dictionary<char, string[]> BlockFont = new Dictionary<char, string[]>
        {
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
        };

        private static void BuildSolanaSign(Transform root, SolanaMats m)
        {
            var group = Group("Cartel_SOLANA", root);
            const float zf = 17.6f, zb = 18.4f, bottom = 19.6f, cell = 10f / 7f; // la viga a +19,2: se pasa por debajo al salir de la caseta
            // La estructura de marquesina, oxidada.
            foreach (float px in new[] { 46f, 70f, 95f, 120f })
                BoxMinMax("Estructura_Poste", group, new Vector3(px - 0.25f, 17f, zf), new Vector3(px + 0.25f, bottom, zb), m.Rust);
            BoxMinMax("Estructura_Viga", group, new Vector3(44f, bottom - 0.4f, zf), new Vector3(124f, bottom, zb), m.Rust);
            // El sol del emblema, sonriente, con un rayo caído.
            var sun = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sun.name = "Emblema_Sol";
            Object.DestroyImmediate(sun.GetComponent<Collider>());
            sun.transform.SetParent(group, false);
            sun.transform.position = new Vector3(48.5f, 24.6f, 18f);
            sun.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            sun.transform.localScale = new Vector3(8f, 0.4f, 8f);
            sun.GetComponent<MeshRenderer>().sharedMaterial = m.Yellow;
            BoxMinMax("Sol_Sonrisa", group, new Vector3(46.5f, 22.6f, 17.5f), new Vector3(50.5f, 23.1f, 17.6f), m.Orange, false);
            var fallen = BoxMinMax("Sol_Rayo_Caido", group, new Vector3(52f, 17f, 21f), new Vector3(56f, 17.4f, 21.6f), m.Orange);
            fallen.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            // Las letras: S amarilla, O turquesa (agrietada), L coral (inclinada), A rosa, N verde, A naranja (colgando de un soporte).
            string word = "SOLANA";
            Material[] colors = { m.Yellow, m.Turquoise, m.Coral, m.Pink, m.Green, m.Orange };
            float x = 56f, width = 5f * cell, gap = 4.5f;
            for (int k = 0; k < word.Length; k++)
            {
                var letter = Group($"Letra_{k + 1}_{word[k]}", group);
                letter.position = new Vector3(x, bottom, zf);
                string[] rows = BlockFont[word[k]];
                for (int r = 0; r < rows.Length; r++)
                    for (int c = 0; c < rows[r].Length; c++)
                        if (rows[r][c] == '#')
                            BoxMinMax("Bloque", letter, new Vector3(c * cell, (6 - r) * cell, 0f), new Vector3((c + 1) * cell, (7 - r) * cell, zb - zf), colors[k]);
                if (word[k] == 'L' && k == 2) letter.rotation = Quaternion.Euler(0f, 0f, 11f);                        // inclinada
                if (k == word.Length - 1) { letter.position += new Vector3(0f, -1.6f, 0f); letter.rotation = Quaternion.Euler(0f, 0f, -24f); } // cuelga
                x += width + gap;
            }
            // El banner SUMMER PARTY!, roto, con un extremo caído.
            var banner = Group("Banner_Summer_Party", group);
            banner.position = new Vector3(58f, 19.2f, 17.3f);
            banner.rotation = Quaternion.Euler(0f, 0f, -4f);
            BoxMinMax("Tela", banner, new Vector3(0f, -1.6f, 0f), new Vector3(52f, 0f, 0.05f), m.Pink, false);
            SolanaLabel(banner, "SUMMER PARTY!", new Vector3(84f, 18.4f, 17.2f), 0f, 0.28f, Color.white);
        }

        // ------------------------------------------------------------------ La vía al sur, el tren de referencia y el camino de farolas

        private static void BuildSolanaTrack(Transform root, SolanaMats m, Palette p)
        {
            var group = Group("Via_Sur", root);
            const float zc = -42f, y = SnowWest;
            BoxMinMax("Balasto", group, new Vector3(-400f, y - 0.1f, zc - 1.6f), new Vector3(600f, y + 0.15f, zc + 1.6f), p.Ballast);
            BoxMinMax("Riel_A", group, new Vector3(-400f, y + 0.15f, zc - 0.78f), new Vector3(600f, y + 0.3f, zc - 0.7f), p.Track, false);
            BoxMinMax("Riel_B", group, new Vector3(-400f, y + 0.15f, zc + 0.7f), new Vector3(600f, y + 0.3f, zc + 0.78f), p.Track, false);
            BoxMinMax("Tren_Referencia_19m", group, new Vector3(54.5f, y + 0.3f, zc - 1.7f), new Vector3(73.5f, y + 4.4f, zc + 1.7f), m.Coral, false);
            SolanaLabel(group, "TREN (19 m, de referencia)", new Vector3(64f, y + 5.5f, zc - 1.8f), 0f, 0.15f, Color.black);
            // El camino de farolas apagadas con banderines: miden 18 m y asoman 8 m; llevan de la vía al vestíbulo.
            Material[] flags = { m.Yellow, m.Turquoise, m.Coral, m.Pink };
            for (int i = 0; i < 4; i++)
            {
                float z = -34f + i * 9f;
                BoxMinMax($"Farola_{i}", group, new Vector3(63.8f, 0f, z - 0.2f), new Vector3(64.2f, 18f, z + 0.2f), m.Exterior);
                BoxMinMax($"Farola_{i}_Cabeza", group, new Vector3(63.4f, 17.6f, z - 0.4f), new Vector3(65.2f, 18.1f, z + 0.4f), m.Exterior, false);
                BoxMinMax($"Farola_{i}_Banderin", group, new Vector3(64.2f, 15.6f, z - 0.02f), new Vector3(65.4f, 17f, z + 0.02f), flags[i], false);
            }
            SolanaLabel(group, "SOLANA SHOPPING · Everything under one sun", new Vector3(-30f, SnowWest + 2.5f, 12f), 20f, 0.12f, new Color(0.2f, 0.2f, 0.2f));
        }

        // ------------------------------------------------------------------ Rutas para las pruebas (sección 12: "una prueba por paso")

        private static void BuildSolanaRoutes(List<SLevel> levels, Transform root)
        {
            var group = Group("Rutas_Prueba", root);
            void Route(string name, params Vector3[] points)
            {
                var r = Group(name, group);
                for (int i = 0; i < points.Length; i++) { var w = Group($"P{i:00}", r); w.position = points[i]; }
            }
            // Paso 4: entrar por el derrumbe (E1) y bajar a la Administración (P1-08).
            Route("E1_a_Administracion",
                new Vector3(-45f, 10f, 35f), new Vector3(-1f, 17f, 35f), new Vector3(22f, 11.8f, 35f), new Vector3(32f, 11f, 35f),
                new Vector3(41f, 11f, 35f), new Vector3(41f, 11f, 19f), new Vector3(84f, 11f, 19f), new Vector3(84f, 11f, 15f),
                new Vector3(87f, 11f, 14f), new Vector3(87f, 8.5f, 2f), new Vector3(81f, 8.5f, 2f), new Vector3(81f, 6f, 14f),
                new Vector3(84f, 6f, 15f), new Vector3(84f, 6f, 19f), new Vector3(41f, 6f, 19f), new Vector3(41f, 6f, 35f),
                new Vector3(34f, 6f, 35f), new Vector3(15f, 6f, 19f), new Vector3(15f, 6f, 8f));
            // Paso 2: del subsuelo a la azotea por la escalera de emergencia, sin saltos de posición.
            var up = new List<Vector3> { new Vector3(40f, -4.5f, 30f), new Vector3(84f, -4.5f, 28f), new Vector3(84f, -4.5f, 20f), new Vector3(84f, -4.5f, 15f), new Vector3(81f, -4.5f, 14f) };
            float[] stops = { -4.5f, 0f, 6f, 11f, 17f };
            for (int i = 0; i < stops.Length - 1; i++)
            {
                float mid = (stops[i] + stops[i + 1]) * 0.5f;
                up.Add(new Vector3(81f, mid, 2f)); up.Add(new Vector3(87f, mid, 2f)); up.Add(new Vector3(87f, stops[i + 1], 14f));
                if (i < stops.Length - 2) up.Add(new Vector3(81f, stops[i + 1], 14f));
            }
            up.Add(new Vector3(84f, 17f, 15f)); up.Add(new Vector3(84f, 17f, 18f)); up.Add(new Vector3(84f, 17f, 26f));
            Route("S1_a_Azotea", up.ToArray());
            // Las escaleras mecánicas del atrio: planta baja → Piso 1 → Piso 2.
            Route("Escaleras_Mecanicas",
                new Vector3(60f, 0f, 19f), new Vector3(66f, 0f, 25f), new Vector3(83.6f, 6f, 25f), new Vector3(83.6f, 6f, 29f),
                new Vector3(69f, 11f, 29f), new Vector3(65f, 11f, 26f), new Vector3(65f, 11f, 19f));
            // E2: de la azotea por la pasarela técnica y el panel roto hasta el balcón del Piso 2.
            Route("E2_Claraboya",
                new Vector3(88f, 17f, 30f), new Vector3(85.5f, 17f, 35f), new Vector3(44f, 17f, 35f), new Vector3(41f, 17f, 35f),
                new Vector3(41f, 17f, 50f), new Vector3(46f, 16.4f, 50f), new Vector3(64.5f, 11f, 50f), new Vector3(70f, 11f, 51f));
            // E5: subir por la nieve del este a la torre, la rampa al nivel 4 y la pasarela hasta el hall del cine (P2-03).
            Route("E5_Torre_a_Cine",
                new Vector3(196f, 5f, 41.5f), new Vector3(184.6f, 7.4f, 41.5f), new Vector3(178.7f, 7.4f, 42f), new Vector3(178.7f, 11.1f, 64f),
                new Vector3(175f, 11.1f, 64.5f), new Vector3(147f, 11.1f, 62f), new Vector3(145f, 11.1f, 58f), new Vector3(136f, 11f, 58f),
                new Vector3(120f, 11f, 58f));
        }
    }
}
