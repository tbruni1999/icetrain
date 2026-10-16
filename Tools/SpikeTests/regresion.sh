#!/bin/bash
# Regresión completa: corre todas las pruebas una por una (cada una con su escena recién puesta en Play) y deja el resultado
# en el archivo que se pasa (por defecto /tmp/regresion.txt). Tarda más de una hora: no tocar Unity mientras corre.
# Uso (desde la carpeta del proyecto): bash Tools/SpikeTests/regresion.sh [archivo_de_salida]
cd "$(dirname "$0")/../.."
OUT="${1:-/tmp/regresion.txt}"
M2=Assets/_Project/Scenes/Pruebas/M2_Conduccion.unity
SP=Assets/_Project/Scenes/Spike/Spike_TrenEstable.unity
MENU=Assets/_Project/Scenes/Menu_Principal.unity
: > "$OUT"
run() { echo "=== $1" >> "$OUT"; timeout 900 bash Tools/SpikeTests/run_test.sh "$1" "$2" "$3" >> "$OUT" 2>&1; }
run m2_cambio_y_parada M2 $M2
run m2_tope Tope $M2
run m3_puertas Puertas $M2
run m3_llevar Llevar $M2
run m4_temperatura Temperatura $M2
run m4_comer_dormir_morir ComerDormir $M2
run m4_feedback_y_caida Feedback $M2
run m5_sonda_portatil SondaPortatil $M2
run m6_combustible Combustible $M2
run m6_equipo_y_bidon Equipo $M2
run m6_interactor_e InteractorE $M2
run m6_expedicion Expedicion $M2
run m7_mochila Mochila $M2
run m7_panel_y_bultos Panel $M2
run m7_desmontaje Desmontaje $M2
run m7_expedicion ExpedicionM7 $M2
run m8_obras_y_cama Obras $M2
run m8_almacenamiento Almacen $M2
run m8_ciclo_completo Ciclo $M2
run m8_mouse_inventario MouseInv $M2
run m9_planos Planos $M2
run m9_banco Banco $M2
run m9_taller_ciclo TallerCiclo $M2
run m10_bifurcacion Bifurcacion $M2
run m10_reparacion Reparacion $M2
run m10_sonda_red SondaRed $M2
run m10_slice Slice $M2
run m11_lugares Lugares $M2
run m11_loop Loop $M2
run m12_guardado Guardado $M2
run m13_sonido Sonido $M2
run m13_dia_noche DiaNoche $M2
run m13_menu_y_final MenuFinal $M2
run h1_herramientas Herramientas $M2
run p1_nieve Nieve $M2
run p1_escarcha Escarcha $M2
run p3_inicio Inicio $MENU
run parado_en_caja Caja $SP
run recentrado Recentrado $SP
run m13_rendimiento Rendimiento $M2
echo "done" >> "$OUT"
