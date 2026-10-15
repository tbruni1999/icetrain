// Vista previa del arte (no es una prueba del juego): abre una escena vacía con luz, pone el vagón MR-04 girado 180°
// (la cabina hacia +Z, como el tren del juego) y saca fotos desde varios ángulos a Temp/ArtPreview/*.png.
// Cómo correrla (con el editor fuera de Play):
//   unity command eval_file --file "Tools/ArtPreview/vista_previa_vagon.cs" --timeout 60
// Después volver a abrir la escena de juego (la deja cerrada).

var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Project/Art/Tren/MR04/RedBlanca_MR04_v03_invierno.fbx");
UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
var holder = new UnityEngine.GameObject("Vagon");
holder.transform.rotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
var wagon = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fbx);
wagon.transform.SetParent(holder.transform, false);
var ground = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane);
ground.transform.localScale = new UnityEngine.Vector3(6f, 1f, 6f);
ground.GetComponent<UnityEngine.Renderer>().sharedMaterial = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")) { color = new UnityEngine.Color(0.86f, 0.88f, 0.92f) };
var sun = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Light>();
sun.transform.rotation = UnityEngine.Quaternion.Euler(40f, 150f, 0f);
sun.intensity = 1.6f;
UnityEngine.RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
UnityEngine.RenderSettings.ambientLight = new UnityEngine.Color(0.45f, 0.48f, 0.55f);
var interiorLight = new UnityEngine.GameObject("Luz_Interior").AddComponent<UnityEngine.Light>();
interiorLight.type = UnityEngine.LightType.Point; interiorLight.range = 9f; interiorLight.intensity = 6f;
interiorLight.color = new UnityEngine.Color(1f, 0.82f, 0.6f);
interiorLight.transform.position = new UnityEngine.Vector3(0f, 3.0f, 2f);
var interiorLight2 = UnityEngine.Object.Instantiate(interiorLight); interiorLight2.transform.position = new UnityEngine.Vector3(0f, 3.0f, -3f);

var cam = UnityEngine.Camera.main;
cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
cam.backgroundColor = new UnityEngine.Color(0.62f, 0.68f, 0.76f);
cam.nearClipPlane = 0.05f;
System.IO.Directory.CreateDirectory("Temp/ArtPreview");
var rt = new UnityEngine.RenderTexture(1280, 720, 24);
cam.targetTexture = rt;
var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
string Shot(string name, UnityEngine.Vector3 from, UnityEngine.Vector3 to, float fov)
{
    cam.fieldOfView = fov;
    cam.transform.position = from;
    cam.transform.LookAt(to);
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = null;
    System.IO.File.WriteAllBytes($"Temp/ArtPreview/{name}.png", tex.EncodeToPNG());
    return name;
}
var shots = new System.Collections.Generic.List<string>();
shots.Add(Shot("1_frente_derecha", new UnityEngine.Vector3(9f, 4.5f, 17f), new UnityEngine.Vector3(0f, 1.6f, 2f), 50f));
shots.Add(Shot("2_atras_izquierda", new UnityEngine.Vector3(-10f, 4f, -16f), new UnityEngine.Vector3(0f, 1.6f, -2f), 50f));
shots.Add(Shot("3_costado_derecho", new UnityEngine.Vector3(16f, 2.2f, 0f), new UnityEngine.Vector3(0f, 1.8f, 0f), 60f));
var roofs = new System.Collections.Generic.List<UnityEngine.Renderer>();
foreach (var r in wagon.GetComponentsInChildren<UnityEngine.Renderer>()) if (r.name.StartsWith("Carroceria") || r.name.StartsWith("Nieve_LOD") || r.name.StartsWith("Carambanos") || r.name.StartsWith("Escape")) { r.enabled = false; roofs.Add(r); }
shots.Add(Shot("4_interior_desde_arriba", new UnityEngine.Vector3(0f, 16f, -1f), new UnityEngine.Vector3(0f, 1f, 0.5f), 70f));
foreach (var r in roofs) r.enabled = true;
shots.Add(Shot("5_interior_hacia_cabina", new UnityEngine.Vector3(0f, 2.65f, -1f), new UnityEngine.Vector3(0f, 2.0f, 9f), 75f));
shots.Add(Shot("6_interior_hacia_atras", new UnityEngine.Vector3(0f, 2.65f, 6.5f), new UnityEngine.Vector3(0f, 1.9f, -6f), 75f));
shots.Add(Shot("7_cabina_pupitre", new UnityEngine.Vector3(-0.45f, 2.65f, 7.6f), new UnityEngine.Vector3(-0.4f, 2.0f, 9.2f), 70f));
cam.targetTexture = null;
return "fotos: " + string.Join(", ", shots);
