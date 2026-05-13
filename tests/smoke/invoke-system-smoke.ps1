param(
    [string]$BaseUrl = "http://localhost:5040",
    [string]$Email = "admin@local.test",
    [string]$Password = "Admin#1234",
    [string]$AuthorEmail = "autor.demo@uta.edu.ec",
    [string]$AuthorPassword = "Autor#1234",
    [switch]$CheckAuthorTermsGate,
    [switch]$RunEtl
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-Json {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )

    $parameters = @{
        Method = $Method
        Uri = $Url
        Headers = $Headers
    }

    if ($null -ne $Body) {
        $parameters.Body = ($Body | ConvertTo-Json -Depth 10)
        $parameters.ContentType = "application/json"
    }

    Invoke-RestMethod @parameters
}

function Invoke-JsonStatus {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )

    $parameters = @{
        Method = $Method
        Uri = $Url
        Headers = $Headers
    }

    if ($null -ne $Body) {
        $parameters.Body = ($Body | ConvertTo-Json -Depth 10)
        $parameters.ContentType = "application/json"
    }

    try {
        $response = Invoke-WebRequest @parameters
        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Body = $response.Content
        }
    }
    catch {
        $response = $_.Exception.Response
        $statusCode = 0
        $body = $_.ErrorDetails.Message

        if ($null -ne $response) {
            if ($response.StatusCode) {
                $statusCode = [int]$response.StatusCode
            }

            if ([string]::IsNullOrWhiteSpace($body)) {
                if ($response.Content) {
                    $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                }
                elseif ($response.GetResponseStream) {
                    $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
                    $body = $reader.ReadToEnd()
                }
            }
        }

        return [pscustomobject]@{
            StatusCode = $statusCode
            Body = $body
        }
    }
}

function Write-ReportingHealth {
    param([object]$Health)

    Write-Host ("OK DW: conectado={0} | BD={1} | ETL={2} | articulos={3} | lotes={4} | etapas={5}" -f `
        $Health.canConnect,
        $Health.databaseName,
        $Health.lastEtlStatus,
        $Health.articleRows,
        $Health.batchRows,
        $Health.workflowStageRows)
}

function Write-DashboardSummary {
    param([object]$Dashboard)

    Write-Host ("OK dashboard: articulos={0} | open access={1} | PEDI IIIT={2} | TDD Total={3} | facultades={4}" -f `
        $Dashboard.scientificProduction.totalArticles,
        $Dashboard.scientificProduction.openAccessArticles,
        $Dashboard.pediIiitArticles.Count,
        $Dashboard.tddTotalArticles.Count,
        $Dashboard.articlesByFaculty.Count)
}

