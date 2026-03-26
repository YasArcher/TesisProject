param(
    [string]$BaseUrl = "http://localhost:5041",
    [int]$Count = 25,
    [int]$PauseMs = 0,
    [string]$ExternalSource = "ManualLoadTest",
    [string]$RunTag = ([DateTime]::UtcNow.ToString("yyyyMMddHHmmss"))
)

function New-RegistrationPayload {
    param(
        [int]$Index,
        [string]$ExternalSource,
        [string]$RunTag
    )

    return @{
        formKey = $null
        article = @{
            title = "Articulo agregado $Index"
            doi = "10.5555/aggregate.$RunTag.$Index"
            year = 2024
            publicationUrl = "https://example.org/aggregate/$Index"
            isProjectResult = $false
            hasInterculturalComponent = $false
            isOpenAccess = $true
            externalSource = $ExternalSource
            externalId = "AGG-$RunTag-$Index"
        }
        venue = @{
            journalName = "Revista Registro Manual $(([math]::Floor(($Index - 1) / 10)) + 1)"
            issnCode = "4321-78{0:D2}" -f ($Index % 90)
            issueNumber = (($Index % 4) + 1).ToString()
            volumeNumber = (($Index % 10) + 1).ToString()
            journalUrl = "https://example.org/journal/$(([math]::Floor(($Index - 1) / 10)) + 1)"
            type = "Journal"
        }
        venueMetric = @{
            year = 2024
            sjr = 1.25
            quartile = "Q2"
        }
        dynamicFields = @()
        participants = @(
            @{
                index = 1
                nombre = "Autor de Prueba $Index"
                participantType = "Autor"
                isPrimaryAuthor = $true
                email = "autor$Index@example.org"
                affiliation = "DIDE"
                dynamicFields = @()
            }
        )
    }
}

$results = New-Object System.Collections.Generic.List[object]
$total = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 1; $i -le $Count; $i++) {
    $payload = New-RegistrationPayload -Index $i -ExternalSource $ExternalSource -RunTag $RunTag
    $json = $payload | ConvertTo-Json -Depth 20
    $watch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/articles/aggregate" -ContentType "application/json" -Body $json
        $watch.Stop()
        $results.Add([pscustomobject]@{
            index = $i
            ok = $true
            elapsedMs = $watch.ElapsedMilliseconds
            articleId = $response.articleId
            error = $null
        })
    }
    catch {
        $watch.Stop()
        $results.Add([pscustomobject]@{
            index = $i
            ok = $false
            elapsedMs = $watch.ElapsedMilliseconds
            articleId = $null
            error = $_.Exception.Message
        })
    }

    if ($PauseMs -gt 0) {
        Start-Sleep -Milliseconds $PauseMs
    }
}

$total.Stop()

[pscustomobject]@{
    startedAt = [DateTime]::UtcNow.AddMilliseconds(-$total.ElapsedMilliseconds).ToString("o")
    finishedAt = [DateTime]::UtcNow.ToString("o")
    baseUrl = $BaseUrl
    count = $Count
    successes = @($results | Where-Object { $_.ok }).Count
    failures = @($results | Where-Object { -not $_.ok }).Count
    avgMs = [Math]::Round((($results | Measure-Object -Property elapsedMs -Average).Average), 2)
    maxMs = ($results | Measure-Object -Property elapsedMs -Maximum).Maximum
    minMs = ($results | Measure-Object -Property elapsedMs -Minimum).Minimum
    totalElapsedMs = $total.ElapsedMilliseconds
    runTag = $RunTag
    items = $results
} | ConvertTo-Json -Depth 20
