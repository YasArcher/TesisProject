# Despliegue Docker Unified

## Inicio para colaboradores

El stack usa SQL Server **externo**. Docker ejecuta el migrador, la API ASP.NET y el frontend Blazor/Nginx. Antes del primer inicio, el colaborador necesita una instancia SQL Server con autenticacion SQL y acceso TCP desde Docker. Para probar con datos existentes, debe restaurar un respaldo de `tesis_unified` en su propia instancia; la imagen no contiene respaldos ni credenciales.

1. Actualizar la rama y crear `.env` a partir de `.env.example` si aun no existe. Nunca sobrescribir un `.env` personal ni subirlo a Git.
2. Ajustar `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASS` y `JWT_KEY` en `.env`. Desde Docker Desktop, `DB_HOST=host.docker.internal` apunta al equipo anfitrion; `DB_PORT` debe ser el puerto TCP real de SQL Server, no el nombre de una instancia. El login SQL necesita permisos para crear y migrar la base. La autenticacion integrada de Windows no funciona dentro de estos contenedores Linux.
3. Hacer respaldo de la base si ya contiene datos. Revisar `docker compose config --quiet` y levantar con `docker compose up --build -d`.
4. Comprobar `docker compose ps -a`: `migrate-database` debe terminar con codigo 0 y `api`/`web` deben estar en ejecucion. Consultar `docker compose logs --tail=100 migrate-database api web` si algo falla.
5. Abrir `http://localhost:8090` (o el valor de `WEB_HOST_PORT`). `http://localhost:8091/health` debe responder 200 desde el mismo equipo. La API se publica solo en la interfaz local; el navegador consume `/api` mediante Nginx.

En PowerShell, la copia inicial del entorno puede hacerse con `Copy-Item .env.example .env` **solo si `.env` no existe**. En Bash, usar `cp .env.example .env` bajo la misma condicion. `DB_NAME` usa `tesis_unified` y los archivos se guardan en `./.docker-data/files` si no se especifican otros valores. Esa carpeta y `.env` se ignoran en Git y en el contexto de build.

El modo predeterminado del contenedor API es `Production`, sin Swagger ni paginas de excepcion de desarrollo. Solo en un equipo de pruebas, cambiar `ASPNETCORE_ENVIRONMENT=Development` en `.env` y recrear `api` para acceder a Swagger en `http://localhost:8091/`. Volver a `Production` al finalizar. Las migraciones crean el esquema, pero no sustituyen los catalogos institucionales ni los datos de prueba; ejecutar las sincronizaciones y migraciones administrativas requeridas mediante el flujo autorizado de la aplicacion.

El frontend requiere `tesisproject.frontend/wwwroot/appsettings.json` con `ApiBaseUrl=/api`; es configuracion publica, no un secreto. Debe formar parte del commit que recibira el colaborador. Nginx puede iniciar y mostrar la interfaz aunque la API aun no este disponible; en ese caso las solicitudes `/api/` devolveran error hasta que el backend arranque.

No usar `docker compose down -v` para reiniciar: puede eliminar volumenes persistentes de otros servicios si se agregan al proyecto Compose. Para detener sin borrar datos, `docker compose down`.

Se revisó `Develop` y `origin/Develop` en el commit local `0ea84384b17286142d3c6b5247a930d034fb5174`. Los ajustes están sobre la rama de unificación actual, preservando su trabajo pendiente; no se cambia Develop ni se hace merge.

## Flujo

Se mantiene la arquitectura de Develop: frontend Blazor publicado en Nginx, proxy `/api/` al backend ASP.NET en puerto 8080, SQL Server externo y bind mount de archivos. No se modifica frontend ni Nginx.

Compose ahora inicia:

1. `migrate-database`: la misma imagen backend, con `--migrate-database`, crea `tesis_unified` si falta y migra los tres contextos, en orden.
2. `api`: arranca solo si los tres streams terminaron correctamente. Projects, Articles e Identity usan la misma base física. Luego el arranque habitual asegura los roles Identity.
3. `web`: expone el puerto WEB_HOST_PORT y conserva el proxy al servicio `api`.

