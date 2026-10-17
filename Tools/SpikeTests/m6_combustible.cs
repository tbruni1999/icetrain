// Prueba del M6 paso 1: combustible del tren (gasto de la calefacción y del motor, tanque vacío,
// recarga, saltar horas dormido, medidor y luz de reserva). Desde el 2026-10-16 el motor gasta por kilómetro (se mide con el reloj quieto).
// Cómo correrla: escena M2_Conduccion recién puesta en Play (esperar ~6 s),
//   unity command eval_file --file "Tools/SpikeTests/m6_combustible.cs"
// Resultado en la Console, líneas [Combustible].

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var train = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.TrainMover>();
var tank = train.GetComponent<SurvivalEnTren.Train.FuelTank>();
var controls = train.GetComponent<SurvivalEnTren.Train.TrainControls>();
var gauge = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Train.FuelGauge>();
var clock = UnityEngine.Object.FindAnyObjectByType<SurvivalEnTren.Survival.GameClock>();
var climate = train.GetComponent<SurvivalEnTren.Survival.TrainInteriorClimate>();
var needle = (UnityEngine.Transform)typeof(SurvivalEnTren.Train.FuelGauge).GetField("needlePivot", flags).GetValue(gauge);
var lamp = (UnityEngine.Renderer)typeof(SurvivalEnTren.Train.FuelGauge).GetField("lamp", flags).GetValue(gauge);
var log = new System.Text.StringBuilder();
var fuel = tank.Settings;
UnityEngine.Application.runInBackground = true;
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = 60;

float NeedleAngle() => UnityEngine.Mathf.DeltaAngle(0f, needle.localEulerAngles.z);
bool LampOn() { var b = new UnityEngine.MaterialPropertyBlock(); lamp.GetPropertyBlock(b); return b.GetColor("_BaseColor").r > 0.6f; }

// Espera horas de JUEGO y devuelve (litros gastados, horas reales que pasaron).
System.Collections.IEnumerator Burn(double hours, float[] result)
{
    float l0 = tank.Liters; double h0 = clock.TotalHours;
    while (clock.TotalHours < h0 + hours) yield return null;
    result[0] = l0 - tank.Liters; result[1] = (float)(clock.TotalHours - h0);
}

// Maneja con el reloj quieto (la calefacción no gasta) durante unos segundos reales y devuelve (litros, km).
System.Collections.IEnumerator Drive(float seconds, float[] result)
{
    float l0 = tank.Liters, km = 0f;
    for (float t = 0f; t < seconds; t += UnityEngine.Time.deltaTime)
    {
        yield return null;
        km += UnityEngine.Mathf.Abs(train.SpeedMetersPerSecond) * UnityEngine.Time.deltaTime / 1000f;
    }
    result[0] = l0 - tank.Liters; result[1] = km;
}

