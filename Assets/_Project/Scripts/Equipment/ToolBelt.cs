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
                float wheel = scroll.action.ReadValue<float>();
                if (wheel > 0.01f) Cycle(-1);
                else if (wheel < -0.01f) Cycle(+1);
                if (use.action.IsPressed()) TrySwing();
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
                float lowered = 1f - Mathf.SmoothStep(0f, 1f, _raise);
                model.localPosition = _restPositions[i] + Vector3.down * (0.35f * lowered);
                model.localRotation = _restRotations[i] * Quaternion.Euler(SwingAngle(), 0f, 0f);
            }
        }

        // Levantar (echarse atrás), bajar hasta pegar, volver. Ángulo alrededor del eje X del modelo (+ = hacia adelante y abajo).
        private float SwingAngle()
        {
            if (!IsSwinging) return 0f;
            ToolSlot s = settings.Slots[_selected];
            float windupEnd = s.hitAt * 0.55f;
            float t = _swing;
            if (t < windupEnd) return -settings.WindupDegrees * Mathf.SmoothStep(0f, 1f, t / windupEnd);
            if (t < s.hitAt) return Mathf.Lerp(-settings.WindupDegrees, settings.StrikeDegrees, (t - windupEnd) / (s.hitAt - windupEnd));
            return Mathf.Lerp(settings.StrikeDegrees, 0f, Mathf.SmoothStep(0f, 1f, (t - s.hitAt) / (1f - s.hitAt)));
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
            if (!TryFindTarget(s.reachMeters, out RaycastHit hit)) return;
            var info = new ToolHit(s.gear, hit.point, hit.normal, hit.collider, this);
            IToolTarget target = hit.collider.GetComponentInParent<IToolTarget>();
            bool useful = target != null && target.OnToolHit(in info);
            HitCount++;
            LastHitName = hit.collider.name;
            LastHitUseful = useful;
            _impactPause = settings.ImpactPauseSeconds;
            _audio.PlayOneShot(useful || target == null ? _thud : _clank, 0.6f);
            Struck?.Invoke(info, useful);
        }

        // Lo primero que hay adelante (sin contar al jugador ni lo que lleva). Las zonas de uso transparentes cuentan solo si
        // reciben golpes; las demás zonas (disparadores) se atraviesan.
        private bool TryFindTarget(float reach, out RaycastHit best)
        {
            best = default;
            int count = Physics.SphereCastNonAlloc(head.position, 0.06f, head.forward, HitBuffer, reach, ~0, QueryTriggerInteraction.Collide);
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
