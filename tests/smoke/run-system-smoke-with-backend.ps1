param(
    [string]$BaseUrl = "http://localhost:5040",
    [int]$StartupTimeoutSeconds = 180,
    [switch]$RunEtl,
    [switch]$CheckAuthorTermsGate
)

$ErrorActionPreference = "Stop"

$pathValue = [Environment]::GetEnvironmentVariable("Path", "Process")
if ([string]::IsNullOrWhiteSpace($pathValue)) {
    $pathValue = [Environment]::GetEnvironmentVariable("PATH", "Process")
}
[Environment]::SetEnvironmentVariable("Path", $pathValue, "Process")
[Environment]::SetEnvironmentVariable("PATH", $null, "Process")

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$backendOut = Join-Path $repoRoot "artifacts\smoke-backend.out.log"
$backendErr = Join-Path $repoRoot "artifacts\smoke-backend.err.log"
$smokeScript = Join-Path $PSScriptRoot "invoke-system-smoke.ps1"

New-Item -ItemType Directory -Force -Path (Split-Path $backendOut) | Out-Null

Write-Host "Levantando backend para smoke en $BaseUrl..."
$backend = Start-Process `
    -FilePath dotnet `
    -ArgumentList @("run", "--project", "tesisproject.backend\tesisproject.backend.csproj", "--no-build") `
    -WorkingDirectory $repoRoot `
    -WindowStyle Hidden `
    -RedirectStandardOutput $backendOut `
    -RedirectStandardError $backendErr `
    -PassThru

try {
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $ready = $false

    while ((Get-Date) -lt $deadline) {
        if ($backend.HasExited) {
            throw "El backend finalizo antes de responder. ExitCode=$($backend.ExitCode)"
        }

        try {
            $ping = Invoke-RestMethod -Method Get -Uri "$BaseUrl/ping" -TimeoutSec 3
            if ($ping -eq "pong") {
                $ready = $true
                break
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $ready) {
        throw "Backend no estuvo listo en $BaseUrl despues de $StartupTimeoutSeconds segundos."
    }

    Write-Host "Backend listo. Ejecutando smoke..."
    $args = @("-ExecutionPolicy", "Bypass", "-File", $smokeScript, "-BaseUrl", $BaseUrl)
    if ($RunEtl) {
        $args += "-RunEtl"
    }
    if ($CheckAuthorTermsGate) {
        $args += "-CheckAuthorTermsGate"
    }

    & powershell @args
}
finally {
    if ($backend -and -not $backend.HasExited) {
        Stop-Process -Id $backend.Id -Force
    }

    Write-Host ""
    Write-Host "Logs backend:"
    Write-Host "  stdout: $backendOut"
    Write-Host "  stderr: $backendErr"
}
