using System;
using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Los brazos en primera persona (pedido de Tomi del 2026-10-16: "tal vez se podría llegar a ver el brazo"), con el rig que
    /// pasó Tomi ("fps arms", CC0, vestido en Fuentes/Scripts_Manos/brazos_rig.py). Cada cuadro se acomodan los huesos, sin
    /// animaciones hechas a mano:
    /// - Con una herramienta: puño cerrado, fijo al mango (el vacío "Agarre_Derecha" de la herramienta dice dónde está el mango,
    ///   hacia dónde va y hacia dónde miran los nudillos; la mano trae sus huesos "agarre" y "agarre_pulgar" sobre su mango) y el
    ///   antebrazo en línea con la mano, con la muñeca apenas doblada hacia el meñique (Tomi: "sigue agarrando re chueca").
    /// - Cargando una caja o un kit: las dos manos abiertas contra los costados; un bidón: la mano derecha en la manija. Cargando,
    ///   el codo se calcula desde el hombro (IK de dos huesos).
    /// - Con las manos libres no se ven (decisión de Tomi).
    /// - Según el traje (decisión de Tomi): con el traje, manga de campera y guante grueso; sin el traje, buzo y guante de trabajo.
    /// Los dedos toman las poses del modelo ("Puno", "Puno_Grueso", "Abierta"), guardadas al armar la escena.
    /// </summary>
    [DefaultExecutionOrder(60)] // después de ViewmodelRig (50), PlayerLook y de que ToolBelt ponga la herramienta en su lugar
    public class FirstPersonArms : MonoBehaviour
    {
        /// <summary>Los huesos y la ropa de un brazo del rig.</summary>
        [Serializable]
        public class ArmSide
        {
            public bool isLeft;
            [Tooltip("La raíz del brazo (la clavícula): se corre para que el brazo llegue al codo.")]
            public Transform root;
            public Transform upperArm, forearm, hand;
            [Tooltip("Huesos de la mano (no deforman): el centro del mango en el puño, un punto sobre el mango hacia el pulgar y el centro de la palma.")]
            public Transform grip, gripThumb, palm;
            [Tooltip("El nudillo del dedo mayor: de la muñeca hacia acá va el eje de la mano.")]
            public Transform middleKnuckle;
            [Tooltip("Hacia dónde está el hombro (hijo de la raíz de las manos): de ahí sale el brazo.")]
            public Transform shoulder;
            [Tooltip("La ropa de cada variante: 0 = con el traje, 1 = sin el traje.")]
            public Renderer[] sleeves = new Renderer[2];
            public Renderer[] gloves = new Renderer[2];
            [Tooltip("Los huesos de los dedos (los que cambian con las poses).")]
            public Transform[] fingerBones = Array.Empty<Transform>();

            // Medidas del reposo (se toman al arrancar).
            [NonSerialized] public float upperLength, foreLength;
            [NonSerialized] public Quaternion upperRest, foreRelHand, foreRelUpper;
            [NonSerialized] public Vector3 upperAlong, foreAlong;
            [NonSerialized] public Vector3 handleLocal, knuckleLocal, gripOffset, palmOffset, palmNormal, wristAxis, foreInHand;
            [NonSerialized] public bool visible = true;
        }

        /// <summary>Una pose de los dedos: la rotación de cada hueso de los dedos, de cada mano.</summary>
        [Serializable]
        public class FingerPose
        {
            public string name;
            public Quaternion[] right = Array.Empty<Quaternion>();
            public Quaternion[] left = Array.Empty<Quaternion>();
        }

        [SerializeField] private ToolBelt belt;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private PlayerGear gear;
        [Tooltip("La cabeza (la cámara): para orientar las manos al cargar.")]
        [SerializeField] private Transform head;
        [Tooltip("La raíz del rig: las medidas de reposo se toman respecto de ella.")]
        [SerializeField] private Transform rigRoot;
        [Tooltip("Los modelos de cada casilla de la barra (los mismos de ToolBelt): de ahí salen los agarres.")]
        [SerializeField] private Transform[] toolModels = Array.Empty<Transform>();
        [SerializeField] private ArmSide right = new ArmSide();
        [SerializeField] private ArmSide left = new ArmSide { isLeft = true };
        [SerializeField] private FingerPose[] poses = Array.Empty<FingerPose>();
        [Tooltip("Hacia dónde se va el codo cargando, respecto de la cabeza (afuera y abajo), del brazo derecho.")]
        [SerializeField] private Vector3 elbowPole = new Vector3(0.7f, -1f, -0.3f);
        [Tooltip("Cuánto se dobla la muñeca hacia el meñique al agarrar una herramienta (como al sostener un martillo): con 0, el antebrazo sigue derecho a la mano.")]
        [SerializeField, Range(0f, 40f)] private float wristDeviation = 20f;
        [Tooltip("Cuánto sigue el antebrazo el giro de la mano (el resto lo toma el codo): reparte la torsión para que la tela no se retuerza.")]
        [SerializeField, Range(0f, 1f)] private float forearmTwist = 0.6f;
        [Tooltip("Qué tan rápido cambian los dedos de una pose a otra.")]
        [SerializeField, Range(1f, 60f)] private float fingerBlendSpeed = 18f;

        private int _variant = -1;
        private string _pose = "";
        private bool _measured;

        /// <summary>Qué hacen las manos ahora ("", "herramienta", "herramienta dos manos", "caja", "bidón"): para las pruebas.</summary>
        public string Pose => _pose;
        public bool Visible => _pose.Length > 0;
        public int Variant => _variant;

        private void Awake() => Measure();

        // Medidas del reposo, en el rig tal como viene (los huesos del brazo en su lugar y la mano abierta).
        private void Measure()
        {
            if (_measured || rigRoot == null) return;
            foreach (ArmSide s in new[] { right, left })
            {
                if (s.hand == null) continue;
                s.upperLength = Vector3.Distance(s.upperArm.position, s.forearm.position);
                s.foreLength = Vector3.Distance(s.forearm.position, s.hand.position);
                s.upperRest = Quaternion.Inverse(rigRoot.rotation) * s.upperArm.rotation;
                s.upperAlong = Quaternion.Inverse(s.upperArm.rotation) * (s.forearm.position - s.upperArm.position).normalized;
                s.foreAlong = Quaternion.Inverse(s.forearm.rotation) * (s.hand.position - s.forearm.position).normalized;
                s.foreRelHand = Quaternion.Inverse(s.hand.rotation) * s.forearm.rotation;
                s.foreRelUpper = Quaternion.Inverse(s.upperArm.rotation) * s.forearm.rotation;
                Quaternion inv = Quaternion.Inverse(s.hand.rotation);
                s.wristAxis = (inv * (s.middleKnuckle.position - s.hand.position)).normalized;
                s.foreInHand = (inv * (s.hand.position - s.forearm.position)).normalized;
                s.handleLocal = (inv * (s.gripThumb.position - s.grip.position)).normalized;
                s.knuckleLocal = Vector3.ProjectOnPlane(s.wristAxis, s.handleLocal).normalized;
                s.gripOffset = inv * (s.grip.position - s.hand.position);
                s.palmOffset = inv * (s.palm.position - s.hand.position);
                s.palmNormal = Vector3.ProjectOnPlane(s.palmOffset, s.wristAxis).normalized;
            }
            _measured = true;
        }

        private void LateUpdate()
        {
            Measure();
            int variant = gear != null && gear.SuitWorn ? 0 : 1;
            if (variant != _variant)
            {
                _variant = variant;
                HideAll(right); HideAll(left);      // la próxima vez se muestra la ropa del traje que corresponde
            }

            _pose = "";
            Carryable held = carrier != null ? carrier.Holding : null;
            if (held != null) PoseCarrying(held);
            else if (belt != null && belt.Selected >= 0 && belt.Selected < toolModels.Length && toolModels[belt.Selected] != null &&
                     toolModels[belt.Selected].gameObject.activeInHierarchy)
                PoseTool(toolModels[belt.Selected], belt.Settings.Slots[belt.Selected].twoHanded);

            if (_pose.Length == 0) { SetVisible(right, false); SetVisible(left, false); }
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
            else SetVisible(left, false);
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
                SetVisible(left, false);
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
        // y los nudillos hacia "knuckleDir". Con "rigidForearm" el antebrazo sigue a la mano; si no, el codo sale del hombro.
        private void PlaceFist(ArmSide s, Vector3 grip, Vector3 handleDir, Vector3 knuckleDir, bool rigidForearm)
        {
            if (s.hand == null) return;
            ApplyFingers(s, _variant == 0 ? "Puno_Grueso" : "Puno", SetVisible(s, true));
            Vector3 h = handleDir.normalized;
            Vector3 k = Vector3.ProjectOnPlane(knuckleDir, h);
            if (k.sqrMagnitude < 1e-6f) k = Vector3.ProjectOnPlane(head.forward, h);
            Quaternion rot = Quaternion.LookRotation(k.normalized, h) * Quaternion.Inverse(Quaternion.LookRotation(s.knuckleLocal, s.handleLocal));
            Vector3 wrist = grip - rot * s.gripOffset;
            Vector3 along;
            if (rigidForearm)
            {
                // La muñeca apenas doblada hacia el meñique: el antebrazo se inclina hacia el lado del pulgar.
                Vector3 thumbSide = Vector3.ProjectOnPlane(s.handleLocal, s.foreInHand).normalized;
                float dev = wristDeviation * Mathf.Deg2Rad;
                along = rot * (s.foreInHand * Mathf.Cos(dev) + thumbSide * Mathf.Sin(dev));
            }
            else along = (wrist - ElbowIK(s, wrist)).normalized;
            PlaceChain(s, wrist, along, rot);
        }

        // Una mano abierta: el centro de la palma en "palmPoint", la palma mirando hacia "palmDir" y los dedos hacia adelante.
        private void PlaceOpen(ArmSide s, Vector3 palmPoint, Vector3 palmDir)
        {
            if (s.hand == null) return;
            ApplyFingers(s, "Abierta", SetVisible(s, true));
            Vector3 fingers = Vector3.ProjectOnPlane(head.forward, palmDir).normalized;
            Quaternion rot = Quaternion.LookRotation(fingers, palmDir) * Quaternion.Inverse(Quaternion.LookRotation(s.wristAxis, s.palmNormal));
            Vector3 wrist = palmPoint - rot * s.palmOffset;
            PlaceChain(s, wrist, (wrist - ElbowIK(s, wrist)).normalized, rot);
        }

        // El codo con IK de dos huesos desde el hombro, hacia afuera y abajo.
        private Vector3 ElbowIK(ArmSide s, Vector3 wrist)
        {
            Vector3 shoulder = s.shoulder.position;
            float a = s.upperLength, c = s.foreLength;
            Vector3 toWrist = wrist - shoulder;
            float d = toWrist.magnitude;
            if (d > (a + c) * 0.995f) { shoulder = wrist - toWrist.normalized * (a + c) * 0.995f; toWrist = wrist - shoulder; d = toWrist.magnitude; }
            d = Mathf.Max(d, Mathf.Abs(a - c) + 0.01f);
            Vector3 dir = toWrist / d;
            Vector3 pole = head.TransformDirection(s.isLeft ? new Vector3(-elbowPole.x, elbowPole.y, elbowPole.z) : elbowPole);
            Vector3 bend = Vector3.ProjectOnPlane(pole, dir).normalized;
            float cosA = Mathf.Clamp((a * a + d * d - c * c) / (2f * a * d), -1f, 1f);
            return shoulder + dir * (a * cosA) + bend * (a * Mathf.Sqrt(1f - cosA * cosA));
        }

        // Acomoda el brazo entero: el antebrazo hacia "along" terminando en la muñeca, el brazo desde el codo hacia el hombro (la
        // raíz del brazo se corre para que llegue) y la mano con su giro.
        private void PlaceChain(ArmSide s, Vector3 wrist, Vector3 along, Quaternion handRot)
        {
            Vector3 elbow = wrist - along * s.foreLength;
            Vector3 upperDir = elbow - s.shoulder.position;
            upperDir = upperDir.sqrMagnitude > 1e-6f ? upperDir.normalized : -along;
            Vector3 shoulderPos = elbow - upperDir * s.upperLength;
            s.root.position += shoulderPos - s.upperArm.position;
            Quaternion upperRest = rigRoot.rotation * s.upperRest;
            s.upperArm.rotation = Quaternion.FromToRotation(upperRest * s.upperAlong, upperDir) * upperRest;
            // El antebrazo: alineado al codo-muñeca, con su giro repartido entre el del brazo y el de la mano.
            Quaternion fromHand = handRot * s.foreRelHand;
            fromHand = Quaternion.FromToRotation(fromHand * s.foreAlong, along) * fromHand;
            Quaternion fromUpper = s.upperArm.rotation * s.foreRelUpper;
            fromUpper = Quaternion.FromToRotation(fromUpper * s.foreAlong, along) * fromUpper;
            s.forearm.rotation = Quaternion.Slerp(fromUpper, fromHand, forearmTwist);
            s.forearm.rotation = Quaternion.FromToRotation(s.forearm.rotation * s.foreAlong, along) * s.forearm.rotation;
            s.hand.rotation = handRot;
        }

        // Los dedos van hacia la pose (de a poco, o de una vez si el brazo recién aparece).
        private void ApplyFingers(ArmSide s, string poseName, bool snap)
        {
            FingerPose pose = null;
            foreach (FingerPose p in poses) if (p.name == poseName) { pose = p; break; }
            if (pose == null) return;
            Quaternion[] target = s.isLeft ? pose.left : pose.right;
            float t = snap ? 1f : 1f - Mathf.Exp(-fingerBlendSpeed * Time.deltaTime);
            for (int i = 0; i < s.fingerBones.Length && i < target.Length; i++)
                if (s.fingerBones[i] != null) s.fingerBones[i].localRotation = Quaternion.Slerp(s.fingerBones[i].localRotation, target[i], t);
        }

        // ---------------------------------------------------------------- Ayudas

        // Muestra u oculta la ropa de un brazo (la de la variante del traje que corresponde). Devuelve true si recién apareció.
        private bool SetVisible(ArmSide s, bool on)
        {
            if (s.visible == on) return false;
            s.visible = on;
            int v = Mathf.Clamp(_variant, 0, 1);
            for (int i = 0; i < 2; i++)
            {
                if (i < s.sleeves.Length && s.sleeves[i] != null) s.sleeves[i].enabled = on && i == v;
                if (i < s.gloves.Length && s.gloves[i] != null) s.gloves[i].enabled = on && i == v;
            }
            return on;
        }

        private static void HideAll(ArmSide s)
        {
            s.visible = false;
            foreach (Renderer r in s.sleeves) if (r != null) r.enabled = false;
            foreach (Renderer r in s.gloves) if (r != null) r.enabled = false;
        }

        private static Bounds BoundsOf(Transform t)
        {
            Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(t.position, Vector3.one * 0.3f);
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return b;
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
