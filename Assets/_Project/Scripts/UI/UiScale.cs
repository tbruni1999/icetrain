using UnityEngine;

namespace SurvivalEnTren.UI
{
    /// <summary>
    /// El tamaño de la interfaz (Tomi, 2026-10-16: "el UI de todo se ve extra chico"). Toda la interfaz se arma para una pantalla
    /// de referencia de 1280 x 720 y se agranda según la pantalla real: así se ve igual en un monitor de 1080 o en uno de 4K. El
    /// jugador lo ajusta en Opciones ("Tamaño de la interfaz"). Es solo una cuenta (no guarda estado de la partida).
    /// </summary>
    public static class UiScale
    {
        /// <summary>El alto de la pantalla de referencia: la interfaz se diseñó para 1280 x 720.</summary>
        public const float ReferenceHeight = 720f;
        public const float MinUser = 0.7f, MaxUser = 1.6f;

        private static float _user = 1f;

        /// <summary>El ajuste del jugador (1 = el tamaño de diseño).</summary>
        public static float User
        {
            get => _user;
            set => _user = Mathf.Clamp(value, MinUser, MaxUser);
        }

        /// <summary>Cuántos píxeles de la pantalla real mide un punto de la interfaz.</summary>
        public static float Factor => Mathf.Max(0.25f, Screen.height / ReferenceHeight * _user);

        /// <summary>El ancho y el alto de la pantalla en puntos de la interfaz.</summary>
        public static float Width => Screen.width / Factor;
        public static float Height => Screen.height / Factor;

        /// <summary>Al empezar un OnGUI: la interfaz pasa a dibujarse en puntos. Devuelve la matriz anterior (para dejarla al final).</summary>
        public static Matrix4x4 Begin()
        {
            Matrix4x4 old = GUI.matrix;
            float f = Factor;
            GUI.matrix = Matrix4x4.Scale(new Vector3(f, f, 1f));
            return old;
        }

        /// <summary>Una posición del mouse (píxeles, desde abajo a la izquierda) en puntos de la interfaz (desde arriba).</summary>
        public static Vector2 ScreenToGui(Vector2 screen) => new Vector2(screen.x, Screen.height - screen.y) / Factor;

        /// <summary>Un punto de la interfaz en píxeles de la pantalla (desde abajo a la izquierda, como el mouse).</summary>
        public static Vector2 GuiToScreen(Vector2 gui) => new Vector2(gui.x * Factor, Screen.height - gui.y * Factor);
    }
}
