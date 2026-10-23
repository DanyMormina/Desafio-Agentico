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
- src/LabReservations.Cli: Aplicación de consola en C# (.NET 10).
- tests/LabReservations.Tests: Suite de pruebas unitarias e integrales en xUnit.
- data/: Almacenamiento local del inventario (inventario.json) y reservas persistidas (reservas.json).
- scripts/: Scripts de conveniencia para ejecución y pruebas.

### Requisitos Previos
- Docker y Docker Compose (método recomendado sin dependencias locales).
- Alternativa local: .NET 10 SDK instalado.

### Persistencia y Estado
Los datos persisten en data/reservas.json. Para Docker, este directorio se monta como volumen (./data:/app/data), asegurando persistencia entre detenciones y reinicios de contenedores.

### Comandos de Ejecución y Pruebas
Compilación y verificación inicial con Docker:

    docker compose build

Ejecución de pruebas automatizadas:

    docker compose run --rm tests

---

## 3. Preparación del agente

- Entorno y Harness: Agente de desarrollo asistido ejecutado en Google Antigravity sobre Windows 11 (PowerShell).
- Modelo: Gemini 3.8 Flash / 4 Argon.
- Instrucciones y Contexto: Suministrados de forma declarativa mediante el archivo GEMINI.md en la raíz del repositorio, definiendo reglas de dominio, estándares de prueba y control de procesos.
- Permisos y Controles: El agente tiene acceso a creación y modificación de archivos de código y pruebas. Las ejecuciones de terminal y la aceptación final de cada hito requieren confirmación y validación manual por parte del desarrollador.
- Adaptación Específica Comprobable: Instrucción matemática estricta sobre solapamiento de intervalos (< y > estrictos para permitir turnos consecutivos exactos) y prohibición expresa de borrado físico en cancelaciones, enlazada en GEMINI.md.

---

## 4. Registro de decisiones y delegación

- Episodio 1: Diseño de Dominio, Inventario Fijo y Persistencia JSON (En progreso).
- Episodio 2: Validación de Intervalos y Superposición (Pendiente).
- Episodio 3: Cancelación No Destructiva y Verificación de Reinicio (Pendiente).