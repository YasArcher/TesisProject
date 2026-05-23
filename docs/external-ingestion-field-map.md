# Módulo de ingesta externa institucional

## Objetivo

El módulo permite tomar datasets académicos recuperados desde APIs externas, principalmente Scopus, y registrarlos en el modelo transaccional extensible sin alterar el formulario de registro manual. La información procesada alimenta posteriormente el Data Warehouse mediante el ETL de reportería.

## Flujo operativo

1. El usuario prepara el dataset institucional desde Scopus por filiación.
2. El frontend conserva el dataset recuperado y lo envía al backend.
3. El backend divide el dataset en lotes de staging.
4. Los lotes `ExternalApi` usan validación liviana, independiente de las reglas del formulario manual.
5. El procesamiento registra artículos, revistas/venues, autores/coautores y campos dinámicos.
6. El ETL actualiza el DW para que la reportería refleje la producción importada.

## Reglas de separación

- La configuración del formulario controla el registro manual del usuario.
- La ingesta externa no modifica el formulario ni obliga a crear campos visibles para el usuario.
- Los campos externos que ya tienen equivalente en el modelo se registran en tablas principales.
- Los campos externos sin equivalente directo se guardan como campos dinámicos asociados al artículo.

## Campos Scopus hacia el modelo base

| Campo externo | Destino | Observación |
| --- | --- | --- |
| `Title` | `Articles.Title` | Campo principal para identificar la publicación. |
| `Doi` | `Articles.Doi` | Se usa también para deduplicación. |
| `PublicationYear` | `Articles.Year` | Año de publicación. |
| `PublicationDate` | `Articles.PublishedAt` | Se normaliza a rango válido 1900-2200. |
| `SourceUrl` | `Articles.PublicationUrl` | URL de la publicación. |
| `IsOpenAccess` | `Articles.IsOpenAccess` | Indicador de acceso abierto. |
| `ExternalSource` | `Articles.ExternalSource` | Fuente de origen, por ejemplo Scopus. |
| `ExternalId` / `ScopusId` | `Articles.ExternalId` | Identificador externo para trazabilidad. |
| `JournalName` | `Venues.Name` | Se resuelve o crea venue. |
| `IssnCode` | `Venues.IssnCode` | Identificador de revista. |
| `JournalUrl` | `Venues.JournalUrl` | URL de revista o venue. |
| `Volume` | `Venues.VolumeNumber` | Volumen de publicación. |
| `Issue` | `Venues.IssueNumber` | Número de edición. |
| `PageRange` | `Articles.PageCount` | Se infiere solo si el rango permite calcular páginas. |
| `AuthorNames` | `ArticleParticipants.Nombre` | Primer autor como Autor; los demás como Coautor. |
| `AuthorAffiliations` | `ArticleParticipants.Affiliation` | Se asigna por posición cuando es posible. |

## Campos Scopus dinámicos

Estos campos se crean automáticamente en `FieldCatalog` con `SourceType = ExternalApi`, `IsDynamic = true`, `IsVisible = false`, `IsEditable = false` e `IsRequired = false`.

| Campo dinámico | Tipo | Uso |
| --- | --- | --- |
| `ScopusCitationCount` | int | Conteo de citas. |
| `ScopusOpenAccessStatus` | string | Estado detallado de acceso abierto. |
| `ScopusLicenseUrl` | string | URL de licencia. |
| `ScopusDocumentType` | string | Tipo documental externo. |
| `ScopusAbstract` | string | Resumen del artículo. |
| `ScopusLanguage` | string | Idioma declarado. |
| `ScopusPageRange` | string | Rango original de páginas. |
| `ScopusPublisher` | string | Editorial. |
| `ScopusEIssn` | string | E-ISSN. |
| `ScopusKeywords` | json | Palabras clave. |
| `ScopusSubjectAreas` | json | Áreas temáticas. |
| `ScopusAuthorAffiliations` | json | Afiliaciones originales de autores. |
| `ScopusRawPayload` | json | Payload completo recibido. |

## Deduplicación

El procesamiento intenta evitar duplicados en este orden:

1. DOI.
2. `ExternalSource + ExternalId`.
3. `Title + Year + Venue` cuando no existe DOI confiable.

Si encuentra un duplicado, la fila del staging se marca como procesada, queda vinculada al artículo existente y se registra una advertencia `EXTERNAL_DUPLICATE_SKIPPED`.

## Requisitos para producción

- Aplicar `docs/database/oltp/20260521_external_ingestion_indexes.sql` en la base OLTP.
- Verificar que el ETL de DW termine en estado `Success`.
- Mantener `ValidateAfterCreate` desactivado para cargas institucionales grandes.
- Para cargas superiores a miles de registros, planificar procesamiento en segundo plano con progreso y reintentos.

