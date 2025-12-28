# Deploy DocFX documentation to docs/ directory for GitHub Pages
$ErrorActionPreference = "Stop"

Write-Host "Deploying API Documentation..." -ForegroundColor Cyan

# Build documentation
Write-Host "Building documentation..." -ForegroundColor Cyan
& "$PSScriptRoot/build-docfx.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build documentation" -ForegroundColor Red
    exit 1
}

# Copy to docs/ directory
$sourceDir = "$PSScriptRoot/../Docs_/_site"
$targetDir = "$PSScriptRoot/../docs"

if (Test-Path $targetDir) {
    Write-Host "Cleaning existing docs directory..." -ForegroundColor Yellow
    Remove-Item -Path $targetDir -Recurse -Force
}

Write-Host "Copying documentation to docs/..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
Copy-Item -Path "$sourceDir/*" -Destination $targetDir -Recurse -Force

Write-Host "`nDocumentation deployed successfully!" -ForegroundColor Green
Write-Host "Output: docs/index.html" -ForegroundColor Green
Write-Host "`nTo preview locally: docfx serve docs" -ForegroundColor Yellow

