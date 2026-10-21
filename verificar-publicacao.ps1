param([string]$Root = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$Root = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
$allowed = @(Get-Content -LiteralPath (Join-Path $Root 'public-files.txt') | Where-Object { $_.Trim() -ne '' })
if (($allowed | Select-Object -Unique).Count -ne $allowed.Count) { throw 'A lista pública contém arquivos repetidos.' }
foreach ($relative in $allowed) {
    $file = [IO.Path]::GetFullPath((Join-Path $Root $relative))
    if (-not $file.StartsWith($Root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Caminho fora do projeto na lista pública.' }
    $item = Get-Item -LiteralPath $file
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Arquivo público inválido: $relative" }
    # Report only the file name, never the matching credential.
    $content = [IO.File]::ReadAllText($file)
    $patterns = @('-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----', '\bAKIA[0-9A-Z]{16}\b', '\bgh[pousr]_[A-Za-z0-9]{30,}\b', '\bgithub_pat_[A-Za-z0-9_]{40,}\b', '(?m)^\s*(?:AWS_SECRET_ACCESS_KEY|AMAZON_REFRESH_TOKEN|AMAZON_COOKIE|TUYA_ACCESS_SECRET)\s*=\s*\S+')
    foreach ($pattern in $patterns) { if ($content -match $pattern) { throw "Possível credencial em $relative. Revise o arquivo antes de publicar." } }
}
# Generated/private folders are never public. Unexpected source files must be reviewed.
$entries = @(Get-ChildItem -LiteralPath $Root -Force | Where-Object { $_.Name -notin @('outputs','work','.git','.vs') })
foreach ($entry in $entries) {
    if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Link não permitido na pasta pública: $($entry.Name)" }
    $files = if ($entry.PSIsContainer) { @(Get-ChildItem -LiteralPath $entry.FullName -Recurse -Force -File) } else { @($entry) }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($Root.Length + 1).Replace('\','/')
        if ($relative -notin $allowed) { throw "Arquivo fora da lista pública: $relative" }
    }
}
Write-Output "$($allowed.Count) arquivos públicos verificados. Dados locais e arquivos gerados não fazem parte da publicação."
