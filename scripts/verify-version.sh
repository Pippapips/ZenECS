#!/usr/bin/env bash
set -euo pipefail

PKG_JSON="Packages/com.zenecs.core/package.json"

# Accept tag as first argument (highest priority)
# If no argument provided, fall back to GITHUB_REF_NAME (for tag push events)
if [[ -n "${1:-}" ]]; then
  TAG="$1"
elif [[ -n "${GITHUB_REF_NAME:-}" ]]; then
  TAG="${GITHUB_REF_NAME}"
else
  echo "❌ Tag is empty. Usage: $0 <tag> or set GITHUB_REF_NAME env var"
  exit 1
fi

if [[ -z "$TAG" ]]; then
  echo "❌ Tag is empty. Usage: $0 <tag> or set GITHUB_REF_NAME env var"
  exit 1
fi

TAG_VER="${TAG#v}"

if ! command -v jq >/dev/null 2>&1; then
  echo "Installing jq..."
  sudo apt-get update -y && sudo apt-get install -y jq
fi

PKG_VER=$(jq -r '.version' "$PKG_JSON")
echo "Tag: $TAG (ver=$TAG_VER)"
echo "package.json version: $PKG_VER"

if [[ "$PKG_VER" != "$TAG_VER" ]]; then
  echo "❌ Version mismatch: package.json=$PKG_VER vs tag=$TAG_VER"
  exit 2
fi
echo "✅ Version verified"
