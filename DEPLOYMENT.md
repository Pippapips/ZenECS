# Documentation Deployment Guide

This guide explains how to deploy ZenECS documentation to GitHub Pages.

## Automatic Deployment

Documentation is automatically deployed via GitHub Actions when:

- Changes are pushed to `main` branch
- Files in `src/`, `Packages/`, `Docs_/`, or `Docs/` are modified
- Workflow is manually triggered via `workflow_dispatch`

## Deployment Process

The GitHub Actions workflow (`docs-deploy.yml`) performs the following steps:

1. **Checkout** repository
2. **Setup .NET** SDK 8.0
3. **Install DocFX** tool
4. **Build** ZenECS.Core project (generate XML docs)
5. **Build** ZenECS.Adapter.Unity project (generate XML docs)
6. **Build DocFX** documentation site
7. **Prepare** `docs/` directory
8. **Copy** built site to `docs/` folder
9. **Deploy** to GitHub Pages

## GitHub Pages Setup

### Initial Setup

1. Go to repository **Settings** → **Pages**
2. Under **Source**, select:
   - **Deploy from a branch**
   - **Branch**: `main`
   - **Folder**: `/docs`
3. Click **Save**

### Verify Deployment

After setup, documentation will be available at:
- `https://[username].github.io/[repository]/`

Example:
- `https://pippapips.github.io/ZenECS_deprecated/`

## Manual Deployment

### Local Build and Deploy

```powershell
# Build documentation
.\scripts\build-docfx.ps1

# Deploy to docs/ folder
.\scripts\deploy-docs.ps1

# Commit and push
git add docs/
git commit -m "Update documentation"
git push
```

### Using GitHub Actions UI

1. Go to **Actions** tab
2. Select **Deploy Documentation to GitHub Pages** workflow
3. Click **Run workflow**
4. Select branch (usually `main`)
5. Click **Run workflow**

## Troubleshooting

### Deployment Not Working

**Issue:** Documentation not updating on GitHub Pages

**Solutions:**
- Check GitHub Pages is enabled (Settings → Pages)
- Verify workflow runs successfully (Actions tab)
- Ensure `docs/` folder contains built files
- Check workflow logs for errors

### Build Failures

**Issue:** Workflow fails to build documentation

**Solutions:**
- Check .NET SDK version compatibility
- Verify DocFX installation
- Review build logs for compilation errors
- Ensure XML documentation is generated

### Missing Files

**Issue:** Some documentation files missing

**Solutions:**
- Verify source files exist in `Docs/` and `Docs_/`
- Check DocFX build logs for warnings
- Ensure file paths are correct in `docfx.json`
- Review `.gitignore` doesn't exclude needed files

## Alternative: gh-pages Branch

If you prefer deploying to `gh-pages` branch instead of `docs/` folder:

1. Use the legacy workflow: `.github/workflows/docs-deploy-legacy.yml`
2. Rename to `docs-deploy.yml` (replace current)
3. Update GitHub Pages source to `gh-pages` branch

## See Also

- [GitHub Pages Documentation](https://docs.github.com/en/pages)
- [DocFX Documentation](https://dotnet.github.io/docfx/)
- [Documentation Guidelines](Docs/references/documentation-guidelines.md)

