# Avance de integracion: capa de datos de articulos

Fecha: 2026-06-17
Workspace: C:\Users\Personal\Source\Repos\TesisProject_FusionWorkspace

## Objetivo

Preparar el sistema de articulos para integrarse al sistema de proyectos respetando la arquitectura base del sistema de proyectos, sin romper su compilacion ni mezclar de forma prematura los contextos, identidad, workflow o servicios.

## Cambio aplicado

Se creo una primera capa activa e independiente para el modelo cientifico principal del sistema de articulos en:

- tesisproject.backend\Data\Articles\ArticlesDbContext.cs
- tesisproject.backend\Data\Articles\Entities
- tesisproject.backend\Data\Articles\Configurations

Todos los archivos incorporados fueron marcados con el comentario:

// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.

## Entidades activadas en esta fase

Se integraron entidades centrales del dominio de articulos:

- Article
- ArticleParticipant
- ArticleFile
- ArticleIndexing
- AcademicTerm
- PublicationStatus
- ResearchLine
- IndexingSource
- Faculty
- BroadField
- SpecificField
- DetailedField
- Venue
- VenueMetric
- FieldCatalogEntry
- DynamicFieldOption
- FormDefinition
- FormFieldDefinition
- DynamicFieldValue
- ArticleParticipantDynamicFieldValue

## Configuraciones activadas

Se incorporaron configuraciones de Entity Framework para:

- Catalogos academicos
- Articulos
- Participantes
- Archivos
- Indexaciones
- Formularios dinamicos
- Campos OCDE
- Revistas y metricas

## Criterio de seguridad aplicado

No se fusiono todavia el modelo completo de articulos dentro del AppDbContext principal del sistema de proyectos. Se creo ArticlesDbContext como contexto temporal separado para evitar conflictos con:

- Identity del sistema de proyectos
- Usuarios y roles del sistema de articulos
- Workflow de articulos
- Staging/importacion masiva
- Matriz de registro
- Modulo de IA
- Servicios transaccionales existentes

Estos modulos quedan pendientes para una fase posterior, donde deben adaptarse a la identidad, repositorios y UnitOfWork del sistema de proyectos.

## Resultado de compilacion

Comando ejecutado:

dotnet build "C:\Users\Personal\Source\Repos\TesisProject_FusionWorkspace\TesisProject.sln" --no-restore -v:minimal

Resultado:

- Compilacion correcta
- 0 errores
- 20 advertencias existentes del sistema base de proyectos

## Estado actual

La fusion fisica y la primera activacion tecnica del modelo de datos de articulos estan aplicadas de forma segura. El sistema de proyectos sigue compilando y el sistema de articulos permanece separado por trazabilidad.

## Siguiente paso recomendado

Registrar ArticlesDbContext de forma controlada en la configuracion del backend o iniciar la adaptacion de repositorios para articulos siguiendo el patron del sistema de proyectos. La ruta recomendada es:

1. Registrar ArticlesDbContext sin exponer endpoints aun.
2. Crear repositorios de lectura para articulos y catalogos.
3. Adaptar servicios de articulos gradualmente.
4. Integrar endpoints minimos.
5. Migrar workflow/staging cuando identidad y permisos esten alineados.

## Avance adicional: repositorios de lectura

Se agregaron repositorios temporales de lectura para articulos siguiendo la organizacion del sistema de proyectos:

- tesisproject.backend\Repositories\Interfaces\IArticleReadRepository.cs
- tesisproject.backend\Repositories\Implementations\ArticleReadRepository.cs

Estos repositorios trabajan contra ArticlesDbContext y permiten preparar consultas de articulos sin acoplar todavia el modulo al UnitOfWork principal ni exponer endpoints. Se mantienen como una capa de transicion segura para revisar datos, detalle y busquedas antes de fusionar servicios reales.

## Validacion posterior

Se ejecuto nuevamente:

dotnet build "C:\Users\Personal\Source\Repos\TesisProject_FusionWorkspace\TesisProject.sln" --no-restore -v:minimal

Resultado:

- Compilacion correcta
- 0 errores
- 20 advertencias existentes del sistema base

## Pendiente inmediato

Antes de activar endpoints o UI, se recomienda registrar ArticlesDbContext en la configuracion del backend con una cadena de conexion controlada y despues decidir si los repositorios de articulos entran al UnitOfWork principal o permanecen como repositorios especializados por modulo.

