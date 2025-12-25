# DocFX Build Script for ZenECS
$ErrorActionPreference = "Stop"

Write-Host "Building ZenECS API Documentation with DocFX..." -ForegroundColor Cyan

# Check if DocFX is installed
$docfxPath = Get-Command docfx -ErrorAction SilentlyContinue
if (-not $docfxPath) {
    Write-Host "DocFX is not installed. Installing..." -ForegroundColor Yellow
    dotnet tool install -g docfx
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install DocFX. Please install manually: dotnet tool install -g docfx" -ForegroundColor Red
        exit 1
    }
}

# Build Core project to generate XML documentation
Write-Host "Building ZenECS.Core to generate XML documentation..." -ForegroundColor Cyan
Push-Location "$PSScriptRoot/../src"
try {
    dotnet build ZenECS.Core.csproj -c Release /p:GenerateDocumentationFile=true
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to build ZenECS.Core" -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}

# Build Adapter Unity project to generate XML documentation
Write-Host "Building ZenECS.Adapter.Unity to generate XML documentation..." -ForegroundColor Cyan
Push-Location "$PSScriptRoot/../src"
try {
    dotnet build ZenECS.Adapter.Unity.csproj -c Release /p:GenerateDocumentationFile=true
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to build ZenECS.Adapter.Unity" -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}

# Run DocFX
Write-Host "Running DocFX..." -ForegroundColor Cyan
Push-Location "$PSScriptRoot/../Docs_"
try {
    docfx docfx.json
    if ($LASTEXITCODE -ne 0) {
        Write-Host "DocFX build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`nDocumentation built successfully!" -ForegroundColor Green
    Write-Host "Output: Docs_/_site/index.html" -ForegroundColor Green
    Write-Host "`nTo preview, run: docfx serve Docs_/_site" -ForegroundColor Yellow
}
finally {
    Pop-Location
}

