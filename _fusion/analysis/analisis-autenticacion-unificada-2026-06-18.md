# Analisis de autenticacion unificada

Fecha: 2026-06-18
Objetivo: determinar si ambos sistemas pueden usar un solo login y permitir el acceso a la pagina principal de proyectos o articulos.

## Dictamen

La integracion es viable y recomendable, pero no debe fusionar los dos sistemas ASP.NET Identity. La arquitectura correcta es mantener la autenticacion del sistema de proyectos como autoridad unica y adaptar el modulo de articulos para consumir el mismo usuario, JWT, refresh token, roles y estado de sesion.

No se recomienda conservar ApplicationUser/ApplicationRole del sistema de articulos ni emitir un segundo token.

## Situacion actual

### Sistema de proyectos

- IdentityDbContext con IdentityUser<int> e IdentityRole<int>.
- Usuarios y roles almacenados en la base de proyectos.
- JWT con sub, email, name, role y ClaimTypes.Role.
- Access token en localStorage mediante la clave authToken.
- Refresh token rotativo almacenado como hash en base de datos.
- Refresh token enviado mediante cookie HttpOnly.
- AuthenticationStateProvider renueva la sesion cuando el access token expira.
- AppUser actua como puente entre usuario local, usuario institucional ASP e identificador interno de negocio.
- El login redirige actualmente a / y luego /projects/home.

### Sistema de articulos

- IdentityDbContext con ApplicationUser/ApplicationRole y claves string.
- JWT propio, sin refresh token rotativo.
- Token almacenado bajo otra clave: auth_token.
- Roles funcionales especializados para registro, workflow, reportería, IA y configuracion.
- Endpoint /api/auth/me y aceptacion de terminos.
- Redireccion posterior al login dependiente del rol.

## Incompatibilidades detectadas

1. Clave de usuario: proyectos usa int; articulos usa string.
2. Dos tablas Identity producirian usuarios y contraseñas duplicadas.
3. Los contratos de login son distintos: AuthResponse frente a LoguinResponse.
4. Las claves localStorage son distintas.
5. Los nombres de roles de proyectos estan en minusculas y los de articulos usan PascalCase.
6. Articulos depende de /auth/me y aceptacion de terminos; proyectos no expone actualmente ese perfil.
7. El logout del layout de proyectos solo borra el access token local y no llama al endpoint para revocar la cookie de refresh.
8. La API institucional consultada por proyectos solo recupera perfiles; no valida credenciales.

## Arquitectura recomendada

### Autoridad unica

La base de proyectos conserva:

- AspNetUsers
- AspNetRoles
- AspNetUserRoles
- RefreshTokens
- AppUsers

Las bases TesisDB_Extensible y TesisDW_Extensible no almacenan contraseñas ni una segunda copia de Identity.

### Token unico

El backend de proyectos emite un solo JWT. Ese token es enviado por el mismo AuthMessageHandler a endpoints de proyectos y articulos dentro del backend integrado.

No es necesario cambiar de token al entrar en otro modulo.

### Autorizacion de articulos

Los permisos de articulos deben registrarse en la misma base Identity de proyectos. Se recomienda crear constantes separadas:

- ArticleRegistrationUser
- RegistrationMatrixUser
- DirectArticleSaveUser
- BulkImportUser
- ExternalApiUser
- WorkflowTrackingUser
- WorkflowReviewerUodide
- WorkflowReviewerAreaTecnica
- ReportingViewer
- ReportingExporter
- ReportingAdvancedUser
- IntelligenceViewer
- IntelligenceTrainer
- ConfigurationManager
- CatalogManager
- SecurityAdministrator
- RoleManager

No debe crearse otro rol Admin con diferente capitalizacion. Las politicas de articulos deben aceptar admin y superadmin del sistema base como acceso total, mas los permisos especializados correspondientes.

A medio plazo es preferible convertir estas capacidades en claims de permiso, pero reutilizar roles especializados es la ruta de menor riesgo inicial.

### Referencias de usuario en la base de articulos

Como OLTP de articulos permanece separado, no puede existir una FK SQL normal hacia AspNetUsers de la base de proyectos. Los registros de workflow deben almacenar un identificador estable, recomendado AppUser.IdUser, como valor escalar sin FK entre bases.

La resolucion de nombre, correo y roles se realiza mediante el servicio de identidad central. Para auditoria historica puede almacenarse adicionalmente un nombre/correo snapshot.

## Flujo de usuario propuesto

1. El usuario entra en /login.
2. Proyectos valida credenciales y entrega JWT + cookie de refresh.
3. El frontend guarda un solo access token.
4. El usuario llega a /portal o /modules.
5. La pagina muestra tarjetas segun sus permisos:
   - Gestion de proyectos.
   - Gestion de articulos cientificos.