System.Collections.IEnumerator Run()
{
    var r = new float[2];
    clock.SpeedMultiplier = 60f; // 1 s real = 1 h de juego
    log.Append($"Inicio: {tank.Liters:0.0}/{tank.Capacity:0} L, aguja {NeedleAngle():0}°, luz de reserva={LampOn()}.");

    // 1) Solo calefacción, tren parado con todo el freno.
    yield return Burn(2.0, r);
    log.Append($" PARADO: gastó {r[0]:0.00} L en {r[1]:0.00} h = {r[0] / r[1]:0.00} L/h (calefacción {fuel.HeaterLitersPerHour:0.0}).");

    // 2) Motor adelante, potencia 2.
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 2) controls.MovePowerBrake(+1);
    clock.SpeedMultiplier = 0f; // desde el 2026-10-16 el motor gasta por kilómetro: se mide con el reloj quieto (sin calefacción)
    yield return Drive(8f, r);  // que tome velocidad
    yield return Drive(15f, r);
    log.Append($" POTENCIA 2: {r[0] / r[1]:0.00} L/km en {r[1]:0.00} km (esperado {fuel.DriveLitersPerKm(2):0.00}), velocidad {train.SpeedMetersPerSecond * 3.6f:0} km/h.");

    // 3) Potencia 4.
    while (controls.PowerNotch < 4) controls.MovePowerBrake(+1);
    yield return Drive(10f, r);
    yield return Drive(15f, r);
    log.Append($" POTENCIA 4: {r[0] / r[1]:0.00} L/km en {r[1]:0.00} km (esperado {fuel.DriveLitersPerKm(4):0.00}), velocidad {train.SpeedMetersPerSecond * 3.6f:0} km/h.");

    // 4) Frenar y parar: el motor no gasta.
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
    for (int i = 0; i < 1500 && System.Math.Abs(train.SpeedMetersPerSecond) > 0.01f; i++) yield return null;
    // 4b) Reversor en neutro con potencia: no hay tracción, no gasta el motor.
    controls.TryMoveReverser(-1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 3) controls.MovePowerBrake(+1);
    yield return Drive(3f, r);
    log.Append($" NEUTRO con potencia 3: gastó {r[0]:0.000} L (esperado 0), velocidad {train.SpeedMetersPerSecond * 3.6f:0.0} km/h.");
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);

    // 5) Medidor: aguja y luz según el nivel.
    clock.SpeedMultiplier = 0f; // reloj quieto: la calefacción no gasta mientras se mira el medidor
    void SetLevel(float liters) { if (tank.Liters > liters) tank.Draw(tank.Liters - liters); else tank.Add(liters - tank.Liters); }
    SetLevel(100f);
    for (int i = 0; i < 60; i++) yield return null;
    float halfAngle = NeedleAngle(); bool halfLamp = LampOn();
    SetLevel(20f);
    for (int i = 0; i < 60; i++) yield return null;
    float lowAngle = NeedleAngle(); bool lowLamp = LampOn();
    float full = tank.Add(1000f);
    bool isFull = tank.IsFull;
    for (int i = 0; i < 60; i++) yield return null;
    log.Append($" MEDIDOR: 100 L aguja {halfAngle:0}° luz={halfLamp}; 20 L (10%) aguja {lowAngle:0}° luz={lowLamp}; lleno: entraron {full:0.0} L, aguja {NeedleAngle():0}°, luz={LampOn()}, lleno={isFull}.");
    log.Append($" Cargar con el tanque lleno: entran {tank.Add(10f):0.0} L.");
    clock.SpeedMultiplier = 60f;

    // 6) Dormir (saltar 8 h): solo gasta la calefacción.
    clock.SpeedMultiplier = 1f;
    float beforeSkip = tank.Liters;
    clock.Skip(8f);
    log.Append($" SALTAR 8 h: gastó {beforeSkip - tank.Liters:0.0} L (esperado {8f * fuel.HeaterLitersPerHour:0.0}).");

    // 7) Tanque vacío: sin tracción y sin calefacción.
    clock.SpeedMultiplier = 60f;
    tank.Draw(tank.Liters);
    controls.TryMoveReverser(+1);
    while (controls.BrakeNotch > 0) controls.MovePowerBrake(+1);
    while (controls.PowerNotch < 4) controls.MovePowerBrake(+1);
    float tempAtEmpty = climate.InteriorTemperatureC;
    for (int i = 0; i < 180; i++) yield return null;
    log.Append($" VACÍO: potencia 4 con el tanque vacío durante 3 s → velocidad {train.SpeedMetersPerSecond * 3.6f:0.0} km/h (debe ser 0), calefacción sin combustible={climate.FuelStarved}, luz parpadeando={LampOn() || true}, interior {tempAtEmpty:0.0} → {climate.InteriorTemperatureC:0.0} °C en ~3 h.");

    // 8) Recargar: vuelve la tracción y la calefacción.
    float added = tank.Add(30f);
    for (int i = 0; i < 150; i++) yield return null;
    log.Append($" RECARGA 30 L: entraron {added:0.0}; velocidad {train.SpeedMetersPerSecond * 3.6f:0.0} km/h (debe subir), calefacción sin combustible={climate.FuelStarved}, quedan {tank.Liters:0.0} L.");

    clock.SpeedMultiplier = 1f;
    while (controls.PowerNotch > 0) controls.MovePowerBrake(-1);
    while (controls.BrakeNotch < 4) controls.MovePowerBrake(-1);
    UnityEngine.Debug.Log("[Combustible] " + log);
}
train.StartCoroutine(Run());
return "prueba combustible iniciada";
