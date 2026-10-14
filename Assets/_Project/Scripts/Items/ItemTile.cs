using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Cómo se dibuja una pieza suelta en pantalla (fondo del color del material, su imagen y, si no tiene imagen, el nombre):
    /// lo mismo que muestra el inventario, para que una pieza se vea igual en la mochila, en un estante y en el banco de trabajo.
    /// </summary>
    public static class ItemTile
    {
        /// <summary>Dibuja la pieza en el rectángulo. "alpha" menor a 1 la deja translúcida (una pieza que falta).</summary>
        public static void Draw(Rect r, ItemDefinition item, float alpha, GUIStyle nameStyle)
        {
            GUI.color = new Color(item.Color.r * 0.35f, item.Color.g * 0.35f, item.Color.b * 0.35f, 0.85f * alpha);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            Outline(r, new Color(item.Color.r, item.Color.g, item.Color.b, 0.9f * alpha), 1.5f);

            Sprite icon = item.Icon;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            if (icon != null)
                GUI.DrawTexture(new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f), icon.texture, ScaleMode.StretchToFill, true);
            else
                GUI.Label(r, item.PieceName, nameStyle);
            GUI.color = Color.white;
        }

        /// <summary>Un recuadro vacío con borde (para marcar selección o una casilla).</summary>
        public static void Outline(Rect r, Color c, float t)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
