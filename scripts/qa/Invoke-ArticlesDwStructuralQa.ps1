[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Setup', 'Validate', 'Cleanup')]
    [string]$Action,
    [string]$EnvironmentFile = (Join-Path $PSScriptRoot '..\..\.env')
)

$ErrorActionPreference = 'Stop'

Get-Content -LiteralPath $EnvironmentFile | ForEach-Object {
    $line = $_.Trim()
    if ($line -and -not $line.StartsWith('#')) {
        $parts = $line.Split('=', 2)
        if ($parts.Count -eq 2) {
            Set-Item -Path ('Env:' + $parts[0].Trim()) -Value $parts[1].Trim().Trim('"').Trim("'")
        }
    }
}

$server = if ($env:DB_HOST -eq 'host.docker.internal') { 'localhost' } else { $env:DB_HOST }
$port = if ($env:DB_PORT) { $env:DB_PORT } else { '1433' }
$database = if ($env:UNIFIED_DB_NAME) { $env:UNIFIED_DB_NAME } else { 'tesis_unified' }
$operationalDatabase = if ($env:DB_NAME) { $env:DB_NAME } else { 'tesis' }
$connectionString = "Server=$server,$port;Database=$database;User Id=$($env:DB_USER);Password=$($env:DB_PASS);Encrypt=False;TrustServerCertificate=True;"
$scriptName = switch ($Action) {
    'Setup' { 'articles-dw-structural-setup.sql' }
    'Validate' { 'articles-dw-structural-validate.sql' }
    'Cleanup' { 'articles-dw-structural-cleanup.sql' }
}
$sql = Get-Content -LiteralPath (Join-Path $PSScriptRoot $scriptName) -Raw
$sql = $sql.Replace('__OPERATIONAL_DB__', $operationalDatabase.Replace(']', ']]'))

$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 120
    $command.CommandText = $sql
    $reader = $command.ExecuteReader()
    do {
        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                if ($reader.IsDBNull($i)) { 'NULL' } else { [string]$reader.GetValue($i) }
            }
            Write-Output ($values -join '|')
        }
    } while ($reader.NextResult())
    $reader.Close()
}
finally {
    $connection.Dispose()
}
