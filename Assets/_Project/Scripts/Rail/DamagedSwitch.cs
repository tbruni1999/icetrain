using SurvivalEnTren.Building;
using SurvivalEnTren.Poi;
using UnityEngine;

namespace SurvivalEnTren.Rail
{
    /// <summary>
    /// Una bifurcación dañada (secciones 6.3 y 20.9, "primer gran objetivo"): conecta tres cosas que ya existen.
    /// - El hielo sobre el mecanismo (FrozenDoor): al romperse, el cambio pasa de CONGELADO a ROTO.
    /// - La obra de reparación (BuildSite): no se ve ni se puede empezar mientras el hielo la tapa; al completarse la
    ///   última fase, el cambio pasa a REPARADO y la palanca funciona.
    /// - El cambio (RailSwitch): su estado decide si el tren puede tomar el desvío.
    /// El estado vive en el cambio (RailSwitch.Condition), no acá: esta clase solo reacciona.
    /// </summary>
    public class DamagedSwitch : MonoBehaviour
    {
        [SerializeField] private RailSwitch railSwitch;
        [Tooltip("El hielo que tapa el mecanismo (opcional: sin hielo, el cambio empieza roto).")]
        [SerializeField] private FrozenDoor ice;
        [Tooltip("La obra de reparación (opcional: sin obra, el cambio queda roto).")]
        [SerializeField] private BuildSite repair;

        public RailSwitch Switch => railSwitch;

        private void Start()
        {
            if (ice != null) ice.Broken += OnThawed;
            if (repair != null) repair.Changed += OnRepairChanged;
            railSwitch.ConditionChanged += ApplyGate;
            ApplyGate();
        }

        private void OnDestroy()
        {
            if (ice != null) ice.Broken -= OnThawed;
            if (repair != null) repair.Changed -= OnRepairChanged;
            if (railSwitch != null) railSwitch.ConditionChanged -= ApplyGate;
        }

        private void OnThawed()
        {
            if (railSwitch.Condition == RailSwitchCondition.Frozen) railSwitch.SetCondition(RailSwitchCondition.Broken);
        }

        private void OnRepairChanged()
        {
            if (repair.IsComplete && railSwitch.Condition == RailSwitchCondition.Broken) railSwitch.SetCondition(RailSwitchCondition.Repaired);
        }

        // La obra solo se puede empezar cuando el hielo ya no tapa el mecanismo.
        private void ApplyGate()
        {
            if (repair != null) repair.GateOpen = railSwitch.Condition != RailSwitchCondition.Frozen;
        }
    }
}
