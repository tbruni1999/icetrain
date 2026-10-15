// Vista previa del arte en el juego (no es una prueba): pone al jugador en un punto del tren (coordenadas locales del tren)
// mirando a otro punto, para sacar una captura con capture_game_view. Editar PUNTO y MIRA antes de correrla.
//   unity command eval_file --file "Tools/ArtPreview/mirar_desde.cs" --timeout 60
var PUNTO = new UnityEngine.Vector3(0.3f, 1.05f, 8.0f);
var MIRA = new UnityEngine.Vector3(-0.9f, 2.4f, 6.3f);
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var body = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var overlay = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Spike.SpikeOverlay>();
if (overlay != null) overlay.Visible = false;
motor.TeleportTo(body.position + body.rotation * PUNTO, body);
var eye = body.position + body.rotation * PUNTO + UnityEngine.Vector3.up * motor.Settings.EyeHeightMeters;
var to = body.position + body.rotation * MIRA - eye;
motor.SetYaw(UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(to.x, 0f, to.z)).eulerAngles.y);
look.SetPitch(-UnityEngine.Mathf.Atan2(to.y, new UnityEngine.Vector2(to.x, to.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
return "listo";
