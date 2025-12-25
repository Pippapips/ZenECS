# DocFX Integration Guide

> Docs / References / DocFX Integration Guide

Complete guide to building, maintaining, and deploying ZenECS documentation using DocFX.

## Overview

ZenECS uses **DocFX** to generate a unified documentation website that combines:

- **API Documentation**: Auto-generated from XML comments in C# source code
- **Conceptual Documentation**: Manual markdown files in `Docs/` directory
- **Samples**: Code examples and tutorials

The final output is a static website in `Docs_/_site/` that can be deployed to GitHub Pages or any web server.

## Architecture

### Directory Structure

```
Repository Root/
├── Docs/                    # Manual documentation (source)
│   ├── overview/
│   ├── getting-started/
│   ├── core/
│   └── ...
│
├── Docs_/                   # DocFX configuration and build
│   ├── docfx.json          # DocFX configuration
│   ├── toc.yml             # Table of contents
│   ├── index.md            # Landing page
│   └── _site/              # Generated website (output)
│       ├── index.html
│       ├── api/            # API documentation
│       └── docs/           # Manual documentation
│
└── src/                    # Source code (XML comments)
    ├── ZenECS.Core.csproj
    └── ZenECS.Adapter.Unity.csproj
```

### Build Process

1. **Build Projects**: Generate XML documentation files
2. **Extract Metadata**: DocFX reads XML comments and code structure
3. **Process Markdown**: Convert markdown files to HTML
4. **Generate Site**: Combine API docs and manual docs into single site
5. **Output**: Static HTML files in `Docs_/_site/`

## Configuration

### docfx.json

Main configuration file located at `Docs_/docfx.json`:

```json
{
  "metadata": [
    {
      "src": [
        {
          "src": "../src",
          "files": [
            "ZenECS.Core.csproj",
            "ZenECS.Adapter.Unity.csproj"
          ]
        }
      ],
      "dest": "api",
      "filter": "apiFilter.yml"
    }
  ],
  "build": {
    "content": [
      {
        "files": ["**/*.{md,yml}"],
        "exclude": ["_site/**"]
      },
      {
        "files": ["../README.md"],
        "src": "..",
        "dest": "."
      },
      {
        "files": ["**/*.md"],
        "src": "../Docs",
        "dest": "docs"
      }
    ],
    "output": "_site"
  }
}
```

### Key Settings

- **metadata**: Source projects for API documentation
- **content**: Markdown files to include
- **output**: Generated site location
- **filter**: API filtering rules (see `apiFilter.yml`)

### apiFilter.yml

Controls which APIs are included in documentation:

```yaml
apiRules:
  - exclude:
      uidRegex: ^ZenECS\.Core\.Internal
      type: Namespace
  - exclude:
      uidRegex: ^ZenECS\.Adapter\.Unity\.Internal
      type: Namespace
  - include:
      uidRegex: ^ZenECS\.Core
      type: Namespace
  - include:
      uidRegex: ^ZenECS\.Adapter\.Unity
      type: Namespace
```

### toc.yml

Table of contents structure:

```yaml
- name: Home
  href: index.md
- name: Getting Started
  href: ../README.md
- name: Documentation
  items:
    - name: Overview
      href: ../Docs/overview/zenecs-at-a-glance.md
    # ... more items
- name: API Reference
  href: api/
```

## Building Documentation

### Prerequisites

1. **.NET SDK 8.0+**: For building projects
2. **DocFX**: Install with `dotnet tool install -g docfx`

### Build Steps

#### Option 1: Using Build Script (Recommended)

**Windows:**
```powershell
.\scripts\build-docfx.ps1
```

**Linux/macOS:**
```bash
./scripts/build-docfx.sh
```

#### Option 2: Manual Build

```powershell
# 1. Build Core project
cd src
dotnet build ZenECS.Core.csproj -c Release /p:GenerateDocumentationFile=true

# 2. Build Adapter Unity project
dotnet build ZenECS.Adapter.Unity.csproj -c Release /p:GenerateDocumentationFile=true

# 3. Run DocFX
cd ..\Docs_
docfx docfx.json
```

### Build Output

Generated files in `Docs_/_site/`:

