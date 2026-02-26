#!/bin/bash
set -euo pipefail

# Only run in remote (cloud) environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Install .NET 10.0 SDK via apt if not already installed
if ! dotnet --list-sdks 2>/dev/null | grep -q "^10\."; then
  dpkg --configure -a 2>/dev/null || true
  apt-get update -qq 2>/dev/null || true
  apt-get install -y --no-install-recommends dotnet-sdk-10.0
fi

# Allow net6.0 projects to run on the .NET 10 runtime
echo 'export DOTNET_ROLL_FORWARD=LatestMajor' >> "$CLAUDE_ENV_FILE"
export DOTNET_ROLL_FORWARD=LatestMajor

# Restore NuGet packages
cd "$CLAUDE_PROJECT_DIR"
dotnet restore
