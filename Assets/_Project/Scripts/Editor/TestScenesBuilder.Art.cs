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

        private static Material LibraryMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{LibraryMaterialsFolder}/{name}.mat");

        private static GameObject InstantiateArt(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.SetParent(parent, false);
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

            // La pantalla de la sonda (la dibuja el juego) va donde el arte puso el vidrio del radar: vertical, mirando al pasillo.
            Transform screen = console.Find("Pantalla");
            screen.position = root.transform.TransformPoint(new Vector3(-0.935f, 1.9f, 7.2f));
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
            foreach (Transform c in G("Interior/Zona_Taller/Taller_EstanteReforzado"))
            {
                if (c.name.StartsWith("Soporte_")) Paint(c, "MT_AceroOscuro");
                else if (c.name.StartsWith("Poste_") || c.name.StartsWith("Tabla_")) Paint(c, "MT_Madera");
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