El comando de migración se ejecuta antes de componer HTTP/Identity/servicios. Exige que las tres conexiones usen el mismo servidor, base y usuario. Rechaza bases de sistema y el POC histórico. Un conflicto SQL produce salida distinta de cero y bloquea el inicio dependiente.

Las migrations permanecen fuera del arranque normal de la API. `DefaultConnection`, `AppDbContext` y las conexiones OLTP históricas ya no forman parte del runtime.

## Configuración y uso

Crear `.env` a partir de `.env.example` sin sobrescribir un `.env` existente. Configurar:

Las conexiones de Docker se suministran exclusivamente por Compose desde `.env`; no agregarlas a `appsettings.Development.json`. Para ejecutar el backend o las herramientas EF fuera de Docker, usar `tesisproject.backend/appsettings.Local.json` (ignorado por Git y excluido de las imágenes) o variables de entorno. El arranque Development y el factory local ya cargan ese archivo.

- DB_HOST/DB_PORT: SQL Server accesible por TCP desde contenedores. No usar autenticación integrada de Windows dentro de Linux.
- DB_USER/DB_PASS: login SQL con permisos para crear y migrar `DB_NAME`.
- DB_NAME: única base física, por defecto `tesis_unified`. Contiene `dbo`, `ProjectsDW` y `ArticlesDW`.
- JWT_KEY: secreto aleatorio de al menos 32 caracteres; JWT_ISSUER/JWT_AUDIENCE deben coincidir entre emisores/validadores del despliegue.
- STORAGE_HOST_PATH: directorio persistente del host. Si se omite, se usa `./.docker-data/files`; en Linux se puede indicar una ruta absoluta con permisos apropiados.
- EXTERNAL_APIS_BASE_URL: directorio/APIs institucionales accesibles desde el backend.
- WEB_HOST_PORT: puerto externo de Nginx, por defecto 8090. API_HOST_PORT (8091) se enlaza solo a `127.0.0.1` del host.

`host.docker.internal` se mapea mediante host-gateway, compatible con hosts Linux y Docker Desktop. Los nombres internos siguen siendo `api` y `web`; se retiraron container_name fijos para permitir proyectos Compose aislados.

```sh
docker compose config --quiet
docker compose up --build -d
docker compose logs migrate-database
docker compose logs api
docker compose ps -a
```

Revisar que `migrate-database` terminó con código 0. Si detecta conflicto, resolverlo explícitamente antes de repetir el despliegue. No ejecutar `down -v` ni limpiar datos para sortearlo. La migración es idempotente; también puede ejecutarse deliberadamente con `docker compose run --rm migrate-database`.

La adaptación inicial de Docker no ejecutó migrations contra la BD de aplicación. El bootstrap opcional descrito abajo sí provisiona la cuenta indicada por el operador. No inventa seeds/forms/fields: el baseline Articles ausente continúa siendo una limitación funcional conocida, no solucionada por Docker. Los ajustes funcionales del frontend siguen en frontend-merge-impacts.md.

## Verificación

- Revision local (2026-11-12): `docker compose config --quiet` con `.env` y con `.env.example`: PASS. El `.env` local sin `DB_NAME` ni `STORAGE_HOST_PATH` ahora resuelve `tesis_unified` y `./.docker-data/files`.
- `docker compose build api web`: PASS con el codigo de trabajo actual. `docker compose run --rm --no-deps --entrypoint nginx web -t`: PASS incluso sin API en la red.
- Smoke temporal del contenedor web: `/`, `/login`, `/appsettings.json` y `/tailwind.css` respondieron 200. Se detuvo el contenedor despues de la prueba. No se ejecuto el migrador ni se modifico SQL Server.

