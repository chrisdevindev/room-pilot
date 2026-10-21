param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
& (Join-Path $root 'verificar-publicacao.ps1')
if (-not $SkipBuild) { & (Join-Path $root 'build.ps1') }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$output = Join-Path $root 'outputs'
$runtime = @('room-pilot.exe','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','README.md','README.en.md','LICENSE','SECURITY.md','THIRD_PARTY_NOTICES.md','docs/ARCHITECTURE.md','docs/I18N.md','licenses/Microsoft-WebView2-LICENSE.txt','licenses/Microsoft-WebView2-NOTICE.txt')
$source = @(Get-Content -LiteralPath (Join-Path $root 'public-files.txt') | Where-Object { $_.Trim() -ne '' })
Copy-Item -LiteralPath (Join-Path $root 'THIRD_PARTY_NOTICES.md') -Destination (Join-Path $output 'THIRD_PARTY_NOTICES.md') -Force
function Write-PublicArchive([string]$base, [string[]]$files, [string]$destination) {
    $temporary = $destination + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    try {
        $archive = [IO.Compression.ZipFile]::Open($temporary,[IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($relative in $files) {
                $file = Join-Path $base $relative
                $item = Get-Item -LiteralPath $file
                if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Arquivo inválido: $relative" }
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file,$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        } finally { $archive.Dispose() }
        $check = [IO.Compression.ZipFile]::OpenRead($temporary)
        try {
            $actual = @($check.Entries | ForEach-Object { $_.FullName })
            if (@(Compare-Object ($files | Sort-Object) ($actual | Sort-Object)).Count -ne 0) { throw 'O conteúdo do pacote difere da lista permitida.' }
        } finally { $check.Dispose() }
        Move-Item -LiteralPath $temporary -Destination $destination -Force
    } finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force } }
}
Write-PublicArchive $output $runtime (Join-Path $output 'room-pilot-windows-x64.zip')
Write-PublicArchive $root $source (Join-Path $output 'room-pilot-source.zip')
$archives = @('room-pilot-windows-x64.zip','room-pilot-source.zip')
$hashes = foreach ($name in $archives) { $hash = (Get-FileHash -LiteralPath (Join-Path $output $name) -Algorithm SHA256).Hash.ToLowerInvariant(); "$hash  $name" }
[IO.File]::WriteAllLines((Join-Path $output 'SHA256SUMS.txt'),$hashes,[Text.UTF8Encoding]::new($false))
Write-Output 'Pacotes portátil e de código-fonte conferidos e criados em outputs.'
