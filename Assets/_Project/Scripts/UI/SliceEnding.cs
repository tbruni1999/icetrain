using SurvivalEnTren.Rail;
using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// El final de la vertical slice (M13; secciones 21.2 y 25.3: "reparar el cambio, accionar la palanca y entrar por primera
    /// vez a la ruta desconocida"; "al ver al tren entrar en una vía desconocida, el jugador debería querer saber qué hay
    /// después"). La primera vez que el tren se mete en la ruta desconocida, la pantalla se funde suave a blanco nieve, aparece
    /// el nombre de la ruta y "continuará", y vuelve. El juego no se detiene: se puede seguir manejando.
    /// </summary>
    public class SliceEnding : MonoBehaviour
    {
        [SerializeField] private TrainMover train;
        [SerializeField] private RailSegment unknownRoute;
        [Tooltip("Cuántos metros adentro de la ruta tiene que estar el centro del tren para que aparezca.")]
        [SerializeField, Range(10f, 800f)] private float triggerMeters = 120f;
        [SerializeField] private string title = "RUTA DESCONOCIDA";
        [SerializeField] private string subtitle = "continuará";
        [Tooltip("Cuánto dura todo, en segundos (fundido, texto y vuelta).")]
        [SerializeField, Range(4f, 30f)] private float seconds = 11f;

        private float _t = -1f;
        private GUIStyle _big, _small;

        /// <summary>Ya se mostró (una vez por partida).</summary>
        public bool Shown { get; private set; }
        public bool Showing => _t >= 0f;

        private void Update()
        {
            if (!Shown && train != null && unknownRoute != null && train.RailPosition.Segment == unknownRoute && train.RailPosition.Distance > triggerMeters)
            {
                Shown = true;
                _t = 0f;
            }
            if (_t < 0f) return;
            _t += Time.unscaledDeltaTime;
            if (_t > seconds) _t = -1f;
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
            if (_t < 0f) return;
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            float fadeIn = Mathf.Clamp01(_t / 2.5f);
            float fadeOut = Mathf.Clamp01((seconds - _t) / 2.5f);
            float veil = Mathf.Min(fadeIn, fadeOut);
            GUI.color = new Color(0.9f, 0.93f, 0.96f, 0.8f * veil);
            GUI.DrawTexture(new Rect(0f, 0f, UiScale.Width, UiScale.Height), Texture2D.whiteTexture);
            float text = Mathf.Min(Mathf.Clamp01((_t - 1.5f) / 1.5f), fadeOut);
            GUI.color = Color.white;
            _big.normal.textColor = new Color(0.12f, 0.14f, 0.18f, text);
            _small.normal.textColor = new Color(0.25f, 0.28f, 0.33f, text);
            GUI.Label(new Rect(0f, UiScale.Height * 0.42f, UiScale.Width, 50f), title, _big);
            GUI.Label(new Rect(0f, UiScale.Height * 0.42f + 54f, UiScale.Width, 30f), subtitle, _small);
        }
    }
}
