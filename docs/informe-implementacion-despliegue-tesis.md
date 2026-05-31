# Informe de implementacion y despliegue del sistema

## 1. Contexto de implementacion

La implementacion del sistema se realizo con el objetivo de dejar disponible una version funcional del sistema de gestion de articulos cientificos en un entorno operativo similar al que utilizara la institucion. Para ello, se preparo una rama especifica de despliegue denominada `feature/articles-reporting-docker`, en la cual se incorporo la configuracion necesaria para ejecutar el sistema mediante contenedores Docker.

La estrategia adoptada permitio separar la version estable de desarrollo de la version preparada para despliegue. De esta manera, los cambios relacionados con infraestructura, variables de entorno, Docker, Nginx y configuracion de servidor quedaron aislados de la rama funcional principal del sistema.

El despliegue fue realizado en un servidor institucional con Rocky Linux 9.5, donde ya existia otro sistema en ejecucion. Por este motivo, se opto por una instalacion independiente, con su propio directorio, stack Docker, puerto de exposicion y variables de configuracion, evitando interferir con los servicios previamente instalados.

## 2. Arquitectura de despliegue implementada

La implementacion en servidor se estructuro bajo una arquitectura de dos contenedores principales:

- Contenedor `api`: ejecuta el backend desarrollado en ASP.NET Core sobre .NET 9.
- Contenedor `web`: ejecuta el frontend Blazor WebAssembly publicado como archivos estaticos y servido mediante Nginx.

El contenedor frontend expone el sistema a traves del puerto `8088` del servidor, mientras que el backend queda disponible de forma interna dentro de la red Docker. Nginx actua como servidor web y proxy inverso, redirigiendo las solicitudes que llegan a `/api/` hacia el contenedor backend.

Esta organizacion permite que el usuario acceda al sistema desde una unica URL:

```text
http://10.102.12.194:8088
```

La comunicacion interna entre contenedores se realiza mediante la red generada por Docker Compose. El backend se conecta a SQL Server instalado directamente en el host del servidor, utilizando `host.docker.internal` y el mapeo `host-gateway`, lo cual permite que el contenedor acceda al servicio SQL Server del servidor Linux.

## 3. Componentes configurados para Docker

Para el despliegue se incorporaron los siguientes archivos:

- `docker-compose.yml`: define los servicios `api` y `web`, las variables de entorno, volumen persistente y configuracion de red.
- `tesisproject.backend/Dockerfile`: construye y publica el backend ASP.NET Core.
- `tesisproject.frontend/Dockerfile`: publica el frontend Blazor WebAssembly y lo sirve con Nginx.
- `tesisproject.frontend/nginx/default.conf`: configura Nginx como servidor estatico y proxy hacia la API.
- `tesisproject.frontend/nginx/nginx.conf`: configuracion base de Nginx.
- `tesisproject.frontend/wwwroot/appsettings.Production.json`: configuracion del frontend para entorno productivo.
- `.dockerignore`: excluye archivos innecesarios del contexto de construccion.
- `docker-compose.env.example`: plantilla de variables de entorno.

Ademas, se agrego documentacion tecnica para reproducir el despliegue y operar el sistema en servidor.

## 4. Configuracion de variables de entorno

La configuracion sensible del sistema se separo del codigo fuente mediante un archivo `.env`, el cual no debe versionarse en Git. Este archivo contiene las variables necesarias para que los contenedores se conecten al servidor SQL, configuren JWT y expongan el frontend en el puerto definido.

Las variables principales utilizadas fueron:

```env
WEB_HOST_PORT=8088
DB_HOST=host.docker.internal
DB_PORT=1466
DB_USER=sa
DB_OLTP_NAME=TesisDB_Extensible
DB_DW_NAME=TesisDW_Extensible
DB_REPORTING_NAME=TesisDW_Extensible
```

El puerto de SQL Server identificado en el servidor fue `1466`, no el puerto estandar `1433`. Por esta razon, la configuracion final del contenedor backend fue ajustada para conectarse a SQL Server mediante:

```text
host.docker.internal:1466
```

El archivo `.env` tambien incluye la configuracion de JWT y parametros opcionales para integraciones externas, tales como Scopus, Crossref, OpenAlex y Semantic Scholar.

