# Dockerizacion del sistema de articulos

Esta rama prepara el sistema de articulos para despliegue con dos contenedores:

- `api`: backend ASP.NET Core sobre .NET 9, escuchando internamente en `8080`.
- `web`: frontend Blazor WebAssembly publicado como archivos estaticos y servido por Nginx.

El frontend llama rutas `api/...`; por eso en produccion su `ApiBaseUrl` queda como `/` y Nginx redirige `/api/` hacia el contenedor `api`.

## Archivos principales

- `docker-compose.yml`
- `docker-compose.env.example`
- `tesisproject.backend/Dockerfile`
- `tesisproject.frontend/Dockerfile`
- `tesisproject.frontend/nginx/nginx.conf`
- `tesisproject.frontend/nginx/default.conf`
- `tesisproject.frontend/wwwroot/appsettings.Production.json`

## Preparacion

Copiar el archivo de variables:

```powershell
copy docker-compose.env.example .env
```

Editar `.env` con los datos reales del servidor:

- `DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASS`
- `DB_OLTP_NAME`
- `DB_DW_NAME`
- `DB_REPORTING_NAME`
- `JWT_KEY`
- credenciales opcionales de APIs externas

Para el sistema actual de articulos, la base analitica correcta es `TesisDW_Extensible`. Por eso `DB_DW_NAME` y `DB_REPORTING_NAME` deben apuntar a `TesisDW_Extensible`, salvo que se defina una separacion nueva de bases en el servidor.

`DB_PASS` y `JWT_KEY` son obligatorios. Docker Compose detendra la ejecucion si no estan definidos.

## Ejecucion

```powershell
docker compose build
docker compose up -d
```

El frontend queda disponible en el puerto configurado por `WEB_HOST_PORT`.

## Despliegue en servidor institucional

Servidor objetivo previsto:

```bash
ssh dinnova@10.102.12.194
```

Como el servidor ya contiene otros sistemas, levantar este proyecto con un nombre de stack independiente:

```bash
docker compose -p tesis-articulos build
docker compose -p tesis-articulos up -d
```

Revisar logs:

```bash
docker compose -p tesis-articulos logs -f api
docker compose -p tesis-articulos logs -f web
```

Detener solo este sistema:

```bash
docker compose -p tesis-articulos down
```

Si `WEB_HOST_PORT=8088`, la URL esperada sera:

```text
http://10.102.12.194:8088
```

## Notas de despliegue

- Si SQL Server esta en el host de Docker Desktop para Windows, `host.docker.internal` suele funcionar.
- En Linux, el servicio `api` ya incluye `extra_hosts: host.docker.internal:host-gateway` para que el contenedor pueda conectarse al SQL Server instalado en el host.
- Antes de desplegar en el servidor, verificar que `WEB_HOST_PORT` no este ocupado por otro sistema.
- La proteccion de datos de ASP.NET se persiste en el volumen `articles-data-protection`.
- Nginx esta configurado con `client_max_body_size 100m` y timeouts de 900 segundos para soportar ingesta masiva, reportes PDF y ETL.
- En Docker, `HttpsRedirection` del backend queda deshabilitado porque Nginx se comunica con la API por HTTP interno. Si un proxy institucional termina HTTPS, debe hacerlo antes de llegar al contenedor web.
- No subir el archivo `.env` al repositorio.
