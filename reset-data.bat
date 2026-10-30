@echo off
echo ============================================================================
echo [ADVERTENCIA] Esta accion borrara TODAS las reservas registradas en disco,
echo restaurando el archivo ./data/reservas.json a su estado inicial vacio ([]).
echo ============================================================================
if not exist "data" mkdir "data"
> "data\reservas.json" echo []
echo [OK] Estado reiniciado correctamente en ./data/reservas.json.