## 5. Restauracion de bases de datos

Antes de levantar el sistema en el servidor, fue necesario restaurar las bases de datos requeridas por la aplicacion. El servidor inicialmente contaba con otras bases, pero no tenia las bases propias del sistema de articulos.

Las bases restauradas fueron:

- `TesisDB_Extensible`: base transaccional OLTP.
- `TesisDW_Extensible`: base analitica para reportería y BI.

Se generaron archivos `.bak` desde la instancia local de SQL Server y posteriormente se subieron al servidor. En el servidor, los backups fueron copiados a:

```text
/var/opt/mssql/backup/
```

Luego se validaron los nombres logicos de los archivos mediante `RESTORE FILELISTONLY` y se restauraron las bases usando `RESTORE DATABASE` con la clausula `MOVE`, redirigiendo los archivos fisicos `.mdf` y `.ldf` hacia:

```text
/var/opt/mssql/data/
```

La restauracion fue verificada con consultas sobre `sys.databases` y `sys.tables`.

Resultados obtenidos:

- `TesisDB_Extensible`: 48 tablas restauradas.
- `TesisDW_Extensible`: 34 tablas restauradas.

Tambien se verifico que el modelo analitico contara con los esquemas necesarios:

- `dw`
- `etl`

Y que existiera el procedimiento principal de actualizacion del Data Warehouse:

```text
etl.sp_RunFullLoad
```

Esta verificacion fue importante porque el modulo de reportería depende del proceso ETL para reflejar correctamente la informacion registrada en el sistema.

## 6. Construccion y ejecucion con Docker Compose

El sistema fue levantado en el servidor mediante Docker Compose con un nombre de proyecto independiente:

```bash
docker compose -p tesis-articulos up -d --build
```

Se utilizo el nombre `tesis-articulos` para evitar conflictos con otros sistemas desplegados en el mismo servidor. En el servidor ya existia otro stack llamado `procesmanager`, el cual utilizaba los puertos `80` y `8080`. Por esta razon, el sistema de articulos fue expuesto en el puerto `8088`.

Los contenedores generados fueron:

- `tesis_articles_api`
- `tesis_articles_web`

El estado de ejecucion fue verificado con:

```bash
docker compose -p tesis-articulos ps
```

Resultado esperado:

- Backend en estado `Up`.
- Frontend en estado `Up`.
- Puerto `8088` publicado hacia el contenedor web.

## 7. Configuracion de Nginx

Nginx fue configurado para cumplir dos funciones:

1. Servir el frontend Blazor WebAssembly como contenido estatico.
2. Redirigir las solicitudes `/api/` hacia el backend ASP.NET Core.

La configuracion incluye:

- soporte para aplicaciones SPA mediante `try_files`;
- proxy hacia `http://api:8080`;
- encabezados `X-Forwarded-*`;
- soporte para upgrade de conexion;
- aumento de `client_max_body_size` a `100m`;
- timeouts de `900s` para soportar procesos largos.

Estos ajustes fueron necesarios debido a que el sistema puede ejecutar operaciones de mayor duracion, como:

- generacion de reportes PDF;
- actualizacion ETL;
- ingesta masiva de datos;
- exportacion de datasets;
- carga de matrices.

## 8. Ajuste de HTTPS interno

Durante las pruebas locales con Docker Desktop se detecto una advertencia relacionada con `UseHttpsRedirection` en el backend. Esta advertencia se producia porque, dentro del entorno Docker, Nginx se comunica con la API mediante HTTP interno.

Para evitar redirecciones innecesarias o advertencias en produccion, se agrego una configuracion controlada por variable de entorno:

```env
HttpsRedirection__Enabled=false
```

El backend fue ajustado para aplicar redireccion HTTPS solo cuando dicha configuracion este habilitada. Esto permite que, en un entorno institucional, HTTPS pueda ser administrado por un proxy externo o por la infraestructura de red, sin afectar la comunicacion interna entre contenedores.

## 9. Verificaciones realizadas

La implementacion fue validada en dos fases:

### 9.1. Prueba local con Docker Desktop

Antes de subir al servidor, se probo la dockerizacion en la maquina local. Las verificaciones realizadas fueron:

