using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace SurvivalEnTren.EditorTools
{
    /// <summary>
    /// Arte (entrega del chat de arte, 2026-10-14): ajustes de importación y materiales de la librería MR-04.
    /// - Las texturas que entran en Assets/_Project/Art se configuran solas según el final del nombre:
    ///   _Normal = mapa de normales; _MetalSmooth = datos, sin sRGB (R metálico, G oclusión, A suavidad); _BaseColor = color.
    /// - Los modelos (FBX) de esa carpeta se importan sin cámaras, luces, animación ni colliders: la colisión del tren sigue
    ///   siendo la del greybox (medida en el M1). Con los ejes ya convertidos, para que las piezas móviles giren limpias.
    /// - "Survival en tren > Arte > Crear materiales y conectar modelos": un material URP Lit por cada juego de texturas
    ///   (mismo nombre, MT_...) con los especiales de la guía de arte (rejilla recortada; vidrio, escarcha, hielo y calcos
    ///   transparentes; emisivo de la estufa) y conecta cada material de cada FBX al suyo, por nombre.
    /// Un juego de texturas es una carpeta "Texturas" (sus materiales van a la carpeta "Materiales" de al lado) o la
    /// librería entera (Art/Libreria, materiales en Art/Libreria/Materiales). Cada modelo usa primero los de su carpeta y
    /// después los de la librería.
    /// </summary>
    public class ArtImport : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/_Project/Art";
        private const string LibraryRoot = ArtRoot + "/Libreria";

        private static bool InArt(string path) => path.Replace('\\', '/').StartsWith(ArtRoot + "/");

        private void OnPreprocessTexture()
        {
            if (!InArt(assetPath)) return;
            var importer = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            if (name.EndsWith("_Normal"))
            {
                importer.textureType = TextureImporterType.NormalMap;
            }
            else if (name.EndsWith("_MetalSmooth"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
            }
            else if (name.EndsWith("_BaseColor"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            }
        }

        private void OnPreprocessModel()
        {
            if (!InArt(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            importer.importCameras = false;
            importer.importLights = false;
            // Los brazos en primera persona (rig de Tomi, 2026-10-16) traen las poses de los dedos como animaciones de un
            // cuadro: el armado de la escena las lee. El resto del arte no se anima.
            bool rig = Path.GetFileNameWithoutExtension(assetPath) == "BrazosFPS";
            importer.importAnimation = rig;
            importer.animationType = rig ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
            importer.addCollider = false;
            importer.generateSecondaryUV = false; // los meshes ya traen el segundo canal de UV
            importer.bakeAxisConversion = true;
            importer.isReadable = false;
        }

        [MenuItem("Survival en tren/Arte/Crear materiales y conectar modelos")]
        public static void CreateMaterialsAndRemap()
        {
            int created = 0, updated = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (!file.EndsWith("_BaseColor")) continue;
                string matName = file.Substring(0, file.Length - "_BaseColor".Length);
                string folder = MaterialsFolderFor(path);
                if (folder == null) continue;
                bool isNew = BuildMaterial(folder, matName, Path.GetDirectoryName(path).Replace('\\', '/'), path) == null;
                if (isNew) created++; else updated++;
            }
            AssetDatabase.SaveAssets();

            int models = 0, missing = 0;
            var missingNames = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                var names = new HashSet<string>(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m => m.name));
                foreach (var pair in importer.GetExternalObjectMap())
                    if (pair.Key.type == typeof(Material)) names.Add(pair.Key.name);
                string own = Path.GetDirectoryName(path).Replace('\\', '/') + "/Materiales";
                foreach (string name in names)
                {
                    Material mat = FindMaterial(own, name) ?? FindMaterial(LibraryRoot + "/Materiales", name)
                        ?? FindMaterial(LibraryRoot + "/Materiales", Compact(name)) ?? BuildPlainMaterial(own, name);
                    if (mat.name != name && mat.name != Compact(name)) { missing++; missingNames.Add(name); }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
                }
                importer.SaveAndReimport();
                models++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Arte] materiales nuevos {created}, actualizados {updated}; modelos conectados {models}" +
                      (missingNames.Count > 0 ? $"; sin texturas (material liso): {string.Join(", ", missingNames)}" : ""));
        }

        // "MT_Nieve_Compacta" -> "MT_NieveCompacta": el vagón v03 usa algunos nombres con guion bajo; la librería, sin.
        private static string Compact(string name) => name.StartsWith("MT_") ? "MT_" + name.Substring(3).Replace("_", "") : name;

        private static string MaterialsFolderFor(string texturePath)
        {
            texturePath = texturePath.Replace('\\', '/');
            int t = texturePath.IndexOf("/Texturas/");
            if (t >= 0) return texturePath.Substring(0, t) + "/Materiales";
            if (texturePath.StartsWith(LibraryRoot + "/")) return LibraryRoot + "/Materiales";
            return null;
        }

        private static Material FindMaterial(string folder, string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{folder}/{name}.mat");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static Texture2D Sibling(string textureFolder, string matName, string suffix)
        {
            foreach (string ext in new[] { ".png", ".jpg", ".jpeg", ".tga" })
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{textureFolder}/{matName}_{suffix}{ext}");
                if (t != null) return t;
            }
            return null;
        }

        // Crea o actualiza el material. Devuelve el que ya existía (null si es nuevo).
        private static Material BuildMaterial(string folder, string matName, string textureFolder, string baseColorPath)
        {
            EnsureFolder(folder);
            string path = $"{folder}/{matName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            Material mat = existing ?? new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = matName };
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");

            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
            Texture2D normal = Sibling(textureFolder, matName, "Normal");
            Texture2D metalSmooth = Sibling(textureFolder, matName, "MetalSmooth");
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.SetTexture("_MetallicGlossMap", metalSmooth);
            mat.SetFloat("_Metallic", metalSmooth != null ? 1f : 0f);
            mat.SetFloat("_Smoothness", metalSmooth != null ? 1f : 0.35f); // con mapa: multiplica la suavidad del canal A
            mat.SetFloat("_SmoothnessTextureChannel", 0f);                    // suavidad = alfa del mapa metálico
            mat.SetTexture("_OcclusionMap", metalSmooth);                     // la oclusión va en el canal G
            mat.SetFloat("_OcclusionStrength", 1f);
            ApplySpecials(mat, matName, baseMap);

            if (existing == null) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return existing;
        }

        private static Material BuildPlainMaterial(string folder, string matName)
        {
            EnsureFolder(folder);
            string path = $"{folder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = mat == null;
            if (isNew) mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = matName };
            mat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f));
            mat.SetFloat("_Smoothness", 0.3f);
            ApplySpecials(mat, matName, null);
            if (isNew) AssetDatabase.CreateAsset(mat, path); else EditorUtility.SetDirty(mat);
            return mat;
        }

        // Especiales de la guía de arte (por nombre, sin importar guiones bajos).
        private static void ApplySpecials(Material mat, string matName, Texture2D baseMap)
        {
            string key = Compact(matName);
            bool transparent = key == "MT_Vidrio" || key == "MT_VidrioSucio" || key == "MT_Escarcha" || key == "MT_Hielo" || key.StartsWith("MT_Decal");
            mat.SetFloat("_Surface", transparent ? 1f : 0f);       // 0 opaco, 1 transparente
            mat.SetFloat("_Blend", 0f);                             // mezcla alfa
            mat.SetFloat("_AlphaClip", key == "MT_Rejilla" ? 1f : 0f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.SetColor("_EmissionColor", Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            if (key == "MT_Hielo")
            {
                mat.SetColor("_BaseColor", new Color(0xBD / 255f, 0xD9 / 255f, 0xEB / 255f, 0.6f));
                if (mat.GetTexture("_MetallicGlossMap") == null) mat.SetFloat("_Smoothness", 0.9f);
            }
            if (key == "MT_Emisivo") // la estufa: el único punto naranja
            {
                mat.SetColor("_EmissionColor", new Color(1f, 0xD9 / 255f, 0x7A / 255f));
                mat.SetTexture("_EmissionMap", baseMap);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (key == "MT_PantallaRadar") // la pantalla de la sonda: verde apagado (el juego dibuja encima)
            {
                mat.SetColor("_BaseColor", new Color(0.02f, 0.06f, 0.035f));
                mat.SetColor("_EmissionColor", new Color(0.03f, 0.12f, 0.06f));
                mat.SetFloat("_Smoothness", 0.85f);
            }
            BaseShaderGUI.SetMaterialKeywords(mat, LitGUI.SetMaterialKeywords);
        }
    }
}
