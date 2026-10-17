using System.Collections.Generic;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Player;
using SurvivalEnTren.Train;
using SurvivalEnTren.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Building
{
    /// <summary>
    /// Colocar muebles donde el jugador quiere (decisión de Tomi del 2026-10-16: "que aparezca como lo que va a ocupar y yo
    /// ponerla como quiera", y "una herramienta que te deje moverla del lugar donde la pusiste").
    /// - Con el kit de un mueble en la mano se ve el mueble como va a quedar: claro si entra, rojo si no. Es la única vista
    ///   previa del juego y aparece solo mientras se está colocando algo.
    /// - Mirando el piso, el mueble se para ahí, con el frente hacia el jugador; la rueda del mouse lo gira.
    ///   Mirando una pared, se apoya contra ella (la espalda contra la pared).
    /// - Clic: se instala. Solo adentro del tren (PROPUESTA: el tren es la casa) y sin chocar con nada.
    /// - Con la llave de montaje en la mano, un golpe a un mueble colocado lo levanta (con lo que tenga guardado) y se coloca
    ///   igual; cambiar de herramienta lo devuelve a donde estaba.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[16];
        private static readonly Collider[] Overlaps = new Collider[32];

        [SerializeField] private Transform head;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private ToolBelt belt;
        [Tooltip("El clic izquierdo (el mismo que golpea con una herramienta).")]
        [SerializeField] private InputActionReference use;
        [Tooltip("La rueda del mouse (gira el mueble mientras se coloca).")]
        [SerializeField] private InputActionReference scroll;
        [Tooltip("Material de la vista previa cuando el mueble entra.")]
        [SerializeField] private Material okMaterial;
        [Tooltip("Material de la vista previa cuando no entra.")]
        [SerializeField] private Material badMaterial;
        [Tooltip("Hasta dónde se puede colocar, desde los ojos (metros).")]
        [SerializeField, Range(1.5f, 5f)] private float reachMeters = 3.2f;
        [Tooltip("Cuántos grados gira cada paso de la rueda.")]
        [SerializeField, Range(1f, 90f)] private float rotateStepDegrees = 15f;
        [Tooltip("Separación que se deja contra una pared (metros).")]
        [SerializeField, Range(0f, 0.1f)] private float wallGap = 0.01f;
        [Tooltip("Cuánto más alto o más bajo que los pies del jugador puede estar el piso donde se apoya (metros): no se coloca encima de un mueble.")]
        [SerializeField, Range(0.05f, 1f)] private float maxStepMeters = 0.25f;

        private sealed class Look
        {
            public Renderer Renderer;
            public Material[] Materials;
            public UnityEngine.Rendering.ShadowCastingMode Shadows;
        }

        private GameObject _target;          // el mueble que se está colocando (vista previa o el que se mueve)
        private Placeable _placeable;
        private UpgradeKit _kit;             // si viene de un kit
        private bool _moving;                // si es un mueble que ya estaba colocado
        private Transform _originalParent;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private readonly List<Look> _looks = new List<Look>();
        private readonly List<Collider> _disabled = new List<Collider>();
        private readonly Dictionary<GameObject, int> _layers = new Dictionary<GameObject, int>();
        private float _yaw;
        private bool _valid;
        private bool _hasPose;
        private string _reason = "";
        private Transform _trainForPose;
        private int _beganFrame = -1;
        private bool _swallowClick;          // el clic que colocó no debe además golpear con la herramienta
        private AudioSource _audio;
        private AudioClip _placeClip, _badClip;
        private GUIStyle _style;

        /// <summary>Se está colocando algo (desde un kit o moviéndolo con la llave).</summary>
        public bool Active => _target != null;
        public bool IsMoving => _moving && _target != null;
        /// <summary>La barra de herramientas no golpea ni gira: se está colocando algo, o sigue apretado el clic que lo colocó.</summary>
        public bool BlocksTool => Active || _swallowClick;
        /// <summary>El mueble entra donde se está mirando.</summary>
        public bool Valid => _valid;
        public string Reason => _reason;
        public GameObject Preview => _target;
        /// <summary>Cuántos grados se giró con la rueda.</summary>
        public float RotationDegrees => _yaw;

        /// <summary>Se dispara al instalar o mover un mueble (el mueble colocado).</summary>
        public event System.Action<Placeable> Placed;

        private void Awake()
        {
            useGUILayout = false;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _placeClip = SurvivalEnTren.Audio.ProceduralClips.Clank("Colocar", 260f, 0.3f, 307);
            _badClip = SurvivalEnTren.Audio.ProceduralClips.Thud("Colocar_No", 0.15f, 311);
        }

        private void OnEnable()
        {
            use.action.Enable();
            scroll.action.Enable();
        }

        private void OnDisable() => CancelAll();

        // ---------------------------------------------------------------- Empezar y terminar

        /// <summary>Levanta un mueble colocado para moverlo (lo llama el golpe de la llave). Devuelve si empezó.</summary>
        public bool BeginMove(Placeable placeable)
        {
            if (Active || placeable == null) return false;
            _moving = true;
            _placeable = placeable;
            _target = placeable.gameObject;
            _originalParent = _target.transform.parent;
            _originalPosition = _target.transform.localPosition; // relativo al tren: el tren puede moverse mientras tanto
            _originalRotation = _target.transform.localRotation;
            _yaw = Mathf.DeltaAngle(FacingYaw(), _target.transform.eulerAngles.y); // sigue girado como estaba
            MakePreview(_target);
            _beganFrame = Time.frameCount;
            return true;
        }

        private void BeginFromKit(UpgradeKit kit)
        {
            _kit = kit;
            _moving = false;
            _target = Instantiate(kit.PlaceablePrefab);
            _target.name = kit.PlaceablePrefab.name;
            _placeable = _target.GetComponent<Placeable>();
            _yaw = 0f;
            MakePreview(_target);
            _beganFrame = Time.frameCount;
        }

        /// <summary>Instala (o deja) el mueble donde se ve, si entra. Devuelve si se colocó.</summary>
        public bool Confirm()
        {
            if (!Active) return false;
            if (!_valid || !_hasPose)
            {
                _audio.PlayOneShot(_badClip, 0.5f);
                return false;
            }
            GameObject placed = _target;
            Placeable placeable = _placeable;
            RestoreLook();
            if (_trainForPose != null) placed.transform.SetParent(_trainForPose, true);
            if (!_moving)
            {
                placeable.AssignId(Placeable.NewId());
                UpgradeKit kit = _kit;
                carrier.Drop(throwIt: false);
                Destroy(kit.gameObject);
            }
            _swallowClick = true;
            ClearState();
            _audio.PlayOneShot(_placeClip, 0.7f);
            Placed?.Invoke(placeable);
            return true;
        }

        /// <summary>Deja de colocar: la vista previa de un kit desaparece; un mueble que se movía vuelve a su lugar.</summary>
        public void CancelAll()
        {
            if (!Active) return;
            if (_moving)
            {
                RestoreLook();
                _target.transform.SetParent(_originalParent, false);
                _target.transform.localPosition = _originalPosition;
                _target.transform.localRotation = _originalRotation;
            }
            else Destroy(_target);
            ClearState();
        }

        private void ClearState()
        {
            _target = null;
            _placeable = null;
            _kit = null;
            _moving = false;
            _valid = false;
            _hasPose = false;
            _reason = "";
            _looks.Clear();
            _disabled.Clear();
            _layers.Clear();
        }

        // ---------------------------------------------------------------- Cada cuadro

        private void Update()
        {
            if (_swallowClick && !use.action.IsPressed()) _swallowClick = false;
            Carryable held = carrier != null ? carrier.Holding : null;
            UpgradeKit kit = held != null ? held.GetComponent<UpgradeKit>() : null;
            bool kitInHand = kit != null && kit.Kind == KitKind.Placeable && kit.PlaceablePrefab != null;

            if (_moving)
            {
                // Mover sigue mientras la llave esté en la mano y las manos libres.
                if (belt == null || belt.ActiveTool != GearId.Wrench || held != null) CancelAll();
            }
            else if (Active && (!kitInHand || kit != _kit)) CancelAll();
            if (!Active && kitInHand) BeginFromKit(kit);
            if (!Active) return;

            if (Time.timeScale <= 0f || motor.MovementLocked) return; // pausa, un panel abierto
            float wheel = scroll.action.ReadValue<float>();
            if (wheel > 0.01f) _yaw += rotateStepDegrees;
            else if (wheel < -0.01f) _yaw -= rotateStepDegrees;
            UpdatePose();
            if (use.action.WasPressedThisFrame() && Time.frameCount != _beganFrame) Confirm();
        }

        private void LateUpdate()
        {
            if (Active && Time.timeScale > 0f) UpdatePose(); // el tren se movió en la física: la vista previa lo sigue
        }

        // Hacia dónde queda el frente del mueble mirando el piso: hacia el jugador.
        private float FacingYaw()
        {
            Vector3 f = head.forward;
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg + 180f;
        }

        private void UpdatePose()
        {
            Vector3 size = _placeable.FootprintSize;
            Vector3 center = _placeable.FootprintCenter;
            Vector3 pos;
            Quaternion rot;
            Collider floor = null;
            _hasPose = false;

            if (TryRay(head.position, head.forward, reachMeters, out RaycastHit hit) && hit.normal.y > 0.7f)
            {
                pos = hit.point;
                rot = Quaternion.Euler(0f, FacingYaw() + _yaw, 0f);
                floor = hit.collider;
            }
            else if (hit.collider != null && Mathf.Abs(hit.normal.y) < 0.35f)
            {
                // Contra una pared: la espalda contra ella, de frente hacia el jugador.
                Vector3 n = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
                rot = Quaternion.LookRotation(n, Vector3.up);
                pos = hit.point + n * (size.z * 0.5f - center.z + wallGap);
                if (TryRay(pos + Vector3.up * 1.2f, Vector3.down, 3f, out RaycastHit down) && down.normal.y > 0.7f)
                {
                    pos.y = down.point.y;
                    floor = down.collider;
                }
            }
            else
            {
                // Al aire: adelante, a la distancia de alcance, sobre lo que haya abajo.
                pos = head.position + head.forward * reachMeters;
                rot = Quaternion.Euler(0f, FacingYaw() + _yaw, 0f);
                if (TryRay(pos, Vector3.down, 3f, out RaycastHit down) && down.normal.y > 0.7f)
                {
                    pos = down.point;
                    floor = down.collider;
                }
            }

            _target.transform.SetPositionAndRotation(pos, rot);
            TrainMover mover = floor != null ? floor.GetComponentInParent<TrainMover>() : null;
            _trainForPose = mover != null ? mover.transform : null;
            _hasPose = floor != null;
            if (floor == null) { _valid = false; _reason = "mirá el piso o una pared"; }
            else if (Mathf.Abs(pos.y - motor.transform.position.y) > maxStepMeters) { _valid = false; _reason = "va en el piso, no encima de otra cosa"; }
            else if (_trainForPose == null) { _valid = false; _reason = "solo adentro del tren"; }
            else if (!FootprintFree(pos, rot, size, center)) { _valid = false; _reason = "no entra: choca con algo"; }
            else if (Reserved(pos, rot, size, center, out string why)) { _valid = false; _reason = why; }
            else { _valid = true; _reason = ""; }
            SetLook(_valid ? okMaterial : badMaterial);
        }

        // Lo primero sólido en esa dirección, sin contar al jugador, lo que lleva en la mano ni el mueble que se coloca.
        private bool TryRay(Vector3 from, Vector3 direction, float distance, out RaycastHit best)
        {
            best = default;
            int count = Physics.RaycastNonAlloc(from, direction, Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider c = Hits[i].collider;
                if (Ignored(c)) continue;
                if (Hits[i].distance < nearest) { nearest = Hits[i].distance; best = Hits[i]; }
            }
            return nearest < float.MaxValue;
        }

        private bool Ignored(Collider c)
        {
            if (c.transform.IsChildOf(transform)) return true;
            if (_target != null && c.transform.IsChildOf(_target.transform)) return true;
            Carryable held = carrier != null ? carrier.Holding : null;
            return held != null && c.transform.IsChildOf(held.transform);
        }

        // ¿El lugar que ocupa el mueble está libre? (Un poco más chico y un poco levantado, para no tocar el piso ni rozar.)
        private bool FootprintFree(Vector3 pos, Quaternion rot, Vector3 size, Vector3 center)
        {
            Vector3 middle = pos + rot * center + Vector3.up * 0.03f;
            Vector3 half = Vector3.Max(size * 0.5f - Vector3.one * 0.025f, Vector3.one * 0.01f);
            int count = Physics.OverlapBoxNonAlloc(middle, half, Overlaps, rot, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = Overlaps[i];
                if (_target != null && c.transform.IsChildOf(_target.transform)) continue;
                Carryable held = carrier != null ? carrier.Holding : null;
                if (held != null && c.transform.IsChildOf(held.transform)) continue;
                return false; // también el jugador: no se coloca encima de uno mismo
            }
            return true;
        }

        // ¿Pisa un lugar reservado? (Donde el banco deja los kits, el lugar de una obra que todavía no se ve.)
        private bool Reserved(Vector3 pos, Quaternion rot, Vector3 size, Vector3 center, out string why)
        {
            why = "";
            Vector3 middle = pos + rot * center + Vector3.up * 0.03f;
            Vector3 half = Vector3.Max(size * 0.5f - Vector3.one * 0.025f, Vector3.one * 0.01f);
            int count = Physics.OverlapBoxNonAlloc(middle, half, Overlaps, rot, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider c = Overlaps[i];
                if (!c.isTrigger || (_target != null && c.transform.IsChildOf(_target.transform))) continue;
                var blocker = c.GetComponentInParent<PlacementBlocker>();
                if (blocker != null) { why = blocker.Reason; return true; }
                var site = c.GetComponentInParent<BuildSite>();
                if (site != null) { why = $"ahí va la {site.DisplayName}"; return true; }
            }
            return false;
        }

        // ---------------------------------------------------------------- Cómo se ve mientras se coloca

        private void MakePreview(GameObject target)
        {
            _looks.Clear();
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
                _looks.Add(new Look { Renderer = r, Materials = r.sharedMaterials, Shadows = r.shadowCastingMode });
            _disabled.Clear();
            foreach (Collider c in target.GetComponentsInChildren<Collider>(true))
                if (c.enabled) { c.enabled = false; _disabled.Add(c); }
            _layers.Clear();
            foreach (Transform t in target.GetComponentsInChildren<Transform>(true))
            {
                _layers[t.gameObject] = t.gameObject.layer;
                t.gameObject.layer = 2; // Ignore Raycast: no tapa la mira
            }
            foreach (Rigidbody body in target.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        }

        private void SetLook(Material material)
        {
            if (material == null) return;
            foreach (Look look in _looks)
            {
                if (look.Renderer == null) continue;
                var mats = new Material[look.Materials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                look.Renderer.sharedMaterials = mats;
                look.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void RestoreLook()
        {
            foreach (Look look in _looks)
            {
                if (look.Renderer == null) continue;
                look.Renderer.sharedMaterials = look.Materials;
                look.Renderer.shadowCastingMode = look.Shadows;
            }
            foreach (Collider c in _disabled) if (c != null) c.enabled = true;
            foreach (KeyValuePair<GameObject, int> pair in _layers) if (pair.Key != null) pair.Key.layer = pair.Value;
        }

        // ---------------------------------------------------------------- Indicación

        private void OnGUI()
        {
            Matrix4x4 uiOld = UiScale.Begin(); // la interfaz en puntos, agrandada según la pantalla
            DrawGui();
            GUI.matrix = uiOld;
        }

        private void DrawGui()
        {
            if (!Active || Time.timeScale <= 0f) return;
            _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 16 };
            string text = _moving ? "clic: dejar acá   ·   rueda: girar   ·   otra herramienta: cancelar" : "";
            if (!_valid && !string.IsNullOrEmpty(_reason)) text = string.IsNullOrEmpty(text) ? _reason : _reason + "\n" + text;
            if (string.IsNullOrEmpty(text)) return;
            _style.normal.textColor = _valid ? Color.white : new Color(1f, 0.55f, 0.45f);
            GUI.Label(new Rect(UiScale.Width * 0.5f - 300f, UiScale.Height * 0.5f - 70f, 600f, 50f), text, _style);
        }
    }
}
