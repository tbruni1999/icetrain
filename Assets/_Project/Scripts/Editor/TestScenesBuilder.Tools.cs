using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Player;
using SurvivalEnTren.Probe;
using SurvivalEnTren.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>Herramientas y forja (pedido de Tomi del 2026-10-15). H1: las herramientas en la mano y la barra de abajo.</summary>
    public static partial class TestScenesBuilder
    {
        private const string ToolSettingsPath = "Assets/_Project/Data/Equipment/Herramientas.asset";

        // La barreta y la sierra en la mano (hijos de la cabeza), el cinturón que las elige y golpea, y la barra en pantalla.
        // Cada modelo cuelga de un pivote en el puño: el golpe gira ese pivote.
        private static void BuildToolBelt(PlayerMotor motor, GameObject train)
        {
            EnsureFolder("Assets/_Project/Data", "Equipment");
            ToolSettings settings = LoadOrCreateAsset<ToolSettings>(ToolSettingsPath);
            Transform head = motor.transform.Find("Cabeza");
            // H4: el martillo en la casilla 4 (si el archivo es de antes, se le agrega; lo demás no se pisa).
            if (System.Array.FindIndex(settings.Slots, s => s.kind == HandSlotKind.Gear && s.gear == GearId.Hammer) < 0)
            {
                var slotsSo = new SerializedObject(settings);
                var slotList = slotsSo.FindProperty("slots");
                slotList.arraySize++;
                SerializedProperty hammerSlot = slotList.GetArrayElementAtIndex(slotList.arraySize - 1);
                hammerSlot.FindPropertyRelative("label").stringValue = "MARTILLO";
                hammerSlot.FindPropertyRelative("kind").enumValueIndex = (int)HandSlotKind.Gear;
                hammerSlot.FindPropertyRelative("gear").enumValueIndex = (int)GearId.Hammer;
                hammerSlot.FindPropertyRelative("swingSeconds").floatValue = 0.6f;
                hammerSlot.FindPropertyRelative("hitAt").floatValue = 0.45f;
                hammerSlot.FindPropertyRelative("reachMeters").floatValue = 2.0f;
                slotsSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }
            // 2026-10-16: la llave de montaje en la casilla 5 (mueve los muebles colocados; si el archivo es de antes, se le agrega).
            if (System.Array.FindIndex(settings.Slots, s => s.kind == HandSlotKind.Gear && s.gear == GearId.Wrench) < 0)
            {
                var slotsSo = new SerializedObject(settings);
                var slotList = slotsSo.FindProperty("slots");
                slotList.arraySize++;
                SerializedProperty wrenchSlot = slotList.GetArrayElementAtIndex(slotList.arraySize - 1);
                wrenchSlot.FindPropertyRelative("label").stringValue = "LLAVE";
                wrenchSlot.FindPropertyRelative("kind").enumValueIndex = (int)HandSlotKind.Gear;
                wrenchSlot.FindPropertyRelative("gear").enumValueIndex = (int)GearId.Wrench;
                wrenchSlot.FindPropertyRelative("swingSeconds").floatValue = 0.5f;
                wrenchSlot.FindPropertyRelative("hitAt").floatValue = 0.4f;
                wrenchSlot.FindPropertyRelative("reachMeters").floatValue = 2.4f;
                slotsSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }

            // Barreta: hierro hexagonal de 75 cm con la uña doblada hacia adelante arriba (sin arte todavía: pedida al chat de arte).
            Material steel = LibraryMaterial("MT_AceroOscuro") ?? Mat("Barreta_Acero", new Color(0.18f, 0.19f, 0.2f));
            Material paint = LibraryMaterial("MT_PinturaRoja") ?? Mat("Barreta_Pintura", new Color(0.6f, 0.12f, 0.08f));
            var crowbar = Group("Mano_Barreta", head);
            crowbar.localPosition = new Vector3(0.33f, -0.47f, 0.44f);
            crowbar.localRotation = Quaternion.Euler(30f, -14f, -10f);
            BoxMinMax("Hierro", crowbar, new Vector3(-0.012f, -0.05f, -0.012f), new Vector3(0.012f, 0.62f, 0.012f), steel, false);
            BoxMinMax("Pintura", crowbar, new Vector3(-0.013f, -0.05f, -0.013f), new Vector3(0.013f, 0.16f, 0.013f), paint, false); // el mango pintado
            var claw = Group("Una", crowbar);
            claw.localPosition = new Vector3(0f, 0.62f, 0f);
            claw.localRotation = Quaternion.Euler(55f, 0f, 0f);
            BoxMinMax("Curva", claw, new Vector3(-0.012f, -0.01f, -0.012f), new Vector3(0.012f, 0.11f, 0.012f), steel, false);
            BoxMinMax("Punta", claw, new Vector3(-0.016f, 0.1f, -0.004f), new Vector3(0.016f, 0.14f, 0.008f), steel, false);

            // Sierra: la del arte del vagón (la misma que cuelga en el taller), con la hoja hacia adelante.
            var saw = Group("Mano_Sierra", head);
            saw.localPosition = new Vector3(0.26f, -0.26f, 0.38f);
            saw.localRotation = Quaternion.Euler(14f, -12f, 0f);
            GameObject wagonArt = InstantiateArt(WagonArtPath, saw);
            Transform artSaw = wagonArt != null ? wagonArt.transform.Find("Sierra") : null;
            if (artSaw != null)
            {
                artSaw.SetParent(saw, false);
                artSaw.localPosition = Vector3.zero;
                artSaw.localRotation = Quaternion.Euler(-90f, 0f, 0f); // la hoja colgaba hacia abajo: ahora va hacia adelante
                Object.DestroyImmediate(wagonArt);
            }
            else
            {
                if (wagonArt != null) Object.DestroyImmediate(wagonArt);
                Material blade = Mat("Sierra_Hoja", new Color(0.66f, 0.68f, 0.7f));
                BoxMinMax("Hoja", saw, new Vector3(-0.006f, -0.05f, 0.08f), new Vector3(0.006f, 0.05f, 0.58f), blade, false);
                BoxMinMax("Mango", saw, new Vector3(-0.02f, -0.06f, -0.04f), new Vector3(0.02f, 0.06f, 0.08f), Mat("Cama_Madera", new Color(0.55f, 0.4f, 0.24f)), false);
            }

            // Martillo (H4): mango de madera y cabeza de acero (sin arte todavía).
            var hammer = Group("Mano_Martillo", head);
            hammer.localPosition = new Vector3(0.3f, -0.42f, 0.42f);
            hammer.localRotation = Quaternion.Euler(30f, -14f, -10f);
            Material handle = LibraryMaterial("MT_Madera") ?? Mat("Martillo_Mango", new Color(0.55f, 0.4f, 0.24f));
            BoxMinMax("Mango", hammer, new Vector3(-0.014f, -0.04f, -0.014f), new Vector3(0.014f, 0.34f, 0.014f), handle, false);
            BoxMinMax("Cabeza", hammer, new Vector3(-0.025f, 0.3f, -0.07f), new Vector3(0.025f, 0.36f, 0.06f), steel, false);

            // Llave de montaje (2026-10-16): una llave fija larga, de acero, con una boca en cada punta (sin arte todavía).
            var wrench = Group("Mano_Llave", head);
            wrench.localPosition = new Vector3(0.3f, -0.42f, 0.42f);
            wrench.localRotation = Quaternion.Euler(30f, -14f, -10f);
            BoxMinMax("Cuerpo", wrench, new Vector3(-0.012f, -0.03f, -0.005f), new Vector3(0.012f, 0.3f, 0.005f), steel, false);
            BoxMinMax("Boca_A_1", wrench, new Vector3(-0.03f, 0.3f, -0.006f), new Vector3(-0.012f, 0.35f, 0.006f), steel, false);
            BoxMinMax("Boca_A_2", wrench, new Vector3(0.012f, 0.3f, -0.006f), new Vector3(0.03f, 0.35f, 0.006f), steel, false);
            BoxMinMax("Boca_B", wrench, new Vector3(-0.025f, -0.06f, -0.006f), new Vector3(0.025f, -0.03f, 0.006f), steel, false);

            foreach (Transform model in new[] { crowbar, saw, hammer, wrench })
            {
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                foreach (Collider c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            }

            var belt = motor.gameObject.AddComponent<ToolBelt>();
            SetReference(belt, "settings", settings);
            SetReference(belt, "gear", motor.GetComponent<PlayerGear>());
            SetReference(belt, "probe", motor.GetComponent<PortableProbe>());
            SetReference(belt, "motor", motor);
            SetReference(belt, "carrier", motor.GetComponent<PlayerCarrier>());
            SetReference(belt, "interactor", motor.GetComponent<PlayerInteractor>());
            SetReference(belt, "vitals", motor.GetComponent<SurvivalEnTren.Survival.PlayerVitals>());
            SetReference(belt, "head", head);
            SetReference(belt, "use", FindActionReference("Player/UseTool"));
            SetReference(belt, "scroll", FindActionReference("Player/ToolScroll"));
            var so = new SerializedObject(belt);
            // Un modelo por casilla, en el orden de las casillas (la sonda no tiene: es PortableProbe).
            ToolSlot[] slots = settings.Slots;
            var models = so.FindProperty("handModels");
            models.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                Transform model = slots[i].kind != HandSlotKind.Gear ? null
                    : slots[i].gear == GearId.Crowbar ? crowbar : slots[i].gear == GearId.Saw ? saw : slots[i].gear == GearId.Hammer ? hammer : slots[i].gear == GearId.Wrench ? wrench : null;
                models.GetArrayElementAtIndex(i).objectReferenceValue = model;
            }
            var keys = so.FindProperty("slotKeys");
            keys.arraySize = 5;
            for (int i = 0; i < 5; i++) keys.GetArrayElementAtIndex(i).objectReferenceValue = FindActionReference($"Player/Tool{i + 1}");
            so.ApplyModifiedPropertiesWithoutUndo();

            // Colocar muebles (2026-10-16): con el kit de un mueble en la mano, o moviéndolo con la llave.
            var placer = motor.gameObject.AddComponent<SurvivalEnTren.Building.PlacementController>();
            SetReference(placer, "head", head);
            SetReference(placer, "motor", motor);
            SetReference(placer, "carrier", motor.GetComponent<PlayerCarrier>());
            SetReference(placer, "belt", belt);
            SetReference(placer, "use", FindActionReference("Player/UseTool"));
            SetReference(placer, "scroll", FindActionReference("Player/ToolScroll"));
            SetReference(placer, "okMaterial", PreviewMaterial("Colocar_Bien", new Color(0.92f, 0.95f, 1f, 0.3f)));
            SetReference(placer, "badMaterial", PreviewMaterial("Colocar_Mal", new Color(1f, 0.32f, 0.25f, 0.34f)));
            SetReference(belt, "placement", placer);

            var hud = motor.gameObject.AddComponent<ToolBarHud>();
            SetReference(hud, "belt", belt);

            foreach (GearDisplay display in train.GetComponentsInChildren<GearDisplay>(true)) SetReference(display, "belt", belt);
        }
    }
}
