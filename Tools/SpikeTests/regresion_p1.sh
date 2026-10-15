#!/bin/bash
# Regresión de la producción P1 (atmósfera y clima): lo que camina por la nieve (el suelo ahora tiene relieve), la física del
# tren (el casco gris ahora hace sombra), las pruebas nuevas de la nieve y la escarcha, y el rendimiento. Cerca de una hora.
# Uso (desde la carpeta del proyecto): bash Tools/SpikeTests/regresion_p1.sh [archivo_de_salida]
cd "$(dirname "$0")/../.."
OUT="${1:-/tmp/regresion_p1.txt}"
M2=Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity
SP=Assets/_Project/Scenes/Spike/Spike_TrenEstable.unity
: > "$OUT"
run() { echo "=== $1" >> "$OUT"; timeout 900 bash Tools/SpikeTests/run_test.sh "$1" "$2" "$3" >> "$OUT" 2>&1; }
run parado_en_caja Caja $SP
run recentrado Recentrado $SP
run m2_cambio_y_parada M2 $M2
run m3_puertas Puertas $M2
run m5_sonda_portatil SondaPortatil $M2
run m6_expedicion Expedicion $M2
run m7_expedicion ExpedicionM7 $M2
run m10_sonda_red SondaRed $M2
run m10_slice Slice $M2
run m11_lugares Lugares $M2
run m11_loop Loop $M2
run m12_guardado Guardado $M2
run p1_nieve Nieve $M2
run p1_escarcha Escarcha $M2
run m13_rendimiento Rendimiento $M2
echo "done" >> "$OUT"
