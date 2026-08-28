[CmdletBinding()]

param(

    [string] $Root = "D:\citrus_dev\repos\personal\RefinedGem",

    [string] $GameDir = "D:\citrus_steam_games\steamapps\common\Slay the Spire 2"

)



$ErrorActionPreference = 'Stop'

Set-Location $Root



function Ensure-Asset([string]$path, [string]$base64) {

    if (-not (Test-Path $path)) {

        [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String($base64))

    }

}



$assetDir = Join-Path $Root 'assets'

New-Item -ItemType Directory -Force -Path $assetDir | Out-Null



# 1x1 PNG placeholders (purple / dark / teal)

$png = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+X2ZkAAAAASUVORK5CYII='

Ensure-Asset (Join-Path $assetDir 'refined_gem_relic.png') $png

Ensure-Asset (Join-Path $assetDir 'refined_gem_relic_outline.png') $png



Write-Host "[build] Compiling RefinedGem.dll ..."

dotnet build (Join-Path $Root 'RefinedGem.csproj') -c Release

if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }



$dist = Join-Path $Root 'dist\RefinedGem'

New-Item -ItemType Directory -Force -Path $dist | Out-Null

New-Item -ItemType Directory -Force -Path (Join-Path $dist 'locales') | Out-Null

New-Item -ItemType Directory -Force -Path (Join-Path $dist 'assets') | Out-Null



Copy-Item (Join-Path $Root 'build\dll\RefinedGem.dll') (Join-Path $dist 'RefinedGem.dll') -Force

Copy-Item (Join-Path $Root 'mod_manifest.json') (Join-Path $dist 'RefinedGem.json') -Force

Copy-Item (Join-Path $Root 'locales\eng.json') (Join-Path $dist 'locales\eng.json') -Force

Copy-Item (Join-Path $Root 'refined_pool.json') (Join-Path $dist 'refined_pool.json') -Force

Copy-Item (Join-Path $Root 'assets\*') (Join-Path $dist 'assets') -Force -ErrorAction SilentlyContinue



& (Join-Path $Root 'tools\build-pck.ps1') -Root $Root

if ($LASTEXITCODE -ne 0) { throw "PCK build failed" }



if (-not (Test-Path (Join-Path $dist 'RefinedGem.pck'))) {

    throw "RefinedGem.pck was not produced; mod cannot load with has_pck=true"

}



$modsDir = Join-Path $GameDir 'mods\RefinedGem'

New-Item -ItemType Directory -Force -Path $modsDir | Out-Null

try {
    Get-ChildItem -Path $dist | ForEach-Object {
        if ($_.Name -eq 'refined_pool.json') {
            return
        }

        Copy-Item $_.FullName (Join-Path $modsDir $_.Name) -Recurse -Force
    }

    $deployedPool = Join-Path $modsDir 'refined_pool.json'
    if (-not (Test-Path $deployedPool)) {
        Copy-Item (Join-Path $dist 'refined_pool.json') $deployedPool
    }

    $staleManifest = Join-Path $modsDir 'mod_manifest.json'
    if (Test-Path $staleManifest) { Remove-Item $staleManifest -Force }
    Write-Host "[build] Deployed to $modsDir"

}

catch {

    Write-Warning "[build] Deploy failed (close the game if RefinedGem.dll is in use): $_"

}



Write-Host "[build] Output -> $dist"

