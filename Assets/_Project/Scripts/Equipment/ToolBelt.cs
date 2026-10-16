using System;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Player;
using SurvivalEnTren.Probe;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Las herramientas en la mano (herramientas y forja, paso H1). Elige qué lleva el jugador en la mano con 1, 2, 3… o la rueda
    /// del mouse (volver a apretar el mismo número deja las manos libres), la muestra delante de la cámara y, con el clic izquierdo
    /// (o manteniéndolo), da golpes: la herramienta se echa atrás, baja y, en el momento del golpe, pega a lo que haya adelante.
    /// - Lo que reacciona a los golpes implementa IToolTarget (H2: desarmar a golpes).
    /// - La sonda portátil es una casilla más: elegirla la saca; Q la sigue sacando y guardando, y la barra la acompaña.
    /// - Se guarda sola mientras se lleva algo en las manos, se maneja, hay un panel abierto o el jugador no se puede mover.
    /// </summary>
    public class ToolBelt : MonoBehaviour
    {
        [SerializeField] private ToolSettings settings;
        [SerializeField] private PlayerGear gear;
        [SerializeField] private PortableProbe probe;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerCarrier carrier;
        [SerializeField] private PlayerInteractor interactor;
        [Tooltip("Opcional: cada golpe que sirve cansa un poco (ToolSettings.energyPerHit).")]
        [SerializeField] private SurvivalEnTren.Survival.PlayerVitals vitals;
        [Tooltip("Mientras se coloca un mueble, la rueda lo gira y el clic lo deja: la barra no los usa.")]
        [SerializeField] private SurvivalEnTren.Building.PlacementController placement;
        [Tooltip("Las manos en primera persona: el empujón de la vista al pegar y cómo se guarda la herramienta.")]
        [SerializeField] private ViewmodelRig rig;
        [Tooltip("La cabeza (cámara): de acá sale el golpe.")]
        [SerializeField] private Transform head;
        [Tooltip("El modelo de cada casilla en la mano (hijo de la cabeza), en el mismo orden que las casillas. Vacío en la de la sonda.")]
        [SerializeField] private Transform[] handModels = Array.Empty<Transform>();

        [Header("Teclas")]
        [SerializeField] private InputActionReference use;
        [SerializeField] private InputActionReference[] slotKeys = Array.Empty<InputActionReference>();
        [SerializeField] private InputActionReference scroll;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

        private int _selected = -1;
        private float _raise;              // 0 guardada, 1 en la mano
        private float _swing = -1f;        // progreso del golpe (0..1), -1 = sin golpe
        private bool _struck;              // ya pegó en este golpe
        private float _impactPause;
        private enum ImpactKind { None, Useful, Rebound }
        private ImpactKind _impact;
        private float _shake;              // temblor de la herramienta después de un rebote (1 = recién)
        private Vector3[] _restPositions;
        private Quaternion[] _restRotations;
        private AudioSource _audio;
        private AudioClip _whoosh, _thud, _clank;

        /// <summary>La casilla elegida (-1 = manos libres).</summary>
        public int Selected => _selected;
        public ToolSettings Settings => settings;
        /// <summary>La herramienta que se lleva en la mano, si es una herramienta (no la sonda ni manos libres).</summary>
        public GearId? ActiveTool => _selected >= 0 && settings.Slots[_selected].kind == HandSlotKind.Gear ? settings.Slots[_selected].gear : (GearId?)null;
        /// <summary>La herramienta está en la mano y lista (no guardada mientras se lleva una caja, se maneja o hay un panel).</summary>
        public bool IsRaised => ActiveTool.HasValue && _raise > 0.99f;
        public bool IsSwinging => _swing >= 0f;
        /// <summary>Segundos desde el último cambio de casilla (la barra se marca un rato).</summary>
        public float SinceChange { get; private set; } = 99f;

        // Para las pruebas y el sonido.
        public int Swings { get; private set; }
        public int HitCount { get; private set; }
        public string LastHitName { get; private set; } = "";
        public bool LastHitUseful { get; private set; }

        /// <summary>Se dispara en cada golpe que toca algo (herramienta, punto, si sirvió).</summary>
        public event Action<ToolHit, bool> Struck;
        public event Action Changed;

        private void Awake()
        {
            _restPositions = new Vector3[handModels.Length];
            _restRotations = new Quaternion[handModels.Length];
            for (int i = 0; i < handModels.Length; i++)
            {
                if (handModels[i] == null) continue;
                _restPositions[i] = handModels[i].localPosition;
                _restRotations[i] = handModels[i].localRotation;
                handModels[i].gameObject.SetActive(false);
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _whoosh = SurvivalEnTren.Audio.ProceduralClips.Slide("Golpe_Aire", 0.18f, 211);
            _thud = SurvivalEnTren.Audio.ProceduralClips.Thud("Golpe_Seco", 0.2f, 223);
            _clank = SurvivalEnTren.Audio.ProceduralClips.Clank("Golpe_Rebote", 520f, 0.25f, 227);
        }

        private void OnEnable()
        {
            use.action.Enable();
            scroll.action.Enable();
            foreach (InputActionReference key in slotKeys) key.action.Enable();
        }

        /// <summary>Se puede elegir esa casilla (la sonda siempre; una herramienta, si el jugador la tiene).</summary>
        public bool Available(int slot)
        {
            if (slot < 0 || slot >= settings.Slots.Length) return false;
            ToolSlot s = settings.Slots[slot];
            return s.kind == HandSlotKind.Probe ? probe != null : gear.Has(s.gear);
        }

        /// <summary>La casilla de una herramienta (-1 si no está en la barra).</summary>
        public int SlotOf(GearId tool) => Array.FindIndex(settings.Slots, s => s.kind == HandSlotKind.Gear && s.gear == tool);

        /// <summary>Elige una casilla; si ya estaba elegida (o es -1), deja las manos libres.</summary>
        public void Select(int slot)
        {
            if (slot >= 0 && !Available(slot)) { SinceChange = 0f; return; } // no la tiene: la barra se marca y no cambia nada
            int next = slot == _selected ? -1 : slot;
            if (next == _selected) return;
            _selected = next;
            _swing = -1f;
            SinceChange = 0f;
            if (probe != null) probe.SetOut(_selected >= 0 && settings.Slots[_selected].kind == HandSlotKind.Probe);
            Changed?.Invoke();
        }

        /// <summary>La siguiente (o anterior) casilla disponible, pasando por "manos libres".</summary>
        public void Cycle(int direction)
        {
            int count = settings.Slots.Length;
            int i = _selected;
            for (int n = 0; n <= count; n++)
            {
                i += direction;
                if (i >= count) i = -1;
                if (i < -1) i = count - 1;
                if (i == -1 || Available(i)) break;
            }
            if (i == -1) Select(-1); else if (i != _selected) Select(i);
        }

        /// <summary>Empieza un golpe si hay una herramienta en la mano y no está golpeando.</summary>
        public bool TrySwing()
        {
            if (!IsRaised || IsSwinging) return false;
            _swing = 0f;
            _struck = false;
            _impactPause = 0f;
            _impact = ImpactKind.None;
            Swings++;
            _audio.PlayOneShot(_whoosh, 0.35f);
            return true;
        }

        private bool HandsBusy => carrier != null && carrier.Holding != null;
        private bool Blocked => HandsBusy || motor.MovementLocked || (interactor != null && !interactor.enabled) || Time.timeScale <= 0f;

        private void Update()
        {
            SinceChange += Time.unscaledDeltaTime;
            // La sonda se saca y se guarda también con Q (PortableProbe): la barra la sigue.
            if (probe != null)
            {
                int probeSlot = Array.FindIndex(settings.Slots, s => s.kind == HandSlotKind.Probe);
                bool probeSelected = _selected == probeSlot;
                if (probe.Wanted && !probeSelected && probeSlot >= 0) { _selected = probeSlot; _swing = -1f; SinceChange = 0f; Changed?.Invoke(); }
                else if (!probe.Wanted && probeSelected) { _selected = -1; SinceChange = 0f; Changed?.Invoke(); }
            }
            if (_selected >= 0 && !Available(_selected)) Select(-1);

            if (!Blocked)
            {
                for (int i = 0; i < slotKeys.Length && i < settings.Slots.Length; i++)
                    if (slotKeys[i].action.WasPressedThisFrame()) Select(i);
                bool placing = placement != null && placement.BlocksTool;
                float wheel = placing ? 0f : scroll.action.ReadValue<float>();
                if (wheel > 0.01f) Cycle(-1);
                else if (wheel < -0.01f) Cycle(+1);
                if (!placing && use.action.IsPressed()) TrySwing();
            }
        }

        private void LateUpdate()
        {
            bool wantUp = ActiveTool.HasValue && !Blocked;
            _raise = Mathf.MoveTowards(_raise, wantUp ? 1f : 0f, Time.deltaTime * settings.RaiseSpeed);
            if (_raise < 0.99f) _swing = -1f;

            if (IsSwinging) AdvanceSwing(Time.deltaTime);

            for (int i = 0; i < handModels.Length; i++)
            {
                Transform model = handModels[i];
                if (model == null) continue;
                bool shown = i == _selected && _raise > 0f;
                if (model.gameObject.activeSelf != shown) model.gameObject.SetActive(shown);
                if (!shown) continue;
                // Guardada: abajo y girada (sube desde abajo al sacarla). En la mano: la pose del golpe y el temblor del rebote.
                float lowered = 1f - Mathf.SmoothStep(0f, 1f, _raise);
                Vector3 lowPos = rig != null ? rig.Settings.LoweredOffset : new Vector3(0f, -0.35f, 0f);
                Vector3 lowRot = rig != null ? rig.Settings.LoweredRotation : Vector3.zero;
                SwingPose(settings.Slots[i], out Vector3 swingPos, out Vector3 swingRot);
                _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 4f);
                Vector3 tremble = _shake > 0f ? new Vector3(Mathf.Sin(Time.time * 97f), Mathf.Sin(Time.time * 131f), 0f) * (0.006f * _shake) : Vector3.zero;
                model.localPosition = _restPositions[i] + lowPos * lowered + swingPos + tremble;
                // El golpe gira alrededor del puño en los ejes de la vista (derecha, arriba, adelante); guardada, en los del modelo.
                model.localRotation = Quaternion.Euler(swingRot) * _restRotations[i] * Quaternion.Euler(lowRot * lowered);
            }
        }

        // La pose del golpe en este momento (corrimiento y giro respecto del descanso):
        // cargar despacio (y frenar arriba), bajar acelerando hasta pegar, y volver. Después de pegar: si pegó en algo que sirve,
        // un rebotito; si rebotó (herramienta equivocada o algo duro), un rebote grande para atrás; si no pegó en nada, la
        // herramienta sigue de largo un poco antes de volver.
        private void SwingPose(ToolSlot s, out Vector3 position, out Vector3 rotation)
        {
            position = Vector3.zero;
            rotation = Vector3.zero;
            if (!IsSwinging) return;
            float t = _swing;
            float windupEnd = s.hitAt * 0.62f;
            if (t < windupEnd)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / windupEnd);
                position = s.windupPosition * k;
                rotation = s.windupRotation * k;
                return;
            }
            if (t < s.hitAt)
            {
                float u = (t - windupEnd) / Mathf.Max(0.001f, s.hitAt - windupEnd);
                float e = u * u * u; // acelera: el peso de la herramienta
                position = Vector3.Lerp(s.windupPosition, s.strikePosition, e);
                rotation = Vector3.Lerp(s.windupRotation, s.strikeRotation, e);
                return;
            }
            float r = Mathf.Clamp01((t - s.hitAt) / Mathf.Max(0.001f, 1f - s.hitAt));
            float back = Mathf.SmoothStep(0f, 1f, r);
            position = Vector3.Lerp(s.strikePosition, Vector3.zero, back);
            rotation = Vector3.Lerp(s.strikeRotation, Vector3.zero, back);
            float bump = Mathf.Sin(r * Mathf.PI);
            Vector3 towardWindupPos = s.windupPosition - s.strikePosition;
            Vector3 towardWindupRot = s.windupRotation - s.strikeRotation;
            float amount = _impact == ImpactKind.Rebound ? 0.3f : _impact == ImpactKind.Useful ? 0.08f : -0.15f; // negativo: sigue de largo
            position += towardWindupPos * (amount * bump);
            rotation += towardWindupRot * (amount * bump);
        }

        private void AdvanceSwing(float dt)
        {
            ToolSlot s = settings.Slots[_selected];
            if (_impactPause > 0f) { _impactPause -= dt; return; } // frenada contra lo que golpeó
            _swing += dt / s.swingSeconds;
            if (!_struck && _swing >= s.hitAt)
            {
                _struck = true;
                Strike(s);
            }
            if (_swing >= 1f) _swing = -1f;
        }

        private void Strike(ToolSlot s)
        {
            if (!TryFindTarget(s.reachMeters, out RaycastHit hit))
            {
                _impact = ImpactKind.None; // al aire: sigue de largo
                if (rig != null) rig.AddKick(new Vector3(s.kickDegrees * 0.25f, 0f, 0f), false);
                return;
            }
            var info = new ToolHit(s.gear, hit.point, hit.normal, hit.collider, this);
            IToolTarget target = hit.collider.GetComponentInParent<IToolTarget>();
            bool useful = target != null && target.OnToolHit(in info);
            HitCount++;
            LastHitName = hit.collider.name;
            LastHitUseful = useful;
            if (useful && vitals != null) vitals.Tire(settings.EnergyPerHit);
            // Que se sienta: una frenada contra lo que golpeó (más larga si rebota), un empujón de la vista y, si rebota, temblor.
            bool rebound = !useful;
            _impact = rebound ? ImpactKind.Rebound : ImpactKind.Useful;
            _impactPause = settings.ImpactPauseSeconds * (rebound ? 1.6f : 1f);
            if (rebound) _shake = 1f;
            if (rig != null)
            {
                float side = UnityEngine.Random.Range(-1f, 1f);
                Vector3 kick = rebound
                    ? new Vector3(-s.kickDegrees * 0.6f, side * s.kickDegrees * 0.5f, side * s.kickDegrees * 0.4f)
                    : new Vector3(s.kickDegrees, side * s.kickDegrees * 0.2f, side * s.kickDegrees * 0.3f);
                rig.AddKick(kick, rebound);
            }
            _audio.PlayOneShot(useful || target == null ? _thud : _clank, 0.6f);
            Struck?.Invoke(info, useful);
        }

        // Lo primero que hay adelante (sin contar al jugador ni lo que lleva). Las zonas de uso transparentes cuentan solo si
        // reciben golpes; las demás zonas (disparadores) se atraviesan.
        private bool TryFindTarget(float reach, out RaycastHit best)
        {
            best = default;
            int count = Physics.SphereCastNonAlloc(head.position, 0.06f, head.forward, HitBuffer, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = HitBuffer[i];
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (HandsBusy && h.collider.transform.IsChildOf(carrier.Holding.transform)) continue;
                if (h.collider.isTrigger && h.collider.GetComponentInParent<IToolTarget>() == null) continue;
                if (h.distance <= 0f && h.point == Vector3.zero) continue; // empezó adentro del collider
                if (h.distance < nearest) { nearest = h.distance; best = h; }
            }
            return nearest < float.MaxValue;
        }
    }
}
