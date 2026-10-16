using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Building
{
    public enum KitKind
    {
        /// <summary>Una fase de una obra con lugar fijo (la cama, la forja, la bifurcación): se instala mirando la obra.</summary>
        SitePhase,
        /// <summary>Un mueble que se coloca donde el jugador quiere (un estante): se instala con el clic.</summary>
        Placeable
    }

    /// <summary>
    /// Un kit de mejora (decisión de Tomi del 2026-10-16: las mejoras se arman en el banco, con un tiempo, y las instala el
    /// jugador). Es un cajón de verdad que se lleva en la mano con las dos manos y pesa lo que pesan sus materiales.
    /// - Kit de una obra: se lleva hasta la obra (la cama, la forja, la bifurcación) y se instala manteniendo E.
    /// - Kit de un mueble: mientras se lleva se ve el mueble como va a quedar; el clic lo instala (PlacementController).
    /// No se lanza: el clic es para instalar.
    /// </summary>
    [RequireComponent(typeof(Carryable))]
    public class UpgradeKit : MonoBehaviour, IItemDescription, IHeldHint
    {
        [SerializeField] private KitKind kind;
        [Tooltip("Kit de obra: el ID estable de la obra.")]
        [SerializeField] private string siteId;
        [Tooltip("Kit de obra: qué fase instala (0 = la primera).")]
        [SerializeField] private int phase;
        [Tooltip("Kit de mueble: qué mueble (la clave del plano).")]
        [SerializeField] private string placeableKey;
        [Tooltip("Kit de mueble: el mueble que se coloca.")]
        [SerializeField] private GameObject placeablePrefab;
        [Tooltip("Cómo se llama lo que trae (\"base de madera · cama improvisada\").")]
        [SerializeField] private string displayName = "kit";

        public KitKind Kind => kind;
        public string SiteId => siteId;
        public int Phase => phase;
        public string PlaceableKey => placeableKey;
        public GameObject PlaceablePrefab => placeablePrefab;
        public string DisplayName => displayName;

        /// <summary>Marca de los kits en la escena (no están al arrancar: al cargar una partida se crean de nuevo).</summary>
        public const string ObjectName = "Kit_Mejora";

        /// <summary>Cómo se guarda qué kit es ("obra:ID:fase" o "mueble:clave").</summary>
        public string SaveKey => kind == KitKind.SitePhase ? $"obra:{siteId}:{phase}" : $"mueble:{placeableKey}";

        public void ConfigureSite(string site, int sitePhase, string name)
        {
            kind = KitKind.SitePhase;
            siteId = site;
            phase = sitePhase;
            displayName = name;
        }

        public void ConfigurePlaceable(string key, GameObject prefab, string name)
        {
            kind = KitKind.Placeable;
            placeableKey = key;
            placeablePrefab = prefab;
            displayName = name;
        }

        public string Describe() => "kit: " + displayName;

        public string HeldHint() => kind == KitKind.Placeable ? "clic: instalar · rueda: girar" : "mantené E donde va";
    }
}
