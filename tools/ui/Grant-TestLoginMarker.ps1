param([string]$ManifestPath)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$ManifestPath) { $ManifestPath = Join-Path $taskRoot 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/manual-g03-20261019/manifest.json' }
$project = Join-Path $taskRoot 'docs/tasks/temporary-test-login/runs/PermissionDiagnostic/PermissionDiagnostic.csproj'
$output = Join-Path $taskRoot 'docs/tasks/temporary-test-login/runs/permission-bin'
& dotnet build $project -c Debug -o $output -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Không build được công cụ cấp quyền marker test.' }
& dotnet (Join-Path $output 'PermissionDiagnostic.dll') --provision $ManifestPath (Join-Path $taskRoot 'docs/tasks/temporary-test-login/runs/marker-provision.json')
if ($LASTEXITCODE -ne 0) { throw 'Không cấp được quyền marker test; không tự đổi DB hoặc account.' }
