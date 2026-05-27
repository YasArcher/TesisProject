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

## Notas de despliegue

- Si SQL Server esta en el host de Docker Desktop para Windows, `host.docker.internal` suele funcionar.
- En Linux puede requerirse agregar `extra_hosts` con `host-gateway`.
- La proteccion de datos de ASP.NET se persiste en el volumen `articles-data-protection`.
- Nginx esta configurado con `client_max_body_size 100m` y timeouts de 900 segundos para soportar ingesta masiva, reportes PDF y ETL.
- No subir el archivo `.env` al repositorio.
