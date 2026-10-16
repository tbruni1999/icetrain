using System;
using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Los brazos en primera persona (pedido de Tomi del 2026-10-16: "tal vez se podría llegar a ver el brazo"). Son piezas
    /// rígidas (brazo, antebrazo y mano) que se acomodan solas cada cuadro, sin animaciones:
    /// - La mano se pone donde está el mango de lo que se lleva (el vacío "Agarre_Derecha"/"Agarre_Izquierda" de la
    ///   herramienta) y el codo se calcula para que el brazo llegue desde el hombro (IK de dos huesos).
    /// - Con una herramienta en la mano: puño cerrado; la barreta, con las dos manos.
    /// - Cargando una caja o un kit: las dos manos abiertas a los costados; un bidón: la mano derecha en la manija.
    /// - Con las manos libres no se ven (decisión de Tomi).
    /// - Según el traje (decisión de Tomi): con el traje, campera de trabajo y guante grueso; sin el traje, buzo y guante de cuero.
    /// La mano del modelo es una izquierda: la derecha es la misma espejada.
    /// </summary>
    [DefaultExecutionOrder(60)] // después de ViewmodelRig (50), PlayerLook y de que ToolBelt ponga la herramienta en su lugar
    public class FirstPersonArms : MonoBehaviour
    {
        /// <summary>Las piezas de un brazo: una por variante (0 = con el traje, 1 = sin el traje).</summary>
        [Serializable]
        public class ArmSide
        {
            [Tooltip("El hombro (hijo de la raíz de las manos): desde acá sale el brazo.")]
            public Transform shoulder;
            [Tooltip("Es el brazo izquierdo (el codo va para el otro lado).")]
            public bool isLeft;
            [Tooltip("Las piezas están espejadas (el modelo de la mano es una mano izquierda: la derecha es la espejada).")]
            public bool mirrored;
            public Transform[] upper = new Transform[2];
            public Transform[] fore = new Transform[2];
            public Transform[] fist = new Transform[2];
            public Transform[] open = new Transform[2];
        }

        [SerializeField] private ToolBelt belt;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private PlayerGear gear;
        [Tooltip("La cabeza (la cámara): para orientar las manos al cargar.")]
        [SerializeField] private Transform head;
        [Tooltip("Los modelos de cada casilla de la barra (los mismos de ToolBelt): de ahí salen los agarres.")]
        [SerializeField] private Transform[] toolModels = Array.Empty<Transform>();
        [SerializeField] private ArmSide right = new ArmSide();
        [SerializeField] private ArmSide left = new ArmSide { isLeft = true };
        [Header("Medidas (las del modelo)")]
        [SerializeField, Range(0.1f, 0.5f)] private float upperLength = 0.29f;
        [SerializeField, Range(0.1f, 0.5f)] private float foreLength = 0.26f;
        [Tooltip("Hacia dónde se va el codo, respecto de la cabeza (afuera y abajo), del brazo derecho.")]
        [SerializeField] private Vector3 elbowPole = new Vector3(0.7f, -1f, -0.3f);
        [Tooltip("Hacia dónde apunta el largo de cada pieza en su modelo (el FBX deja el largo en Y).")]
        [SerializeField] private Vector3 lengthAxis = Vector3.up;

        private int _variant = -1;
        private string _pose = "";

        /// <summary>Qué hacen las manos ahora ("", "herramienta", "herramienta dos manos", "caja", "bidón"): para las pruebas.</summary>
        public string Pose => _pose;
        public bool Visible => _pose.Length > 0;
        public int Variant => _variant;

        private void LateUpdate()
        {
            int variant = gear != null && gear.SuitWorn ? 0 : 1;
            if (variant != _variant)
            {
                _variant = variant;
                ShowVariant(right);
                ShowVariant(left);
            }

            _pose = "";
            Carryable held = carrier != null ? carrier.Holding : null;
            if (held != null) PoseCarrying(held);
            else if (belt != null && belt.Selected >= 0 && belt.Selected < toolModels.Length && toolModels[belt.Selected] != null &&
                     toolModels[belt.Selected].gameObject.activeInHierarchy)
                PoseTool(toolModels[belt.Selected], belt.Settings.Slots[belt.Selected].twoHanded);

            if (_pose.Length == 0) { Hide(right); Hide(left); }
        }

        // ---------------------------------------------------------------- Poses

        private void PoseTool(Transform tool, bool twoHanded)
        {
            Transform gripR = FindDeep(tool, "Agarre_Derecha");
            if (gripR == null) return;
            Vector3 toolUp = tool.up;
            Place(right, true, gripR.position, toolUp, null);
            Transform gripL = twoHanded ? FindDeep(tool, "Agarre_Izquierda") : null;
            if (gripL != null) Place(left, true, gripL.position, toolUp, null);
            else Hide(left);
            _pose = gripL != null ? "herramienta dos manos" : "herramienta";
        }

        private void PoseCarrying(Carryable held)
        {
            Bounds b = BoundsOf(held.transform);
            Vector3 headRight = head.right;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (held.OneHanded)
            {
                // Un bidón: la mano derecha en la manija (arriba), puño cerrado; el mango cruza de adelante hacia atrás.
                Vector3 top = new Vector3(b.center.x, b.max.y - 0.02f, b.center.z);
                Place(right, true, top, -forward, null);
                Hide(left);
                _pose = "bidón";
                return;
            }
            // Una caja o un kit: las palmas contra los costados, a media altura y un poco hacia el jugador.
            Vector3 toPlayer = -forward;
            float halfWidth = Mathf.Max(0.12f, Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(headRight.x), Mathf.Abs(headRight.y), Mathf.Abs(headRight.z)))));
            Vector3 middle = b.center + toPlayer * Mathf.Min(0.12f, b.extents.magnitude * 0.3f);
            Place(right, false, middle + headRight * halfWidth, Vector3.up, -headRight);
            Place(left, false, middle - headRight * halfWidth, Vector3.up, headRight);
            _pose = "caja";
        }

        // Pone una mano con su agarre en "grip" y acomoda el brazo con el codo hacia afuera y abajo.
        // Puño: el pulgar hacia "thumbDir" (a lo largo del mango). Abierta: la palma mirando hacia "palmDir".
        private void Place(ArmSide side, bool fist, Vector3 grip, Vector3 thumbDir, Vector3? palmDir)
        {
            int v = Mathf.Clamp(_variant, 0, 1);
            Transform hand = fist ? side.fist[v] : side.open[v];
            Transform other = fist ? side.open[v] : side.fist[v];
            if (hand == null) return;
            if (other != null && other.gameObject.activeSelf) other.gameObject.SetActive(false);
            if (!hand.gameObject.activeSelf) hand.gameObject.SetActive(true);
            Vector3 shoulder = side.shoulder.position;

            // Orientación de la mano. En el modelo (ya en Unity): los dedos hacia +Y, el dorso hacia +Z, la palma hacia -Z y el
            // pulgar del lado -X. Puño: los dedos lejos del hombro y el pulgar a lo largo del mango. Abierta: la palma hacia el objeto.
            Quaternion rot;
            if (fist)
            {
                Vector3 fingers = Vector3.ProjectOnPlane(grip - shoulder, thumbDir).normalized;
                if (fingers.sqrMagnitude < 1e-4f) fingers = head.forward;
                // El espejo (brazo izquierdo) da vuelta el lado del pulgar: el dorso sale del otro producto.
                Vector3 back = side.mirrored ? Vector3.Cross(thumbDir, fingers) : Vector3.Cross(fingers, thumbDir);
                rot = Quaternion.LookRotation(back.normalized, fingers);
            }
            else
            {
                Vector3 palm = palmDir ?? -head.right;
                Vector3 fingers = Vector3.ProjectOnPlane(head.forward, palm).normalized;
                rot = Quaternion.LookRotation(-palm, fingers);                  // el dorso hacia afuera, los dedos hacia adelante
            }
            Transform anchor = FindGrip(hand);
            Vector3 gripLocal = anchor != null ? Vector3.Scale(anchor.localPosition, hand.localScale) : Vector3.zero;
            Vector3 wrist = grip - rot * gripLocal;
            hand.SetPositionAndRotation(wrist, rot);

            // El codo (IK de dos huesos): si no llega, el hombro se acerca (está fuera de la vista).
            float a = upperLength, c = foreLength;
            Vector3 toWrist = wrist - shoulder;
            float d = toWrist.magnitude;
            if (d > (a + c) * 0.995f) { shoulder = wrist - toWrist.normalized * (a + c) * 0.995f; toWrist = wrist - shoulder; d = toWrist.magnitude; }
            d = Mathf.Max(d, Mathf.Abs(a - c) + 0.01f);
            Vector3 dir = toWrist / d;
            Vector3 pole = head.TransformDirection(side.isLeft ? new Vector3(-elbowPole.x, elbowPole.y, elbowPole.z) : elbowPole);
            Vector3 bend = Vector3.ProjectOnPlane(pole, dir).normalized;
            float cosA = Mathf.Clamp((a * a + d * d - c * c) / (2f * a * d), -1f, 1f);
            Vector3 elbow = shoulder + dir * (a * cosA) + bend * (a * Mathf.Sqrt(1f - cosA * cosA));

            Transform upper = side.upper[v], fore = side.fore[v];
            if (upper != null) { upper.gameObject.SetActive(true); Align(upper, shoulder, elbow, bend); }
            if (fore != null) { fore.gameObject.SetActive(true); Align(fore, elbow, wrist, rot * Vector3.up); }
        }

        // Pone una pieza desde "from" hasta "to" (su largo en lengthAxis), girada para que "up" quede arriba.
        private void Align(Transform piece, Vector3 from, Vector3 to, Vector3 up)
        {
            Vector3 along = (to - from).normalized;
            Quaternion look = Quaternion.LookRotation(along, up);
            Quaternion axisFix = Quaternion.FromToRotation(lengthAxis, Vector3.forward); // del eje del modelo al "adelante" de LookRotation
            piece.SetPositionAndRotation(from, look * axisFix);
        }

        // ---------------------------------------------------------------- Ayudas

        private void ShowVariant(ArmSide side)
        {
            for (int v = 0; v < 2; v++)
            {
                foreach (Transform[] parts in new[] { side.upper, side.fore, side.fist, side.open })
                    if (parts != null && v < parts.Length && parts[v] != null) parts[v].gameObject.SetActive(false);
            }
        }

        private void Hide(ArmSide side) => ShowVariant(side);

        private static Bounds BoundsOf(Transform t)
        {
            Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(t.position, Vector3.one * 0.3f);
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // El vacío "Agarre" de una mano (Blender les pone .001, .002... a los repetidos).
        private static Transform FindGrip(Transform hand)
        {
            foreach (Transform child in hand)
                if (child.name == "Agarre" || child.name.StartsWith("Agarre.")) return child;
            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
