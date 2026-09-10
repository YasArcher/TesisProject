# Despliegue Docker Unified

Se revisó `Develop` y `origin/Develop` en el commit local `0ea84384b17286142d3c6b5247a930d034fb5174`. Los ajustes están sobre la rama de unificación actual, preservando su trabajo pendiente; no se cambia Develop ni se hace merge.

## Flujo

Se mantiene la arquitectura de Develop: frontend Blazor publicado en Nginx, proxy `/api/` al backend ASP.NET en puerto 8080, SQL Server externo y bind mount de archivos. No se modifica frontend ni Nginx.

Compose ahora inicia:

1. `migrate-unified`: la misma imagen backend, con `--migrate-unified`, aplica únicamente las migrations de UnifiedDideDbContext y termina.
2. `api`: arranca solo si el job terminó correctamente. Projects, Articles e Identity usan UnifiedDideConnection; Articles queda habilitado. Luego el arranque habitual asegura los roles Identity.
3. `web`: expone el puerto WEB_HOST_PORT y conserva el proxy al servicio `api`.

El comando de migración se ejecuta antes de componer HTTP/Identity/servicios. Acepta SQL Authentication de despliegue y no modifica el factory local, que conserva su restricción Development/Windows. Rechaza bases de sistema, el POC histórico y nombres de BD coincidentes con las conexiones legacy/DW configuradas. Un conflicto SQL produce salida distinta de cero y bloquea el inicio dependiente; no se corrigen datos automáticamente.

`DatabaseBootstrap__ApplyMigrations=false` evita ejecutar el bootstrap anterior de AppDbContext/DW. DefaultConnection se conserva exclusivamente para la dependencia DW/ETL existente. Esa base debe estar aprovisionada como antes: este Compose no la crea ni migra. Las conexiones archivadas ArticlesOltp/ArticlesOlap ya no forman parte del despliegue OLTP.

## Configuración y uso

Crear `.env` a partir de `.env.example` sin sobrescribir un `.env` existente. Configurar:

Las conexiones de Docker se suministran exclusivamente por Compose desde `.env`; no agregarlas a `appsettings.Development.json`. Para ejecutar el backend o las herramientas EF fuera de Docker, usar `tesisproject.backend/appsettings.Local.json` (ignorado por Git y excluido de las imágenes) o variables de entorno. El arranque Development y el factory local ya cargan ese archivo.

- DB_HOST/DB_PORT: SQL Server accesible por TCP desde contenedores. No usar autenticación integrada de Windows dentro de Linux.
- DB_USER/DB_PASS: login SQL con permisos para aplicar migrations en UNIFIED_DB_NAME y con el acceso legacy requerido por las funciones DW conservadas.
- UNIFIED_DB_NAME: base canónica separada, por defecto `tesis_unified`.
- DB_NAME: base legacy/DW existente; debe ser diferente de UNIFIED_DB_NAME.
- JWT_KEY: secreto aleatorio de al menos 32 caracteres; JWT_ISSUER/JWT_AUDIENCE deben coincidir entre emisores/validadores del despliegue.
- STORAGE_HOST_PATH: directorio persistente del host; en Linux usar una ruta absoluta Linux con permisos apropiados.
- EXTERNAL_APIS_BASE_URL: directorio/APIs institucionales accesibles desde el backend.
- WEB_HOST_PORT: puerto externo de Nginx, por defecto 8090.

`host.docker.internal` se mapea mediante host-gateway, compatible con hosts Linux y Docker Desktop. Los nombres internos siguen siendo `api` y `web`; se retiraron container_name fijos para permitir proyectos Compose aislados.

```sh
docker compose config --quiet
docker compose up --build -d
docker compose logs migrate-unified
docker compose logs api
docker compose ps -a
```

Revisar que migrate-unified terminó con código 0. Si detecta conflicto, resolverlo explícitamente antes de repetir el despliegue. No ejecutar `down -v` ni limpiar datos para sortearlo. La migración es idempotente; también puede ejecutarse deliberadamente con `docker compose run --rm migrate-unified`.

La adaptación inicial de Docker no ejecutó migrations contra la BD de aplicación. El bootstrap opcional descrito abajo sí provisiona la cuenta indicada por el operador. No inventa seeds/forms/fields: el baseline Articles ausente continúa siendo una limitación funcional conocida, no solucionada por Docker. Los ajustes funcionales del frontend siguen en frontend-merge-impacts.md.

## Verificación

- `docker compose --env-file .env.example config --quiet`: PASS.
- Build backend y solución Release: PASS, 0 errores (21 warnings de la solución). Se regeneraron assets NuGet con SDK 9 porque el frontend tenía assets locales de herramientas .NET 10; no se cambió su código.
- Prueba `tests/tesisproject.articleruntimetests -- deployment-test`: PASS, 12 comprobaciones. Valida destinos, ejecuta el CLI real en Production sobre una BD SQL temporal desde cero y repite la migración; comprueba las cuatro migrations y ArticleReadView. El destino legacy de prueba es inaccesible deliberadamente.
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

No guardar estos valores en archivos versionados. Ejecutar `docker compose up -d --no-deps --build --force-recreate api` cuando la BD Unified ya esté migrada. En un despliegue inicial usar el flujo completo con migrate-unified indicado arriba.

Después de asegurar los roles Identity, el arranque llama a UnifiedSuperadminBootstrap. Reutiliza IUnifiedIdentityProvisioningService con el rol público permitido `user`, obtiene el AppUser por su ID y asigna `superadmin` al IdLocal validado mediante UserManager. La asignación privilegiada solo existe en este bootstrap controlado por configuración; `/register` sigue rechazándola.

Un IdAsp ausente, mapping incompatible o discrepancia de email con una cuenta ya enlazada detiene el bootstrap sin conceder el rol. No se inventa IdAsp ni se reasigna una cuenta existente por email. Las repeticiones reutilizan Identity/AppUser y no cambian la contraseña de usuarios existentes. Si falla la asignación del rol después de un provisioning válido, ese provisioning se conserva conforme a la política Unified.

Tras el éxito, poner `BOOTSTRAP_SUPERADMIN_ENABLED=false`, vaciar `BOOTSTRAP_SUPERADMIN_PASSWORD` y recrear `api` para retirar la credencial del entorno del contenedor. Hacer logout/login para obtener un JWT nuevo con `superadmin`; los JWT anteriores no se actualizan automáticamente. El secreto de bootstrap no se escribe en logs.

Pruebas aisladas: `dotnet run --project tests/tesisproject.articleruntimetests -c Release -- superadmin-test`. Cubren desactivación, configuración inválida, cuenta nueva, reutilización, conflicto sin relink, promoción de usuario existente sin cambio de contraseña, rechazo público de superadmin, JWT y CORS.

Verificación del bootstrap (2026-09-09): 17 checks aislados PASS; build Docker API PASS. Cuenta indicada por el operador provisionada en el runtime local: login 200, JWT nuevo con superadmin, auth/me 200 y preflight CORS 204 con el origen correspondiente para 8090/8091. No se ejecutó el POST de sincronización. Tras verificar, se desactivó el bootstrap y se retiró su contraseña del entorno; la cuenta/rol persisten.
