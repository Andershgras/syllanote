[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot `
    "Syllanote\src\Syllanote.Desktop\Syllanote.Desktop.csproj"
$projectDirectory = Split-Path -Parent $projectPath

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$versionNode = $project.SelectSingleNode("/Project/PropertyGroup/Version")
$version = if ($null -eq $versionNode) {
    $null
}
else {
    $versionNode.InnerText
}

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "The desktop project does not define a Version value."
}

$publishDirectory = Join-Path $projectDirectory `
    "bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\publish"
$artifactsDirectory = Join-Path $repositoryRoot "artifacts"
$artifactName = "Syllanote-v$version-win-x64"
$zipPath = Join-Path $artifactsDirectory "$artifactName.zip"
$checksumPath = "$zipPath.sha256"

dotnet restore $projectPath `
    -r win-x64 `
    -p:Platform=x64 `
    -m:1
if ($LASTEXITCODE -ne 0) {
    throw "Restore failed with exit code $LASTEXITCODE."
}

dotnet clean $projectPath `
    -c Release `
    -r win-x64 `
    -p:Platform=x64 `
    -m:1 `
    -v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "Clean failed with exit code $LASTEXITCODE."
}

dotnet publish $projectPath `
    -c Release `
    -p:Platform=x64 `
    -p:PublishProfile=win-x64 `
    -m:1 `
    --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Publish failed with exit code $LASTEXITCODE."
}

$executablePath = Join-Path $publishDirectory "Syllanote.Desktop.exe"
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "The publish completed without producing $executablePath."
}

New-Item -ItemType Directory -Path $artifactsDirectory -Force | Out-Null

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (Test-Path -LiteralPath $checksumPath) {
    Remove-Item -LiteralPath $checksumPath -Force
}

Compress-Archive `
    -Path (Join-Path $publishDirectory "*") `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal

$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
$checksum = "$($hash.Hash.ToLowerInvariant())  $([System.IO.Path]::GetFileName($zipPath))"
Set-Content -LiteralPath $checksumPath -Value $checksum

Write-Host "Portable release created:"
Write-Host "  $zipPath"
Write-Host "SHA-256:"
Write-Host "  $($hash.Hash)"
