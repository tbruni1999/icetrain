using UnityEngine;

namespace SurvivalEnTren.Building
{
    /// <summary>
    /// Un lugar reservado donde no se puede colocar un mueble (va en un collider "trigger", que no se choca ni se apunta): donde
    /// el banco deja los kits, o donde va a ir una parte de una obra que todavía no se ve (el yunque de la forja). Las zonas de
    /// uso de las obras (la cama, la forja) también reservan su lugar, sin necesidad de esto.
    /// </summary>
    public class PlacementBlocker : MonoBehaviour
    {
        [Tooltip("Lo que se lee al intentar colocar algo encima.")]
        [SerializeField] private string reason = "ese lugar está reservado";

        public string Reason => reason;
    }
}
