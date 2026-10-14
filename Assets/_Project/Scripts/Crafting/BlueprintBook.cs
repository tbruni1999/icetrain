using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEnTren.Crafting
{
    /// <summary>
    /// Los planos que conoce el jugador (secciones 10.3 y 22.2, "Blueprint Registry"): los iniciales (se saben desde el
    /// arranque) más los que fue encontrando. Solo guarda el ID de cada plano aprendido (para el guardado futuro, sin
    /// duplicar). Al aprender uno nuevo muestra un aviso breve arriba en la pantalla.
    /// </summary>
    public class BlueprintBook : MonoBehaviour
    {
        private readonly HashSet<string> _learned = new HashSet<string>();
        private string _notice = "";
        private float _noticeUntil;
        private GUIStyle _style;
        private Texture2D _plate;

        /// <summary>Se dispara cuando se aprende un plano nuevo.</summary>
        public event Action Changed;

        public IEnumerable<string> LearnedIds => _learned;

        public bool Knows(Blueprint blueprint) => blueprint != null && (blueprint.KnownFromStart || _learned.Contains(blueprint.Id));

        /// <summary>Deja el libro con estos planos aprendidos, sin aviso (al cargar una partida).</summary>
        public void Restore(IEnumerable<string> ids)
        {
            _learned.Clear();
            foreach (string id in ids) if (!string.IsNullOrEmpty(id)) _learned.Add(id);
            Changed?.Invoke();
        }

        /// <summary>Aprende un plano. Devuelve false si ya lo sabía (no se aprende dos veces).</summary>
        public bool Learn(Blueprint blueprint)
        {
            if (blueprint == null || Knows(blueprint)) return false;
            _learned.Add(blueprint.Id);
            _notice = $"PLANO NUEVO   ·   {blueprint.DisplayName}";
            _noticeUntil = Time.unscaledTime + 5f;
            Changed?.Invoke();
            return true;
        }

        private void OnGUI()
        {
            if (Time.unscaledTime >= _noticeUntil) return;
            if (_style == null)
            {
                _plate = new Texture2D(1, 1);
                _plate.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.1f));
                _plate.Apply();
                _style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            }
            float fade = Mathf.Clamp01((_noticeUntil - Time.unscaledTime) / 0.6f);
            var rect = new Rect(Screen.width * 0.5f - 260f, 56f, 520f, 44f);
            GUI.color = new Color(1f, 1f, 1f, 0.85f * fade);
            GUI.DrawTexture(rect, _plate);
            _style.normal.textColor = new Color(1f, 0.72f, 0.2f, fade);
            GUI.color = Color.white;
            GUI.Label(rect, _notice, _style);
        }
    }
}
