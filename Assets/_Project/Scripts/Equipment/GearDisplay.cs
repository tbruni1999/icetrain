using UnityEngine;

namespace SurvivalEnTren.Equipment
{
    /// <summary>
    /// Una herramienta fabricada que se ve colgada en el taller (la sierra sobre el banco): aparece recién cuando el jugador
    /// la tiene, así el taller muestra lo que ya se hizo (sección 11.3: el tren muestra el progreso). Es solo una imagen:
    /// la herramienta en sí queda al cinto (PlayerGear). Sin equipo conectado (la escena de pruebas del tren) no se ve.
    /// </summary>
    public class GearDisplay : MonoBehaviour
    {
        [SerializeField] private GearId gearId = GearId.Saw;
        [Tooltip("Equipo del jugador (se conecta en la escena: el prefab del tren no puede apuntar a objetos de la escena).")]
        [SerializeField] private PlayerGear gear;
        [Tooltip("Lo que se muestra u oculta (la malla de la herramienta).")]
        [SerializeField] private GameObject visual;

        private void Awake()
        {
            if (gear != null) gear.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (gear != null) gear.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (visual != null) visual.SetActive(gear != null && gear.Has(gearId));
        }
    }
}
