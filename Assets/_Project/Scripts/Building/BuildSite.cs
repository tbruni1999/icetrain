using System;
using SurvivalEnTren.Crafting;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.Building
{
    /// <summary>
    /// Una obra por fases con lugar fijo (sección 11 y 22.2, "Build Sites"): la cama, la forja, el arreglo de la bifurcación.
    /// Desde el 2026-10-16 (decisiones de Tomi: los fantasmas azules "le quitan inmersión"; "que se mejoren en el workbench y
    /// tengan un timer"; "me gustaría instalarla yo"):
    /// - Cada fase se arma en el banco de trabajo como un kit (Workbench), que tarda su tiempo.
    /// - El jugador lleva el kit en la mano hasta la obra y mantiene E: la fase queda instalada y se ve.
    /// - Lo que todavía no se instaló no se ve (sin piezas fantasma). Las fases van en orden.
    /// - Si no se lleva nada, E usa el objeto de "fallback" (dormir en la cama), así la obra y el mueble comparten el lugar.
    /// - Puede depender de un plano (requiredBlueprint) o de una compuerta (GateOpen: el hielo que tapa un cambio de vía).
    /// - El ID estable es para el guardado (Anexo B.4) y para saber de qué obra es cada kit.
    /// </summary>
    public class BuildSite : MonoBehaviour, IInteractable, IUsableWhileCarrying
    {
        [Serializable]
        public class PhasePieces
        {
            [Tooltip("Las piezas visibles de esta fase (cada una es un objeto con su malla y colliders).")]
            public GameObject[] pieces;
        }

        [SerializeField] private BuildProject project;
        [Tooltip("ID estable de la obra (para el guardado y los kits).")]
        [SerializeField] private string stableId;
        [Tooltip("Las piezas de cada fase, en el mismo orden que las fases del proyecto.")]
        [SerializeField] private PhasePieces[] phasePieces = Array.Empty<PhasePieces>();
        [Tooltip("Plano que hay que conocer para construir esta obra (opcional). Mientras no se conoce, no se puede instalar nada.")]
        [SerializeField] private Blueprint requiredBlueprint;
        [Tooltip("Libro de planos del jugador (se conecta en la escena). Sin libro, la obra no pide plano.")]
        [SerializeField] private BlueprintBook book;
        [Tooltip("Lo que se usa cuando no se lleva nada en la mano (debe implementar IInteractable): dormir.")]
        [SerializeField] private MonoBehaviour fallback;
        [Tooltip("Al terminar la obra, se apagan las zonas de uso para que se apunte directo a lo construido (una palanca que ahora funciona).")]
        [SerializeField] private bool zonesOffWhenComplete;

        private Collider[] _zones;
        private int _level;
        private bool _gateOpen = true;
        private bool _awake;

        public string StableId => stableId;
        public string DisplayName => project != null ? project.DisplayName : name;
        /// <summary>No se puede instalar: falta el plano o algo la tapa (el hielo).</summary>
        public bool Locked => !_gateOpen || (requiredBlueprint != null && book != null && !book.Knows(requiredBlueprint));
        /// <summary>Hay que conocer este plano para que el banco arme sus kits (null si no pide ninguno).</summary>
        public Blueprint RequiredBlueprint => requiredBlueprint;

        /// <summary>Otra cosa decide si la obra se puede instalar (el hielo que tapa un cambio de vía).</summary>
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
        /// <summary>Cuántas fases están instaladas (0 = sin avance, PhaseCount = terminada).</summary>
        public int Level => _level;
        public bool IsComplete => _level >= project.PhaseCount;
        /// <summary>1 si la obra está terminada, 0 si no (ya no hay fases a medias: cada kit instala una fase entera).</summary>
        public float Fraction => IsComplete ? 1f : 0f;
        /// <summary>El valor del efecto ahora (la recuperación de la cama), el del nivel instalado.</summary>
        public float Value => project.LevelValue(_level);
        /// <summary>Cómo se llama la próxima fase a instalar (vacío si está terminada).</summary>
        public string NextPhaseName => IsComplete ? "" : project.Phase(_level).name;

        /// <summary>Cuántas piezas de un material ya tiene la obra (las de las fases instaladas).</summary>
        public int ContributedOf(ItemDefinition item)
        {
            int total = 0;
            for (int p = 0; p < Mathf.Min(_level, project.PhaseCount); p++)
                foreach (BuildRequirement r in project.Phase(p).requirements)
                    if (r.item == item) total += r.count;
            return total;
        }

        /// <summary>Ya no hay aportes sueltos (cada kit es una fase entera): queda vacío, por compatibilidad con el guardado.</summary>
        public int[] ContributedSnapshot() => new int[0];

        /// <summary>Deja la obra en un nivel (al cargar una partida). Lo aportado suelto de partidas viejas se ignora.</summary>
        public void Restore(int level, int[] contributed)
        {
            _level = Mathf.Clamp(level, 0, project.PhaseCount);
            ApplyVisuals();
            Changed?.Invoke();
        }

        /// <summary>Se dispara cuando se instala una fase.</summary>
        public event Action Changed;

        private void Awake()
        {
            var zones = new System.Collections.Generic.List<Collider>();
            foreach (Collider c in GetComponentsInChildren<Collider>(true)) if (c.isTrigger) zones.Add(c);
            _zones = zones.ToArray();
            if (book != null) book.Changed += ApplyVisuals;
            _awake = true;
            ApplyVisuals();
        }

        private void OnDestroy()
        {
            if (book != null) book.Changed -= ApplyVisuals;
        }

        // ---------------------------------------------------------------- Instalar

        /// <summary>¿Este kit es el que la obra necesita ahora?</summary>
        public bool Accepts(UpgradeKit kit) =>
            kit != null && kit.Kind == KitKind.SitePhase && kit.SiteId == stableId && kit.Phase == _level && !IsComplete && !Locked;

        /// <summary>Instala la próxima fase (lo hace el kit; las pruebas lo usan directo). Devuelve false si ya está terminada.</summary>
        public bool InstallNext()
        {
            if (IsComplete) return false;
            _level++;
            ApplyVisuals();
            Changed?.Invoke();
            return true;
        }

        private static UpgradeKit HeldKit(PlayerInteractor interactor, out Carryable held)
        {
            var carrier = interactor.GetComponent<PlayerCarrier>();
            held = carrier != null ? carrier.Holding : null;
            return held != null ? held.GetComponent<UpgradeKit>() : null;
        }

        private IInteractable Fallback => fallback as IInteractable;

        public string GetPrompt(PlayerInteractor interactor)
        {
            UpgradeKit kit = HeldKit(interactor, out Carryable held);
            if (held != null)
            {
                // Con algo en la mano: solo cuenta si es un kit de esta obra (si no, E lo suelta como siempre).
                if (kit == null || kit.Kind != KitKind.SitePhase || kit.SiteId != stableId || IsComplete || Locked) return null;
                if (Accepts(kit)) return $"mantener para instalar: {kit.DisplayName}";
                return kit.Phase < _level ? null : $"primero va: {NextPhaseName}";
            }
            return Locked ? null : Fallback?.GetPrompt(interactor);
        }

        public float GetHoldSeconds(PlayerInteractor interactor)
        {
            UpgradeKit kit = HeldKit(interactor, out Carryable held);
            if (held != null) return Accepts(kit) ? project.InstallSeconds : 0f;
            return Fallback != null ? Fallback.GetHoldSeconds(interactor) : 0f;
        }

        public void Interact(PlayerInteractor interactor)
        {
            UpgradeKit kit = HeldKit(interactor, out Carryable held);
            if (held != null)
            {
                if (!Accepts(kit)) return;
                interactor.GetComponent<PlayerCarrier>().Drop(throwIt: false);
                Destroy(kit.gameObject);
                InstallNext();
                return;
            }
            if (!Locked) Fallback?.Interact(interactor);
        }

        // ---------------------------------------------------------------- Qué se ve

        // Fases instaladas: se ven. Las que faltan: no se ven (sin fantasmas).
        private void ApplyVisuals()
        {
            if (!_awake) return;
            if (_zones != null) foreach (Collider z in _zones) z.enabled = !Locked && !(zonesOffWhenComplete && IsComplete);
            for (int p = 0; p < phasePieces.Length; p++)
            {
                GameObject[] pieces = phasePieces[p].pieces ?? Array.Empty<GameObject>();
                foreach (GameObject piece in pieces) if (piece != null) piece.SetActive(p < _level);
            }
        }
    }
}
