#!/bin/bash
# Corre una prueba de Tools/SpikeTests en su escena recién puesta en Play y muestra su línea de resultado.
# Uso (desde la carpeta del proyecto): bash Tools/SpikeTests/run_test.sh <archivo sin .cs> <etiqueta> <escena>
# Espera un mensaje NUEVO con la etiqueta: cuenta los que ya había (la consola no siempre se limpia) y, si Unity no contesta
# (cuando una prueba mide sin límite de cuadros, el editor deja de atender pedidos un rato), reintenta en vez de contar mal.
F="--caller plugin --skill unity-cli --no-banner --result-only"

# Lee la consola; devuelve vacío si Unity no contestó.
console() {
  local out
  out=$(unity command console $F 2>&1)
  if echo "$out" | grep -q '"message"' && ! echo "$out" | grep -q 'timed out after'; then echo "$out"; fi
}

unity command editor_stop $F >/dev/null 2>&1; sleep 2
unity command open_scene --path "$3" $F >/dev/null 2>&1
unity command editor_play $F >/dev/null; sleep 12
# calentar el compilador de eval (la primera vez tarda y puede pasar el limite de 5 s): reintentar
for k in 1 2 3 4 5 6; do
  r=$(unity command eval --code 'UnityEngine.Application.runInBackground = true; UnityEngine.QualitySettings.vSyncCount = 0; UnityEngine.Application.targetFrameRate = 60; return 1;' $F 2>&1)
  echo "$r" | grep -q '"result": 1' && break; sleep 3
done
unity command clear_console $F >/dev/null
n0=""
for k in $(seq 1 10); do out=$(console); if [ -n "$out" ]; then n0=$(echo "$out" | grep -c "\[$2\]"); break; fi; sleep 3; done
[ -z "$n0" ] && n0=0
# (un solo lanzamiento: si el editor tarda en compilar puede avisar "timed out" y la prueba arranca igual)
unity command eval_file --file "Tools/SpikeTests/$1.cs" --timeout 60 $F 2>&1 | grep -o '"result": .*'
last=""
for i in $(seq 1 110); do
  out=$(console)
  if [ -n "$out" ]; then
    last="$out"
    n=$(echo "$out" | grep -c "\[$2\]")
    if [ "$n" -gt "$n0" ] || echo "$out" | grep -q "Exception:"; then break; fi
  fi
  sleep 8
done
echo "$last" | grep '"message"' | grep "\[$2\]\|Exception" | tail -1
