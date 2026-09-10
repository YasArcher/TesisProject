# Articles: base de lectura Unified

## Clasificación de los campos actuales de Unified Article

| Clasificación | Campos / relaciones | Decisión |
|---|---|---|
| PRODUCT_VALUE | Doi, Year, PublicationUrl | Reemplazo demostrado por ProductAttributeId 8, 9 y 10. La migration traslada datos y elimina las columnas de Article y su mapping EF. |
| ARTICLE_STRUCTURAL | Id, ProductId, Product | Identidad de la extensión y relación 1:1 con el producto. |
| ARTICLE_STRUCTURAL | VenueId/Venue, AcademicTermId/AcademicTerm, PublicationStatusId/PublicationStatus, ResearchLineId/ResearchLine, BroadFieldId/BroadField, SpecificFieldId/SpecificField, DetailedFieldId/DetailedField, FacultyId/Faculty | Relaciones explícitas. Un texto de revista/base de datos no reemplaza una FK ni una relación de indexación. |
| ARTICLE_STRUCTURAL | ExternalSource, ExternalId | Procedencia de la extensión. No son identificadores de autor. |
| ARTICLE_STRUCTURAL | Files, Indexings, DynamicFieldValues | Colecciones estructurales existentes; no se unen como filas a la vista para evitar multiplicar productos. DynamicFieldValues conserva la relación de extensión existente; este cambio no crea una segunda escritura de atributos base. |
| ARTICLE_STRUCTURAL (conservación) | PublishedAt, PageCount, HasInterculturalComponent, IsOpenAccess, ProceedingsName, Proceedings, EventName, GroupName, Filiacion | Metadata existente sin reemplazo canónico demostrado en los atributos 3–10. Se conserva; esta clasificación no impide una futura migración cuando exista una definición equivalente. PublishedAt no se sustituye por Year; Filiacion del artículo no se sustituye por el snapshot de una persona. |

Title, CreatedAt y ProjectId ya pertenecen a Product. La autoría pertenece a ProductAuthors; no se añade ninguna fuente alternativa.

## Contrato de dbo.ArticleReadView

- Incluye todos los Products de tipo 1 (PRODUCCIÓN CIENTÍFICA) o 2 (PRODUCCIÓN REGIONAL), incluso sin extensión Article o valores configurados.
- ProductValues se une por AttributeDefinitionId a ProductAttributeDefinitions, con el mismo ProductTypeId del producto, y luego a ProductAttributes. Nunca se usa un ID físico de ProductValue como clave de atributo.
- IDs canónicos: 3 Journal, 4 IndexingDatabase, 5 Sjr, 6 Quartile, 7 Issn, 8 Doi, 9 Year, 10 PublicationUrl.
- No hay fallback desde Article/Venue/VenueMetric para esos valores. Venue se conserva como relación estructural; sus datos de catálogo no son la fuente de los atributos de esta vista.
- Si hay varias definiciones del mismo atributo/tipo, la lectura selecciona el ProductValue más reciente por UpdatedAt ?? CreatedAt, y después Id descendente para desempatar. La transferencia de columnas antiguas rechaza definiciones duplicadas en lugar de elegir una escritura arbitraria.
- Valores numéricos inválidos producen NULL mediante TRY_CONVERT; YearRaw y SjrRaw conservan el texto para poder identificarlos. No se interpreta silenciosamente una coma decimal ni otro formato local.
- FacultyId es exclusivamente Article.FacultyId. ProjectFacultyId se obtiene de la relación Product.ProjectId. No existe COALESCE ni precedencia implícita.
- El SELECT DISTINCT hace que SQL Server rechace INSERT/UPDATE/DELETE a través de la vista. EF usa HasNoKey/ToView y no permite rastrear instancias para persistirlas. No se crean triggers ni procedimientos almacenados.
- IUnifiedArticleReadRepository.Query() expone IQueryable sin materializar. Sus Where/OrderBy/Skip/Take se traducen a SQL y su vida útil corresponde al DbContext scoped.

