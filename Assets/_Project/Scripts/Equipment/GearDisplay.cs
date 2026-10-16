using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Una herramienta fabricada que se ve colgada en el taller (la sierra sobre el banco): aparece recién cuando el jugador
    /// la tiene, así el taller muestra lo que ya se hizo (sección 11.3: el tren muestra el progreso). Es solo una imagen:
    /// la herramienta en sí queda al cinto (PlayerGear). Sin equipo conectado (la escena de pruebas del tren) no se ve.
    /// Mientras el jugador la lleva en la mano (barra de herramientas, H1) no está colgada.
    /// </summary>
    public class GearDisplay : MonoBehaviour
    {
        [SerializeField] private GearId gearId = GearId.Saw;
        [Tooltip("Equipo del jugador (se conecta en la escena: el prefab del tren no puede apuntar a objetos de la escena).")]
        [SerializeField] private PlayerGear gear;
        [Tooltip("Opcional: la barra de herramientas del jugador (si la lleva en la mano, no está colgada).")]
        [SerializeField] private ToolBelt belt;
        [Tooltip("Lo que se muestra u oculta (la malla de la herramienta).")]
        [SerializeField] private GameObject visual;

        private void Awake()
        {
            if (gear != null) gear.Changed += Refresh;
            if (belt != null) belt.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (gear != null) gear.Changed -= Refresh;
            if (belt != null) belt.Changed -= Refresh;
        }

        private void Refresh()
        {
            bool inHand = belt != null && belt.ActiveTool == gearId;
            if (visual != null) visual.SetActive(gear != null && gear.Has(gearId) && !inHand);
        }
    }
}
