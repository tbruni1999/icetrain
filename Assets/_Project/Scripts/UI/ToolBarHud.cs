using SurvivalEnTren.Equipment;
using UnityEngine;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// La barra de herramientas (herramientas y forja, paso H1): una fila de casillas abajo al centro con su número y su nombre.
    /// La elegida en ámbar; las que el jugador todavía no tiene, apagadas. Bien visible un rato después de cambiar y después
    /// tenue, como las barras del cuerpo (sección 18: HUD mínimo).
    /// </summary>
    public class ToolBarHud : MonoBehaviour
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);

        [SerializeField] private ToolBelt belt;
        [Tooltip("Transparencia de la barra cuando no se está usando (0 = invisible, 1 = opaca).")]
        [SerializeField, Range(0f, 1f)] private float quietAlpha = 0.45f;
        [Tooltip("Opacidad de la placa oscura detrás de cada casilla (para leerla sobre la nieve).")]
        [SerializeField, Range(0f, 1f)] private float backingOpacity = 0.6f;

        private const float SlotWidth = 92f, SlotHeight = 34f, Gap = 6f;

        private GUIStyle _name, _number;
        private float _alpha = 1f;

        private void Awake() => useGUILayout = false;

        private void OnGUI()
        {
            if (belt == null || belt.Settings == null) return;
            if (_name == null)
            {
                _name = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
                _number = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.UpperLeft };
            }
            float target = belt.SinceChange < belt.Settings.BarHighlightSeconds ? 1f : quietAlpha;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.unscaledDeltaTime * 2f);

            ToolSlot[] slots = belt.Settings.Slots;
            float total = slots.Length * SlotWidth + (slots.Length - 1) * Gap;
            float x = (Screen.width - total) * 0.5f, y = Screen.height - 18f - SlotHeight;
            for (int i = 0; i < slots.Length; i++)
            {
                var r = new Rect(x + i * (SlotWidth + Gap), y, SlotWidth, SlotHeight);
                bool selected = belt.Selected == i, available = belt.Available(i);
                GUI.color = new Color(0.04f, 0.05f, 0.07f, backingOpacity * _alpha);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                if (selected)
                {
                    GUI.color = new Color(Amber.r, Amber.g, Amber.b, _alpha);
                    GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), Texture2D.whiteTexture);
                }
                Color text = selected ? Amber : available ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                GUI.color = new Color(text.r, text.g, text.b, text.a * _alpha);
                GUI.Label(new Rect(r.x + 4f, r.y + 2f, 20f, 14f), (i + 1).ToString(), _number);
                GUI.Label(r, available ? slots[i].label : "—", _name);
            }
            GUI.color = Color.white;
        }
    }
}
