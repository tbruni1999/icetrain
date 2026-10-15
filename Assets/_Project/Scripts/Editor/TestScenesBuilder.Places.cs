using System.Collections.Generic;
using SurvivalEnTren.Items;
using SurvivalEnTren.Player;
using SurvivalEnTren.Rail;
using SurvivalEnTren.Train;
using UnityEditor;
using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    // Producción P2 — Lugares con arte (plan de la noche, Tomi 2026-10-15). Los lugares siguen siendo los del juego (mismas
    // cajas, mismos colliders, mismas posiciones: nada de lo jugable cambia), pero se visten con los materiales de la librería
    // MR-04: cada material gris pasa a uno de la librería (MaterialMap) y cada caja recibe coordenadas de textura en metros, así
    // la textura se ve a tamaño real (catalogo.json: escala_m). La vía gana balasto, durmientes y rieles con textura.
    // No toca el tren (ya es el arte del vagón), al jugador, los textos ni las partículas.
    public static partial class TestScenesBuilder
    {
        private const string CatalogPath = "Assets/_Project/Art/Libreria/catalogo.json";

        // Material gris del armado (nombre) → material de la librería. PROPUESTA: se cambia acá.
        private static readonly Dictionary<string, string> MaterialMap = new Dictionary<string, string>
        {
            { "Nieve", "MT_NieveCompacta" },          // montones de nieve contra los lugares
            { "Balasto", "MT_Balasto" },
            { "Metal_Oscuro", "MT_AceroOscuro" },
            { "Tren_Carroceria", "MT_PinturaVerde" },
            { "Tren_Piso", "MT_ChapaDiamante" },
            { "Mobiliario", "MT_Madera" },
            { "Equipos", "MT_PinturaGrisMaquinaria" },
            { "Combustible", "MT_PinturaOcre" },      // lo que tiene que ver con combustible: ocre, color de lectura (guía de arte)
            { "Poste", "MT_MaderaIntemperie" },
            { "Poste_Marca", "MT_PinturaNaranjaSeguridad" },
            { "Caja", "MT_Carton" },
            { "Armario", "MT_PinturaGrisMaquinaria" },
            { "Colchon", "MT_Colchon" },
            { "Colchon_Franja", "MT_TelaGris" },
            { "Estacion_Muro", "MT_HormigonManchado" },
            { "Estacion_Piso", "MT_Hormigon" },
            { "Estacion_Puerta", "MT_PinturaVerde" },
            { "Estacion_Techo", "MT_ChapaCorrugada" },
            { "Hielo", "MT_Hielo" },
            { "Refugio_Madera", "MT_TablasIntemperie" },
            { "Refugio_Piso", "MT_TablasMadera" },
            { "Refugio_Puerta", "MT_MaderaPintada" },
            { "Tablero", "MT_PinturaGrisMaquinaria" },
            { "Taller_Chapa", "MT_ChapaCorrugada" },
            { "Taller_Puerta", "MT_ChapaCorrugadaVerde" },
            { "Vehiculo", "MT_PinturaAzulIndustrial" },
        };

        [System.Serializable] private class CatalogEntry { public string material; public float escala_m; }
        [System.Serializable] private class CatalogList { public CatalogEntry[] items; }
        private static Dictionary<string, float> _textureMeters;

        // Tamaño real de la textura de un material de la librería, en metros (1 si no está en el catálogo).
        private static float TextureMeters(string material)
        {
            if (_textureMeters == null)
            {
                _textureMeters = new Dictionary<string, float>();
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
                if (asset != null)
                {
                    var list = JsonUtility.FromJson<CatalogList>("{\"items\":" + asset.text + "}");
                    if (list?.items != null)
                        foreach (CatalogEntry e in list.items)
                            if (!string.IsNullOrEmpty(e.material) && e.escala_m > 0f) _textureMeters[e.material] = e.escala_m;
                }
            }
            return _textureMeters.TryGetValue(material, out float meters) ? meters : 1f;
        }

        private static readonly Dictionary<string, Mesh> MetricCubes = new Dictionary<string, Mesh>();

        // Un cubo de 1 x 1 x 1 (como el primitivo de Unity) con las coordenadas de textura en metros reales para una caja de
        // tamaño "size", divididas por el tamaño de la textura: cada cara usa sus dos lados.
        private static Mesh MetricCube(Vector3 size, float textureMeters)
        {
            string key = $"{size.x:0.000}_{size.y:0.000}_{size.z:0.000}_{textureMeters:0.00}";
            if (MetricCubes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            void Face(Vector3 normal, Vector3 u, Vector3 w, float su, float sw)
            {
                int s = v.Count;
                Vector3 c = normal * 0.5f;
                v.Add(c - u * 0.5f - w * 0.5f); v.Add(c - u * 0.5f + w * 0.5f); v.Add(c + u * 0.5f + w * 0.5f); v.Add(c + u * 0.5f - w * 0.5f);
                for (int i = 0; i < 4; i++) n.Add(normal);
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(0f, sw / textureMeters)); uv.Add(new Vector2(su / textureMeters, sw / textureMeters)); uv.Add(new Vector2(su / textureMeters, 0f));
                t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
            }
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z);
            Face(Vector3.down, Vector3.right, Vector3.back, size.x, size.z);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z, size.y);
            Face(Vector3.forward, Vector3.left, Vector3.up, size.x, size.y);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y);
            var mesh = new Mesh { name = "Caja_Metrica" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            MetricCubes[key] = mesh;
            return mesh;
        }

        // Viste un renderer con un material de la librería; si es una caja (el cubo de Unity), con la textura a tamaño real.
        private static bool Dress(Renderer renderer, string libraryMaterial)
        {
            Material material = LibraryMaterial(libraryMaterial);
            if (material == null) return false;
            renderer.sharedMaterial = material;
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name == "Cube")
                filter.sharedMesh = MetricCube(renderer.transform.lossyScale, TextureMeters(libraryMaterial));
            return true;
        }

        private static void StylePlaces(GameObject train)
        {
            if (LibraryMaterial("MT_Hormigon") == null) return; // sin la librería, todo queda gris como antes
            MetricCubes.Clear();
            int dressed = 0;
            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (train != null && renderer.transform.IsChildOf(train.transform)) continue;
                if (renderer.GetComponentInParent<PlayerMotor>() != null || renderer.GetComponent<TextMesh>() != null) continue;
                if (renderer.GetComponent<RailSegmentVisual>() != null) continue;
                Material current = renderer.sharedMaterial;
                if (current == null || !MaterialMap.TryGetValue(current.name, out string target)) continue;
                // Los pallets usan el color "Caja": son madera, no cartón.
                if (current.name == "Caja" && renderer.GetComponentInParent<SalvageProp>() != null) target = "MT_MaderaIntemperie";
                if (Dress(renderer, target)) dressed++;
            }

            // La vía: balasto, durmientes y rieles.
            Material ballast = LibraryMaterial("MT_Balasto"), sleeper = LibraryMaterial("MT_Durmiente"), rail = LibraryMaterial("MT_AceroOxidado");
            if (ballast != null && sleeper != null && rail != null)
                foreach (RailSegmentVisual visual in Object.FindObjectsByType<RailSegmentVisual>())
                {
                    visual.GetComponent<MeshRenderer>().sharedMaterials = new[] { ballast, sleeper, rail };
                    visual.SetTextureMeters(TextureMeters("MT_Balasto"), TextureMeters("MT_Durmiente"), TextureMeters("MT_AceroOxidado"));
                    visual.Rebuild();
                }
            // Nieve sobre los techos y montones contra las paredes de los galpones que no tenían (la estación y el taller ya tienen).
            foreach (string shed in new[] { "Taller_Ferroviario", "Refugio" })
            {
                GameObject found = GameObject.Find(shed);
                if (found != null && found.transform.Find("Nieve_Techo") == null) AddSnowCover(found.transform);
            }
            RoundSnowSlabs();
            DressScrapPile();
            DressVehicle();
            Debug.Log($"[Lugares] vestidos {dressed} objetos con la librería MR-04");
        }

        // Los montones de nieve contra las paredes eran cajas inclinadas (se veían duras): se ve un montón redondeado en su lugar.
        // La colisión de la caja queda igual (lo que se camina no cambia).
        private static void RoundSnowSlabs()
        {
            Material packed = LibraryMaterial("MT_NieveCompacta");
            if (packed == null) return;
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>())
            {
                if (!r.name.StartsWith("Nieve_") || r.name == "Nieve_Techo" || r.name == "Nieve_Monton") continue;
                if (r.transform.localRotation == Quaternion.identity) continue; // solo las inclinadas
                Bounds b = r.bounds;
                r.enabled = false;
                var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mound.name = "Nieve_Monton";
                Object.DestroyImmediate(mound.GetComponent<Collider>());
                mound.transform.SetParent(r.transform.parent, true);
                mound.transform.position = new Vector3(b.center.x, Mathf.Max(0f, b.min.y), b.center.z);
                mound.transform.rotation = r.transform.parent != null ? r.transform.parent.rotation : Quaternion.identity;
                Vector3 local = r.transform.parent != null ? r.transform.parent.InverseTransformVector(b.size) : b.size;
                mound.transform.localScale = new Vector3(Mathf.Abs(local.x), Mathf.Max(0.5f, b.max.y) * 2f, Mathf.Abs(local.z));
                mound.GetComponent<MeshRenderer>().sharedMaterial = packed;
            }
        }

        // La masa metálica (la señal que confunde a la sonda, M5): una caja negra. Se ve como un montón de chatarra oxidada; la
        // caja queda como colisión.
        private static void DressScrapPile()
        {
            GameObject pile = GameObject.Find("Restos_Metalicos");
            Material rust = LibraryMaterial("MT_AceroOxidado"), dark = LibraryMaterial("MT_AceroOscuro"), sheet = LibraryMaterial("MT_ChapaCorrugada");
            if (pile == null || rust == null) return;
            var renderer = pile.GetComponent<MeshRenderer>();
            Bounds b = renderer.bounds;
            renderer.enabled = false;
            var random = new System.Random(11);
            float Range(float a, float c) => a + (float)random.NextDouble() * (c - a);
            for (int i = 0; i < 14; i++)
            {
                bool tube = i % 3 == 0;
                var piece = GameObject.CreatePrimitive(tube ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                piece.name = "Chatarra";
                Object.DestroyImmediate(piece.GetComponent<Collider>());
                piece.transform.SetParent(pile.transform.parent, true);
                piece.transform.position = new Vector3(Range(b.min.x + 0.4f, b.max.x - 0.4f), Range(0.1f, b.max.y * 0.6f), Range(b.min.z + 0.4f, b.max.z - 0.4f));
                piece.transform.rotation = Quaternion.Euler(Range(-35f, 35f), Range(0f, 360f), Range(-35f, 35f));
                piece.transform.localScale = tube ? new Vector3(Range(0.2f, 0.5f), Range(0.6f, 1.6f), Range(0.2f, 0.5f))
                                                  : new Vector3(Range(0.4f, 2.2f), Range(0.05f, 0.6f), Range(0.4f, 1.8f));
                Dress(piece.GetComponent<MeshRenderer>(), i % 4 == 1 ? "MT_ChapaCorrugada" : i % 4 == 2 ? "MT_AceroOscuro" : "MT_AceroOxidado");
            }
        }

        // La camioneta abandonada: ruedas medio enterradas, vidrios oscuros y nieve en el techo y el capó (hasta que el arte la
        // reemplace; ver Docs/Props_Pedido_Arte.md).
        private static void DressVehicle()
        {
            GameObject vehicle = GameObject.Find("Vehiculo_Abandonado");
            if (vehicle == null) return;
            Transform root = vehicle.transform;
            Material rubber = LibraryMaterial("MT_Goma");
            foreach (var (x, z) in new[] { (-1.05f, -2.3f), (1.05f, -2.3f), (-1.05f, 2.2f), (1.05f, 2.2f) })
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Rueda";
                Object.DestroyImmediate(wheel.GetComponent<Collider>());
                wheel.transform.SetParent(root, false);
                wheel.transform.localPosition = new Vector3(x, 0.32f, z);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.8f, 0.14f, 0.8f);
                if (rubber != null) wheel.GetComponent<MeshRenderer>().sharedMaterial = rubber;
            }
            Transform glass = root.Find("Parabrisas");
            Material dirtyGlass = LibraryMaterial("MT_VidrioSucio");
            if (glass != null && dirtyGlass != null) glass.GetComponent<MeshRenderer>().sharedMaterial = dirtyGlass;
            foreach (var (name, min, max) in new[] { ("Nieve_Cabina", new Vector3(-1.0f, 1.75f, 0.45f), new Vector3(1.0f, 1.95f, 1.95f)),
                                                    ("Nieve_Capot", new Vector3(-1.0f, 1.15f, 2.05f), new Vector3(1.0f, 1.3f, 3.05f)),
                                                    ("Nieve_Caja", new Vector3(-1.05f, 1.45f, -3.15f), new Vector3(1.05f, 1.7f, 0.35f)) })
            {
                var snow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                snow.name = name;
                Object.DestroyImmediate(snow.GetComponent<Collider>());
                snow.transform.SetParent(root, false);
                snow.transform.localPosition = (min + max) * 0.5f;
                snow.transform.localScale = max - min;
                Dress(snow.GetComponent<MeshRenderer>(), "MT_Nieve");
            }
        }

        // Una capa de nieve sobre el techo (sin colisión: no se camina ahí) y dos montones bajos y largos contra las paredes que
        // no tienen puerta (con colisión: son nieve acumulada, se rodean). Medio enterrados, como en la estación.
        private static void AddSnowCover(Transform shed)
        {
            Transform roof = shed.Find("Techo");
            Transform door = shed.Find("Puerta");
            Material snow = LibraryMaterial("MT_Nieve"), packed = LibraryMaterial("MT_NieveCompacta");
            if (roof == null || snow == null || packed == null) return;
            Vector3 r = roof.localScale, c = roof.localPosition;
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = "Nieve_Techo";
            Object.DestroyImmediate(cover.GetComponent<Collider>());
            cover.transform.SetParent(shed, false);
            cover.transform.localPosition = new Vector3(c.x, c.y + r.y * 0.5f + 0.12f, c.z);
            cover.transform.localScale = new Vector3(r.x - 0.1f, 0.24f, r.z - 0.1f);
            Dress(cover.GetComponent<MeshRenderer>(), "MT_Nieve");

            float halfX = r.x * 0.5f - 0.2f, halfZ = r.z * 0.5f - 0.2f;
            float doorX = door != null ? Mathf.Sign(door.localPosition.x) : 1f;
            // Contra la pared opuesta a la puerta y contra la del norte.
            var drifts = new[] { (new Vector3(-doorX * (halfX + 0.6f), 0f, 0f), new Vector3(1.8f, 1.6f, halfZ * 2.2f)),
                                 (new Vector3(0f, 0f, halfZ + 0.6f), new Vector3(halfX * 2.2f, 1.4f, 1.8f)) };
            foreach (var (position, size) in drifts)
            {
                var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mound.name = "Nieve_Monton";
                Object.DestroyImmediate(mound.GetComponent<Collider>());
                mound.transform.SetParent(shed, false);
                mound.transform.localPosition = position;
                mound.transform.localScale = size;
                mound.GetComponent<MeshRenderer>().sharedMaterial = packed;
                mound.AddComponent<MeshCollider>().sharedMesh = mound.GetComponent<MeshFilter>().sharedMesh;
            }
        }
    }
}