- `docker compose --env-file .env.example config --quiet`: PASS.
- Build backend y solución Release: PASS, 0 errores (21 warnings de la solución). Se regeneraron assets NuGet con SDK 9 porque el frontend tenía assets locales de herramientas .NET 10; no se cambió su código.
- Prueba `tests/tesisproject.articleruntimetests -- deployment-test`: crea una BD SQL temporal desde cero, ejecuta dos veces el CLI real de Production, valida los tres schemas e historiales, verifica tablas representativas y ejecuta ambos ETL completos. La prueba elimina solo su BD temporal al terminar.
- El primer build de imágenes estuvo bloqueado por DNS de Docker Desktop (`lookup mcr.microsoft.com: no such host`). En la verificación posterior del bootstrap se reconstruyó correctamente la imagen API y se comprobó su runtime. No se afirma un smoke funcional completo del frontend.

Logs de verificación local: `artifacts/docker-unified-*.log`. Los Dockerfiles de Develop ya usan .NET 9 y proyectos Shared/backend/frontend correctos; no requieren una reescritura por Unified.

## Superadmin inicial y CORS de Docker

Compose configura explícitamente CORS para `http://localhost:${WEB_HOST_PORT}` y `http://localhost:${API_HOST_PORT}` (8090 y 8091 por defecto), sin modificar los orígenes del desarrollo fuera de Docker. Si se publica en otro host/dominio, sustituir esos orígenes por los reales. CORS no concede roles ni soluciona por sí solo un 403 de autorización.

El bootstrap de superadmin está desactivado por defecto. Para activarlo, completar únicamente en `.env`:

```dotenv
BOOTSTRAP_SUPERADMIN_ENABLED=true
BOOTSTRAP_SUPERADMIN_ASP_USER_ID=<IdAsp institucional real>
BOOTSTRAP_SUPERADMIN_EMAIL=<email de la cuenta>
BOOTSTRAP_SUPERADMIN_USERNAME=<usuario, máximo 10 caracteres>
BOOTSTRAP_SUPERADMIN_PASSWORD=<contraseña inicial según política Identity>
```

No guardar estos valores en archivos versionados. Ejecutar `docker compose up -d --no-deps --build --force-recreate api` cuando la base ya esté migrada. En un despliegue inicial usar el flujo completo con `migrate-database` indicado arriba.

Después de asegurar los roles Identity, el arranque llama a UnifiedSuperadminBootstrap. Reutiliza IUnifiedIdentityProvisioningService con el rol público permitido `user`, obtiene el AppUser por su ID y asigna `superadmin` al IdLocal validado mediante UserManager. La asignación privilegiada solo existe en este bootstrap controlado por configuración; `/register` sigue rechazándola.

Un IdAsp ausente, mapping incompatible o discrepancia de email con una cuenta ya enlazada detiene el bootstrap sin conceder el rol. No se inventa IdAsp ni se reasigna una cuenta existente por email. Las repeticiones reutilizan Identity/AppUser y no cambian la contraseña de usuarios existentes. Si falla la asignación del rol después de un provisioning válido, ese provisioning se conserva conforme a la política Unified.

Tras el éxito, poner `BOOTSTRAP_SUPERADMIN_ENABLED=false`, vaciar `BOOTSTRAP_SUPERADMIN_PASSWORD` y recrear `api` para retirar la credencial del entorno del contenedor. Hacer logout/login para obtener un JWT nuevo con `superadmin`; los JWT anteriores no se actualizan automáticamente. El secreto de bootstrap no se escribe en logs.

Pruebas aisladas: `dotnet run --project tests/tesisproject.articleruntimetests -c Release -- superadmin-test`. Cubren desactivación, configuración inválida, cuenta nueva, reutilización, conflicto sin relink, promoción de usuario existente sin cambio de contraseña, rechazo público de superadmin, JWT y CORS.

Verificación del bootstrap (2026-09-09): 17 checks aislados PASS; build Docker API PASS. Cuenta indicada por el operador provisionada en el runtime local: login 200, JWT nuevo con superadmin, auth/me 200 y preflight CORS 204 con el origen correspondiente para 8090/8091. No se ejecutó el POST de sincronización. Tras verificar, se desactivó el bootstrap y se retiró su contraseña del entorno; la cuenta/rol persisten.
