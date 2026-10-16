using System;
using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Los brazos en primera persona (pedido de Tomi del 2026-10-16: "tal vez se podría llegar a ver el brazo"). Son piezas
    /// rígidas (brazo, antebrazo y mano) que se acomodan solas cada cuadro, sin animaciones:
    /// - Con una herramienta: puño cerrado, fijo al mango (el vacío "Agarre_Derecha" de la herramienta dice dónde está el
    ///   mango, hacia dónde va y hacia dónde miran los nudillos) y el antebrazo en línea con la mano, como en los juegos en
    ///   primera persona (Tomi, 2026-10-16: "sigue agarrando re chueca la barreta"). Todas se agarran con una mano.
    /// - Cargando una caja o un kit: las dos manos abiertas a los costados; un bidón: la mano derecha en la manija.
    /// - Con las manos libres no se ven (decisión de Tomi).
    /// - Cargando: el codo se calcula para que el brazo llegue desde el hombro (IK de dos huesos).
    /// - Según el traje (decisión de Tomi): con el traje, campera de trabajo y guante grueso; sin el traje, buzo y guante de
    ///   trabajo.
    /// La mano del modelo es una derecha: la izquierda es la misma espejada.
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
            [Tooltip("Las piezas están espejadas (el modelo de la mano es una mano derecha: la izquierda es la espejada).")]
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
        [Tooltip("La mano del modelo es una derecha. Solo se usa si el puño no trae el vacío Agarre_Pulgar (con ese vacío, la dirección del mango sale del modelo).")]
        [SerializeField] private bool rightHandModel = true;
        [Tooltip("Cuánto se dobla la muñeca hacia el meñique al agarrar una herramienta (como al sostener un martillo): con 0, el antebrazo sigue derecho a la mano.")]
        [SerializeField, Range(0f, 40f)] private float wristDeviation = 20f;

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

        // El agarre queda fijo a la herramienta (como en los juegos en primera persona): el vacío del mango dice hacia dónde va
        // el mango (su +Y, donde va el pulgar) y hacia dónde miran los nudillos (su +Z). El antebrazo sale en línea con la mano.
        private void PoseTool(Transform tool, bool twoHanded)
        {
            Transform gripR = FindDeep(tool, "Agarre_Derecha");
            if (gripR == null) return;
            PlaceFist(right, gripR.position, gripR.up, gripR.forward, true);
            Transform gripL = twoHanded ? FindDeep(tool, "Agarre_Izquierda") : null;
            if (gripL != null) PlaceFist(left, gripL.position, gripL.up, gripL.forward, true);
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
                // Un bidón: la mano derecha en la manija (arriba), puño cerrado y de arriba: la manija cruza de adelante hacia
                // atrás, el pulgar hacia el jugador, los nudillos hacia el centro y el dorso hacia arriba.
                Vector3 top = new Vector3(b.center.x, b.max.y - 0.02f, b.center.z);
                PlaceFist(right, top, -forward, -headRight, false);
                Hide(left);
                _pose = "bidón";
                return;
            }
            // Una caja o un kit: las palmas contra los costados, a media altura y un poco hacia el jugador.
            Vector3 toPlayer = -forward;
            float halfWidth = Mathf.Max(0.12f, Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(headRight.x), Mathf.Abs(headRight.y), Mathf.Abs(headRight.z)))));
            Vector3 middle = b.center + toPlayer * Mathf.Min(0.12f, b.extents.magnitude * 0.3f);
            PlaceOpen(right, middle + headRight * halfWidth, -headRight);
            PlaceOpen(left, middle - headRight * halfWidth, headRight);
            _pose = "caja";
        }

        // Un puño cerrado alrededor de un mango: el centro del mango en "grip", el mango hacia "handleDir" (del lado del pulgar)
        // y los nudillos hacia "knuckleDir". La mano del modelo trae dos vacíos sobre su mango ("Agarre" y "Agarre_Pulgar"):
        // de ahí sale cómo cruza el mango la palma (en diagonal, como en una mano de verdad). Con "rigidForearm" el antebrazo
        // sigue a la mano (la muñeca no se quiebra); si no, el codo se calcula desde el hombro.
        private void PlaceFist(ArmSide side, Vector3 grip, Vector3 handleDir, Vector3 knuckleDir, bool rigidForearm)
        {
            Transform hand = ShowHand(side, true);
            if (hand == null) return;
            Transform anchor = FindGrip(hand, "Agarre");
            Transform thumb = FindGrip(hand, "Agarre_Pulgar");
            float mirror = side.mirrored ? -1f : 1f;
            Vector3 scale = hand.localScale;
            // El mango y los nudillos en el modelo (en las medidas de la mano ya espejada, si es la izquierda).
            Vector3 handleModel = anchor != null && thumb != null
                ? Vector3.Scale(thumb.localPosition - anchor.localPosition, scale).normalized
                : new Vector3((rightHandModel ? 1f : -1f) * mirror, 0f, 0f);
            Vector3 knuckleModel = Vector3.ProjectOnPlane(Vector3.up, handleModel).normalized;
            Vector3 handle = handleDir.normalized;
            Vector3 knuckles = Vector3.ProjectOnPlane(knuckleDir, handle);
            if (knuckles.sqrMagnitude < 1e-6f) knuckles = Vector3.ProjectOnPlane(head.forward, handle);
            Quaternion rot = Quaternion.LookRotation(knuckles.normalized, handle) *
                             Quaternion.Inverse(Quaternion.LookRotation(knuckleModel, handleModel));
            Vector3 wrist = PutHand(hand, anchor, grip, rot);
            // La muñeca apenas doblada hacia el meñique: el antebrazo se inclina hacia el lado del pulgar.
            Vector3 thumbSide = Vector3.ProjectOnPlane(handleModel, Vector3.up).normalized;
            float dev = wristDeviation * Mathf.Deg2Rad;
            Vector3 forearmModel = Vector3.up * Mathf.Cos(dev) + thumbSide * Mathf.Sin(dev);
            if (rigidForearm) ArmInLine(side, wrist, rot, rot * forearmModel);
            else ArmIK(side, wrist, rot);
        }

        // Una mano abierta: el centro de la palma en "grip", la palma mirando hacia "palmDir" y los dedos hacia adelante.
        private void PlaceOpen(ArmSide side, Vector3 grip, Vector3 palmDir)
        {
            Transform hand = ShowHand(side, false);
            if (hand == null) return;
            Vector3 fingers = Vector3.ProjectOnPlane(head.forward, palmDir).normalized;
            Quaternion rot = Quaternion.LookRotation(-palmDir, fingers);    // en el modelo: los dedos +Y, el dorso +Z
            Vector3 wrist = PutHand(hand, FindGrip(hand, "Agarre"), grip, rot);
            ArmIK(side, wrist, rot);
        }

        private Transform ShowHand(ArmSide side, bool fist)
        {
            int v = Mathf.Clamp(_variant, 0, 1);
            Transform hand = fist ? side.fist[v] : side.open[v];
            Transform other = fist ? side.open[v] : side.fist[v];
            if (hand == null) return null;
            if (other != null && other.gameObject.activeSelf) other.gameObject.SetActive(false);
            if (!hand.gameObject.activeSelf) hand.gameObject.SetActive(true);
            return hand;
        }

        // Pone la mano para que su vacío de agarre caiga en "grip". Devuelve dónde quedó la muñeca (el origen de la mano).
        private static Vector3 PutHand(Transform hand, Transform anchor, Vector3 grip, Quaternion rot)
        {
            Vector3 gripLocal = anchor != null ? Vector3.Scale(anchor.localPosition, hand.localScale) : Vector3.zero;
            Vector3 wrist = grip - rot * gripLocal;
            hand.SetPositionAndRotation(wrist, rot);
            return wrist;
        }

        // El antebrazo sigue a la mano (hacia "along", casi el eje de la muñeca del modelo, +Y); el brazo va del codo hacia el
        // hombro.
        private void ArmInLine(ArmSide side, Vector3 wrist, Quaternion rot, Vector3 along)
        {
            int v = Mathf.Clamp(_variant, 0, 1);
            Vector3 back = rot * Vector3.forward;
            Vector3 elbow = wrist - along * foreLength;
            Vector3 toShoulder = side.shoulder.position - elbow;
            if (toShoulder.sqrMagnitude < 1e-6f) toShoulder = -along;
            Vector3 shoulder = elbow + toShoulder.normalized * upperLength;
            Transform upper = side.upper[v], fore = side.fore[v];
            if (upper != null) { upper.gameObject.SetActive(true); Align(upper, shoulder, elbow, back); }
            if (fore != null) { fore.gameObject.SetActive(true); Align(fore, elbow, wrist, back); }
        }

        // El codo con IK de dos huesos desde el hombro, hacia afuera y abajo (cargando: la muñeca se dobla lo que haga falta).
        private void ArmIK(ArmSide side, Vector3 wrist, Quaternion rot)
        {
            int v = Mathf.Clamp(_variant, 0, 1);
            Vector3 shoulder = side.shoulder.position;
            // Si no llega, el hombro se acerca (está fuera de la vista).
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
            if (fore != null) { fore.gameObject.SetActive(true); Align(fore, elbow, wrist, rot * Vector3.forward); }
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

        // Un vacío de la mano por nombre ("Agarre", "Agarre_Pulgar"; Blender les pone .001, .002... a los repetidos).
        private static Transform FindGrip(Transform hand, string name)
        {
            foreach (Transform child in hand)
                if (child.name == name || child.name.StartsWith(name + ".")) return child;
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
