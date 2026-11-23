param(
    [string]$DotNet = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path (Split-Path -Parent $PSScriptRoot) 'PcAudio.csproj'
$version = (& $DotNet msbuild $project -nologo -getProperty:Version | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $version -notmatch '^\d+\.\d+\.\d+(?:-[a-zA-Z0-9.-]+)?$') {
    throw "Não foi possível ler a versão do projeto: $version"
}

$archiveName = "TocaNoPc-$version-win-x64.zip"
$destination = Join-Path $PSScriptRoot $archiveName
$workRoot = (New-Item -ItemType Directory -Force -Path (Join-Path (Split-Path -Parent $project) 'obj/release-packaging')).FullName
$work = (New-Item -ItemType Directory -Path (Join-Path $workRoot ([guid]::NewGuid().ToString('N')))).FullName

try {
    $publish = Join-Path $work 'publish'
    & $DotNet publish $project -c Release -r win-x64 --self-contained true -o $publish -warnaserror
    if ($LASTEXITCODE -ne 0) {
        throw 'A publicação do Toca no PC falhou.'
    }

    $archive = Join-Path $work $archiveName
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publish, $archive)
    [System.IO.File]::Move($archive, $destination, $true)
    Write-Host "Pacote gerado em $destination"
}
finally {
    $resolvedWork = (Resolve-Path -LiteralPath $work).ProviderPath
    $resolvedRoot = (Resolve-Path -LiteralPath $workRoot).ProviderPath
    if ([System.IO.Path]::GetDirectoryName($resolvedWork) -eq $resolvedRoot) {
        Remove-Item -LiteralPath $resolvedWork -Recurse -Force
    }
}
