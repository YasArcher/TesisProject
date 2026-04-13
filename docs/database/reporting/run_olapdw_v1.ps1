param(
    [string]$Server = "PERSONAL\DINNOVA",
    [string]$User = "sa",
    [string]$Database = "TesisDW_Extensible",
    [switch]$Execute
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$bootstrap = Join-Path $root "OLAPDW_v1_bootstrap.sql"
$model = Join-Path $root "OLAPDW_v1_reviewed.sql"
$validation = Join-Path $root "OLAPDW_v1_validation.sql"

if (-not $Execute) {
    Write-Host "Dry run. Agrega -Execute para correr el bootstrap, modelo, ETL y validacion."
    Write-Host "Servidor: $Server"
    Write-Host "Base destino: $Database"
    Write-Host "Scripts:"
    Write-Host " - $bootstrap"
    Write-Host " - $model"
    Write-Host " - EXEC etl.sp_RunFullLoad"
    Write-Host " - $validation"
    exit 0
}

$password = Read-Host "Password SQL para $User" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)

try {
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)

    sqlcmd -S $Server -U $User -P $plainPassword -i $bootstrap
    sqlcmd -S $Server -U $User -P $plainPassword -i $model
    sqlcmd -S $Server -U $User -P $plainPassword -d $Database -Q "EXEC etl.sp_RunFullLoad;"
    sqlcmd -S $Server -U $User -P $plainPassword -i $validation
}
finally {
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}
