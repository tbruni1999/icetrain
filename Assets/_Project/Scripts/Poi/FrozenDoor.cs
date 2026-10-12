using SurvivalEnTren.Equipment;
using SurvivalEnTren.Interaction;
using UnityEngine;

namespace SurvivalEnTren.Poi
{
    /// <summary>
    /// Hielo que traba una puerta (secciones 6.3 y 20.4): "el marco de hielo comunica el problema sin
    /// texto. La barreta permite desprenderlo. Enseña que el mundo contiene obstáculos físicos
    /// solucionables". Sin barreta, el hielo no cede; con ella, mantener E lo va rompiendo: el hielo se
    /// adelgaza hasta soltarse en pedazos y deja ver la puerta, que se abre como cualquier otra.
    /// - El avance se conserva si se suelta E (no se pierde por mirar a otro lado).
    /// - Va en el objeto del hielo (un grupo con los bloques de hielo, con colliders).
    /// - El ID estable es para el guardado futuro (puertas ya abiertas, Anexo D.2).
    /// </summary>
    public class FrozenDoor : MonoBehaviour, IChanneledInteractable
    {
        [SerializeField] private string stableId;
        [Tooltip("Cuántos segundos hay que mantener E con la barreta para romper el hielo.")]
        [SerializeField, Range(0.5f, 30f)] private float breakSeconds = 4f;
        [Tooltip("Qué tan delgado queda el hielo justo antes de romperse (fracción del grosor original).")]
        [SerializeField, Range(0.05f, 1f)] private float thinnestScale = 0.35f;
        [Tooltip("Cuántos pedazos salen al romperse.")]
        [SerializeField, Range(0, 40)] private int shardCount = 12;

        private float _progress;
        private Renderer _iceRenderer;

        public string StableId => stableId;
        public bool IsBroken { get; private set; }
        /// <summary>Cuánto del hielo está roto, de 0 a 1.</summary>
        public float Progress => _progress;

        private void Awake() => _iceRenderer = GetComponentInChildren<Renderer>();

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (IsBroken) return null;
            var equipment = interactor.GetComponent<PlayerGear>();
            if (equipment == null || !equipment.Has(GearId.Crowbar)) return "el hielo la traba: hace falta una barreta";
            return _progress > 0.01f ? $"mantener para romper el hielo   ({_progress * 100f:0}%)" : "mantener para romper el hielo";
        }

        public float GetHoldSeconds(PlayerInteractor interactor) => 0f;

        public void Interact(PlayerInteractor interactor) { } // todo pasa en Channel

        public bool Channel(PlayerInteractor interactor, float deltaTime)
        {
            var equipment = interactor.GetComponent<PlayerGear>();
            if (IsBroken || equipment == null || !equipment.Has(GearId.Crowbar)) return false;

            _progress = Mathf.Min(1f, _progress + deltaTime / breakSeconds);
            transform.localScale = new Vector3(Mathf.Lerp(1f, thinnestScale, _progress), 1f, 1f);
            if (_progress < 1f) return true;

            Break();
            return false;
        }

        private void Break()
        {
            IsBroken = true;
            Material material = _iceRenderer != null ? _iceRenderer.sharedMaterial : null;
            for (int i = 0; i < shardCount; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Pedazo_Hielo";
                float size = Random.Range(0.07f, 0.16f);
                shard.transform.localScale = Vector3.one * size;
                shard.transform.position = transform.TransformPoint(new Vector3(Random.Range(0f, 0.3f), Random.Range(0.1f, 2.1f), Random.Range(-0.55f, 0.55f)));
                shard.transform.rotation = Random.rotation;
                if (material != null) shard.GetComponent<MeshRenderer>().sharedMaterial = material;
                var body = shard.AddComponent<Rigidbody>();
                body.mass = 0.2f;
                body.linearVelocity = transform.right * Random.Range(0.5f, 2f) + Vector3.up * Random.Range(0.5f, 1.5f) + transform.forward * Random.Range(-1f, 1f);
                Destroy(shard, 8f);
            }
            Destroy(gameObject);
        }
    }
}
