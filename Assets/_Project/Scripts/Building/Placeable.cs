using SurvivalEnTren.Equipment;
using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.Building
{
    /// <summary>
    /// Un mueble que el jugador pone donde quiere (decisión de Tomi del 2026-10-16: "que aparezca como lo que va a ocupar y yo
    /// ponerla como quiera", y "una herramienta que te deje moverla"). Sale de un kit del banco; con la llave de montaje se
    /// vuelve a levantar y se pone en otro lado, con lo que tenga guardado adentro.
    /// - El lugar que ocupa (footprint) es una caja en coordenadas del mueble: con ella se ve si entra.
    /// - El frente del mueble mira a +Z; al apoyarlo contra una pared, la espalda queda contra la pared.
    /// - El ID estable se le da al colocarlo (para el guardado y para lo que guarda adentro).
    /// </summary>
    public class Placeable : MonoBehaviour, IToolTarget
    {
        [Tooltip("ID estable (los que se colocan en el juego lo reciben al instalarse).")]
        [SerializeField] private string stableId;
        [Tooltip("Qué mueble es (la clave del plano que lo fabrica): para volver a crearlo al cargar una partida.")]
        [SerializeField] private string key;
        [SerializeField] private string displayName = "mueble";
        [Tooltip("El lugar que ocupa, en coordenadas del mueble (centro y tamaño, en metros).")]
        [SerializeField] private Vector3 footprintCenter = new Vector3(0f, 0.8f, 0f);
        [SerializeField] private Vector3 footprintSize = new Vector3(1.6f, 1.6f, 0.4f);

        public string StableId => stableId;
        public string Key => key;
        public string DisplayName => displayName;
        public Vector3 FootprintCenter => footprintCenter;
        public Vector3 FootprintSize => footprintSize;
        /// <summary>Lo colocó el jugador (no estaba en la escena): al cargar una partida se crea de nuevo.</summary>
        public bool PlacedInGame => stableId != null && stableId.StartsWith(PlacedPrefix);

        public const string PlacedPrefix = "colocado_";

        /// <summary>Le da su ID (al instalarlo desde un kit, o al cargar una partida) y se lo pasa a lo que guarda adentro.</summary>
        public void AssignId(string id)
        {
            stableId = id;
            foreach (StorageContainer storage in GetComponentsInChildren<StorageContainer>(true)) storage.SetStableId(id);
        }

        public static string NewId() => PlacedPrefix + System.Guid.NewGuid().ToString("N").Substring(0, 10);

        // Con la llave de montaje en la mano, un golpe lo levanta para moverlo.
        public bool OnToolHit(in ToolHit hit)
        {
            if (hit.Tool != GearId.Wrench || hit.By == null) return false;
            var placer = hit.By.GetComponent<PlacementController>();
            return placer != null && placer.BeginMove(this);
        }
    }
}
