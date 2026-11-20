param([Parameter(Mandatory=$true)][string]$Database)
$ErrorActionPreference='Stop'
$exe='D:\Workspace\Nam_3\HK1\CoffeeManager\src\CafeManager.WinForms\bin\Debug\net10.0-windows\CafeManager.WinForms.exe'
$work='D:\Workspace\Nam_3\HK1\CoffeeManager\src\CafeManager.WinForms\bin\Debug\net10.0-windows'
$env:CAFEMANAGER_APP_CONNECTION="Data Source=localhost;Initial Catalog=$Database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=True"
$p=Start-Process -FilePath $exe -WorkingDirectory $work -PassThru
Start-Sleep -Seconds 8
Write-Output $p.Id
Write-Output $p.MainWindowTitle