## Migration y datos previos

20260908194950_AddArticleReadView transfiere los tres atributos antes de eliminar columnas, en la transacción de la migration. Reutiliza definiciones por tipo/atributo y crea la asignación si falta y existe el atributo canónico. No crea ni renumera los atributos de la semilla.

Se detiene con error explícito ante un atributo canónico necesario ausente, definiciones ambiguas, extensión con datos fuera de tipos 1/2 o discrepancia entre Article y un ProductValue no vacío. No sobreescribe el valor canónico. Los datos de aplicación no se migraron durante esta fase; se ejecutó únicamente contra una base de prueba temporal.

Down restaura las columnas desde la vista antes de eliminarla y vuelve a crear el índice DOI anterior. Puede rechazar datos nuevos incompatibles con longitudes/unicidad legacy; en ese caso SQL Server revierte el rollback en lugar de truncarlos. Los ProductValues se conservan.

## Verificación

Ejecutar `dotnet run --project tests/tesisproject.articlereadtests -c Release`.
Requiere SQL Server local `.\DINNOVA` con autenticación Windows. Crea una base `tesis_articles_read_test_<guid>` exclusiva y la elimina al terminar. No abre bases de aplicación, Projects ni DW.

Los tests ejercitan transferencia, conflicto/rollback, pivot de atributos con IDs de definiciones distintos a IDs canónicos, exclusión de definiciones de otro tipo, productos sin extensión, filtros SQL, conversiones inválidas, escrituras rechazadas por SQL y EF, ausencia de columnas duplicadas y correspondencia entre snapshot y modelo.

## Endurecimiento previo a escritura

`20260908201802_HardenArticleReadModel` añade `UX_ProductAttributeDefinitions_Type_Attribute`, único sobre `(ProductTypeId, ProductAttributeId)`. Datos existentes duplicados impiden aplicar la migration; no se elige ni elimina una definición automáticamente.

El DOI no vacío vuelve a tener unicidad global para Products de tipos 1/2 mediante `dbo.ArticleDoiUniqueness`, una vista auxiliar con SCHEMABINDING y un índice UNIQUE CLUSTERED. SQL Server mantiene el índice al insertar/actualizar ProductValues y al cambiar Products o ProductAttributeDefinitions, incluyendo concurrencia. No requiere un Article estructural ni validación de un service. La vista aprobada `ArticleReadView` queda intacta.

La clave es SHA-256 del DOI completo después de trim de espacios exteriores y conversión a minúsculas con collation explícita. Así no se trunca el nvarchar(max) de ProductValues ni se limita el almacenamiento de otros atributos. NULL y vacío/espacios quedan fuera. Una eventual colisión de hash se rechazaría conservadoramente como duplicado; nunca permitiría duplicar un DOI normalizado. Es un índice derivado, no otra fuente editable de DOI. Los clientes SQL deben respetar las opciones SET requeridas por vistas indexadas; las escrituras SqlClient/EF utilizadas por los tests funcionan con esta garantía.

`BaseProductTypeId` y `BaseProductAttributeId` documentan únicamente los seeds base. Las claves de las entidades continúan siendo enteros y admiten nuevos registros fuera de los enums. Las migrations no dependen de estos enums mutables: sus IDs permanecen literales históricos.

El SQL original reside ahora en `20260908194950_AddArticleReadView.Sql.cs`, dentro de una clase privada de esa migration, con los mismos literales SQL. No es un generador reutilizable de la VIEW actual. Cambios posteriores deben incorporarse en nuevas migrations.

Tests mínimos de esta fase: `dotnet run --project tests/tesisproject.articlereadtests -c Release -- --hardening`. Ejecutan la cadena desde cero en una base temporal exclusiva y verifican los IDs base y el rechazo de duplicados tanto en inserciones como en actualizaciones que entrarían al ámbito DOI.
