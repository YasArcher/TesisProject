param(
    [string]$OutputPath = "C:\Users\Personal\Source\Repos\TesisProject\tests\load\generated-bulk-import.csv",
    [int]$RowCount = 100,
    [int]$ErrorEvery = 0,
    [switch]$IncludeParticipants = $true
)

$headers = @(
    "Article.Title|Title",
    "Article.Doi|Doi",
    "Article.Year|Year",
    "Article.JournalName|JournalName",
    "Article.IssnCode|IssnCode",
    "Article.VolumeNumber|VolumeNumber",
    "Article.IssueNumber|IssueNumber",
    "Article.PublicationUrl|PublicationUrl",
    "Article.ExternalSource|ExternalSource",
    "Article.ExternalId|ExternalId"
)

if ($IncludeParticipants) {
    $headers += "ArticleParticipant.Nombre|Nombre"
    $headers += "ArticleParticipant.Email|Email"
    $headers += "ArticleParticipant.Orcid|Orcid"
}

$rows = New-Object System.Collections.Generic.List[string]
$rows.Add(($headers -join ","))

for ($i = 1; $i -le $RowCount; $i++) {
    $hasError = $ErrorEvery -gt 0 -and ($i % $ErrorEvery -eq 0)

    $title = if ($hasError) { "" } else { "Articulo de carga $i" }
    $doi = "10.5555/load.$([DateTime]::UtcNow.ToString('yyyyMMdd')).$i"
    $year = if ($hasError) { "AÑO_INVALIDO" } else { "2024" }
    $journal = "Revista de Pruebas $(([math]::Floor(($i - 1) / 25)) + 1)"
    $issn = "1234-56{0:D2}" -f ($i % 90)
    $volume = (($i % 12) + 1).ToString()
    $issue = (($i % 4) + 1).ToString()
    $publicationUrl = "https://example.org/articles/$i"
    $externalSource = "CargaPrueba"
    $externalId = "LOAD-$i"

    $values = @(
        $title,
        $doi,
        $year,
        $journal,
        $issn,
        $volume,
        $issue,
        $publicationUrl,
        $externalSource,
        $externalId
    )

    if ($IncludeParticipants) {
        $participantName = if ($hasError) { "" } else { "Autor Prueba $i" }
        $participantEmail = "autor$i@example.org"
        $participantOrcid = "0000-0001-0000-{0:D4}" -f $i
        $values += $participantName
        $values += $participantEmail
        $values += $participantOrcid
    }

    $escaped = $values | ForEach-Object {
        $text = [string]$_
        '"' + ($text -replace '"', '""') + '"'
    }

    $rows.Add(($escaped -join ","))
}

Set-Content -Path $OutputPath -Value $rows -Encoding UTF8
Write-Host "CSV generado:" $OutputPath
Write-Host "Filas:" $RowCount
Write-Host "Filas con error intencional:" $(if ($ErrorEvery -gt 0) { [math]::Floor($RowCount / $ErrorEvery) } else { 0 })