function Write-AuthorDashboardSummary {
    param([object]$AuthorDashboard)

    Write-Host ("OK autores: autores={0} | lista={1} | publicaciones={2} | coautorias={3} | vinculos autor-articulo={4}" -f `
        $AuthorDashboard.kpis.totalAuthors,
        $AuthorDashboard.authors.Count,
        $AuthorDashboard.publications.Count,
        $AuthorDashboard.coauthors.Count,
        $AuthorDashboard.kpis.totalAuthorArticleLinks)
}

Write-Step "Validando ping"
$ping = Invoke-RestMethod -Method Get -Uri "$BaseUrl/ping"
if ($ping -ne "pong") {
    throw "Ping inesperado: $ping"
}
Write-Host "OK ping"

Write-Step "Iniciando sesion"
$login = Invoke-Json -Method Post -Url "$BaseUrl/api/Auth/login" -Body @{
    email = $Email
    password = $Password
}

if ([string]::IsNullOrWhiteSpace($login.accessToken)) {
    throw "Login no devolvio accessToken."
}

$headers = @{
    Authorization = "Bearer $($login.accessToken)"
}
Write-Host "OK login para $($login.email)"

Write-Step "Validando usuario actual"
$me = Invoke-Json -Method Get -Url "$BaseUrl/api/Auth/me" -Headers $headers
Write-Host "OK sesion: $($me.email) | Roles: $($me.roles -join ', ')"

Write-Step "Validando salud de reportería"
$health = Invoke-Json -Method Get -Url "$BaseUrl/api/reporting/health" -Headers $headers
Write-ReportingHealth -Health $health

Write-Step "Validando dashboard institucional"
$dashboard = Invoke-Json -Method Get -Url "$BaseUrl/api/reporting/dashboard" -Headers $headers
if ($null -eq $dashboard) {
    throw "Dashboard no devolvio contenido."
}
Write-DashboardSummary -Dashboard $dashboard

Write-Step "Validando analitica de autores y coautoria"
$authorDashboard = Invoke-Json -Method Get -Url "$BaseUrl/api/reporting/authors" -Headers $headers
if ($null -eq $authorDashboard) {
    throw "La analitica de autores no devolvio contenido."
}

if ($authorDashboard.kpis.totalAuthors -lt 100) {
    throw "La analitica de autores devolvio menos autores de lo esperado: $($authorDashboard.kpis.totalAuthors)."
}

if ($authorDashboard.authors.Count -lt 100) {
    throw "La lista de autores parece truncada: $($authorDashboard.authors.Count)."
}

if ($authorDashboard.publications.Count -lt 100) {
    throw "La lista de publicaciones autorales parece incompleta: $($authorDashboard.publications.Count)."
}

Write-AuthorDashboardSummary -AuthorDashboard $authorDashboard

if ($dashboard.filterOptions.articleMonths.Count -gt 0) {
    Write-Step "Validando filtro de reportería por mes"
    $month = $dashboard.filterOptions.articleMonths[0]
    $monthDashboard = Invoke-Json -Method Get -Url "$BaseUrl/api/reporting/dashboard?ArticleMonth=$month" -Headers $headers
    if ($null -eq $monthDashboard) {
        throw "Dashboard filtrado por mes no devolvio contenido."
    }

    if ($monthDashboard.scientificProduction.totalArticles -gt $dashboard.scientificProduction.totalArticles) {
        throw "El filtro por mes devolvio mas articulos que el dashboard general. Mes=$month"
    }

    Write-Host ("OK filtro mes {0}: articulos={1}" -f $month, $monthDashboard.scientificProduction.totalArticles)
}

Write-Step "Validando PDF institucional"
$pdfPath = Join-Path $PSScriptRoot "smoke-report.pdf"
Invoke-WebRequest -Method Get -Uri "$BaseUrl/api/reporting/dashboard/pdf" -Headers $headers -OutFile $pdfPath | Out-Null
$pdfInfo = Get-Item $pdfPath
if ($pdfInfo.Length -lt 1000) {
    throw "El PDF generado parece vacio o invalido: $($pdfInfo.Length) bytes."
}
Write-Host "OK PDF generado: $pdfPath ($($pdfInfo.Length) bytes)"

Write-Step "Validando Excel institucional"
$excelPath = Join-Path $PSScriptRoot "smoke-report.xlsx"
Invoke-WebRequest -Method Get -Uri "$BaseUrl/api/reporting/dashboard/excel" -Headers $headers -OutFile $excelPath | Out-Null
$excelInfo = Get-Item $excelPath
if ($excelInfo.Length -lt 1000) {
    throw "El Excel generado parece vacio o invalido: $($excelInfo.Length) bytes."
}
Write-Host "OK Excel generado: $excelPath ($($excelInfo.Length) bytes)"

if ($CheckAuthorTermsGate) {
    Write-Step "Validando bloqueo de terminos para autores"
    $authorLogin = Invoke-Json -Method Post -Url "$BaseUrl/api/Auth/login" -Body @{
        email = $AuthorEmail
        password = $AuthorPassword
    }

    if ([string]::IsNullOrWhiteSpace($authorLogin.accessToken)) {
        throw "Login de autor no devolvio accessToken."
    }

    $authorHeaders = @{
        Authorization = "Bearer $($authorLogin.accessToken)"
    }

    $authorMe = Invoke-Json -Method Get -Url "$BaseUrl/api/Auth/me" -Headers $authorHeaders
    Write-Host "OK autor: $($authorMe.email) | terminos aceptados=$($authorMe.hasAcceptedTerms)"

    if ($authorMe.hasAcceptedTerms -eq $true) {
        Write-Host "El autor ya acepto terminos; se omite la prueba de bloqueo para no alterar datos." -ForegroundColor Yellow
    }
    else {
        $termsResponse = Invoke-JsonStatus -Method Post -Url "$BaseUrl/api/articles/aggregate/submit-for-review" -Headers $authorHeaders -Body @{}
        if ($termsResponse.StatusCode -ne 400 -or $termsResponse.Body -notmatch "t.rminos") {
            throw "El envio de autor sin terminos no fue bloqueado como se esperaba. Status=$($termsResponse.StatusCode) Body=$($termsResponse.Body)"
        }

        Write-Host "OK bloqueo activo: el autor debe aceptar terminos antes de enviar articulos."
    }
}

if ($RunEtl) {
    Write-Step "Ejecutando ETL completo"
    $etl = Invoke-Json -Method Post -Url "$BaseUrl/api/reporting/etl/full" -Headers $headers
    Write-ReportingHealth -Health $etl

    Write-Step "Validando dashboard despues del ETL"
    $dashboardAfterEtl = Invoke-Json -Method Get -Url "$BaseUrl/api/reporting/dashboard" -Headers $headers
    if ($null -eq $dashboardAfterEtl) {
        throw "Dashboard posterior al ETL no devolvio contenido."
    }
    Write-DashboardSummary -Dashboard $dashboardAfterEtl
}
else {
    Write-Host ""
    Write-Host "ETL omitido. Usa -RunEtl si quieres probar la carga completa." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Smoke completado correctamente." -ForegroundColor Green
