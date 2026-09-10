# auth/me restaurado sobre Unified

`GET /api/auth/me` vuelve a estar disponible en el controller runtime `UnifiedAuthController`, con `[Authorize]`. `AuthController` legacy continúa `[NonController]`. Los controllers Unified de Articles permanecen inactivos.

Cadena: UnifiedAuthController → IUnifiedCurrentSessionService → IUnifiedArticleUserContext → IUnifiedUnitOfWork.AppUsers (Unified). El servicio de sesión también utiliza IAuthorizationService y ArticlesModuleOptions para calcular los mismos flags de acceso del contrato anterior. El controller solo aporta User/CancellationToken y convierte ServiceResult a HTTP.

Se reutiliza la resolución aprobada: el identificador local de la sesión busca AppUser.IdLocal y devuelve AppUser.IdUser. No se transforma en IdAsp, no se busca por email y no se provisiona identidad. IdAsp sigue siendo la identidad institucional canónica administrada por el boundary Identity existente; auth/me no la reasigna.

## Contrato preservado

- ServiceResult<CurrentSessionResponse>, HTTP 200 y mensaje `Sesion activa.`.
- IdentityUserId, AppUserId nullable, Email, DisplayName, Roles, Permissions y los 14 flags de Articles (incluido Enabled).
- Email/DisplayName/Roles/Permissions provienen de claims como antes; no constituyen claves alternativas para resolver AppUser.
- Sin autenticación: challenge HTTP 401. Claim de usuario inválido: 401 con `AUTH_USER_ID_MISSING` y el mensaje previo.
- Identity sin mapping: HTTP 200 con AppUserId null, igual que legacy; no se inventa un vínculo ni se endurece silenciosamente el contrato.
- Articles deshabilitado: sesión disponible con todos sus flags Articles en false.
- Conflictos de vinculación siguen siendo explícitos en el boundary de provisioning. Tras un conflicto rechazado, auth/me conserva el vínculo original y no repara ni relinka por email.

## Revisión transversal acotada

Auth runtime usa IUnifiedAuthService, IUnifiedRefreshTokenService, Identity Unified y el nuevo servicio de sesión. CurrentUserService/JwtTokenService no consultan contextos ni repositories legacy. No se encontró otra dependencia activa de Auth/Identity evaluada que necesite migración para este endpoint.

Referencias conservadas: AuthController inactivo usa IArticleUserContext; Program todavía registra IArticleUserContext/ArticleUserContext, IAppUserRepository/AppUserRepository, IAuthService/AuthService y IRefreshTokenService/RefreshTokenService, además de AppDbContext y ArticlesDbContext para los componentes legacy aún conservados. Esos registros no son dependencias del nuevo auth/me. Se mantienen por la instrucción de no desactivar ni borrar legacy. IUnifiedArticleUserContext hereda el contrato IArticleUserContext para compatibilidad de firmas, pero su implementación e inyección son Unified. No se revisó ni modificó DW.

## Validación

18 comprobaciones SQL/HTTP: autenticación, claim inválido, mapping Unified existente, Identity sin mapping incluso con email coincidente, conflicto de provisioning sin relink, DTO/envelope, flags de autorización, read-only, descubrimiento de controllers y DI ValidateOnBuild/ValidateScopes de AddUnifiedDide. Migrations exclusivamente en BD GUID temporal de LocalDB; sin cambios a BD de aplicación.

Ejecutar `dotnet run --project tests/tesisproject.authmetests -c Release` con ARTICLES_TEST_SQL_SERVER apuntando a una instancia SQL de pruebas. El test crea y elimina su propia BD. No cambia el runtime de la aplicación ni activa Articles.

Build Release de TesisProject.sln: PASS, 0 errores. La instancia LocalDB exclusiva de esta ejecución se eliminó al finalizar. No quedan blockers de auth/me identificados en este alcance; la activación de Articles sigue siendo una fase posterior.
