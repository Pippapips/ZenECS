# DocFX API Documentation

This directory contains the DocFX configuration for generating API documentation from ZenECS source code.

## Building Documentation

### Prerequisites

Install DocFX:
```bash
dotnet tool install -g docfx
```

### Build Locally

**Windows (PowerShell):**
```powershell
.\scripts\build-docfx.ps1
```

**Linux/macOS:**
```bash
./scripts/build-docfx.sh
```

**Manual build:**
```bash
# 1. Build projects to generate XML documentation
cd src
dotnet build ZenECS.Core.csproj -c Release /p:GenerateDocumentationFile=true
dotnet build ZenECS.Adapter.Unity.csproj -c Release /p:GenerateDocumentationFile=true

# 2. Run DocFX
cd ../Docs_
docfx docfx.json
```

### Preview Documentation

After building, preview the documentation:
```bash
docfx serve Docs_/_site
```

Then open http://localhost:8080 in your browser.

## Configuration Files

- **docfx.json** — Main DocFX configuration
- **apiFilter.yml** — API namespace filtering rules
- **toc.yml** — Table of contents structure
- **index.md** — Landing page

## Generated Output

Documentation is generated in `Docs_/_site/` directory.

## CI/CD

Documentation is automatically built on push to `main` or `develop` branches via GitHub Actions (`.github/workflows/docs-build.yml`).

