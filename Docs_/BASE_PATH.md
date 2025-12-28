# Base Path Configuration

This file explains how to configure the base path for GitHub Pages deployment.

## Current Configuration

The repository is named `ZenECS`.

### Current Setup
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

The base path is already correctly set to `/ZenECS/` in `Docs_/docfx.json`.

## GitHub Pages Setup

Make sure GitHub Pages is configured to serve from `/docs` folder:
- Repository Settings → Pages → Source: `/docs` folder from `main` branch

