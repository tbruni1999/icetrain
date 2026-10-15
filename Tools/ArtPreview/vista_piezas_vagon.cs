// Vista previa del arte (no es una prueba del juego): fotos desde arriba del interior del vagón MR-04 mostrando de a una las
// piezas de muebles, para ubicar qué trae cada una. Las deja en Temp/ArtPreview/pieza_*.png.
// Correr después de vista_previa_vagon.cs (usa su escena: el objeto "Vagon").
//   unity command eval_file --file "Tools/ArtPreview/vista_piezas_vagon.cs" --timeout 60

var holder = UnityEngine.GameObject.Find("Vagon");
var renderers = holder.GetComponentsInChildren<UnityEngine.Renderer>(true);
var cam = UnityEngine.Camera.main;
var rt = new UnityEngine.RenderTexture(1600, 700, 24);
cam.targetTexture = rt;
var tex = new UnityEngine.Texture2D(1600, 700, UnityEngine.TextureFormat.RGB24, false);
cam.orthographic = true;
cam.orthographicSize = 3.2f;
cam.transform.position = new UnityEngine.Vector3(0f, 20f, 0f);
cam.transform.rotation = UnityEngine.Quaternion.Euler(90f, -90f, 0f); // arriba de la imagen = -x (izquierda del tren); derecha = +z (cabina)
cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
string Shot(string name, System.Func<UnityEngine.Renderer, bool> show)
{
    foreach (var r in renderers) r.enabled = show(r);
    cam.Render(); cam.Render();
    UnityEngine.RenderTexture.active = rt;
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 700), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = null;
    System.IO.File.WriteAllBytes($"Temp/ArtPreview/pieza_{name}.png", tex.EncodeToPNG());
    return name;
}
cam.orthographicSize = 4.4f;
var shots = new System.Collections.Generic.List<string>();
shots.Add(Shot("muebles", r => r.name.StartsWith("Muebles") || r.name.StartsWith("Interior")));
shots.Add(Shot("utileria", r => r.name.StartsWith("Utileria") || r.name.StartsWith("Interior")));
shots.Add(Shot("cabina", r => r.name.StartsWith("Cabina") || r.name.StartsWith("Interior") || r.name.StartsWith("Palanca") || r.name.StartsWith("Vidrio_Radar")));
shots.Add(Shot("sueltas", r => r.name.StartsWith("Cama") || r.name.StartsWith("Estante") || r.name.StartsWith("Sierra") || r.name.StartsWith("Bidon") || r.name.StartsWith("Interior") || r.name.StartsWith("Sala")));
foreach (var r in renderers) r.enabled = true;
cam.orthographic = false;
cam.targetTexture = null;
return string.Join(", ", shots);
