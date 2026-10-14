using System;
using System.Collections.Generic;
using SurvivalEnTren.Crafting;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.Building
{
    /// <summary>
    /// Una obra por fases en el mundo (sección 11 y 22.2, "Build Sites"): la cama, un estante. Con los materiales en
    /// la mochila, mantener E los va aportando de a uno (salen de la mochila y la obra los guarda: nada se crea ni se
    /// pierde). Lo aportado queda representado físicamente: las piezas que ya se aportaron se ven sólidas, las que
    /// faltan de la fase actual se ven como fantasmas translúcidos (sección 18.3) y las fases siguientes no se ven hasta
    /// llegar a ellas. Una obra puede quedar a medio hacer varias expediciones (sección 11.1).
    /// - Cada avance aporta valor aunque la fase no esté completa (sección 11.2): ver Value.
    /// - Al completar una fase se corta el aporte (hay que volver a mantener E para la siguiente): se nota el hito.
    /// - Si no hay nada que aportar (sin materiales, o la obra terminada), E usa el objeto de "fallback" (dormir en la
    ///   cama, abrir el estante), así la obra y el mueble comparten el mismo lugar.
    /// - El ID estable es para el guardado futuro (obras y materiales aportados, Anexo B.4).
    /// - Puede depender de un plano (requiredBlueprint) o de una compuerta (GateOpen: el hielo que tapa un cambio de vía).
    /// </summary>
    public class BuildSite : MonoBehaviour, IChanneledInteractable
    {
        [Serializable]
        public class PhasePieces
        {
            [Tooltip("Las piezas visibles de esta fase (cada una es un objeto con su malla y colliders).")]
            public GameObject[] pieces;
        }

        [SerializeField] private BuildProject project;
        [Tooltip("ID estable de la obra (para el guardado futuro).")]
        [SerializeField] private string stableId;
        [Tooltip("Las piezas de cada fase, en el mismo orden que las fases del proyecto.")]
        [SerializeField] private PhasePieces[] phasePieces = Array.Empty<PhasePieces>();
        [Tooltip("Material translúcido de las piezas que todavía faltan.")]
        [SerializeField] private Material ghostMaterial;
        [Tooltip("Plano que hay que conocer para construir esta obra (opcional). Mientras no se conoce, no se ve nada: la pared está vacía.")]
        [SerializeField] private Blueprint requiredBlueprint;
        [Tooltip("Libro de planos del jugador (se conecta en la escena). Sin libro, la obra no pide plano.")]
        [SerializeField] private BlueprintBook book;
        [Tooltip("Lo que se usa cuando no hay nada que aportar (debe implementar IInteractable): dormir, abrir el estante.")]
        [SerializeField] private MonoBehaviour fallback;
        [Tooltip("Al terminar la obra, se apagan las zonas de uso para que se apunte directo a lo construido (una palanca que ahora funciona). Los muebles (cama, estante) lo dejan apagado.")]
        [SerializeField] private bool zonesOffWhenComplete;

        private sealed class PieceVisual
        {
            public GameObject Root;
            public Renderer[] Renderers;
            public Material[] Solid;
            public Collider[] Colliders;
        }

        private PieceVisual[][] _visuals;
        private Collider[] _zones;
        private int[] _contributed;
        private int _level;
        private bool _gateOpen = true;
        private float _timer;

        public string StableId => stableId;
        /// <summary>Todavía no se conoce el plano de esta obra: no se ve ni se puede construir.</summary>
        public bool Locked => !_gateOpen || (requiredBlueprint != null && book != null && !book.Knows(requiredBlueprint));

        /// <summary>
        /// Otra cosa decide si la obra se puede empezar (el hielo que tapa un cambio de vía): cerrada, no se ve ni se puede apuntar.
        /// </summary>
        public bool GateOpen
        {
            get => _gateOpen;
            set
            {
                if (_gateOpen == value) return;
                _gateOpen = value;
                ApplyVisuals();
            }
        }
        public BuildProject Project => project;
        /// <summary>Cuántas fases están completas (0 = sin avance, PhaseCount = terminada).</summary>
        public int Level => _level;
        public bool IsComplete => _level >= project.PhaseCount;
        /// <summary>Qué fracción de la fase actual está aportada (0 a 1; 1 si la obra está terminada).</summary>
        public float Fraction => IsComplete ? 1f : (float)ContributedUnits / Mathf.Max(1, project.Phase(_level).TotalUnits);
        /// <summary>El valor del efecto ahora: sube de a poco con lo aportado y llega al de cada nivel al completar la fase.</summary>
        public float Value
        {
            get
            {
                if (IsComplete) return project.LevelValue(project.PhaseCount);
                return Mathf.Lerp(project.LevelValue(_level), project.LevelValue(_level + 1), Fraction);
            }
        }

        public int ContributedUnits
        {
            get
            {
                int total = 0;
                if (_contributed != null) foreach (int c in _contributed) total += c;
                return total;
            }
        }

        /// <summary>Cuántas piezas de un material se aportaron en total a la obra (fases completas + lo de la fase actual).</summary>
        public int ContributedOf(ItemDefinition item)
        {
            int total = 0;
            for (int p = 0; p < Mathf.Min(_level, project.PhaseCount); p++)
                foreach (BuildRequirement r in project.Phase(p).requirements)
                    if (r.item == item) total += r.count;
            if (!IsComplete && _contributed != null)
            {
                BuildRequirement[] current = project.Phase(_level).requirements;
                for (int i = 0; i < current.Length; i++) if (current[i].item == item) total += _contributed[i];
            }
            return total;
        }

        /// <summary>Se dispara cuando se aporta una pieza o se completa una fase.</summary>
        public event Action Changed;

        private void Awake()
        {
            _visuals = new PieceVisual[phasePieces.Length][];
            for (int p = 0; p < phasePieces.Length; p++)
            {
                GameObject[] pieces = phasePieces[p].pieces ?? Array.Empty<GameObject>();
                _visuals[p] = new PieceVisual[pieces.Length];
                for (int i = 0; i < pieces.Length; i++)
                {
                    Renderer[] renderers = pieces[i].GetComponentsInChildren<Renderer>(true);
                    var solid = new Material[renderers.Length];
                    for (int r = 0; r < renderers.Length; r++) solid[r] = renderers[r].sharedMaterial;
                    _visuals[p][i] = new PieceVisual
                    {
                        Root = pieces[i],
                        Renderers = renderers,
                        Solid = solid,
                        Colliders = pieces[i].GetComponentsInChildren<Collider>(true)
                    };
                }
            }
            StartPhase();
            // La zona de uso (un trigger sobre toda la obra) se apaga mientras el plano no se conoce.
            var zones = new System.Collections.Generic.List<Collider>();
            foreach (Collider c in GetComponentsInChildren<Collider>(true)) if (c.isTrigger) zones.Add(c);
            _zones = zones.ToArray();
            if (book != null) book.Changed += ApplyVisuals;
            ApplyVisuals();
        }
        private void OnDestroy()
        {
            if (book != null) book.Changed -= ApplyVisuals;
        }


        private void StartPhase()
        {
            _contributed = IsComplete ? new int[0] : new int[project.Phase(_level).requirements.Length];
            _timer = 0f;
        }

        // ---------------------------------------------------------------- Aportar

        private IInteractable Fallback => fallback as IInteractable;

        // ¿Hay algo en la mochila que la fase actual todavía pida?
        private bool CanContribute(Backpack backpack)
        {
            if (IsComplete || backpack == null) return false;
            BuildRequirement[] requirements = project.Phase(_level).requirements;
            for (int i = 0; i < requirements.Length; i++)
                if (_contributed[i] < requirements[i].count && backpack.CountOf(requirements[i].item) > 0) return true;
            return false;
        }

        private bool ContributeOne(Backpack backpack)
        {
            BuildRequirement[] requirements = project.Phase(_level).requirements;
            for (int i = 0; i < requirements.Length; i++)
            {
                if (_contributed[i] >= requirements[i].count) continue;
                if (backpack.Remove(requirements[i].item, 1) == 0) continue;
                _contributed[i]++;
                if (PhaseDone())
                {
                    _level++;
                    StartPhase();
                }
                ApplyVisuals();
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        private bool PhaseDone()
        {
            BuildRequirement[] requirements = project.Phase(_level).requirements;
            for (int i = 0; i < requirements.Length; i++)
                if (_contributed[i] < requirements[i].count) return false;
            return true;
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (Locked) return null; // sin el plano, la pared está vacía
            var backpack = interactor.GetComponent<Backpack>();
            string fallbackPrompt = Fallback?.GetPrompt(interactor);
            if (IsComplete || backpack == null) return fallbackPrompt;

            BuildPhase phase = project.Phase(_level);
            if (CanContribute(backpack))
                return $"mantener para construir {phase.name}   ({Progress(phase)})";

            // Sin nada para aportar: lo habitual (dormir, abrir) y, si hace falta, qué pide la fase.
            string need = $"{phase.name} pide {Remaining(phase)}";
            return fallbackPrompt != null ? $"{fallbackPrompt}   ·   {need}" : need;
        }

        private string Progress(BuildPhase phase)
        {
            var parts = new List<string>();
            for (int i = 0; i < phase.requirements.Length; i++)
                parts.Add($"{phase.requirements[i].item.PieceName} {_contributed[i]}/{phase.requirements[i].count}");
            return string.Join(", ", parts);
        }

        private string Remaining(BuildPhase phase)
        {
            var parts = new List<string>();
            for (int i = 0; i < phase.requirements.Length; i++)
            {
                int left = phase.requirements[i].count - _contributed[i];
                if (left > 0) parts.Add($"{phase.requirements[i].item.PieceName} ×{left}");
            }
            return string.Join(", ", parts);
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor) { } // todo pasa en Channel

        public bool Channel(PlayerInteractor interactor, float deltaTime)
        {
            if (Locked) return false;
            var backpack = interactor.GetComponent<Backpack>();
            if (!CanContribute(backpack))
            {
                // Nada que aportar: se usa lo de siempre del mueble, una sola vez por pulsación.
                Fallback?.Interact(interactor);
                return false;
            }

            int levelBefore = _level;
            _timer += deltaTime * project.UnitsPerSecond;
            while (_timer >= 1f)
            {
                _timer -= 1f;
                if (!ContributeOne(backpack)) { _timer = 0f; break; }
                if (_level != levelBefore) return false; // se completó una fase: hito, se corta
            }
            return CanContribute(backpack);
        }

        // ---------------------------------------------------------------- Qué se ve

        // Fases ya hechas: piezas sólidas. Fase actual: las aportadas sólidas y el resto fantasma. Fases siguientes: ocultas.
        private void ApplyVisuals()
        {
            if (_visuals == null) return;
            if (_zones != null) foreach (Collider z in _zones) z.enabled = !Locked && !(zonesOffWhenComplete && IsComplete);
            for (int p = 0; p < _visuals.Length; p++)
            {
                PieceVisual[] pieces = _visuals[p];
                int solidCount;
                if (p < _level) solidCount = pieces.Length;
                else if (p == _level && !IsComplete)
                    solidCount = Mathf.FloorToInt(Fraction * pieces.Length + 1e-4f);
                else solidCount = 0;
                bool visible = !Locked && p <= _level;

                for (int i = 0; i < pieces.Length; i++)
                {
                    PieceVisual piece = pieces[i];
                    piece.Root.SetActive(visible);
                    if (!visible) continue;
                    bool solid = i < solidCount;
                    for (int r = 0; r < piece.Renderers.Length; r++)
                    {
                        piece.Renderers[r].sharedMaterial = solid ? piece.Solid[r] : ghostMaterial;
                        piece.Renderers[r].shadowCastingMode = solid ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    foreach (Collider c in piece.Colliders) c.enabled = solid;
                }
            }
        }
    }
}
