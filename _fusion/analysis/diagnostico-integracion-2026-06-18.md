# Diagnostico completo de integracion progresiva

Fecha: 2026-06-18
Workspace: C:\Users\Personal\Source\Repos\TesisProject_FusionWorkspace
Base de comparacion: C:\Users\Personal\Source\Repos\TesisProject_FusionWorkspace_backup_20260617_055254

## Conclusion ejecutiva

La integracion mantiene al sistema de proyectos como arquitectura base y no ha modificado entidades, repositorios, servicios, controladores, pantallas ni flujos funcionales propios de proyectos. Los cambios sobre archivos preexistentes se concentran en seis puntos de extension: Program.cs, appsettings.json, docker-compose.yml y los tres archivos csproj.

El sistema compila con 0 errores. Sin embargo, el modulo de articulos no debe habilitarse todavia porque falta verificar el esquema real de TesisDB_Extensible, corregir diferencias funcionales del servicio de lectura y estabilizar el workspace.

## Estado positivo

- El codigo original de articulos permanece copiado de forma pasiva en ArticlesMigration.
- ArticlesMigration esta excluido de compilacion en backend, frontend y shared.
- El frontend activo de proyectos no tiene referencias a articulos.
- El UnitOfWork de proyectos no fue modificado.
- No se agregaron migraciones de articulos al arranque del sistema de proyectos.
- ArticlesDbContext se registra solo cuando ArticlesModule:Enabled es true.
- ArticlesOltpConnection es obligatoria al habilitar el modulo y no usa DefaultConnection.
- ArticlesOlapConnection esta reservada para TesisDW_Extensible.
- El controlador migrado es de solo lectura y esta protegido por roles.
- Todo archivo activo procedente de articulos contiene la marca ARTICLES-MIGRATION.
- La ruta api/articles no colisiona con controladores existentes.
- Compilacion actual: 0 errores y 0 advertencias en la ultima ejecucion incremental.

## Cambios sobre archivos base de proyectos

Solo se modificaron:

1. tesisproject.backend/Program.cs
2. tesisproject.backend/appsettings.json
3. docker-compose.yml
4. tesisproject.backend/tesisproject.backend.csproj
5. tesisproject.frontend/tesisproject.frontend.csproj
6. tesisproject.shared/tesisproject.shared.csproj

Las modificaciones funcionales corresponden exclusivamente al registro condicionado del modulo de articulos, sus conexiones y la exclusion de la copia pasiva.

## Hallazgos criticos

### 1. Workspace sin control de versiones

TesisProject_FusionWorkspace no contiene carpeta .git. Existe un respaldo completo, pero no hay commits ni rollback granular. Continuar una fusion extensa en estas condiciones eleva el riesgo de perder una etapa estable o no poder identificar el origen de una regresion.

Accion recomendada: inicializar control de versiones en el workspace de fusion o convertirlo en un repositorio privado independiente antes de la siguiente fase.

### 2. Corrupcion de codificacion en Program.cs

La comparacion con el respaldo detecto alteracion accidental de textos preexistentes:

- Universidad Tecnica de Ambato
- despues de migrar
- Genericos

Tambien se introdujeron BOM, cambios de saltos de linea y lineas finales innecesarias. No rompe la compilacion, pero modifica contenido del sistema base sin relacion funcional con articulos.

Accion recomendada: restaurar Program.cs desde el respaldo y reaplicar solamente los bloques ARTICLES-MIGRATION mediante un parche minimo, preservando codificacion y formato originales.

### 3. Archivo accidental en frontend

Existe tesisproject.frontend/Repos.lnk, que no pertenece a proyectos ni a articulos.

Accion recomendada: eliminarlo de forma controlada despues de verificar que no sea requerido por el usuario.

## Hallazgos altos

### 4. Identidad incompatible

Articulos utiliza ApplicationUser/ApplicationRole con claves string. Proyectos usa IdentityUser<int>/IdentityRole<int>. Workflow, staging y auditoria de articulos dependen directamente de la identidad antigua.

Riesgo: migrar workflow antes de resolver esta frontera produciria relaciones incompatibles y duplicacion de autenticacion.

Ruta segura: conservar una sola autenticacion, la del sistema de proyectos, y crear una capa de adaptacion para responsables, autor institucional, UODIDE y Area Tecnica.

### 5. Falta de pruebas automatizadas

La carpeta tests contiene logs y evidencias, pero no proyectos csproj ejecutables. dotnet test finaliza sin ejecutar suites.

Riesgo: la compilacion no detecta regresiones de consultas, filtros, permisos o comportamiento HTTP.

Ruta segura: crear un proyecto de pruebas separado para el modulo migrado antes de activar escritura o workflow.

### 6. Esquema OLTP aun no validado en ejecucion

