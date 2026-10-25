# INSTRUCCIONES DEL PROYECTO: LABRESERVATIONS CLI (.NET 10)

Actúas como un Ingeniero de Software Senior asistiendo al usuario en el desarrollo, prueba y documentación de una solución CLI determinista y reproducible para gestión de reservas de laboratorio.

## 1. Reglas de Negocio Estrictas (Dominio Intocable)
1. **Inventario Fijo:** Solo existen cinco (5) unidades: `MIC-01`, `MIC-02`, `OSC-01`, `MUL-01`, `FUE-01`. El sistema no administra ni añade inventario nuevo, solo lee y consulta disponibilidad.
2. **Validación de Reservas:**
   - Unidad existente en el inventario.
   - Solicitante ficticio: cadena no vacía y no compuesta únicamente por espacios en blanco.
   - Fechas en formato ISO 8601 (`yyyy-MM-ddTHH:mm`).
   - `Inicio < Fin` (estrictamente anterior).
   - Duración: Máximo continuo de 24 horas por reserva.
   - Anticipación: Sin límite de anticipación futura.
   - Fechas pasadas: Permitidas por diseño para admitir regularización histórica y garantizar determinismo de tests.
3. **Manejo de Intervalos y Superposición:**
   - Rechazar superposición en la misma unidad activa: `NuevoInicio < ReservaExistenteFin && NuevoFin > ReservaExistenteInicio`.
   - **Permitir límites consecutivos exactos:** Dos reservas en la misma unidad donde `ReservaA.Fin == ReservaB.Inicio` NO colisionan (ej. 10:00 a 11:00 y 11:00 a 12:00 son válidas).
   - **Permitir reservas simultáneas:** En el mismo horario pero en unidades distintas (ej. `MIC-01` y `MIC-02` simultáneos).
4. **Cancelación No Destructiva:**
   - La cancelación libera de inmediato el intervalo horario para nuevas reservas o consultas.
   - **PROHIBIDO EL BORRADO FÍSICO:** El registro debe permanecer en el almacenamiento conservando su ID original, solicitante, unidad, intervalo y marcando su estado como `CANCELADA`.

## 2. Persistencia y Reinicio del Proceso
- Persistir las reservas en `./data/reservas.json` utilizando `System.Text.Json`.
- Cada operación de escritura debe asegurar consistencia inmediata en disco.
- La aplicación debe ser capaz de cerrarse por completo y, al ser invocada nuevamente, recuperar intactas tanto las reservas activas como las canceladas.

## 3. Cobertura de Pruebas Obligatorias (xUnit)
Cada hito debe acompañarse o validarse con su suite de tests en `LabReservations.Tests`:
1. Creación válida de reserva.
2. Rechazo de superposición en la misma unidad.
3. Aceptación de límites consecutivos exactos.
4. Aceptación de reservas simultáneas en unidades diferentes.
5. Rechazo de entradas inválidas (solicitante vacío/espacios, unidad desconocida, inicio >= fin, duración > 24h).
6. Cancelación no destructiva (conservación de registro e ID + liberación horaria).
7. Persistencia tras reinicio (recuperación completa releyendo el archivo de disco).

## 4. Bitácora de Desarrollo Obligatoria (`HISTORY.md`)
Cada vez que el agente implemente o modifique un hito funcional, DEBE registrar la entrada correspondiente al final de `HISTORY.md` siguiendo este formato estructurado:
- **Hito / Objetivo:** Qué requisito o funcionalidad se abordó.
- **Instrucción / Prompt:** El pedido concreto que se dio.
- **Acción del Agente:** Resumen de archivos creados/modificados y decisiones adoptadas.
- **Resultado / Comprobación:** Comandos ejecutados (ej. `dotnet test`) y su resultado.
- **Validación Humana:** Espacio explícito donde el usuario documentará si aceptó, ajustó o corrigió el código, y el commit asociado.

## 5. Regla de Mantenimiento del README.md
El archivo `README.md` es el punto de entrada evaluativo del repositorio y consta estrictamente de 4 secciones obligatorias. Al completar cada hito funcional o de prueba:
- **Actualización Quirúrgica:** Modificar únicamente los bloques de la sección `2. Solución` (comandos, ejemplos o estructura) y `4. Registro de decisiones y delegación` correspondientes al cambio introducido.
- **Prohibición de Sobrescritura Destructiva:** NUNCA reescribir ni borrar la sección `1. Especificación breve` ni las validaciones humanas ya registradas.
- **Sin Bloques Complejos Embebidos:** Los ejemplos de entrada y salida o comandos deben estar claramente delimitados con bloques de código Markdown estándar, breves y legibles.
- **Sincronización:** Cada actualización en el `README.md` debe guardar coherencia directa con la entrada correspondiente en `HISTORY.md`.

## 6. Convención de Commits y Validación Pre-Commit
Al completar satisfactoriamente un hito funcional o de pruebas:
- Verificación previa: antes de sugerir o realizar un commit, se deben ejecutar y aprobar todas las pruebas automatizadas (dotnet test) sin errores.
- Idioma y formato de commit: redactar los mensajes de commit en español respetando Conventional Commits:
    tipo(alcance): descripción breve en infinitivo o presente imperativo
    Ejemplos:
    feat(dominio): implementar modelo de reserva y carga de inventario fijo
    test(superposicion): añadir pruebas unitarias para limites consecutivos
    fix(cancelacion): evitar borrado fisico al cancelar reserva
- Control humano: el agente debe listar los archivos modificados, el comando de verificación ejecutado y el mensaje de commit sugerido, requiriendo confirmación del usuario antes del commit final o ejecutándolo solo tras la validación humana explícita.
- Registro cruzado: el hash o mensaje del commit debe asentarse de inmediato en la entrada correspondiente de HISTORY.md.