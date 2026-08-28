[CmdletBinding()]
param(
    [string] $Root = $(if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { (Get-Location).Path }),
    [string] $GodotExe = "D:\citrus_dev\repos\personal\cassiopeia\tools\bin\godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe",
    [string] $Gdre = "D:\citrus_dev\repos\personal\cassiopeia\tools\bin\gdre\gdre_tools.exe",
    [string] $GodotProj = "godot",
    [string] $PckRoot = "build/pck_root",
    [string] $OutPck = "dist/RefinedGem/RefinedGem.pck",
    [string] $EngineVer = "4.5.1"
)

$ErrorActionPreference = 'Stop'
Set-Location $Root

if (-not (Test-Path $GodotExe)) { throw "Godot not found at $GodotExe" }
if (-not (Test-Path $Gdre)) { throw "GDRE not found at $Gdre" }

$godot = (Resolve-Path $GodotExe).Path
$gdre = (Resolve-Path $Gdre).Path
$godotDir = Join-Path $Root $GodotProj
$assetSrc = Join-Path $Root 'assets'
$assetDst = Join-Path $godotDir 'assets'

New-Item -ItemType Directory -Force -Path $assetDst | Out-Null
Copy-Item (Join-Path $assetSrc '*.png') $assetDst -Force

function Resolve-CtexFromImport {
    param(
        [string] $ImportFile,
        [string] $FallbackPrefix,
        [string] $ImportedDir
    )
    if (-not (Test-Path $ImportFile)) { return $null }
    $destLine = (Get-Content $ImportFile | Where-Object { $_ -match 'dest_files=|\.ctex' } | Select-Object -First 1)
    if ($destLine -match 'imported/([^"]+\.ctex)') {
        $ctexPath = Join-Path $ImportedDir $Matches[1]
        if (Test-Path $ctexPath) { return Get-Item $ctexPath }
    }
    return Get-ChildItem $ImportedDir -Filter "$FallbackPrefix-*.ctex" -File -ErrorAction SilentlyContinue | Select-Object -First 1
}

Write-Host "[pck] Importing assets with Godot ..."
$prevEap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& $godot --headless --path $GodotProj --import 2>&1 | Out-File -Encoding utf8 build/godot_import.log
$godotExit = $LASTEXITCODE
$ErrorActionPreference = $prevEap
if ($godotExit -ne 0) { throw "Godot import failed (exit $godotExit); see build/godot_import.log" }

Write-Host "[pck] Staging pck root ..."
if (Test-Path $PckRoot) { Remove-Item -Recurse -Force $PckRoot }
New-Item -ItemType Directory -Force -Path (Join-Path $PckRoot ".godot/imported") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $PckRoot "assets") | Out-Null

$imported = Join-Path $godotDir ".godot/imported"
$pngNames = Get-ChildItem $assetDst -Filter '*.png' | ForEach-Object { $_.Name }

foreach ($name in $pngNames) {
    $rel = "assets/$name"
    $importFile = Join-Path $godotDir "$rel.import"
    $ctex = Resolve-CtexFromImport -ImportFile $importFile -FallbackPrefix $name -ImportedDir $imported
    if ($null -eq $ctex) { throw "Missing imported ctex for res://$rel" }

    Copy-Item $ctex.FullName (Join-Path $PckRoot ".godot/imported/$($ctex.Name)") -Force
    Copy-Item (Join-Path $godotDir $rel) (Join-Path $PckRoot $rel) -Force
    Copy-Item $importFile (Join-Path $PckRoot "$rel.import") -Force
    Write-Host "    + res://$rel"
}

$locSrc = Join-Path $Root 'localization'
if (Test-Path $locSrc) {
    $locDst = Join-Path $PckRoot 'localization'
    Copy-Item $locSrc $locDst -Recurse -Force
    Get-ChildItem $locDst -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring((Resolve-Path $PckRoot).Path.Length + 1).Replace('\', '/')
        Write-Host "    + res://$rel"
    }
}

Write-Host "[pck] Creating RefinedGem.pck with GDRE ..."
$outFull = Join-Path $Root $OutPck
New-Item -ItemType Directory -Force -Path (Split-Path $outFull) | Out-Null
& $gdre --headless --pck-create="$((Resolve-Path $PckRoot).Path)" --output="$outFull" --pck-version=2 --pck-engine-version=$EngineVer 2>&1 |
    Out-File -Encoding utf8 build/pck_create.log
if (-not (Test-Path $outFull)) { throw "PCK creation failed; see build/pck_create.log" }

$magic = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($outFull)[0..3])
if ($magic -ne 'GDPC') { throw "Output is not a valid Godot PCK (magic=$magic)" }

Write-Host ("[pck] Done: {0} ({1:N0} bytes)" -f $OutPck, (Get-Item $outFull).Length)
