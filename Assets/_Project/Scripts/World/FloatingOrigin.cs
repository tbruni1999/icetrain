using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivalEnTren.World
{
    /// <summary>
    /// Estrategia C del spike (Anexo C.1, "híbrido + floating origin"): el tren se mueve por el
    /// mundo como en la estrategia A, pero cuando el jugador se aleja demasiado del origen
    /// (0,0,0) se mueve todo el mundo de golpe para que vuelva a quedar cerca. Así los números
    /// de posición se mantienen chicos y no se pierde precisión (medido: A sola falla desde ~5 km).
    ///
    /// Mueve todos los objetos raíz de las escenas cargadas, sincroniza la física y avisa a los
    /// componentes que guardan posiciones por su cuenta (IOriginShiftListener).
    /// Lleva la cuenta de dónde quedó el origen en coordenadas absolutas (en doble precisión),
    /// por si hiciera falta; el guardado usa la posición ferroviaria (tramo + distancia).
    /// Corre antes que el jugador y el tren en cada tic de física (orden -200).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class FloatingOrigin : MonoBehaviour
    {
        [SerializeField] private FloatingOriginSettings settings;
        [Tooltip("Lo que no debe alejarse del origen: el jugador.")]
        [SerializeField] private Transform reference;

        /// <summary>Se dispara después de cada re-centrado, con el desplazamiento aplicado.</summary>
        public event Action<Vector3> Shifted;

        public int ShiftCount { get; private set; }
        public float LastShiftMilliseconds { get; private set; }
        /// <summary>Posición absoluta del origen actual (en metros, doble precisión).</summary>
        public double AbsoluteOriginX { get; private set; }
        public double AbsoluteOriginZ { get; private set; }

        private void FixedUpdate()
        {
            Vector3 position = reference.position;
            position.y = 0f;
            if (position.magnitude > settings.RecenterDistanceMeters)
                Shift(-position);
        }

        /// <summary>Mueve todo el mundo "delta" metros.</summary>
        public void Shift(Vector3 delta)
        {
            var timer = Stopwatch.StartNew();

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                    root.transform.position += delta;
            }
            Physics.SyncTransforms();

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (IOriginShiftListener listener in root.GetComponentsInChildren<IOriginShiftListener>(true))
                    listener.OnOriginShifted(delta);
            }

            AbsoluteOriginX -= delta.x;
            AbsoluteOriginZ -= delta.z;
            ShiftCount++;
            LastShiftMilliseconds = (float)timer.Elapsed.TotalMilliseconds;
            Shifted?.Invoke(delta);
        }
    }
}
