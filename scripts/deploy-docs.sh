#!/bin/bash
# Deploy DocFX documentation to docs/ directory for GitHub Pages
set -euo pipefail

echo "Deploying API Documentation..."

# Build documentation
echo "Building documentation..."
"$(dirname "$0")/build-docfx.sh"
if [ $? -ne 0 ]; then
    echo "Failed to build documentation"
    exit 1
fi

# Copy to docs/ directory
SOURCE_DIR="$(dirname "$0")/../Docs_/_site"
TARGET_DIR="$(dirname "$0")/../docs"

if [ -d "$TARGET_DIR" ]; then
    echo "Cleaning existing docs directory..."
    rm -rf "$TARGET_DIR"
fi

echo "Copying documentation to docs/..."
mkdir -p "$TARGET_DIR"
cp -r "$SOURCE_DIR"/* "$TARGET_DIR/"

echo ""
echo "Documentation deployed successfully!"
echo "Output: docs/index.html"
echo ""
echo "To preview locally: docfx serve docs"

