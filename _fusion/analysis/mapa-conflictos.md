# Mapa inicial de conflictos para fusion

Este documento detecta conflictos por nombre y zonas criticas. No implica que todos deban eliminarse; marca puntos a revisar antes de activar codigo.

## Zonas criticas detectadas

- Program.cs existe en proyectos y articulos. Mantener el de proyectos y migrar registros necesarios desde articulos.
- AppDbContext existe en proyectos y articulos. Decidir contexto separado temporal o integracion al contexto de proyectos.
- AuthController y servicios de autenticacion existen en ambos. Debe prevalecer seguridad de proyectos.
- AppRoles y roles deben mapearse antes de activar permisos de articulos.
- Catalogos existen en ambos sistemas y requieren mapa antes de fusionar.
- ApiClient y clientes HTTP deben unificarse alrededor del cliente base de proyectos.
- MainLayout, SideBar, App.razor e _Imports.razor deben mantenerse desde proyectos.
- docker-compose, .env y appsettings deben prevalecer desde proyectos y recibir variables de articulos gradualmente.

## Conflictos backend por nombre de archivo

- AppDbContext.cs
- AppDbContextModelSnapshot.cs
- AuthController.cs
- IAuthService.cs
- Program.cs

## Conflictos frontend por nombre de archivo

- _Imports.razor
- ApiClient.cs
- app.css
- App.razor
- AuthMessageHandler.cs
- ConfirmDialog.razor
- ConfirmDialog.razor.cs
- ConfirmEnums.cs
- GlobalUsings.cs
- IApiClient.cs
- Index.razor
- input.css
- ITokenStore.cs
- LocalTokenStore.cs
- Login.razor
- MainLayout.razor
- MainLayout.razor.css
- Program.cs
- SideBar.razor
- tailwind.css

## Conflictos shared por nombre de archivo

- Sin elementos detectados.

## Regla de trazabilidad

Todo codigo activado desde ArticlesMigration debe incluir comentario [ARTICLES-MIGRATION] o [ARTICLES-INTEGRATED] segun corresponda.

## Prioridad recomendada

- Activar DTOs no conflictivos de articulos en shared.
- Definir estrategia de AppDbContext/contexto de articulos.
- Migrar servicios backend por modulo, empezando por catalogos y registro.
- Adaptar controladores a seguridad y convenciones de proyectos.
- Migrar frontend por features, no por frontend completo.
- Unificar navegacion, permisos y layouts.
