#!/bin/bash
# Corre una prueba de Tools/SpikeTests en su escena recién puesta en Play y muestra su línea de resultado.
# Uso (desde la carpeta del proyecto): bash Tools/SpikeTests/run_test.sh <archivo sin .cs> <etiqueta> <escena>
# Espera un mensaje NUEVO con la etiqueta (cuenta los que ya había: la consola no siempre se limpia).
F="--caller plugin --skill unity-cli --no-banner --result-only"
unity command editor_stop $F >/dev/null 2>&1; sleep 2
unity command open_scene --path "$3" $F >/dev/null 2>&1
unity command editor_play $F >/dev/null; sleep 12
# calentar el compilador de eval (la primera vez tarda y puede pasar el limite de 5 s): reintentar
for k in 1 2 3 4; do
  r=$(unity command eval --code 'UnityEngine.Application.runInBackground = true; UnityEngine.QualitySettings.vSyncCount = 0; UnityEngine.Application.targetFrameRate = 60; return 1;' $F 2>&1)
  echo "$r" | grep -q '"result": 1' && break; sleep 3
done
unity command clear_console $F >/dev/null
n0=$(unity command console $F | grep -c "\[$2\]")
# (un solo lanzamiento: si el editor tarda en compilar puede avisar "timed out" y la prueba arranca igual)
unity command eval_file --file "Tools/SpikeTests/$1.cs" --timeout 60 $F 2>&1 | grep -o '"result": .*'
for i in $(seq 1 90); do out=$(unity command console $F); n=$(echo "$out" | grep -c "\[$2\]"); if [ "$n" -gt "$n0" ] || echo "$out" | grep -q "Exception:"; then break; fi; sleep 8; done
echo "$out" | grep '"message"' | grep "\[$2\]\|Exception" | tail -1
