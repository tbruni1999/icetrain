using SurvivalEnTren.Interaction;
using SurvivalEnTren.Survival;
using SurvivalEnTren.Train;
using UnityEditor;
using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    // Arte (entrega del chat de arte, 2026-10-14): el vagón MR-04 de invierno es lo que se ve del tren, y los props (bidón,
    // lata, bomba manual) son lo que se ve de esos objetos.
    // - La colisión sigue siendo la del greybox (medida en el M1, el riesgo del Anexo C): el arte no tiene colliders y las
    //   cajas grises quedan invisibles. Nada de luz horneada ni Static: el tren se mueve.
    // - Las piezas móviles del arte se conectan a lo que ya las mueve: palancas y agujas (CabInstruments, FuelGauge),
    //   puertas (SlidingDoor, ahora con bisagra) y ruedas (TrainWheels).
    // - Lo que el juego arma por fases (la cama y el estante reforzado) sigue siendo del juego, con los materiales de la
    //   librería: el arte los trae terminados de una pieza por fase y el juego muestra el avance pieza por pieza (M8).
    // - Si el arte no está en el proyecto, todo se arma como antes (greybox).
    public static partial class TestScenesBuilder
    {
        private const string WagonArtPath = "Assets/_Project/Art/Tren/MR04/RedBlanca_MR04_v03_invierno.fbx";
        private const string PropsArtFolder = "Assets/_Project/Art/Props";
        private const string LibraryMaterialsFolder = "Assets/_Project/Art/Libreria/Materiales";
        private const string GeneratedArtFolder = "Assets/_Project/Art/Tren/MR04/Generado";
        private const float ConsoleRaise = 0.6f;     // cuánto se sube la consola de la sonda (la pantalla queda a 1,5 m del piso)
        private const float ConsoleShiftZ = -0.9f;   // cuánto se corre hacia atrás (de z 7,2 a 6,3: a la pared lisa entre ventanas)

        // Separa una malla en dos según el centro de cada triángulo (en coordenadas de la malla): [0] lo que queda, [1] lo que
        // cumple "take". Se guardan en Art/Tren/MR04/Generado (se rehacen cada vez que se arma la escena). Conserva materiales
        // (una submalla por material) y los dos canales de UV.
        private static Mesh[] SplitMesh(Mesh source, string restName, string partName, System.Func<Vector3, bool> take)
        {
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            var uv0 = new System.Collections.Generic.List<Vector2>();
            var uv1 = new System.Collections.Generic.List<Vector2>();
            source.GetUVs(0, uv0);
            source.GetUVs(1, uv1);

            Mesh Build(string name, bool wanted)
            {
                var map = new System.Collections.Generic.Dictionary<int, int>();
                var v = new System.Collections.Generic.List<Vector3>();
                var n = new System.Collections.Generic.List<Vector3>();
                var t = new System.Collections.Generic.List<Vector4>();
                var u0 = new System.Collections.Generic.List<Vector2>();
                var u1 = new System.Collections.Generic.List<Vector2>();
                var subs = new System.Collections.Generic.List<int>[source.subMeshCount];
                for (int sm = 0; sm < source.subMeshCount; sm++)
                {
                    subs[sm] = new System.Collections.Generic.List<int>();
                    int[] tris = source.GetTriangles(sm);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        Vector3 center = (vertices[tris[i]] + vertices[tris[i + 1]] + vertices[tris[i + 2]]) / 3f;
                        if (take(center) != wanted) continue;
                        for (int k = 0; k < 3; k++)
                        {
                            int old = tris[i + k];
                            if (!map.TryGetValue(old, out int index))
                            {
                                index = v.Count;
                                map[old] = index;
                                v.Add(vertices[old]);
                                if (normals.Length > 0) n.Add(normals[old]);
                                if (tangents.Length > 0) t.Add(tangents[old]);
                                if (uv0.Count > 0) u0.Add(uv0[old]);
                                if (uv1.Count > 0) u1.Add(uv1[old]);
                            }
                            subs[sm].Add(index);
                        }
                    }
                }
                var mesh = new Mesh { name = name };
                mesh.indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(v);
                if (n.Count > 0) mesh.SetNormals(n);
                if (t.Count > 0) mesh.SetTangents(t);
                if (u0.Count > 0) mesh.SetUVs(0, u0);
                if (u1.Count > 0) mesh.SetUVs(1, u1);
                mesh.subMeshCount = source.subMeshCount;
                for (int sm = 0; sm < source.subMeshCount; sm++) mesh.SetTriangles(subs[sm], sm);
                mesh.RecalculateBounds();

                if (!AssetDatabase.IsValidFolder(GeneratedArtFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art/Tren/MR04", "Generado");
                string path = $"{GeneratedArtFolder}/{name}.asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
                EditorUtility.CopySerialized(mesh, existing);
                existing.name = name;
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Mesh[] result = { Build(restName, false), Build(partName, true) };
            AssetDatabase.SaveAssets();
            return result;
        }

        private static Material LibraryMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{LibraryMaterialsFolder}/{name}.mat");

        private static GameObject InstantiateArt(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.SetParent(parent, false);
            // Las mallas arregladas (P4: Survival en tren > Arte > Arreglar mallas), si están; si no, las del FBX.
            string model = System.IO.Path.GetFileNameWithoutExtension(path);
            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh fixedMesh = MeshFix.FixedFor(model, filter.sharedMesh);
                if (fixedMesh != null) filter.sharedMesh = fixedMesh;
            }
            return go;
        }

        private static GameObject PropArt(string name, Transform parent) => InstantiateArt($"{PropsArtFolder}/{name}/{name}.fbx", parent);

        // Deja de verse (los colliders quedan).
        private static void Hide(Transform t)
        {
            if (t == null) return;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }

        private static void Paint(Transform t, string libraryMaterial)
        {
            Material m = LibraryMaterial(libraryMaterial);
            if (t == null || m == null) return;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = m;
        }

        // Medida del arte en las coordenadas de "space" (todas las mallas, de todos los LOD).
        private static Bounds BoundsIn(Transform space, GameObject art)
        {
            bool first = true;
            var b = new Bounds();
            foreach (MeshFilter mf in art.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 m = space.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? mb.min.x : mb.max.x, (i & 2) == 0 ? mb.min.y : mb.max.y, (i & 4) == 0 ? mb.min.z : mb.max.z);
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        // Apoya el arte sobre el origen de su padre: base en y = 0, centrado en x y z.
        private static Bounds SitOnParent(GameObject art)
        {
            Transform parent = art.transform.parent;
            Bounds b = BoundsIn(parent, art);
            art.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            return BoundsIn(parent, art);
        }

        // ------------------------------------------------------------------------------------------------ El vagón

        private static void ApplyTrainArt(GameObject root)
        {
            GameObject art = InstantiateArt(WagonArtPath, root.transform);
            if (art == null) return;
            art.name = "Arte_MR04";
            art.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // el FBX trae la cabina hacia -Z (la guía de arte lo avisa)
            Transform A(string name) => art.transform.Find(name);
            Transform G(string path) => root.transform.Find(path);

            // Lo gris deja de verse: el casco, el pupitre, la consola, la estufa, el banco, el estante precario y la sala de máquinas.
            Transform desk = G("Interior/Zona_Cabina/Cabina_Pupitre");
            Transform console = G("Interior/Zona_Cabina/Cabina_ConsolaSonda");
            Hide(G("Exterior"));
            Hide(desk.Find("Mueble"));
            foreach (string lever in new[] { "Reversor", "Potencia", "Freno" })
            {
                Hide(desk.Find(lever + "_Base"));
                Hide(desk.Find(lever + "_Eje"));
            }
            Hide(desk.Find("Velocimetro"));
            Transform fuelGauge = desk.Find("Medidor_Combustible");
            foreach (Transform c in fuelGauge) if (c.name != "Luz_Reserva") Hide(c);
            Hide(G("Interior/Zona_Cabina/Cabina_Asiento"));
            Hide(console.Find("Mueble"));
            Hide(console.Find("Pantalla/Carcasa"));
            Hide(G("Interior/Zona_Descanso/Descanso_Calefaccion"));
            Hide(G("Interior/Zona_Taller/Taller_Workbench"));
            Transform starterShelf = G("Interior/Zona_Taller/Taller_Estante");
            foreach (Transform c in starterShelf) if (c.name.StartsWith("Lateral_") || c.name.StartsWith("Repisa_")) Hide(c);
            Hide(G("Interior/Zona_SalaMaquinas"));

            // El casco gris sella el tren (pedido de Tomi, 2026-10-15: "las sombras se ven recontra mal"): sin dibujarse, solo hace
            // sombra. El modelo tiene uniones finas entre techo y paredes por donde el sol se colaba en dientes de sierra; las cajas
            // grises son gruesas y cerradas, con los mismos huecos de ventanas y puertas: la luz entra y sale solo por ahí.
            foreach (string shell in new[] { "Exterior/Piso", "Exterior/Techo", "Exterior/Pared_Derecha", "Exterior/Pared_Izquierda", "Exterior/Pared_Frente", "Exterior/Pared_Fondo" })
                foreach (Renderer r in G(shell).GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }

            // Luces de adentro (guía de arte: cálidas, puntuales; la estufa, el único punto naranja). Las dos del techo hacen sombra:
            // la luz cálida sale a la nieve solo por las ventanas. Una luz chica sobre el pupitre (la cabina quedaba muy oscura) y el
            // resplandor de la estufa, que se apaga si la calefacción se queda sin combustible (StoveGlow).
            foreach (Light interiorLight in G("Interior").GetComponentsInChildren<Light>(true))
            {
                interiorLight.shadows = LightShadows.Soft;
                interiorLight.shadowStrength = 0.85f;
                interiorLight.shadowNearPlane = 0.1f;
            }
            var cabLight = new GameObject("Luz_Cabina").AddComponent<Light>();
            cabLight.transform.SetParent(G("Interior/Zona_Cabina"), false);
            cabLight.transform.localPosition = new Vector3(0f, RoofBottom - 0.25f, 8.2f);
            cabLight.type = LightType.Point;
            cabLight.color = new Color(1f, 0.78f, 0.52f);
            cabLight.range = 4f;
            cabLight.intensity = 1.1f;
            cabLight.shadows = LightShadows.None;
            var stoveLight = new GameObject("Luz_Estufa").AddComponent<Light>();
            stoveLight.transform.SetParent(G("Interior/Zona_Descanso"), false);
            stoveLight.transform.localPosition = new Vector3(-InnerHalfWidth + 0.75f, FloorTop + 0.45f, 4.05f); // frente a la reja de la estufa
            stoveLight.type = LightType.Point;
            stoveLight.color = new Color(1f, 0.55f, 0.2f);
            stoveLight.range = 2.6f;
            stoveLight.intensity = 1.2f;
            stoveLight.shadows = LightShadows.None;
            var glow = stoveLight.gameObject.AddComponent<StoveGlow>();
            SetReference(glow, "stoveLight", stoveLight);

            // Lo que el juego ya tiene por su cuenta: la cama y el estante reforzado (se arman por fases), el bidón (es un objeto
            // que se lleva) y la pantalla de la sonda (la dibuja el juego).
            foreach (string name in new[] { "Cama_Base", "Cama_Colchon", "Cama_Relleno", "Estante_Estructura", "Estante_Soportes", "Estante_Tablas", "Bidon", "Vidrio_Radar" })
                if (A(name) != null) Object.DestroyImmediate(A(name).gameObject);

            // Las tablas del estante precario del arte quedan 3 cm más abajo que las del greybox: se bajan sus colliders y los
            // lugares donde se ve lo guardado, para que todo quede apoyado.
            foreach (Transform c in starterShelf)
                if (c.name.StartsWith("Repisa_") || c.name.StartsWith("Guardado_")) c.localPosition += Vector3.down * 0.03f;

            // Palancas: la palanca del arte pasa a girar con el eje del juego (el eje va a la base de la palanca del arte).
            var instruments = root.GetComponent<CabInstruments>();
            foreach (var (artName, pivotName) in new[] { ("Palanca_Reversor", "Reversor_Eje"), ("Palanca_Potencia", "Potencia_Eje"), ("Palanca_Freno", "Freno_Eje") })
            {
                Transform lever = A(artName), pivot = desk.Find(pivotName);
                pivot.position = lever.position;
                lever.SetParent(pivot, true);
            }

            // Agujas: un eje nuevo en el centro de cada dial, sin el giro de 180° del arte (gira sobre el eje z del tren, como el greybox).
            Transform NeedlePivot(Transform needle, string name)
            {
                Transform pivot = Group(name, desk);
                pivot.position = needle.position;
                pivot.rotation = root.transform.rotation;
                needle.SetParent(pivot, true);
                return pivot;
            }
            SetReference(instruments, "needlePivot", NeedlePivot(A("Aguja_Velocimetro"), "Aguja_Velocimetro_Eje"));
            Transform fuelNeedle = NeedlePivot(A("Aguja_Combustible"), "Aguja_Combustible_Eje");
            var gaugeComponent = fuelGauge.GetComponent<FuelGauge>();
            SetReference(gaugeComponent, "needlePivot", fuelNeedle);
            // La luz de reserva, abajo del centro del dial de combustible (entre la E y la F).
            fuelGauge.Find("Luz_Reserva").position = fuelNeedle.position + new Vector3(0f, -0.075f, -0.012f);

            // Escalas medidas en la textura de los diales (MT_Diales): el velocímetro va de 0 km/h a 135° (abajo a la izquierda)
            // a 120 km/h a -107° (2,02° por km/h): equivale a una escala simétrica de ±135° con el máximo en 133,6 km/h.
            // El de combustible va de E a 135° a F a -135°. La guía de arte da ±30° a las palancas.
            var instrumentsSo = new SerializedObject(instruments);
            instrumentsSo.FindProperty("gaugeMaxKmh").floatValue = 133.6f;
            instrumentsSo.FindProperty("needleSweepDegrees").floatValue = 135f;
            instrumentsSo.FindProperty("leverMaxDegrees").floatValue = 30f;
            instrumentsSo.ApplyModifiedPropertiesWithoutUndo();
            var gaugeSo = new SerializedObject(gaugeComponent);
            gaugeSo.FindProperty("sweepDegrees").floatValue = 135f;
            gaugeSo.ApplyModifiedPropertiesWithoutUndo();

            // Ruedas: los cuatro ejes giran con la velocidad del tren.
            var wheels = root.AddComponent<TrainWheels>();
            SetReference(wheels, "train", root.GetComponent<TrainMover>());
            var wheelsSo = new SerializedObject(wheels);
            var axles = wheelsSo.FindProperty("axles");
            axles.arraySize = 4;
            for (int i = 0; i < 4; i++) axles.GetArrayElementAtIndex(i).objectReferenceValue = A($"Eje_Ruedas_{i + 1}");
            wheelsSo.ApplyModifiedPropertiesWithoutUndo();

            // Puertas: las del arte van con bisagra (abren 100° hacia afuera). La hoja del juego pasa a la bisagra y su panel
            // (lo que bloquea el paso cerrada) toma la medida de la puerta del arte; el hueco y la zona de uso no cambian.
            foreach (var (side, doorName) in new[] { (1f, "Puerta_Derecha"), (-1f, "Puerta_Izquierda") })
            {
                Transform door = G("Exterior/" + doorName);
                Transform leaf = door.Find("Hoja");
                Transform artDoor = A(doorName);
                for (int i = leaf.childCount - 1; i >= 0; i--) Object.DestroyImmediate(leaf.GetChild(i).gameObject);
                leaf.position = artDoor.position;
                leaf.rotation = root.transform.rotation;
                var panel = new GameObject("Panel");
                panel.transform.SetParent(leaf, false);
                var box = panel.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 1.09f, 0.55f);
                box.size = new Vector3(0.08f, 2.18f, 1.1f);
                artDoor.SetParent(leaf, true);
                artDoor.name = "Arte_" + doorName; // que no se confunda con la puerta del juego al buscarla por nombre
                var doorSo = new SerializedObject(door.GetComponent<SlidingDoor>());
                doorSo.FindProperty("openOffset").vector3Value = Vector3.zero;
                doorSo.FindProperty("openEuler").vector3Value = new Vector3(0f, side * 100f, 0f);
                doorSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // La consola de la sonda (pedido de Tomi, 2026-10-14: "poco visible, no queda a la altura de la vista" y "debería estar
            // más sobre la pared y no tanto sobre la ventana"). En el modelo viene pegada a la malla de la cabina, en el borde de la
            // ventana (z 6,8 a 7,6) y con la pantalla a 0,9 m del piso. Se separa de la cabina, se corre a la pared lisa entre dos
            // ventanas (z 5,9 a 6,7) y se sube sobre una base de acero: la pantalla queda a 1,5 m del piso (los ojos, a 1,65 m).
            Vector3 consoleMove = new Vector3(0f, ConsoleRaise, ConsoleShiftZ);
            Transform cabin = A("Cabina_LOD0");
            Mesh[] parts = SplitMesh(cabin.GetComponent<MeshFilter>().sharedMesh, "Cabina", "Consola_Sonda",
                c => -c.x >= -1.7f && -c.x <= -0.8f && -c.z >= 6.5f && -c.z <= 7.9f); // el FBX está girado 180°: x y z al revés
            cabin.GetComponent<MeshFilter>().sharedMesh = parts[0];
            var consoleArt = new GameObject("Arte_Consola_Sonda");
            consoleArt.transform.SetParent(console, false);
            consoleArt.transform.SetPositionAndRotation(cabin.position + root.transform.TransformVector(consoleMove), cabin.rotation);
            consoleArt.AddComponent<MeshFilter>().sharedMesh = parts[1];
            consoleArt.AddComponent<MeshRenderer>().sharedMaterials = cabin.GetComponent<MeshRenderer>().sharedMaterials;
            Bounds consoleBox = BoundsIn(console, consoleArt);

            // La base: cuatro patas de acero y una repisa abajo, debajo de la consola.
            Material steel = LibraryMaterial("MT_AceroOscuro");
            float baseTop = consoleBox.min.y;
            foreach (float x in new[] { consoleBox.min.x + 0.02f, consoleBox.max.x - 0.06f })
                foreach (float z in new[] { consoleBox.min.z + 0.02f, consoleBox.max.z - 0.06f })
                    BoxMinMax("Base_Pata", console, new Vector3(x, FloorTop, z), new Vector3(x + 0.04f, baseTop, z + 0.04f), steel, false);
            BoxMinMax("Base_Repisa", console, new Vector3(consoleBox.min.x + 0.02f, FloorTop + 0.12f, consoleBox.min.z + 0.02f),
                new Vector3(consoleBox.max.x - 0.02f, FloorTop + 0.15f, consoleBox.max.z - 0.02f), steel, false);
            BoxMinMax("Base_Marco", console, new Vector3(consoleBox.min.x + 0.02f, baseTop - 0.04f, consoleBox.min.z + 0.02f),
                new Vector3(consoleBox.max.x - 0.02f, baseTop, consoleBox.max.z - 0.02f), steel, false);

            // Lo sólido de la consola (y lo que se apunta para fijar objetivo) va con ella: de piso a la parte de arriba.
            Transform furniture = console.Find("Mueble");
            furniture.localPosition = new Vector3(consoleBox.center.x, (FloorTop + consoleBox.max.y) * 0.5f, consoleBox.center.z);
            furniture.localScale = new Vector3(consoleBox.size.x, consoleBox.max.y - FloorTop, consoleBox.size.z);

            // La pantalla de la sonda (la dibuja el juego) va donde quedó el vidrio del radar: vertical, mirando al pasillo.
            Transform screen = console.Find("Pantalla");
            screen.position = root.transform.TransformPoint(new Vector3(-0.935f, 1.9f, 7.2f) + consoleMove);
            screen.localRotation = Quaternion.LookRotation(Vector3.left);

            // La sierra fabricada: se ve la del arte (colgada entre el banco y el estante), cuando el jugador la tiene.
            Transform sawVisual = G("Interior/Zona_Taller/Taller_Sierra/Sierra");
            for (int i = sawVisual.childCount - 1; i >= 0; i--) Object.DestroyImmediate(sawVisual.GetChild(i).gameObject);
            Transform artSaw = A("Sierra");
            artSaw.SetParent(sawVisual, true);
            artSaw.name = "Arte_Sierra";

            // Lo que arma el juego, con los materiales de la librería.
            Transform bed = G("Interior/Zona_Descanso/Descanso_Cama");
            Paint(bed.Find("Tela_Sucia"), "MT_TelaGris");
            foreach (Transform c in bed.Find("Obra"))
            {
                if (c.name.StartsWith("Base_")) Paint(c, "MT_Madera");
                else if (c.name.StartsWith("Relleno_")) Paint(c, "MT_Colchon");
                else if (c.name == "Manta") Paint(c, "MT_TelaManta");
                else if (c.name == "Almohada") Paint(c, "MT_TelaGris");
            }
        }

        // ------------------------------------------------------------------------------------------------ Props

        // El bidón (rojo; el fabricado, verde): la misma caja de colisión de siempre, con el arte encima.
        private static bool JerrycanArt(Transform can, string propName)
        {
            GameObject art = PropArt(propName, can);
            if (art == null) return false;
            art.name = "Arte";
            SitOnParent(art);
            return true;
        }

        // Una ración: la lata de conserva, con su caja de colisión a medida. Sin el arte, la caja de antes.
        private static GameObject NewRation(string name, Transform parent, Material fallback)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            GameObject art = PropArt("Lata_Conserva", go.transform);
            if (art != null)
            {
                art.name = "Arte";
                Bounds b = SitOnParent(art);
                // El origen queda 5 cm arriba de la base, como el centro de la caja de antes: las posiciones de siempre sirven.
                art.transform.localPosition += Vector3.down * 0.05f;
                b.center += Vector3.down * 0.05f;
                var box = go.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size;
            }
            else
            {
                Object.DestroyImmediate(go);
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localScale = new Vector3(0.18f, 0.1f, 0.12f);
                go.GetComponent<MeshRenderer>().sharedMaterial = fallback;
            }
            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<FoodItem>();
            return go;
        }

        // La bomba manual de la estación: acostada sobre la mesa (mide casi un metro).
        private static bool PumpArt(Transform pump)
        {
            GameObject art = PropArt("Bomba_Manual", pump);
            if (art == null) return false;
            art.name = "Arte";
            art.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            SitOnParent(art);
            return true;
        }
    }
}
