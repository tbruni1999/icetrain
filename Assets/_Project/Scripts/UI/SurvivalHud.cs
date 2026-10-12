using SurvivalEnTren.Survival;
using UnityEngine;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// HUD mínimo de supervivencia (secciones 7 y 18.1): cuatro barras chicas en una esquina —
    /// vida, comida (saciedad), energía y calor del cuerpo —, discretas mientras todo está bien
    /// y más visibles cuando algo baja. Colores de la sección 18.4: blanco normal, ámbar atención,
    /// rojo peligro, azul frío cuando el cuerpo se está enfriando. Greybox: sin íconos todavía.
    /// </summary>
    public class SurvivalHud : MonoBehaviour
    {
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f);
        private static readonly Color Danger = new Color(0.95f, 0.25f, 0.2f);
        private static readonly Color Cold = new Color(0.55f, 0.75f, 1f);

        [SerializeField] private PlayerVitals vitals;
        [Tooltip("Por debajo de este valor una barra se muestra en ámbar.")]
        [SerializeField, Range(0f, 100f)] private float warningBelow = 35f;
        [Tooltip("Por debajo de este valor una barra se muestra en rojo.")]
        [SerializeField, Range(0f, 100f)] private float dangerBelow = 15f;
        [Tooltip("Si todas las barras están por encima de este valor, el HUD se vuelve casi transparente.")]
        [SerializeField, Range(0f, 100f)] private float quietAbove = 60f;
        [Tooltip("Transparencia del HUD cuando todo está bien (0 = invisible, 1 = opaco).")]
        [SerializeField, Range(0f, 1f)] private float quietAlpha = 0.35f;

        private GUIStyle _label;
        private float _alpha = 1f;

        private void OnGUI()
        {
            if (_label == null)
                _label = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft };

            float min = Mathf.Min(Mathf.Min(vitals.Health, vitals.Satiety), Mathf.Min(vitals.Energy, vitals.Warmth));
            float target = min >= quietAbove ? quietAlpha : 1f;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.unscaledDeltaTime * 2f);

            bool cooling = vitals.AmbientTemperatureC < vitals.Settings.ComfortTemperatureC;
            float x = 18f, y = Screen.height - 18f - 4 * 18f;
            Bar(ref y, x, "VIDA", vitals.Health, null);
            Bar(ref y, x, "COMIDA", vitals.Satiety, null);
            Bar(ref y, x, "ENERGÍA", vitals.Energy, null);
            Bar(ref y, x, "CALOR", vitals.Warmth, cooling ? Cold : (Color?)null);
            GUI.color = Color.white;
        }

        private void Bar(ref float y, float x, string name, float value, Color? normalColor)
        {
            const float width = 110f, height = 6f;
            Color fill = value < dangerBelow ? Danger : value < warningBelow ? Amber : normalColor ?? Color.white;

            // Sombra para que se lea sobre la nieve.
            GUI.color = new Color(0f, 0f, 0f, 0.6f * _alpha);
            GUI.Label(new Rect(x + 1f, y - 3f, 70f, 16f), name, _label);
            GUI.color = new Color(1f, 1f, 1f, 0.9f * _alpha);
            GUI.Label(new Rect(x, y - 4f, 70f, 16f), name, _label);
            GUI.color = new Color(0f, 0f, 0f, 0.45f * _alpha);
            GUI.DrawTexture(new Rect(x + 62f, y + 1f, width, height), Texture2D.whiteTexture);
            GUI.color = new Color(fill.r, fill.g, fill.b, _alpha);
            GUI.DrawTexture(new Rect(x + 62f, y + 1f, width * Mathf.Clamp01(value / 100f), height), Texture2D.whiteTexture);
            y += 18f;
        }
    }
}
