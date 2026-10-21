$ErrorActionPreference = 'Stop'
$version = '1.0.4258.31'
$expectedHash = '56F7F4B8BF9AEE4B8EFEFBBDD4F67D5F74EBD1B100ED0806DA71BF76AF481AA9'
$workspace = $PSScriptRoot
$archive = Join-Path $workspace 'work/webview2-sdk.zip'
$destination = Join-Path $workspace 'work/webview2'
New-Item -ItemType Directory -Force -Path (Join-Path $workspace 'work') | Out-Null
if (-not (Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest -UseBasicParsing "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/$version/microsoft.web.webview2.$version.nupkg" -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expectedHash) { throw 'O pacote WebView2 não corresponde à versão verificada.' }
Expand-Archive -LiteralPath $archive -DestinationPath $destination -Force
Write-Output 'Componente Microsoft WebView2 preparado. Execute build.ps1.'
