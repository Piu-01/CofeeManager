$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$projectRoot = (Resolve-Path (Join-Path $taskRoot '../../..')).Path
$docPath = Join-Path $projectRoot 'artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx'
$pdfPath = Join-Path $taskRoot 'qa/BaoCao_CoffeeManager-review.pdf'
$wordApp = $null
$wordDocument = $null
try {
    $wordApp = New-Object -ComObject Word.Application
    $wordApp.Visible = $false
    $wordApp.DisplayAlerts = 0
    $wordDocument = $wordApp.Documents.Open($docPath, $false, $false)
    $wordDocument.Fields.Update() | Out-Null
    foreach ($tocItem in $wordDocument.TablesOfContents) { $tocItem.Update() }
    $wordDocument.Repaginate()
    foreach ($tocItem in $wordDocument.TablesOfContents) { $tocItem.UpdatePageNumbers() }
    $wordDocument.Save()
    $wordDocument.ExportAsFixedFormat($pdfPath, 17)
    [pscustomobject]@{ Pages = $wordDocument.ComputeStatistics(2); Contents = $wordDocument.TablesOfContents.Count; Fields = $wordDocument.Fields.Count; Output = $pdfPath } | ConvertTo-Json
}
finally {
    if ($null -ne $wordDocument) { try { $wordDocument.Close(0) } catch { Write-Warning "Document close: $($_.Exception.Message)" } }
    if ($null -ne $wordApp) { try { $wordApp.Quit() } catch { Write-Warning "Word COM quit: $($_.Exception.Message)" } }
}