- `index.html` - Main landing page
- `api/` - API documentation (auto-generated)
- `docs/` - Manual documentation (from `Docs/`)
- `toc.html` - Table of contents
- Static assets (CSS, JS, images)

## Previewing Documentation

### Local Preview Server

```powershell
cd Docs_
docfx serve _site
```

Then open `http://localhost:8080` in your browser.

### Direct File Access

Open `Docs_/_site/index.html` directly in a browser (some features may not work without a server).

## Adding New Documentation

### Adding Manual Documentation

1. **Create file** in appropriate `Docs/` subdirectory
2. **Follow naming convention**: `kebab-case.md`
3. **Add to toc.yml**: Update table of contents
4. **Rebuild**: Run build script
5. **Preview**: Check in browser

### Adding API Documentation

1. **Add XML comments** to source code:
   ```csharp
   /// <summary>
   /// Creates a new entity.
   /// </summary>
   public Entity CreateEntity() { }
   ```
2. **Build project**: Generate XML files
3. **Run DocFX**: API docs auto-generated
4. **Verify**: Check `Docs_/_site/api/`

## Troubleshooting

### Common Issues

#### Issue: API Documentation Missing

**Cause**: XML documentation not generated

**Solution**:
```powershell
# Ensure GenerateDocumentationFile is enabled
dotnet build -c Release /p:GenerateDocumentationFile=true
```

#### Issue: Links Broken

**Cause**: Incorrect relative paths

**Solution**:
- Use relative paths from `Docs/` root
- Test links after build
- Check `toc.yml` structure

#### Issue: Build Fails

**Cause**: Invalid markdown or configuration

**Solution**:
- Check `docfx.json` syntax
- Validate markdown files
- Review build logs

### Build Logs

Check build output for errors:

```powershell
docfx docfx.json --logLevel verbose
```

## Deployment

### GitHub Pages

#### Option 1: Automatic Deployment

GitHub Actions workflow (`.github/workflows/docs-deploy.yml`):

- Triggers on push to `main` branch
- Builds documentation
- Deploys to `gh-pages` branch or `docs/` directory

#### Option 2: Manual Deployment

```powershell
# Build documentation
.\scripts\build-docfx.ps1

# Deploy to docs/ directory
.\scripts\deploy-docs.ps1

# Commit and push
git add docs/
git commit -m "Update documentation"
git push
```

### Other Hosting

The `Docs_/_site/` directory contains a complete static website that can be deployed to:

- **Netlify**: Drag and drop `_site` folder
- **Vercel**: Point to `_site` directory
- **Azure Static Web Apps**: Deploy `_site` folder
- **Any web server**: Copy `_site` contents to web root

## Maintenance

### Keeping Documentation Updated

1. **Update with code changes**: When APIs change, update XML comments
2. **Review quarterly**: Schedule regular documentation reviews
3. **Test builds**: Verify builds work after major changes
4. **Check links**: Validate all links periodically

### Versioning

- **API docs**: Versioned with code releases
- **Manual docs**: Update as features change
- **Changelog**: Document significant doc changes

### CI/CD Integration

GitHub Actions automatically:

- Builds documentation on code changes
- Validates documentation structure
- Deploys to GitHub Pages (if configured)

## Best Practices

### Documentation Structure

- **Organize logically**: Group related topics
- **Use consistent naming**: Follow kebab-case convention
- **Maintain toc.yml**: Keep table of contents updated
- **Cross-reference**: Link related documents

### API Documentation

- **Complete XML comments**: Document all public APIs
- **Include examples**: Show usage in XML comments
- **Link related APIs**: Use `<see cref="..."/>`
- **Document exceptions**: Use `<exception>` tags

### Markdown Files

- **Follow template**: Use standard document structure
- **Test locally**: Preview before committing
- **Validate links**: Check all links work
- **Keep current**: Update with code changes

## Resources

- [DocFX Documentation](https://dotnet.github.io/docfx/)
- [DocFX Markdown Reference](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html)
- [XML Documentation Comments](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)
- [GitHub Pages Documentation](https://docs.github.com/en/pages)

---

**Need Help?** Check build logs or contact the documentation team.

