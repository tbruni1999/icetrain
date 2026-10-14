#!/bin/bash
# Regresión parcial: las pruebas que tocan lo que cambió en los últimos milestones (M11 lugares, M12 guardado) más las de cada
# sistema que se modificó. La completa (regresion.sh) tarda más de 2 horas y se corre al cerrar la V1.
# Uso (desde la carpeta del proyecto): bash Tools/SpikeTests/regresion_parcial.sh [archivo_de_salida]
cd "$(dirname "$0")/../.."
OUT="${1:-/tmp/regresion_parcial.txt}"
M2=Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity
: > "$OUT"
run() { echo "=== $1" >> "$OUT"; timeout 900 bash Tools/SpikeTests/run_test.sh "$1" "$2" "$3" >> "$OUT" 2>&1; }
run m2_cambio_y_parada M2 $M2
run m5_sonda_portatil SondaPortatil $M2
run m6_expedicion Expedicion $M2
run m7_desmontaje Desmontaje $M2
run m7_expedicion ExpedicionM7 $M2
run m8_ciclo_completo Ciclo $M2
run m9_banco Banco $M2
run m9_taller_ciclo TallerCiclo $M2
run m10_reparacion Reparacion $M2
run m10_sonda_red SondaRed $M2
run m10_slice Slice $M2
run m11_lugares Lugares $M2
run m11_loop Loop $M2
run m12_guardado Guardado $M2
echo "done" >> "$OUT"
