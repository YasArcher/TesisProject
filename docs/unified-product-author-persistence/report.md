# Persistencia Unified de Product, ProductAuthor y Author

## A. Repositories creados

- `IUnifiedProductRepository` / `UnifiedProductRepository`
- `IUnifiedProductAuthorRepository` / `UnifiedProductAuthorRepository`
- `IUnifiedAuthorRepository` / `UnifiedAuthorRepository`

Los tres reutilizan `GenericRepository<T>` para CRUD y reciben exclusivamente
`UnifiedDideDbContext`.

## B. Contratos

### IUnifiedProductRepository

```csharp
Task<Product?> GetByIdWithRefsAsync(int productId, CancellationToken ct = default);
Task<List<Product>> GetByProjectAsync(int projectId, CancellationToken ct = default);
IQueryable<Product> QueryWithRefs(bool asNoTracking = true);
```

### IUnifiedProductAuthorRepository

```csharp
Task<List<ProductAuthor>> GetByProductAsync(int productId, CancellationToken ct = default);
Task<bool> ExistsForAuthorAsync(int productId, int authorId, CancellationToken ct = default);
IQueryable<ProductAuthor> QueryByProduct(int productId, bool asNoTracking = true);
```

### IUnifiedAuthorRepository

```csharp
Task<Author?> GetByAppUserIdAsync(int appUserId, CancellationToken ct = default);
Task<Author?> GetByExternalResearcherIdAsync(
    int externalResearcherId,
    CancellationToken ct = default);
```

Los identificadores mantienen dominios distintos:

- `AppUser.IdUser` identifica al usuario institucional de negocio.
- `ExternalResearcher.ExternalResearcherId` identifica al investigador externo.
- `Author.AuthorId` identifica la abstracción autoral que apunta exactamente a una
  de esas dos fuentes.
- `ProductAuthor.AuthorId` referencia a `Author`; nunca recibe directamente un
  `IdUser` o `ExternalResearcherId`.
- `ProductAuthor.ProductId` identifica el producto cuya autoría representa.
- `Product.ProjectId` es `int?`: con valor representa un resultado de proyecto y
  `null` representa producción científica independiente.

El DTO actual de creación todavía declara `ProjectId` como `int` obligatorio. Esa
diferencia debe resolverse en la futura migración funcional; el repository no
cambia la nulabilidad intencional del modelo Unified.

## C. ExternalAuthorId

`Author.ExternalAuthorId` todavía existe como `string?` y su configuración limita
la columna a 150 caracteres. En el modelo Unified no tiene índice, relación ni
contrato de repository. Los usos Unified actuales están limitados a la propiedad,
su configuración, migraciones/snapshot y una prueba de datos.

En Articles también existe como `string?` en `ArticleParticipant`, en sus DTOs y en
la configuración de longitud 150. El código activo de Articles lo persiste desde
la entrada, lo procesa en validaciones/campos dinámicos, lo transporta por bulk
import y lo utiliza como primera opción de identidad en reportes del DW.

Semánticamente representa al autor o investigador externo legacy. No constituye
una tercera fuente autoral. En Unified, ese actor debe resolverse mediante:

```text
ExternalResearcher.ExternalResearcherId
    -> Author.ExternalResearcherId
    -> Author.AuthorId
    -> ProductAuthor.AuthorId
```

Es redundante una vez que todos los registros legacy puedan resolverse a
`ExternalResearcher`, pero no puede eliminarse todavía. Antes se necesita una
migración de datos que inventaríe los valores, los relacione usando evidencia como
ORCID, correo, identificación, nombre y afiliación, cree o seleccione el
`ExternalResearcher` y `Author` correctos, registre casos ambiguos y valide que no
queden referencias sin resolver. No se debe igualar físicamente
`ExternalAuthorId` con `ExternalResearcherId`.

## D. Product repository

| Legacy | Unified | Comportamiento |
|---|---|---|
| `GetByIdWithRefsAsync` | `GetByIdWithRefsAsync` | Tracking, split query, ProductType, Values y autores. La ruta autoral ahora es `Product.Authors -> ProductAuthor.Author -> AppUser/ExternalResearcher`. No carga Article. |
| `GetByProjectAsync` | `GetByProjectAsync` | Filtra `Product.ProjectId == projectId`, incluye ProductType y usa `AsNoTracking`. La comparación es segura para el FK nullable. |
| `QueryWithRefs` | `QueryWithRefs` | Conserva split query, los mismos refs funcionales y el selector de tracking. La autoría usa el modelo Unified. |

