[CmdletBinding()]
param(
    [string] $Root = "D:\citrus_dev\repos\personal\RefinedGem",
    [string] $GameDir = "D:\citrus_steam_games\steamapps\common\Slay the Spire 2",
    [switch] $Deploy
)

$ErrorActionPreference = 'Stop'
Set-Location $Root

function Ensure-Asset([string]$path, [string]$base64) {
    if (-not (Test-Path $path)) {
        [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String($base64))
    }
}

$assetDir = Join-Path $Root 'assets'
$pckAssetDir = Join-Path $Root 'pck_root\assets'
New-Item -ItemType Directory -Force -Path $assetDir, $pckAssetDir | Out-Null

# 1x1 PNG placeholders (purple / dark / teal)
$png = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+X2ZkAAAAASUVORK5CYII='
Ensure-Asset (Join-Path $assetDir 'refined_gem_relic.png') $png
Ensure-Asset (Join-Path $assetDir 'refined_gem_relic_outline.png') $png
Ensure-Asset (Join-Path $assetDir 'refined_pool_filter_icon.png') $png
Copy-Item (Join-Path $assetDir '*') $pckAssetDir -Force

Write-Host "[build] Compiling RefinedGem.dll ..."
dotnet build (Join-Path $Root 'RefinedGem.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

$dist = Join-Path $Root 'dist\RefinedGem'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $dist 'locales') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $dist 'assets') | Out-Null

Copy-Item (Join-Path $Root 'build\dll\RefinedGem.dll') (Join-Path $dist 'RefinedGem.dll') -Force
Copy-Item (Join-Path $Root 'mod_manifest.json') $dist -Force
Copy-Item (Join-Path $Root 'mod_manifest.json') (Join-Path $dist 'RefinedGem.json') -Force
Copy-Item (Join-Path $Root 'locales\eng.json') (Join-Path $dist 'locales\eng.json') -Force
Copy-Item (Join-Path $Root 'assets\*') (Join-Path $dist 'assets') -Force -ErrorAction SilentlyContinue

$pckRoot = Join-Path $Root 'pck_root'
if (Test-Path $pckRoot) {
    $godot = Get-Command godot -ErrorAction SilentlyContinue
    if ($godot) {
        Write-Host "[build] Packing RefinedGem.pck ..."
        Push-Location $pckRoot
        & godot --headless --export-pack "Windows Desktop" (Join-Path $dist 'RefinedGem.pck') 2>$null
        if (-not (Test-Path (Join-Path $dist 'RefinedGem.pck'))) {
            & godot --headless --path . --quit-after 1
            if (Test-Path (Join-Path $pckRoot 'RefinedGem.pck')) {
                Copy-Item (Join-Path $pckRoot 'RefinedGem.pck') (Join-Path $dist 'RefinedGem.pck') -Force
            }
        }
        Pop-Location
    }
}

if (-not (Test-Path (Join-Path $dist 'RefinedGem.pck'))) {
    Write-Host "[build] Creating minimal PCK from pck_root via zip fallback ..."
    $zipPath = Join-Path $env:TEMP 'RefinedGem.pck.zip'
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    if (Test-Path $pckRoot) {
        Compress-Archive -Path (Join-Path $pckRoot '*') -DestinationPath $zipPath
        Copy-Item $zipPath (Join-Path $dist 'RefinedGem.pck') -Force
    }
}

if ($Deploy) {
    $modsDir = Join-Path $GameDir 'mods\RefinedGem'
    New-Item -ItemType Directory -Force -Path $modsDir | Out-Null
    Copy-Item (Join-Path $dist '*') $modsDir -Recurse -Force
    Write-Host "[build] Deployed to $modsDir"
}

Write-Host "[build] Output -> $dist"
