#!/bin/bash
# Regresión del arte: las pruebas que tocan lo que cambió al poner el vagón MR-04 y los props (lo que se ve del tren, las puertas
# con bisagra, el estante precario, los bidones, las raciones y la bomba), más el rendimiento. Cerca de una hora.
# Uso (desde la carpeta del proyecto): bash Tools/SpikeTests/regresion_arte.sh [archivo_de_salida]
cd "$(dirname "$0")/../.."
OUT="${1:-/tmp/regresion_arte.txt}"
M2=Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity
SP=Assets/_Project/Scenes/Spike/Spike_TrenEstable.unity
: > "$OUT"
run() { echo "=== $1" >> "$OUT"; timeout 900 bash Tools/SpikeTests/run_test.sh "$1" "$2" "$3" >> "$OUT" 2>&1; }
run parado_en_caja Caja $SP
run recentrado Recentrado $SP
run m2_cambio_y_parada M2 $M2
run m2_tope Tope $M2
run m3_puertas Puertas $M2
run m3_llevar Llevar $M2
run m4_temperatura Temperatura $M2
run m4_comer_dormir_morir ComerDormir $M2
run m6_equipo_y_bidon Equipo $M2
run m6_interactor_e InteractorE $M2
run m6_expedicion Expedicion $M2
run m7_expedicion ExpedicionM7 $M2
run m8_obras_y_cama Obras $M2
run m8_almacenamiento Almacen $M2
run m9_banco Banco $M2
run m12_guardado Guardado $M2
run m13_sonido Sonido $M2
run m13_rendimiento Rendimiento $M2
echo "done" >> "$OUT"
