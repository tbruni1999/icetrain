using SurvivalEnTren.Equipment;
using SurvivalEnTren.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Las manos en primera persona (pedido de Tomi del 2026-10-16: "no es nada inmersivo, ni la barreta ni el golpe"): la capa
    /// y la cámara de las manos, la raíz que se balancea y cómo se mueve el golpe de cada herramienta.
    /// </summary>
    public static partial class TestScenesBuilder
    {
        private const string HandsLayerName = "Manos";
        private const string ViewmodelSettingsPath = "Assets/_Project/Data/Equipment/Manos.asset";

        // La capa de las manos: lo que se lleva en las manos se dibuja con su propia cámara, encima de la escena.
        private static int EnsureHandsLayer()
        {
            int existing = LayerMask.NameToLayer(HandsLayerName);
            if (existing >= 0) return existing;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue)) continue;
                layer.stringValue = HandsLayerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            Debug.LogError("[Manos] No hay lugar para la capa de las manos.");
            return 0;
        }

        // La raíz de las manos (hija de la cabeza), el ViewmodelRig y la cámara de las manos: una cámara "overlay" de URP que
        // dibuja solo la capa Manos encima de la principal (que ya no la dibuja). Así nada se mete en las paredes.
        private static ViewmodelRig BuildViewmodel(Transform head, PlayerMotor motor)
        {
            int layer = EnsureHandsLayer();
            ViewmodelSettings vmSettings = LoadOrCreateAsset<ViewmodelSettings>(ViewmodelSettingsPath);
            var hands = Group("Manos", head);
            var rig = head.gameObject.AddComponent<ViewmodelRig>();
            SetReference(rig, "settings", vmSettings);
            SetReference(rig, "motor", motor);
            SetReference(rig, "look", motor.GetComponent<PlayerLook>());
            SetReference(rig, "root", hands);

            Camera main = head.GetComponent<Camera>();
            main.cullingMask &= ~(1 << layer);
            var overlayObject = new GameObject("Camara_Manos");
            overlayObject.transform.SetParent(head, false);
            var overlay = overlayObject.AddComponent<Camera>();
            overlay.cullingMask = 1 << layer;
            overlay.fieldOfView = vmSettings.FieldOfView;
            overlay.nearClipPlane = 0.01f;
            overlay.farClipPlane = 10f;
            var overlayData = overlay.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            overlayData.renderShadows = false;
            overlayData.renderPostProcessing = true;
            var mainData = main.GetUniversalAdditionalCameraData();
            mainData.cameraStack.Clear();
            mainData.cameraStack.Add(overlay);
            return rig;
        }

        // Cómo se mueve el golpe de cada herramienta (PROPUESTA del 2026-10-16, en Data/Equipment/Herramientas.asset): se carga
        // una sola vez; después se ajusta en el archivo. La barreta en diagonal desde arriba (con una mano); el martillo corto y
        // seco; la sierra de ida y vuelta; la llave con un giro de muñeca.
        private static void MigrateSwingPoses(ToolSettings settings)
        {
            var so = new SerializedObject(settings);
            SerializedProperty slots = so.FindProperty("slots");
            bool changed = false;
            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);
                if (slot.FindPropertyRelative("kind").enumValueIndex != (int)HandSlotKind.Gear) continue;
                // Solo si no se ajustó a mano: vacío, o valores de versiones anteriores de este armado (la primera estaba en los
                // ejes del modelo; la barreta con dos manos quedó vieja: desde el 2026-10-16 va con una).
                Vector3 current = slot.FindPropertyRelative("windupRotation").vector3Value;
                bool oldVersion = current == new Vector3(-65f, -10f, 25f) || current == new Vector3(-8f, 0f, 0f) ||
                                  current == new Vector3(-70f, 0f, -25f) || current == new Vector3(-55f, 0f, -25f) ||
                                  current == new Vector3(-70f, 0f, 10f) || current == new Vector3(-20f, 0f, 35f);
                if (current != Vector3.zero && !oldVersion) continue;
                Vector3 wp, wr, sp, sr;
                float kick;
                bool two = false;
                switch ((GearId)slot.FindPropertyRelative("gear").enumValueIndex)
                {
                    case GearId.Crowbar: wp = new Vector3(0.06f, 0.2f, -0.03f); wr = new Vector3(-62f, 0f, -22f); sp = new Vector3(-0.08f, -0.03f, 0.16f); sr = new Vector3(48f, 0f, 24f); kick = 2.5f; two = false; break; // con una mano (Tomi, 2026-10-16)
                    case GearId.Saw: wp = new Vector3(0f, 0.02f, -0.14f); wr = new Vector3(-5f, 0f, 0f); sp = new Vector3(0f, -0.04f, 0.2f); sr = new Vector3(8f, 0f, 0f); kick = 0.6f; break;
                    case GearId.Hammer: wp = new Vector3(0.04f, 0.22f, -0.08f); wr = new Vector3(-75f, 0f, -10f); sp = new Vector3(-0.04f, -0.05f, 0.16f); sr = new Vector3(50f, 0f, 5f); kick = 1.6f; break;
                    case GearId.Wrench: wp = new Vector3(0.02f, 0.04f, -0.04f); wr = new Vector3(0f, 0f, 30f); sp = new Vector3(-0.02f, -0.02f, 0.12f); sr = new Vector3(20f, 0f, -35f); kick = 0.8f; break;
                    default: continue;
                }
                slot.FindPropertyRelative("windupPosition").vector3Value = wp;
                slot.FindPropertyRelative("windupRotation").vector3Value = wr;
                slot.FindPropertyRelative("strikePosition").vector3Value = sp;
                slot.FindPropertyRelative("strikeRotation").vector3Value = sr;
                slot.FindPropertyRelative("kickDegrees").floatValue = kick;
                slot.FindPropertyRelative("twoHanded").boolValue = two;
                changed = true;
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }
        // Cómo queda una herramienta en la mano, en los ejes de la vista: el mango hacia "handle" (del lado del pulgar) y los
        // nudillos hacia "forearm" (la mano agarra en su vacío Agarre_Derecha: su +Y es el mango y su +Z los nudillos, así que
        // el antebrazo, que sale en línea con la mano, apunta más o menos hacia ahí).
        private static Quaternion GripPose(Vector3 handle, Vector3 forearm)
        {
            Vector3 up = handle.normalized;
            return Quaternion.LookRotation(Vector3.ProjectOnPlane(forearm, up).normalized, up);
        }

        private const string ToolsArtFolder = "Assets/_Project/Art/Herramientas";
        private const string ArmsArtPath = "Assets/_Project/Art/Manos/Manos.fbx";

        // El modelo de una herramienta (Art/Herramientas/<nombre>.fbx) dentro de su pivote (el puño). False si no está.
        private static bool ArtTool(Transform pivot, string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ToolsArtFolder}/{name}.fbx");
            if (model == null) return false;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, pivot);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "Modelo";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return true;
        }

        // Una pieza de Manos.fbx (por nombre), suelta, como hija de "parent". Null si el modelo no está.
        private static Transform ArmPiece(string name, Transform parent, bool mirrored)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ArmsArtPath);
            if (model == null) return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform piece = instance.transform.Find(name);
            if (piece != null)
            {
                piece.SetParent(parent, false);
                piece.localPosition = Vector3.zero;
                piece.localRotation = Quaternion.identity;
                piece.localScale = mirrored ? new Vector3(-1f, 1f, 1f) : Vector3.one;
                foreach (Renderer r in piece.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                piece.gameObject.SetActive(false);
            }
            Object.DestroyImmediate(instance);
            return piece;
        }

        // Los dos brazos (piezas de Manos.fbx: con el traje y sin él) y el FirstPersonArms que los acomoda.
        private static void BuildArms(Transform head, Transform hands, ToolBelt belt, Transform[] toolModels, PlayerMotor motor)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ArmsArtPath) == null) return;
            var arms = head.gameObject.AddComponent<FirstPersonArms>();
            SetReference(arms, "belt", belt);
            SetReference(arms, "carrier", motor.GetComponent<SurvivalEnTren.Interaction.PlayerCarrier>());
            SetReference(arms, "gear", motor.GetComponent<PlayerGear>());
            SetReference(arms, "head", head);
            var so = new SerializedObject(arms);
            var models = so.FindProperty("toolModels");
            models.arraySize = toolModels.Length;
            for (int i = 0; i < toolModels.Length; i++) models.GetArrayElementAtIndex(i).objectReferenceValue = toolModels[i];
            string[] variants = { "Campera", "Buzo" };
            string[] gloves = { "Grueso", "Trabajo" };
            foreach (bool left in new[] { false, true })
            {
                var root = Group(left ? "Brazo_Izquierdo" : "Brazo_Derecho", hands);
                var shoulder = Group("Hombro", root);
                shoulder.localPosition = new Vector3(left ? -0.38f : 0.38f, -0.62f, -0.18f); // abajo y atrás: la manga no tapa la vista
                SerializedProperty side = so.FindProperty(left ? "left" : "right");
                side.FindPropertyRelative("shoulder").objectReferenceValue = shoulder;
                side.FindPropertyRelative("isLeft").boolValue = left;
                side.FindPropertyRelative("mirrored").boolValue = left; // la mano del modelo es derecha: la izquierda es la espejada
                foreach (string list in new[] { "upper", "fore", "fist", "open" })
                {
                    SerializedProperty parts = side.FindPropertyRelative(list);
                    parts.arraySize = 2;
                    for (int v = 0; v < 2; v++)
                    {
                        string piece = list == "upper" ? $"Brazo_{variants[v]}" : list == "fore" ? $"Antebrazo_{variants[v]}"
                            : list == "fist" ? $"Mano_{gloves[v]}_Puno" : $"Mano_{gloves[v]}_Abierta";
                        parts.GetArrayElementAtIndex(v).objectReferenceValue = ArmPiece(piece, root, left);
                    }
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