Las 20 entidades nucleares y sus configuraciones conservan el modelo original; las diferencias son namespace y trazabilidad. Sin embargo, no se ha ejecutado una validacion read-only contra TesisDB_Extensible.

Riesgo: nombres, tipos, indices o relaciones pueden compilar pero diferir del esquema restaurado.

Ruta segura: ejecutar una prueba de conectividad y consultas de solo lectura con ArticlesModule habilitado en un entorno controlado, sin migraciones automaticas.

## Hallazgos medios

### 7. Lectura adaptada no es equivalente al servicio original

Diferencias actuales:

- La busqueda adaptada no incluye nombre de revista.
- PublicationStatusKey usa igualdad directa y perdio equivalencias PUBLICADO/ACEPTADO/SIN ESTADO.
- PageSize maximo paso de 500 a 100.
- El orden predeterminado cambio de Id descendente a CreatedAt descendente.

Estas diferencias no rompen proyectos, pero deben corregirse antes de declarar funcional el listado de articulos.

### 8. Controlador visible aun con modulo apagado

api/articles existe y devuelve 503 cuando ArticlesModule esta deshabilitado. Es seguro, pero aparecera en Swagger y expone una funcion incompleta.

Recomendacion: mantenerlo temporalmente o excluir el controlador del descubrimiento MVC cuando el modulo este apagado.

### 9. Contratos shared activados antes que sus modulos

Se compilaron DTOs de workflow, reporteria, IA, ingesta y configuracion aunque sus servicios aun no estan integrados. No generan comportamiento ni errores, pero amplian prematuramente la superficie compartida.

Recomendacion: aceptable como inventario activo, aunque una integracion estricta deberia activar DTOs modulo por modulo.

### 10. Configuracion base reformateada

appsettings.json fue reescrito completamente por ConvertTo-Json. Los valores funcionales de proyectos se conservaron, pero el cambio genera ruido de versionado y BOM.

Recomendacion: restaurar formato base y agregar solo ArticlesOltpConnection, ArticlesOlapConnection y ArticlesModule.

## Entidades pendientes

Siguen excluidas correctamente:

- WorkflowDefinition, WorkflowInstance y etapas
- ImportBatch, filas, valores y errores
- RegistrationMatrix y componentes
- IntelligenceTrainingRun y metricas
- ReportingPerformanceMetric
- AuditLog
- Project del sistema de articulos

Estas entidades no deben activarse individualmente. Deben migrarse por bloques coherentes.

## Evaluacion de riesgo actual

- Riesgo para funcionalidad de proyectos con ArticlesModule=false: bajo.
- Riesgo de habilitar lectura de articulos sin validar BD: medio-alto.
- Riesgo de migrar workflow antes de identidad: alto.
- Riesgo de continuar sin Git propio: alto.
- Riesgo de integrar OLAP/IA antes del ETL: alto.

## Ruta segura recomendada

### Fase 0: estabilizacion obligatoria

1. Crear control de versiones para FusionWorkspace.
2. Corregir Program.cs preservando exactamente el codigo base y reaplicando solo bloques ARTICLES-MIGRATION.
3. Restaurar formato de appsettings.json y docker-compose.yml, manteniendo solo adiciones marcadas.
4. Eliminar Repos.lnk tras confirmacion.
5. Crear una nueva instantanea estable.

### Fase 1: validar lectura OLTP

1. Corregir equivalencia funcional de busqueda, estados, paginacion y orden.
2. Crear pruebas para ArticleQueryService y ArticleReadRepository.
3. Configurar ArticlesOltpConnection en un entorno local controlado.
4. Validar conteo, listado, detalle, participantes, revistas y campos dinamicos.
5. Mantener escritura deshabilitada.

### Fase 2: frontend minimo

1. Crear Features/Articles usando componentes del sistema de proyectos.
2. Reutilizar ApiClient y autenticacion de proyectos.
3. Agregar navegacion condicionada por bandera y permiso.
4. No copiar MainLayout, SideBar, Auth ni TokenStore de articulos.

### Fase 3: identidad y permisos

1. Mapear roles de articulos a permisos del sistema base.
2. Diseñar referencia de usuario compatible con Identity int.
3. Separar autores institucionales de usuarios administrativos.

### Fase 4: bloque transaccional

Migrar conjuntamente staging, matriz, workflow y auditoria, con pruebas de flujo completas.

### Fase 5: OLAP, ETL, reportería e IA

Usar ArticlesOlapConnection y conservar TesisDW_Extensible independiente. Integrar primero ETL, luego reportería y finalmente IA.

## Dictamen

La arquitectura elegida es viable y el aislamiento actual protege al sistema de proyectos. Antes de continuar con frontend o workflow, debe ejecutarse la Fase 0 y validar el primer flujo de lectura contra la base OLTP real. No se recomienda activar ArticlesModule en el estado actual.
