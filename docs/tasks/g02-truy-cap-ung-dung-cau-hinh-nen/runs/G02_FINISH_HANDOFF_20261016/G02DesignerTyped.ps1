param([string]$Output)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class G02DesignerImage {
 [DllImport("user32.dll")]public static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [DllImport("user32.dll")]public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr h);
}
'@
[void][G02DesignerImage]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
$assembly='C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\PublicAssemblies\envdte.dll'
Add-Type -Path $assembly
$interop='C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\PublicAssemblies\Microsoft.VisualStudio.Interop.dll'
Add-Type -Path $interop
Add-Type -ReferencedAssemblies @($assembly,$interop) -TypeDefinition @'
using System;
using EnvDTE;
public static class G02Dte {
 public static void OpenSolution(object raw,string path){DTE d=(DTE)raw;d.Solution.Open(path);}
 public static string Info(object raw){DTE d=(DTE)raw;return d.Solution.FullName+" | "+d.MainWindow.Caption+" | "+(d.ActiveDocument==null?"":d.ActiveDocument.FullName);}
 public static string Open(object raw,string path){DTE d=(DTE)raw;Window w=d.ItemOperations.OpenFile(path,Constants.vsViewKindDesigner);return w.Caption;}
 public static string SaveClose(object raw,string root){DTE d=(DTE)raw;Document doc=d.ActiveDocument;if(doc==null||!doc.FullName.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("Refusing save outside snapshot");doc.Save();string p=doc.FullName;doc.Close(vsSaveChanges.vsSaveChangesNo);return p;}
}
'@
[void][IO.Directory]::CreateDirectory($Output)
$process=Start-Process 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe' -ArgumentList '"C:\Users\Lenovo\AppData\Local\Temp\opencode\G02DesignerFinal\CafeManager.slnx"' -PassThru
Start-Sleep -Seconds 15
$dte=[Runtime.InteropServices.Marshal]::GetActiveObject('VisualStudio.DTE.18.0')
$root='C:\Users\Lenovo\AppData\Local\Temp\opencode\G02DesignerFinal'
$solution=Join-Path $root 'CafeManager.slnx'
[G02Dte]::OpenSolution($dte,$solution)
Start-Sleep -Seconds 12
$records=[Collections.Generic.List[object]]::new()
Write-Output ([G02Dte]::Info($dte))
foreach($path in @('Forms\MainForm.cs','Forms\LoginForm.cs','Forms\BootstrapForm.cs','Forms\CatalogEditorDialog.cs','Modules\Catalogs\CatalogsControl.cs')){
 $item=[ordered]@{File=$path;Status='BLOCKED';Open=$null;After=$null;Error=$null}
 try{$full=Join-Path $root ('src\CafeManager.WinForms\'+$path);$item.Open=[G02Dte]::Open($dte,$full);Start-Sleep -Seconds 5;$item.Info=[G02Dte]::Info($dte);if($item.Open -notmatch 'Design'){throw 'Designer view not confirmed'}
  $p=Get-Process devenv|Where-Object {$_.MainWindowTitle -like '*[[]Design[]]*'}|Select-Object -First 1
  $window=[System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
  $item.Elements=@($window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|ForEach-Object {$_.Current.Name})
  $rect=$window.Current.BoundingRectangle;$b=[Drawing.Bitmap]::new([int]$rect.Width,[int]$rect.Height);$g=[Drawing.Graphics]::FromImage($b)
  try{$dc=$g.GetHdc();try{[void][G02DesignerImage]::PrintWindow($p.MainWindowHandle,$dc,2)}finally{$g.ReleaseHdc($dc)};$b.Save((Join-Path $Output (([IO.Path]::GetFileNameWithoutExtension($path))+'-designer.png')))}finally{$g.Dispose();$b.Dispose()}
  if($item.Elements -match 'To prevent possible data loss|could not be shown|The designer cannot|Failed to parse'){throw 'Native Designer error page'}
  [void][G02Dte]::SaveClose($dte,$root);$item.After=[G02Dte]::Open($dte,$full);Start-Sleep -Seconds 3;$item.Status='OBSERVED'}catch{$item.Error=$_.Exception.Message}finally{$records.Add($item)}
}
[IO.File]::WriteAllText((Join-Path $Output 'typed-designer-results.json'),($records|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
$records|Format-List
