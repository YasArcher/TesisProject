# Ruta de estabilizacion funcional

Fecha: 2026-04-18

## Objetivo

Dejar una ruta repetible de verificacion antes de seguir puliendo UX, reportería y refactors. La prioridad es confirmar que el flujo principal sigue funcionando despues de cada cambio.

## Estado base

- Backend compila correctamente con `dotnet build tesisproject.backend\tesisproject.backend.csproj --no-restore`.
- Frontend compila correctamente despues de restaurar `browser-wasm` con `dotnet restore tesisproject.frontend\tesisproject.frontend.csproj -r browser-wasm`.
- El flujo Autor -> UODIDE -> Area Tecnica -> Procesamiento esta funcionando segun la ultima prueba manual.
- La reportería institucional ya consume el DW extensible y contiene bloques de indicadores, participacion, PEDI IIIT, TDD Total, PDF y ejecucion ETL.

## Smoke tecnico minimo

Ejecutar con backend levantado:

```powershell
.\tests\smoke\invoke-system-smoke.ps1
```

O ejecutar el smoke levantando el backend automaticamente y esperando a que `/ping` responda:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\smoke\run-system-smoke-with-backend.ps1
```

Validaciones incluidas:

- `/ping` responde.
- Login devuelve token valido.
- `/api/Auth/me` devuelve usuario y roles.
- `/api/reporting/health` responde para usuario autorizado.
- `/api/reporting/dashboard` responde para usuario autorizado.
- `/api/reporting/dashboard/pdf` genera contenido PDF.
- ETL completo puede ejecutarse de forma opcional con `-RunEtl`.

## Matriz de QA por rol

### Administrador

- Iniciar sesion con usuario admin.
- Ver menu completo segun permisos.
- Gestionar usuarios y roles sin ver informacion tecnica innecesaria.
- Acceder a configuracion y validar campos visibles del formulario.
- Acceder a registro de articulo y confirmar que no duplica caminos de carga.
- Acceder a carga masiva, APIs externas, listado de articulos y reportería.
- Ejecutar ETL desde reportería y verificar pantalla bloqueante de carga.
- Generar PDF y revisar que respete filtros actuales.

### Autor

- Iniciar sesion con usuario autor.
- Ver unicamente vistas permitidas para registro, revision de envios y soporte/cuenta.
- Registrar articulo individual y enviar a revision.
- Registrar articulos en masa desde matriz del autor.
- Confirmar modal de envio y pantalla de enfriamiento.
- Revisar estado del envio: pendiente, en revision, aprobado, devuelto o procesado.
- Revisar notas u observaciones de revisores.
- Abrir previsualizacion de articulos propios sin errores `Forbidden`.

### UODIDE

- Iniciar sesion con usuario UODIDE.
- Ver solo revision de envios y soporte/cuenta.
- Filtrar casos pendientes y en revision.
- Tomar caso.
- Revisar staging del lote desde panel dedicado dentro de la misma pagina.
- Corregir filas o revisar errores sin salir al modulo global de carga masiva.
- Enviar notas al autor o al siguiente validador.
- Validar lote y confirmar que avanza a Area Tecnica.

### Area Tecnica

- Iniciar sesion con usuario de Area Tecnica.
- Ver bandeja con casos provenientes de UODIDE.
- Tomar caso.
- Revisar staging propio del flujo de revision.
- Validar lote.
- Procesar informacion.
- Confirmar que el lote queda procesado y disponible para ETL/reportería.

## Puntos que no deben romperse

- El staging global de carga masiva debe seguir independiente del staging de revision.
- El autor no debe ver informacion de otros autores.
- UODIDE no debe procesar lotes; solo revisar, corregir y validar.
- Area Tecnica debe poder validar y procesar cuando el workflow ya avanzo correctamente.
- Reportería debe mostrar informacion del DW extensible, no del modelo legacy.
- Los filtros de reportería deben afectar tarjetas, tablas, graficas y PDF.

## Riesgos actuales

- Hay paginas y servicios grandes que conviene dividir cuando el flujo este estable:
  - `ArticlesBiImportExport.razor`
  - `BulkImportService.cs`
  - `StatisticalReports.razor`
  - `WorkflowReview.razor`
  - `Configuration.razor`
  - `RegisterArticle.razor`
- Parte del esquema se crea con SQL embebido en arranque y parte con scripts versionados. Para despliegue conviene una unica estrategia controlada.
- La reportería puede crecer mucho si se cargan detalles completos en cada filtro. El siguiente endurecimiento tecnico deberia incluir endpoints agregados, cache y paginacion para detalle.

## Siguiente ruta recomendada

1. Ejecutar smoke tecnico con backend levantado.
2. Hacer QA manual por rol siguiendo esta matriz.
3. Corregir bloqueos funcionales antes de seguir con UX.
4. Pulir reportería sobre datos reales: rendimiento, filtros, PDF y disposicion visual.
5. Refactorizar archivos grandes por componentes/servicios pequenos sin cambiar comportamiento.
