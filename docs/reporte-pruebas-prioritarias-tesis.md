# Reporte de pruebas prioritarias del sistema

## 1. Objetivo del reporte

Documentar las pruebas mas relevantes aplicadas o preparadas para validar el sistema de gestion de produccion cientifica, priorizando los flujos criticos para la tesis:

- registro individual de articulos
- carga masiva mediante staging
- validaciones de datos
- flujo de revision institucional
- reportería y exportacion
- rendimiento funcional con volumen de datos

Este reporte se enfoca en las pruebas prioritarias y relevantes para demostrar confiabilidad funcional, integracion entre modulos y soporte operativo.

## 2. Estado actual de automatizacion

En la rama revisada no existe un proyecto formal de pruebas unitarias integrado a la solucion, como `xUnit`, `NUnit` o `MSTest`.

Si existen artefactos de prueba operativa y de integracion:

- `docs/plan-pruebas-oltp-extensible.md`
- `tests/smoke/invoke-system-smoke.ps1`
- `tests/smoke/run-system-smoke-with-backend.ps1`
- `tests/load/invoke-registration-load.ps1`
- `tests/load/invoke-bulk-import-scenario.ps1`
- `tests/load/generate-bulk-import-csv.ps1`
- datasets CSV/XLSX para pruebas de carga y validacion
- archivos de evidencia generados como PDF y Excel de smoke

Por tanto, para tesis se puede documentar:

- pruebas unitarias: definidas como necesarias/prioritarias, pero no implementadas formalmente en esta rama
- pruebas de integracion: cubiertas principalmente por scripts contra API y base de datos
- pruebas end-to-end/smoke: cubiertas por flujo automatizado contra backend
- pruebas de carga funcional: cubiertas por scripts y datasets de volumen

## 3. Pruebas prioritarias documentadas

### 3.1 Pruebas de humo del sistema

Archivo principal:

```text
tests/smoke/invoke-system-smoke.ps1
```

Objetivo:

Verificar rapidamente que el sistema esta operativo despues de cambios o antes de una demostracion.

Cobertura prioritaria:

| Caso | Validacion | Resultado esperado |
| --- | --- | --- |
| SMK-01 | Ping del backend | El endpoint responde `pong`. |
| SMK-02 | Login administrador | Se obtiene token de acceso valido. |
| SMK-03 | Usuario actual | Se recuperan datos y roles del usuario autenticado. |
| SMK-04 | Salud de reportería | El DW responde y muestra estado de conexion/ETL. |
| SMK-05 | Dashboard institucional | Se obtiene resumen de articulos, Open Access, PEDI IIIT, TDD Total y facultades. |
| SMK-06 | Analitica de autores | Se valida que autores, publicaciones y coautorias no lleguen truncados. |
| SMK-07 | Filtro por mes | El dashboard filtrado no devuelve mas registros que la vista general. |
| SMK-08 | PDF institucional | Se genera archivo PDF no vacio. |
| SMK-09 | Excel institucional | Se genera archivo Excel no vacio. |
| SMK-10 | ETL opcional | Se ejecuta refresco ETL y se vuelve a consultar dashboard. |

Tipo de prueba:

Integracion / smoke / end-to-end parcial.

Importancia para la tesis:

Demuestra que autenticacion, reportería, DW, exportacion y consultas principales trabajan en conjunto.

## 4. Pruebas de integracion del registro individual

Archivo principal:

```text
tests/load/invoke-registration-load.ps1
```

Objetivo:

Validar que el endpoint de registro agregado puede crear articulos con revista, metricas, participantes y campos base.

Cobertura prioritaria:

| Caso | Validacion | Resultado esperado |
| --- | --- | --- |
| REG-01 | Registro minimo valido | Se crea articulo y participante principal. |
| REG-02 | Registro repetido por lote de prueba | Se crean multiples articulos con DOI unico. |
| REG-03 | Persistencia de venue | La revista se crea o resuelve correctamente. |
| REG-04 | Persistencia de metrica de revista | Se registra SJR/cuartil cuando aplica. |
| REG-05 | Medicion de tiempos | Se obtiene promedio, minimo y maximo por registro. |
| REG-06 | Control de fallos | Se reportan errores devueltos por la API. |

Tipo de prueba:

Integracion API + persistencia.

