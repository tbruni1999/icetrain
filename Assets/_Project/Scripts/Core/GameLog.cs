using UnityEngine;

namespace SurvivalEnTren.Core
{
    /// <summary>
    /// Log básico del juego. Todos los mensajes salen con la etiqueta [Categoría]
    /// para poder filtrarlos en la ventana Console de Unity.
    /// Es una clase estática simple (no guarda estado), no un "manager".
    /// </summary>
    public static class GameLog
    {
        /// <summary>Si es falso, los mensajes de información se apagan (errores y avisos siguen).</summary>
        public static bool InfoEnabled = true;

        public static void Info(string category, string message, Object context = null)
        {
            if (!InfoEnabled) return;
            Debug.Log(Format(category, message), context);
        }

        public static void Warning(string category, string message, Object context = null)
        {
            Debug.LogWarning(Format(category, message), context);
        }

        public static void Error(string category, string message, Object context = null)
        {
            Debug.LogError(Format(category, message), context);
        }

        private static string Format(string category, string message)
        {
            return $"[{category}] {message}";
        }
    }
}
