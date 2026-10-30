@echo off
echo ==========================================================
echo Ejecutando suite de pruebas automatizadas (LabReservations)
echo ==========================================================
dotnet test --logger "console;verbosity=normal"
exit /b %ERRORLEVEL%
