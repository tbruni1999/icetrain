using UnityEngine;

namespace SurvivalEnTren.Train
{
    /// <summary>
    /// Medidor de combustible de la cabina (sección 18.1: la información del tren va en el tren): una
    /// aguja que marca el nivel del tanque y una luz de reserva (roja fija con poco combustible, que
    /// parpadea con el tanque vacío). Solo dibuja; el estado vive en FuelTank.
    /// </summary>
    public class FuelGauge : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color LampOff = new Color(0.22f, 0.04f, 0.04f);
        private static readonly Color LampOn = new Color(1f, 0.18f, 0.08f);

        [SerializeField] private FuelTank tank;
        [SerializeField] private Transform needlePivot;
        [SerializeField] private Renderer lamp;
        [Tooltip("Ángulo de la aguja en vacío y en lleno, en grados hacia cada lado (vacío a la izquierda).")]
        [SerializeField] private float sweepDegrees = 60f;
        [Tooltip("Qué tan rápido se mueve la aguja hasta su posición (más alto = más brusco).")]
        [SerializeField] private float needleSpeed = 6f;
        [Tooltip("Parpadeos por segundo de la luz con el tanque vacío.")]
        [SerializeField] private float emptyBlinkHz = 2f;

        private float _angle;
        private bool _initialized;
        private MaterialPropertyBlock _block;

        private void LateUpdate()
        {
            float target = Mathf.Lerp(sweepDegrees, -sweepDegrees, tank.Fraction);
            if (!_initialized) { _angle = target; _initialized = true; }
            _angle = Mathf.Lerp(_angle, target, 1f - Mathf.Exp(-needleSpeed * Time.deltaTime));
            needlePivot.localRotation = Quaternion.Euler(0f, 0f, _angle);

            bool on = tank.IsEmpty ? Mathf.Repeat(Time.time * emptyBlinkHz, 1f) < 0.5f : tank.IsLow;
            _block ??= new MaterialPropertyBlock();
            _block.SetColor(BaseColor, on ? LampOn : LampOff);
            lamp.SetPropertyBlock(_block);
        }
    }
}
