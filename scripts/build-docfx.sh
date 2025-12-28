#!/bin/bash
set -e

echo "Building ZenECS API Documentation with DocFX..."

# Check if DocFX is installed
if ! command -v docfx &> /dev/null; then
    echo "DocFX is not installed. Installing..."
    dotnet tool install -g docfx
    if [ $? -ne 0 ]; then
        echo "Failed to install DocFX. Please install manually: dotnet tool install -g docfx"
        exit 1
    fi
fi

# Build Core project to generate XML documentation
echo "Building ZenECS.Core to generate XML documentation..."
cd "$(dirname "$0")/../src"
dotnet build ZenECS.Core.csproj -c Release /p:GenerateDocumentationFile=true
if [ $? -ne 0 ]; then
    echo "Failed to build ZenECS.Core"
    exit 1
fi

# Build Adapter Unity project to generate XML documentation
echo "Building ZenECS.Adapter.Unity to generate XML documentation..."
dotnet build ZenECS.Adapter.Unity.csproj -c Release /p:GenerateDocumentationFile=true
if [ $? -ne 0 ]; then
    echo "Failed to build ZenECS.Adapter.Unity"
    exit 1
fi

# Run DocFX
echo "Running DocFX..."
cd "$(dirname "$0")/../Docs_"
docfx docfx.json
if [ $? -ne 0 ]; then
    echo "DocFX build failed"
    exit 1
fi

echo ""
echo "Documentation built successfully!"
echo "Output: Docs_/_site/index.html"
echo ""
echo "To preview, run: docfx serve Docs_/_site"