Escenarios recomendados:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-registration-load.ps1 -Count 10
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-registration-load.ps1 -Count 50
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-registration-load.ps1 -Count 100 -PauseMs 100
```

Importancia para la tesis:

Permite evidenciar tiempos de registro y estabilidad del flujo individual bajo repeticion controlada.

## 5. Pruebas de integracion de carga masiva con staging

Archivo principal:

```text
tests/load/invoke-bulk-import-scenario.ps1
```

Objetivo:

Validar el flujo:

```text
upload -> staging -> validate -> process
```

Cobertura prioritaria:

| Caso | Validacion | Resultado esperado |
| --- | --- | --- |
| IMP-01 | Subida de CSV | Se crea lote en `ImportBatch`. |
| IMP-02 | Preview/resumen del lote | El sistema retorna `batchId`, `batchCode` y resumen inicial. |
| IMP-03 | Validacion de lote | Se clasifican filas validas y filas con error. |
| IMP-04 | Procesamiento de lote valido | Se crean articulos definitivos. |
| IMP-05 | Medicion de upload | Se registra tiempo de carga. |
| IMP-06 | Medicion de validacion | Se registra tiempo de validacion. |
| IMP-07 | Medicion de procesamiento | Se registra tiempo de procesamiento. |

Tipo de prueba:

Integracion API + staging + persistencia.

Comandos recomendados:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-bulk-import-scenario.ps1 -CsvPath .\tests\load\generated-bulk-import.csv -Validate
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-bulk-import-scenario.ps1 -CsvPath .\tests\load\generated-bulk-import.csv -Validate -Process
```

Importancia para la tesis:

Demuestra que el sistema puede recibir informacion en masa, validarla antes de persistirla y procesarla de forma controlada.

## 6. Pruebas con errores controlados

Archivos/datasets relacionados:

```text
tests/load/current-active-delivery/ui-carga-con-errores-controlados-16-registros.csv
tests/load/current-active-delivery/ui-carga-con-errores-controlados-16-registros.xlsx
```

Objetivo:

Comprobar que el sistema detecta inconsistencias sin registrar informacion invalida.

Errores evaluados:

- titulo obligatorio faltante
- fecha invalida
- participante sin nombre
- catalogo o campo no resoluble
- errores de mapeo por fila

Cobertura prioritaria:

| Caso | Validacion | Resultado esperado |
| --- | --- | --- |
| ERR-01 | Campo obligatorio vacio | La fila queda marcada con error. |
| ERR-02 | Fecha invalida | El sistema rechaza el valor y muestra observacion. |
| ERR-03 | Autor incompleto | Se bloquea procesamiento de la fila. |
| ERR-04 | Error por fila/campo | El usuario puede identificar donde corregir. |
| ERR-05 | No persistencia parcial | Las filas invalidas no pasan al modelo definitivo. |

Tipo de prueba:

Integracion funcional / validacion negativa.

Importancia para la tesis:

Demuestra control de calidad de datos antes de alimentar el modelo transaccional y posteriormente la reportería.

## 7. Pruebas de carga funcional

Carpeta principal:

```text
tests/load/current-active-delivery
```

Datasets prioritarios:

| Dataset | Registros | Proposito |
| --- | ---: | --- |
| `ui-carga-minima-valida-20-registros` | 20 | Validar carga minima correcta. |
| `ui-carga-completa-valida-25-registros` | 25 | Validar campos completos, catalogos y datos extendidos. |
| `ui-carga-catalogos-valida-18-registros` | 18 | Validar resolucion de catalogos. |
| `ui-carga-revista-valida-15-registros` | 15 | Validar revistas/venues. |
| `ui-carga-con-errores-controlados-16-registros` | 16 | Validar errores esperados. |
| `ui-carga-completa-extensa-a-1000-registros` | 1000 | Validar volumen alto prudente. |
| `ui-carga-completa-extensa-b-1000-registros` | 1000 | Repetir volumen alto con dataset alterno. |

Objetivo:

Medir estabilidad y comportamiento funcional con lotes pequenos, medianos y altos.

Metricas relevantes:

- tiempo de upload
- tiempo de validacion
- tiempo de procesamiento
- cantidad de filas validas
- cantidad de filas con error
- articulos creados
- participantes creados
- revistas creadas o resueltas
- errores o timeouts

Tipo de prueba:

Carga funcional / rendimiento operativo.

Importancia para la tesis:

Aporta evidencia cuantitativa para el objetivo de reduccion de tiempos de reportería y gestion operativa.

## 8. Pruebas de reportería y exportacion

Archivo principal:

```text
tests/smoke/invoke-system-smoke.ps1
```

Cobertura prioritaria:

| Caso | Validacion | Resultado esperado |
| --- | --- | --- |
| REP-01 | Salud DW | Conexion activa con base analitica. |
| REP-02 | Dashboard institucional | KPI y conteos cargan correctamente. |
| REP-03 | Filtro por mes | El filtro reduce o mantiene el universo, no lo aumenta. |
| REP-04 | Analitica de autores | Autores y publicaciones no se truncan. |
| REP-05 | PDF | Archivo generado con contenido. |
| REP-06 | Excel | Archivo generado con contenido. |
| REP-07 | ETL completo | La reportería se actualiza despues de procesar datos. |

