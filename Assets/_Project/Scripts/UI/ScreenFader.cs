using System.Collections;
using UnityEngine;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// Fundido a negro de toda la pantalla, con un texto corto opcional ("8 horas después").
    /// Lo usan dormir y despertar después de desmayarse.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private float _alpha;
        private string _caption;
        private GUIStyle _style;

        public bool IsBlack => _alpha >= 0.999f;

        public IEnumerator FadeTo(float alpha, float seconds, string caption = null)
        {
            _caption = caption;
            float start = _alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _alpha = Mathf.Lerp(start, alpha, t / seconds);
                yield return null;
            }
            _alpha = alpha;
            if (alpha <= 0f) _caption = null;
        }

        // Dibuja con GUI.* (sin el maquetado automático de GUILayout): menos memoria nueva por cuadro (M13).
        private void Awake() => useGUILayout = false;

        private void OnGUI()
        {
            Matrix4x4 uiOld = UiScale.Begin(); // la interfaz en puntos, agrandada según la pantalla
            DrawGui();
            GUI.matrix = uiOld;
        }

        private void DrawGui()
        {
            if (_alpha <= 0f) return;
            GUI.depth = -1000;
            GUI.color = new Color(0f, 0f, 0f, _alpha);
            GUI.DrawTexture(new Rect(0f, 0f, UiScale.Width, UiScale.Height), Texture2D.whiteTexture);
            if (!string.IsNullOrEmpty(_caption))
            {
                if (_style == null) _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
                GUI.color = new Color(0.9f, 0.88f, 0.82f, _alpha);
                GUI.Label(new Rect(0f, UiScale.Height * 0.5f - 20f, UiScale.Width, 40f), _caption, _style);
            }
            GUI.color = Color.white;
        }
    }
}
