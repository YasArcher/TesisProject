# Convencion de trazabilidad para fusion de articulos

Durante la fusion, todo archivo, metodo, bloque de metodos, componente, DTO, entidad, servicio o configuracion que provenga del sistema de articulos debe marcarse explicitamente con un comentario.

## Objetivo

Mantener trazabilidad tecnica durante la migracion para poder distinguir claramente:

- Codigo propio del sistema base de proyectos.
- Codigo copiado desde el sistema de articulos.
- Codigo adaptado o fusionado entre ambos sistemas.
- Codigo pendiente de depuracion o eliminacion.

## Comentario recomendado en C#

```csharp
// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
```

Para bloques grandes:

```csharp
// [ARTICLES-MIGRATION-BEGIN] Origen: sistema de articulos. Modulo: Reporteria/Workflow/Registro/etc.
...
// [ARTICLES-MIGRATION-END]
```

## Comentario recomendado en Razor

```razor
@* [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de integrar a la navegacion/base visual de proyectos. *@
```

Para bloques grandes:

```razor
@* [ARTICLES-MIGRATION-BEGIN] Modulo: Registro de articulos *@
...
@* [ARTICLES-MIGRATION-END] *@
```

## Comentario recomendado en CSS

```css
/* [ARTICLES-MIGRATION] Estilos provenientes del sistema de articulos. Revisar antes de fusionar con estilos base. */
```

## Comentario recomendado en SQL

```sql
-- [ARTICLES-MIGRATION] Script proveniente del sistema de articulos. Validar contra modelo de proyectos.
```

## Regla operativa

No se debe activar codigo de articulos dentro del sistema base sin dejar trazabilidad. Una vez que el bloque sea completamente adaptado y aprobado, el comentario puede cambiar a:

```csharp
// [ARTICLES-INTEGRATED] Codigo integrado desde articulos y adaptado a arquitectura de proyectos.
```
