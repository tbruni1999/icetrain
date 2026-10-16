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

            foreach (Transform model in new[] { crowbar, saw })
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
                    : slots[i].gear == GearId.Crowbar ? crowbar : slots[i].gear == GearId.Saw ? saw : null;
                models.GetArrayElementAtIndex(i).objectReferenceValue = model;
            }
            var keys = so.FindProperty("slotKeys");
            keys.arraySize = 4;
            for (int i = 0; i < 4; i++) keys.GetArrayElementAtIndex(i).objectReferenceValue = FindActionReference($"Player/Tool{i + 1}");
            so.ApplyModifiedPropertiesWithoutUndo();

            var hud = motor.gameObject.AddComponent<ToolBarHud>();
            SetReference(hud, "belt", belt);

            foreach (GearDisplay display in train.GetComponentsInChildren<GearDisplay>(true)) SetReference(display, "belt", belt);
        }
    }
}
