$ErrorActionPreference = 'Stop'
$revisionProject = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$revisionDoc = Join-Path $revisionProject 'artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx'
$revisionPdf = Join-Path $PSScriptRoot 'revisions/20261110-native-cover/BaoCao_CoffeeManager-review.pdf'
$revisionWord = $null
$revisionOpened = $null
try {
    $revisionWord = New-Object -ComObject Word.Application
    $revisionWord.Visible = $false
    $revisionWord.DisplayAlerts = 0
    $revisionWord.AutomationSecurity = 3
    $revisionOpened = $revisionWord.Documents.Open($revisionDoc, $false, $false)
    $revisionOpened.Fields.Update() | Out-Null
    foreach ($revisionToc in $revisionOpened.TablesOfContents) { $revisionToc.Update() }
    $revisionOpened.Repaginate()
    foreach ($revisionToc in $revisionOpened.TablesOfContents) { $revisionToc.UpdatePageNumbers(); $revisionToc.Range.Font.Color = 0 }
    $revisionOpened.Save()
    $revisionOpened.ExportAsFixedFormat($revisionPdf, 17)
    [pscustomobject]@{ Pages = $revisionOpened.ComputeStatistics(2); Contents = $revisionOpened.TablesOfContents.Count; Tables = $revisionOpened.Tables.Count; Output = $revisionPdf } | ConvertTo-Json
}
finally {
    if ($null -ne $revisionOpened) { try { $revisionOpened.Close(0) } catch { Write-Warning $_.Exception.Message } }
    if ($null -ne $revisionWord) { try { $revisionWord.Quit() } catch { Write-Warning $_.Exception.Message } }
}

