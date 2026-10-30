#!/usr/bin/env bash
set -euo pipefail

echo "============================================================================"
echo "[ADVERTENCIA] Esta accion borrara TODAS las reservas registradas en disco,"
echo "restaurando el archivo ./data/reservas.json a su estado inicial vacio ([])."
echo "============================================================================"
mkdir -p data
printf "[]\n" > data/reservas.json
echo "[OK] Estado reiniciado correctamente en ./data/reservas.json."
