using UnityEngine;

namespace SurvivalEnTren.Audio
{
    /// <summary>
    /// Sonidos provisorios generados por código (M13): no hay archivos de audio todavía y no se agregan paquetes. Cada
    /// sonido se arma una sola vez al arrancar (ruido filtrado, tonos, golpes) y se reemplaza por el sonido final cuando
    /// exista: los componentes que los usan reciben un AudioClip, venga de donde venga.
    /// - Los "loops" (viento, motor, calefacción) empalman el final con el principio para que no se note el corte.
    /// - Mono, 22 050 muestras por segundo: livianos en memoria.
    /// </summary>
    public static class ProceduralClips
    {
        public const int Rate = 22050;

        // Un generador de números al azar propio y fijo: el mismo sonido cada vez (y sin depender de UnityEngine.Random).
        private sealed class Noise
        {
            private uint _state;
            public Noise(uint seed) { _state = seed == 0 ? 1u : seed; }
            public float Next()
            {
                _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5;
                return (_state & 0xFFFFFF) / (float)0xFFFFFF * 2f - 1f;
            }
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Funde el final con el principio (y lo recorta) para que el loop no tenga un salto.
        private static float[] Trimmed(float[] data, int fadeSamples)
        {
            var copy = (float[])data.Clone();
            int n = copy.Length;
            for (int i = 0; i < fadeSamples; i++)
            {
                float k = i / (float)fadeSamples;
                copy[i] = copy[i] * k + copy[n - fadeSamples + i] * (1f - k);
            }
            var result = new float[n - fadeSamples];
            System.Array.Copy(copy, result, result.Length);
            return result;
        }

        private static void Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            foreach (float v in data) max = Mathf.Max(max, Mathf.Abs(v));
            float k = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] *= k;
        }

        /// <summary>Viento afuera: ruido grave filtrado con ráfagas lentas (loop).</summary>
        public static AudioClip Wind(float seconds = 8f, uint seed = 11)
        {
            var noise = new Noise(seed);
            int n = Mathf.RoundToInt(seconds * Rate);
            var data = new float[n];
            float low = 0f, band = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = noise.Next();
                low += (white - low) * 0.02f;    // pasa-bajos: el "soplido"
                band += (white - band) * 0.12f;  // algo más agudo: el silbido de las ráfagas
                float gust = 0.55f + 0.3f * Mathf.Sin(t * 0.7f) + 0.15f * Mathf.Sin(t * 2.3f + 1f);
                data[i] = (low * 3.2f + (band - low) * 0.35f * gust) * gust;
            }
            Normalize(data, 0.8f);
            return Make("Viento", Trimmed(data, Rate / 2));
        }

        /// <summary>Un zumbido grave (motor o calefacción): una nota con armónicos y un poco de ruido (loop).</summary>
        public static AudioClip Hum(string name, float baseHz, float[] harmonics, float noiseAmount, float seconds = 2f, uint seed = 7)
        {
            var noise = new Noise(seed);
            int n = Mathf.RoundToInt(seconds * Rate);
            var data = new float[n];
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                for (int h = 0; h < harmonics.Length; h++) v += harmonics[h] * Mathf.Sin(2f * Mathf.PI * baseHz * (h + 1) * t);
                low += (noise.Next() - low) * 0.05f;
                data[i] = v + low * noiseAmount;
            }
            Normalize(data, 0.7f);
            return Make(name, Trimmed(data, Rate / 10));
        }

        /// <summary>Golpe metálico corto (palancas, cambios de vía, piezas que se aportan a una obra).</summary>
        public static AudioClip Clank(string name, float baseHz = 180f, float seconds = 0.35f, uint seed = 3)
        {
            var noise = new Noise(seed);
            int n = Mathf.RoundToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 18f);
                float ring = Mathf.Sin(2f * Mathf.PI * baseHz * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * baseHz * 2.76f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * baseHz * 5.4f * t);
                data[i] = (ring * 0.7f + noise.Next() * Mathf.Exp(-t * 60f)) * env;
            }
            Normalize(data, 0.9f);
            return Make(name, data);
        }

        /// <summary>Un golpe sordo (madera, bultos que caen, aportar a una obra).</summary>
        public static AudioClip Thud(string name, float seconds = 0.25f, uint seed = 5)
        {
            var noise = new Noise(seed);
            int n = Mathf.RoundToInt(seconds * Rate);
            var data = new float[n];
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                low += (noise.Next() - low) * 0.08f;
                data[i] = (low * 2f + Mathf.Sin(2f * Mathf.PI * 70f * t)) * Mathf.Exp(-t * 22f);
            }
            Normalize(data, 0.9f);
            return Make(name, data);
        }

        /// <summary>Hielo que se rompe: crujidos secos que se apagan.</summary>
        public static AudioClip Crack(string name, float seconds = 0.6f, uint seed = 9)
        {
            var noise = new Noise(seed);
            int n = Mathf.RoundToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float bursts = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 70f) * Mathf.Sin(t * 23f + 0.5f)), 6f);
                data[i] = noise.Next() * (bursts + 0.2f) * Mathf.Exp(-t * 6f);
            }
            Normalize(data, 0.85f);
            return Make(name, data);
        }

        /// <summary>Puerta corrediza: un roce que sube y baja.</summary>
        public static AudioClip Slide(string name, float seconds = 0.6f, uint seed = 13)
        {
            var noise = new Noise(seed);
            int n = Mathf.RoundToInt(seconds * Rate);
            var data = new float[n];
            float band = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                band += (noise.Next() - band) * 0.25f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds));
                data[i] = band * env * (0.8f + 0.2f * Mathf.Sin(t * 90f));
            }
            Normalize(data, 0.6f);
            return Make(name, data);
        }

        /// <summary>Un pitido electrónico corto (la sonda).</summary>
        public static AudioClip Beep(string name, float hz, float seconds = 0.08f)
        {
            int n = Mathf.RoundToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t * 400f) * Mathf.Min(1f, (seconds - t) * 300f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * hz * t) > 0f ? 0.5f : -0.5f) * env; // cuadrada: suena a aparato viejo
            }
            return Make(name, data);
        }
    }
}
