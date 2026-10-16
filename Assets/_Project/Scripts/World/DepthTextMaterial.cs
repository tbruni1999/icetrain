using UnityEngine;

namespace SurvivalEnTren.World
{
    /// <summary>
    /// Los carteles de texto (TextMesh) de este objeto y sus hijos, con un material que respeta lo que tiene adelante (el de Unity
    /// se dibuja encima de todo: se veían a través de la nieve y las paredes). La letra es "dinámica": cuando Unity rehace su
    /// textura, el material la vuelve a tomar.
    /// </summary>
    public class DepthTextMaterial : MonoBehaviour
    {
        [Tooltip("Un material con el shader SurvivalEnTren/TextoConProfundidad.")]
        [SerializeField] private Material material;

        private Material _instance;
        private Font _font;

        private void Awake()
        {
            TextMesh[] texts = GetComponentsInChildren<TextMesh>(true);
            if (texts.Length == 0 || material == null) return;
            _font = texts[0].font;
            _instance = new Material(material) { name = material.name + " (letra)" };
            Refresh(_font);
            foreach (TextMesh text in texts)
                if (text.font == _font) text.GetComponent<MeshRenderer>().sharedMaterial = _instance;
            Font.textureRebuilt += Refresh;
        }

        private void OnDestroy() => Font.textureRebuilt -= Refresh;

        private void Refresh(Font font)
        {
            if (font == _font && _instance != null && font != null) _instance.mainTexture = font.material.mainTexture;
        }
    }
}
