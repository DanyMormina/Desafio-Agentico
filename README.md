# LabReservations CLI (.NET 10)

Sistema de línea de comandos (CLI) local y determinista para la gestión de reservas de equipos de laboratorio, desarrollado con asistencia de un agente de programación bajo supervisión y verificación humana.

---

## 1. Especificación breve

### Problema y Alcance
La coordinación del laboratorio requiere resolver la superposición de horarios en sus equipos y la pérdida de historial ante cancelaciones. El sistema permite consultar disponibilidad por unidad e intervalo, registrar reservas y cancelar liberando el turno sin borrar el registro histórico.

### Inventario Fijo Precargado
El catálogo cuenta únicamente con 5 unidades físicas predefinidas (no administrable desde la interfaz):
- MIC-01: Microscopio digital
- MIC-02: Microscopio digital
- OSC-01: Osciloscopio
- MUL-01: Multímetro
- FUE-01: Fuente de alimentación

### Reglas de Negocio
- Validaciones: se exige unidad existente en inventario, solicitante ficticio no vacío (rechazando cadenas vacías o compuestas solo por espacios en blanco) y rango temporal con Inicio < Fin.
- Superposición: se rechaza la superposición de reservas activas sobre la misma unidad si se cumple (NuevoInicio < ExistenteFin && NuevoFin > ExistenteInicio).
- Límites Consecutivos: se admiten turnos contiguos exactos en la misma unidad (por ejemplo, 10:00 a 11:00 y 11:00 a 12:00 no colisionan).
- Simultaneidad: se admiten reservas en el mismo horario sobre unidades diferentes.
- Cancelación No Destructiva: cancelar una reserva libera inmediatamente el horario para nuevas consultas y reservas, conservando el identificador, solicitante y datos históricos con estado CANCELADA.

### Supuestos Técnicos Documentados
- Formato Temporal y Zona Horaria: formato ISO 8601 (yyyy-MM-ddTHH:mm). La operación se procesa en la hora local única del laboratorio (America/Argentina/Buenos_Aires, UTC-3) sin conversión automática de huso horario para garantizar paridad entre el host y contenedores Docker.
- Tratamiento de Fechas Pasadas: se permiten reservas retroactivas sin acoplar validaciones a la hora del sistema, permitiendo la regularización de registros históricos y garantizando que las pruebas con fechas fijas sean reproducibles en cualquier fecha futura.
- Límites de Duración y Anticipación: duración máxima continua de 24 horas por reserva. No se impone límite de anticipación futura para admitir planificación abierta.

---

## 2. Solución

### Estructura del Proyecto
- `src/LabReservations.Cli`: Aplicación de consola en C# (.NET 10). Contiene el punto de entrada y parseo CLI (`Program.cs`), el modelo de dominio (`Models.cs`), el catálogo de inventario (`Inventario.cs`), el repositorio de persistencia (`ReservaRepository.cs`) y el servicio de negocio (`ReservaService.cs`).
- `tests/LabReservations.Tests`: Suite de pruebas unitarias e integrales en xUnit (`InventarioTests.cs`, `ReservaRepositoryTests.cs`, `ReservaServiceTests.cs`, `CliIntegrationTests.cs`).
- `data/`: Almacenamiento local del inventario (`inventario.json`) y reservas persistidas (`reservas.json`).
- Scripts en raíz (`run-tests`, `reset-data`, `verify-persistence` en variantes `.bat`, `.ps1` y `.sh`): Automatización multiplataforma de pruebas, reinicio de estado y verificación end-to-end de persistencia entre procesos.

### Requisitos Previos
- Docker y Docker Compose (método recomendado sin dependencias locales).
- Alternativa local: .NET 10 SDK instalado.

