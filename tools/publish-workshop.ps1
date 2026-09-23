[CmdletBinding()]
param(
    [string] $Root = $(if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { (Get-Location).Path }),
    [switch] $SkipBuild,
    [switch] $SkipUpload,
    [string] $UploaderUrl = "https://github.com/megacrit/sts2-mod-uploader/releases/download/v0.2.0/ModUploader-win-x64.zip"
)

$ErrorActionPreference = 'Stop'
Set-Location $Root

$workshop = Join-Path $Root 'workshop'
$content = Join-Path $workshop 'content'
$dist = Join-Path $Root 'dist\RefinedGem'
$image = Join-Path $workshop 'image.png'
$config = Join-Path $workshop 'workshop.json'

if (-not (Test-Path $config)) { throw "Missing $config" }

if (-not (Test-Path $image)) {
    Write-Host "[workshop] Generating preview image ..."
    python (Join-Path $Root 'tools\make-workshop-preview.py')
    if ($LASTEXITCODE -ne 0) { throw "Failed to generate workshop/image.png" }
}

$imageBytes = (Get-Item $image).Length
if ($imageBytes -ge 1MB) {
    throw "workshop/image.png must be under 1MB (Steam limit); currently $imageBytes bytes"
}

if (-not $SkipBuild) {
    & (Join-Path $Root 'build.ps1') -Root $Root
    if ($LASTEXITCODE -ne 0) { throw "build.ps1 failed" }
}

if (-not (Test-Path $dist)) { throw "Missing $dist. Run build.ps1 first or omit -SkipBuild." }

Write-Host "[workshop] Staging content from $dist ..."
if (Test-Path $content) { Remove-Item -Recurse -Force $content }
New-Item -ItemType Directory -Force -Path $content | Out-Null
Copy-Item (Join-Path $dist '*') $content -Recurse -Force
[System.IO.File]::WriteAllText((Join-Path $content 'refined_pool.json'), "[]`n")

$staleManifest = Join-Path $content 'mod_manifest.json'
if (Test-Path $staleManifest) { Remove-Item $staleManifest -Force }

$required = @('RefinedGem.dll', 'RefinedGem.pck', 'RefinedGem.json')
foreach ($name in $required) {
    if (-not (Test-Path (Join-Path $content $name))) {
        throw "Workshop package is missing $name"
    }
}

Write-Host "[workshop] Staged $content"

if ($SkipUpload) {
    Write-Host "[workshop] SkipUpload set; not calling ModUploader."
    return
}

$uploaderDir = Join-Path $Root 'tools\bin\sts2-mod-uploader'
$uploaderExe = Join-Path $uploaderDir 'ModUploader.exe'
if (-not (Test-Path $uploaderExe)) {
    Write-Host "[workshop] Downloading ModUploader ..."
    $zip = Join-Path $env:TEMP 'ModUploader-win-x64.zip'
    Invoke-WebRequest -Uri $UploaderUrl -OutFile $zip
    New-Item -ItemType Directory -Force -Path $uploaderDir | Out-Null
    Expand-Archive -Path $zip -DestinationPath $uploaderDir -Force
    Remove-Item $zip -Force
    if (-not (Test-Path $uploaderExe)) {
        $found = Get-ChildItem -Path $uploaderDir -Filter 'ModUploader.exe' -Recurse | Select-Object -First 1
        if ($null -eq $found) { throw "ModUploader.exe not found after extracting $UploaderUrl" }
        $uploaderExe = $found.FullName
        $uploaderDir = $found.Directory.FullName
    }
}

Write-Host "[workshop] Uploading with $uploaderExe ..."
$workspacePath = (Resolve-Path $workshop).Path
Push-Location $uploaderDir
try {
    & $uploaderExe upload -w $workspacePath
    if ($LASTEXITCODE -ne 0) { throw "ModUploader exited with code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

$modId = Join-Path $workshop 'mod_id.txt'
if (Test-Path $modId) {
    Write-Host "[workshop] Workshop item ID: $((Get-Content $modId -Raw).Trim())"
    Write-Host "[workshop] Commit workshop/mod_id.txt so later uploads update this item."
}