Tipo de prueba:

Integracion BI / reportería / exportacion.

Importancia para la tesis:

Demuestra funcionamiento del componente de inteligencia de negocios y generacion de reportes institucionales.

## 9. Pruebas prioritarias unitarias recomendadas

Aunque no existe un proyecto formal de unit tests en esta rama, para una tesis conviene documentar las pruebas unitarias prioritarias que deben implementarse o que se pueden presentar como propuesta de aseguramiento.

Casos unitarios recomendados:

| ID | Componente | Prueba unitaria prioritaria |
| --- | --- | --- |
| UT-01 | Validador de campos dinamicos | Campo requerido vacio devuelve error. |
| UT-02 | Validador de campos dinamicos | Campo numerico rechaza texto. |
| UT-03 | Validador de campos dinamicos | Longitud maxima se respeta. |
| UT-04 | Validacion DOI | DOI duplicado o invalido se rechaza. |
| UT-05 | Participantes | Debe existir al menos un autor/participante. |
| UT-06 | Participantes | Orden de autor no puede exceder cantidad de participantes. |
| UT-07 | Venue/revista | Revista se resuelve por nombre/ISSN sin duplicar innecesariamente. |
| UT-08 | Carga masiva | Una fila invalida genera error de staging, no excepcion general. |
| UT-09 | Workflow | UODIDE puede devolver al autor, pero no procesar final. |
| UT-10 | Workflow | Area tecnica puede procesar o devolver segun regla institucional. |

Herramienta sugerida:

- xUnit para pruebas unitarias
- Moq o NSubstitute para dependencias
- FluentAssertions para aserciones legibles

## 10. Pruebas prioritarias de integracion recomendadas

Estas pruebas son las mas importantes para mantener el sistema estable.

| ID | Flujo | Prueba |
| --- | --- | --- |
| IT-01 | Registro individual | Enviar articulo valido y comprobar persistencia en `Articles` y `ArticleParticipants`. |
| IT-02 | Registro individual | Enviar articulo con DOI duplicado y comprobar rechazo. |
| IT-03 | Matriz | Subir lote valido, validar y procesar. |
| IT-04 | Matriz | Subir lote con errores, comprobar `ImportBatchError`. |
| IT-05 | Workflow | Autor envia, UODIDE revisa, area tecnica procesa. |
| IT-06 | Workflow | UODIDE devuelve al autor y el autor reenvia. |
| IT-07 | ReporterIA | Procesar datos, ejecutar ETL y verificar conteos actualizados. |
| IT-08 | Exportes | Generar PDF y Excel con datos filtrados. |
| IT-09 | Seguridad | Usuario sin permiso no puede entrar a reportería o procesar lote. |
| IT-10 | IA | Reentrenar con datos suficientes y registrar historial. |

## 11. Evidencia que debe conservarse

Para cada corrida relevante se recomienda guardar:

- fecha y hora
- rama usada
- base de datos usada
- usuario/rol usado
- script ejecutado
- dataset utilizado
- numero de registros
- resultado: pasa, falla o pasa con observaciones
- tiempos de ejecucion
- capturas de pantalla si fue desde UI
- archivo generado si aplica: PDF, Excel, CSV
- error del backend si existio

## 12. Conclusiones para tesis

1. El sistema cuenta con pruebas operativas orientadas a validar flujos reales, especialmente reportería, registro, carga masiva y exportacion.
2. La mayor cobertura existente corresponde a pruebas de integracion y pruebas end-to-end parciales mediante scripts PowerShell.
3. La carga masiva se valida con datasets pequenos, medianos y extensos, incluyendo archivos de 1000 registros.
4. Las pruebas de errores controlados permiten demostrar que el sistema no solo registra datos, sino que tambien protege la calidad de la informacion.
5. La reportería se valida mediante consulta del dashboard, analitica de autores, filtros, exportacion PDF/Excel y ETL opcional.
6. Como mejora tecnica pendiente, se recomienda implementar un proyecto formal de pruebas unitarias para validadores, reglas de negocio y servicios criticos.

## 13. Prioridad final de pruebas para presentar

Para el documento de tesis, las pruebas mas relevantes son:

1. Smoke general del sistema.
2. Registro individual exitoso.
3. Registro individual con error de validacion.
4. Carga masiva valida.
5. Carga masiva con errores controlados.
6. Workflow completo autor -> UODIDE -> area tecnica.
7. ReporterIA actualizada despues de ETL.
8. Exportacion PDF y Excel.
9. Carga funcional con 1000 registros.
10. Control de permisos por rol.
