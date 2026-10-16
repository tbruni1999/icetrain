using System.Collections.Generic;
using SurvivalEnTren.Building;
using SurvivalEnTren.Crafting;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using SurvivalEnTren.Survival;
using UnityEditor;
using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Mejoras nuevas (decisiones de Tomi del 2026-10-16): se arman en el banco con un tiempo y salen como kits que el jugador
    /// instala; los estantes se colocan donde uno quiere y la llave de montaje los mueve. Sin fantasmas azules.
    /// </summary>
    public static partial class TestScenesBuilder
    {
        private const string KitPrefabPath = "Assets/_Project/Prefabs/Items/Kit_Mejora.prefab";
        private const string ShelfPrefabPath = "Assets/_Project/Prefabs/Items/Estante_Reforzado.prefab";

        // Material translúcido sin luz (la vista previa al colocar un mueble).
        private static Material PreviewMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetFloat("_Surface", 1f);                 // transparente
            material.SetFloat("_Blend", 0f);                   // mezcla normal
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        // El cajón de un kit: madera con dos flejes de chapa. Se lleva con las dos manos; su peso lo pone el banco (lo que pesan
        // sus materiales). El origen está abajo al centro: sale apoyado en el piso.
        private static GameObject BuildKitPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs", "Items");
            Material wood = LibraryMaterial("MT_Madera") ?? Mat("Kit_Madera", new Color(0.55f, 0.4f, 0.24f));
            Material steel = LibraryMaterial("MT_AceroOscuro") ?? Mat("Kit_Fleje", new Color(0.2f, 0.21f, 0.22f));
            var root = new GameObject(UpgradeKit.ObjectName);
            BoxMinMax("Cajon", root.transform, new Vector3(-0.3f, 0f, -0.25f), new Vector3(0.3f, 0.4f, 0.25f), wood);
            foreach (float x in new[] { -0.17f, 0.17f })
                BoxMinMax($"Fleje_{(x < 0 ? "A" : "B")}", root.transform, new Vector3(x - 0.025f, -0.004f, -0.254f), new Vector3(x + 0.025f, 0.404f, 0.254f), steel, false);
            var body = root.AddComponent<Rigidbody>();
            body.mass = 10f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var carry = root.AddComponent<Carryable>();
            var carrySo = new SerializedObject(carry);
            carrySo.FindProperty("throwable").boolValue = false; // el clic instala
            carrySo.FindProperty("heldLow").boolValue = true;    // a la cintura: se ve por encima dónde va
            carrySo.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<UpgradeKit>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, KitPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // El estante reforzado, ahora un mueble que se coloca donde uno quiere: parado, con cuatro postes, cuatro tablas y
        // tirantes de chapa atrás. 1,6 m de ancho, 1,6 de alto y 0,42 de fondo; el frente mira a +Z. Guarda 6 x 4 casillas
        // (lo mismo que el estante reforzado terminado del M8).
        private static GameObject BuildShelfPrefab(string blueprintId)
        {
            EnsureFolder("Assets/_Project/Prefabs", "Items");
            Material wood = LibraryMaterial("MT_Madera") ?? Mat("Cama_Madera", new Color(0.55f, 0.4f, 0.24f));
            Material steel = LibraryMaterial("MT_AceroOscuro") ?? Mat("Estante_Acero", new Color(0.2f, 0.21f, 0.22f));
            var root = new GameObject("Estante_Reforzado");
            Transform t = root.transform;
            foreach (float x in new[] { -0.77f, 0.77f })
                foreach (float z in new[] { -0.18f, 0.18f })
                    BoxMinMax($"Poste_{(x < 0 ? "I" : "D")}{(z < 0 ? "A" : "F")}", t, new Vector3(x - 0.025f, 0f, z - 0.025f), new Vector3(x + 0.025f, 1.6f, z + 0.025f), wood);
            float[] boards = { 0.1f, 0.54f, 0.98f, 1.42f };
            for (int i = 0; i < boards.Length; i++)
                BoxMinMax($"Tabla_{i + 1}", t, new Vector3(-0.8f, boards[i], -0.21f), new Vector3(0.8f, boards[i] + 0.04f, 0.21f), wood);
            foreach (float y in new[] { 0.32f, 1.2f })
                BoxMinMax($"Tirante_{y:0.0}", t, new Vector3(-0.78f, y - 0.025f, -0.21f), new Vector3(0.78f, y + 0.025f, -0.195f), steel, false);

            // Lo guardado se ve en las tres tablas de abajo (un cubo por material).
            var slots = new List<Transform>();
            for (int i = 0; i < 3; i++)
                foreach (float x in new[] { -0.4f, 0.4f })
                {
                    var slot = Group($"Guardado_{slots.Count + 1}", t);
                    slot.localPosition = new Vector3(x, boards[i] + 0.04f, 0f);
                    slots.Add(slot);
                }
            var storage = root.AddComponent<StorageContainer>();
            ConfigureStorage(storage, "", "estante reforzado", new Vector2Int(6, 4), null, slots.ToArray());

            var placeable = root.AddComponent<Placeable>();
            var so = new SerializedObject(placeable);
            so.FindProperty("key").stringValue = blueprintId;
            so.FindProperty("displayName").stringValue = "estante reforzado";
            so.FindProperty("footprintCenter").vector3Value = new Vector3(0f, 0.8f, 0f);
            so.FindProperty("footprintSize").vector3Value = new Vector3(1.6f, 1.6f, 0.42f);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ShelfPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // El plano del estante reforzado era una obra contra la pared (M8): desde el 2026-10-16 es un mueble que el banco arma
        // (con su tiempo) y el jugador coloca. Se convierte una sola vez; después se ajusta en el archivo.
        private static void ConfigureShelfBlueprint(Blueprint shelf, MaterialSet materials)
        {
            GameObject prefab = BuildShelfPrefab(shelf.Id);
            var so = new SerializedObject(shelf);
            if (so.FindProperty("kind").enumValueIndex == (int)BlueprintKind.Project)
            {
                so.FindProperty("kind").enumValueIndex = (int)BlueprintKind.Craft;
                so.FindProperty("outputKind").enumValueIndex = (int)CraftOutputKind.Placeable;
                so.FindProperty("description").stringValue = "Un estante de verdad, de 6 x 4 casillas: lo colocás donde quieras dentro del tren.";
                so.FindProperty("outputText").stringValue = "un kit en el piso, al lado del banco";
                so.FindProperty("workMinutes").floatValue = 40f;
                // Lo mismo que pedían las cuatro fases de la obra del M8: soportes (chapa ×2) y madera ×6.
                var list = so.FindProperty("ingredients");
                list.arraySize = 2;
                list.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = materials.Metal;
                list.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = 2;
                list.GetArrayElementAtIndex(1).FindPropertyRelative("item").objectReferenceValue = materials.Wood;
                list.GetArrayElementAtIndex(1).FindPropertyRelative("count").intValue = 6;
            }
            so.FindProperty("placeablePrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shelf);
        }

        // El banco conoce las obras cuyos kits arma (la cama y la forja del tren y la bifurcación) y el reloj del juego.
        private static void WireWorkbench(GameObject train)
        {
            var bench = train.GetComponentInChildren<Workbench>(true);
            if (bench == null) return;
            var order = new[] { "obra_tren_cama_001", "obra_tren_forja_001", "obra_cambio_oeste_001" };
            var found = new List<BuildSite>();
            foreach (string id in order)
                foreach (BuildSite site in Object.FindObjectsByType<BuildSite>(FindObjectsInactive.Include))
                    if (site.StableId == id) { found.Add(site); break; }
            var so = new SerializedObject(bench);
            var list = so.FindProperty("sites");
            list.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            so.FindProperty("clock").objectReferenceValue = Object.FindAnyObjectByType<GameClock>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
