# Bitácora de Desarrollo y Registro de Delegación

Este documento registra de forma cronológica y acumulativa los hitos implementados con asistencia del agente de programación, documentando las instrucciones, resultados y la posterior verificación y decisión humana.

---

## Hito 0: Inicialización y Especificación
- **Fecha:** 2026-10-23
- **Objetivo / Requisito:** Inicializar la solución .NET 10, estructura de carpetas, Dockerfile, catálogo de inventario e instrucciones base.
- **Instrucción suministrada:** Generar la estructura de proyectos CLI y Tests, cargar las reglas de negocio y los supuestos técnicos acordados (formato ISO 8601 local, sin conversión de huso, máximo 24h, sin límite de anticipación).
- **Acción del agente:** Creó la solución `LabReservations.sln`, los proyectos `LabReservations.Cli` y `LabReservations.Tests`, el archivo `GEMINI.md`, `data/inventario.json` y el esqueleto del `README.md`.
- **Verificación Humana y Decisión:** 
  - *Comando ejecutado:* `dotnet build`
  - *Resultado:* Compilación exitosa (0 errores, 0 advertencias).
  - *Decisión:* Aceptado con ajuste: se reemplazó el uso de comandos incompatibles con PowerShell (`touch`) por compatibilidad nativa de Windows 11.
- **Commit asociado:** `b1a3a57`

---

## Hito 1: Modelos de Dominio, Catálogo de Inventario y Persistencia JSON
- **Fecha:** 2026-10-25
- **Objetivo / Requisito:** Implementar los modelos de dominio (`EstadoReserva`, `Unidad`, `Reserva`), catálogo de inventario con las 5 unidades fijas hidratado desde `data/inventario.json` con fallback en memoria, y el repositorio de persistencia `ReservaRepository` con serialización JSON atómica.
- **Instrucción suministrada:** Implementar modelos de dominio `EstadoReserva`, `Unidad`, `Reserva`. Unificar inventario en clase `Inventario` con `Lazy<Inventario>`, lectura de `data/inventario.json`, fallback inmutable con 5 unidades en memoria, y aplicación defensiva de `.Trim()` y validación de nulos/espacios en `ExisteUnidad` y `ObtenerUnidadPorId`. Crear repositorio atómico `ReservaRepository` y suite de pruebas en `LabReservations.Tests`.
- **Acción del agente:** 
  - Creó `src/LabReservations.Cli/Models.cs` con el enum y records inmutables de dominio.
  - Creó `data/inventario.json` con las 5 unidades físicas precargadas (`MIC-01`, `MIC-02`, `OSC-01`, `MUL-01`, `FUE-01`).
  - Creó `src/LabReservations.Cli/Inventario.cs` con carga JSON, fallback estático inmutable y validación defensiva con `.Trim()`.
  - Creó `src/LabReservations.Cli/ReservaRepository.cs` con serialización `System.Text.Json`, conversión de enum a string y guardado atómico mediante archivo temporal.
  - Creó pruebas unitarias en `tests/LabReservations.Tests/InventarioTests.cs` y `tests/LabReservations.Tests/ReservaRepositoryTests.cs`.
- **Resultado / Comprobación:**
  - *Comando ejecutado:* `dotnet test`
  - *Resultado:* 24 pruebas ejecutadas y superadas con éxito (0 errores, 0 omitidas).
- **Verificación Humana y Decisión:**
  - *Decisión:* Aceptado con ajuste arquitectónico y defensivo:
    1. Se descartó la separación de repositorio/servicio para inventario en favor de una clase `Inventario` unificada con hidratación desde archivo y fallback en memoria, desacoplando dependencias en pruebas unitarias.
    2. Se exigió validación defensiva `!string.IsNullOrWhiteSpace` y normalización `.Trim()` en búsquedas de ID de unidad para tolerar entradas desde consola con espacios marginales.
  - *Commit sugerido:* `feat(dominio): implementar modelos, catalogo de inventario con fallback y persistencia json`
- **Commit asociado:** `8b9403a`

---

## Hito 2: Servicio de Reservas, Reglas de Superposición y Cancelación No Destructiva
- **Fecha:** 2026-10-25
- **Objetivo / Requisito:** Implementar `ReservaService` con la lógica de consulta de disponibilidad, creación de reservas con validaciones de negocio y detección de superposición de intervalos, cancelación no destructiva e historial, junto con la suite completa de los 7 escenarios obligatorios en `ReservaServiceTests`.
- **Instrucción suministrada:** Crear `src/LabReservations.Cli/ReservaService.cs` inyectando `ReservaRepository` e `Inventario`, implementando `ConsultarDisponibilidad`, `CrearReserva`, `CancelarReserva` y `ObtenerHistorial`. Crear `tests/LabReservations.Tests/ReservaServiceTests.cs` cubriendo los 7 escenarios centrales de dominio y persistencia.
- **Acción del agente:**
  - Creó `src/LabReservations.Cli/ReservaService.cs` con validación de inventario, normalización `.Trim()` de solicitante e identificadores, control de intervalo (`Inicio < Fin` y duración `<= 24h`), evaluación estricta de superposición sobre reservas activas (`inicio < existente.Fin && fin > existente.Inicio`), generación de IDs `RES-XXXXXXXX` y cancelación no destructiva preservando el registro original en estado `Cancelada`.
  - Creó `tests/LabReservations.Tests/ReservaServiceTests.cs` verificando los 7 escenarios obligatorios: creación válida, rechazo de superposición en la misma unidad, aceptación de límites consecutivos exactos, reservas simultáneas en distintas unidades, rechazo de entradas inválidas, cancelación no destructiva y persistencia tras reinicio.
