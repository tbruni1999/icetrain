using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// G1 — picar el mundo (`Docs/G1_Picar_Montanas_Plan.md`): los iconos provisorios de los materiales nuevos (piedra, mineral de
    /// hierro, carbón, chatarra, cable y piezas mecánicas). Un dibujito simple por material, como los de la V1; el arte final los
    /// reemplaza en el archivo de cada material.
    /// </summary>
    public static partial class TestScenesBuilder
    {
        // Una piedra: un bulto irregular hecho de discos, con luz arriba a la izquierda.
        private static void DrawRockShape(Texture2D t, Color c, int seed)
        {
            var random = new System.Random(seed);
            Color dark = c * 0.6f; dark.a = 1f;
            Color light = Color.Lerp(c, Color.white, 0.25f);
            int cx = t.width / 2, cy = t.height / 2, r = Mathf.Min(t.width, t.height) / 2 - 3;
            for (int k = 0; k < 7; k++)
            {
                float a = k / 7f * Mathf.PI * 2f;
                int dx = Mathf.RoundToInt(Mathf.Cos(a) * r * 0.45f), dy = Mathf.RoundToInt(Mathf.Sin(a) * r * 0.35f);
                FillDisc(t, cx + dx, cy + dy, Mathf.RoundToInt(r * (0.55f + (float)random.NextDouble() * 0.15f)), dark);
            }
            FillDisc(t, cx - 1, cy + 1, Mathf.RoundToInt(r * 0.7f), c);
            FillDisc(t, cx - r / 3, cy + r / 3, Mathf.RoundToInt(r * 0.3f), light);
        }

        private static void DrawStoneIcon(Texture2D t, Color c) => DrawRockShape(t, c, 3);

        // Mineral de hierro: una piedra gris con vetas y manchas marrón rojizo.
        private static void DrawOreIcon(Texture2D t, Color c)
        {
            DrawRockShape(t, new Color(0.45f, 0.44f, 0.43f), 5);
            Color vein = c;
            int cx = t.width / 2, cy = t.height / 2;
            for (int k = -10; k <= 10; k++) FillRect(t, cx + k, cy + k / 2 - 2, 3, 3, vein);
            FillDisc(t, cx - 8, cy - 6, 4, vein);
            FillDisc(t, cx + 7, cy + 8, 3, Color.Lerp(vein, Color.white, 0.2f));
        }

        // Carbón: tres pedazos negros con borde gris y reflejos (que se lea sobre el fondo oscuro del panel).
        private static void DrawCoalIcon(Texture2D t, Color c)
        {
            Color edge = new Color(0.42f, 0.43f, 0.46f);
            Color shine = new Color(0.62f, 0.64f, 0.7f);
            int[,] lumps = { { 16, 17, 11 }, { 32, 19, 10 }, { 24, 32, 10 } };
            for (int k = 0; k < 3; k++) FillDisc(t, lumps[k, 0], lumps[k, 1], lumps[k, 2] + 2, edge);
            for (int k = 0; k < 3; k++)
            {
                FillDisc(t, lumps[k, 0], lumps[k, 1], lumps[k, 2], c);
                FillDisc(t, lumps[k, 0] - 4, lumps[k, 1] + 4, 3, shine);
            }
        }

        // Chatarra: dos pedazos de chapa doblados y oxidados, uno sobre otro.
        private static void DrawScrapIcon(Texture2D t, Color c)
        {
            Color dark = c * 0.55f; dark.a = 1f;
            Color rust = new Color(0.62f, 0.34f, 0.18f);
            Color light = Color.Lerp(c, Color.white, 0.3f);
            for (int k = 0; k < t.width - 20; k += 2)
            {
                int bend = k < (t.width - 20) / 2 ? k / 4 : (t.width - 20) / 8 - (k - (t.width - 20) / 2) / 6;
                FillRect(t, 10 + k, 8 + bend, 3, 14, k % 14 < 9 ? c : rust);
                FillRect(t, 10 + k, 8 + bend + 13, 3, 2, dark);
            }
            for (int k = 0; k < t.width - 36; k += 2)
                FillRect(t, 18 + k, t.height - 22 - k / 6, 3, 12, k % 10 < 6 ? light : dark);
            FillDisc(t, 24, 15, 3, dark);
            FillDisc(t, t.width - 26, 17, 3, dark);
        }

        // Cable: un rollo de cable de cobre visto de frente (anillos).
        private static void DrawCableIcon(Texture2D t, Color c)
        {
            Color dark = c * 0.55f; dark.a = 1f;
            int cx = t.width / 2, cy = t.height / 2;
            FillDisc(t, cx, cy, 19, dark);
            FillDisc(t, cx, cy, 17, c);
            FillDisc(t, cx, cy, 13, dark);
            FillDisc(t, cx, cy, 11, c);
            FillDisc(t, cx, cy, 7, new Color(0f, 0f, 0f, 0f));
            FillRect(t, cx + 15, cy - 3, 9, 4, c); // la punta suelta
        }

        // Pieza mecánica: un engranaje con su agujero.
        private static void DrawGearIcon(Texture2D t, Color c)
        {
            Color dark = c * 0.55f; dark.a = 1f;
            int cx = t.width / 2, cy = t.height / 2;
            for (int k = 0; k < 8; k++)
            {
                float a = k / 8f * Mathf.PI * 2f;
                FillDisc(t, cx + Mathf.RoundToInt(Mathf.Cos(a) * 15f), cy + Mathf.RoundToInt(Mathf.Sin(a) * 15f), 5, dark);
            }
            FillDisc(t, cx, cy, 15, c);
            FillDisc(t, cx, cy, 6, dark);
            FillDisc(t, cx, cy, 4, new Color(0f, 0f, 0f, 0f));
        }
    }
}
