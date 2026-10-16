using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.Crafting
{
    /// <summary>Qué parte de la forja es (cada una se usa con E por separado).</summary>
    public enum ForgePartRole
    {
        /// <summary>La boca del hogar: meter la chapa al fuego, sacarla al yunque.</summary>
        Mouth,
        /// <summary>La puerta del fogón: echar tablones.</summary>
        Firebox,
        /// <summary>La válvula del combustible del tanque.</summary>
        Valve,
        /// <summary>El yunque: E devuelve la pieza al fuego; se trabaja con el martillo en la mano (golpes).</summary>
        Anvil
    }

    /// <summary>
    /// Una parte de la forja que se usa con E o a golpes (H4). Cada parte tiene su collider: la boca del hogar, la puerta del fogón,
    /// la válvula y el yunque, así cada E hace una sola cosa clara.
    /// </summary>
    public class ForgePart : MonoBehaviour, IInteractable, IToolTarget
    {
        [SerializeField] private Forge forge;
        [SerializeField] private ForgePartRole role;

        public ForgePartRole Role => role;

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (forge == null || !forge.Ready) return null;
            var backpack = interactor.GetComponent<Backpack>();
            ForgeSettings s = forge.Settings;
            switch (role)
            {
                case ForgePartRole.Firebox:
                {
                    string fire = forge.WoodHours > 0f ? $"   (leña para {forge.WoodHours * 60f:0} min)" : forge.Burning ? "   (ardiendo con combustible)" : "   (apagada)";
                    if (!forge.CanAddWood) return $"el fogón está lleno{fire}";
                    if (backpack == null || backpack.CountOf(s.WoodItem) == 0) return $"echar leña: hace falta un tablón{fire}";
                    return $"echar un tablón al fuego{fire}";
                }
                case ForgePartRole.Valve:
                    return forge.ValveOpen ? "cerrar el combustible de la forja" : $"abrir el combustible de la forja   ({s.DieselLitersPerHour:0.#} L/h del tanque)";
                case ForgePartRole.Mouth:
                    if (forge.PieceAt == ForgePieceAt.Fire)
                        return forge.PieceHeat >= 0.95f ? "llevar la pieza al yunque   (al rojo vivo: lista)"
                            : forge.PieceHot ? $"llevar la pieza al yunque   (al rojo {forge.PieceHeat * 100f:0}%: se va a enfriar rápido)"
                            : $"llevar la pieza al yunque   (calentando {forge.PieceHeat * 100f:0}%)";
                    if (forge.PieceAt == ForgePieceAt.Anvil) return null;
                    if (backpack == null || backpack.CountOf(s.RawItem) == 0) return $"forja {Heat()}: hace falta una {s.RawItem.PieceName} para trabajar";
                    return $"meter una {s.RawItem.PieceName} al fuego   (forja {Heat()})";
                case ForgePartRole.Anvil:
                {
                    if (forge.PieceAt != ForgePieceAt.Anvil) return null;
                    string hits = $"{forge.PieceHits}/{s.HitsToForge}";
                    var belt = interactor.GetComponent<ToolBelt>();
                    bool hammer = belt != null && belt.ActiveTool == GearId.Hammer;
                    if (!forge.PieceHot) return $"se enfrió ({hits}): volver la pieza al fuego";
                    return hammer ? $"martillar la pieza al rojo ({hits})   ·   E: volver al fuego" : $"pieza al rojo ({hits}): martillala con el martillo (4)   ·   E: volver al fuego";
                }
            }
            return null;
        }

        private string Heat() => forge.Temperature >= 0.99f ? "a pleno" : forge.Temperature > 0.01f ? $"{forge.Temperature * 100f:0}%" : "fría";

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor)
        {
            if (forge == null || !forge.Ready) return;
            var backpack = interactor.GetComponent<Backpack>();
            switch (role)
            {
                case ForgePartRole.Firebox: forge.AddWood(backpack); break;
                case ForgePartRole.Valve: forge.SetValve(!forge.ValveOpen); break;
                case ForgePartRole.Mouth:
                    if (forge.PieceAt == ForgePieceAt.Fire) forge.MovePiece(ForgePieceAt.Anvil);
                    else if (forge.PieceAt == ForgePieceAt.None) forge.PutPiece(backpack);
                    break;
                case ForgePartRole.Anvil:
                    if (forge.PieceAt == ForgePieceAt.Anvil) forge.MovePiece(ForgePieceAt.Fire);
                    break;
            }
        }

        // Solo el yunque recibe golpes (el martillo); las otras partes no reaccionan.
        public bool OnToolHit(in ToolHit hit) => role == ForgePartRole.Anvil && forge != null && forge.Ready && forge.Hammer(in hit);
    }
}
