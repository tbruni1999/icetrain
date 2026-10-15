// Vista previa del arte en el juego (no es una prueba): abre la puerta derecha del tren y pone al jugador afuera, en la nieve,
// mirando la puerta, para sacar una captura con capture_game_view.
//   unity command eval_file --file "Tools/ArtPreview/mirar_afuera.cs" --timeout 60
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var body = train.GetComponent<UnityEngine.Rigidbody>();
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var look = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerLook>();
var who = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Interaction.PlayerInteractor>();
var door = UnityEngine.GameObject.Find("Puerta_Derecha").GetComponent<SurvivalEnTren.Interaction.SlidingDoor>();
if (!door.IsOpen) door.Interact(who);
var p = body.position + body.rotation * new UnityEngine.Vector3(8f, 0f, 2f); p.y = 0.1f;
motor.TeleportTo(p, null);
var to = body.position + body.rotation * new UnityEngine.Vector3(1.5f, 1.4f, -5f) - (p + UnityEngine.Vector3.up * 1.6f);
motor.SetYaw(UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(to.x, 0f, to.z)).eulerAngles.y);
look.SetPitch(-UnityEngine.Mathf.Atan2(to.y, new UnityEngine.Vector2(to.x, to.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
return door.IsOpen;
