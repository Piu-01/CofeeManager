param([string]$Output,[string]$CloneRoot='C:\Users\Lenovo\AppData\Local\Temp\opencode\G02B03B06Designer')
$ErrorActionPreference='Stop'
$CloneRoot=[IO.Path]::GetFullPath($CloneRoot)
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
[System.Runtime.InteropServices.ComImport,System.Runtime.InteropServices.Guid("00000016-0000-0000-C000-000000000046"),System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
public interface IG02MessageFilter {
 [System.Runtime.InteropServices.PreserveSig]int HandleInComingCall(int a,IntPtr b,int c,IntPtr d);
 [System.Runtime.InteropServices.PreserveSig]int RetryRejectedCall(IntPtr a,int elapsed,int reject);
 [System.Runtime.InteropServices.PreserveSig]int MessagePending(IntPtr a,int b,int c);
}
public class G02MessageFilter:IG02MessageFilter {
 [System.Runtime.InteropServices.DllImport("ole32.dll")]static extern int CoRegisterMessageFilter(IG02MessageFilter next,out IG02MessageFilter previous);
 public static void Register(){IG02MessageFilter old;CoRegisterMessageFilter(new G02MessageFilter(),out old);}
 public int HandleInComingCall(int a,IntPtr b,int c,IntPtr d){return 0;}
 public int RetryRejectedCall(IntPtr a,int elapsed,int reject){return elapsed<30000?100:-1;}
 public int MessagePending(IntPtr a,int b,int c){return 2;}
}
public static class G02Dte {
 public static IntPtr WindowHandle(object raw){return ((DTE)raw).MainWindow.HWnd;}
 [System.Runtime.InteropServices.DllImport("ole32.dll")]static extern int GetRunningObjectTable(int reserved,out System.Runtime.InteropServices.ComTypes.IRunningObjectTable table);
 public static object Connect(int pid){System.Runtime.InteropServices.ComTypes.IRunningObjectTable table;GetRunningObjectTable(0,out table);System.Runtime.InteropServices.ComTypes.IEnumMoniker iterator;table.EnumRunning(out iterator);var list=new System.Runtime.InteropServices.ComTypes.IMoniker[1];while(iterator.Next(1,list,IntPtr.Zero)==0){string name;list[0].GetDisplayName(null,null,out name);if(name=="!VisualStudio.DTE.18.0:"+pid){object value;table.GetObject(list[0],out value);return value;}}throw new Exception("DTE for test process not yet registered: "+pid);}
 public static void OpenSolution(object raw,string path){DTE d=(DTE)raw;d.Solution.Open(path);foreach(Project p in d.Solution.Projects)if(p.Name=="CafeManager.WinForms")d.Solution.SolutionBuild.StartupProjects=new object[]{p.UniqueName};}
 public static string Info(object raw){DTE d=(DTE)raw;return d.Solution.FullName+" | "+d.MainWindow.Caption+" | "+(d.ActiveDocument==null?"":d.ActiveDocument.FullName);}
 public static string Open(object raw,string path){DTE d=(DTE)raw;ProjectItem item=d.Solution.FindProjectItem(path);if(item==null)throw new Exception("File not resolved in cloned solution hierarchy");if(item.ContainingProject.Name!="CafeManager.WinForms")throw new Exception("Wrong project hierarchy: "+item.ContainingProject.FullName);Window w=item.Open(Constants.vsViewKindDesigner);w.Activate();return w.Caption+" | project="+item.ContainingProject.FullName;}
 public static string SaveClose(object raw,string root){DTE d=(DTE)raw;Document doc=d.ActiveDocument;if(doc==null||!doc.FullName.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("Refusing save outside snapshot");doc.Save();string p=doc.FullName;doc.Close(vsSaveChanges.vsSaveChangesNo);return p;}
}
'@
[G02MessageFilter]::Register()
[void][IO.Directory]::CreateDirectory($Output)
$process=Start-Process 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe' -ArgumentList ('"'+(Join-Path $CloneRoot 'CafeManager.slnx')+'"') -PassThru
Start-Sleep -Seconds 15
$dte=$null
for($attempt=0;$attempt -lt 30 -and -not $dte;$attempt++){try{$dte=[G02Dte]::Connect($process.Id)}catch{try{$candidate=[Runtime.InteropServices.Marshal]::GetActiveObject('VisualStudio.DTE.18.0');$info=[G02Dte]::Info($candidate);if($info.StartsWith($CloneRoot,[StringComparison]::OrdinalIgnoreCase)){$dte=$candidate}}catch{};if(-not $dte){Start-Sleep -Seconds 1}}}
if(-not $dte){throw 'Own test Visual Studio DTE unavailable; refusing another instance'}
$root=$CloneRoot
$currentInfo=[G02Dte]::Info($dte)
$currentSolution=($currentInfo -split '\|')[0].Trim()
if($currentSolution -and -not $currentSolution.StartsWith('C:\Users\Lenovo\AppData\Local\Temp\opencode\',[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing to replace a non-test Visual Studio solution'}
$solution=Join-Path $root 'CafeManager.slnx'
[G02Dte]::OpenSolution($dte,$solution)
Start-Sleep -Seconds 12
$records=[Collections.Generic.List[object]]::new()
Write-Output ([G02Dte]::Info($dte))
foreach($path in @('Forms\MainForm.cs','Forms\LoginForm.cs','Forms\BootstrapForm.cs','Forms\CatalogEditorDialog.cs','Modules\Catalogs\CatalogsControl.cs')){
 $item=[ordered]@{File=$path;Status='BLOCKED';Open=$null;After=$null;Error=$null}
 try{$full=Join-Path $root ('src\CafeManager.WinForms\'+$path);$item.Open=[G02Dte]::Open($dte,$full);Start-Sleep -Seconds 5;$item.Info=[G02Dte]::Info($dte);if($item.Open -notmatch 'Design'){throw 'Designer view not confirmed'}
  $handle=[G02Dte]::WindowHandle($dte)
  $window=[System.Windows.Automation.AutomationElement]::FromHandle($handle)
  $item.Elements=@($window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|ForEach-Object {$_.Current.Name})
  $rect=$window.Current.BoundingRectangle;$b=[Drawing.Bitmap]::new([int]$rect.Width,[int]$rect.Height);$g=[Drawing.Graphics]::FromImage($b)
  try{$dc=$g.GetHdc();try{[void][G02DesignerImage]::PrintWindow($handle,$dc,2)}finally{$g.ReleaseHdc($dc)};$b.Save((Join-Path $Output (([IO.Path]::GetFileNameWithoutExtension($path))+'-designer.png')))}finally{$g.Dispose();$b.Dispose()}
  if($item.Elements -match 'To prevent possible data loss|could not be shown|The designer cannot|Failed to parse'){throw 'Native Designer error page'}
  [void][G02Dte]::SaveClose($dte,$root);$item.After=[G02Dte]::Open($dte,$full);Start-Sleep -Seconds 3;$item.Status='OBSERVED'}catch{$item.Error=$_.Exception.Message}finally{$records.Add($item)}
}
[IO.File]::WriteAllText((Join-Path $Output 'typed-designer-results.json'),($records|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
$records|Format-List
