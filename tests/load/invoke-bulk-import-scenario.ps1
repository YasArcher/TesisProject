param(
    [string]$BaseUrl = "http://localhost:5041",
    [string]$CsvPath = "C:\Users\Personal\Source\Repos\TesisProject\tests\load\generated-bulk-import.csv",
    [string]$SourceType = "Csv",
    [string]$Notes = "Escenario de prueba de carga masiva",
    [switch]$Validate = $true,
    [switch]$Process = $false
)

if (-not (Test-Path $CsvPath)) {
    throw "No encontré el archivo CSV en '$CsvPath'."
}

Add-Type -AssemblyName System.Net.Http

function ConvertFrom-JsonCompat {
    param(
        [string]$Json
    )

    $command = Get-Command ConvertFrom-Json
    if ($command.Parameters.ContainsKey('Depth')) {
        return $Json | ConvertFrom-Json -Depth 100
    }

    return $Json | ConvertFrom-Json
}

function Invoke-MultipartUpload {
    param(
        [string]$Url,
        [string]$FilePath,
        [string]$SourceType,
        [string]$Notes
    )

    $client = [System.Net.Http.HttpClient]::new()
    try {
        $content = [System.Net.Http.MultipartFormDataContent]::new()
        $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
        $fileContent = [System.Net.Http.ByteArrayContent]::new($fileBytes)
        $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("text/csv")
        $content.Add($fileContent, "file", [System.IO.Path]::GetFileName($FilePath))
        $content.Add([System.Net.Http.StringContent]::new($SourceType), "sourceType")
        $content.Add([System.Net.Http.StringContent]::new($Notes), "notes")

        $response = $client.PostAsync($Url, $content).GetAwaiter().GetResult()
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        if (-not $response.IsSuccessStatusCode) {
            throw "HTTP $([int]$response.StatusCode): $body"
        }

        return ConvertFrom-JsonCompat -Json $body
    }
    finally {
        $client.Dispose()
    }
}

function Invoke-JsonPost {
    param(
        [string]$Url,
        [object]$Body
    )

    $json = $Body | ConvertTo-Json -Depth 100
    return Invoke-RestMethod -Method Post -Uri $Url -ContentType "application/json" -Body $json
}

$summary = [ordered]@{
    startedAt = [DateTime]::UtcNow.ToString("o")
    baseUrl = $BaseUrl
    csvPath = $CsvPath
    sourceType = $SourceType
    uploadMs = $null
    validateMs = $null
    processMs = $null
    batchId = $null
    batchCode = $null
    afterUpload = $null
    afterValidate = $null
    afterProcess = $null
}

$uploadStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$uploadResult = Invoke-MultipartUpload -Url "$BaseUrl/api/import-batches/upload" -FilePath $CsvPath -SourceType $SourceType -Notes $Notes
$uploadStopwatch.Stop()

$summary.uploadMs = $uploadStopwatch.ElapsedMilliseconds
$summary.batchId = $uploadResult.summary.importBatchId
$summary.batchCode = $uploadResult.summary.batchCode
$summary.afterUpload = $uploadResult.summary

if ($Validate) {
    $validateStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $validateResult = Invoke-JsonPost -Url "$BaseUrl/api/import-batches/$($summary.batchId)/validate" -Body @{}
    $validateStopwatch.Stop()
    $summary.validateMs = $validateStopwatch.ElapsedMilliseconds
    $summary.afterValidate = $validateResult.batch.summary
}

if ($Process) {
    $processStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $processResult = Invoke-JsonPost -Url "$BaseUrl/api/import-batches/$($summary.batchId)/process" -Body @{}
    $processStopwatch.Stop()
    $summary.processMs = $processStopwatch.ElapsedMilliseconds
    $summary.afterProcess = $processResult.batch.summary
}

$summary.finishedAt = [DateTime]::UtcNow.ToString("o")
$summary | ConvertTo-Json -Depth 100
