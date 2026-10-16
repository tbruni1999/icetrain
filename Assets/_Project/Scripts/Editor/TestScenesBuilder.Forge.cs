using System.Collections.Generic;
using SurvivalEnTren.Building;
using SurvivalEnTren.Crafting;
using SurvivalEnTren.Items;
using SurvivalEnTren.Player;
using SurvivalEnTren.Survival;
using SurvivalEnTren.Train;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>Herramientas y forja (pedido de Tomi del 2026-10-15), paso H4: la forja del tren y los restos de chatarra.</summary>
    public static partial class TestScenesBuilder
    {
        private const string ForgeSettingsPath = "Assets/_Project/Data/Crafting/Forja.asset";

        // La forja en el rincón derecho contra la sala de máquinas (entre el motor y la puerta de atrás), y el yunque pasando la
        // puerta. Es una obra por fases (hogar, fuelle, yunque): hasta terminarla, E aporta materiales; terminada, cada parte se
        // usa por separado (la boca del hogar, la puerta del fogón, la válvula del combustible y el yunque).
        // Coordenadas locales del tren (z+ adelante; la sala de máquinas empieza en z -7,5).
        private static void BuildForge(PlayerMotor motor, GameObject train)
        {
            MaterialSet materials = LoadMaterials();
            ForgeSettings settings = LoadOrCreateAsset<ForgeSettings>(ForgeSettingsPath);
            var settingsSo = new SerializedObject(settings);
            if (settingsSo.FindProperty("rawItem").objectReferenceValue == null)
            {
                settingsSo.FindProperty("rawItem").objectReferenceValue = materials.Metal;
                settingsSo.FindProperty("forgedItem").objectReferenceValue = materials.Bar;
                settingsSo.FindProperty("woodItem").objectReferenceValue = materials.Wood;
                settingsSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }
            // PROPUESTA (en datos): hogar chapa ×2 + tablón ×1; fuelle tela ×2 + tablón ×1; yunque chapa ×2.
            BuildProject project = LoadOrCreateBuildProject("Forja", "forja", new[] { 0f, 0f, 0f, 1f },
                ("hogar", new[] { (materials.Metal, 2), (materials.Wood, 1) }),
                ("fuelle", new[] { (materials.Cloth, 2), (materials.Wood, 1) }),
                ("yunque", new[] { (materials.Metal, 2) }));

            Material brick = LibraryMaterial("MT_Ladrillo") ?? Mat("Forja_Ladrillo", new Color(0.45f, 0.22f, 0.16f));
            Material iron = LibraryMaterial("MT_HierroFundido") ?? Mat("Forja_Hierro", new Color(0.16f, 0.16f, 0.17f));
            Material steel = LibraryMaterial("MT_AceroOscuro") ?? Mat("Forja_Acero", new Color(0.2f, 0.21f, 0.22f));
            Material leather = LibraryMaterial("MT_Cuero") ?? Mat("Forja_Cuero", new Color(0.35f, 0.22f, 0.13f));
            Material wood = LibraryMaterial("MT_Madera") ?? Mat("Forja_Madera", new Color(0.55f, 0.4f, 0.24f));
            Material red = LibraryMaterial("MT_PinturaRoja") ?? Mat("Forja_Rojo", new Color(0.6f, 0.1f, 0.08f));
            Material embersMat = Mat("Forja_Brasas", new Color(0.08f, 0.07f, 0.06f));
            Material pieceMat = Mat("Forja_Pieza", new Color(0.32f, 0.33f, 0.35f));
            foreach (Material glowing in new[] { embersMat, pieceMat }) // brillan con el calor (la forja les pone el color)
            {
                glowing.EnableKeyword("_EMISSION");
                glowing.SetColor("_EmissionColor", Color.black);
                glowing.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                EditorUtility.SetDirty(glowing);
            }

            var root = Group("Forja", train.transform); // en el origen del tren: todo en coordenadas del tren
            float y0 = FloorTop;

            // Fase 1: el hogar de ladrillo con su batea, las brasas, la campana y el caño hasta el techo, la puerta del fogón y la
            // válvula del combustible (un caño que sale del bloque del motor).
            var phase1 = new List<GameObject>
            {
                BoxMinMax("Hogar_Base", root, new Vector3(0.62f, y0, -7.42f), new Vector3(1.55f, y0 + 0.72f, -6.62f), brick),
                BoxMinMax("Hogar_Borde_A", root, new Vector3(0.7f, y0 + 0.72f, -7.34f), new Vector3(1.47f, y0 + 0.8f, -7.26f), iron),
                BoxMinMax("Hogar_Borde_B", root, new Vector3(0.7f, y0 + 0.72f, -6.78f), new Vector3(1.47f, y0 + 0.8f, -6.7f), iron),
                BoxMinMax("Hogar_Borde_C", root, new Vector3(0.7f, y0 + 0.72f, -7.26f), new Vector3(0.78f, y0 + 0.8f, -6.78f), iron),
                BoxMinMax("Hogar_Borde_D", root, new Vector3(1.39f, y0 + 0.72f, -7.26f), new Vector3(1.47f, y0 + 0.8f, -6.78f), iron),
                BoxMinMax("Campana", root, new Vector3(0.66f, y0 + 1.25f, -7.42f), new Vector3(1.52f, y0 + 1.55f, -6.92f), steel, false),
                BoxMinMax("Chimenea", root, new Vector3(1.0f, y0 + 1.55f, -7.32f), new Vector3(1.18f, RoofBottom, -7.14f), steel, false),
                BoxMinMax("Cano_Combustible", root, new Vector3(0.6f, y0 + 0.5f, -7.72f), new Vector3(0.64f, y0 + 0.54f, -6.8f), steel, false),
            };
            GameObject embers = BoxMinMax("Brasas", root, new Vector3(0.78f, y0 + 0.72f, -7.26f), new Vector3(1.39f, y0 + 0.78f, -6.78f), embersMat);
            GameObject firebox = BoxMinMax("Puerta_Fogon", root, new Vector3(0.59f, y0 + 0.15f, -7.2f), new Vector3(0.63f, y0 + 0.45f, -6.86f), iron);
            GameObject valve = BoxMinMax("Valvula_Combustible", root, new Vector3(0.55f, y0 + 0.44f, -6.82f), new Vector3(0.6f, y0 + 0.6f, -6.66f), red);
            phase1.Add(embers);
            phase1.Add(firebox);
            phase1.Add(valve);

            // Fase 2: el fuelle de cuero contra la sala de máquinas, detrás de la batea, con su palanca.
            var phase2 = new List<GameObject>
            {
                BoxMinMax("Fuelle_Cuero", root, new Vector3(0.7f, y0 + 0.85f, -7.45f), new Vector3(1.05f, y0 + 1.2f, -7.36f), leather, false),
                BoxMinMax("Fuelle_Tapa", root, new Vector3(0.68f, y0 + 1.2f, -7.46f), new Vector3(1.07f, y0 + 1.24f, -7.34f), wood, false),
                BoxMinMax("Fuelle_Palanca", root, new Vector3(0.6f, y0 + 1.22f, -7.42f), new Vector3(0.7f, y0 + 1.26f, -7.38f), wood, false),
            };

            // Fase 3: el yunque sobre un tronco, pasando la puerta.
            GameObject anvil = BoxMinMax("Yunque", root, new Vector3(0.88f, y0 + 0.6f, -5.18f), new Vector3(1.12f, y0 + 0.72f, -4.62f), iron);
            var phase3 = new List<GameObject>
            {
                BoxMinMax("Yunque_Tronco", root, new Vector3(0.8f, y0, -5.1f), new Vector3(1.2f, y0 + 0.6f, -4.7f), wood),
                anvil,
                BoxMinMax("Yunque_Cuerno", root, new Vector3(0.94f, y0 + 0.64f, -4.62f), new Vector3(1.06f, y0 + 0.7f, -4.45f), iron, false),
            };

            // Donde queda la pieza, la pieza, y la luz del fuego (no son piezas de la obra).
            var fireSlot = Group("Forja_Lugar_Fuego", root);
            fireSlot.localPosition = new Vector3(1.08f, y0 + 0.78f, -7.02f);
            var anvilSlot = Group("Forja_Lugar_Yunque", root);
            anvilSlot.localPosition = new Vector3(1.0f, y0 + 0.72f, -4.9f);
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = "Forja_Pieza";
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.transform.SetParent(fireSlot, false);
            piece.GetComponent<MeshRenderer>().sharedMaterial = pieceMat;
            var fireLight = new GameObject("Luz_Forja").AddComponent<Light>();
            fireLight.transform.SetParent(root, false);
            fireLight.transform.localPosition = new Vector3(1.05f, y0 + 1.05f, -7.0f);
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.55f, 0.25f);
            fireLight.range = 4.5f;
            fireLight.intensity = 0f;
            fireLight.shadows = LightShadows.None; // ya hay dos luces con sombra adentro (P1): esta no suma otra
            fireLight.enabled = false;

            // La zona de uso de la obra: el hogar (el yunque fantasma se ve más allá; se aporta mirando el hogar).
            var zone = BoxMinMax("Zona_Obra", root, new Vector3(0.55f, y0, -7.45f), new Vector3(1.6f, y0 + 1.6f, -6.6f), brick);
            zone.GetComponent<Collider>().isTrigger = true;
            Object.DestroyImmediate(zone.GetComponent<MeshRenderer>());

            BuildSite site = NewBuildSite(root, project, "obra_tren_forja_001",
                new List<GameObject[]> { phase1.ToArray(), phase2.ToArray(), phase3.ToArray() }, null);
            var siteSo = new SerializedObject(site);
            siteSo.FindProperty("zonesOffWhenComplete").boolValue = true; // terminada, se apunta a cada parte
            siteSo.ApplyModifiedPropertiesWithoutUndo();

            var forge = root.gameObject.AddComponent<Forge>();
            SetReference(forge, "settings", settings);
            SetReference(forge, "clock", Object.FindAnyObjectByType<GameClock>());
            SetReference(forge, "tank", train.GetComponent<FuelTank>());
            SetReference(forge, "climate", train.GetComponent<TrainInteriorClimate>());
            SetReference(forge, "site", site);
            SetReference(forge, "backpackSettings", LoadBackpackSettings());
            SetReference(forge, "fireSlot", fireSlot);
            SetReference(forge, "anvilSlot", anvilSlot);
            SetReference(forge, "piece", piece.GetComponent<MeshRenderer>());
            SetReference(forge, "embers", embers.GetComponent<MeshRenderer>());
            SetReference(forge, "fireLight", fireLight);
            foreach (var (part, role) in new[] { (embers, ForgePartRole.Mouth), (firebox, ForgePartRole.Firebox), (valve, ForgePartRole.Valve), (anvil, ForgePartRole.Anvil) })
            {
                var component = part.AddComponent<ForgePart>();
                SetReference(component, "forge", forge);
                var so = new SerializedObject(component);
                so.FindProperty("role").enumValueIndex = (int)role;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Restos de chatarra junto a la masa metálica (H4): el metal extra que piden la forja y las planchuelas (sin esto, las chapas
        // del mundo apenas alcanzaban para la bifurcación del M10). Se desarman a golpes de barreta: chapa ×2 cada uno.
        private static void BuildScrapHeaps(Transform marker, Palette p)
        {
            MaterialSet materials = LoadMaterials();
            BackpackSettings settings = LoadBackpackSettings();
            SalvageRecipe recipe = LoadOrCreateRecipe("Chatarra", "restos de chatarra", 8, SalvageSurface.Metal, (materials.Metal, 2));
            Material rust = LibraryMaterial("MT_AceroOxidado") ?? p.Track;
            Material dark = LibraryMaterial("MT_AceroOscuro") ?? p.Track;
            foreach (var (id, position, yaw) in new[] { ("salv_chatarra_001", new Vector3(2.6f, 0f, -1.8f), 20f), ("salv_chatarra_002", new Vector3(2.4f, 0f, 2.3f), -35f) })
            {
                var root = NewSalvageProp("Restos_Chatarra", marker, position, yaw, recipe, settings, id);
                BoxMinMax("Chapa_Doblada", root, new Vector3(-0.5f, 0f, -0.35f), new Vector3(0.45f, 0.08f, 0.35f), rust);
                var tilted = BoxMinMax("Chapa_Parada", root, new Vector3(-0.4f, 0f, -0.03f), new Vector3(0.4f, 0.6f, 0.03f), rust);
                tilted.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
                BoxMinMax("Cano", root, new Vector3(-0.55f, 0.08f, 0.15f), new Vector3(0.5f, 0.18f, 0.25f), dark);
                BoxMinMax("Caja", root, new Vector3(0.1f, 0.08f, -0.3f), new Vector3(0.42f, 0.36f, -0.02f), dark);
            }
        }
    }
}
