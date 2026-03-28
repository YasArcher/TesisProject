param(
    [string]$OutputDirectory = "C:\Users\Personal\Source\Repos\TesisProject\tests\load\current-active-delivery",
    [string]$RunTag = ([DateTime]::UtcNow.ToString("yyyyMMddHHmmss"))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$headers = @(
    "Article.Title|Titulo",
    "Article.PublishedAt|FechaPublicacion",
    "Article.PageCount|NumeroPaginas",
    "Article.Doi|Doi",
    "Article.PublisherCountry|PaisEditorial",
    "Article.PublicationUrl|UrlPublicacion",
    "Article.IsOpenAccess|AccesoAbierto",
    "Article.JournalName|NombreRevista",
    "Article.PublicationStatusId|EstadoPublicacion",
    "Article.JournalUrl|UrlRevista",
    "Article.Sjr|Sjr",
    "Article.AcademicTermId|PeriodoAcademico",
    "Article.ResearchLineId|LineaInvestigacion",
    "Article.BroadFieldId|CampoAmplio",
    "Article.SpecificFieldId|CampoEspecifico",
    "Article.DetailedFieldId|CampoDetallado",
    "ArticleParticipant.Index|OrdenAutor",
    "ArticleParticipant.Nombre|NombreCompleto",
    "ArticleParticipant.Participacion|Participacion",
    "ArticleParticipant.IsPrimaryAuthor|AutorPrincipal",
    "ArticleParticipant.Identificacion|Identificacion",
    "ArticleParticipant.ParticipantType|TipoParticipante",
    "ArticleParticipant.Email|CorreoElectronico",
    "ArticleParticipant.Affiliation|Afiliacion"
)

$catalog = @{
    AcademicTerms = @("2025-A", "2025-B")
    PublicationStatuses = @("Publicado", "Aceptado", "En revisión")
    ResearchLines = @("Inteligencia Artificial")
    BroadField = "Administración"
    SpecificField = "Educación comercial y administración"
    DetailedFields = @(
        "Administración",
        "Comercio",
        "Competencias laborales",
        "Contabilidad y auditoría",
        "Gestión financiera",
        "Información gerencial",
        "Mercadotecnia y publicidad"
    )
}

function New-HeavyRow {
    param(
        [int]$Index,
        [string]$Scenario
    )

    $publishedAt = "2025-{0:D2}-{1:D2}" -f ((($Index - 1) % 12) + 1), ((($Index - 1) % 27) + 1)
    $pageCount = 8 + ($Index % 20)
    $doi = "10.8888/$Scenario.$RunTag.$Index"
    $journalName = "Revista Extensa $(([math]::Floor(($Index - 1) / 50)) + 1)"
    $journalUrl = "https://revistas.example.org/$Scenario/$Index"
    $publicationUrl = "https://articulos.example.org/$Scenario/$Index"
    $country = @("Ecuador", "Colombia", "Perú", "Chile")[$Index % 4]
    $status = $catalog.PublicationStatuses[$Index % $catalog.PublicationStatuses.Count]
    $term = $catalog.AcademicTerms[$Index % $catalog.AcademicTerms.Count]
    $researchLine = $catalog.ResearchLines[0]
    $detailedField = $catalog.DetailedFields[$Index % $catalog.DetailedFields.Count]
    $participantName = "Autor $Scenario $Index"
    $participantRole = @("Autor", "Coautor", "Investigador")[$Index % 3]
    $participantType = @("Docente", "Investigador externo", "Estudiante")[$Index % 3]
    $primary = if ($Index % 2 -eq 0) { "false" } else { "true" }
    $identification = "ID-$Scenario-{0:D5}" -f $Index
    $email = "autor.$scenario.$index@example.org".ToLowerInvariant()
    $affiliation = @("Universidad Estatal", "Universidad Técnica", "Centro de Investigación")[$Index % 3]
    $isOpenAccess = if ($Index % 2 -eq 0) { "true" } else { "false" }
    $sjr = [string]([math]::Round((0.5 + (($Index % 7) * 0.2)), 2))

    return [ordered]@{
        "Article.Title|Titulo" = "Articulo $Scenario $Index"
        "Article.PublishedAt|FechaPublicacion" = $publishedAt
        "Article.PageCount|NumeroPaginas" = $pageCount
        "Article.Doi|Doi" = $doi
        "Article.PublisherCountry|PaisEditorial" = $country
        "Article.PublicationUrl|UrlPublicacion" = $publicationUrl
        "Article.IsOpenAccess|AccesoAbierto" = $isOpenAccess
        "Article.JournalName|NombreRevista" = $journalName
        "Article.PublicationStatusId|EstadoPublicacion" = $status
        "Article.JournalUrl|UrlRevista" = $journalUrl
        "Article.Sjr|Sjr" = $sjr
        "Article.AcademicTermId|PeriodoAcademico" = $term
        "Article.ResearchLineId|LineaInvestigacion" = $researchLine
        "Article.BroadFieldId|CampoAmplio" = $catalog.BroadField
        "Article.SpecificFieldId|CampoEspecifico" = $catalog.SpecificField
        "Article.DetailedFieldId|CampoDetallado" = $detailedField
        "ArticleParticipant.Index|OrdenAutor" = 1
        "ArticleParticipant.Nombre|NombreCompleto" = $participantName
        "ArticleParticipant.Participacion|Participacion" = $participantRole
        "ArticleParticipant.IsPrimaryAuthor|AutorPrincipal" = $primary
        "ArticleParticipant.Identificacion|Identificacion" = $identification
        "ArticleParticipant.ParticipantType|TipoParticipante" = $participantType
        "ArticleParticipant.Email|CorreoElectronico" = $email
        "ArticleParticipant.Affiliation|Afiliacion" = $affiliation
    }
}

function Write-CsvFile {
    param(
        [string]$Path,
        [System.Collections.IEnumerable]$Rows
    )

    $exportRows = foreach ($row in $Rows) {
        [pscustomobject]$row
    }

    $exportRows | Select-Object $headers | Export-Csv -Path $Path -NoTypeInformation -Encoding UTF8
}

function Write-XlsxFile {
    param(
        [string]$Path,
        [System.Collections.IEnumerable]$Rows
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    function Escape-Xml([string]$Value) {
        return [System.Security.SecurityElement]::Escape([string]$Value)
    }

    function To-ColumnName([int]$Index) {
        $name = ""
        while ($Index -gt 0) {
            $remainder = ($Index - 1) % 26
            $name = [char](65 + $remainder) + $name
            $Index = [math]::Floor(($Index - 1) / 26)
        }
        return $name
    }

    if (Test-Path $Path) {
        Remove-Item $Path -Force
    }

    $fileStream = [System.IO.File]::Open($Path, [System.IO.FileMode]::CreateNew)
    try {
        $archive = New-Object System.IO.Compression.ZipArchive($fileStream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
        $sheetRows = New-Object System.Collections.Generic.List[string]

        $headerCells = for ($c = 0; $c -lt $headers.Count; $c++) {
            $ref = "{0}1" -f (To-ColumnName ($c + 1))
            "<c r=`"$ref`" t=`"inlineStr`"><is><t>$(Escape-Xml $headers[$c])</t></is></c>"
        }
        $sheetRows.Add("<row r=`"1`">$($headerCells -join '')</row>")

        $rowIndex = 2
        foreach ($row in $Rows) {
            $cells = for ($c = 0; $c -lt $headers.Count; $c++) {
                $header = $headers[$c]
                $value = Escape-Xml ([string]$row[$header])
                $ref = "{0}{1}" -f (To-ColumnName ($c + 1)), $rowIndex
                "<c r=`"$ref`" t=`"inlineStr`"><is><t>$value</t></is></c>"
            }
            $sheetRows.Add("<row r=`"$rowIndex`">$($cells -join '')</row>")
            $rowIndex++
        }

        $sheetXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
    $($sheetRows -join "`n    ")
  </sheetData>
</worksheet>
"@

        $contentTypes = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>
"@

        $rootRels = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
"@

        $workbookXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Carga" sheetId="1" r:id="rId1"/>
  </sheets>
</workbook>
"@

        $workbookRels = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
</Relationships>
"@

        $created = [DateTime]::UtcNow.ToString("s") + "Z"
        $coreXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"
                   xmlns:dc="http://purl.org/dc/elements/1.1/"
                   xmlns:dcterms="http://purl.org/dc/terms/"
                   xmlns:dcmitype="http://purl.org/dc/dcmitype/"
                   xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:creator>Codex</dc:creator>
  <cp:lastModifiedBy>Codex</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">$created</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">$created</dcterms:modified>
</cp:coreProperties>
"@

        $appXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"
            xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Codex</Application>
</Properties>
"@

        $entries = [ordered]@{
            "[Content_Types].xml" = $contentTypes
            "_rels/.rels" = $rootRels
            "xl/workbook.xml" = $workbookXml
            "xl/_rels/workbook.xml.rels" = $workbookRels
            "xl/worksheets/sheet1.xml" = $sheetXml
            "docProps/core.xml" = $coreXml
            "docProps/app.xml" = $appXml
        }

        foreach ($entryName in $entries.Keys) {
            $entry = $archive.CreateEntry($entryName)
            $writer = New-Object System.IO.StreamWriter($entry.Open())
            try {
                $writer.Write($entries[$entryName])
            }
            finally {
                $writer.Dispose()
            }
        }

        $archive.Dispose()
    }
    finally {
        $fileStream.Dispose()
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$scenarioA = New-Object System.Collections.Generic.List[object]
$scenarioB = New-Object System.Collections.Generic.List[object]

for ($i = 1; $i -le 1000; $i++) {
    $scenarioA.Add((New-HeavyRow -Index $i -Scenario "ui-carga-completa-extensa-a"))
    $scenarioB.Add((New-HeavyRow -Index $i -Scenario "ui-carga-completa-extensa-b"))
}

$csvPath = Join-Path $OutputDirectory "ui-carga-completa-extensa-a-1000-registros.csv"
$csvPathB = Join-Path $OutputDirectory "ui-carga-completa-extensa-b-1000-registros.csv"
$xlsxPath = Join-Path $OutputDirectory "ui-carga-completa-extensa-b-1000-registros.xlsx"

Write-CsvFile -Path $csvPath -Rows $scenarioA
Write-CsvFile -Path $csvPathB -Rows $scenarioB
Write-XlsxFile -Path $xlsxPath -Rows $scenarioB

Write-Host "Archivo CSV:" $csvPath
Write-Host "Archivo CSV B:" $csvPathB
Write-Host "Archivo Excel:" $xlsxPath
Write-Host "Registros CSV:" 1000
Write-Host "Registros CSV B:" 1000
Write-Host "Registros Excel:" 1000
