param([switch]$SkipTests, [string]$OutputDirectory = 'outputs')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'O compilador do .NET Framework não foi encontrado neste Windows.' }
$outputDir = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
$workDir = Join-Path $projectRoot 'work'
New-Item -ItemType Directory -Force -Path $outputDir,$workDir | Out-Null
function Copy-IfChanged([string]$source,[string]$target) {
    if ((Test-Path -LiteralPath $target) -and ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $target).Hash)) { return }
    Copy-Item -LiteralPath $source -Destination $target -Force
}
$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Recurse -Filter '*.cs' | Select-Object -ExpandProperty FullName
$references = @('/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll','/r:System.Security.dll','/r:System.Net.Http.dll')
$webview = Join-Path $projectRoot 'work/webview2/lib/net462'
if (-not (Test-Path -LiteralPath (Join-Path $webview 'Microsoft.Web.WebView2.Core.dll'))) { throw 'Falta o SDK Microsoft WebView2. Execute preparar-webview.ps1 uma vez.' }
foreach ($name in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll')) {
    $references += "/r:$(Join-Path $webview $name)"
    Copy-IfChanged (Join-Path $webview $name) (Join-Path $workDir $name)
    Copy-IfChanged (Join-Path $webview $name) (Join-Path $outputDir $name)
}
foreach ($target in @($workDir,$outputDir)) { Copy-IfChanged (Join-Path $projectRoot 'work/webview2/runtimes/win-x64/native/WebView2Loader.dll') (Join-Path $target 'WebView2Loader.dll') }
$testSourceFiles = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests') -Recurse -Filter '*.cs' | Select-Object -ExpandProperty FullName
$resourceFiles = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src/Localization') -Filter '*.json'
$resourceArguments = @($resourceFiles | ForEach-Object { "/resource:$($_.FullName),RoomPilot.Localization.$($_.Name)" })
$testExecutable = Join-Path $workDir 'RoomPilot.Tests.exe'
& $compiler /nologo /target:exe /platform:anycpu /optimize+ /main:RoomPilot.Tests.TestRunner "/out:$testExecutable" @references @resourceArguments @sourceFiles @testSourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes.' }
& $testExecutable --icon (Join-Path $workDir 'app.ico')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar o ícone.' }
if (-not $SkipTests) {
    & $testExecutable
    if ($LASTEXITCODE -ne 0) { throw 'Os testes falharam.' }
}
$application = Join-Path $outputDir 'room-pilot.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /main:RoomPilot.Program "/win32manifest:$(Join-Path $projectRoot 'src/app.manifest')" "/win32icon:$(Join-Path $workDir 'app.ico')" "/out:$application" @references @resourceArguments @sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o aplicativo.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $outputDir 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.en.md') -Destination (Join-Path $outputDir 'README.en.md') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination (Join-Path $outputDir 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'SECURITY.md') -Destination (Join-Path $outputDir 'SECURITY.md') -Force
$outputDocs = Join-Path $outputDir 'docs'
New-Item -ItemType Directory -Force -Path $outputDocs | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/ARCHITECTURE.md') -Destination (Join-Path $outputDocs 'ARCHITECTURE.md') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/I18N.md') -Destination (Join-Path $outputDocs 'I18N.md') -Force
$licenses = Join-Path $outputDir 'licenses'
New-Item -ItemType Directory -Force -Path $licenses | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'work/webview2/LICENSE.txt') -Destination (Join-Path $licenses 'Microsoft-WebView2-LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'work/webview2/NOTICE.txt') -Destination (Join-Path $licenses 'Microsoft-WebView2-NOTICE.txt') -Force
Write-Output "Aplicativo criado: $application"
