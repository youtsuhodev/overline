# Overline - build the Windows installer (.exe)
# 1. Builds + publishes a self-contained win-x64 Overline.exe
# 2. Compiles it into an Inno Setup installer
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Resolve-Dotnet {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidate = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    if (Test-Path $candidate) { return $candidate }
    throw '.NET SDK introuvable. Installe le SDK 10.0.'
}

function Resolve-Iscc {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    throw 'Inno Setup 6 introuvable. Installe-le depuis https://jrsoftware.org/isinfo.php'
}

function Get-Version {
    $props = Get-Content -Path 'Directory.Build.props' -Raw
    $m = [regex]::Match($props, '<Version>\s*([^<]+?)\s*</Version>')
    if ($m.Success) { return $m.Groups[1].Value.Trim() }
    return '0.1.0'
}

$dotnet = Resolve-Dotnet
$iscc = Resolve-Iscc
$version = Get-Version
$publishDir = Join-Path $root 'artifacts\publish\win-x64'
$exe = Join-Path $publishDir 'Overline.exe'
$installerDir = Join-Path $root 'artifacts\installer'

Write-Host "[Overline] Version: $version"

if (-not $SkipBuild) {
    Write-Host "[Overline] Build..."
    & $dotnet build Overline.slnx -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Echec du build.' }

    Write-Host "[Overline] Publish win-x64 (self-contained)..."
    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    & $dotnet publish 'Overline.App\Overline.App.csproj' `
        -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:PublishReadyToRun=false -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw 'Echec du publish.' }
}

if (-not (Test-Path $exe)) {
    throw "Overline.exe introuvable: $exe (lance d'abord un publish)."
}

Write-Host "[Overline] Compiling installer..."
New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
& $iscc /Qp "/DAppVersion=$version" (Join-Path $root 'installer\Overline.iss')
if ($LASTEXITCODE -ne 0) { throw 'Echec de la compilation Inno Setup.' }

Write-Host "`n[Overline] Termine ! Installateur:"
Get-ChildItem $installerDir -Filter *.exe | ForEach-Object { Write-Host "  $($_.FullName)" }
Write-Host "`n[Overline] Portable: $exe"