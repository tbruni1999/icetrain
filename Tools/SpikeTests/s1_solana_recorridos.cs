// Prueba del blockout de Solana (documento "Solana y POIs grandes", sección 12, pasos 1 a 4).
// 1) Cada sala tiene piso firme en su nivel (salvo el pozo del montacargas, que es un pozo).
// 2) Recorre las rutas del documento caminando como el jugador (empujón hacia abajo como el motor): entrar por el derrumbe y bajar a
//    la Administración, del subsuelo a la azotea por la escalera de emergencia, las escaleras mecánicas, la claraboya (E2) y la
//    torre (E5) hasta el hall del cine. Anota si llegó, en cuánto, el salto de posición más grande entre dos pasos y, si se trabó,
//    dónde y contra qué.
// Cómo correrla: escena Solana_Blockout recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/s1_solana_recorridos.cs"
// Resultado en la Console, línea [Solana].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var motor = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Player.PlayerMotor>();
var cc = motor.GetComponent<UnityEngine.CharacterController>();
var tm = typeof(SurvivalEnTren.Player.PlayerMotor);
var toLocal = tm.GetMethod("ToReferenceLocal", flags);
var log = new System.Text.StringBuilder();
var wait = new UnityEngine.WaitForFixedUpdate();
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

System.Collections.IEnumerator Walk(UnityEngine.Transform route, System.Text.StringBuilder outLog)
{
    var points = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (UnityEngine.Transform w in route) points.Add(w.position);
    motor.TeleportTo(points[0] + UnityEngine.Vector3.up * 0.1f, null);
    for (int i = 0; i < 20; i++) yield return wait;
    int reached = 1; float t = 0f, stuck = 0f, maxJump = 0f, lowest = 999f; string fail = "";
    const float speed = 4f;
    while (reached < points.Count && t < 240f)
    {
        yield return wait;
        float dt = UnityEngine.Time.fixedDeltaTime;
        t += dt;
        var target = points[reached];
        var here = motor.transform.position;
        var flat = new UnityEngine.Vector3(target.x - here.x, 0f, target.z - here.z);
        if (flat.magnitude < 0.6f)
        {
            if (UnityEngine.Mathf.Abs(here.y - target.y) > 1.6f) { fail = $"llegó a P{reached:00} pero a otra altura ({here.y:0.0} m en vez de {target.y:0.0})"; break; }
            reached++; stuck = 0f; continue;
        }
        var dir = flat.normalized;
        motor.SetYaw(UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg);
        var before = motor.transform.position;
        cc.Move(dir * speed * dt + UnityEngine.Vector3.down * 2f * dt);
        var local = toLocal.Invoke(motor, new object[] { motor.transform.position });
        tm.GetField("_currentLocal", flags).SetValue(motor, local);
        tm.GetField("_previousLocal", flags).SetValue(motor, local);
        var after = motor.transform.position;
        float moved = UnityEngine.Vector3.ProjectOnPlane(after - before, UnityEngine.Vector3.up).magnitude;
        maxJump = UnityEngine.Mathf.Max(maxJump, (after - before).magnitude);
        lowest = UnityEngine.Mathf.Min(lowest, after.y - target.y);
        stuck = moved < speed * dt * 0.2f ? stuck + dt : 0f;
        if (stuck > 2f)
        {
            var hits = UnityEngine.Physics.OverlapSphere(after + UnityEngine.Vector3.up * 0.9f + dir * 0.6f, 0.5f, ~0, UnityEngine.QueryTriggerInteraction.Ignore);
            var names = new System.Collections.Generic.List<string>();
            foreach (var h in hits) if (h.transform != motor.transform) names.Add(h.name + "/" + (h.transform.parent ? h.transform.parent.name : "-"));
            fail = $"trabado yendo a P{reached:00} en ({after.x:0.0}, {after.y:0.0}, {after.z:0.0}) contra {string.Join(", ", names)}";
            break;
        }
        if (after.y < target.y - 4f && after.y < before.y - 0.01f && lowest < -4f) { fail = $"se cayó yendo a P{reached:00}: está en y {after.y:0.0}"; break; }
    }
    bool ok = reached >= points.Count;
    outLog.Append($" [{route.name}: {(ok ? "LLEGÓ" : "NO LLEGÓ")} {reached}/{points.Count} puntos en {t:0.0} s; salto máximo entre pasos {maxJump * 100f:0} cm{(fail != "" ? "; " + fail : "")}]");
}

System.Collections.IEnumerator Run()
{
    for (int i = 0; i < 30; i++) yield return wait;
    // 1) Piso firme en cada sala.
    var markers = UnityEngine.GameObject.Find("Salas_Marcadores").transform;
    int rooms = 0, withFloor = 0; var missing = new System.Collections.Generic.List<string>();
    foreach (UnityEngine.Transform m in markers)
    {
        if (m.name.EndsWith("NM")) continue; // el pozo del montacargas
        rooms++;
        if (UnityEngine.Physics.Raycast(m.position + UnityEngine.Vector3.up * 1.5f, UnityEngine.Vector3.down, out var hit, 2.5f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)
            && UnityEngine.Mathf.Abs(hit.point.y - m.position.y) < 0.4f) withFloor++;
        else missing.Add(m.name.Replace("Sala_", ""));
    }
    log.Append($"SALAS con piso firme: {withFloor}/{rooms}{(missing.Count > 0 ? " (faltan: " + string.Join(", ", missing) + ")" : "")}.");
    // 2) Las rutas.
    var routes = UnityEngine.GameObject.Find("Rutas_Prueba").transform;
    foreach (UnityEngine.Transform route in routes) yield return Walk(route, log);
    UnityEngine.Debug.Log($"[Solana] {log}");
}
motor.StartCoroutine(Run());
return "prueba Solana iniciada";