### Persistencia, Ubicación de Datos y Reinicio al Estado Inicial
- **Ubicación de datos:** Las reservas se almacenan localmente en `data/reservas.json` (y el catálogo base en `data/inventario.json`). En Docker, el directorio `./data` se monta como volumen (`./data:/app/data`), asegurando que el estado sobreviva entre ejecuciones independientes y reinicios de contenedores. Las escrituras son atómicas mediante archivos temporales e intercambio con `File.Move`.
- **Reinicio al estado inicial (Advertencia de borrado):** Para restaurar el sistema a su estado inicial vacío (`[]`), ejecute `reset-data.bat` (Windows) o `./reset-data.sh` (Linux/macOS). **ADVERTENCIA:** Esta operación sobrescribe `data/reservas.json` y **elimina de forma permanente todas las reservas activas y canceladas existentes**.

### Comandos de Ejecución y Pruebas

Compilación y ejecución de pruebas con Docker:

```bash
docker compose build
docker compose run --rm tests
```

Ejecución de pruebas local (script o .NET CLI):

```powershell
.\run-tests.bat
dotnet test
```

Verificación demostrativa de persistencia tras reinicio de proceso (PowerShell o Batch):

```powershell
powershell -ExecutionPolicy Bypass -File .\verify-persistence.ps1
.\verify-persistence.bat
```

### Uso de la Interfaz CLI y Ejemplos de Entrada / Salida

Puede invocar los subcomandos localmente con `dotnet run --project src/LabReservations.Cli -- <comando>` o mediante Docker con `docker compose run --rm cli <comando>`.

1. **Consultar inventario fijo (`inventario`):**

```powershell
dotnet run --project src/LabReservations.Cli -- inventario
```

Salida:

```text
INVENTARIO FIJO DE LABORATORIO
---------------------------------------------
ID         | EQUIPO                        
---------------------------------------------
MIC-01     | Microscopio digital           
MIC-02     | Microscopio digital           
OSC-01     | Osciloscopio                  
MUL-01     | Multímetro                    
FUE-01     | Fuente de alimentación        
```

2. **Registrar una reserva (`reservar`):**

```powershell
dotnet run --project src/LabReservations.Cli -- reservar --unit MIC-01 --user "Investigador A" --start 2026-10-27T10:00 --end 2026-10-27T11:00
```

Salida:

```text
[RESERVA CREADA]
ID: RES-311F13E8
Unidad: MIC-01
Solicitante: Investigador A
Inicio: 2026-10-27T10:00
Fin: 2026-10-27T11:00
Estado: ACTIVA
```

3. **Consultar disponibilidad (`disponibilidad`):**

```powershell
dotnet run --project src/LabReservations.Cli -- disponibilidad --unit MIC-01 --start 2026-10-27T10:00 --end 2026-10-27T11:00
```

Salida:

```text
[DISPONIBILIDAD] Unidad: MIC-01 | Intervalo: 2026-10-27T10:00 a 2026-10-27T11:00 | Estado: OCUPADA
```

4. **Cancelar una reserva sin borrado físico (`cancelar`):**

```powershell
dotnet run --project src/LabReservations.Cli -- cancelar --id RES-311F13E8
```

Salida:

```text
[RESERVA CANCELADA] ID: RES-311F13E8 | Unidad: MIC-01 | Solicitante: Investigador A | Intervalo: 2026-10-27T10:00 a 2026-10-27T11:00 | Estado: CANCELADA (Turno liberado, registro conservado)
```

5. **Consultar historial de reservas (`historial`):**

```powershell
dotnet run --project src/LabReservations.Cli -- historial --unit MIC-01
```

Salida:

```text
ID             | UNIDAD   | SOLICITANTE          | INICIO           | FIN              | ESTADO    
---------------------------------------------------------------------------------------------------
RES-311F13E8   | MIC-01   | Investigador A       | 2026-10-27T10:00 | 2026-10-27T11:00 | CANCELADA 
RES-A4DC5C96   | MIC-01   | Investigador B       | 2026-10-27T11:00 | 2026-10-27T12:00 | ACTIVA    
```

