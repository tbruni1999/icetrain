// Vista previa en el juego (no es una prueba): pone al jugador cerca de un lugar (por el nombre de su objeto), en coordenadas
// locales de ese lugar, mirando a otro punto local. Editar LUGAR, PUNTO y MIRA antes de correrla; después capture_game_view.
//   unity command eval_file --file "Tools/ArtPreview/mirar_lugar.cs" --timeout 60
var LUGAR = "Refugio";
var PUNTO = new UnityEngine.Vector3(40f, 0f, 40f);
var MIRA = new UnityEngine.Vector3(200f, 0f, 120f);
var place = UnityEngine.GameObject.Find(LUGAR).transform;
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var overlay = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeOverlay>();
if (overlay != null) overlay.Visible = false;
var p = place.TransformPoint(PUNTO);
var terrain = UnityEngine.Terrain.activeTerrain;
p.y = (terrain != null ? terrain.SampleHeight(p) + terrain.transform.position.y : 0f) + 0.1f;
motor.TeleportTo(p, null);
var eye = p + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
var to = place.TransformPoint(MIRA) - eye;
motor.SetYaw(UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(to.x, 0f, to.z)).eulerAngles.y);
look.SetPitch(-UnityEngine.Mathf.Atan2(to.y, new UnityEngine.Vector2(to.x, to.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
return "listo";
