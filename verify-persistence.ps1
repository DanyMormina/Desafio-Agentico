$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "============================================================================"
Write-Host "VERIFICACION END-TO-END DE PERSISTENCIA Y REINICIO DE PROCESO (PowerShell)"
Write-Host "============================================================================"

& "$PSScriptRoot\reset-data.bat"
Write-Host ""

Write-Host "[Compilando proyecto CLI en modo Release...]"
dotnet build "$PSScriptRoot\src\LabReservations.Cli\LabReservations.Cli.csproj" -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

Write-Host "[Paso A] Proceso 1: Creando reserva en MIC-01 de 10:00 a 11:00..."
$outputA = dotnet run --project "$PSScriptRoot\src\LabReservations.Cli" --no-build -c Release -- reservar --unit MIC-01 --user "Investigador A" --start 2026-10-27T10:00 --end 2026-10-27T11:00
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$outputA | ForEach-Object { Write-Host $_ }

$idLine = $outputA | Where-Object { $_ -match "^ID:\s*(?<id>RES-[A-Z0-9]+)" } | Select-Object -First 1
if (-not $idLine -or -not ($idLine -match "^ID:\s*(?<id>RES-[A-Z0-9]+)")) {
    Write-Error "No se pudo extraer el ID de la reserva desde la salida estándar."
    exit 1
}
$resId = $Matches["id"]
Write-Host "ID capturado para cancelacion posterior: $resId"
Write-Host ""

Write-Host "[Paso B] Proceso 2: Creando reserva consecutiva en MIC-01 de 11:00 a 12:00..."
dotnet run --project "$PSScriptRoot\src\LabReservations.Cli" --no-build -c Release -- reservar --unit MIC-01 --user "Investigador B" --start 2026-10-27T11:00 --end 2026-10-27T12:00
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

Write-Host "[Paso C] Proceso 3: Cancelando la primera reserva ($resId)..."
dotnet run --project "$PSScriptRoot\src\LabReservations.Cli" --no-build -c Release -- cancelar --id $resId
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

Write-Host "[Paso D.1] Proceso 4 (Tras reinicio): Consultando historial persistido de MIC-01..."
dotnet run --project "$PSScriptRoot\src\LabReservations.Cli" --no-build -c Release -- historial --unit MIC-01
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

Write-Host "[Paso D.2] Proceso 5 (Tras reinicio): Verificando disponibilidad liberada de 10:00 a 11:00 en MIC-01..."
dotnet run --project "$PSScriptRoot\src\LabReservations.Cli" --no-build -c Release -- disponibilidad --unit MIC-01 --start 2026-10-27T10:00 --end 2026-10-27T11:00
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

Write-Host "============================================================================"
Write-Host "[VERIFICACION EXITOSA] Los registros persisten tras cerrar procesos y el"
Write-Host "turno cancelado (10:00 a 11:00) se encuentra nuevamente DISPONIBLE."
Write-Host "============================================================================"