### Limitaciones Conocidas
- **Ejecución local mono-proceso:** El almacenamiento en `data/reservas.json` emplea reemplazo atómico de archivo pensado para uso por consola local; no implementa bloqueo distribuido para escrituras concurrentes de alta frecuencia.
- **Sin autenticación ni gestión de usuarios:** El campo solicitante es informativo y de validación sintáctica (no vacío), conforme al alcance de la consigna.
- **Catálogo estático:** Las 5 unidades de laboratorio están fijas por diseño y no se administran altas ni bajas de equipamiento desde la CLI.

---

## 3. Preparación del agente

- Entorno y Harness: Agente de desarrollo asistido ejecutado en Google Antigravity sobre Windows 11 (PowerShell).
- Modelo: Gemini 3.8 Flash / 4 Argon.
- Instrucciones y Contexto: Suministrados de forma declarativa mediante el archivo GEMINI.md en la raíz del repositorio, definiendo reglas de dominio, estándares de prueba y control de procesos.
- Permisos y Controles: El agente tiene acceso a creación y modificación de archivos de código y pruebas. Las ejecuciones de terminal y la aceptación final de cada hito requieren confirmación y validación manual por parte del desarrollador.
- Adaptación Específica Comprobable: Instrucción matemática estricta sobre solapamiento de intervalos (< y > estrictos para permitir turnos consecutivos exactos) y prohibición expresa de borrado físico en cancelaciones, enlazada en GEMINI.md.

---

## 4. Registro de decisiones y delegación

- **Episodio 1: Diseño de Dominio, Inventario Fijo y Persistencia JSON (Completado).**
  - *Decisión Humana 1.1:* Unificación del catálogo en la clase `Inventario` con `Lazy<Inventario>`, carga desde `data/inventario.json` y fallback en memoria con las 5 unidades fijas para desacoplar dependencias de archivos en pruebas unitarias.
  - *Decisión Humana 1.2:* Incorporación defensiva de validación `!string.IsNullOrWhiteSpace` y normalización `.Trim()` en búsquedas de ID de unidad para tolerar entradas desde consola con espacios marginales.
- **Episodio 2: Servicio de Reservas, Validación de Intervalos y Cancelación No Destructiva (Completado — Commit `045efaa`).**
  - *Decisión Humana 2.1:* Verificación e inspección directa de `ExisteSuperposicionActiva` (`inicio < existente.Fin && fin > existente.Inicio`) y `CancelarReserva` en `ReservaService.cs`, confirmando admisión de límites consecutivos exactos y cancelación inmutable no destructiva con `with { Estado = EstadoReserva.Cancelada }`.
  - *Cobertura de Pruebas:* Validación automatizada en `ReservaServiceTests` cubriendo los 7 escenarios obligatorios de dominio y persistencia tras reinicio (43 pruebas totales aprobadas).
- **Episodio 3: Interfaz de Línea de Comandos (CLI), Scripts de Automatización y Verificación End-to-End (En validación humana).**
  - *Decisión de Diseño 3.1:* Implementación de parseo de argumentos nativo en `Program.cs` (`CliRunner`) sin librerías externas, forzando validación explícita del formato `yyyy-MM-ddTHH:mm` con `DateTime.TryParseExact` y códigos de salida POSIX/ estándar (`0` éxito, `1` error de validación o negocio).
  - *Decisión de Diseño 3.2:* Creación de scripts multiplataforma (`run-tests`, `reset-data` con advertencia explícita de pérdida de datos y `verify-persistence` en `.bat`, `.ps1` y `.sh` con compilación previa `-c Release --no-build`) para auditar en procesos separados la persistencia en disco, la admisión de turnos consecutivos y la liberación horaria tras cancelación no destructiva.
  - *Decisión Humana 3.3:* Exigencia de fijar `Console.OutputEncoding = System.Text.Encoding.UTF8;` como primera línea de `Program.cs` para garantizar la correcta visualización de caracteres acentuados (`Multímetro`, `Fuente de alimentación`) en consolas Windows y contenedores, e incorporación de `CliIntegrationTests.cs` (47 pruebas totales aprobadas).
