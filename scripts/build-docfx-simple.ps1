# Simple DocFX Build Script (no execution policy issues)
# Run this with: powershell -ExecutionPolicy Bypass -File build-docfx-simple.ps1

$ErrorActionPreference = "Stop"

Write-Host "Building ZenECS API Documentation with DocFX..." -ForegroundColor Cyan

# Build Core project
Write-Host "Building ZenECS.Core..." -ForegroundColor Cyan
Set-Location "$PSScriptRoot/../src"
dotnet build ZenECS.Core.csproj -c Release /p:GenerateDocumentationFile=true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build ZenECS.Core" -ForegroundColor Red
    exit 1
}

# Build Adapter Unity project
Write-Host "Building ZenECS.Adapter.Unity..." -ForegroundColor Cyan
dotnet build ZenECS.Adapter.Unity.csproj -c Release /p:GenerateDocumentationFile=true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build ZenECS.Adapter.Unity" -ForegroundColor Red
    exit 1
}

# Run DocFX
Write-Host "Running DocFX..." -ForegroundColor Cyan
Set-Location "$PSScriptRoot/../Docs_"
docfx docfx.json
if ($LASTEXITCODE -ne 0) {
    Write-Host "DocFX build failed" -ForegroundColor Red
    exit 1
}

Write-Host "`nDocumentation built successfully!" -ForegroundColor Green
Write-Host "Output: Docs_/_site/index.html" -ForegroundColor Green
Write-Host "`nTo preview, run: docfx serve Docs_/_site" -ForegroundColor Yellow

Set-Location "$PSScriptRoot/.."

