# Unified backend legacy cleanup

El runtime backend usa únicamente:

- `UnifiedDideDbContext` para Projects, Articles e Identity;
- `ProjectsDwContext` para ProjectsDW;
- `ArticlesDwContext` para ArticlesDW.

Se retiraron `AppDbContext`, su factory y bootstrap, `DefaultConnection`, los
fallbacks DW, `Data/Articles`, `Migrations/Articles`, `ArticlesMigration` y los
contratos base históricos de Articles. `GenericRepository<T>` e
`IGenericRepository<T>` permanecen como infraestructura Unified; se eliminó solo
el constructor que aceptaba `AppDbContext`.

Las migrations antiguas de `AppDbContext` permanecen en
`tesisproject.backend/LegacyHistory/AppDbContextMigrations`, excluidas de
compilación. Git conserva la historia de los directorios eliminados. No se
modificaron datos ni tablas de historial EF.

Las entidades de `tesisproject.shared` se preservan por seguridad contractual.
No tienen consumidores runtime backend, pero su eliminación física requiere una
validación separada del ensamblado compartido y de cualquier consumidor externo.
