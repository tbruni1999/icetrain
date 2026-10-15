using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// El panel de opciones (producción P3), compartido por el menú principal y el de pausa: volumen, sensibilidad del mouse,
    /// calidad y pantalla completa. Se dibuja dentro del espacio del menú que lo usa (OnGUI, sin GUILayout) y se maneja con el
    /// mouse (clic en la barra o en el valor) o con el teclado (flechas arriba/abajo para elegir, izquierda/derecha para cambiar,
    /// Enter en "Volver"). Al cambiar algo se guarda y se aplica enseguida.
    /// </summary>
    public class OptionsPanel
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);
        private static readonly string[] Labels = { "Volumen", "Sensibilidad del mouse", "Calidad", "Pantalla completa", "Volver" };
        private const int BackRow = 4;

        private readonly Func<int, Rect> _rowRect;
        private GameOptions _options;
        private Action<GameOptions> _apply;
        private int _row;

        public bool IsOpen { get; private set; }
        public GameOptions Options => _options;

        /// <param name="rowRect">Dónde va cada fila, en el espacio del menú que lo dibuja.</param>
        public OptionsPanel(Func<int, Rect> rowRect) => _rowRect = rowRect;

        public void Open(GameOptions options, Action<GameOptions> apply)
        {
            _options = options;
            _apply = apply;
            _row = 0;
            IsOpen = true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _options.Save();
        }

        private void Changed()
        {
            _apply?.Invoke(_options);
            _options.Save();
        }

        // Cambia el valor de una fila un paso (dir = -1 o +1) o lo pone en "t" (0 a 1) si se hizo clic en la barra.
        private void Adjust(int row, int dir, float? t = null)
        {
            switch (row)
            {
                case 0: _options.volume = Mathf.Clamp01(t ?? _options.volume + dir * 0.1f); break;
                case 1: _options.sensitivity = Mathf.Clamp(t.HasValue ? Mathf.Lerp(0.3f, 3f, t.Value) : _options.sensitivity + dir * 0.1f, 0.3f, 3f); break;
                case 2:
                    int q = t.HasValue ? Mathf.RoundToInt(t.Value * 2f) : (int)_options.quality + dir;
                    _options.quality = (GameOptions.Quality)Mathf.Clamp(q, 0, 2);
                    break;
                case 3: _options.fullscreen = t.HasValue ? !_options.fullscreen : dir > 0; break;
                default: return;
            }
            Changed();
        }

        private static Rect ControlRect(Rect row) => new Rect(row.x + row.width * 0.52f, row.y + 8f, row.width * 0.44f, row.height - 16f);

        /// <summary>Teclado y mouse. Devuelve true cuando se eligió "Volver" (el menú que lo usa lo cierra).</summary>
        public bool HandleInput(Vector2 mouseInMenu)
        {
            if (!IsOpen) return false;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame) _row = (_row + Labels.Length - 1) % Labels.Length;
                if (keyboard.downArrowKey.wasPressedThisFrame) _row = (_row + 1) % Labels.Length;
                if (keyboard.leftArrowKey.wasPressedThisFrame) Adjust(_row, -1);
                if (keyboard.rightArrowKey.wasPressedThisFrame) Adjust(_row, +1);
                if (keyboard.enterKey.wasPressedThisFrame)
                {
                    if (_row == BackRow) { Close(); return true; }
                    if (_row == 3) Adjust(3, 0, 0f);
                }
            }
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                for (int i = 0; i < Labels.Length; i++)
                {
                    Rect r = _rowRect(i);
                    if (!r.Contains(mouseInMenu)) continue;
                    _row = i;
                    if (i == BackRow) { Close(); return true; }
                    Rect c = ControlRect(r);
                    Adjust(i, 0, Mathf.Clamp01((mouseInMenu.x - c.x) / c.width));
                }
            }
            return false;
        }

        private static string QualityName(GameOptions.Quality q) => q == GameOptions.Quality.Alta ? "alta" : q == GameOptions.Quality.Media ? "media" : "baja";

        /// <summary>Dibuja las filas (dentro del GUI.matrix del menú).</summary>
        public void Draw(Vector2 mouseInMenu, GUIStyle item, GUIStyle value)
        {
            for (int i = 0; i < Labels.Length; i++)
            {
                Rect r = _rowRect(i);
                bool selected = i == _row || r.Contains(mouseInMenu);
                GUI.color = new Color(1f, 1f, 1f, selected ? 0.12f : 0.04f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Color.white;
                item.normal.textColor = selected ? Amber : Color.white;
                if (i == BackRow)
                {
                    item.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(r, Labels[i], item);
                    continue;
                }
                item.alignment = TextAnchor.MiddleLeft;
                GUI.Label(new Rect(r.x + 14f, r.y, r.width * 0.5f, r.height), Labels[i], item);
                Rect c = ControlRect(r);
                float fill = i == 0 ? _options.volume : i == 1 ? Mathf.InverseLerp(0.3f, 3f, _options.sensitivity) : -1f;
                if (fill >= 0f)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.15f);
                    GUI.DrawTexture(new Rect(c.x, c.center.y - 3f, c.width, 6f), Texture2D.whiteTexture);
                    GUI.color = selected ? Amber : new Color(1f, 1f, 1f, 0.8f);
                    GUI.DrawTexture(new Rect(c.x, c.center.y - 3f, c.width * fill, 6f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(c.x + c.width * fill - 4f, c.center.y - 9f, 8f, 18f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                else
                {
                    value.normal.textColor = selected ? Amber : Color.white;
                    string text = i == 2 ? $"‹  {QualityName(_options.quality)}  ›" : (_options.fullscreen ? "‹  sí  ›" : "‹  no  ›");
                    GUI.Label(c, text, value);
                }
            }
            item.alignment = TextAnchor.MiddleCenter;
        }
    }
}