- **Resultado / Comprobación:**
  - *Comando ejecutado:* `dotnet test`
  - *Resultado:* 43 pruebas ejecutadas y superadas con éxito (0 errores, 0 omitidas).
- **Verificación Humana y Decisión:**
  - *Decisión:* Aceptado tras inspección visual de `ExisteSuperposicionActiva` y `CancelarReserva` en `ReservaService.cs`, confirmando la fórmula estricta de intervalos (`<` y `>`) y la actualización inmutable no destructiva mediante `with { Estado = EstadoReserva.Cancelada }`.
  - *Commit sugerido:* `feat(servicio): implementar servicio de reservas, validacion de superposicion y cancelacion no destructiva`
- **Commit asociado:** `045efaa`

---

## Hito 3: Interfaz CLI, Scripts de Automatización y Comprobación de Reinicio
- **Fecha:** 2026-10-26
- **Hito / Objetivo:** Implementar la interfaz de línea de comandos (`Program.cs`) con subcomandos nativos, crear los scripts multiplataforma de pruebas, reinicio de datos y verificación de persistencia entre procesos, y actualizar la documentación final en `README.md` y `HISTORY.md`.
- **Instrucción / Prompt:** Implementar en `src/LabReservations.Cli/Program.cs` los subcomandos `disponibilidad`, `reservar`, `cancelar`, `historial`, `inventario` y `--help` con parseo nativo, formato ISO 8601 (`yyyy-MM-ddTHH:mm`) y códigos de retorno `0`/`1`. Crear en la raíz `run-tests`, `reset-data` (con advertencia explícita de borrado) y `verify-persistence` (`.bat` y `.sh`). Actualizar `README.md` (secciones 2 y 4) y registrar el Hito 3 en `HISTORY.md`.
- **Acción del Agente:**
  - Implementó `src/LabReservations.Cli/Program.cs` (`Program.Main` y `CliRunner`) con `Console.OutputEncoding = System.Text.Encoding.UTF8;` en la primera línea, parseo nativo de argumentos, validación estricta de formato temporal ISO 8601 (`yyyy-MM-ddTHH:mm`), salida tabular para inventario e historial, y captura controlada de excepciones de dominio devolviendo código de salida `1`.
  - Creó `tests/LabReservations.Tests/CliIntegrationTests.cs` verificando los casos de integración de CLI (`--help`/`-h`, manejo de errores de sintaxis en `Console.Error` con retorno `1`, y consulta de las 5 unidades del subcomando `inventario`).
  - Creó los scripts multiplataforma en la raíz: `run-tests.bat`, `run-tests.sh`, `reset-data.bat`, `reset-data.sh`, `verify-persistence.bat`, `verify-persistence.ps1` y `verify-persistence.sh` utilizando compilación previa `-c Release` y ejecución `--no-build -c Release`.
  - Actualizó quirúrgicamente `README.md` en las secciones `2. Solución` y `4. Registro de decisiones y delegación`.
- **Resultado / Comprobación:**
  - *Comandos ejecutados:* `dotnet test` y `powershell -ExecutionPolicy Bypass -File .\verify-persistence.ps1`
  - *Resultado:* 47 pruebas unitarias e integrales aprobadas (0 errores) y verificación end-to-end en procesos independientes completada con éxito (conservación de reserva `CANCELADA` y `ACTIVA` en historial tras reinicio, y liberación efectiva del turno de 10:00 a 11:00).
- **Validación Humana:**
  - *Ajustes requeridos:*
    1. Se exigió fijar `Console.OutputEncoding = System.Text.Encoding.UTF8;` como primera línea de `src/LabReservations.Cli/Program.cs`.
    2. Se solicitó precompilar en modo `Release` (`--no-build -c Release`) en los scripts de verificación, añadir `verify-persistence.ps1` y agregar pruebas de integración en `CliIntegrationTests.cs`.
  - *Decisión:* Aprobado tras revisión de la salida completa de `verify-persistence.ps1` y las 47 pruebas automáticas en `dotnet test`.
- **Commit asociado:** `ea59c29`

---

<!-- Las siguientes entradas se irán agregando al final de este archivo a medida que completemos cada hito -->