No se agregó Article como dependencia obligatoria ni se optimizaron las consultas
fuera del comportamiento requerido.

## E. ProductAuthor repository

El contrato legacy `ExistsForUserAsync(productId, userId)` comparaba
`ProductAuthor.UserId`. En Unified fue reemplazado por
`ExistsForAuthorAsync(productId, authorId)`, que compara únicamente
`ProductAuthor.AuthorId`.

La resolución `AppUser -> Author` o `ExternalResearcher -> Author` debe suceder
antes de agregar o buscar una autoría. No existe equivalencia numérica entre esos
IDs. `ExternalResearcherProject` permanece separado: describe participación en un
proyecto y no se usa para autoría de productos.

## F. Author repository

`GetByAppUserIdAsync` resuelve un `Author` por `Author.AppUserId`.
`GetByExternalResearcherIdAsync` lo resuelve por
`Author.ExternalResearcherId`. Ambas consultas son de solo lectura y no crean
autores. La restricción `CK_Authors_ExactlyOneSource` del modelo exige exactamente
una fuente y los índices filtrados mantienen una relación uno a uno por fuente.

No se creó `GetByExternalAuthorIdAsync` porque ese campo es residuo de
compatibilidad y no una identidad autoral nueva.

## G. ProductService pendiente

`ProductService` no fue modificado. Su `SyncAuthorsAsync` recibe actualmente
`AuthorUserIds`, compara `ProductAuthor.UserId`, elimina asociaciones ausentes y
crea las nuevas con ese mismo `UserId`.

La futura migración institucional debe, por cada `AppUser.IdUser`, consultar
`Authors.GetByAppUserIdAsync`, decidir en el Service si debe crear el `Author`, y
sincronizar `ProductAuthor` usando el `Author.AuthorId` resultante. La resolución o
creación de autores seguirá siendo orquestación del Service.

`MapToDetailDTO` actualmente llena `ProductAuthorResponseDTO.UserId` desde
`ProductAuthor.UserId`. En Unified deberá leer
`ProductAuthor.Author.AppUserId` para autores institucionales. El DTO solo expone
`UserId`, por lo que no puede representar correctamente un autor externo; ese gap
de contrato debe decidirse antes de migrar el Service y no se inventó una forma en
esta fase.

## H. Unified Unit of Work

- Propiedades anteriores: 45.
- Nuevas: `Products`, `ProductAuthors`, `Authors`.
- Total: 48 repositories.
- Implementación: 31 especializados y 17 catálogos.
- Constructor: `UnifiedDideDbContext` más 48 interfaces, 49 dependencias totales.

Las tres instancias nuevas se reciben mediante DI explícita y se asignan
directamente. La UoW no construye repositories.

## I. Exclusiones

- Articles: no modificado.
- ArticleParticipant: no modificado.
- AspNetUser e Identity: no modificados.
- Services y Controllers: no modificados.
- `Program.cs` y DI runtime: no modificados.
- BD, modelo y migraciones: no modificados.
- Frontend y DW: no modificados.

## J. Validación

La prueba DI aislada registra contexto, 31 repositories especializados, 17
catálogos y UoW como Scoped. Verifica 48 propiedades, identidad entre cada
propiedad y la instancia resuelta por DI, un único `UnifiedDideDbContext` compartido
y 49 parámetros de constructor. No abre conexión, no ejecuta consultas y no llama
`SaveChangesAsync`.

Build solicitado:

```text
dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet
```

Resultado: 0 errores, 39 warnings preexistentes y 0 warnings nuevos.

## K. Codebase Memory

La inspección Verify confirmó las claves y navegaciones de Product, ProductAuthor,
Author, AppUser y ExternalResearcher; la separación de ExternalResearcherProject;
los Includes y tracking legacy; y el consumo de `UserId` en `SyncAuthorsAsync` y
`MapToDetailDTO`. Los archivos consultados no tenían gaps registrados, aunque la
metadata de cobertura indicaba cambios; por ello las fuentes exactas y los usos
literales de `ExternalAuthorId` también se comprobaron directamente.
