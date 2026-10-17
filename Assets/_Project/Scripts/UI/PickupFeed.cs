using System.Collections.Generic;
using SurvivalEnTren.Items;
using UnityEngine;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// Lo que va entrando en la mochila al picar (G1, "grindeo a lo 7 Days"): una lista corta a la derecha de la pantalla, con lo
    /// que se juntó ("+3 piedra") y cuánto hay ahora en la mochila. Si algo no entra, avisa que cayó al piso. Cada renglón se
    /// suma mientras se sigue juntando lo mismo y se apaga solo después de unos segundos. Va en el jugador.
    /// </summary>
    public class PickupFeed : MonoBehaviour
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);

        [Tooltip("Cuántos segundos queda cada renglón (después se apaga).")]
        [SerializeField, Range(0.5f, 10f)] private float showSeconds = 3f;
        [Tooltip("Cuántos renglones se ven a la vez.")]
        [SerializeField, Range(1, 10)] private int maxLines = 5;
        [Tooltip("Opacidad de la placa oscura detrás de cada renglón (para leerlo sobre la nieve).")]
        [SerializeField, Range(0f, 1f)] private float backingOpacity = 0.55f;

        private sealed class Line
        {
            public ItemDefinition Item;
            public bool Dropped;
            public int Amount;
            public int Total;
            public float Until;
            public string Text;
        }

        private readonly List<Line> _lines = new List<Line>();
        private GUIStyle _style;

        /// <summary>Lo último que se mostró (para las pruebas).</summary>
        public string LastText => _lines.Count > 0 ? _lines[0].Text : "";

        private void Awake() => useGUILayout = false;

        /// <summary>Entraron "amount" piezas de un material a la mochila (ahora hay "total").</summary>
        public void Show(ItemDefinition item, int amount, int total) => Add(item, amount, total, false);

        /// <summary>"amount" piezas no entraron en la mochila y cayeron al piso.</summary>
        public void ShowDropped(ItemDefinition item, int amount) => Add(item, amount, 0, true);

        private void Add(ItemDefinition item, int amount, int total, bool dropped)
        {
            if (item == null || amount <= 0) return;
            Line line = null;
            foreach (Line l in _lines) if (l.Item == item && l.Dropped == dropped && Time.unscaledTime < l.Until) line = l;
            if (line == null)
            {
                line = new Line { Item = item, Dropped = dropped };
                _lines.Insert(0, line);
                if (_lines.Count > maxLines) _lines.RemoveAt(_lines.Count - 1);
            }
            line.Amount += amount;
            line.Total = total;
            line.Until = Time.unscaledTime + showSeconds;
            line.Text = dropped ? $"+{line.Amount} {item.DisplayName}: no entra, cayó al piso" : $"+{line.Amount} {item.DisplayName}   ({line.Total})";
        }

        private void OnGUI()
        {
            if (_lines.Count == 0) return;
            Matrix4x4 uiOld = UiScale.Begin(); // la interfaz en puntos, agrandada según la pantalla
            DrawGui();
            GUI.matrix = uiOld;
        }

        private void DrawGui()
        {
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleRight };
            const float width = 300f, height = 26f, gap = 4f;
            float x = UiScale.Width - width - 18f, y = UiScale.Height * 0.45f;
            for (int i = _lines.Count - 1; i >= 0; i--)
                if (Time.unscaledTime >= _lines[i].Until) _lines.RemoveAt(i);
            for (int i = 0; i < _lines.Count; i++)
            {
                Line line = _lines[i];
                float alpha = Mathf.Clamp01((line.Until - Time.unscaledTime) / 0.6f); // se apaga en el último medio segundo
                var r = new Rect(x, y + i * (height + gap), width, height);
                GUI.color = new Color(0.04f, 0.05f, 0.07f, backingOpacity * alpha);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = new Color(line.Item.Color.r, line.Item.Color.g, line.Item.Color.b, alpha);
                GUI.DrawTexture(new Rect(r.x, r.y, 4f, r.height), Texture2D.whiteTexture);
                Color text = line.Dropped ? Amber : Color.white;
                GUI.color = new Color(text.r, text.g, text.b, alpha);
                GUI.Label(new Rect(r.x + 8f, r.y, r.width - 16f, r.height), line.Text, _style);
            }
            GUI.color = Color.white;
        }
    }
}