## Registro controlado en inyeccion de dependencias

Se actualizo Program.cs para registrar:

- ArticlesDbContext con la cadena DefaultConnection.
- IArticleReadRepository con ArticleReadRepository.

El bloque fue marcado con comentarios [ARTICLES-MIGRATION]. ArticlesDbContext no fue agregado a ApplyMigrationsAsync y el repositorio no fue incorporado al UnitOfWork principal. Esta decision evita modificar automaticamente el esquema o mezclar transacciones de dos contextos antes de consolidar la arquitectura.

## Comprobaciones finales de la fase

- Solucion completa: compilacion correcta.
- Errores de compilacion: 0.
- Advertencias: 20, pertenecientes al sistema base y no introducidas por esta fase.
- dotnet test --no-build --no-restore: finalizo correctamente; la solucion no reporto suites ejecutables en la salida actual.
- Repositorio original C:\Users\Personal\Source\Repos\TesisProject: sin cambios locales.

## Proxima fase segura

Crear una capa de servicio de consulta para articulos que devuelva DTOs del shared, seguida de un controlador de lectura minimo. Antes de habilitar escritura, se debe definir la estrategia definitiva de base de datos, identidad y transacciones.

## Servicio de consulta y controlador protegido

Se incorporaron los siguientes componentes:

- tesisproject.shared\DTOs\Articles\ArticlePageDto.cs
- tesisproject.backend\Services\Interfaces\IArticleQueryService.cs
- tesisproject.backend\Services\Implementations\ArticleQueryService.cs
- tesisproject.backend\Options\ArticlesModuleOptions.cs
- tesisproject.backend\Controllers\ArticlesController.cs

El repositorio de lectura fue ampliado para soportar busqueda, filtros institucionales, ordenamiento, paginacion y detalle con participantes, indexaciones, revista, metricas y campos dinamicos visibles.

El controlador expone exclusivamente operaciones GET, exige roles de lectura del sistema base y esta protegido por ArticlesModule:Enabled. El valor predeterminado es false cuando la seccion no existe, por lo que no consulta la base de articulos hasta habilitarlo de forma explicita.

ArticlesDbContext ahora acepta ConnectionStrings:ArticlesConnection y utiliza DefaultConnection solamente como compatibilidad temporal si la conexion especifica no esta definida.

No se agregaron operaciones de escritura, migraciones automaticas, cambios al UnitOfWork ni componentes visuales.

## Validacion de esta fase

- Compilacion completa: correcta.
- Errores: 0.
- Advertencias en compilacion incremental: 20, existentes en el sistema base.
- No existen proyectos de pruebas unitarias ejecutables en el workspace; tests contiene evidencias y logs de pruebas previas.

## Siguiente decision arquitectonica

Definir donde residiran definitivamente las tablas de articulos:

1. En la misma base del sistema integrado, consolidando esquemas y migraciones.
2. En una base separada mediante ArticlesConnection, manteniendo contextos independientes.

Hasta tomar esta decision, ArticlesModule debe permanecer deshabilitado.

## Decision confirmada: persistencia separada

Se definio que el sistema integrado conservara tres almacenes independientes durante la migracion progresiva:

- Base transaccional del sistema de proyectos: administrada por AppDbContext.
- Base OLTP de articulos: TesisDB_Extensible, administrada por ArticlesDbContext mediante ArticlesOltpConnection.
- Base OLAP de articulos: TesisDW_Extensible, reservada mediante ArticlesOlapConnection para ETL, reportería e IA.

Se elimino el fallback que permitia a ArticlesDbContext utilizar DefaultConnection. Cuando ArticlesModule:Enabled sea true, ArticlesOltpConnection es obligatoria y no puede estar vacia. Cuando el modulo esta deshabilitado, ArticlesDbContext y sus repositorios no se registran; un servicio inerte mantiene resoluble el controlador sin acceder a ninguna base.

Docker Compose y .env.example incluyen variables independientes para OLTP y OLAP. El modulo permanece apagado por defecto.

Validaciones realizadas:

- appsettings.json valido.
- docker compose config valido.
- Compilacion completa correcta con 0 errores.
- Las advertencias Docker restantes corresponden a variables preexistentes del sistema base de proyectos, no al modulo de articulos.