- validacion de `docker compose config`;
- construccion de imagenes Docker;
- ejecucion de contenedores;
- conexion del backend a `TesisDB_Extensible`;
- respuesta del frontend en `http://localhost:8088`;
- respuesta protegida de la API con codigo `401 Unauthorized`, comportamiento esperado sin token.

El sistema fue revisado visualmente y se confirmo que la interfaz cargaba correctamente.

### 9.2. Prueba en servidor institucional

En el servidor se verifico:

- existencia de Docker y Docker Compose;
- contenedores existentes para evitar conflictos;
- puertos ocupados;
- puerto real de SQL Server;
- restauracion de bases de datos;
- existencia de esquemas `dw` y `etl`;
- existencia de `etl.sp_RunFullLoad`;
- construccion de imagenes Docker;
- ejecucion de contenedores;
- respuesta del frontend;
- comunicacion del proxy Nginx con el backend.

Las pruebas finales realizadas desde el servidor fueron:

```bash
curl -I http://localhost:8088
```

Resultado:

```text
HTTP/1.1 200 OK
```

Y:

```bash
curl -I http://localhost:8088/api/auth/me
```

Resultado:

```text
HTTP/1.1 401 Unauthorized
```

El codigo `401 Unauthorized` fue considerado correcto porque el endpoint requiere autenticacion mediante token JWT.

## 10. Evidencias tecnicas del despliegue

Durante el despliegue se confirmo que el backend podia conectarse correctamente a la base transaccional restaurada. En los logs se evidencio la conexion a:

```text
TesisDB_Extensible
```

Tambien se verifico la presencia de tablas operativas como:

- `Articles`
- `ArticleParticipants`
- `ImportBatch`
- `WorkflowDefinition`
- `WorkflowInstance`
- `ReportingPerformanceMetrics`
- `IntelligenceTrainingRuns`

Ademas, se confirmo que el backend pudo verificar los modulos de:

- identidad y seguridad;
- configuracion institucional;
- matriz de registro;
- workflow;
- reportería;
- inteligencia institucional.

## 11. Aislamiento respecto a otros sistemas

Un aspecto importante de la implementacion fue no afectar otros sistemas existentes en el servidor. Se identifico que ya existia un sistema desplegado bajo el stack `procesmanager`, con contenedores activos y puertos ocupados:

- puerto `80`;
- puerto `8080`.

Para evitar conflictos, el sistema de articulos fue desplegado en:

```text
/root/Tesis_Christopher/app
```

Y expuesto en:

```text
8088
```

El stack Docker fue nombrado:

```text
tesis-articulos
```

Esto permite administrar el sistema de articulos de forma independiente, sin detener ni modificar los contenedores existentes del servidor.

## 12. Comandos de operacion posterior

Para revisar el estado del sistema:

```bash
cd /root/Tesis_Christopher/app
docker compose -p tesis-articulos ps
```

Para revisar logs del backend:

```bash
docker compose -p tesis-articulos logs --tail=200 api
```

Para revisar logs del frontend:

```bash
docker compose -p tesis-articulos logs --tail=100 web
```

Para revisar logs en tiempo real:

```bash
docker compose -p tesis-articulos logs -f api
```

Para actualizar el sistema desde Git:

```bash
cd /root/Tesis_Christopher/app
git pull
docker compose -p tesis-articulos up -d --build
```

Para detener solamente el sistema de articulos:

```bash
docker compose -p tesis-articulos down
```

## 13. Resultado de la implementacion

Como resultado del proceso de implementacion, el sistema quedo desplegado en el servidor institucional, ejecutandose de forma aislada mediante Docker Compose y disponible para revision de usuarios a traves de la URL:

```text
http://10.102.12.194:8088
```

La implementacion demostro que el sistema puede ejecutarse en un entorno operativo controlado, conectarse a sus bases de datos restauradas, servir su interfaz web mediante Nginx y exponer su backend de forma segura a traves de rutas protegidas.

Este proceso constituye una evidencia practica de implantacion del sistema, validando su portabilidad, reproducibilidad y capacidad de operar en infraestructura institucional sin interferir con otros servicios existentes.
