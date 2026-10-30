#!/usr/bin/env bash
set -euo pipefail

echo "=========================================================="
echo "Ejecutando suite de pruebas automatizadas (LabReservations)"
echo "=========================================================="
dotnet test --logger "console;verbosity=normal"