6. Al elegir proyectos navega a /projects/home.
7. Al elegir articulos navega a /articles/home.
8. Ambos modulos reutilizan MainLayout, AuthenticationStateProvider, ApiClient y token.
9. Cerrar sesion llama /api/auth/logout, revoca refresh token, elimina cookie y limpia localStorage.

Si el usuario solo tiene acceso a un modulo, puede redirigirse automaticamente. Si tiene ambos, se muestra el selector.

## Paginas principales

### Portal integrado

Debe ser una pagina sencilla posterior al login, no un dashboard duplicado. Sus tarjetas se construyen desde permisos reales:

- Proyectos: roles del sistema base habilitados para gestion de proyectos.
- Articulos: al menos un permiso o rol funcional de articulos.

### Inicio de articulos

No se debe copiar el MainLayout ni SideBar originales de articulos. Se crea Features/Articles/Pages/ArticlesHome. El layout y navegacion son los del sistema de proyectos, agregando un grupo de menu condicionado por permisos.

## Autores institucionales

La unificacion inmediata funciona para cuentas existentes en Identity. Sin embargo, la universidad posee muchos autores que no deben gestionarse manualmente desde administracion.

Para ellos existen dos rutas futuras:

1. SSO institucional: el proveedor valida credenciales y el sistema crea o actualiza AppUser bajo demanda.
2. Aprovisionamiento JIT: tras validar identidad institucional, se crea una cuenta local minima y se asigna acceso Author.

La API actual solo consulta perfiles por correo o documento; no es suficiente para autenticar usuarios. Hasta disponer de SSO, los autores necesitan una cuenta local existente o un mecanismo institucional adicional.

## Cambios necesarios

### Backend

1. Conservar AuthController, AuthService y JwtTokenService de proyectos.
2. No migrar AuthController, TokenService, ApplicationUser ni ApplicationRole de articulos.
3. Crear ArticleRoles y ArticlePolicies separados y marcados ARTICLES-MIGRATION.
4. Sembrar roles de articulos en la base Identity de proyectos.
5. Adaptar controladores de articulos a politicas centrales.
6. Crear opcionalmente /api/auth/me compatible con AuthMeResponse.
7. Adaptar aceptacion de terminos a la identidad int si sigue siendo requisito.
8. Corregir logout frontend para revocar el refresh token.

### Frontend

1. Conservar Login, CustomAuthStateProvider, AuthMessageHandler y LocalTokenStore de proyectos.
2. Mejorar visualmente el login usando la presentacion de articulos sin copiar su logica.
3. Cambiar redireccion post-login a /portal.
4. Crear selector de modulos.
5. Agregar menu de articulos al sidebar existente mediante permisos.
6. No migrar JwtAuthStateProvider, IAuthClient ni LocalTokenStore de articulos.

## Riesgos

### Alto

- Duplicar Identity o migrar usuarios string a la base OLTP de articulos.
- Usar simultaneamente admin y Admin como roles distintos.
- Migrar workflow antes de definir AppUser.IdUser como referencia central.
- Suponer que la API de perfiles institucionales autentica credenciales.

### Medio

- La cookie refresh usa Secure y SameSite=None; requiere HTTPS real para funcionar de forma confiable.
- El logout actual no revoca sesion del servidor.
- Proyectos no posee aun nombre completo y terminos equivalentes a ApplicationUser de articulos.
- Mostrar ambos modulos sin comprobar permisos puede revelar rutas no autorizadas.

### Bajo

- Adaptar el aspecto visual del login.
- Crear la pagina portal.
- Reutilizar el token existente para endpoints de articulos en el mismo backend.

## Secuencia segura

### Fase A: contratos de seguridad

1. Crear ArticleRoles y ArticlePolicies sin activar paginas.
2. Añadir seeds idempotentes de roles especializados.
3. Crear pruebas de politicas y claims.

### Fase B: sesion unificada

1. Implementar logout completo.
2. Crear /api/auth/me si se requiere perfil o terminos.
3. Mantener un solo token store.

### Fase C: portal

1. Crear /portal.
2. Redirigir login a /portal.
3. Mostrar tarjetas por permisos.
4. Conservar /projects/home intacto.
5. Crear /articles/home como pagina minima protegida por ArticlesModule.

### Fase D: autores institucionales

Definir con DITIC el mecanismo SSO o validacion institucional antes de habilitar acceso masivo de autores.

## Recomendacion final

Se puede iniciar la integracion del login. La primera entrega debe abarcar usuarios administrativos y cuentas existentes, con un solo Identity y un selector de modulos. No se debe intentar todavia migrar la autenticacion masiva de autores ni el workflow.
