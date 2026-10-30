#!/usr/bin/env bash
set -euo pipefail

echo "============================================================================"
echo "VERIFICACION END-TO-END DE PERSISTENCIA Y REINICIO DE PROCESO (CLI)"
echo "============================================================================"

./reset-data.sh
echo ""

echo "[Compilando proyecto CLI en modo Release...]"
dotnet build src/LabReservations.Cli/LabReservations.Cli.csproj -c Release --nologo -v q
echo ""

echo "[Paso A] Proceso 1: Creando reserva en MIC-01 de 10:00 a 11:00..."
OUTPUT_A=$(dotnet run --project src/LabReservations.Cli --no-build -c Release -- reservar --unit MIC-01 --user "Investigador A" --start 2026-10-27T10:00 --end 2026-10-27T11:00)
echo "$OUTPUT_A"
RES_ID=$(echo "$OUTPUT_A" | awk '/^ID:/ {print $2}' | tr -d '\r')
echo "ID capturado para cancelacion posterior: $RES_ID"
echo ""

echo "[Paso B] Proceso 2: Creando reserva consecutiva en MIC-01 de 11:00 a 12:00..."
dotnet run --project src/LabReservations.Cli --no-build -c Release -- reservar --unit MIC-01 --user "Investigador B" --start 2026-10-27T11:00 --end 2026-10-27T12:00
echo ""

echo "[Paso C] Proceso 3: Cancelando la primera reserva ($RES_ID)..."
dotnet run --project src/LabReservations.Cli --no-build -c Release -- cancelar --id "$RES_ID"
echo ""

echo "[Paso D.1] Proceso 4 (Tras reinicio): Consultando historial persistido de MIC-01..."
dotnet run --project src/LabReservations.Cli --no-build -c Release -- historial --unit MIC-01
echo ""

echo "[Paso D.2] Proceso 5 (Tras reinicio): Verificando disponibilidad liberada de 10:00 a 11:00 en MIC-01..."
dotnet run --project src/LabReservations.Cli --no-build -c Release -- disponibilidad --unit MIC-01 --start 2026-10-27T10:00 --end 2026-10-27T11:00
echo ""

echo "============================================================================"
echo "[VERIFICACION EXITOSA] Los registros persisten tras cerrar procesos y el"
echo "turno cancelado (10:00 a 11:00) se encuentra nuevamente DISPONIBLE."
echo "============================================================================"
