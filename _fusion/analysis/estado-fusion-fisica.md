# Estado de fusion fisica

Fecha: 2026-06-17

## Objetivo

Preparar el sistema de proyectos como base principal y colocar el sistema de articulos dentro de la misma estructura fisica de backend, frontend y shared, sin activar todavia la integracion funcional.

## Base activa

- Backend base: `tesisproject.backend/`
- Frontend base: `tesisproject.frontend/`
- Shared base: `tesisproject.shared/`

Estos corresponden al sistema de proyectos y se mantienen como sistema principal.

## Articulos incorporado fisicamente

- Backend de articulos: `tesisproject.backend/ArticlesMigration/`
- Frontend de articulos: `tesisproject.frontend/Features/ArticlesMigration/`
- Shared de articulos: `tesisproject.shared/ArticlesMigration/`

Tambien se conserva la copia completa original en:

- `_fusion/articles-source/`

## Estado de compilacion

Las carpetas `ArticlesMigration` fueron excluidas temporalmente de compilacion en los archivos `.csproj` para evitar conflictos iniciales por nombres, rutas, DbContexts, controladores, DTOs y componentes Razor duplicados.

## Siguiente ruta segura

1. Generar inventario de controladores, servicios, entidades, DTOs y componentes.
2. Detectar duplicados entre proyectos y articulos.
3. Definir que se migra como modulo nuevo, que se fusiona y que se elimina.
4. Activar primero contratos compartidos necesarios.
5. Integrar backend de articulos por modulo.
6. Integrar frontend por paginas y rutas.
7. Unificar seguridad, roles, permisos y navegacion.
8. Ejecutar compilacion y pruebas por cada fase.
