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
  - *Decisión:* Pendiente de validación humana.
  - *Commit sugerido:* `feat(servicio): implementar servicio de reservas, validacion de superposicion y cancelacion no destructiva`
- **Commit asociado:** Pendiente

---

<!-- Las siguientes entradas se irán agregando al final de este archivo a medida que completemos cada hito -->
