#!/usr/bin/env bash
# Sprint M3.2 — Deterministic build verification.
#
# Purpose: give CI and local developers a single, repeatable entry point for
# "does the platform build" that fails loudly instead of silently skipping
# when the .NET SDK is unavailable (Execution Honesty Rule,
# docs/milestones/IMPLEMENTATION_GUIDELINES.md §11). This script has NOT been
# executed in the authoring sandbox — no dotnet SDK is available there
# (confirmed in the M3.1/M3.2 reports). It is prepared for execution in a
# real .NET SDK environment, not verified by a run.
#
# Usage: scripts/verify-build.sh [Debug|Release]

set -euo pipefail

CONFIGURATION="${1:-Release}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION="${REPO_ROOT}/AOVIAS.Studio/Aovias.Platform.slnx"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK not found on PATH. This check requires Runtime Verification" >&2
  echo "(see IMPLEMENTATION_GUIDELINES.md §11) and cannot be faked. Install the .NET SDK" >&2
  echo "matching the repo's net10.0 target and re-run this script." >&2
  exit 2
fi

if [ ! -f "${SOLUTION}" ]; then
  echo "ERROR: expected consolidated solution not found at ${SOLUTION}" >&2
  exit 2
fi

echo "== dotnet --version =="
dotnet --version

echo "== dotnet restore (locked mode, deterministic) =="
dotnet restore "${SOLUTION}" --locked-mode

echo "== dotnet build (Configuration=${CONFIGURATION}, deterministic, CI flags) =="
dotnet build "${SOLUTION}" \
  --configuration "${CONFIGURATION}" \
  --no-restore \
  --nologo \
  -p:ContinuousIntegrationBuild=true \
  -p:Deterministic=true \
  -clp:NoSummary=false

echo "== Build complete: ${SOLUTION} (${CONFIGURATION}) =="
