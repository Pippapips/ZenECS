# Base Path Configuration

This file explains how to configure the base path for GitHub Pages deployment.

## Current Configuration

The repository is currently named `ZenECS_deprecated`, but will be renamed to `ZenECS` for the official release.

### Current Setup (ZenECS_deprecated)
- Base URL: `https://pippapips.github.io/ZenECS_deprecated/`
- Base Path: `/ZenECS_deprecated/` (if using gh-pages branch)

### Future Setup (ZenECS)
- Base URL: `https://pippapips.github.io/ZenECS/`
- Base Path: `/ZenECS/`

## Configuration Location

The base path is configured in `docfx.json`:

```json
{
  "build": {
    "globalMetadata": {
      "_appRootPath": "/ZenECS/"
    }
  }
}
```

## Changing the Base Path

When the repository is renamed from `ZenECS_deprecated` to `ZenECS`:

1. Update `Docs_/docfx.json`:
   ```json
   "_appRootPath": "/ZenECS/"
   ```

2. Or set it via environment variable in GitHub Actions:
   ```yaml
   - name: Build DocFX documentation
     env:
       REPO_NAME: ${{ github.repository_owner }}/${{ github.event.repository.name }}
     run: |
       # Use REPO_NAME to dynamically set base path
       cd Docs_
       docfx docfx.json
   ```

## GitHub Pages Setup

Make sure GitHub Pages is configured to serve from `/docs` folder:
- Repository Settings → Pages → Source: `/docs` folder from `main` branch

