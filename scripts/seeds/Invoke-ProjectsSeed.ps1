param(
    [switch]$Apply,
    [string]$ExpectedDatabase = 'tesis_unified',
    [int]$OwnerAspId = 1,
    [string]$OwnerEmail = 'admin@uta.edu.ec'
)
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# Dedicated deployment variables may override local development configuration.
$connectionString = $env:ConnectionStrings__UnifiedDideConnection
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    $localFile = Join-Path $repository 'tesisproject.backend/appsettings.Local.json'
    if (Test-Path -LiteralPath $localFile) {
        $localConfig = Get-Content -LiteralPath $localFile -Raw | ConvertFrom-Json
        $connectionString = $localConfig.ConnectionStrings.UnifiedDideConnection
    }
}
if ([string]::IsNullOrWhiteSpace($connectionString)) { throw 'Configure UnifiedDideConnection via environment or ignored appsettings.Local.json.' }
$target = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
if ($target.InitialCatalog -ne $ExpectedDatabase -or $ExpectedDatabase -in @('tesis','master','model','msdb','tempdb')) { throw 'Unexpected/non-Unified target.' }
Write-Output "Target: $($target.DataSource)/$($target.InitialCatalog); Apply=$($Apply.IsPresent)"
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 120
    $command.CommandText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'projects-unified.sql') -Raw
    [void]$command.Parameters.Add('@Apply', [System.Data.SqlDbType]::Bit)
    $command.Parameters['@Apply'].Value = $Apply.IsPresent
    [void]$command.Parameters.Add('@ExpectedDatabase', [System.Data.SqlDbType]::NVarChar, 128)
    $command.Parameters['@ExpectedDatabase'].Value = $ExpectedDatabase
    [void]$command.Parameters.Add('@OwnerAspId', [System.Data.SqlDbType]::Int)
    $command.Parameters['@OwnerAspId'].Value = $OwnerAspId
    [void]$command.Parameters.Add('@OwnerEmail', [System.Data.SqlDbType]::NVarChar, 256)
    $command.Parameters['@OwnerEmail'].Value = $OwnerEmail
    $reader = $command.ExecuteReader()
    try {
        do {
            while ($reader.Read()) {
                $row = [ordered]@{}
                for ($index=0; $index -lt $reader.FieldCount; $index++) { $row[$reader.GetName($index)] = $reader.GetValue($index) }
                $row | ConvertTo-Json -Compress
            }
        } while ($reader.NextResult())
    } finally { $reader.Dispose() }
} finally { $connection.Dispose() }
