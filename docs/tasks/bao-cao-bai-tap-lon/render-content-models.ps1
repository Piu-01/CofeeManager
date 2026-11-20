$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$taskDoc=Join-Path $taskRoot 'artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager_HoanThien.docx'
$taskPdf=Join-Path $PSScriptRoot 'revisions/20261110-content-models/review.pdf'
$taskWord=$null;$taskOpened=$null
try {
 $taskWord=New-Object -ComObject Word.Application
 $taskWord.Visible=$false;$taskWord.DisplayAlerts=0;$taskWord.AutomationSecurity=3
 $taskOpened=$taskWord.Documents.Open($taskDoc,$false,$false)
 $taskOpened.Fields.Update() | Out-Null
 foreach($toc in $taskOpened.TablesOfContents){$toc.Update()}
 $index=0
 foreach($toc in $taskOpened.TablesOfContents){
  $index++
  $toc.Range.Font.Name='Times New Roman';$toc.Range.Font.Size=13;$toc.Range.Font.Color=0
  foreach($para in $toc.Range.Paragraphs){
   $para.Range.ParagraphFormat.KeepWithNext=0
   $para.Range.ParagraphFormat.KeepTogether=1
   $para.Range.ParagraphFormat.FirstLineIndent=0
   $para.Range.ParagraphFormat.RightIndent=0
   $para.Range.ParagraphFormat.LeftIndent=0
   $para.Range.ParagraphFormat.SpaceBefore=0
   $para.Range.ParagraphFormat.SpaceAfter=0
   $para.Range.ParagraphFormat.LineSpacingRule=4
   $para.Range.ParagraphFormat.LineSpacing=15
   if($index -eq 1){
    $txt=$para.Range.Text.Trim()
    if($txt -match '^\d+\.\d+\.\d+'){ $para.Range.ParagraphFormat.LeftIndent=28.35 }
    elseif($txt -match '^\d+\.\d+'){ $para.Range.ParagraphFormat.LeftIndent=14.175 }
   }
   $para.Range.ParagraphFormat.TabStops.ClearAll()
   $para.Range.ParagraphFormat.TabStops.Add(439.37,2,1) | Out-Null
  }
 }
 $taskOpened.Repaginate()
 foreach($toc in $taskOpened.TablesOfContents){$toc.UpdatePageNumbers()}
 $taskOpened.Save()
 $taskOpened.ExportAsFixedFormat($taskPdf,17)
 [pscustomobject]@{Pages=$taskOpened.ComputeStatistics(2);Tables=$taskOpened.Tables.Count;AutomaticLists=$taskOpened.TablesOfContents.Count;Output=$taskPdf} | ConvertTo-Json
}
finally {
 if($null -ne $taskOpened){try{$taskOpened.Close(0)}catch{Write-Warning $_.Exception.Message}}
 if($null -ne $taskWord){try{$taskWord.Quit()}catch{Write-Warning $_.Exception.Message}}
}


