param([string]$Output)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class G02DesignCapture {
 [DllImport("user32.dll")]public static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [DllImport("user32.dll")]public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr h);
}
'@
[void][G02DesignCapture]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
$clone='C:\Users\Lenovo\AppData\Local\Temp\opencode\G02DesignerSnapshot'
[void][IO.Directory]::CreateDirectory($Output)
$dte=[Runtime.InteropServices.Marshal]::GetActiveObject('VisualStudio.DTE.18.0')
$original=$dte.Solution.FullName
$before=@(Get-ChildItem (Join-Path $clone 'src') -Recurse -File|Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'}|ForEach-Object {[ordered]@{Path=$_.FullName.Substring($clone.Length+1);Hash=(Get-FileHash $_.FullName).Hash}})
$records=[Collections.Generic.List[object]]::new()
try{
 $dte.ExecuteCommand('File.OpenProject','"'+(Join-Path $clone 'src\CafeManager.WinForms\CafeManager.WinForms.csproj')+'"')
 Start-Sleep -Seconds 8
 foreach($item in @('Forms\LoginForm.cs','Forms\BootstrapForm.cs','Forms\MainForm.cs','Forms\CatalogEditorDialog.cs','Modules\Catalogs\CatalogsControl.cs')){
  $result=[ordered]@{File=$item;Expected='Designer open/save/close/reopen without layout loss';Status='BLOCKED';Before=$null;After=$null;Error=$null}
  try{
   $path=Join-Path $clone ('src\CafeManager.WinForms\'+$item)
   [void]$dte.ItemOperations.OpenFile($path)
   $dte.ExecuteCommand('View.ViewDesigner');Start-Sleep -Seconds 5
   $result.Before=$dte.ActiveWindow.Caption
   $p=Get-Process devenv|Where-Object {$_.MainWindowTitle -like '*Microsoft Visual Studio*'}|Select-Object -First 1
   $window=[System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
   $texts=@($window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|ForEach-Object {$_.Current.Name})
   $result.Elements=$texts
   $rect=$window.Current.BoundingRectangle;$b=[Drawing.Bitmap]::new([int]$rect.Width,[int]$rect.Height);$g=[Drawing.Graphics]::FromImage($b)
   try{$dc=$g.GetHdc();try{[void][G02DesignCapture]::PrintWindow($p.MainWindowHandle,$dc,2)}finally{$g.ReleaseHdc($dc)};$b.Save((Join-Path $Output (([IO.Path]::GetFileNameWithoutExtension($item))+'-designer.png')))}finally{$g.Dispose();$b.Dispose()}
   if($texts -match 'could not be shown|Failed to|To prevent possible data loss|The designer cannot'){$result.Status='FAIL';$result.Error='Native Designer displays error page';continue}
   if(-not $dte.ActiveDocument.FullName.StartsWith($clone)){throw 'Refusing save outside cloned snapshot'}
   $dte.ActiveDocument.Save()
   $dte.ActiveWindow.Close(2)
   [void]$dte.ItemOperations.OpenFile($path);$dte.ExecuteCommand('View.ViewDesigner');Start-Sleep -Seconds 3
   $result.After=$dte.ActiveWindow.Caption
   $result.Status='OBSERVED';$result.Error='Inspect serialized diff and native screenshot before PASS'
  }catch{$result.Error=$_.Exception.Message}
  finally{$records.Add($result)}
 }
}finally{
 $after=@(Get-ChildItem (Join-Path $clone 'src') -Recurse -File|Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'}|ForEach-Object {[ordered]@{Path=$_.FullName.Substring($clone.Length+1);Hash=(Get-FileHash $_.FullName).Hash}})
 [IO.File]::WriteAllText((Join-Path $Output 'designer-results.json'),([ordered]@{Clone=$clone;OriginalSolution=$original;Records=$records;Before=$before;After=$after}|ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
 $dte.ExecuteCommand('File.OpenProject','"'+$original+'"')
}
